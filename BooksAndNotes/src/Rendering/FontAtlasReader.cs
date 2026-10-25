using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BooksAndNotes.Rendering;

/// <summary>Reads supported font texture formats into pixels without switching render targets.</summary>
internal static class FontAtlasReader
{
    /// <summary>Reads the first mip level of a color, alpha-only, or DXT-compressed font atlas.</summary>
    public static Color[] Read(Texture2D texture)
    {
        Color[] pixels = new Color[checked(texture.Width * texture.Height)];
        if (texture.Format == SurfaceFormat.Color)
        {
            texture.GetData(pixels);
            return pixels;
        }

        if (texture.Format == SurfaceFormat.Alpha8)
        {
            byte[] alpha = new byte[pixels.Length];
            texture.GetData(alpha);
            for (int i = 0; i < alpha.Length; i++)
                pixels[i] = new Color(alpha[i], alpha[i], alpha[i], alpha[i]);

            return pixels;
        }

        if (texture.Format is not (SurfaceFormat.Dxt1 or SurfaceFormat.Dxt3 or SurfaceFormat.Dxt5))
            throw new NotSupportedException($"Animated writing cannot read the game's {texture.Format} font atlas.");

        DecodeCompressedAtlas(texture, pixels);
        return pixels;
    }

    /// <summary>Decodes each 4-by-4 DXT block into the corresponding atlas pixels.</summary>
    private static void DecodeCompressedAtlas(Texture2D texture, Color[] pixels)
    {
        int bytesPerBlock = texture.Format == SurfaceFormat.Dxt1 ? 8 : 16;
        int blockColumns = (texture.Width + 3) / 4;
        int blockRows = (texture.Height + 3) / 4;
        byte[] data = new byte[checked(blockColumns * blockRows * bytesPerBlock)];
        texture.GetData(data);

        Color[] palette = new Color[4];
        byte[] alphaPalette = new byte[8];
        for (int blockY = 0; blockY < blockRows; blockY++)
        {
            for (int blockX = 0; blockX < blockColumns; blockX++)
            {
                int blockOffset = (blockY * blockColumns + blockX) * bytesPerBlock;
                int colorOffset = blockOffset + bytesPerBlock - 8;
                ushort firstColor = BitConverter.ToUInt16(data, colorOffset);
                ushort secondColor = BitConverter.ToUInt16(data, colorOffset + 2);
                palette[0] = DecodeRgb565(firstColor);
                palette[1] = DecodeRgb565(secondColor);

                bool fourColors = texture.Format != SurfaceFormat.Dxt1 || firstColor > secondColor;
                palette[2] = MixColors(palette[0], palette[1], fourColors ? 2 : 1, 1);
                palette[3] = fourColors ? MixColors(palette[0], palette[1], 1, 2) : Color.Transparent;
                uint colorSelectors = BitConverter.ToUInt32(data, colorOffset + 4);
                ulong alphaSelectors = 0;
                if (texture.Format == SurfaceFormat.Dxt5)
                    alphaSelectors = ReadDxt5Alpha(data, blockOffset, alphaPalette);

                for (int pixelIndex = 0; pixelIndex < 16; pixelIndex++)
                {
                    int x = blockX * 4 + pixelIndex % 4;
                    int y = blockY * 4 + pixelIndex / 4;
                    if (x >= texture.Width || y >= texture.Height)
                        continue;

                    Color color = palette[(int)((colorSelectors >> (pixelIndex * 2)) & 3)];
                    if (texture.Format == SurfaceFormat.Dxt3)
                    {
                        int alphaNibble = (data[blockOffset + pixelIndex / 2] >> (4 * (pixelIndex % 2))) & 15;
                        color.A = (byte)(alphaNibble * 17);
                    }
                    else if (texture.Format == SurfaceFormat.Dxt5)
                    {
                        color.A = alphaPalette[(int)((alphaSelectors >> (3 * pixelIndex)) & 7)];
                    }

                    pixels[y * texture.Width + x] = color;
                }
            }
        }
    }

    /// <summary>Builds a DXT5 block's alpha palette and reads its packed three-bit pixel selectors.</summary>
    private static ulong ReadDxt5Alpha(byte[] data, int offset, byte[] palette)
    {
        palette[0] = data[offset];
        palette[1] = data[offset + 1];
        if (palette[0] > palette[1])
        {
            for (int i = 2; i < 8; i++)
                palette[i] = (byte)(((8 - i) * palette[0] + (i - 1) * palette[1]) / 7);
        }
        else
        {
            for (int i = 2; i < 6; i++)
                palette[i] = (byte)(((6 - i) * palette[0] + (i - 1) * palette[1]) / 5);

            palette[6] = 0;
            palette[7] = 255;
        }

        ulong selectors = 0;
        for (int i = 0; i < 6; i++)
            selectors |= (ulong)data[offset + 2 + i] << (8 * i);

        return selectors;
    }

    /// <summary>Expands a packed 16-bit RGB color to the same 8-bit channels used by the original atlas reader.</summary>
    private static Color DecodeRgb565(ushort color)
        => new((byte)(((color >> 11) & 31) * 255 / 31),
            (byte)(((color >> 5) & 63) * 255 / 63),
            (byte)((color & 31) * 255 / 31), (byte)255);

    /// <summary>Interpolates two DXT palette colors using integer weights.</summary>
    private static Color MixColors(Color first, Color second, int firstWeight, int secondWeight)
        => new((byte)((first.R * firstWeight + second.R * secondWeight) / (firstWeight + secondWeight)),
            (byte)((first.G * firstWeight + second.G * secondWeight) / (firstWeight + secondWeight)),
            (byte)((first.B * firstWeight + second.B * secondWeight) / (firstWeight + secondWeight)), (byte)255);
}
