using MelonLoader;

// This attribute registers the mod with MelonLoader.
// (name, version, author) — the type it points at is our Mod class below.
[assembly: MelonInfo(typeof(HelloBTD6.HelloMod), "HelloBTD6", "0.1.0", "Hugh & David")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace HelloBTD6
{
    /// <summary>
    /// The smallest possible MelonLoader mod. It does NOT touch the game at all —
    /// its only job is to prove that MelonLoader can inject and run our code on macOS.
    ///
    /// On purpose it depends on NOTHING except MelonLoader itself (no UnityEngine),
    /// so the build can't fail on game-specific reference paths. If you see the
    /// "Hello from Hugh's mod!" lines in the MelonLoader log, injection works and
    /// we can move on to real mods.
    /// </summary>
    public class HelloMod : MelonMod
    {
        private int _frames;

        // Called once, right after MelonLoader loads this mod.
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("=== Hello from Hugh's mod! Injection works. ===");
            LoggerInstance.Msg("If you can read this in the log, macOS modding is GO.");
        }

        // Called every frame. Games run ~60 frames per second, so logging once every
        // ~300 frames gives us a heartbeat roughly every 5 seconds without spamming.
        // (We count frames instead of using UnityEngine's timer, so this mod needs no
        //  game references to build.)
        public override void OnUpdate()
        {
            if (++_frames % 300 == 0)
            {
                LoggerInstance.Msg("[HelloBTD6] still alive — heartbeat");
            }
        }
    }
}
