using MelonLoader;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.Main.EventPanel;
using MenuStabilizer;

[assembly: MelonInfo(typeof(MenuStabilizerMod), "MenuStabilizer", "1.0.0", "David & Hugh")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6-Epic")]

namespace MenuStabilizer;

public class MenuStabilizerMod : MelonMod
{
    public override void OnLateInitializeMelon()
    {
        HarmonyInstance.PatchAll(typeof(MenuStabilizerMod).Assembly);
        LoggerInstance.Msg(
            "MenuStabilizer ON — skipping MainMenuEventPanel.Refresh (the event-panel " +
            "online refresh that crashes this port). Event/sweepstakes icons won't show.");
    }
}

// The main-menu event/sweepstakes panel refresh is the consistent prefix to the
// port's SIGILL/SIGBUS crashes. Skip the original method entirely.
[HarmonyPatch(typeof(MainMenuEventPanel), nameof(MainMenuEventPanel.Refresh))]
public static class SkipEventPanelRefresh
{
    // Returning false tells Harmony to skip the original method body.
    public static bool Prefix() => false;
}
