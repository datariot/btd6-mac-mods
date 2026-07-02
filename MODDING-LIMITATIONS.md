# Modding limitations on the macOS arm64 BTD6 port

What you can and can't do when using public mods or writing your own on the
Apple-Silicon il2cpp port of Bloons TD 6. Read this before pulling in a community
mod or designing a new one — it will save you a crash investigation.

> **TL;DR:** You can build or use almost any mod that changes how the game
> **plays** (logic, values, rules, new towers with stock visuals, 2D art). You
> **cannot** use mods that add new **3D art** (custom meshes/shaders/prefabs in a
> Unity AssetBundle). That's a Ninja Kiwi build-and-packaging limitation we can't
> patch around from the mod side.

---

## Why — the two root causes

The macOS port is a faithful runtime for the code paths the **macOS game itself
uses**. Two things fall outside that boundary, and both surfaced while diagnosing
the **StarshipEnterprise** mod (which crashes on tower placement):

### 1. Unity AssetBundles are platform-locked

A mod's custom 3D content (tower models, projectile meshes, particle prefabs)
ships inside a Unity **AssetBundle**. Bundles are compiled for one specific
platform — StarshipEnterprise's is built for **StandaloneWindows**. Unity refuses
to load a bundle whose build target ≠ the running player, and BTD6-mac runs the
**Metal/OSX** player. Worse, the **shaders** inside are DirectX/Vulkan bytecode
that Metal cannot use, and there is no runtime conversion. A Windows bundle simply
cannot load on macOS.

### 2. Ninja Kiwi stripped code the macOS build never calls

This is the deeper problem. il2cpp (the ahead-of-time compiler) only emits machine
code for methods that are **reachable** in the build. The custom-display asset
pipeline — the Addressables → mesh → display-node creation path that mods use to
show custom 3D content — isn't exercised by the stock macOS game, so **its code was
stripped**. The methods still exist in *metadata* (so reflection/interop thinks
they're callable), but their code pointer aims into a data segment, not
instructions. Any call into one is an **uncatchable SIGBUS** (bus error on
instruction fetch).

This was verified to be whack-a-mole: a shim that fixed the first stripped-method
crash immediately exposed a second one in the same subsystem.

**Consequence:** rebuilding the bundle for macOS probably still wouldn't save a
custom-model mod — the crash is in the display-**creation code**, not just the
asset format.

### (Related) il2cpp exceptions can crash where they wouldn't on Windows

A normal il2cpp exception that's benign on Windows (e.g. Addressables throwing
`InvalidKeyException` for a not-yet-substituted mod asset key) can SIGBUS on this
port when it propagates across the il2cpp→coreclr boundary through a stripped
method. Don't rely on exception paths being caught.

---

## What works ✅

The entire "logic and stock-content" category is reliable, because it only touches
code the base game actually runs (so il2cpp compiled it):

- **Gameplay-logic mods** — Harmony-patching game methods, changing values,
  behaviors, rules, economy. Every mod in Hugh's set is this:
  MegaCash, GameSpeed, UnlimitedUpgrades, UnlockAllUpgrades, MonkeyMoney*,
  VeteranRanks, AbilityMonkey, EveryMonkey, EveryHeroAndSkins,
  RanksAndKnowledgePerRound, MenuStabilizer, **TimeMachine**, and
  **PathsPlusPlus** (it rewrites the whole upgrade/crosspath system — works
  perfectly, including all upgrade tiers and crosspathing).
- **New towers / heroes that reuse stock visuals** — reference an existing in-game
  display by its GUID instead of shipping a model.
- **2D content** — custom tower icons, portraits, sprites, UI. These load from
  **embedded PNG resources**, not platform-locked bundles.
- **Restyling stock effects** — recoloring/retexturing an existing display (the way
  PathsPlusPlus's weapon displays tint stock beams/particles) works, because the
  base display comes from the game, not a bundle.
- **Sounds / music** — embedded audio works.

## What doesn't work ❌

Anything whose selling point is bespoke 3D visuals:

- **Custom 3D models (meshes)** for towers, bloons, projectiles.
- **Custom shaders / materials** — platform-specific bytecode, no Metal transcode.
- **Custom particle effects / prefabs** shipped in an AssetBundle.
- Mods built on Mod Helper's `ModCustomDisplay` (bundle-backed) — e.g.
  **StarshipEnterprise**. These will SIGBUS on placement.

---

## Rules for writing our own mods

1. **Logic + stock displays + 2D art = safe.** That's a huge design space (new
   towers, abilities, economies, modes, UI) — all reliable.
2. **Never ship a custom mesh/shader AssetBundle.** If a tower needs a look, reuse
   a stock display GUID or restyle it with a 2D sprite/tint.
3. **Build AnyCPU (PE32), never x64.** The port's .NET loader rejects PE32+ x64
   assemblies with `FileLoadException`. Do **not** set
   `<PlatformTarget>x64</PlatformTarget>`. Verify: `file mod.dll` must say
   `PE32 ... Intel 80386`.
4. **Assume nothing about exceptions.** Handle or avoid error paths rather than
   relying on them being caught (see "il2cpp exceptions" above).
5. Beware the pre-existing **intermittent native crash** on this port (partly
   mitigated by MenuStabilizer) — unrelated to your mod, but it exists.

## Vetting a public mod before adding it

Quick tells, cheapest first:

- **Does it embed a 3D bundle?** `grep -c UnityFS <mod>.dll` — if `> 0` **and** the
  mod is about custom models/skins, expect it to fail on placement/use. Pure-logic
  mods embed no bundle and are almost always fine.
- **What does it decompile to?** `ilspycmd <mod>.dll` and look for
  `ModCustomDisplay`, `AssetBundleName`, `CreatePrefabReference<...Display>` →
  custom 3D content → risky. `[HarmonyPatch(...)]` on gameplay methods only → safe.
- **Is it the AnyCPU build?** `file <mod>.dll` → `Intel 80386` (works) vs
  `x86-64` (won't load; the loader auto-converts pure-IL x64, but native-mixed x64
  won't).

## Reference

Full diagnostic trail for the StarshipEnterprise / PathsPlusPlus investigation is
in the maintainer's notes (`btd6-community-mod-placement-crash`). Key sources:
`port-patches/` (the MelonLoader patches that make the port work), `MOD-HELPER-ARM64.md`
(Mod Helper support status), and `src/` (working example mods to copy).
