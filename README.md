# btd6-mac-mods

A father-and-kid lab for getting **Bloons TD 6 mods working on macOS** — and writing our own.

> Started by David & Hugh, June 2026. This is an experiment notebook as much as a code repo.

---

## The honest situation (read this first)

Most people will tell you "BTD6 mods don't work on Mac." That's *almost* right, but the reason
matters, because it tells us where to actually spend effort.

### What a BTD6 "mod" actually is

- Nearly every BTD6 mod is built on [BTD Mod Helper](https://github.com/gurrenm3/BTD-Mod-Helper).
- A mod is just a **C# `.dll`** that uses **[HarmonyX](https://github.com/BepInEx/HarmonyX) patches**
  to hook into the game's code (run code before/after the game's own functions, change values, etc.).
- Harmony patches are **cross-platform**. The same `.dll` is not inherently Windows-only.

So "the mods are written for Windows" isn't really true at the code level. **Nobody writes
Mac-specific mods because the mod code was never the platform-specific part.**

### What the actual blocker is

The thing that's Windows-specific is the **loader** — the program that injects mod DLLs into the
running game. That's [MelonLoader](https://github.com/LavaGang/MelonLoader).

- MelonLoader **recently added initial macOS support**
  ([PR #900](https://github.com/LavaGang/MelonLoader/pull/900)). That's why our install was able to
  register BTD6 and create the `Mods/` folder.
- **BUT** BTD6 is an **IL2CPP** game (Unity compiled to native code), and MelonLoader's macOS support
  is:
  - **Good for Mono games** ✅
  - **"Very flaky / results may vary" for IL2CPP games** ⚠️
- Why IL2CPP on Mac is hard:
  - macOS **System Integrity Protection (SIP)** blocks the normal "hook before the game starts" trick.
  - The compiler (LLVM) **aggressively inlines** `GameAssembly.dylib`, which breaks the function
    hooking MelonLoader depends on.

### The conclusion that shapes this repo

> **Writing our own mod does NOT fix the Mac problem by itself**, because the mod was never the
> blocker. The blocker is getting *any* loader to inject into BTD6 on macOS.

So our experiment order is:

1. **Prove injection works at all** — get a trivial "hello world" loader mod to run. (This repo's
   `HelloBTD6` mod.) This is the make-or-break test.
2. **If injection works** → try dropping in the prebuilt **BTD Mod Helper** + existing Windows mods.
   They may "just work" because they're cross-platform C#.
3. **Only then** → write our own gameplay mods.

If step 1 fails on Hugh's Mac, the answer isn't "write a better mod" — it's "the loader can't inject,"
and we look at alternatives (see below).

---

## Quick start

```bash
# On HUGH'S laptop (where BTD6 + MelonLoader are installed):
./scripts/diagnose-mac.sh
```

That script finds the BTD6 install, confirms it's IL2CPP, checks the MelonLoader version, and pulls
the MelonLoader log so we can see *why* injection did or didn't happen.

---

## The "hello world" injection test (`src/HelloBTD6`)

This is the smallest possible test. It's a **pure MelonLoader mod** (it does NOT use BTD Mod Helper) —
on purpose, so we're testing *only* "can MelonLoader inject and run code on this Mac." It just writes
a line to the MelonLoader log every few seconds.

- If you see `[HelloBTD6] Hello from Hugh's mod!` in the log → **injection works**, we're in business.
- If you never see it → injection is the wall, and gameplay mods won't help.

See `src/HelloBTD6/README.md` for how to build it (needs the .NET SDK).

---

## If injection just won't work on Mac

Honest fallback options, roughly in order of "keeps it on the Mac":

1. **CrossOver / Wine** — run the *Windows* version of BTD6 + MelonLoader under a Windows
   compatibility layer on macOS. This is how a lot of people actually mod BTD6 on Mac. Known to be
   finicky but it sidesteps the IL2CPP-on-macOS problem entirely.
2. **A cloud / spare Windows PC** — saddest option, but the most reliable.
3. **Contribute upstream** — the IL2CPP-on-macOS work in MelonLoader is active. A clean BTD6 repro +
   log is genuinely useful to that project, and is a great "we found a real bug" lesson for Hugh.

We'll record which of these we try in `JOURNAL.md`.

---

## Repo layout

```
btd6-mac-mods/
├── README.md            ← you are here
├── JOURNAL.md           ← lab notebook: what we tried, what happened
├── scripts/
│   └── diagnose-mac.sh  ← run on Hugh's Mac to inspect the install + logs
└── src/
    └── HelloBTD6/       ← minimal injection-test mod
        ├── README.md
        ├── HelloBTD6.csproj
        └── Main.cs
```

## Sources / further reading

- BTD Mod Helper — https://github.com/gurrenm3/BTD-Mod-Helper
- MelonLoader — https://github.com/LavaGang/MelonLoader
- MelonLoader initial macOS support (PR #900) — https://github.com/LavaGang/MelonLoader/pull/900
- MelonLoader Wiki — https://melonwiki.xyz/
- BTD6 modding tutorial (Windows) — https://hemisemidemipresent.github.io/btd6-modding-tutorial/
