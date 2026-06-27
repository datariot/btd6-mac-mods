using MelonLoader;
using Il2CppAssets.Scripts.Unity;

[assembly: MelonInfo(typeof(UnlockAllUpgrades.Main), "Unlock All Upgrades", "1.0.0", "Hugh & David")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace UnlockAllUpgrades
{
    // Turns on BTD6's own "debug: unlock all upgrades" flag on the player, so every
    // upgrade on every tower is available without grinding tower XP. No Mod Helper and
    // no class injection — we just set Game.Player.debugUnlockAllUpgrades through the
    // game's Il2Cpp interop, the same way Mega Cash sets your cash.
    public class Main : MelonMod
    {
        private bool _done;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Unlock All Upgrades is ON — every tower upgrade will be available!");
        }

        public override void OnUpdate()
        {
            if (_done)
                return;

            try
            {
                var player = Game.Player; // static Btd6Player, available once the profile is loaded
                if (player == null)
                    return;

                if (!player.debugUnlockAllUpgrades)
                    player.debugUnlockAllUpgrades = true;

                LoggerInstance.Msg("All tower upgrades unlocked!");
                _done = true;
            }
            catch
            {
                // Player profile not ready yet — try again next frame.
            }
        }
    }
}
