using HarmonyLib;
using System.Runtime.CompilerServices;
using SObject = StardewValley.Object;

namespace BooksAndNotes;

/// <summary>Customizes book/note stack sizes and inventory labels while retaining vanilla item storage.</summary>
internal static class ItemPatches
{
    private static readonly ConditionalWeakTable<SObject, DocumentSignatureCache> SignatureCaches = new();

    /// <summary>Installs postfixes so native item logic runs before this mod adjusts its results.</summary>
    public static void Apply(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(SObject), nameof(SObject.maximumStackSize)),
            postfix: new HarmonyMethod(typeof(ItemPatches), nameof(StackSize)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(SObject), nameof(SObject.DisplayName)),
            postfix: new HarmonyMethod(typeof(ItemPatches), nameof(DisplayName)));
        harmony.Patch(AccessTools.Method(typeof(SObject), nameof(SObject.getDescription)),
            postfix: new HarmonyMethod(typeof(ItemPatches), nameof(Description)));
    }

    /// <summary>Makes each writable item non-stackable so its writing remains attached to one item.</summary>
    private static void StackSize(SObject __instance, ref int __result)
    {
        if (ModEntry.IsWritingItem(__instance))
            __result = 1;
    }

    /// <summary>Uses the saved signed title as the inventory name when it is present.</summary>
    private static void DisplayName(SObject __instance, ref string __result)
    {
        if (ModEntry.IsWritingItem(__instance)
            && __instance.modData.TryGetValue(ModEntry.TitleKey, out string title)
            && !string.IsNullOrWhiteSpace(title))
            __result = title;
    }

    /// <summary>Replaces a signed item's description with the translated author credit.</summary>
    private static void Description(SObject __instance, ref string __result)
    {
        if (!ModEntry.IsWritingItem(__instance) || !__instance.modData.TryGetValue(ModEntry.DataKey, out string json))
            return;

        string? author = SignatureCaches.GetValue(__instance, static _ => new DocumentSignatureCache()).GetAuthor(json);
        if (author is not null)
            __result = ModEntry.I18n.Get("item.signed-description", new { username = author }).ToString();
    }
}
