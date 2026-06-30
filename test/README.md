# Autonomous test harness (macOS arm64 port)

Hands-free boot → verify → quit loop for the BTD6 macOS il2cpp port. No human play needed.

## Files
- `test-loop.sh` — launches the modded game, drives the title screen via an HID click, waits for
  the menu, then asserts three things and quits cleanly:
  - **BUG_A** — the Dobby short-function overrun fix (near-island) held; neighbour intact, 0 overruns.
  - **MOD_HELPER** — Mod Helper hooks fired, content APIs passed, and a custom `ModTower`
    (`ModHelperProbe`) registered into the game model via Il2Cpp class injection.
  - **BUG_B** — a clean Apple-Event quit produced no crash report (exit teardown is skipped on macOS).
- `hidclick.c` — HID-level CGEvent mouse click. Unity ignores AppKit (System Events) synthetic
  clicks, so we post a real CoreGraphics event. Build:
  `clang -arch arm64 -O2 -o hidclick hidclick.c -framework ApplicationServices`

## Run
1. Build + deploy `ModHelperProbe` (src/ModHelperProbe) into the game's Mods/ folder.
2. `clang -arch arm64 -O2 -o test/hidclick test/hidclick.c -framework ApplicationServices`
3. `bash test/test-loop.sh`
