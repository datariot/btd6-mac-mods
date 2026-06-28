# BTD6 native-arm64 modding runtime (Apple Silicon)

This is the **patched MelonLoader runtime** that makes Bloons TD 6 mods run **natively on Apple Silicon
Macs (M1–M4)** — no CrossOver, no Rosetta. It's the missing piece that a stock MelonLoader install
can't do on arm64 (the IL2CPP interop crashes); this build fixes that.

> Pair it with the mods in **https://github.com/datariot/btd6-mac-mods** (Mega Cash, Game Speed,
> Unlimited Upgrades, unlock-everything, etc.).

## What you need first

- An **Apple Silicon Mac** (M1/M2/M3/M4). *(Intel Mac? This won't work — use the CrossOver route in the
  btd6-mac-mods repo's `PART-F-CROSSOVER.md` instead.)*
- **Bloons TD 6 installed via Steam**, and **launched once** normally so the app exists.
- To *build* the mods later: the **.NET SDK** — `brew install --cask dotnet-sdk`.

## Install (one time)

```bash
# from inside this extracted folder:
./install-runtime.sh
```

That copies the runtime into your BTD6 folder, links `GameAssembly.dylib`, installs the .NET 6 arm64
runtime to `~/.dotnet`, sets a couple of window prefs, and drops a `play.sh` launcher.

## Get and build the mods

```bash
git clone https://github.com/datariot/btd6-mac-mods.git
cd btd6-mac-mods
./scripts/build-and-install.sh        # builds every mod into the game's Mods/ folder
```

## Play

```bash
bash "$HOME/Library/Application Support/Steam/steamapps/common/BloonsTD6/play.sh"
```

**The first launch regenerates the IL2CPP interop assemblies — give it a minute.** After that it's
fast. Start a game and your money stays maxed (Mega Cash); press 1–5 for game speed; etc.

## Notes & caveats

- **Launch with `play.sh`, not the Steam button.** Launching through Steam runs the game under Rosetta
  (x86_64), which mismatches the arm64 MelonLoader and crashes instantly. `play.sh` launches native.
- **Game version:** this was built against a specific BTD6 version. If Steam has auto-updated BTD6 to a
  newer version, the interop will regenerate fine, but a mod that calls a changed game method might
  misbehave — rebuild the mods from the latest repo if so.
- **These mods are mostly account-level cheats** (Monkey Money, ranks, unlocks are server-synced). Use
  them on your own account for fun; don't use them where a modded balance would matter.
- Not notarized by Apple — the installer clears the quarantine flag so macOS will load the files.

## How this was built (for the curious)

The fixes live in **https://github.com/datariot/MelonLoader** (arm64 branch) and
**https://github.com/datariot/Il2CppInterop** (arm64 branch): an arm64 instruction decoder for the
xref scanner, per-hook fixes for clang/arm64 codegen, and an arm64 fix for the icall injector. This
folder is just those builds, assembled and ready to drop in.
