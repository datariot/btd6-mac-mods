using MelonLoader;
using HarmonyLib;
using Il2CppAssets.Scripts.Unity.UI_New.Main.EventPanel;
using Il2CppNinjaKiwi.LiNK.Client.LiNKAccountControllers;
using MenuStabilizer;

[assembly: MelonInfo(typeof(MenuStabilizerMod), "MenuStabilizer", "1.1.0", "David & Hugh")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6-Epic")]

namespace MenuStabilizer;

public class MenuStabilizerMod : MelonMod
{
    public override void OnLateInitializeMelon()
    {
        HarmonyInstance.PatchAll(typeof(MenuStabilizerMod).Assembly);
        LoggerInstance.Msg(
            "MenuStabilizer ON — skipping MainMenuEventPanel.Refresh and the LiNK mobile-webview " +
            "login URL builder (the two confirmed crash paths on this macOS port).");
    }
}

// Crash path #1: the main-menu event/sweepstakes panel online refresh.
[HarmonyPatch(typeof(MainMenuEventPanel), nameof(MainMenuEventPanel.Refresh))]
public static class SkipEventPanelRefresh
{
    public static bool Prefix() => false; // skip original body
}

// Crash path #2 (the dominant in-game/menu SIGILL): macOS crash reports fault deterministically
// at MobileWebviewLiNKAccountController.GetUrlV2 (GameAssembly+0x152F0A8) — the game running its
// MOBILE webview login URL builder on desktop. Skip its body: return an empty URL so the crashing
// native code never executes. The method is `private string GetUrlV2()`, so patch it by name.
// Harmless while playing modded/offline.
[HarmonyPatch(typeof(MobileWebviewLiNKAccountController), "GetUrlV2")]
public static class SkipLiNKMobileWebviewUrlV2
{
    public static bool Prefix(ref string __result)
    {
        __result = "";
        return false; // skip original body
    }
}
