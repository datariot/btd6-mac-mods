#include <sys/resource.h>
#include <dlfcn.h>
#include <string.h>
#include <pthread.h>
#include <unistd.h>
#include <mach/mach.h>
#include <libkern/OSCacheControl.h>

extern "C"
{
    void Init();
    int SetRlimitHook(int resource, rlimit* rlp);
    void MLRegisterDlsymHook(void* detour);
    void* MLRealDlsym(void* handle, const char* symbol);
    int MLMacOSJitCopy(void* dst, const void* src, size_t len);
    void* DlsymHook(void* handle, const char* symbol);
}

#define DYLD_INTERPOSE(_replacement,_replacee) \
       __attribute__((used)) static struct{ const void* replacement; const void* replacee; } _interpose_##_replacee \
       __attribute__ ((section ("__DATA,__interpose"))) = { (const void*)(unsigned long)&_replacement, (const void*)(unsigned long)&_replacee };

int SetRlimitHook(int resource, rlimit* rlp)
{
    int result = setrlimit(resource, rlp);
    Init();
    return result;
}

DYLD_INTERPOSE(SetRlimitHook, setrlimit)

// Detour registered by the managed bootstrap (ModuleSymbolRedirect.Attach).
// Receives the symbol AND its already-resolved real address, and returns either
// MelonLoader's hook (for the runtime entry points) or the real address.
typedef void* (*DlsymDetourFn)(void* handle, const char* symbol, void* real);
static DlsymDetourFn g_dlsymDetour = nullptr;

void MLRegisterDlsymHook(void* detour)
{
    g_dlsymDetour = (DlsymDetourFn)detour;
}

// The real dlsym, exposed to managed code. dyld does not interpose the image
// that declares the interpose, so this call resolves the genuine export. The
// managed side MUST use this (not its own dlsym P/Invoke, whose GOT slot IS
// interposed) so MelonLoader resolves the real mono/il2cpp exports rather than
// its own detours -- otherwise the detours call themselves and recurse.
void* MLRealDlsym(void* handle, const char* symbol)
{
    return dlsym(handle, symbol);
}

#if defined(__APPLE__) && defined(__arm64__)
// Bug B fix: write detour bytes into a mapped image's __TEXT (e.g. GameAssembly.dylib) by
// temporarily flipping the page to writable via copy-on-write, then restoring its EXACT original
// protection. pthread_jit_write_protect_np no-ops on these non-MAP_JIT pages, so a plain memcpy
// there faults SIGBUS (the on-exit teardown crash). Used ONLY for image-backed addresses; coreclr's
// anonymous MAP_JIT trampolines still take the pthread path (vm_protect on them broke the JIT).
static int MLMacOSJitCopyVmProtect(void* dst, const void* src, size_t len)
{
    size_t ps = (size_t)getpagesize();
    uintptr_t start = (uintptr_t)dst & ~(ps - 1);
    uintptr_t end   = ((uintptr_t)dst + len + ps - 1) & ~(ps - 1);
    vm_size_t rlen  = (vm_size_t)(end - start);

    // capture the page's current protection so we can restore it byte-for-byte afterwards.
    vm_address_t region = (vm_address_t)start; vm_size_t rsize = 0;
    vm_region_basic_info_data_64_t bi; mach_msg_type_number_t cnt = VM_REGION_BASIC_INFO_COUNT_64;
    mach_port_t obj;
    kern_return_t kr = vm_region_64(mach_task_self(), &region, &rsize,
                                    VM_REGION_BASIC_INFO_64, (vm_region_info_t)&bi, &cnt, &obj);
    vm_prot_t orig = (kr == KERN_SUCCESS) ? bi.protection : (VM_PROT_READ | VM_PROT_EXECUTE);

    // VM_PROT_COPY forces a private copy so neither the on-disk dylib nor other mappings change.
    kr = vm_protect(mach_task_self(), (vm_address_t)start, rlen, FALSE,
                    VM_PROT_READ | VM_PROT_WRITE | VM_PROT_COPY);
    if (kr != KERN_SUCCESS)
    {
        kr = vm_protect(mach_task_self(), (vm_address_t)start, rlen, FALSE,
                        VM_PROT_READ | VM_PROT_WRITE);
        if (kr != KERN_SUCCESS)
            return 0;
    }
    memcpy(dst, src, len);
    sys_icache_invalidate(dst, len);
    vm_protect(mach_task_self(), (vm_address_t)start, rlen, FALSE, orig);
    return 1;
}
#endif

int MLMacOSJitCopy(void* dst, const void* src, size_t len)
{
#if defined(__APPLE__) && defined(__arm64__)
    if (len == 0)
        return 1;
    if (dst == nullptr || src == nullptr)
        return 0;

    // Page-type-aware dispatch. dladdr resolves a module only for addresses inside a mapped image
    // (dylib/exe segments); those __TEXT pages are NOT MAP_JIT, so the pthread toggle no-ops and a
    // plain memcpy SIGBUSes. Anonymous addresses (coreclr's MAP_JIT JIT trampolines) return no
    // module and must keep the per-thread write-protect toggle.
    Dl_info info;
    if (dladdr(dst, &info) != 0 && info.dli_fname != nullptr)
        return MLMacOSJitCopyVmProtect(dst, src, len);

    if (!pthread_jit_write_protect_supported_np())
        return 0;
    pthread_jit_write_protect_np(0);
    memcpy(dst, src, len);
    sys_icache_invalidate(dst, len);
    pthread_jit_write_protect_np(1);
    return 1;
#else
    return 0;
#endif
}

// The game's engine (UnityPlayer.dylib) resolves the mono/il2cpp entry points
// via dlsym, so we interpose dlsym to return MelonLoader's detours for them.
// plthook cannot patch UnityPlayer.dylib's GOT on modern macOS
// (LC_DYLD_CHAINED_FIXUPS); DYLD interpose is the mechanism that reliably works
// here (it is what triggers Init via setrlimit). Only runtime-entry symbols
// (mono_/il2cpp_) are handed to managed code -- routing every dlsym in the
// process (e.g. AppKit's during startup) through a managed callback is needless
// and unsafe.
void* DlsymHook(void* handle, const char* symbol)
{
    void* real = dlsym(handle, symbol);
    if (g_dlsymDetour != nullptr && symbol != nullptr &&
        (strncmp(symbol, "mono_", 5) == 0 || strncmp(symbol, "il2cpp_", 7) == 0))
        return g_dlsymDetour(handle, symbol, real);
    return real;
}

DYLD_INTERPOSE(DlsymHook, dlsym)
