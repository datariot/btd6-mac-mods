using MelonLoader;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Unity;

[assembly: MelonInfo(typeof(VeteranRanks.Main), "Veteran Ranks", "1.0.0", "Hugh & David")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace VeteranRanks
{
    // Gives you 999 veteran ranks — the prestige track that starts once you hit the
    // game's max normal rank. Veteran rank has its own XP pool (ProfileModel.veteranXp),
    // separate from regular player XP, so we set it directly: veteranXp is set to the
    // XP for 999 veteran ranks and veteranRank is set to 999, so the two agree and the
    // game won't recompute them back down. No Mod Helper, no class injection — just the
    // profile's own Il2Cpp values, like Mega Cash sets your cash. Runs once.
    public class Main : MelonMod
    {
        private const int VeteranRanksToGive = 999;

        private bool _done;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Veteran Ranks is ON — you'll be granted 999 veteran ranks once your profile loads.");
        }

        public override void OnUpdate()
        {
            if (_done)
                return;

            try
            {
                var data = Game.Player?.Data;
                if (data == null || data.veteranRank == null || data.veteranXp == null)
                    return; // profile not ready yet

                var rankInfo = GameData.Instance?.rankInfo;
                if (rankInfo == null)
                    return;

                long xpPerVeteranRank = rankInfo.xpNeededPerVeteranRank;

                // Set the XP pool and the rank together so they stay consistent.
                data.veteranXp.Value = (double)VeteranRanksToGive * xpPerVeteranRank;
                data.veteranRank.Value = VeteranRanksToGive;

                LoggerInstance.Msg($"Granted {VeteranRanksToGive} veteran ranks (veteranXp set to {data.veteranXp.Value})!");
                _done = true;
            }
            catch
            {
                // Profile not ready / not loaded yet — try again next frame.
            }
        }
    }
}
