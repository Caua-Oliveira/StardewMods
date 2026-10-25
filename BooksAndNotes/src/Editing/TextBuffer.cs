using System.Globalization;
using System.Text;

namespace BooksAndNotes;

/// <summary>Stores page text, selection, Unicode caret boundaries, and undo/redo history without game dependencies.</summary>
public sealed class TextBuffer(string text, int limit, bool multiline)
{
    public string Text { get; private set; } = text;
    public int Caret { get; private set; } = text.Length;
    public int Anchor { get; private set; } = text.Length;
    public int SelectionStart => Math.Min(Caret, Anchor);
    public int SelectionEnd => Math.Max(Caret, Anchor);
    public bool HasSelection => Caret != Anchor;
    private const int MaxUndoStates = 100;
    private readonly List<(string Text, int Caret, int Anchor)> _undoStates = [];
    private readonly List<(string Text, int Caret, int Anchor)> _redoStates = [];
    private string? _boundaryText;
    private int[]? _cachedBoundaries;

    private int[] ElementBoundaries
    {
        get
        {
            if (!ReferenceEquals(_boundaryText, Text))
            {
                _cachedBoundaries = StringInfo.ParseCombiningCharacters(Text);
                _boundaryText = Text;
            }
            return _cachedBoundaries!;
        }
    }

    /// <summary>Records the state before an edit and discards redo history from the abandoned branch.</summary>
    private void Remember()
    {
        _undoStates.Add((Text, Caret, Anchor));
        if (_undoStates.Count > MaxUndoStates)
            _undoStates.RemoveAt(0);

        _redoStates.Clear();
    }

    /// <summary>Restores the most recent text and selection before an edit.</summary>
    public bool Undo() => Restore(_undoStates, _redoStates);

    /// <summary>Reapplies the most recently undone edit and its selection.</summary>
    public bool Redo() => Restore(_redoStates, _undoStates);

    /// <summary>Transfers the current state to one history list and restores the last state from the other.</summary>
    private bool Restore(List<(string Text, int Caret, int Anchor)> source, List<(string Text, int Caret, int Anchor)> destination)
    {
        if (source.Count == 0)
            return false;

        destination.Add((Text, Caret, Anchor));
        (Text, Caret, Anchor) = source[^1];
        source.RemoveAt(source.Count - 1);
        return true;
    }

    /// <summary>Moves to a clamped UTF-16 position, optionally preserving the selection anchor.</summary>
    public void MoveTo(int position, bool extend = false)
    {
        Caret = Math.Clamp(position, 0, Text.Length);
        // A paired character must not be split by an insertion or selection boundary.
        if (Caret > 0 && Caret < Text.Length && char.IsLowSurrogate(Text[Caret]) && char.IsHighSurrogate(Text[Caret - 1]))
            Caret--;

        if (!extend)
            Anchor = Caret;
    }

    /// <summary>Selects the complete buffer with the caret at its end.</summary>
    public void SelectAll()
    {
        Anchor = 0;
        Caret = Text.Length;
    }

    /// <summary>Moves by a Unicode text element, or collapses an unextended selection toward the requested side.</summary>
    public void MoveHorizontal(int direction, bool extend)
    {
        if (HasSelection && !extend)
        {
            MoveTo(direction < 0 ? SelectionStart : SelectionEnd);
            return;
        }

        MoveTo(direction < 0 ? PreviousElement(Caret) : NextElement(Caret), extend);
    }

    /// <summary>Replaces the selection with the accepted input prefix as a single undoable edit.</summary>
    public bool Insert(string input, Func<string, bool>? fits = null)
    {
        string value = NormalizeInput(input);
        if (value.Length == 0)
            return false;

        int available = Math.Max(0, limit - (Text.Length - (SelectionEnd - SelectionStart)));
        if (value.Length > available)
        {
            int length = available;
            if (length > 0 && char.IsHighSurrogate(value[length - 1]))
                length--;
            value = value[..length];
        }

        if (value.Length == 0)
            return false;

        int start = SelectionStart;
        string before = Text[..start];
        string after = Text[SelectionEnd..];
        if (fits is not null && !fits(before + value + after))
        {
            value = FindFittingPrefix(value, before, after, fits);
            if (value.Length == 0)
                return false;
        }

        // Do not change selection or history until some input has actually been accepted.
        Remember();
        Text = before + value + after;
        MoveTo(start + value.Length);
        return true;
    }

    /// <summary>Deletes the selection, or one Unicode text element before/after the caret.</summary>
    public bool Delete(bool backwards)
    {
        int start;
        int end;
        if (HasSelection)
        {
            start = SelectionStart;
            end = SelectionEnd;
        }
        else if (backwards)
        {
            start = PreviousElement(Caret);
            end = Caret;
        }
        else
        {
            start = Caret;
            end = NextElement(Caret);
        }

        if (start == end)
            return false;

        Remember();
        Text = Text.Remove(start, end - start);
        MoveTo(start);
        return true;
    }

    /// <summary>Normalizes newlines, expands tabs, and filters control characters from incoming text.</summary>
    private string NormalizeInput(string input)
    {
        input = input.Replace("\r\n", "\n").Replace('\r', '\n');
        StringBuilder normalized = new();
        foreach (char character in input)
        {
            if (character == '\n')
                normalized.Append(multiline ? '\n' : ' ');
            else if (character == '\t')
                normalized.Append("    ");
            else if (!char.IsControl(character))
                normalized.Append(character);
        }

        return normalized.ToString();
    }

    /// <summary>Binary-searches the accepted paste prefix without splitting a Unicode text element.</summary>
    private static string FindFittingPrefix(string value, string before, string after, Func<string, bool> fits)
    {
        int[] boundaries = StringInfo.ParseCombiningCharacters(value);
        int acceptedElements = 0;
        int maximumElements = boundaries.Length;
        while (acceptedElements < maximumElements)
        {
            int candidateElements = (acceptedElements + maximumElements + 1) / 2;
            int length = candidateElements == boundaries.Length ? value.Length : boundaries[candidateElements];
            if (fits(before + value[..length] + after))
                acceptedElements = candidateElements;
            else
                maximumElements = candidateElements - 1;
        }

        if (acceptedElements == 0)
            return "";

        int acceptedLength = acceptedElements == boundaries.Length ? value.Length : boundaries[acceptedElements];
        return value[..acceptedLength];
    }

    /// <summary>Finds the text-element boundary immediately before a caret position.</summary>
    private int PreviousElement(int position)
    {
        int[] boundaries = ElementBoundaries;
        int index = Array.BinarySearch(boundaries, position);
        index = index >= 0 ? index - 1 : ~index - 1;
        return index >= 0 ? boundaries[index] : 0;
    }

    /// <summary>Finds the text-element boundary immediately after a caret position.</summary>
    private int NextElement(int position)
    {
        int[] boundaries = ElementBoundaries;
        int index = Array.BinarySearch(boundaries, position);
        index = index >= 0 ? index + 1 : ~index;
        return index < boundaries.Length ? boundaries[index] : Text.Length;
    }
}
