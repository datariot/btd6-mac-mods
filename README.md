# btd6-mac-mods

A father-and-kid lab for getting **Bloons TD 6 mods working on macOS** — and writing our own.

> Started by David & Hugh, June 2026. This is an experiment notebook as much as a code repo.

> 🎮 **Is this Hugh, or is it your first time?** Start with **[START-HERE.md](START-HERE.md)** — a
> friendly, step-by-step guide written for an 11-year-old's first time using the Terminal. This README
> below is the **grown-up reference** with all the technical detail.

This README is a **complete, from-scratch guide**: everything you need to install, in order, ending
with our test mod actually running in the game. If you're brand new, just start at
[Part B: Installation](#part-b-installation-from-a-fresh-mac) and follow it top to bottom.

---

## Contents

- [Part A: The honest situation](#part-a-the-honest-situation-read-this-first) — *what we're doing and why*
- [Part B: Installation (from a fresh Mac)](#part-b-installation-from-a-fresh-mac) — *every prerequisite, in order*
- [Part C: Build & run our mod](#part-c-build--run-our-mod)
- [Part D: Reading the result](#part-d-reading-the-result)
- [Part E: Troubleshooting](#part-e-troubleshooting)
- [Part F: If injection just won't work](#part-f-if-injection-just-wont-work-on-mac)
- [Repo layout & sources](#repo-layout)

---

## Part A: The honest situation (read this first)

Most people say "BTD6 mods don't work on Mac." That's *almost* right, but the reason matters because
it tells us where to spend effort.

**What a BTD6 mod actually is:** nearly every BTD6 mod is built on
[BTD Mod Helper](https://github.com/gurrenm3/BTD-Mod-Helper), and each mod is just a **C# `.dll`** that
uses **[HarmonyX](https://github.com/BepInEx/HarmonyX) patches** to hook the game's code. Harmony
patches are **cross-platform** — the same `.dll` isn't inherently Windows-only. **Nobody writes
"Mac mods" because the mod code was never the platform-specific part.**

**The real blocker is the loader.** [MelonLoader](https://github.com/LavaGang/MelonLoader) is what
injects mod DLLs into the running game.

- MelonLoader added **initial macOS support** in March 2025
  ([PR #900](https://github.com/LavaGang/MelonLoader/pull/900)) — that's why our install could register
  BTD6 and create the `Mods/` folder.
- **BUT** BTD6 is an **IL2CPP** game (Unity compiled to native code), and MelonLoader's macOS support
  is **good for Mono games but "very flaky" for IL2CPP**. macOS **SIP** blocks the normal pre-launch
  injection hook, and LLVM inlining in `GameAssembly.dylib` breaks the function hooking MelonLoader
  needs.

**What this means for us:**

> Writing our own mod does NOT fix the Mac problem by itself, because the mod was never the blocker.
> The first real experiment is: *can any loader inject into BTD6 on this Mac at all?*

So our order of operations is:

1. **Prove injection works** — get the tiny `HelloBTD6` test mod (in this repo) to print to the log.
2. **If it works** → drop in BTD Mod Helper + existing Windows mods; they may "just work."
3. **Only then** → write our own gameplay mods.

If step 1 fails, the answer isn't "write a better mod" — it's "the loader can't inject," and we go to
[Part F](#part-f-if-injection-just-wont-work-on-mac).

---

## Part B: Installation (from a fresh Mac)

Do these in order. **Items marked ✅ you've likely already done** (you said MelonLoader is installed) —
skim them to confirm, then focus on **B5 (.NET SDK)**, which is the one piece still needed to build a
mod.

### B0. What we're installing, and why

| # | Thing | Why we need it |
|---|-------|----------------|
| B1 | **Xcode Command Line Tools** | gives us `git` (to get this repo) |
| B2 | **Homebrew** | easiest way to install the .NET SDK |
| B3 | **Steam + Bloons TD 6** | the game itself |
| B4 | **MelonLoader** ✅ | the mod loader that injects our `.dll` |
| B5 | **.NET SDK** | compiles our C# mod into a `.dll`; also provides the .NET 6 runtime IL2CPP games need |

### B1. Xcode Command Line Tools (for git)

```bash
xcode-select --install
```
Click through the popup. (If `git --version` already prints a version, you can skip this.)

### B2. Homebrew

If you don't have it (`brew --version` to check):
```bash
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
```
Then follow the "Next steps" it prints to add `brew` to your PATH.

### B3. Steam + Bloons TD 6 ✅

- Install **Steam**: https://store.steampowered.com/about/
- In Steam, install **Bloons TD 6** (you already own it).
- **Launch BTD6 once normally** and quit. This confirms the base game runs before we add anything.

The Mac install lives at roughly:
```
~/Library/Application Support/Steam/steamapps/common/BloonsTD6/
```

### B4. MelonLoader ✅ (you've done this — here's the record of how)

We use the **official MelonLoader Installer** (the app, not the Windows `.exe`):

1. Download the **macOS `.dmg`** from the installer releases:
   https://github.com/LavaGang/MelonLoader.Installer/releases
2. It isn't notarized by Apple, so clear the quarantine flag once (or right-click → Open → Open):
   ```bash
   xattr -dr com.apple.quarantine "/Applications/MelonLoader Installer.app"
   ```
3. Open **MelonLoader Installer**, let it auto-detect Steam games, **select Bloons TD 6**, pick the
   latest version, and click **Install**.
4. **Launch BTD6 once** so MelonLoader bootstraps. This is what creates the `Mods/` folder and the
   `MelonLoader/Latest.log` we'll read later. Quit after the menu loads.

> If MelonLoader's log later complains about a missing **.NET 6 runtime**, B5 below covers it
> (IL2CPP games need the .NET 6.0 runtime; the SDK includes runtimes).

### B5. .NET SDK (this is the one you still need — to build the mod)

Install the SDK with Homebrew:
```bash
brew install --cask dotnet-sdk
```
…or download it directly: https://dotnet.microsoft.com/download

Verify:
```bash
dotnet --version       # should print 8.x or 9.x
dotnet --list-runtimes # should list runtime(s); used to run the built mod
```

> **Why the SDK and not just the runtime?** The SDK *builds* C# into a `.dll` (what a mod is) **and**
> bundles a runtime. If MelonLoader specifically needs the **.NET 6.0** runtime and yours is newer,
> install it too:
> ```bash
> # pin a 6.0 runtime if the MelonLoader log asks for it
> curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 6.0 --runtime dotnet
> ```

### B6. Get this repo

```bash
git clone <this-repo-url> btd6-mac-mods   # or copy the folder onto Hugh's Mac
cd btd6-mac-mods
```

✅ **Installation done.** You now have: the game, the loader, the build toolchain, and this repo.

---

## Part C: Build & run our mod

`HelloBTD6` is the smallest possible MelonLoader mod. It does **not** change the game — it only logs a
line, so we can confirm injection works on this Mac.

### C1. Inspect the install (also finds the paths the build needs)

```bash
./scripts/diagnose-mac.sh
```
This finds BTD6, confirms it's IL2CPP, prints the **MelonLoader dir**, the **Mods dir**, and tails the
log. Note the MelonLoader path — you may need it in the next step.

### C2. Build the mod

```bash
cd src/HelloBTD6
dotnet build -c Release
```
Output: `bin/Release/HelloBTD6.dll`.

If the build fails on a missing reference, the paths in `HelloBTD6.csproj` don't match this Mac.
Override them without editing the file (use the `MelonLoader dir` from C1):
```bash
dotnet build -c Release -p:MelonDir="/Users/<you>/Library/Application Support/Steam/steamapps/common/BloonsTD6/MelonLoader"
```

### C3. Install the mod into the game

Copy the built `.dll` into the `Mods/` folder that `diagnose-mac.sh` reported, e.g.:
```bash
cp bin/Release/HelloBTD6.dll "$HOME/Library/Application Support/Steam/steamapps/common/BloonsTD6/Mods/"
```
If macOS quarantines the fresh file:
```bash
xattr -dr com.apple.quarantine "$HOME/Library/Application Support/Steam/steamapps/common/BloonsTD6/Mods/HelloBTD6.dll"
```

### C4. Run

Launch BTD6, sit on the main menu ~15 seconds, then quit.

---

## Part D: Reading the result

```bash
./scripts/diagnose-mac.sh   # it tails the log and searches it for "HelloBTD6"
```

- ✅ You see `Hello from Hugh's mod! Injection works.` and `heartbeat` lines →
  **macOS modding is viable.** Next: try BTD Mod Helper + a real mod.
- ❌ No HelloBTD6 lines, or the log shows IL2CPP / injection errors → injection is the wall. Copy the
  errors into `JOURNAL.md` and go to [Part F](#part-f-if-injection-just-wont-work-on-mac).

Either way, **paste the run into `JOURNAL.md`** so we have a record.

---

## Part E: Troubleshooting

| Symptom | Fix |
|---------|-----|
| `dotnet: command not found` | B5 not done, or restart the terminal so PATH updates. |
| Build error: can't find `MelonLoader.dll` / `UnityEngine.*` | HintPaths differ by MelonLoader version (`net6/` vs `Managed/`, IL2CPP interop under `Il2CppAssemblies/`). Fix the path in `HelloBTD6.csproj` or pass `-p:MelonDir=...`. |
| Mod builds but never appears in the log | Confirm the `.dll` is in the **right** `Mods/` folder (diagnose-mac.sh prints it); clear quarantine (C3); make sure you launched the game *after* copying. |
| Log: wrong/missing **.NET runtime** | Install the .NET 6 runtime (see B5 note). |
| Log: IL2CPP errors / SIP / nothing injects | Expected failure mode on macOS IL2CPP. This is the real wall → [Part F](#part-f-if-injection-just-wont-work-on-mac). |
| No `Latest.log` at all | MelonLoader never started — re-run the MelonLoader Installer (B4) and launch the game once. |

---

## Part F: If injection just won't work on Mac

**This is where we ended up** (June 2026): on Hugh's Intel Mac, native MelonLoader injection produced
**zero logs** — the IL2CPP/SIP wall, confirmed. So we're taking the reliable workaround.

👉 **Full kid-friendly walkthrough: [PART-F-CROSSOVER.md](PART-F-CROSSOVER.md)**

The options, in order of "most likely to work":

1. **CrossOver (a "bottle")** ✅ *our chosen route* — run the **Windows** BTD6 + Windows MelonLoader
   inside a Wine-based compatibility layer. On Windows the loader works the easy way, so mods (and
   our own `HelloBTD6.dll`) just work. No SIP, no re-buying the game.
   - **This Mac is Intel + macOS 15.7.7**, so use **CrossOver 26** (free 14-day trial; needs 10.15+).
     CrossOver **27 drops Intel**, so stay on 26.
   - **Whisky is ruled out**: never supported Intel, and discontinued April 2025.
   - **Free alternative:** **Sikarugir** (formerly Kegworks/Wineskin) — more tinkering, no cost.
2. **A spare/cloud Windows PC** — least fun, most reliable.
3. **Contribute upstream** — the IL2CPP-on-macOS work in MelonLoader is active. A clean BTD6 repro +
   log is genuinely useful to that project, and is a great "we found a real bug" lesson for Hugh.

Record what we try in `JOURNAL.md`.

---

## Repo layout

```
btd6-mac-mods/
├── README.md            ← you are here (full install + run guide)
├── JOURNAL.md           ← lab notebook: what we tried, what happened
├── scripts/
│   └── diagnose-mac.sh  ← run on Hugh's Mac to inspect the install + logs
└── src/
    └── HelloBTD6/       ← minimal injection-test mod
        ├── README.md    ← mod-specific build notes
        ├── HelloBTD6.csproj
        └── Main.cs
```

## Sources / further reading

- BTD Mod Helper — https://github.com/gurrenm3/BTD-Mod-Helper
- MelonLoader — https://github.com/LavaGang/MelonLoader
- MelonLoader **Installer** (macOS `.dmg` + xattr bypass) — https://github.com/LavaGang/MelonLoader.Installer
- MelonLoader initial macOS support (PR #900) — https://github.com/LavaGang/MelonLoader/pull/900
- MelonLoader Wiki — https://melonwiki.xyz/
- .NET SDK download — https://dotnet.microsoft.com/download
- BTD6 modding tutorial (Windows-oriented) — https://hemisemidemipresent.github.io/btd6-modding-tutorial/
