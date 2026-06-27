using System;
using MelonLoader;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Player;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;

[assembly: MelonInfo(typeof(RanksAndKnowledgePerRound.Main), "Ranks And Knowledge Per Round", "1.0.0", "Hugh & David")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace RanksAndKnowledgePerRound
{
    // Each time you reach a new round you gain ~100 account ranks and 100 Monkey
    // Knowledge points. We watch the in-game round counter and, when it goes up:
    //
    //   * Ranks  — we grant real player XP (Btd6Player.GainPlayerXP) equal to the
    //     XP the game says is needed to climb 100 ranks, so the rank-up is the
    //     game's own legitimate one and it PERSISTS across restarts. (SetRank only
    //     nudged the display and reverted, because rank is derived from XP.) The
    //     game still stops you at its real max rank.
    //   * Knowledge — we add straight to ProfileModel.KnowledgePoints (no cap).
    //
    // Both are persistent / server-synced.
    public class Main : MelonMod
    {
        private const int RanksPerNewRound = 100;
        private const int KnowledgePerNewRound = 100;

        // -1 means "not in a round yet". We baseline on the first frame in a game so
        // we only reward rounds you actually play through this session.
        private int _lastRound = -1;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Ranks And Knowledge Per Round is ON — gain 100 ranks + 100 Monkey Knowledge every round!");
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
                    int displayRound = _lastRound + 1; // what Hugh sees on screen

                    var player = Game.Player;
                    var data = player?.Data;
                    if (player == null || data == null || data.rank == null)
                        return; // profile not ready; try again next frame

                    // Monkey Knowledge: plain int on the profile, no cap.
                    data.KnowledgePoints = data.KnowledgePoints + KnowledgePerNewRound;

                    // Ranks: grant the real XP needed to climb ~100 ranks, so the
                    // rank-up is legitimate and persists. Capped at the game's max rank.
                    int rankBefore = data.rank.ValueInt;
                    GrantRanks(player, rankBefore, RanksPerNewRound);

                    LoggerInstance.Msg($"Reached round {displayRound} — +{RanksPerNewRound} ranks (now {player.Data.rank.ValueInt}), +{KnowledgePerNewRound} knowledge (now {data.KnowledgePoints})!");
                }
            }
            catch
            {
                // Not in a round yet / player not ready — try again next frame.
            }
        }

        // Grant enough real player XP to climb `ranks` ranks from `currentRank`.
        // The game's RankInfo tells us the cumulative XP needed for each rank, so
        // the XP to add is the difference between the target rank and now. Because
        // it's real XP, the rank-up is the game's own and it saves.
        private static void GrantRanks(Btd6Player player, int currentRank, int ranks)
        {
            var gameData = GameData.Instance;
            var rankInfo = gameData?.rankInfo;
            if (rankInfo == null)
                return;

            int maxRank = rankInfo.GetMaxRank();
            int target = Math.Min(currentRank + ranks, maxRank);
            if (target <= currentRank)
                return; // already at the game's max rank

            long xpNow = rankInfo.GetRankInfo(currentRank).totalXpNeeded;
            long xpTarget = rankInfo.GetRankInfo(target).totalXpNeeded;
            long delta = xpTarget - xpNow;
            if (delta > 0)
                player.GainPlayerXP((float)delta);
        }
    }
}
