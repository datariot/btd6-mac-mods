using MelonLoader;
using UnityEngine;
using Il2CppAssets.Scripts;
using Il2CppAssets.Scripts.Simulation;

[assembly: MelonInfo(typeof(GameSpeed.Main), "Game Speed", "1.0.0", "Hugh & David")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace GameSpeed
{
    // Press a number key to choose how fast the game runs:
    //   1 = normal (1x)   2 = 2x   3 = 3x   4 = 5x   5 = 10x
    //
    // BTD6 already has a "fast forward" button. Fast forward just runs the game at
    // Constants.fastForwardTimeScaleMultiplier times normal speed (3x by default).
    // So to make ANY speed we want, we set that multiplier ourselves and turn the
    // game's fast forward on. Press 1 to turn it back off (normal speed).
    //
    // No Mod Helper, no class injection — we just set the game's own values through
    // Il2Cpp interop, the same way Mega Cash sets your cash.
    public class Main : MelonMod
    {
        // The speed each number key picks. Index 0 is unused; keys are 1..5.
        private static readonly double[] Speeds = { 1, 1, 2, 3, 5, 10 };

        // Which speed we last set, so we can keep it applied every frame (the game
        // can reset the multiplier between rounds) and only log when it changes.
        private double _speed = 1;

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Game Speed is ON — press 1,2,3,4,5 to set speed (1x, 2x, 3x, 5x, 10x).");
        }

        public override void OnUpdate()
        {
            try
            {
                // 1..5 on the top-row number keys.
                for (int key = 1; key <= 5; key++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha0 + key) || Input.GetKeyDown(KeyCode.Keypad0 + key))
                        SetSpeed(Speeds[key]);
                }

                // Keep our chosen speed applied. When faster than 1x, make sure the
                // game's fast forward is on and running at our multiplier.
                if (_speed > 1)
                {
                    Constants.fastForwardTimeScaleMultiplier = _speed;
                    if (!TimeManager.FastForwardActive)
                        TimeManager.FastForwardActive = true;
                }
            }
            catch
            {
                // Not in a game yet — try again next frame.
            }
        }

        private void SetSpeed(double speed)
        {
            _speed = speed;
            if (speed <= 1)
            {
                TimeManager.FastForwardActive = false;
                LoggerInstance.Msg("Speed: 1x (normal)");
            }
            else
            {
                Constants.fastForwardTimeScaleMultiplier = speed;
                TimeManager.FastForwardActive = true;
                LoggerInstance.Msg($"Speed: {speed}x");
            }
        }
    }
}
