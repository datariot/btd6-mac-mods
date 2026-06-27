using MelonLoader;
using Il2CppAssets.Scripts.Unity;

[assembly: MelonInfo(typeof(MonkeyMoneyGift.Main), "Monkey Money Gift", "1.0.0", "Hugh & David")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace MonkeyMoneyGift
{
    // Gives you 999,999,999 monkey money, once, when your profile loads. We call the
    // game's own Btd6Player.GainMonkeyMoney through Il2Cpp interop — the same call
    // Monkey Money Per Round uses, just one big lump. No Mod Helper, no class
    // injection. Monkey money is persistent / server-synced.
    public class Main : MelonMod
    {
        private const int MonkeyMoneyToGive = 999999999;

        private bool _done;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Monkey Money Gift is ON — you'll get 999,999,999 monkey money once your profile loads.");
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

                player.GainMonkeyMoney(MonkeyMoneyToGive, "MonkeyMoneyGift");
                LoggerInstance.Msg($"Granted {MonkeyMoneyToGive:N0} monkey money!");
                _done = true;
            }
            catch
            {
                // Profile not ready yet — try again next frame.
            }
        }
    }
}
