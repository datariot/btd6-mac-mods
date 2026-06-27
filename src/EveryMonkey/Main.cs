using MelonLoader;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Models.Bloons;

[assembly: MelonInfo(typeof(EveryMonkey.Main), "Every Monkey", "1.0.0", "Hugh & David")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace EveryMonkey
{
    // Unlocks every monkey in the game, once your profile loads:
    //   * all towers — via BTD6's own debugUnlockAllTowers flag (like Unlock All
    //     Upgrades flips debugUnlockAllUpgrades), and
    //   * every hero — we walk the game model's heroSet and call UnlockHero on each.
    // No Mod Helper, no class injection — just the player's own Il2Cpp flags/calls.
    // Runs once.
    public class Main : MelonMod
    {
        private bool _done;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Every Monkey is ON — every tower and hero will be unlocked once your profile loads.");
        }

        public override void OnUpdate()
        {
            if (_done)
                return;

            try
            {
                var player = Game.Player;
                if (player == null)
                    return;

                // All towers: the game's built-in "unlock everything" flag.
                player.debugUnlockAllTowers = true;

                // Every hero: walk the game model's hero list and unlock each by id.
                int heroes = 0;
                var model = BloonType.GetGameModel();
                if (model != null && model.heroSet != null)
                {
                    var heroSet = model.heroSet;
                    for (int i = 0; i < heroSet.Count; i++)
                    {
                        var hero = heroSet[i];
                        if (hero == null)
                            continue;
                        try
                        {
                            player.UnlockHero(hero.name);
                            heroes++;
                        }
                        catch { /* this hero can't be unlocked right now */ }
                    }
                }

                LoggerInstance.Msg($"Every Monkey: all towers unlocked + {heroes} heroes unlocked!");
                _done = true;
            }
            catch
            {
                // Profile/model not ready yet — try again next frame.
            }
        }
    }
}
