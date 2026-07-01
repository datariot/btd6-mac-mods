using System;
using System.Collections;
using System.Reflection;
using MelonLoader;
using HarmonyLib;
using BTD_Mod_Helper.Api;
using BTD_Mod_Helper.Api.Display;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Display;
using Il2CppAssets.Scripts.Models.GenericBehaviors;
using Il2CppNinjaKiwi.Common.ResourceUtils;
using CustomDisplayFallback;

[assembly: MelonInfo(typeof(CustomDisplayFallbackMod), "CustomDisplayFallback", "1.1.0", "David & Hugh")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6-Epic")]

namespace CustomDisplayFallback;

// macOS-port hardening for Mod Helper custom displays (ModDisplay / ModCustomDisplay).
//
// On this arm64 il2cpp port, placing a modded tower with a custom display SIGBUS-crashes.
// Two distinct causes, two patches:
//
//  (1) Factory.CreateAsync — the game loads every display GUID through
//      Addressables.LoadAssetAsync. For a Mod Helper display GUID (e.g.
//      "StarshipEnterprise-StarfleetBase") Addressables has no location and throws
//      InvalidKeyException. On Windows that's benign (Mod Helper substitutes the display in
//      the load-completion callback). On this port, propagating that il2cpp exception across
//      the il2cpp->coreclr boundary faults on a method whose code was stripped (methodPointer
//      in __DATA) -> uncatchable SIGBUS. Fix: intercept CreateAsync, and for any GUID that is
//      a registered ModDisplay, build it directly from ModDisplay.Cache and SKIP the
//      Addressables call entirely (same substitution Mod Helper does, just moved earlier).
//
//  (2) ModCustomDisplay.GetBasePrototype — a ModCustomDisplay (e.g. the Enterprise ship hulls)
//      loads its 3D mesh from an embedded Unity AssetBundle built for StandaloneWindows, which
//      the macOS/Metal player cannot use. Fix: fall back to a stock in-game display instead of
//      the bundle mesh (custom model lost, tower stays placeable).
public class CustomDisplayFallbackMod : MelonMod
{
    public override void OnLateInitializeMelon()
    {
        HarmonyInstance.PatchAll(typeof(CustomDisplayFallbackMod).Assembly);
        LoggerInstance.Msg(
            "CustomDisplayFallback ON — mod displays resolve from Mod Helper's cache before the " +
            "game's Addressables call (avoids the InvalidKeyException SIGBUS), and ModCustomDisplay " +
            "bundle meshes fall back to a stock display. Modded towers stay placeable on macOS.");
    }
}

// (1) Resolve a Mod Helper display GUID directly, skipping the game's Addressables load.
[HarmonyPatch(typeof(Factory), "CreateAsync")]
public static class Factory_CreateAsync_SkipAddressablesForModDisplays
{
    private static bool _init;
    private static IDictionary _cache;      // ModDisplay.Cache : Dictionary<string, ModDisplay> (internal)
    private static MethodInfo _create;      // ModDisplay.Create(Factory, PrefabReference, Action<UnityDisplayNode>) (internal)

    private static void EnsureInit()
    {
        if (_init) return;
        _init = true;
        var t = typeof(ModDisplay);
        _cache = t.GetField("Cache", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) as IDictionary;
        _create = t.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Instance);
        MelonLogger.Msg($"[CustomDisplayFallback] cache={(_cache != null)} create={(_create != null)} " +
                        $"entries={(_cache?.Count ?? -1)}");
    }

    // Param names match the game's Factory.CreateAsync(PrefabReference objectId,
    // DisplayCategory category, Action<UnityDisplayNode> onComplete).
    public static bool Prefix(Factory __instance, PrefabReference objectId,
                              Il2CppSystem.Action<UnityDisplayNode> onComplete)
    {
        try
        {
            EnsureInit();
            if (_cache == null || _create == null) return true;
            var guid = objectId?.guidRef;
            if (guid != null && _cache.Contains(guid))
            {
                var modDisplay = _cache[guid];
                _create.Invoke(modDisplay, new object[] { __instance, objectId, onComplete });
                return false; // handled directly — do not let the game hit Addressables
            }
        }
        catch (Exception e)
        {
            MelonLogger.Warning($"[CustomDisplayFallback] CreateAsync prefix error (falling through): {e.Message}");
        }
        return true;
    }
}

// (2) ModCustomDisplay bundle mesh -> stock display fallback (Windows bundle can't load on Metal).
[HarmonyPatch(typeof(ModCustomDisplay), "GetBasePrototype")]
public static class CustomDisplayStockFallback
{
    // Stock "StarfleetBase" platform display GUID the mod references as a base (valid on macOS).
    private const string StockGuid = "43873ddc40185ac438cb0da6e60e327f";

    public static bool Prefix(Il2CppSystem.Action<UnityDisplayNode> onComplete)
    {
        var factory = Game.instance.GetDisplayFactory();
        var stock = ModContent.CreatePrefabReference(StockGuid);
        factory.FindAndSetupPrototypeAsync(stock, (DisplayCategory)1, onComplete);
        return false; // skip the Windows-bundle mesh load
    }
}
