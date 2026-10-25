namespace BooksAndNotes;

/// <summary>A displayed line's character range in the original, unmodified text.</summary>
public readonly record struct VisualLine(int Start, int Length)
{
    public int End => Start + Length;
}

/// <summary>Wrapped character ranges and the scale needed to fit dense writing on its page.</summary>
public sealed record PageTextLayout(IReadOnlyList<VisualLine> Lines, float Scale)
{
    /// <summary>Converts the logical layout size to a uniform scale inside the displayed writing area.</summary>
    public float FitScale(float width, float height, float logicalWidth, float logicalHeight)
        => Math.Min(width / logicalWidth, height / logicalHeight) * Scale;
}

/// <summary>Wraps and fits literal writing while retaining its original spaces and character offsets.</summary>
public static class TextLayout
{
    /// <summary>Checks whether new text fits the page's unscaled width and writable line count.</summary>
    public static bool FitsPage(string text, float width, int lineCount, Func<string, float> measure)
    {
        List<VisualLine> lines = Wrap(text, width, measure);
        return lines.Count <= lineCount && lines.All(line => measure(text.Substring(line.Start, line.Length)) <= width);
    }

    /// <summary>Finds a display scale for existing writing without truncating or changing the saved string.</summary>
    public static PageTextLayout FitPage(string text, float width, float height, int lineHeight, Func<string, float> measure)
    {
        List<VisualLine> lines = Wrap(text, width, measure);
        if (Fits(lines, 1))
            return new PageTextLayout(lines, 1);

        float minimumScale = 0;
        float maximumScale = 1;
        const int searchIterations = 16;
        for (int iteration = 0; iteration < searchIterations; iteration++)
        {
            float scale = (minimumScale + maximumScale) / 2;
            List<VisualLine> candidate = Wrap(text, width / scale, measure);
            if (Fits(candidate, scale))
                minimumScale = scale;
            else
                maximumScale = scale;
        }

        return new PageTextLayout(Wrap(text, width / minimumScale, measure), minimumScale);

        // Check both dimensions: a single wide glyph can overflow even when the line count fits.
        bool Fits(IReadOnlyList<VisualLine> candidate, float scale) => candidate.Count * lineHeight * scale <= height
            && candidate.All(line => measure(text.Substring(line.Start, line.Length)) * scale <= width);
    }

    /// <summary>Wraps at spaces when possible, retaining explicit blank lines and complete surrogate pairs.</summary>
    public static List<VisualLine> Wrap(string text, float width, Func<string, float> measure)
    {
        List<VisualLine> lines = [];
        int start = 0;
        while (start <= text.Length)
        {
            int end = text.IndexOf('\n', start);
            if (end < 0)
                end = text.Length;
            if (start == end)
                lines.Add(new VisualLine(start, 0));
            while (start < end)
            {
                int count = 0;
                while (start + count < end && measure(text.Substring(start, count + 1)) <= width)
                    count++;
                count = Math.Max(count, 1);
                if (start + count < end)
                {
                    int space = text.LastIndexOf(' ', start + count - 1, count);
                    if (space >= start)
                        count = space - start + 1;
                }
                // Keep the second half of a paired character with its first half.
                if (start + count < end && char.IsHighSurrogate(text[start + count - 1]) && char.IsLowSurrogate(text[start + count]))
                    count++;
                lines.Add(new VisualLine(start, count));
                start += count;
            }
            if (end == text.Length)
                break;
            start = end + 1;
        }
        return lines;
    }

    /// <summary>Finds the last visual line starting at or before a caret position.</summary>
    public static int LineAt(IReadOnlyList<VisualLine> lines, int position)
    {
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            if (position >= lines[i].Start)
                return i;
        }
        return 0;
    }
}
