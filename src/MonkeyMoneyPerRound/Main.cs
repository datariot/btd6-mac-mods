using MelonLoader;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;

[assembly: MelonInfo(typeof(MonkeyMoneyPerRound.Main), "Monkey Money Per Round", "1.0.0", "Hugh & David")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace MonkeyMoneyPerRound
{
    // Each time you reach a new round, you earn Monkey Money equal to that round number.
    // We watch the in-game round counter (UnityToSimulation.GetCurrentRound) and, when it
    // goes up, call Btd6Player.GainMonkeyMoney. No Mod Helper, no class injection — just
    // the game's own Il2Cpp interop, like Mega Cash.
    public class Main : MelonMod
    {
        // -1 means "not in a round yet". We baseline on the first frame in a game so we
        // only reward rounds you actually play through this session (no retroactive dump).
        private int _lastRound = -1;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Monkey Money Per Round is ON — earn Monkey Money for every round you reach!");
        }

        public override void OnUpdate()
        {
            try
            {
                var inGame = InGame.instance;
                if (inGame == null)
                {
                    _lastRound = -1; // left the game; re-baseline next time
                    return;
                }

                var bridge = inGame.bridge;
                if (bridge == null)
                    return;

                int round = bridge.GetCurrentRound(); // 0-indexed (round 1 == 0)

                if (_lastRound < 0)
                {
                    _lastRound = round; // first frame in this game: set baseline, don't reward
                    return;
                }

                while (round > _lastRound)
                {
                    _lastRound++;
                    int displayRound = _lastRound + 1;      // what Hugh sees on screen
                    Game.Player.GainMonkeyMoney(displayRound, "MonkeyMoneyPerRound");
                    LoggerInstance.Msg($"Reached round {displayRound} — +{displayRound} Monkey Money!");
                }
            }
            catch
            {
                // Not in a round yet / player not ready — try again next frame.
            }
        }
    }
}
