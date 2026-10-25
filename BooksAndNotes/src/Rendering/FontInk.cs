using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;

namespace BooksAndNotes.Rendering;

/// <summary>Draws SpriteFont glyphs into CPU-side ink pixels without changing the game's graphics state.</summary>
internal sealed class FontInk
{
    private static readonly ConditionalWeakTable<SpriteFont, FontInk> CachedFonts = new();
    private readonly SpriteFont _font;
    private readonly Dictionary<char, SpriteFont.Glyph> _glyphs;
    private readonly Color[] _atlasPixels;
    private readonly int _atlasWidth;

    /// <summary>Reads the font's glyph metadata and atlas once for subsequent page captures.</summary>
    private FontInk(SpriteFont font)
    {
        _font = font;
        _glyphs = font.GetGlyphs();
        _atlasWidth = font.Texture.Width;
        _atlasPixels = FontAtlasReader.Read(font.Texture);
    }

    /// <summary>Reuses captured font data without keeping an otherwise-unused font alive.</summary>
    public static FontInk Get(SpriteFont font) => CachedFonts.GetValue(font, value => new FontInk(value));

    /// <summary>Draws a line with the native font's bearings, spacing, and fallback character.</summary>
    public void Draw(Color[] canvas, int width, int height, string text, Vector2 position, float scale, Color tint)
    {
        if (scale <= 0)
            return;

        // Match DrawString's integer origin before applying per-glyph positioning.
        position = new Vector2((int)position.X, (int)position.Y);
        float penPosition = 0;
        bool firstCharacter = true;
        foreach (char character in text)
        {
            if (!_glyphs.TryGetValue(character, out SpriteFont.Glyph glyph))
            {
                if (_font.DefaultCharacter is not char fallback || !_glyphs.TryGetValue(fallback, out glyph))
                    throw new ArgumentException("The game's font has no fallback for a character in this document.");
            }

            penPosition = firstCharacter
                ? Math.Max(glyph.LeftSideBearing, 0)
                : penPosition + _font.Spacing + glyph.LeftSideBearing;
            firstCharacter = false;

            Vector2 glyphPosition = position + new Vector2(
                (penPosition + glyph.Cropping.X) * scale, glyph.Cropping.Y * scale);
            StampGlyph(canvas, width, height, glyph.BoundsInTexture, glyphPosition, scale, tint);
            penPosition += glyph.Width + glyph.RightSideBearing;
        }
    }

    /// <summary>Samples one scaled glyph into the page canvas using premultiplied-alpha blending.</summary>
    private void StampGlyph(Color[] canvas, int width, int height, Rectangle source, Vector2 position, float scale, Color tint)
    {
        int left = Math.Max(0, (int)MathF.Ceiling(position.X - .5f));
        int top = Math.Max(0, (int)MathF.Ceiling(position.Y - .5f));
        int right = Math.Min(width, (int)MathF.Ceiling(position.X + source.Width * scale - .5f));
        int bottom = Math.Min(height, (int)MathF.Ceiling(position.Y + source.Height * scale - .5f));
        for (int y = top; y < bottom; y++)
        {
            int sourceY = source.Y + Math.Clamp((int)((y + .5f - position.Y) / scale), 0, source.Height - 1);
            for (int x = left; x < right; x++)
            {
                int sourceX = source.X + Math.Clamp((int)((x + .5f - position.X) / scale), 0, source.Width - 1);
                Color pixel = _atlasPixels[sourceY * _atlasWidth + sourceX];
                int alpha = pixel.A * tint.A / 255;
                if (alpha == 0)
                    continue;

                int inverseAlpha = 255 - alpha;
                Color existingPixel = canvas[y * width + x];
                canvas[y * width + x] = new Color(
                    (byte)Math.Min(255, pixel.R * tint.R * tint.A / 65025 + existingPixel.R * inverseAlpha / 255),
                    (byte)Math.Min(255, pixel.G * tint.G * tint.A / 65025 + existingPixel.G * inverseAlpha / 255),
                    (byte)Math.Min(255, pixel.B * tint.B * tint.A / 65025 + existingPixel.B * inverseAlpha / 255),
                    (byte)Math.Min(255, alpha + existingPixel.A * inverseAlpha / 255));
            }
        }
    }
}
