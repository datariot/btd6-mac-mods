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

## 2026-06-23 — native macOS modding confirmed dead → going CrossOver
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
