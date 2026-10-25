using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace BooksAndNotes;

/// <summary>Checks when native furniture placement should take priority over opening a carried document.</summary>
internal static class DocumentPlacement
{
    /// <summary>Probes a nearby surface without moving the item, leaving the actual transfer to Stardew.</summary>
    public static bool ShouldDefer(Item? item, ButtonPressedEventArgs e)
    {
        if (!e.Button.IsActionButton() || !WritingItemIds.IsWritingItem(item?.QualifiedItemId)
            || Game1.player.isRidingHorse() || Game1.player.onBridge.Value)
            return false;

        Vector2 tile = TargetTile(e);
        return Utility.tileWithinRadiusOfPlayer((int)tile.X, (int)tile.Y, 1, Game1.player)
            && Game1.currentLocation.GetFurnitureAt(tile)?.performObjectDropInAction(item!, probe: true, Game1.player) == true;
    }

    /// <summary>Uses the pointed world tile for right-click and the native grab tile for controller actions.</summary>
    internal static Vector2 TargetTile(ButtonPressedEventArgs e) => e.Button == SButton.MouseRight
        ? new Vector2((int)Math.Floor(e.Cursor.AbsolutePixels.X / 64), (int)Math.Floor(e.Cursor.AbsolutePixels.Y / 64))
        : e.Cursor.GrabTile;
}
