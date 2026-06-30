# Mod Helper on the macOS arm64 port — support status (verified 2026-06-29)

BloonsTD6 **Mod Helper v3.6.5** is **functionally supported** on the Apple-Silicon il2cpp port.
Verified autonomously by `src/ModHelperProbe` (a `BloonsTD6Mod` that self-tests and logs `[PROBE]`):

| Subsystem | Result |
|---|---|
| Mod registration (`BloonsTD6Mod` discovered, `GetMod<T>()`) | ✅ |
| Hook delivery: `OnGameModelLoaded`, `OnTitleScreen`, `OnProfileLoaded`, `OnMainMenu` | ✅ all fire |
| Game-model reads: 2093 towers / 235 bloons / 766 upgrades | ✅ |
| Interop singletons (`Game.instance`, `.model`), `GetTowerFromId` | ✅ |
| In-game UI (Mod Helper "MODS (n)" menu button) | ✅ |
| **Il2Cpp class injection** — custom `ModTower` registers as `ModHelperProbe-ProbeTower` | ✅ |
| Custom `ModUpgrade<T>` injection | ✅ registers in game model |
| Custom embedded texture (PNG → Texture2D) | ✅ `GetTexture`/`TextureExists` |
| Custom sprite reference (`GetSpriteReferenceOrNull`) | ✅ |
| Custom `ModHero` injection | ✅ registers as `ModHelperProbe-ProbeHero` |
| Custom `ModBloon` injection | ✅ registers as `ModHelperProbe-ProbeBloon` |
| No crashes throughout boot/menu/exit | ✅ |

This rests on the detour-layer fixes in the port (near-island short-function fix = Bug A; page-aware
`MLMacOSJitCopy` + exit-teardown skip = Bug B). Class injection in particular needs working native
detours, so its success is downstream of those fixes.

## In-match (verified 2026-06-29)
- `OnMatchStart` and `OnRoundStart` hooks fire in a live match.
- The custom tower appears in the in-game shop with its custom (magenta) icon at the discounted cost.
- The custom tower is **placed and simulated** in a real match via `InGame.instance.bridge.CreateTowerAt(...)`
  (programmatic placement — macOS synthetic mouse input can't drive Unity *world* placement, only UI).

## Not yet verified
- Custom **sprites/textures** from embedded mod resources (the probe tower reuses base art).
- `ModUpgrade` / `ModHero` / `ModBloon` injection (expected to work — same mechanism as `ModTower`).

## Reproduce
`bash test/test-loop.sh` → expects `MOD_HELPER: PASS`.

## Gotcha (recorded)
`SpriteReference` (and any il2cpp type with custom equality) **NREs on `== null`** — its game-side
equality dereferences a null field on a fresh instance. Null-check Il2Cpp objects with
`ReferenceEquals(x, null)` (or `.Pointer == IntPtr.Zero`), never `== null`.

## Community mods (verified 2026-06-29)
Community Mod Helper mods are **architecture-neutral IL**, but they ship built as **x64 (PE32+ AMD64)**
— the Mod Helper template's default. This port's loader rejects x64 assemblies with
`FileLoadException` (coreclr `LoadFromPath`), even though the IL would run fine: a survey of
doombubbles mods (Unlimited5thTiers, CardMonkey, FasterForward, UsefulUtilities, RetryAnywhere,
AutoEscape) found **all** x64, all `ILOnly`.

**They are pure IL, so converting to AnyCPU makes them load + run.** Verified: Unlimited5thTiers v1.1.12,
converted via `tools/anycpu-convert`, loaded and initialized (`14 Mods loaded`, banner printed, no errors).

Install any community mod with:  `tools/install-community-mod.sh <owner/repo>`
(e.g. `tools/install-community-mod.sh doombubbles/Unlimited5thTiers`) — downloads the latest release
DLL, converts to AnyCPU, installs to the Mods folder. Only pure-IL mods convert; mixed-mode mods (with
bundled native Windows code) are refused (would need an arm64 native build).

**Answer to "do community mods work?":** yes — and as of the root-cause fix below, **drop-in**.

## Loader-integrated auto-conversion — DONE (2026-06-30)
The x64→AnyCPU conversion is now **baked into the port's MelonLoader**, so community mods (including ones
downloaded by Mod Helper's in-game browser) load with **no manual step** and **persist across restarts**.

Patch site: `macos-il2cpp-port/MelonLoader/MelonLoader/Melons/MelonAssembly.cs`,
`LoadMelonAssembly(string path)`. When `AssemblyLoadContext.Default.LoadFromAssemblyPath` throws (the x64
rejection), a macOS-only helper `TryConvertWindowsX64ToAnyCpu(path)` re-marks the assembly AnyCPU in place
via **Mono.Cecil** (`Architecture = I386`, `Attributes = ILOnly`) and the load is retried once. It no-ops
for already-AnyCPU assemblies and **refuses mixed-mode** (native-code) assemblies, so the fast path and
non-convertible mods are untouched. Mono.Cecil is already a MelonLoader dependency → zero new packages.
On success the log shows: `Auto-converted x64 Melon Assembly to AnyCPU for load: <name>.dll`.

Build & deploy the patched loader:
```
cd macos-il2cpp-port/MelonLoader
dotnet build MelonLoader/MelonLoader.csproj -c Release -f net6 \
  -p:SolutionDir="$PWD/" -p:ForceRID=osx-arm64 -p:Platform=arm64
cp Output/Release/osx-arm64/MelonLoader/net6/MelonLoader.dll \
   "$HOME/Library/Application Support/Steam/steamapps/common/BloonsTD6/MelonLoader/net6/MelonLoader.dll"
```
Verified: a raw x64 `UsefulUtilities.dll` (PE32+) dropped into `Mods/` auto-converted to PE32 on boot →
`16 Mods loaded`, zero `FileLoadException`, menu reached.

`tools/anycpu-convert` (offline CLI) and `tools/install-community-mod.sh` remain useful for converting
without launching the game, but are no longer required for a mod to load.

> Heads-up: once a mod actually loads, **its own** errors can appear — e.g. `StarshipEnterprise` logs
> `Missing dependency doombubbles/paths-plus-plus`. That's a mod-to-mod dependency (install PathsPlusPlus
> too), not a port/loader problem.
