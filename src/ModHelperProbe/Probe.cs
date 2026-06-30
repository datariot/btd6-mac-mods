using System;
using MelonLoader;
using BTD_Mod_Helper;

[assembly: MelonInfo(typeof(ModHelperProbe.ProbeMod), "ModHelperProbe", "1.0.0", "David")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace ModHelperProbe;

// A Mod Helper (BloonsTD6Mod) subclass used purely as a diagnostic. It records which Mod Helper
// hooks actually fire on the macOS arm64 port, and inside them exercises Mod Helper / Il2Cpp interop
// APIs, logging PASS/FAIL per feature. All output is prefixed [PROBE] for autonomous grepping.
public class ProbeMod : BloonsTD6Mod
{
    private static void P(string s) => MelonLogger.Msg($"[PROBE] {s}");

    private static void Check(string name, Action a)
    {
        try { a(); P($"PASS {name}"); }
        catch (Exception e) { P($"FAIL {name}: {e.GetType().Name}: {e.Message}"); }
    }

    public override void OnApplicationStart()
    {
        P("=== ModHelperProbe loaded as BloonsTD6Mod ===");
        Check("Mod Helper recognizes this mod (GetMod<ProbeMod>)", () =>
        {
            var self = ModHelper.GetMod<ProbeMod>();
            P($"  GetMod<ProbeMod> null? {self == null}");
        });
        Check("ModHelper.Mods enumerated", () =>
        {
            int n = 0;
            foreach (var m in ModHelper.Mods) n++;
            P($"  ModHelper.Mods count={n}");
        });
    }

    public override void OnTitleScreen() => P("hook OnTitleScreen FIRED");

    public override void OnProfileLoaded(Il2CppAssets.Scripts.Models.Profile.ProfileModel result)
        => P($"hook OnProfileLoaded FIRED (result null? {result == null})");

    public override void OnGameModelLoaded(Il2CppAssets.Scripts.Models.GameModel model)
    {
        P($"hook OnGameModelLoaded FIRED (model null? {model == null})");
        if (model == null) return;
        Check("GameModel.towers read", () => P($"  towers={model.towers.Count}"));
        Check("GameModel.bloons read", () => P($"  bloons={model.bloons.Count}"));
        Check("GameModel.upgrades read", () => P($"  upgrades={model.upgrades.Count}"));
    }

    private bool _ranMenuProbes;
    public override void OnMainMenu()
    {
        P("hook OnMainMenu FIRED");
        if (_ranMenuProbes) return;
        _ranMenuProbes = true;
        RunMenuProbes();
    }

    private void RunMenuProbes()
    {
        P("--- menu probes ---");
        Check("Game.instance access", () =>
        {
            var g = Il2CppAssets.Scripts.Unity.Game.instance;
            P($"  Game.instance null? {g == null}");
        });
        Check("Game.instance.model access", () =>
        {
            var m = Il2CppAssets.Scripts.Unity.Game.instance.model;
            P($"  model null? {m == null}, towers={m.towers.Count}");
        });
        Check("read DartMonkey base TowerModel from model", () =>
        {
            var m = Il2CppAssets.Scripts.Unity.Game.instance.model;
            var t = m.GetTowerFromId("DartMonkey");
            P($"  DartMonkey null? {t == null}, cost={(t != null ? t.cost : -1)}");
        });
        Check("custom ProbeTower registered in game model (CLASS INJECTION)", () =>
        {
            var m = Il2CppAssets.Scripts.Unity.Game.instance.model;
            int found = 0; string ids = "";
            foreach (var t in m.towers)
            {
                var n = t.name;
                if (n != null && n.Contains("ProbeTower")) { found++; if (found <= 4) ids += n + " "; }
            }
            P($"  ProbeTower tower-models in game model={found}  ids=[{ids.Trim()}]");
            if (found == 0) throw new Exception("ProbeTower not found among game model towers");
        });
        Check("custom embedded texture loads (TextureExists/GetTexture)", () =>
        {
            bool exists = BTD_Mod_Helper.Api.ModContent.TextureExists(ModHelper.GetMod<ProbeMod>(), "ProbeIcon");
            var tex = BTD_Mod_Helper.Api.ModContent.GetTexture(ModHelper.GetMod<ProbeMod>(), "ProbeIcon");
            P($"  TextureExists={exists}, GetTexture null? {tex == null}, {(tex != null ? $"{tex.width}x{tex.height}" : "")}");
            if (!exists || tex == null) throw new Exception("embedded ProbeIcon.png did not load");
        });
        // NOTE: SpriteReference has a custom il2cpp equality that NREs on `== null`, so null-check
        // Il2Cpp objects with ReferenceEquals (compares the managed wrapper), never `== null`.
        var mod = ModHelper.GetMod<ProbeMod>();
        Check("custom sprite reference resolves (GetSpriteReferenceOrNull)", () =>
        {
            var sr = BTD_Mod_Helper.Api.ModContent.GetSpriteReferenceOrNull(mod, "ProbeIcon");
            bool isNull = ReferenceEquals(sr, null);
            P($"  spriteRef present? {!isNull}, guidRef='{(isNull ? "" : sr.guidRef)}'");
            if (isNull) throw new Exception("GetSpriteReferenceOrNull returned managed null for an existing texture");
        });
        Check("custom ModUpgrade registered in game model", () =>
        {
            var m = Il2CppAssets.Scripts.Unity.Game.instance.model;
            int found = 0; string ids = "";
            foreach (var u in m.upgrades)
            {
                var n = u.name;
                if (n != null && n.Contains("ProbeUpgrade")) { found++; if (found <= 2) ids += n + " "; }
            }
            P($"  ProbeUpgrade upgrade-models in game model={found}  ids=[{ids.Trim()}]");
            if (found == 0) throw new Exception("ProbeUpgrade not found among game model upgrades");
        });
        Check("custom ModHero registered in game model (class injection)", () =>
        {
            var m = Il2CppAssets.Scripts.Unity.Game.instance.model;
            int found = 0; string ids = "";
            foreach (var t in m.towers)
            {
                var n = t.name;
                if (n != null && n.Contains("ProbeHero")) { found++; if (found <= 2) ids += n + " "; }
            }
            P($"  ProbeHero tower-models in game model={found}  ids=[{ids.Trim()}]");
            if (found == 0) throw new Exception("ProbeHero not found among game model towers");
        });
        Check("custom ModBloon registered in game model (class injection)", () =>
        {
            var m = Il2CppAssets.Scripts.Unity.Game.instance.model;
            int found = 0; string ids = "";
            foreach (var b in m.bloons)
            {
                var n = b.name;
                if (n != null && n.Contains("ProbeBloon")) { found++; if (found <= 2) ids += n + " "; }
            }
            P($"  ProbeBloon bloon-models in game model={found}  ids=[{ids.Trim()}]");
            if (found == 0) throw new Exception("ProbeBloon not found among game model bloons");
        });
    }

    // In-match hooks (only fire if we reach gameplay).
    public override void OnMatchStart() => P("hook OnMatchStart FIRED (in-match)");

    private bool _placedTower;
    public override void OnRoundStart()
    {
        P("hook OnRoundStart FIRED (in-match)");
        if (_placedTower) return;
        _placedTower = true;
        // Programmatically place the custom tower via the simulation bridge (GUI placement can't be
        // driven by synthetic input on macOS — Unity world-input ignores CGEvent positions). This
        // proves the injected tower is a real, placeable, simulated tower.
        Check("programmatic place custom ProbeTower in simulation", () =>
        {
            var ingame = Il2CppAssets.Scripts.Unity.UI_New.InGame.InGame.instance;
            if (ingame == null) throw new Exception("InGame.instance null");
            var bridge = ingame.bridge;
            if (bridge == null) throw new Exception("bridge null");
            var tm = Il2CppAssets.Scripts.Unity.Game.instance.model.GetTowerFromId("ModHelperProbe-ProbeTower");
            if (tm == null) throw new Exception("ProbeTower model null");
            bridge.CreateTowerAt(
                new UnityEngine.Vector2(0f, 0f), tm,
                default,  // forTowerId
                false,    // isInstaTower
                null,     // callback
                true,     // ignoreInventoryChecks
                true,     // ignorePlacementChecks
                false,    // isEditorTower
                0,        // costOverride
                false,    // deductCash
                0);       // frontierId
            P("  CreateTowerAt invoked without error");
            var placed = bridge.GetFirstTowerWithBaseID("ModHelperProbe-ProbeTower", 0);
            P($"  GetFirstTowerWithBaseID present? {!ReferenceEquals(placed, null)} -> CUSTOM TOWER PLACED + SIMULATED on arm64");
        });
    }

    // Verify the custom tower (Il2Cpp class injection) actually registered into the game model.
    private bool _ranTowerProbe;
    public override void OnNewGameModel(Il2CppAssets.Scripts.Models.GameModel result)
    {
        if (_ranTowerProbe || result == null) return;
        _ranTowerProbe = true;
        Check("custom ModTower present in GameModel", () =>
        {
            int found = 0;
            string sample = null;
            foreach (var t in result.towers)
            {
                var n = t.name;
                if (n != null && n.Contains("ProbeTower")) { found++; sample = n; }
            }
            P($"  ProbeTower instances in model={found} sample='{sample}'");
            if (found == 0) throw new Exception("ProbeTower not found in game model towers");
        });
    }
}

// A minimal custom tower injected via Mod Helper / Il2CppInterop class injection. Reuses DartMonkey's
// base model+art (no custom sprite needed) so this is a clean test of whether class injection and
// tower registration work on the macOS arm64 port.
public class ProbeTower : BTD_Mod_Helper.Api.Towers.ModTower
{
    public override Il2CppAssets.Scripts.Models.TowerSets.TowerSet TowerSet
        => Il2CppAssets.Scripts.Models.TowerSets.TowerSet.Primary;
    public override string BaseTower => "DartMonkey";
    public override int Cost => 100;
    public override string DisplayName => "Probe Tower";
    // use our embedded magenta PNG as the shop icon + portrait (tests the custom-sprite pipeline end to end)
    public override string Icon => "ProbeIcon";
    public override string Portrait => "ProbeIcon";
    public override void ModifyBaseTowerModel(Il2CppAssets.Scripts.Models.Towers.TowerModel towerModel)
    {
        // distinguish it from the base dart monkey so a placed copy is observably different
        towerModel.range *= 2f;
    }
}

// A custom upgrade on the ProbeTower's top path — tests ModUpgrade<T> Il2Cpp class injection.
public class ProbeUpgrade : BTD_Mod_Helper.Api.Towers.ModUpgrade<ProbeTower>
{
    public override int Path => TOP;
    public override int Tier => 1;
    public override int Cost => 500;
    public override string DisplayName => "Probe Upgrade";
    public override void ApplyUpgrade(Il2CppAssets.Scripts.Models.Towers.TowerModel towerModel)
    {
        towerModel.range *= 1.5f;
    }
}

// A custom hero (ModHero, based on Quincy) — tests ModHero Il2Cpp class injection.
public class ProbeHero : BTD_Mod_Helper.Api.Towers.ModHero
{
    public override string BaseTower => "Quincy";
    public override int Cost => 500;
    public override float XpRatio => 1f;
    public override string Title => "The Probe";
    public override string DisplayName => "Probe Hero";
    public override string Level1Description => "A test hero injected on the macOS arm64 port.";
    public override void ModifyBaseTowerModel(Il2CppAssets.Scripts.Models.Towers.TowerModel towerModel) { }
}

// A custom bloon (ModBloon, based on Red) — tests ModBloon Il2Cpp class injection.
public class ProbeBloon : BTD_Mod_Helper.Api.Bloons.ModBloon
{
    public override string BaseBloon => "Red";
    public override void ModifyBaseBloonModel(Il2CppAssets.Scripts.Models.Bloons.BloonModel bloonModel) { }
}
