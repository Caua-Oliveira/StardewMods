using Configuration;
using StardewModdingAPI;
using StardewValley;

namespace Core;

internal static class SwapPolicy
{
    public static bool IsTractor(Farmer player) => player.isRidingHorse()
        && player.mount?.Name?.Contains("tractor", StringComparison.OrdinalIgnoreCase) == true;

    public static bool CanSwap(Farmer player, ModConfig config) => config.Enabled && Context.IsPlayerFree
        && player.currentLocation != null && Game1.activeClickableMenu == null
        && !Game1.eventUp && !Game1.dialogueUp && !Game1.fadeToBlack
        && player.canMove && !player.UsingTool
        && !(config.DisableTractorSwap && IsTractor(player))
        && (player.CurrentItem == null || !config.ItemBlacklist.Contains(player.CurrentItem.QualifiedItemId))
        && !IsMineUtility(player.CurrentItem);

    public static bool IsMineUtility(Item? item) => item?.QualifiedItemId is "(O)286" or "(O)287" or "(O)288" or "(O)71";
}
