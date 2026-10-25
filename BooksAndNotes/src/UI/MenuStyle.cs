using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace BooksAndNotes;

/// <summary>Shared writing colors and simple drawing helpers for the reader and editor.</summary>
internal static class MenuStyle
{
    public static readonly Color Ink = new(69, 43, 37);
    public static readonly Color Selection = new(176, 206, 183);

    /// <summary>Draws a solid rectangle for a caret or selection highlight.</summary>
    public static void Fill(SpriteBatch batch, Rectangle rectangle, Color color) => batch.Draw(Game1.staminaRect, rectangle, color);

    /// <summary>Draws literal text with the writing font and its normal ink color.</summary>
    public static void Label(SpriteBatch batch, string text, Vector2 position, Color? color = null, float scale = 1f)
        => batch.DrawString(Game1.smallFont, text, position, color ?? Ink, 0, Vector2.Zero, scale, SpriteEffects.None, .9f);
}
