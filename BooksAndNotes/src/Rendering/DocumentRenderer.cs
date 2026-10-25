using BooksAndNotes.Integrations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace BooksAndNotes.Rendering;

/// <summary>Draws resting pages with native text and delegates animated ink to a separate texture renderer.</summary>
internal sealed class DocumentRenderer(IParchmentApi api, bool isBook) : IDisposable
{
    private readonly BookTurnRenderer _turnRenderer = new();
    private readonly Dictionary<(string Text, int Width, int Height, SpriteFont Font), PageTextLayout> _pageLayouts = [];
    private int _documentRevision;

    /// <summary>Draws note ruling and visible saved pages, leaving the focused page's text to its editor.</summary>
    public void DrawReader(SpriteBatch batch, Document document, int firstPage, PageEditor editor)
    {
        if (!api.TryGetLeftPageBounds(out Rectangle left))
            return;

        api.TryGetRightPageBounds(out Rectangle right);

        // Capture bitmap ink only for animation; resting writing uses native font pixels.
        if (!isBook)
            PagePresentation.DrawNoteRules(batch, PagePresentation.WritingBounds(left, false), firstPage == 1);

        int editedPage = editor.HasFocus ? editor.Page : -1;
        DrawPage(batch, document, firstPage, left, editedPage);
        if (isBook && !right.IsEmpty)
            DrawPage(batch, document, firstPage + 1, right, editedPage);

        editor.Draw(batch);
    }

    /// <summary>Draws an unfocused saved page using a layout cached for its text, font, and displayed size.</summary>
    private void DrawPage(SpriteBatch batch, Document document, int page, Rectangle bounds, int editedPage)
    {
        if (page == editedPage || page >= document.Pages.Count)
            return;

        string text = document.Pages[page];
        Rectangle writingBounds = PagePresentation.WritingBounds(bounds, isBook);
        var cacheKey = (text, writingBounds.Width, writingBounds.Height, Game1.smallFont);
        if (!_pageLayouts.TryGetValue(cacheKey, out PageTextLayout? layout))
        {
            layout = PagePresentation.Layout(text, isBook);
            _pageLayouts[cacheKey] = layout;
        }

        PagePresentation.Draw(batch, text, writingBounds, layout, isBook);
    }

    /// <summary>Suspends caret input and draws a turn when all native book bounds are available.</summary>
    public bool DrawTurn(SpriteBatch batch, Document document, PageEditor editor, ParchmentTurn turn, int visiblePageCount)
    {
        if (!TryGetBookPageBounds(out Rectangle book, out Rectangle left, out Rectangle right))
            return false;

        editor.Suspend();
        _turnRenderer.Draw(batch, document, _documentRevision, turn, book, left, right, visiblePageCount);
        return true;
    }

    /// <summary>Suspends caret input and draws ink on the closing book's visible inside faces.</summary>
    public bool DrawClosing(SpriteBatch batch, Document document, PageEditor editor, ParchmentClose close, int visiblePageCount)
    {
        if (!TryGetBookPageBounds(out Rectangle book, out Rectangle left, out Rectangle right))
            return false;

        editor.Suspend();
        _turnRenderer.DrawClosing(batch, document, _documentRevision, close, book, left, right, visiblePageCount);
        return true;
    }

    /// <summary>Gets the outer book and both page rectangles together for animation.</summary>
    private bool TryGetBookPageBounds(out Rectangle book, out Rectangle left, out Rectangle right)
    {
        left = right = default;
        return api.TryGetBookBounds(out book) && api.TryGetLeftPageBounds(out left) && api.TryGetRightPageBounds(out right);
    }

    /// <summary>Discards layout measurements and releases captured ink after a presentation change.</summary>
    public void Clear()
    {
        _pageLayouts.Clear();
        _turnRenderer.Clear();
    }

    /// <summary>Marks edited writing as a new revision and removes stale presentation caches.</summary>
    public void Invalidate()
    {
        _documentRevision++;
        Clear();
    }

    /// <summary>Releases the animation renderer's GPU resources when the session ends.</summary>
    public void Dispose() => _turnRenderer.Dispose();
}
