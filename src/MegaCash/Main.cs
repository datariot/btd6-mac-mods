using MelonLoader;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;

[assembly: MelonInfo(typeof(MegaCash.Main), "Mega Cash", "1.0.0", "Hugh & David")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace MegaCash
{
    // Keeps your money topped up while you're in a round. Whenever your available
    // cash drops below 100,000 we bump it back up to 1,000,000 — so you can buy
    // basically anything. No Mod Helper needed: we just call the game's own
    // UnityToSimulation.SetCash through MelonLoader's Il2Cpp interop.
    public class Main : MelonMod
    {
        private const double TopUpWhenBelow = 100_000.0;
        private const double TopUpTo = 1_000_000.0;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Mega Cash is ON — your money will stay topped up in every round!");
        }

        public override void OnUpdate()
        {
            try
            {
                // InGame.instance is null unless we're actually in a round.
                var inGame = InGame.instance;
                if (inGame == null)
                    return;

                var bridge = inGame.bridge; // UnityToSimulation — the economy bridge
                if (bridge == null)
                    return;

                if (bridge.GetAvailableCash() < TopUpWhenBelow)
                    bridge.SetCash(TopUpTo);
            }
            catch
            {
                // Not in a round yet (or the bridge isn't ready) — ignore and try next frame.
            }
        }
    }
}
