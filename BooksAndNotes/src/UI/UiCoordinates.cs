using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace BooksAndNotes;

/// <summary>Converts input positions into the UI coordinate space used by the reader's bounds.</summary>
internal static class UiCoordinates
{
    /// <summary>Applies UI scaling explicitly, including events that run outside Stardew's UI drawing mode.</summary>
    public static Point Cursor(ICursorPosition cursor)
    {
        Vector2 point = Utility.ModifyCoordinatesForUIScale(cursor.ScreenPixels);
        return new Point((int)point.X, (int)point.Y);
    }
}
