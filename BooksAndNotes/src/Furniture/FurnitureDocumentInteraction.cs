using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace BooksAndNotes;

/// <summary>Opens displayed writing in place while leaving furniture storage, pickup, and drawing to Stardew.</summary>
internal static class FurnitureDocumentInteraction
{
    public const string LecternId = "Trapyy.BooksAndNotes_Lectern";

    /// <summary>Reads a displayed document first, or reserves an empty lectern's action for native placement.</summary>
    public static bool Handle(ButtonPressedEventArgs e, IInputHelper input, Action<Item, bool> read)
    {
        if (!e.Button.IsActionButton() || Game1.player.isRidingHorse() || Game1.player.onBridge.Value)
            return false;

        Furniture? surface = FindReadingSurface(e);
        if (surface is null)
            return false;

        if (surface.heldObject.Value is Item displayed && ModEntry.IsWritingItem(displayed))
        {
            input.Suppress(e.Button);
            read(displayed, e.Button.TryGetController(out _));
            return true;
        }

        // Do not open the held book when the player is trying to put it on an empty lectern.
        return surface.QualifiedItemId == "(F)" + LecternId && ModEntry.IsWritingItem(Game1.player.CurrentItem);
    }

    /// <summary>Finds a nearby table or lectern, including the lectern's raised clickable artwork.</summary>
    private static Furniture? FindReadingSurface(ButtonPressedEventArgs e)
    {
        Vector2 tile = DocumentPlacement.TargetTile(e);
        Furniture? surface = Utility.tileWithinRadiusOfPlayer((int)tile.X, (int)tile.Y, 1, Game1.player)
            ? Game1.currentLocation.GetFurnitureAt(tile) : null;
        if (surface is not null && (surface.QualifiedItemId == "(F)" + LecternId
            || ModEntry.IsWritingItem(surface.heldObject.Value)))
            return surface;

        // Check raised lectern art even when a rug or another floor tile was hit first.
        Vector2 position = e.Button == SButton.MouseRight ? e.Cursor.AbsolutePixels : tile * 64 + new Vector2(32);
        foreach (Furniture furniture in Game1.currentLocation.furniture)
        {
            if (furniture.QualifiedItemId != "(F)" + LecternId
                || !Utility.tileWithinRadiusOfPlayer((int)furniture.TileLocation.X, (int)furniture.TileLocation.Y, 1, Game1.player))
                continue;

            Rectangle bounds = furniture.GetBoundingBox();
            // Extend from the floor bounds up to the top of the displayed lectern sprite.
            Rectangle target = new(bounds.X, bounds.Bottom - furniture.sourceRect.Height * 4,
                bounds.Width, furniture.sourceRect.Height * 4);
            if (target.Contains((int)position.X, (int)position.Y))
                return furniture;
        }
        return surface;
    }
}
