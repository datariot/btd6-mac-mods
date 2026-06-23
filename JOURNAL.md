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

## 2026-06-22 — repo created, problem scoped
- Tried: Researched whether Mac BTD6 mods exist.
- Result: No Mac-specific mods exist — but that's because mods are cross-platform C#/Harmony DLLs.
  The real blocker is MelonLoader injecting into an **IL2CPP** game on macOS, which is officially
  "flaky." MelonLoader DID install + create the Mods folder (initial macOS support landed), so
  injection is at least *possible* to attempt.
- Next: Run `scripts/diagnose-mac.sh` on Hugh's laptop. Build + drop in `HelloBTD6` and check the
  MelonLoader log for our hello-world line. That single test tells us if this whole thing is viable.
