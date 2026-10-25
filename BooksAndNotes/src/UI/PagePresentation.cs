using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace BooksAndNotes;

/// <summary>Lays out and draws literal writing and note rules inside Parchment's page rectangles.</summary>
internal static class PagePresentation
{
    public const int TextWidth = 360;
    public const int TextHeight = 420;
    public const int NoteTextHeight = 552;
    public static int LineHeight => Game1.smallFont.LineSpacing + 4;
    private static int BookLayoutLineCount => TextHeight / LineHeight + 1;

    /// <summary>Returns the existing writing-row spacing for books or notes.</summary>
    public static int LineHeightFor(bool isBook) => Game1.smallFont.LineSpacing + (isBook ? 2 : 4);

    /// <summary>Limits new writing while reserving one book row for the final line's glyph height.</summary>
    public static int LineCapacity(bool isBook) => isBook ? Math.Max(1, BookLayoutLineCount - 1) : NoteTextHeight / LineHeight;

    /// <summary>Keeps the full logical writing height, including the reserved book-row headroom.</summary>
    public static int LogicalHeight(bool isBook) => isBook ? BookLayoutLineCount * LineHeightFor(true) : NoteTextHeight;

    /// <summary>Measures unscaled text with the game's small font.</summary>
    public static float Measure(string text) => Game1.smallFont.MeasureString(text).X;

    /// <summary>Excludes the book's lower corners and page numbers from its editable writing area.</summary>
    public static Rectangle WritingBounds(Rectangle page, bool isBook = true)
        => new(page.X, page.Y, page.Width, Math.Max(1, page.Height - (isBook ? (int)(page.Width * 32f / TextWidth) : 0)));

    /// <summary>Wraps writing at the fixed logical width, scaling dense saved text rather than deleting it.</summary>
    public static PageTextLayout Layout(string text, bool isBook = true)
        => TextLayout.FitPage(text, TextWidth, LogicalHeight(isBook), LineHeightFor(isBook), Measure);

    /// <summary>Keeps normal book writing at native size, with a fit fallback for small or crowded pages.</summary>
    public static float Scale(Rectangle bounds, PageTextLayout layout, bool isBook = true)
    {
        if (!isBook)
            return layout.FitScale(bounds.Width, bounds.Height, TextWidth, LogicalHeight(false));

        // Fit actual glyphs rather than shrinking every line to fit the logical page's empty headroom.
        float scale = Math.Min(1f, bounds.Width / (float)TextWidth) * layout.Scale;
        float lastRow = Math.Max(0, layout.Lines.Count - 1) * LineHeightFor(true);
        float glyphHeight = Math.Max(1, Game1.smallFont.MeasureString("Ay").Y);
        float lastY = MathF.Round(bounds.Y + lastRow * scale);
        if (lastY + glyphHeight * scale <= bounds.Bottom)
            return scale;

        // Leave half a pixel for the last row's rounded drawing origin.
        float heightLimit = Math.Max(.5f, bounds.Height - .5f) / (lastRow + glyphHeight);
        return Math.Min(scale, heightLimit);
    }

    /// <summary>Returns displayed row spacing for drawing and caret hit testing.</summary>
    public static float RowHeight(Rectangle bounds, PageTextLayout layout, bool isBook)
    {
        float scale = isBook
            ? Scale(bounds, layout)
            : layout.FitScale(bounds.Width, bounds.Height, TextWidth, LogicalHeight(false));
        return LineHeightFor(isBook) * scale;
    }

    /// <summary>Places a line on whole UI pixels using the same spacing as caret selection.</summary>
    public static Vector2 LineOrigin(Rectangle bounds, PageTextLayout layout, int line, bool isBook)
        => new(bounds.X, MathF.Round(bounds.Y + line * RowHeight(bounds, layout, isBook)));

    /// <summary>Draws wrapped lines with native SpriteFont rendering and no markup interpretation.</summary>
    public static void Draw(SpriteBatch batch, string text, Rectangle bounds, PageTextLayout? layout = null, bool isBook = true)
    {
        layout ??= Layout(text, isBook);
        float scale = Scale(bounds, layout, isBook);
        for (int i = 0; i < layout.Lines.Count; i++)
        {
            VisualLine line = layout.Lines[i];
            MenuStyle.Label(batch, text.Substring(line.Start, line.Length),
                LineOrigin(bounds, layout, i, isBook), scale: scale);
        }
    }

    /// <summary>Draws a note's horizontal ruling and puts the vertical margin on the appropriate side.</summary>
    public static void DrawNoteRules(SpriteBatch batch, Rectangle bounds, bool back)
    {
        float scale = Math.Min(bounds.Width / (float)TextWidth, bounds.Height / (float)NoteTextHeight);
        Texture2D rule = Game1.content.Load<Texture2D>("Mods/Trapyy.BooksAndNotes/rule");
        Texture2D margin = Game1.content.Load<Texture2D>("Mods/Trapyy.BooksAndNotes/margin");
        int thickness = Math.Max(1, (int)scale);
        for (int i = 0; i < NoteTextHeight / LineHeight; i++)
        {
            int y = bounds.Y + (int)((i * LineHeight + Game1.smallFont.LineSpacing) * scale);
            batch.Draw(rule, new Rectangle(bounds.X, y, bounds.Width, thickness), Color.White);
        }
        int gap = Math.Max(4, bounds.Width / 24);
        batch.Draw(margin, new Rectangle(back ? bounds.Right + gap : bounds.X - gap,
            bounds.Y, thickness, bounds.Height), Color.White);
    }
}
