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
    }

    // In-match hooks (only fire if we reach gameplay) — confirm the loop can drive that far later.
    public override void OnMatchStart() => P("hook OnMatchStart FIRED (in-match)");
    public override void OnRoundStart() => P("hook OnRoundStart FIRED (in-match)");

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
    public override void ModifyBaseTowerModel(Il2CppAssets.Scripts.Models.Towers.TowerModel towerModel)
    {
        // distinguish it from the base dart monkey so a placed copy is observably different
        towerModel.range *= 2f;
    }
}
