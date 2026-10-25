using BooksAndNotes.Integrations;
using Microsoft.Xna.Framework;

namespace BooksAndNotes;

/// <summary>Builds reader artwork and controls; player writing is drawn separately so it cannot become executable markup.</summary>
internal sealed class ParchmentBookFactory(IParchmentApi api, string token, Func<string, string> translate)
{
    internal const string BookTexture = "Mods/Trapyy.BooksAndNotes/smallBook";
    public const string ActionName = "Trapyy.BooksAndNotes_Document";
    public const string Ready = "PeacefulEnd.Parchment_CurrentBookState Ready";

    /// <summary>Builds a callback containing this reading session's token and zero-based page number.</summary>
    public string Command(string verb, int page = 0) => $"{ActionName} {token} {verb} {page}";

    /// <summary>Creates the native hover tag used to identify a bookmark ribbon.</summary>
    internal static string BookmarkTag(int page) => $"Trapyy.BooksAndNotes_Bookmark_{page}";
    public string TitleInputId => $"Trapyy.BooksAndNotes_Title_{token}";

    /// <summary>Builds visible pages, reader buttons, navigation bindings, and optional bookmark ribbons.</summary>
    public IBookBuilder Reader(Document document, bool isBook, int width, int height, Bookmarks? bookmarks = null, bool readOnly = false)
    {
        IBookBuilder book = Create("Reader", isBook, width, height);
        int pageCount = isBook && bookmarks is not null
            ? bookmarks.VisibleCount(document)
            : DocumentPages.VisibleCount(document, isBook);
        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            AddReaderPage(book, pageIndex, isBook, !document.IsSigned && !readOnly, width, height);
        }

        if (document.IsSigned || readOnly)
            AddToolbarButton(book, translate("button.close"), "close", 0, bottom: isBook);
        else
            AddToolbarButton(book, translate("button.title-sign"), "name", 0, bottom: isBook);

        book.OnKeyPress("PageDown, RightShoulder").Action(Command("next"));
        book.OnKeyPress("PageUp, LeftShoulder").Action(Command("previous"));
        if (isBook && bookmarks is not null)
        {
            if (!readOnly)
                AddToolbarButton(book, translate("tab.bookmarks"), "bookmarks", 0).Set("Id", "bookmarks");

            AddBookmarks(book, bookmarks, width, height);
        }

        return book;
    }

    /// <summary>Adds a page-view callback, invisible controller focus target, and book page number.</summary>
    private void AddReaderPage(IBookBuilder book, int pageIndex, bool isBook, bool editable, int width, int height)
    {
        IPageBuilder page = book.AddPage($"page-{pageIndex}");
        page.OnView(Command("view", pageIndex));
        if (editable)
        {
            page.AddForeground("Panel").Set("Id", $"writing-page-{pageIndex}")
                .Set("Action", Command("focus", pageIndex)).Set("Sound", null)
                .Set("Sizing", "Fill").Set("Height", Math.Max(1, (int)(page.GetAvailableHeight()
                    - (isBook ? 26 * BookScale(true, width, height) : 0))));
        }

        if (isBook)
        {
            page.AddForeground("Paragraph").Set("Text", (pageIndex + 1).ToString())
                .Set("Alignment", "Center").Set("VerticalAlignment", "Bottom").Set("Scale", .65f);
        }
    }

    /// <summary>Places page-ordered ribbons around the cover using Parchment's native hover sprite swap.</summary>
    private void AddBookmarks(IBookBuilder book, Bookmarks bookmarks, int width, int height)
    {
        const int bookmarksPerSide = Bookmarks.MaxTabs / 2;
        const int spriteWidth = 24;
        float bookScale = BookScale(true, width, height);
        // Ribbons overlap the cover but stop at the paper's five-pixel inset.
        int paperInset = (int)MathF.Round(5 * bookScale);
        float sideSpace = (width - 219 * bookScale) / 2 - 8;
        float ribbonScale = Math.Clamp((sideSpace + paperInset) / spriteWidth, .25f, bookScale);
        int ribbonWidth = (int)MathF.Round(spriteWidth * ribbonScale);
        const string visible = "ANY \"PeacefulEnd.Parchment_CurrentBookState Ready\" \"PeacefulEnd.Parchment_CurrentBookState Turning\"";
        Bookmark[] ordered = bookmarks.Tabs.OrderBy(bookmark => bookmark.Page).ToArray();
        for (int slot = 0; slot < ordered.Length; slot++)
        {
            Bookmark bookmark = ordered[slot];
            bool left = slot < bookmarksPerSide;
            int y = (int)((20 + slot % bookmarksPerSide * 19) * bookScale) + (int)MathF.Round(3 * ribbonScale);
            // Both sprite crops are 14 pixels tall, so hover grows sideways rather than vertically.
            book.AddOverlay("Image").Set("Id", $"bookmark-{bookmark.Page}")
                .Set("Tags", new List<string> { BookmarkTag(bookmark.Page) })
                .Set("TexturePath", "Mods/Trapyy.BooksAndNotes/bookmark")
                .Set("TextureSourceRectangle", new Rectangle(0, 3, spriteWidth, 14))
                .Set("HoverTextureSourceRectangle", new Rectangle(0, 17, spriteWidth, 14))
                .Set("TintColor", BookmarkColors.Choices[bookmark.Color].Tint).Set("Scale", ribbonScale)
                .Set("Alignment", left ? "Left" : "Right").Set("SpriteEffects", left ? "FlipHorizontally" : "None")
                .Set("Position", new Point(left ? paperInset - ribbonWidth : ribbonWidth - paperInset, y))
                .Set("Action", Command("bookmark-jump", bookmark.Page)).Set("ParseTokenizableStrings", false)
                .Set("Condition", visible);
        }
    }

    /// <summary>Builds the native title/signature form with a blank left page and controls on the right.</summary>
    public IBookBuilder Confirmation(bool isBook, int width, int height)
    {
        IBookBuilder book = Create("Sign", true, width, height);
        book.Set("PageCurl.IsEnabled", false);
        book.AddPage("signature-blank");
        IPageBuilder page = book.AddPage("signature");
        page.AddHeading(translate(isBook ? "sign.book-title" : "sign.note-title")).Set("SpacingAfter", 12);
        // Keep title entry native so Parchment owns focus and the controller keyboard.
        page.Add("Input").Set("InputId", TitleInputId).Set("Id", "signing-title")
            .Set("MaxLength", Document.MaxTitleLength).Set("Placeholder", "")
            .Set("TexturePath", "Mods/Trapyy.BooksAndNotes/button")
            .Set("TextureSourceRectangle", new Rectangle(0, 0, 18, 18))
            .Set("FontType", "Small").Set("TextScale", 1f).Set("Scale", 1f).Set("Padding", 5)
            .Set("ParseTokenizableStrings", false).Set("SpacingAfter", 32);
        page.AddParagraph(translate("sign.warning")).Set("FontType", "Small").Set("Scale", 1f).Set("SpacingAfter", 24);
        // Read the input at click time; Parchment quotes it as one action argument.
        StyleButton(page.AddButton(translate("button.sign-close"), $"{Command("confirm")} %Input:{TitleInputId}%"))
            .Set("ParseTokenizableStrings", false).Set("SpacingAfter", 8);
        StyleButton(page.AddButton(translate("button.cancel"), Command("cancel")));
        return book;
    }

    /// <summary>Applies shared book appearance or the static, mirrored-note presentation.</summary>
    private IBookBuilder Create(string suffix, bool spread, int width, int height)
    {
        float scale = BookScale(spread, width, height);
        // Reuse presentation IDs so reopening does not grow Parchment's reading history.
        IBookBuilder book = api.CreateBook($"Trapyy.BooksAndNotes_{suffix}")
            .Set("Format", "1.15.1").Set("Appearance.Scale", scale)
            .Set("Appearance.TexturePath", BookTexture)
            // This tint affects the cover layer, not the paper.
            .Set("Appearance.TintColor", "160 96 56").Set("PageCurl.Scale", scale)
            .Set("Layout.MarginTop", spread ? 22 : 18)
            .Set("Layout.MarginBottom", spread ? 24 : 18)
            .Set("Animation.SlideDuration", 1f).Set("Animation.OpenDuration", 1f);
        if (!spread)
        {
            book.Set("Appearance.TexturePath", "Mods/Trapyy.BooksAndNotes/note-frame")
                .Set("Appearance.GrayscaleTexturePath", null).Set("Appearance.FrameWidth", 512)
                .Set("Appearance.FrameHeight", 632).Set("Appearance.OpenFrameCount", 1)
                .Set("Appearance.TurnFrameCount", 0).Set("Layout.IsSinglePage", true)
                .Set("Layout.MarginOuter", 76).Set("Layout.MarginSpine", 76)
                .Set("Layout.MarginTop", 40).Set("Layout.MarginBottom", 40)
                .Set("PageCurl.IsEnabled", false);
            // Mirror only the back-side paper artwork; the separately drawn writing stays readable.
            book.AddUnderlay("Image").Set("TexturePath", "Mods/Trapyy.BooksAndNotes/note")
                .Set("Scale", scale).Set("IgnoreCursor", true)
                .Set("Condition", "!PeacefulEnd.Parchment_CurrentPageId page-1");
            book.AddUnderlay("Image").Set("TexturePath", "Mods/Trapyy.BooksAndNotes/note")
                .Set("Scale", scale).Set("SpriteEffects", "FlipHorizontally").Set("IgnoreCursor", true)
                .Set("Condition", "PeacefulEnd.Parchment_CurrentPageId page-1");
            AddToolbarButton(book, translate("button.previous"), "previous", -85, bottom: true);
            AddToolbarButton(book, translate("button.next"), "next", 85, bottom: true);
        }
        return book;
    }

    /// <summary>Uses the fixed reader size when space permits, with the existing small-viewport fallback.</summary>
    internal static float BookScale(bool spread, int width, int height)
    {
        float scale = Math.Max(.25f, Math.Min(spread ? 4f : 1f, Math.Min(
            (width - 48f) / (spread ? 219 : 512), (height - 80f) / (spread ? 158 : 632))));
        return spread && scale >= 1 ? MathF.Floor(scale) : scale;
    }

    /// <summary>Adds a centered top or bottom reader button with a horizontal offset.</summary>
    private IElementBuilder AddToolbarButton(IBookBuilder book, string text, string verb, int offset, bool bottom = false)
        => StyleButton(book.AddOverlay("Button")).Set("Text", text).Set("Action", Command(verb))
            .Set("Alignment", "Center").Set("VerticalAlignment", bottom ? "Bottom" : "Top")
            .Set("Position", new Point(offset, bottom ? 40 : -40)).Set("Condition", Ready);

    /// <summary>Applies the shared button sprite, hover frame, font, and padding.</summary>
    private static IElementBuilder StyleButton(IElementBuilder element)
        => element.Set("TexturePath", "Mods/Trapyy.BooksAndNotes/button")
            .Set("TextureSourceRectangle", new Rectangle(0, 0, 18, 18))
            .Set("HoverTextureSourceRectangle", new Rectangle(0, 18, 18, 18))
            .Set("FontType", "Small").Set("TextScale", 1f).Set("Scale", 1f).Set("Padding", 5);
}
