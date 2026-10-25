using BooksAndNotes.Integrations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace BooksAndNotes.Rendering;

/// <summary>Caches page ink and bends or clips its columns to follow Parchment's turning and closing paper.</summary>
internal sealed class BookTurnRenderer : IDisposable
{
    private readonly Dictionary<int, Texture2D> _pageTextures = [];
    private Rectangle _bookBounds;
    private Rectangle _leftContentBounds;
    private Rectangle _rightContentBounds;
    private SpriteFont? _font;
    private int _oldFirstPage = -1;
    private int _newFirstPage = -1;
    private int _documentRevision = -1;
    private int _pageCount;

    private float Scale => _bookBounds.Width / 219f;

    // Sprite-space paper bounds include both the writing margins and page numbers.
    private static readonly Rectangle LeftPaper = new(8, 17, 102, 122);
    private static readonly Rectangle RightPaper = new(110, 17, 102, 122);

    /// <summary>Draws the exposed pages and the correct front or back of the moving leaf for one turn frame.</summary>
    public void Draw(SpriteBatch batch, Document document, int revision, ParchmentTurn turn,
        Rectangle book, Rectangle left, Rectangle right, int count)
    {
        if (turn.OldFirst < 0 || turn.NewFirst < 0 || turn.OldFirst >= count || turn.NewFirst >= count
            || turn.OldFirst % 2 != 0 || turn.NewFirst % 2 != 0 || turn.SpriteFrame is < 0 or > 5)
        {
            throw new InvalidOperationException("Parchment reported an invalid personal-book turn.");
        }

        PrepareCache(revision, turn.OldFirst, turn.NewFirst, book, left, right, count);

        int frame = turn.Forward ? turn.SpriteFrame : 5 - turn.SpriteFrame;
        PageTurnShape movingFace = PageTurnShape.Faces[frame];
        PageTurnShape? foldedEdge = frame == 3 ? PageTurnShape.Fold : null;
        int leftPageUnderneath = turn.Forward ? turn.OldFirst : turn.NewFirst;
        int rightPageUnderneath = turn.Forward ? turn.NewFirst + 1 : turn.OldFirst + 1;
        int movingPage;
        if (frame < 3)
            movingPage = turn.Forward ? turn.OldFirst + 1 : turn.NewFirst + 1;
        else
            movingPage = turn.Forward ? turn.NewFirst : turn.OldFirst;

        // Finish captures before drawing: a capture error can still fall back to native rendering cleanly.
        Texture2D leftInk = GetPageTexture(batch.GraphicsDevice, document, leftPageUnderneath);
        Texture2D rightInk = GetPageTexture(batch.GraphicsDevice, document, rightPageUnderneath);
        Texture2D movingInk = GetPageTexture(batch.GraphicsDevice, document, movingPage);
        DrawExposedPage(batch, leftInk, GetPaperBounds(leftPageUnderneath), movingFace, foldedEdge);
        DrawExposedPage(batch, rightInk, GetPaperBounds(rightPageUnderneath), movingFace, foldedEdge);
        DrawMovingPage(batch, movingInk, movingFace);
    }

    /// <summary>Reuses page textures only while geometry, font, writing revision, and spread are unchanged.</summary>
    private void PrepareCache(int revision, int oldFirst, int newFirst, Rectangle book, Rectangle left, Rectangle right, int count)
    {
        bool cacheMatches = _bookBounds == book && _leftContentBounds == left && _rightContentBounds == right
            && _font == Game1.smallFont && _oldFirstPage == oldFirst && _newFirstPage == newFirst
            && _documentRevision == revision && _pageCount == count
            && !_pageTextures.Values.Any(texture => texture.IsDisposed);
        if (cacheMatches)
            return;

        Clear();
        _bookBounds = book;
        _leftContentBounds = left;
        _rightContentBounds = right;
        _font = Game1.smallFont;
        _oldFirstPage = oldFirst;
        _newFirstPage = newFirst;
        _documentRevision = revision;
        _pageCount = count;
    }

    /// <summary>Draws both inside faces during closing, hiding their writing once only the covers remain.</summary>
    public void DrawClosing(SpriteBatch batch, Document document, int revision, ParchmentClose close,
        Rectangle book, Rectangle left, Rectangle right, int visiblePageCount)
    {
        if (close.OpenFrame is < 0 or > 3 || close.First < 0 || close.First >= visiblePageCount)
            throw new InvalidOperationException("Parchment reported an invalid closing frame.");

        if (close.OpenFrame == 0)
            return;

        PrepareCache(revision, close.First, close.First, book, left, right, visiblePageCount);
        Texture2D leftInk = GetPageTexture(batch.GraphicsDevice, document, close.First);
        Texture2D rightInk = GetPageTexture(batch.GraphicsDevice, document, close.First + 1);
        if (close.OpenFrame == 3)
        {
            batch.Draw(leftInk, GetPaperBounds(close.First), Color.White);
            batch.Draw(rightInk, GetPaperBounds(close.First + 1), Color.White);
            return;
        }

        DrawMovingPage(batch, leftInk, PageTurnShape.ClosingLeft[close.OpenFrame]!);
        DrawMovingPage(batch, rightInk, PageTurnShape.ClosingRight[close.OpenFrame]!);
    }

    /// <summary>Converts the left or right paper rectangle from sprite pixels into displayed UI pixels.</summary>
    private Rectangle GetPaperBounds(int page)
    {
        Rectangle paper = page % 2 == 0 ? LeftPaper : RightPaper;
        return new Rectangle(_bookBounds.X + (int)MathF.Round(paper.X * Scale),
            _bookBounds.Y + (int)MathF.Round(paper.Y * Scale),
            Math.Max(1, (int)MathF.Round(paper.Width * Scale)), Math.Max(1, (int)MathF.Round(paper.Height * Scale)));
    }

    /// <summary>Captures writing and its page number once; nonexistent signed-book leaves stay fully transparent.</summary>
    private Texture2D GetPageTexture(GraphicsDevice device, Document document, int page)
    {
        if (_pageTextures.TryGetValue(page, out Texture2D? cachedTexture))
            return cachedTexture;

        Rectangle paper = GetPaperBounds(page);
        Color[] pixels = new Color[checked(paper.Width * paper.Height)];
        if (page >= 0 && page < _pageCount)
        {
            string text = DocumentPages.GetText(document, page);
            Rectangle content = page % 2 == 0 ? _leftContentBounds : _rightContentBounds;
            Rectangle writingBounds = PagePresentation.WritingBounds(content);
            PageTextLayout layout = PagePresentation.Layout(text);
            float textScale = PagePresentation.Scale(writingBounds, layout);
            FontInk ink = FontInk.Get(Game1.smallFont);
            for (int lineIndex = 0; lineIndex < layout.Lines.Count; lineIndex++)
            {
                VisualLine line = layout.Lines[lineIndex];
                Vector2 linePosition = PagePresentation.LineOrigin(writingBounds, layout, lineIndex, true)
                    - new Vector2(paper.X, paper.Y);
                ink.Draw(pixels, paper.Width, paper.Height, text.Substring(line.Start, line.Length),
                    linePosition, textScale, MenuStyle.Ink);
            }

            string pageNumber = (page + 1).ToString();
            Vector2 labelSize = Game1.smallFont.MeasureString(pageNumber) * .65f;
            Vector2 labelPosition = new(content.X - paper.X + (content.Width - labelSize.X) / 2,
                content.Bottom - paper.Y - labelSize.Y);
            ink.Draw(pixels, paper.Width, paper.Height, pageNumber, labelPosition, .65f, MenuStyle.Ink);
        }

        Texture2D texture = new(device, paper.Width, paper.Height, false, SurfaceFormat.Color);
        try
        {
            texture.SetData(pixels);
            _pageTextures.Add(page, texture);
            return texture;
        }
        catch
        {
            texture.Dispose();
            throw;
        }
    }

    /// <summary>Draws only the stationary-page columns not covered by the moving sheet or its folded edge.</summary>
    private void DrawExposedPage(SpriteBatch batch, Texture2D ink, Rectangle paper, PageTurnShape face, PageTurnShape? fold)
    {
        for (int column = 0; column < paper.Width; column++)
        {
            float spriteX = (paper.X + column + .5f - _bookBounds.X) / Scale;
            bool covered = TryGetCoverage(face, spriteX, out float top, out float bottom);
            if (fold is not null && TryGetCoverage(fold, spriteX, out float foldTop, out float foldBottom))
            {
                top = covered ? Math.Min(top, foldTop) : foldTop;
                bottom = covered ? Math.Max(bottom, foldBottom) : foldBottom;
                covered = true;
            }

            if (!covered)
            {
                DrawColumnSegment(batch, ink, paper, column, 0, paper.Height);
                continue;
            }

            int beforeSheet = Math.Clamp((int)MathF.Floor(_bookBounds.Y + top * Scale - paper.Y), 0, paper.Height);
            int afterSheet = Math.Clamp((int)MathF.Ceiling(_bookBounds.Y + bottom * Scale - paper.Y), 0, paper.Height);
            DrawColumnSegment(batch, ink, paper, column, 0, beforeSheet);
            DrawColumnSegment(batch, ink, paper, column, afterSheet, paper.Height);
        }
    }

    /// <summary>Draws one visible vertical segment from a stationary page's ink texture.</summary>
    private static void DrawColumnSegment(SpriteBatch batch, Texture2D ink, Rectangle paper, int column, int start, int end)
    {
        if (end <= start)
            return;

        batch.Draw(ink, new Rectangle(paper.X + column, paper.Y + start, 1, end - start),
            new Rectangle(column, start, 1, end - start), Color.White, 0, Vector2.Zero, SpriteEffects.None, .9f);
    }

    /// <summary>Finds the sheet's covered vertical span, including the sprite border and narrow shadow.</summary>
    private static bool TryGetCoverage(PageTurnShape shape, float x, out float top, out float bottom)
    {
        if (x < shape.Left - 1 || x > shape.Right + 1)
        {
            top = bottom = 0;
            return false;
        }

        shape.Sample(Math.Clamp(x, shape.Left, shape.Right), out top, out bottom);
        top -= 1.5f;
        bottom += 1.5f;
        return true;
    }

    /// <summary>Stretches individual ink columns along the moving paper's curved top and bottom edges.</summary>
    private void DrawMovingPage(SpriteBatch batch, Texture2D ink, PageTurnShape face)
    {
        int startX = (int)MathF.Ceiling(_bookBounds.X + face.Left * Scale);
        int endX = (int)MathF.Floor(_bookBounds.X + face.Right * Scale);
        for (int x = startX; x < endX; x++)
        {
            float spriteX = (x + .5f - _bookBounds.X) / Scale;
            face.Sample(spriteX, out float top, out float bottom);
            float horizontalFraction = Math.Clamp((spriteX - face.Left) / (face.Right - face.Left), 0, 1);
            int sourceX = Math.Min(ink.Width - 1, (int)(horizontalFraction * ink.Width));
            int y = (int)MathF.Round(_bookBounds.Y + top * Scale);
            int height = Math.Max(1, (int)MathF.Round((bottom - top) * Scale));
            batch.Draw(ink, new Rectangle(x, y, 1, height), new Rectangle(sourceX, 0, 1, ink.Height),
                Color.White, 0, Vector2.Zero, SpriteEffects.None, .91f);
        }
    }

    /// <summary>Releases all cached page textures after editing, navigation, or a presentation change.</summary>
    public void Clear()
    {
        foreach (Texture2D texture in _pageTextures.Values)
            texture.Dispose();

        _pageTextures.Clear();
    }

    /// <summary>Releases GPU resources when the reading session ends.</summary>
    public void Dispose() => Clear();
}
