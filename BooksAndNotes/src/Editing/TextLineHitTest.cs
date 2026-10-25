using System.Globalization;

namespace BooksAndNotes;

/// <summary>Caches each visible character's midpoint so pointer movement need not remeasure the whole line.</summary>
internal sealed class TextLineHitTest
{
    private readonly int[] _characterEnds;
    private readonly float[] _characterMidpoints;

    /// <summary>Measures complete Unicode text elements and records their click boundaries.</summary>
    public TextLineHitTest(string text, Func<string, float> measure)
    {
        int[] starts = StringInfo.ParseCombiningCharacters(text);
        _characterEnds = new int[starts.Length];
        _characterMidpoints = new float[starts.Length];
        float previousEnd = 0;
        for (int i = 0; i < starts.Length; i++)
        {
            int next = i + 1 < starts.Length ? starts[i + 1] : text.Length;
            float end = measure(text[..next]);
            _characterEnds[i] = next;
            _characterMidpoints[i] = (previousEnd + end) / 2;
            previousEnd = end;
        }
    }

    /// <summary>Returns the nearest insertion boundary for a horizontal position in unscaled text units.</summary>
    public int PositionAt(float x)
    {
        for (int i = 0; i < _characterMidpoints.Length; i++)
        {
            if (x < _characterMidpoints[i])
                return i == 0 ? 0 : _characterEnds[i - 1];
        }

        return _characterEnds.Length == 0 ? 0 : _characterEnds[^1];
    }
}
