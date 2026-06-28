using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Simulation.Towers.Behaviors.Abilities;

[assembly: MelonInfo(typeof(AbilityMonkey.Main), "Ability Monkey", "1.0.0", "Hugh & David")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace AbilityMonkey
{
    // A special panel (its own on-screen UI) that commands EVERY ability in the game.
    //
    //   * "Fire EVERY Ability!"  -> every ability on the board goes off at once.
    //   * "No Cooldowns"         -> while ON, every ability is always ready to use.
    //
    // Press the backtick key  `  (top-left, under Esc) to show or hide the panel.
    //
    // No Mod Helper and no class injection: we walk the game's own towers and call
    // each ability's Activate / ClearCooldown through Il2Cpp interop, exactly like
    // Mega Cash sets your cash. (Making a brand-new monkey type would need class
    // injection, which doesn't work yet on this Mac — so instead this gives you a
    // panel that wields every ability that's already out there.)
    public class Main : MelonMod
    {
        private bool _show;
        private bool _noCooldowns;
        private int _lastAbilityCount;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Ability Monkey is ON — press the ` key (under Esc) to open the panel.");
        }

        public override void OnUpdate()
        {
            // Toggle the panel with the backtick / tilde key.
            if (Input.GetKeyDown(KeyCode.BackQuote))
                _show = !_show;

            // While "No Cooldowns" is on, keep every ability ready every frame.
            if (_noCooldowns)
            {
                foreach (var ability in AllAbilities())
                {
                    try { ability.ClearCooldown(); }
                    catch { /* this ability isn't ready to touch yet */ }
                }
            }
        }

        public override void OnGUI()
        {
            if (!_show)
                return;

            // A simple panel in the top-left. IMGUI is drawn by Unity every frame.
            GUILayout.BeginArea(new Rect(20, 20, 260, 180), GUI.skin.box);
            GUILayout.Label("🐵  Ability Monkey");

            if (GUILayout.Button("🔥  Fire EVERY Ability!"))
                FireEveryAbility();

            _noCooldowns = GUILayout.Toggle(_noCooldowns, "  No Cooldowns (always ready)");

            GUILayout.Label($"Abilities on the board: {_lastAbilityCount}");
            GUILayout.Label("Press  `  to hide this panel.");
            GUILayout.EndArea();
        }

        private void FireEveryAbility()
        {
            int fired = 0;
            foreach (var ability in AllAbilities())
            {
                try
                {
                    ability.ClearCooldown();
                    ability.Activate(true); // true = ignore cooldown
                    fired++;
                }
                catch { /* some abilities can't fire right now (e.g. mid-animation) */ }
            }
            LoggerInstance.Msg($"Fired {fired} abilities!");
        }

        // Every ability behavior on every tower currently in play.
        private IEnumerable<Ability> AllAbilities()
        {
            var found = new List<Ability>();
            try
            {
                var inGame = InGame.instance;
                var bridge = inGame?.bridge;
                var sim = bridge?.simulation;
                var towerManager = sim?.towerManager;
                if (towerManager == null)
                {
                    _lastAbilityCount = 0;
                    return found;
                }

                // Il2Cpp collections don't support C# foreach directly, so we copy
                // into an Il2Cpp List and walk it by index.
                var towers = new Il2CppSystem.Collections.Generic.List<Tower>(towerManager.GetTowers());
                for (int i = 0; i < towers.Count; i++)
                {
                    var tower = towers[i];
                    if (tower == null)
                        continue;
                    CollectAbilities(tower, found);
                }
            }
            catch { /* not in a game yet */ }

            _lastAbilityCount = found.Count;
            return found;
        }

        // A tower's abilities live among its behaviors. We look through the behavior
        // lists and keep the ones that are actually Ability behaviors.
        private static void CollectAbilities(Tower tower, List<Ability> into)
        {
            try
            {
                var behaviors = new Il2CppSystem.Collections.Generic.List<
                    Il2CppAssets.Scripts.Simulation.Objects.ITowerBehavior>(
                        tower.towerBehaviors.Cast<Il2CppSystem.Collections.Generic.IEnumerable<
                            Il2CppAssets.Scripts.Simulation.Objects.ITowerBehavior>>());
                for (int i = 0; i < behaviors.Count; i++)
                {
                    var ability = behaviors[i]?.TryCast<Ability>();
                    if (ability != null)
                        into.Add(ability);
                }
            }
            catch { /* this tower's behaviors aren't readable right now */ }
        }
    }
}
