using Il2CppInterop.HarmonySupport;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.Startup;
using MelonLoader.Support.Preferences;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader.CoreClrUtils;
using UnityEngine;
using Il2CppInterop.Common;
using Microsoft.Extensions.Logging;
using MelonLoader.Utils;
using System.IO;
using MelonLoader.InternalUtils;

[assembly: MelonLoader.PatchShield]

namespace MelonLoader.Support
{
    internal static class Main
    {
        internal static ISupportModule_From Interface;
        internal static InteropInterface Interop;
        internal static GameObject obj = null;
        internal static SM_Component component = null;

        private static Assembly Il2Cppmscorlib = null;
        private static Type streamType = null;

        private static ISupportModule_To Initialize(ISupportModule_From interface_from)
        {
            Interface = interface_from; 

            foreach (var file in Directory.GetFiles(MelonEnvironment.Il2CppAssembliesDirectory, "*.dll"))
            {
                try
                {
                    Assembly.LoadFrom(file);
                }
                catch { }
            }

            UnityMappers.RegisterMappers();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver(typeof(Il2CppInteropRuntime).Assembly,
                    MacOsIl2CppInteropLibraryResolver);
            }

            Il2CppInteropRuntime runtime = Il2CppInteropRuntime.Create(new()
            {
                DetourProvider = new MelonDetourProvider(),
                UnityVersion = new Version(
                    InternalUtils.UnityInformationHandler.EngineVersion.Major,
                    InternalUtils.UnityInformationHandler.EngineVersion.Minor,
                    InternalUtils.UnityInformationHandler.EngineVersion.Build)
            }).AddLogger(new InteropLogger())
              .AddHarmonySupport();

            Interop = new InteropInterface();
            Interface.SetInteropSupportInterface(Interop);
            runtime.Start();

            if (!LoaderConfig.Current.UnityEngine.DisableConsoleLogCleaner)
                ConsoleCleaner();

            MonoEnumeratorWrapper.Register();

            GetSceneManagerMethods(out MethodInfo sceneLoaded,
                out MethodInfo sceneUnloaded);
            if (sceneLoaded == null)
            {
                MelonLogger.Warning("Failed to find Internal_SceneLoaded method");
                MelonLogger.Warning("Falling back to SupportModule Component Creation");
                SM_Component.Create();
            }
            else
                SceneHandler.Init(sceneLoaded, sceneUnloaded);

            return new SupportModule_To();
        }

        private static void GetSceneManagerMethods(out MethodInfo sceneLoaded,
            out MethodInfo sceneUnloaded)
        {
            sceneLoaded = null;
            sceneUnloaded = null;
            Type scenemanager = null;
            try
            {
                Assembly unityengine = Assembly.Load("UnityEngine.CoreModule");
                if (unityengine != null)
                    scenemanager = unityengine.GetType("UnityEngine.SceneManagement.SceneManager");

                if (scenemanager == null)
                {
                    unityengine = Assembly.Load("UnityEngine");
                    if (unityengine != null)
                        scenemanager = unityengine.GetType("UnityEngine.SceneManagement.SceneManager");
                }
            }
            catch { scenemanager = null; }
            if (scenemanager == null)
                return;

            sceneLoaded = scenemanager.GetMethod("Internal_SceneLoaded", BindingFlags.Public | BindingFlags.Static);
            sceneUnloaded = scenemanager.GetMethod("Internal_SceneUnloaded", BindingFlags.Public | BindingFlags.Static);
        }

        private static IntPtr MacOsIl2CppInteropLibraryResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == "GameAssembly")
            {
                string gameAssemblyPath = Path.Combine(MelonEnvironment.GameExecutablePath, "Contents", "Frameworks", $"{libraryName}.dylib");
                return System.Runtime.InteropServices.NativeLibrary.Load(gameAssemblyPath);
            }
            return IntPtr.Zero;
        }

        private static void ConsoleCleaner()
        {
            // Il2CppSystem.Console.SetOut(new Il2CppSystem.IO.StreamWriter(Il2CppSystem.IO.Stream.Null));
            try
            {
                Il2Cppmscorlib = Assembly.Load("Il2Cppmscorlib");
                if (Il2Cppmscorlib == null)
                    throw new Exception("Unable to Find Assembly Il2Cppmscorlib!");

                streamType = Il2Cppmscorlib.GetType("Il2CppSystem.IO.Stream");
                if (streamType == null)
                    throw new Exception("Unable to Find Type Il2CppSystem.IO.Stream!");

                PropertyInfo propertyInfo = streamType.GetProperty("Null", BindingFlags.Static | BindingFlags.Public);
                if (propertyInfo == null)
                    throw new Exception("Unable to Find Property Il2CppSystem.IO.Stream.Null!");

                MethodInfo nullStreamField = propertyInfo.GetGetMethod();
                if (nullStreamField == null)
                    throw new Exception("Unable to Find Get Method of Property Il2CppSystem.IO.Stream.Null!");

                object nullStream = nullStreamField.Invoke(null, new object[0]);
                if (nullStream == null)
                    throw new Exception("Unable to Get Value of Property Il2CppSystem.IO.Stream.Null!");

                Type streamWriterType = Il2Cppmscorlib.GetType("Il2CppSystem.IO.StreamWriter");
                if (streamWriterType == null)
                    throw new Exception("Unable to Find Type Il2CppSystem.IO.StreamWriter!");

                object nullStreamWriter = null;
                ConstructorInfo[] constructors = streamWriterType.GetConstructors();
                foreach (var ctor in constructors)
                {
                    ParameterInfo[] parameters = ctor.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType == streamType)
                    {
                        nullStreamWriter = ctor.Invoke(new[] { nullStream });
                        break;
                    }
                    else if (parameters.Length == 4 && parameters[0].ParameterType == streamType)
                    {
                        Type encodingType = Il2Cppmscorlib.GetType("Il2CppSystem.Text.Encoding");
                        if (encodingType == null)
                            throw new Exception("Unable to Find Type Il2CppSystem.Text.Encoding!");

                        MethodInfo getUtf8Method = encodingType.GetProperty("UTF8", BindingFlags.Static | BindingFlags.Public)?.GetGetMethod();
                        if (getUtf8Method == null)
                            throw new Exception("Unable to Find Method Il2CppSystem.Text.Encoding.get_UTF8!");

                        object utf8Encoding = getUtf8Method.Invoke(null, null);
                        if (utf8Encoding == null)
                            throw new Exception("Unable to Get Value of Il2CppSystem.Text.Encoding.UTF8!");

                        nullStreamWriter = ctor.Invoke(new[] { nullStream, utf8Encoding, 1024, false });
                        break;
                    }
                }

                if (nullStreamWriter == null)
                    throw new Exception("Unable to Find Suitable Constructor of Type Il2CppSystem.IO.StreamWriter!");

                Type consoleType = Il2Cppmscorlib.GetType("Il2CppSystem.Console");
                if (consoleType == null)
                    throw new Exception("Unable to Find Type Il2CppSystem.Console!");

                MethodInfo setOutMethod = consoleType.GetMethod("SetOut", BindingFlags.Static | BindingFlags.Public);
                if (setOutMethod == null)
                    throw new Exception("Unable to Find Method Il2CppSystem.Console.SetOut!");

                setOutMethod.Invoke(null, new[] { nullStreamWriter });
            }
            catch (Exception ex) { MelonLogger.Warning($"Console Cleaner Failed: {ex}"); }
        }
    }

    internal sealed class MelonDetourProvider : IDetourProvider
    {
        public IDetour Create<TDelegate>(nint original, TDelegate target) where TDelegate : Delegate
        {
            return new MelonDetour(original, target);
        }

        private sealed class MelonDetour : IDetour
        {
            private nint _detourFrom;
            private nint _originalPtr;
            private nint _island; // near trampoline used to avoid the short-function overrun (0 if none)

            private Delegate _target;
            private IntPtr _targetPtr;
            
            private GCHandle _pin;

            /// <summary>
            /// Original method
            /// </summary>
            public nint Target => _detourFrom;

            public nint Detour => _targetPtr;
            public nint OriginalTrampoline => _originalPtr;
            
            public MelonDetour(nint detourFrom, Delegate target)
            {
                _detourFrom = detourFrom;
                _target = target;
                _pin = GCHandle.Alloc(_target);

                // We have to apply immediately because we're gonna be asked for a trampoline right away
                Apply();
            }

            // Dobby (the native inline-hook engine) writes a 12-byte ADRP/ADD/BR patch over the
            // target's prologue and relocates the overwritten instructions into a trampoline. A
            // function must be strictly longer than that patch to leave a valid continue point
            // inside its own body; 16 = 12-byte patch + one instruction of headroom.
            private const int SafeHookMinBytes = 16;

            public unsafe void Apply()
            {
                if (_targetPtr != IntPtr.Zero)
                    return;

                //_targetPtr = Marshal.GetFunctionPointerForDelegate(_target);
                _targetPtr = CoreClrDelegateFixer.GetFixedPointerForDelegate(_target);

                // === arm64 short-function overrun fix ===
                // Dobby patches a target by overwriting its prologue with a branch to the detour. When
                // the detour is >±4GB away (the managed delegate is far from GameAssembly) Dobby emits
                // a 16-byte LDR/BR/literal stub; when it is within ±4GB it emits a 12-byte ADRP/ADD/BR
                // stub. If that stub is longer than the target il2cpp function, the write overruns into
                // the NEXT function and clobbers its entry -> EXC_BAD_INSTRUCTION (SIGILL) when that
                // neighbour is later invoked (the GameAssembly+0x1523028 crash on tower placement).
                //
                // Fix: for short functions, route the detour through a trampoline ISLAND placed within
                // ±4GB of the target. Dobby then emits the smaller 12-byte stub, which fits a 12-byte
                // function exactly (no overrun), and the island forwards to the real detour. The hook
                // stays fully functional. Functions shorter than the 12-byte minimum stub cannot be
                // inline-hooked safely at all, so those are refused (logged) rather than corrupting a
                // neighbour — none have been observed in this game.
                nint detourPtr = _targetPtr;
                int fnLen = Arm64FunctionLength(_detourFrom, 64);
                if (fnLen > 0 && fnLen < SafeHookMinBytes)
                {
                    if (fnLen < DobbyMinPatchBytes)
                    {
                        MelonLogger.Warning(
                            $"[arm64-detour] refusing to hook {fnLen}-byte fn at 0x{_detourFrom:X}: " +
                            "shorter than the 12-byte minimum patch; any inline hook would overrun the neighbour.");
                        _targetPtr = IntPtr.Zero;
                        _originalPtr = _detourFrom;
                        return;
                    }

                    _island = AllocNearIsland(_detourFrom, _targetPtr);
                    if (_island == 0)
                    {
                        MelonLogger.Warning(
                            $"[arm64-detour] could not place a near-island for {fnLen}-byte fn at 0x{_detourFrom:X}; " +
                            "refusing the hook to avoid overrunning the neighbour.");
                        _targetPtr = IntPtr.Zero;
                        _originalPtr = _detourFrom;
                        return;
                    }
                    detourPtr = _island; // Dobby sees a near target -> emits the 12-byte (fits) stub
                }

                // Snapshot the neighbour's entry so we can verify the patch did not overrun it.
                nint neighbour = fnLen > 0 ? _detourFrom + fnLen : 0;
                uint neighbourBefore = neighbour != 0 ? *(uint*)neighbour : 0;

                var addr = _detourFrom;
                nint addrPtr = (nint)(&addr);

                BootstrapInterop.NativeHookAttachDirect(addrPtr, detourPtr);
                NativeStackWalk.RegisterHookAddr((ulong)addrPtr, $"Il2CppInterop detour of 0x{addrPtr:X} -> 0x{_targetPtr:X}");

                _originalPtr = addr;

                if (neighbour != 0 && *(uint*)neighbour != neighbourBefore)
                    MelonLogger.Error(
                        $"[arm64-detour] OVERRUN NOT PREVENTED: hooking {fnLen}B fn @0x{_detourFrom:X} still " +
                        $"clobbered neighbour @0x{neighbour:X} (island=0x{_island:X}). This will SIGILL.");
                else if (_island != 0)
                    MelonLogger.Msg($"[arm64-detour] hooked {fnLen}B fn @0x{_detourFrom:X} via near-island 0x{_island:X} — neighbour @0x{neighbour:X} intact, no overrun.");
            }

            // Dobby's smallest arm64 stub (ADRP/ADD/BR, used when the detour is within ±4GB).
            private const int DobbyMinPatchBytes = 12;

            // Allocate a 16-byte executable trampoline ("island") within ±4GB of `near` that performs an
            // absolute jump to `detour` (LDR x16,#8 ; BR x16 ; .quad detour). Returns 0 if it could not
            // be placed in range. Routing Dobby's hook through this island makes Dobby emit its smaller
            // 12-byte stub at the target instead of the 16-byte one.
            private static unsafe nint AllocNearIsland(nint near, nint detour)
            {
                const nuint pageSize = 16384; // Apple Silicon page size
                nint hint = (nint)((ulong)near & ~(ulong)(pageSize - 1));
                nint p = mmap(hint, pageSize, PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANON, -1, 0);
                if (p == 0 || p == -1)
                    return 0;

                long delta = (long)p - (long)near;
                if (delta < -0x70000000L || delta > 0x70000000L) // keep well inside ADRP/ADD's ±4GB reach
                {
                    munmap(p, pageSize);
                    return 0;
                }

                uint* w = (uint*)p;
                w[0] = 0x58000050u; // ldr x16, #8
                w[1] = 0xD61F0200u; // br  x16
                *(long*)(p + 8) = (long)detour;

                mprotect(p, pageSize, PROT_READ | PROT_EXEC);
                sys_icache_invalidate(p, 16);
                return p;
            }

            [DllImport("libc", EntryPoint = "mmap", SetLastError = true)]
            private static extern nint mmap(nint addr, nuint length, int prot, int flags, int fd, nint offset);
            [DllImport("libc", EntryPoint = "munmap")]
            private static extern int munmap(nint addr, nuint length);
            [DllImport("libc", EntryPoint = "mprotect")]
            private static extern int mprotect(nint addr, nuint length, int prot);
            [DllImport("libSystem.B.dylib", EntryPoint = "sys_icache_invalidate")]
            private static extern void sys_icache_invalidate(nint start, nuint len);
            private const int PROT_READ = 1, PROT_WRITE = 2, PROT_EXEC = 4;
            private const int MAP_PRIVATE = 0x0002, MAP_ANON = 0x1000;

            // Length in bytes of the arm64 function at `start`, by scanning for the terminating
            // RET. Returns 0 if no terminator is found within maxBytes (treated as "long enough").
            private static unsafe int Arm64FunctionLength(nint start, int maxBytes)
            {
                if (start == 0)
                    return 0;
                for (int off = 0; off < maxBytes; off += 4)
                {
                    uint ins = *(uint*)(start + off);
                    if ((ins & 0xFFFFFC1Fu) == 0xD65F0000u) // RET
                        return off + 4;
                    if (ins == 0u) // zero padding between functions
                        return off;
                }
                return 0;
            }

            public unsafe void Dispose()
            {
                if (_targetPtr == IntPtr.Zero)
                    return;

                var addr = _detourFrom;
                nint addrPtr = (nint)(&addr);

                BootstrapInterop.NativeHookDetachDirect(addrPtr, _targetPtr);
                NativeStackWalk.UnregisterHookAddr((ulong)addrPtr);
                CoreClrDelegateFixer.Unpin(_target.Method);

                _targetPtr = IntPtr.Zero;
                _originalPtr = IntPtr.Zero;

                if (_island != 0)
                {
                    munmap(_island, 16384);
                    _island = 0;
                }

                if (_pin.IsAllocated)
                    _pin.Free();
            }

            public T GenerateTrampoline<T>()
                where T : Delegate
            {
                if (_originalPtr == IntPtr.Zero)
                    return null;
                return Marshal.GetDelegateForFunctionPointer<T>(_originalPtr);
            }
        }
    }

    internal class InteropLogger
        : Microsoft.Extensions.Logging.ILogger
    {
        private MelonLogger.Instance _logger = new("Il2CppInterop");

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            string formattedTxt = formatter(state, exception);
            switch (logLevel)
            {
                case LogLevel.Debug:
                case LogLevel.Trace:
                    MelonDebug.Msg(formattedTxt);
                    break;

                case LogLevel.Critical:
                case LogLevel.Error:
                    _logger.Error(formattedTxt);
                    break;

                case LogLevel.Warning:
                    _logger.Warning(formattedTxt);
                    break;

                case LogLevel.Information:
                case LogLevel.None:
                default:
                    _logger.Msg(formattedTxt);
                    break;
            }
        }

        public bool IsEnabled(LogLevel logLevel)
            => logLevel switch
            {
                LogLevel.Debug or LogLevel.Trace => MelonDebug.IsEnabled(),
                _ => true
            };

        public IDisposable BeginScope<TState>(TState state)
            => throw new NotImplementedException();
    }
}
