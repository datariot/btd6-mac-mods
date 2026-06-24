# Lab journal

Newest entries at the top. Keep it casual — date, what we tried, what happened, what's next.

Template:
```
## YYYY-MM-DD — short title
- Tried:
- Result:
- Next:
```

---

## 2026-06-23 (night) — native INJECTION works; native INTEROP generation is the real wall → CrossOver
- Set the Steam launch option correctly → MelonLoader now injects and runs natively. Logs appear in
  `MelonLoader/Logs/`. Confirmed: the loader works on this Mac. 🎉
- New (deeper) error: `Il2CppAssemblyGenerator` → `Il2CppInterop` `Pass16ScanMethodRefs` throws
  `ArgumentOutOfRangeException` in `XrefScannerLowLevel.ExtractTargetAddress` while disassembling
  GameAssembly. Interop assembly generation fails.
- Why it's a REAL wall this time (unlike the launch-method false alarm): Il2CppInterop maintainers say
  macOS lacks the runtime APIs it needs (no way to get the dylib base address; no /proc/self/maps;
  GetCurrentProcess().Modules excludes the dylib) → "Il2CppInterop can't work well on macOS." Plus
  MelonLoader PR #900: IL2CPP on macOS is "very flaky" (LLVM inlining of GameAssembly).
- Consequence: injection ✅ but interop ❌ → mods that touch game code (BTD Mod Helper + all real mods)
  can't load. HelloBTD6 (needs no interop) might limp through but that isn't the goal.
- **Verdict: native modding is NOT viable for real mods on this Mac. Plan B = CrossOver
  (PART-F-CROSSOVER.md).** (CrossOver runs the Windows GameAssembly.dll, which Il2CppInterop scans fine.)

## 2026-06-23 (evening) — [SUPERSEDED by night entry] thought native was fine after the launch-wrapper fix
- Found `melonloader-launch.sh` + `MelonLoader.Bootstrap.dylib` already installed in the BTD6 folder.
  The script's own docs explain it: macOS LaunchServices doesn't pass DYLD_INSERT_LIBRARIES when Steam
  starts a .app, so MelonLoader never injected — NOT because injection is impossible, but because we
  launched the game the normal way. The earlier "no logs" was this, not a hard wall.
- **The fix:** set BTD6's Steam Launch Options to (absolute path required):
    "/Users/hugh/Library/Application Support/Steam/steamapps/common/BloonsTD6/melonloader-launch.sh" %command%
  Generate the exact line on Hugh's Mac with:
    echo "\"$HOME/Library/Application Support/Steam/steamapps/common/BloonsTD6/melonloader-launch.sh\" %command%"
- First try gave "OS error 260" = macOS "file not found" = the launch-options path was wrong. Just a
  typo'd path, no deeper issue. Retry with the exact generated line.
- Status: CrossOver (PART-F-CROSSOVER.md) is now PLAN B, not Plan A. Try the native launch option first.
- Next (when rested): paste exact launch-option line into Steam → Play → diagnose-mac.sh, look for
  "Hello from Hugh's mod!".

## 2026-06-23 — [SUPERSEDED, see entry above] thought native modding was dead → CrossOver
- Tried: Built HelloBTD6 (after fixing the .csproj framework + UnityEngine ref bugs), installed it via
  build-and-install.sh, launched BTD6, ran diagnose-mac.sh.
- Result: **No MelonLoader logs anywhere, nothing touched in the MelonLoader folder.** The loader is
  not initializing on launch = the macOS IL2CPP / SIP injection wall. Confirmed, not a setup mistake.
- Machine facts: **Intel Mac, macOS 15.7.7 (Sequoia)** → CrossOver 26 is the right tool (Whisky never
  supported Intel + is discontinued).
- Next: Follow PART-F-CROSSOVER.md — run the Windows BTD6 + Windows MelonLoader in a CrossOver bottle.
  Goal: see HelloBTD6 print in the Windows console, then use BTD Mod Helper's in-game mod browser.

## 2026-06-22 — repo created, problem scoped
- Tried: Researched whether Mac BTD6 mods exist.
- Result: No Mac-specific mods exist — but that's because mods are cross-platform C#/Harmony DLLs.
  The real blocker is MelonLoader injecting into an **IL2CPP** game on macOS, which is officially
  "flaky." MelonLoader DID install + create the Mods folder (initial macOS support landed), so
  injection is at least *possible* to attempt.
- Next: Run `scripts/diagnose-mac.sh` on Hugh's laptop. Build + drop in `HelloBTD6` and check the
  MelonLoader log for our hello-world line. That single test tells us if this whole thing is viable.
