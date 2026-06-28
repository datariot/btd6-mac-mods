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
- [Part C: Build & run the mods](#part-c-build--run-the-mods)
- [Mods included](#mods-included) — *what each mod does*
- [Part D: Reading the result](#part-d-reading-the-result)
- [Part E: Troubleshooting](#part-e-troubleshooting)
- [Part F: If injection just won't work](#part-f-if-injection-just-wont-work-on-mac)
- [Repo layout & sources](#repo-layout)

---

## Part A: The honest situation (read this first)

> ## ✅ Update (2026-06-26): native modding WORKS on Apple Silicon.
> The earlier conclusion below ("native interop fails → use CrossOver") has been **superseded**. On an
> Apple Silicon Mac (M1–M4), BTD6 mods now run **natively, no CrossOver, no Rosetta**. The fix was
> making MelonLoader's `Il2CppInterop` arm64-aware (the `XrefScannerLowLevel` crash, the icall
> injector, and several hooks). That work lives in a **separate repo, `macos-il2cpp-port`** — it patches
> MelonLoader's runtime. **You need that patched runtime to run these mods**; a stock MelonLoader
> install (Part B4) is *not* enough on arm64.
>
> The mods in this repo are "bare" MelonMods (they call the game's own methods; no class injection), and
> a pile of them work: Mega Cash, Game Speed, Unlimited Upgrades, Monkey Money / Trophies, account
> rank & Monkey Knowledge, unlock-all-towers/heroes/skins. See **[Mods included](#mods-included)**.
>
> ⚠️ **Sharing caveat:** cloning this repo gives you the mod *source*. To actually run them on another
> Apple Silicon Mac you also need the patched MelonLoader runtime from `macos-il2cpp-port` — that part
> isn't packaged here yet. The historical narrative below is kept for context.

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

## Part C: Build & run the mods

> Requires the patched MelonLoader runtime on this Mac (see the Part A update). The csprojs reference
> the game's `MelonLoader/net6` and `MelonLoader/Il2CppAssemblies` DLLs, which exist once MelonLoader is
> installed.

### C1. Inspect the install (also finds the paths the build needs)

```bash
./scripts/diagnose-mac.sh
```
This finds BTD6, confirms it's IL2CPP, prints the **MelonLoader dir** and the **Mods dir**, and tails
the log.

### C2. Build & install every mod (one command)

```bash
./scripts/build-and-install.sh
```
This builds each mod in `src/` and copies the `.dll`s into the game's `Mods/` folder (clearing the
macOS quarantine flag for you). To build a single mod by hand instead:
```bash
dotnet build -c Release src/MegaCash/MegaCash.csproj -o src/MegaCash/bin
cp src/MegaCash/bin/MegaCash.dll "$HOME/Library/Application Support/Steam/steamapps/common/BloonsTD6/Mods/"
```

### C3. Run

Launch BTD6 and **start a game**. With **Mega Cash** installed your money stays maxed — buy anything.

---

## Mods included

All are **bare MelonMods** (no Mod Helper, no class injection): they call the game's own methods.

| Mod | What it does |
|-----|-------------|
| **MegaCash** | Keeps your in-game cash topped up every round |
| **GameSpeed** | Press number keys **1–5** for 1× / 2× / 3× / 5× / 10× speed |
| **UnlimitedUpgrades** | Every monkey can max all 3 upgrade paths (5-5-5), no crosspath limit |
| **UnlockAllUpgrades** | Flips the game's "all upgrades available" flag (no XP grind) |
| **MonkeyMoneyPerRound** | Earn Monkey Money equal to each round you reach |
| **MonkeyMoneyGift** | One-time +999,999,999 Monkey Money |
| **VeteranRanks** | One-time 999 veteran ranks |
| **RanksAndKnowledgePerRound** | +100 Monkey Knowledge and +100 ranks each round (rank caps at the game max) |
| **EveryMonkey** | Unlock all towers + every hero |
| **EveryHeroAndSkins** | Unlock every hero and all their skins |
| **AbilityMonkey** | On-screen panel (toggle with **`**) to fire every ability / no cooldowns |

> Most are persistent / account-level cheats (Monkey Money, trophies, ranks, unlocks are server-synced).
> These are for messing around on your own account — don't use them where a modded balance would matter.

---

## Part D: Reading the result

```bash
./scripts/diagnose-mac.sh   # tails the log and looks for our mods loading
```

- ✅ You see lines like `Mega Cash is ON …` and the mods' messages → they're loading and running.
- ❌ No mod lines, or the log shows IL2CPP / injection errors → the patched runtime isn't in place
  (see the Part A update) or injection failed. Copy the errors into `JOURNAL.md`.

Either way, **paste the run into `JOURNAL.md`** so we have a record.

---

## Part E: Troubleshooting

| Symptom | Fix |
|---------|-----|
| `dotnet: command not found` | B5 not done, or restart the terminal so PATH updates. |
| Build error: can't find `MelonLoader.dll` / `UnityEngine.*` | HintPaths differ by MelonLoader version (`net6/` vs `Managed/`, IL2CPP interop under `Il2CppAssemblies/`). Fix `<GameRoot>` in the mod's `.csproj` or pass `-p:GameRoot=...`. |
| Mod builds but never appears in the log | Confirm the `.dll` is in the **right** `Mods/` folder (diagnose-mac.sh prints it); clear quarantine (C3); make sure you launched the game *after* copying. |
| Log: wrong/missing **.NET runtime** | Install the .NET 6 runtime (see B5 note). |
| Log: IL2CPP errors / SIP / nothing injects | Expected failure mode on macOS IL2CPP. This is the real wall → [Part F](#part-f-if-injection-just-wont-work-on-mac). |
| No `Latest.log` at all | MelonLoader never started — re-run the MelonLoader Installer (B4) and launch the game once. |

---

## Part F: If injection just won't work on Mac

> ⚠️ **Update (2026-06-23): native modding got CLOSE but hit a real wall — use CrossOver.**
> With the `melonloader-launch.sh` wrapper set as BTD6's Steam Launch Option (absolute path +
> `%command%`), MelonLoader **does inject and run natively** — but it then **crashes generating the
> IL2CPP interop assemblies** (`Il2CppInterop` `XrefScannerLowLevel` → `ArgumentOutOfRangeException`).
> That interop layer is what every real mod needs, and Il2CppInterop's maintainers state it "can't work
> well on macOS" (no runtime API for the dylib base address). So: native **injection ✅**, native
> **interop ❌**. CrossOver is the path for actual mods. (See JOURNAL.md for the full debugging arc.)

The reliable workaround:

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
├── START-HERE.md        ← kid-friendly first-time walkthrough
├── JOURNAL.md           ← lab notebook: what we tried, what happened
├── PART-F-CROSSOVER.md  ← Intel-Mac / CrossOver fallback route
├── scripts/
│   ├── diagnose-mac.sh        ← inspect the install + logs
│   └── build-and-install.sh   ← build every mod and copy into Mods/
└── src/                 ← one folder per mod (see "Mods included" above)
    ├── MegaCash/
    ├── GameSpeed/
    ├── UnlimitedUpgrades/
    └── …  (each has its own .csproj + Main.cs)
```

## Sources / further reading

- BTD Mod Helper — https://github.com/gurrenm3/BTD-Mod-Helper
- MelonLoader — https://github.com/LavaGang/MelonLoader
- MelonLoader **Installer** (macOS `.dmg` + xattr bypass) — https://github.com/LavaGang/MelonLoader.Installer
- MelonLoader initial macOS support (PR #900) — https://github.com/LavaGang/MelonLoader/pull/900
- MelonLoader Wiki — https://melonwiki.xyz/
- .NET SDK download — https://dotnet.microsoft.com/download
- BTD6 modding tutorial (Windows-oriented) — https://hemisemidemipresent.github.io/btd6-modding-tutorial/
