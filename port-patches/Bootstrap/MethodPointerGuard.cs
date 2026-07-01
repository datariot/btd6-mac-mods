using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace MelonLoader.Bootstrap.RuntimeHandlers.Il2Cpp;

// Port-hardening guard for il2cpp_runtime_invoke on macOS arm64.
//
// Some community mods (built for the Windows build of the game) drive a reflective
// il2cpp invoke of a method whose `methodPointer` resolves into GameAssembly's __DATA
// (il2cpp metadata) rather than executable __TEXT — e.g. a method the macOS il2cpp
// build never generated code for, or an asset path that only exists on Windows. The
// il2cpp invoker then does `blr methodPointer`, branches into data, and the process
// dies with an uncatchable SIGBUS (fetch of a non-executable page).
//
// This guard checks, before forwarding a runtime_invoke, that the target
// methodPointer lives in an EXECUTABLE page. If it doesn't, we skip the call and
// return null instead of branching into data — turning a hard process-killing crash
// into a graceful (catchable) failure that only affects the offending mod action.
//
// Safety: the guard is strictly FAIL-OPEN. It only ever blocks a call when it can
// positively confirm the pointer is null, in an unmapped hole, or in a mapped page
// with no execute permission. Any uncertainty (query failure) allows the call, so a
// legitimate method can never be wrongly skipped. A valid il2cpp methodPointer is by
// definition in executable memory, so the block path is reachable only by the
// pathological data-pointer case above.
internal static class MethodPointerGuard
{
    // Kill switch: set MELON_INVOKE_GUARD=0 to disable entirely.
    internal static readonly bool Enabled =
        Environment.GetEnvironmentVariable("MELON_INVOKE_GUARD") != "0";

    // Verbose: set MELON_INVOKE_GUARD_VERBOSE=1 to log every blocked invoke (default:
    // log only the first few distinct method names, then a periodic count).
    private static readonly bool Verbose =
        Environment.GetEnvironmentVariable("MELON_INVOKE_GUARD_VERBOSE") == "1";

    private const int VM_PROT_EXECUTE = 4;
    private const int VM_REGION_BASIC_INFO_64 = 9;
    private const int PageShift = 14; // 16 KiB pages on arm64 macOS

    [StructLayout(LayoutKind.Sequential)]
    private struct VMRegionBasicInfo64
    {
        public int protection, max_protection;
        public uint inheritance;
        public int shared, reserved;
        public ulong offset;
        public int behavior;
        public ushort user_wired_count;
    }

    [DllImport("libSystem.B.dylib", EntryPoint = "task_self_trap")]
    private static extern uint task_self_trap();

    [DllImport("libSystem.B.dylib", EntryPoint = "mach_vm_region")]
    private static extern int mach_vm_region(uint task, ref ulong address, ref ulong size,
        int flavor, ref VMRegionBasicInfo64 info, ref uint infoCnt, out uint objName);

    // page index -> executable? Code stays code and data stays data, so a definitive
    // verdict is cached forever. Steady state is a pure dictionary hit; only the first
    // touch of each page issues a mach_vm_region query.
    private static readonly ConcurrentDictionary<ulong, bool> PageExec = new();

    private static long _blockedTotal;
    private static readonly ConcurrentDictionary<string, byte> _reported = new();

    // Returns true if `methodPointer` may be safely invoked (executable, or unknown).
    internal static bool IsExecutable(nint methodPointer)
    {
        if (methodPointer == 0)
            return false; // null code pointer: never branch to it

        ulong page = (ulong)methodPointer >> PageShift;
        if (PageExec.TryGetValue(page, out var cached))
            return cached;

        ulong addr = (ulong)methodPointer, size = 0;
        var info = new VMRegionBasicInfo64();
        uint cnt = 9;
        int kr = mach_vm_region(task_self_trap(), ref addr, ref size,
            VM_REGION_BASIC_INFO_64, ref info, ref cnt, out _);
        if (kr != 0)
            return true; // fail-open: can't determine protection -> allow the call

        // mach_vm_region returns the region at-or-above the address; if the pointer is
        // not actually inside it, the pointer sits in an unmapped hole.
        bool contains = (ulong)methodPointer >= addr && (ulong)methodPointer < addr + size;
        if (!contains)
            return false; // unmapped: definitely not code (don't cache; holes may fill)

        bool exec = (info.protection & VM_PROT_EXECUTE) != 0;
        PageExec[page] = exec; // definitive: cache it
        return exec;
    }

    // Called after a blocked invoke to surface a diagnostic instead of a silent crash.
    internal static void ReportBlocked(nint method, nint methodPointer, string? name)
    {
        long n = System.Threading.Interlocked.Increment(ref _blockedTotal);
        string key = name ?? $"0x{method:X}";
        bool firstForName = _reported.TryAdd(key, 0);
        if (Verbose || firstForName || (n % 256) == 0)
        {
            Core.Logger.Warning(
                $"[invoke-guard] blocked il2cpp_runtime_invoke of '{key}': methodPointer 0x{methodPointer:X} " +
                $"is non-executable (metadata/__DATA, not code) — this method is absent from the macOS game build. " +
                $"Returning null instead of crashing. (total blocked: {n})");
        }
    }
}
