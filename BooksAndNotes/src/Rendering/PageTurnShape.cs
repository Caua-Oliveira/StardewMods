namespace BooksAndNotes.Rendering;

/// <summary>Calibrated top/bottom paper outlines in the standard 219-by-158 book sprite's coordinate space.</summary>
internal sealed class PageTurnShape(params PageTurnShape.Column[] columns)
{
    /// <summary>One measured vertical cross-section of a moving paper outline.</summary>
    internal readonly record struct Column(float X, float Top, float Bottom);
    public float Left => columns[0].X;
    public float Right => columns[^1].X;

    /// <summary>Interpolates the paper's upper and lower edges at a sprite-space horizontal position.</summary>
    public bool Sample(float x, out float top, out float bottom)
    {
        top = bottom = 0;
        if (x < Left || x > Right)
            return false;

        for (int i = 1; i < columns.Length; i++)
        {
            if (x > columns[i].X)
                continue;

            Column previous = columns[i - 1];
            Column next = columns[i];
            float fraction = (x - previous.X) / (next.X - previous.X);
            top = previous.Top + (next.Top - previous.Top) * fraction;
            bottom = previous.Bottom + (next.Bottom - previous.Bottom) * fraction;
            return true;
        }

        return false;
    }

    // Keep these measured coordinates unchanged. Backward turns reuse the profiles in reverse order.
    public static readonly PageTurnShape[] Faces =
    [
        new(new(110, 19, 139), new(120, 17, 132), new(140, 12, 128), new(155, 11, 127), new(180, 14, 130), new(201, 16, 136)),
        new(new(110, 20, 139), new(120, 14, 130), new(130, 11, 127), new(140, 11, 127), new(160, 14, 130), new(180, 17, 134), new(198, 18, 136)),
        new(new(106, 13, 135), new(112, 11, 130), new(125, 11, 128), new(140, 12, 129), new(160, 17, 133), new(180, 20, 135), new(190, 21, 136)),
        new(new(68, 13, 125), new(80, 12, 123), new(90, 12, 123), new(100, 14, 128), new(110, 20, 139)),
        new(new(30, 10, 119), new(40, 11, 125), new(50, 11, 129), new(65, 13, 128), new(80, 15, 129), new(100, 19, 134), new(110, 20, 139)),
        new(new(10, 19, 136), new(30, 14, 131), new(50, 11, 127), new(65, 11, 127), new(80, 12, 128), new(100, 18, 133), new(110, 20, 139))
    ];

    // The curled rim in frame 3 covers underlying ink even though it has no readable face of its own.
    public static readonly PageTurnShape Fold = new(new(54, 14, 122), new(60, 10, 128), new(66, 12, 133), new(68, 13, 125));

    // Closing profiles use opening-frame indices; index 0 has no visible inside paper.
    public static readonly PageTurnShape?[] ClosingLeft =
    [
        null,
        new(new(81, 9, 108), new(105, 33, 131), new(109, 35, 133)),
        new(new(34, 3, 99), new(105, 35, 136), new(110, 36, 139)),
        new(new(8, 17, 139), new(110, 17, 139))
    ];

    public static readonly PageTurnShape?[] ClosingRight =
    [
        null,
        new(new(111, 35, 133), new(115, 30, 130), new(137, 9, 108)),
        new(new(110, 36, 139), new(115, 35, 136), new(184, 3, 99)),
        new(new(110, 17, 139), new(212, 17, 139))
    ];
}
