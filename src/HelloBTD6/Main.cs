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
    /// If you see the "Hello from Hugh's mod!" lines in the MelonLoader log,
    /// injection works and we can move on to real mods.
    /// </summary>
    public class HelloMod : MelonMod
    {
        private float _timer;

        // Called once, right after MelonLoader loads this mod.
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("=== Hello from Hugh's mod! Injection works. ===");
            LoggerInstance.Msg("If you can read this in the log, macOS modding is GO.");
        }

        // Called every frame. We throttle to once every ~5 seconds so the log
        // shows a steady heartbeat without spamming.
        public override void OnUpdate()
        {
            _timer += UnityEngine.Time.deltaTime;
            if (_timer >= 5f)
            {
                _timer = 0f;
                LoggerInstance.Msg("[HelloBTD6] still alive — heartbeat");
            }
        }
    }
}
