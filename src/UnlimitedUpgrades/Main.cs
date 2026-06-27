using MelonLoader;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;

[assembly: MelonInfo(typeof(UnlimitedUpgrades.Main), "Unlimited Upgrades", "1.0.0", "Hugh & David")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace UnlimitedUpgrades
{
    // Removes the crosspathing limit so every monkey can max all three upgrade paths
    // (5-5-5) instead of the usual one-path-to-5 + one-to-2. BTD6 stores how many tiers
    // of each path are blocked on the tower itself (path1/2/3NumBlockedTiers); each
    // frame we walk the towers in play and set those to 0, so the shop lets you buy
    // every tier on every path. No Mod Helper, no class injection — just the towers'
    // own Il2Cpp fields. Pair with Unlock All Upgrades + Mega Cash to buy it all.
    public class Main : MelonMod
    {
        private bool _announced;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Unlimited Upgrades is ON — every monkey can max all 3 paths (5-5-5).");
        }

        public override void OnUpdate()
        {
            try
            {
                var towerManager = InGame.instance?.bridge?.simulation?.towerManager;
                if (towerManager == null)
                    return;

                // Il2Cpp collections don't support C# foreach directly, so copy into an
                // Il2Cpp List and walk it by index.
                var towers = new Il2CppSystem.Collections.Generic.List<Tower>(towerManager.GetTowers());
                for (int i = 0; i < towers.Count; i++)
                {
                    var tower = towers[i];
                    if (tower == null)
                        continue;
                    tower.path1NumBlockedTiers = 0;
                    tower.path2NumBlockedTiers = 0;
                    tower.path3NumBlockedTiers = 0;
                }

                if (!_announced && towers.Count > 0)
                {
                    LoggerInstance.Msg("Unlimited Upgrades active — crosspath limits removed on placed towers.");
                    _announced = true;
                }
            }
            catch
            {
                // Not in a game yet — try again next frame.
            }
        }
    }
}
