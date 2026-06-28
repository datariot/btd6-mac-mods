using System.Collections.Generic;
using MelonLoader;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Models.Bloons;

[assembly: MelonInfo(typeof(EveryHeroAndSkins.Main), "Every Hero And Skins", "1.0.0", "Hugh & David")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace EveryHeroAndSkins
{
    // Unlocks every hero AND all of their skins, once your profile loads. We both call
    // the game's UnlockHero/UnlockHeroSkin AND write the profile's own ownership sets
    // (unlockedHeroes / unlockedTowerSkins) directly, then SaveNow() so it persists.
    // No Mod Helper, no class injection.
    //
    // heroSet entries are named "HeroDetailsModel_Quincy" etc., but the plain id
    // "Quincy" is what everything else uses, so we strip the prefix.
    public class Main : MelonMod
    {
        private const string HeroPrefix = "HeroDetailsModel_";

        private bool _done;
        private int _attempts;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Every Hero And Skins is ON — every hero and every skin will be unlocked once your profile loads.");
        }

        public override void OnUpdate()
        {
            if (_done)
                return;

            try
            {
                var player = Game.Player;
                var data = player?.Data;
                if (player == null || data == null)
                    return;

                var model = BloonType.GetGameModel();
                if (model == null || model.heroSet == null)
                    return;

                var items = GameData.Instance?.skinsData?.SkinList?.items;

                // Skin data can load a frame or two after the profile. Wait for it,
                // but give up after ~10s and at least do the heroes.
                _attempts++;
                bool skinsReady = items != null && items.Count > 0;
                if (!skinsReady && _attempts <= 600)
                    return;

                // --- Heroes ---
                var heroIds = new HashSet<string>();
                int heroes = 0;
                var heroSet = model.heroSet;
                for (int i = 0; i < heroSet.Count; i++)
                {
                    var hero = heroSet[i];
                    if (hero == null)
                        continue;
                    string id = StripPrefix(hero.name);
                    heroIds.Add(id);
                    try { player.UnlockHero(id); } catch { }
                    try { data.unlockedHeroes.Add(id); heroes++; } catch { }
                }

                // --- Skins (hero, non-default) ---
                int skins = 0;
                string sampleSkin = null;
                if (items != null)
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        var skin = items[i];
                        if (skin == null || skin.isDefaultTowerSkin)
                            continue;
                        if (!heroIds.Contains(skin.baseTowerName))
                            continue;
                        string skinId = skin.skinName;
                        try { player.UnlockHeroSkin(skin.baseTowerName, skinId); } catch { }
                        try { data.unlockedTowerSkins.Add(skinId); skins++; sampleSkin = skinId; } catch { }
                    }
                }

                // Persist so the menu reads it.
                try { player.SaveNow(); } catch { }

                // Verify against the game's own checks.
                bool heroOk = false, skinOk = false;
                try { heroOk = player.HasUnlockedHero("Quincy"); } catch { }
                try { if (sampleSkin != null) skinOk = player.HasUnlockedTowerSkin(sampleSkin); } catch { }

                LoggerInstance.Msg($"Every Hero And Skins: {heroes} heroes, {skins} skins. " +
                    $"unlockedHeroes={data.unlockedHeroes.Count}, unlockedTowerSkins={data.unlockedTowerSkins.Count}. " +
                    $"verify HasUnlockedHero(Quincy)={heroOk}, HasUnlockedTowerSkin({sampleSkin})={skinOk}");
                _done = true;
            }
            catch
            {
                // Profile/model not ready yet — try again next frame.
            }
        }

        private static string StripPrefix(string name)
        {
            if (name != null && name.StartsWith(HeroPrefix))
                return name.Substring(HeroPrefix.Length);
            return name;
        }
    }
}
