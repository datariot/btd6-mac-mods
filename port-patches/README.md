# macOS arm64 port patches (MelonLoader)

The BTD6 Apple-Silicon il2cpp port lives at `~/Workspace/macos-il2cpp-port` and is **not** a git
repository. These are durable, version-controlled copies of the source files we patched to make
MelonLoader + Mod Helper work (and exit cleanly) on the macOS arm64 port. Each file here maps to a
path under the port; deploy by building the port and copying the built artifact into the live game.

Live game dir: `~/Library/Application Support/Steam/steamapps/common/BloonsTD6` (referred to as `<game>`).

## Files & what they fix

| File here | Port path | Fix |
|-----------|-----------|-----|
| `MelonLoader/MelonAssembly.cs` | `MelonLoader/MelonLoader/Melons/MelonAssembly.cs` | **x64→AnyCPU auto-convert.** On load failure, macOS-only `TryConvertWindowsX64ToAnyCpu` re-marks a pure-IL x64 (PE32+) mod AnyCPU via Mono.Cecil and retries → community mods are drop-in. |
| `MelonLoader/MelonBase.cs` | `MelonLoader/MelonLoader/Melons/MelonBase.cs` | **Exit crash, part 3.** `UnregisterInstance` skips the per-mod `HarmonyInstance.UnpatchSelf()` on macOS (this runs on the Cmd+Q path *before* `Core.Quit`, once per mod → SIGBUS in `MLMacOSJitCopy`). |
| `MelonLoader/Core.cs` | `MelonLoader/MelonLoader/Core.cs` | **Exit crash, part 2.** `Quit()` skips `HarmonyInstance.UnpatchSelf()` on macOS and `Process.Kill()`s immediately (teardown detour-restore faults during coreclr shutdown). |
| `Bootstrap/osxentry.cpp` | `MelonLoader/MelonLoader.Bootstrap/OSXEntry/osxentry.cpp` | **Exit crash, part 1.** Page-type-aware `MLMacOSJitCopy`: `dladdr`-dispatch — image `__TEXT` → `vm_protect` COW; anonymous MAP_JIT → `pthread_jit` toggle. |
| `MelonLoader/Il2Cpp.Main.cs` | `MelonLoader/Dependencies/SupportModules/Il2Cpp/Main.cs` | **Boot SIGILL (Bug A).** `MelonDetour.Apply` routes <16-byte il2cpp functions through a near-island trampoline so Dobby's stub doesn't overrun the neighbour. |
| `Bootstrap/MethodPointerGuard.cs` | `MelonLoader/MelonLoader.Bootstrap/RuntimeHandlers/Il2Cpp/MethodPointerGuard.cs` | **Placement SIGBUS hardening (NEW, unvalidated).** Guards `il2cpp_runtime_invoke`: if the target `MethodInfo.methodPointer` is in a non-executable page (metadata/__DATA, e.g. a Windows-only mod method absent from the macOS build), skip the call and return null instead of branching into data → converts an uncatchable process-killing SIGBUS into a graceful, catchable failure. Fail-open (only blocks a positively-confirmed non-exec/unmapped pointer); page-cached; kill switch `MELON_INVOKE_GUARD=0`. |
| `Bootstrap/Il2CppHandler.cs` | `MelonLoader/MelonLoader.Bootstrap/RuntimeHandlers/Il2Cpp/Il2CppHandler.cs` | **Wires the guard into `InvokeDetour`** (reads `methodPointer` @ MethodInfo+0, calls `MethodPointerGuard.IsExecutable`, logs the blocked method name via `il2cpp_method_get_name`). |

After both UnpatchSelf guards (Core.cs + MelonBase.cs), **no `HarmonyInstance.UnpatchSelf()` runs on
macOS from any quit path**, so `MLMacOSJitCopy` is never invoked for teardown restore. Verified: 6/6
real Cmd+Q exits produced zero crash reports.

## Build & deploy

```bash
PORT=~/Workspace/macos-il2cpp-port
GAME="$HOME/Library/Application Support/Steam/steamapps/common/BloonsTD6"
cd "$PORT/MelonLoader"

# Managed loader (MelonAssembly.cs, MelonBase.cs, Core.cs) -> MelonLoader.dll
DOTNET_ROLL_FORWARD=LatestMajor dotnet build MelonLoader/MelonLoader.csproj -c Release -f net6 \
  -p:SolutionDir="$PORT/MelonLoader/" -p:ForceRID=osx-arm64 -p:Platform=arm64
cp Output/Release/osx-arm64/MelonLoader/net6/MelonLoader.dll "$GAME/MelonLoader/net6/MelonLoader.dll"

# Native bootstrap (osxentry.cpp) -> MelonLoader.Bootstrap.dylib  (NativeAOT publish)
dotnet publish MelonLoader.Bootstrap/MelonLoader.Bootstrap.csproj -c Release \
  -p:SolutionDir="$PORT/MelonLoader/" -p:ForceRID=osx-arm64 -p:RuntimeIdentifier=osx-arm64 -p:Platform=arm64
cp Output/Release/osx-arm64/MelonLoader.Bootstrap.dylib "$GAME/MelonLoader.Bootstrap.dylib"

# Detour support module (Il2Cpp.Main.cs) -> SupportModules/Il2Cpp.dll
dotnet build Dependencies/SupportModules/Il2Cpp/Il2Cpp.csproj -c Release \
  -p:SolutionDir="$PORT/MelonLoader/" -p:RuntimeIdentifier=osx-arm64 -p:ForceRID=osx-arm64 -p:Platform=arm64
cp Output/Release/osx-arm64/MelonLoader/Dependencies/SupportModules/Il2Cpp.dll \
   "$GAME/MelonLoader/Dependencies/SupportModules/Il2Cpp.dll"
```

> These are full source snapshots, not diffs (the port has no baseline to diff against). To re-apply
> after a port rebuild that overwrote them, copy each file back to its port path above, then build.

## Testing exit crashes

Use **Cmd+Q**, not the `quit` Apple Event — the Apple Event can terminate without running the full
`OnApplicationDefiniteQuit` per-mod teardown, so it gives false passes:

```bash
osascript -e 'tell application "BloonsTD6" to activate' \
          -e 'tell application "System Events" to keystroke "q" using command down'
# then poll ~/Library/Logs/DiagnosticReports for a new BloonsTD6-*.ips
```
