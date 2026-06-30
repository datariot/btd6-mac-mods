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
