using BooksAndNotes.Integrations;
using BooksAndNotes.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Triggers;
using System.Text.Json;

namespace BooksAndNotes;

/// <summary>
/// Coordinates one item's reader, editor, and persistence. Input and secondary menus
/// are in the other DocumentSession files; Parchment still owns the live reader menu.
/// </summary>
internal sealed partial class DocumentSession
{
    public const string ActionName = ParchmentBookFactory.ActionName;
    private const string ReaderReadyCondition = ParchmentBookFactory.Ready;

    private readonly IParchmentApi _parchment;
    private readonly IViewEngine _viewEngine;
    private readonly Item _item;
    private readonly Document _document;
    private readonly IClickableMenu? _returnMenu;
    private readonly Action<Action> _deferAction;
    private readonly Func<string, string> _translate;
    private readonly IInputHelper _input;
    private readonly bool _isBook;
    private readonly bool _openedReadOnly;
    private readonly string _sessionToken = Guid.NewGuid().ToString("N");
    private readonly ParchmentBookFactory _bookFactory;
    private readonly PageEditor _editor;
    private readonly DocumentRenderer _renderer;
    private readonly Bookmarks _bookmarks = new();
    private readonly bool _bookmarksSupported = true;

    private IClickableMenu? _readerMenu;
    private IMenuController? _bookmarkMenu;
    private bool _usingController;
    private bool _controllerCaretMode;
    private bool _hasSavedDocument;
    private bool _autoFocusPage = true;
    private bool _showingSigningForm;
    private bool _finished;
    private int _firstVisiblePage;
    private int? _pageToRestore;

    private bool IsReadOnly => _openedReadOnly || _document.IsSigned;
    private bool ShouldAutoFocus => !IsReadOnly && !_usingController;
    private int VisiblePageCount => _isBook
        ? _bookmarks.VisibleCount(_document)
        : DocumentPages.VisibleCount(_document, false);

    /// <summary>Connects an item to its local editor and loads bookmarks without risking its writing.</summary>
    public DocumentSession(IParchmentApi api, IViewEngine viewEngine, Item item, Document document,
        IClickableMenu? returnMenu, Action<Action> defer, Func<string, string> translate,
        IInputHelper input, IMonitor monitor, bool controller, bool readOnly = false)
    {
        _parchment = api;
        _viewEngine = viewEngine;
        _item = item;
        _document = document;
        _openedReadOnly = readOnly;
        _isBook = ModEntry.IsBook(item);

        if (_isBook && item.modData.TryGetValue(Bookmarks.DataKey, out string bookmarkJson))
        {
            try
            {
                _bookmarks = Bookmarks.Deserialize(bookmarkJson);
            }
            catch (Exception exception) when (exception is JsonException or InvalidDataException)
            {
                _bookmarksSupported = false;
                monitor.Log("Page tabs are corrupt or unsupported and will not be overwritten. Writing remains available. "
                    + exception.Message, LogLevel.Warn);
            }
        }

        _returnMenu = returnMenu;
        _deferAction = defer;
        _translate = translate;
        _input = input;
        _usingController = controller;
        _bookFactory = new ParchmentBookFactory(api, _sessionToken, translate);
        _renderer = new DocumentRenderer(api, _isBook);
        _editor = new PageEditor(_isBook, IsReadOnly, input, SavePage, CanEditCurrentMenu);
    }

    /// <summary>Accepts typing only while this reader is active, stationary, and outside its signing form.</summary>
    private bool CanEditCurrentMenu() => Game1.activeClickableMenu == _readerMenu
        && !_showingSigningForm && _pageToRestore is null
        && GameStateQuery.CheckConditions(ReaderReadyCondition);

    /// <summary>Identifies menus belonging to this session so unrelated menus can end it safely.</summary>
    public bool Owns(IClickableMenu? menu) => menu is not null
        && (menu == _readerMenu || menu == _bookmarkMenu?.Menu);

    /// <summary>Builds the reader and schedules restoration of its previous spread once Parchment is ready.</summary>
    public void Open()
    {
        _showingSigningForm = false;
        _editor.Blur();
        _controllerCaretMode = false;
        _autoFocusPage = ShouldAutoFocus;
        _firstVisiblePage = Math.Clamp(_firstVisiblePage, 0, VisiblePageCount - 1);
        int requestedPage = _firstVisiblePage;

        OpenBook(CreateReader());
        _pageToRestore = requestedPage > 0 ? requestedPage : null;
    }

    /// <summary>Creates the reader definition, omitting bookmark controls when their metadata is unsupported.</summary>
    private IBookBuilder CreateReader() => _bookFactory.Reader(_document, _isBook,
        Game1.uiViewport.Width, Game1.uiViewport.Height,
        _isBook && _bookmarksSupported ? _bookmarks : null, _openedReadOnly);

    /// <summary>Restores navigation and editor focus after opening or turning a page, then updates input.</summary>
    public void Update()
    {
        if (Game1.activeClickableMenu != _readerMenu || _showingSigningForm
            || !GameStateQuery.CheckConditions(ReaderReadyCondition))
        {
            _editor.Suspend();
            return;
        }

        if (_pageToRestore is int page)
        {
            _pageToRestore = null;
            RunParchmentAction($"PeacefulEnd.Parchment_JumpToPage {page} true");
        }

        if (_autoFocusPage && !IsReadOnly)
        {
            FocusPage(_firstVisiblePage);
            _autoFocusPage = false;
        }

        if (_editor.HasFocus && TryGetWritingBounds(_editor.Page, out Rectangle bounds))
            _editor.Resume(bounds);

        _editor.Update(Game1.currentGameTime);
    }

    /// <summary>Opens a Parchment definition and arranges to return to the menu that opened the item.</summary>
    private void OpenBook(IBookBuilder book)
    {
        _renderer.Clear();
        if (!book.TryOpen(out string error))
            throw new InvalidOperationException($"Parchment could not open the document: {error}");

        _readerMenu = Game1.activeClickableMenu;
        _readerMenu.exitFunction = () => Game1.activeClickableMenu = _returnMenu;
    }

    /// <summary>Runs a native Parchment action and reports failures through the mod's normal error handler.</summary>
    private static void RunParchmentAction(string action)
    {
        if (!TriggerActionManager.TryRunAction(action, out string error, out Exception exception))
            throw new InvalidOperationException(error, exception);
    }

    /// <summary>Validates a reader callback and defers menu-changing work until the current event finishes.</summary>
    public bool Handle(string[] args, out string error)
    {
        // Stardew treats every non-null error string, including an empty one, as failure.
        error = null!;
        if (args.Length < 4 || args[1] != _sessionToken || !int.TryParse(args[3], out int page)
            || (args[2] == "confirm" ? args.Length > 5 : args.Length != 4))
        {
            error = "Invalid or expired Books and Notes session.";
            return false;
        }

        string action = args[2];
        string title = action == "confirm" && args.Length == 5 ? args[4] : "";
        if (action == "view")
        {
            ViewPage(page);
            return true;
        }

        if (Game1.activeClickableMenu != _readerMenu)
        {
            error = "This document is no longer open.";
            return false;
        }

        IClickableMenu expectedMenu = _readerMenu;
        Point focusPoint = UiCoordinates.Cursor(_input.GetCursorPosition());
        bool controllerFocus = _usingController;
        _deferAction(() =>
        {
            if (_finished || Game1.activeClickableMenu != expectedMenu)
                return;

            HandleDeferredAction(action, page, focusPoint, controllerFocus, title);
        });
        return true;
    }

    /// <summary>Tracks the first visible leaf; books align to even-indexed two-page spreads.</summary>
    private void ViewPage(int page)
    {
        int firstPage = _isBook ? page / 2 * 2 : page;
        if (firstPage != _firstVisiblePage)
            PreparePageTurn();

        _firstVisiblePage = firstPage;
    }

    /// <summary>Dispatches validated reader buttons while respecting read-only and signing states.</summary>
    private void HandleDeferredAction(string action, int page, Point focusPoint, bool controllerFocus, string title)
    {
        switch (action)
        {
            case "focus" when !IsReadOnly && !_showingSigningForm:
                if (controllerFocus)
                    EnterControllerCaret(page);
                else
                    FocusPage(page, focusPoint);
                break;

            case "name" when !IsReadOnly && !_showingSigningForm:
                OpenSigningConfirmation();
                break;

            case "confirm" when !IsReadOnly && _showingSigningForm:
                SignAndClose(title);
                break;

            case "cancel" when _showingSigningForm:
                Open();
                break;

            case "close":
                CloseDocument();
                break;

            case "previous" when !_showingSigningForm && _firstVisiblePage > 0:
                PreparePageTurn();
                RunParchmentAction("PeacefulEnd.Parchment_PreviousPage");
                break;

            case "next" when !_showingSigningForm && _firstVisiblePage + (_isBook ? 2 : 1) < VisiblePageCount:
                PreparePageTurn();
                RunParchmentAction("PeacefulEnd.Parchment_NextPage");
                break;

            case "bookmarks" when _isBook && !_openedReadOnly && !_showingSigningForm && _bookmarksSupported:
                OpenBookmarks();
                break;

            case "bookmark-jump" when _isBook && !_showingSigningForm && _bookmarksSupported
                && _bookmarks.Tabs.Any(bookmark => bookmark.Page == page):
                if (!GameStateQuery.CheckConditions(ReaderReadyCondition))
                    break;

                _editor.Flush();
                PreparePageTurn();
                RunParchmentAction($"PeacefulEnd.Parchment_JumpToPage {page} false");
                break;
        }
    }

    /// <summary>Pauses editing for animation and remembers whether mouse/keyboard focus should resume.</summary>
    private void PreparePageTurn()
    {
        _controllerCaretMode = false;
        _editor.Suspend();
        _autoFocusPage = ShouldAutoFocus;
    }

    /// <summary>Saves a valid signature and exits; an invalid title leaves the signing form open.</summary>
    private void SignAndClose(string title)
    {
        if (IsReadOnly)
            return;

        // The button supplies the current input, after Parchment's own quote handling.
        if (!_document.Sign(title, Game1.player.Name))
        {
            Game1.showRedMessage(ModEntry.I18n.Get("error.name", new { limit = Document.MaxTitleLength }).ToString());
            return;
        }

        Save();
        _readerMenu!.exitFunction = () => Game1.activeClickableMenu = _returnMenu;
        _readerMenu.exitThisMenu();
    }

    /// <summary>Commits edits and closes books with animation, but closes static notes immediately.</summary>
    private void CloseDocument()
    {
        _controllerCaretMode = false;
        _editor.Flush();
        _editor.Blur();
        if (_isBook)
            RunParchmentAction("PeacefulEnd.Parchment_CloseBook");
        else
            _readerMenu!.exitThisMenu();
    }

    /// <summary>Draws stationary text and tooltips, then restores the native cursor above the writing.</summary>
    public void Draw(SpriteBatch batch)
    {
        if (_readerMenu is not { } menu || Game1.activeClickableMenu != menu || _pageToRestore is not null)
            return;

        if (!_showingSigningForm && GameStateQuery.CheckConditions(ReaderReadyCondition))
        {
            _renderer.DrawReader(batch, _document, _firstVisiblePage, _editor);
            DrawBookmarkTooltip(batch);
        }

        menu.drawMouse(batch, ignore_transparency: true);
    }

    /// <summary>Uses native ribbon hover state but draws names literally, outside Parchment's token parser.</summary>
    private void DrawBookmarkTooltip(SpriteBatch batch)
    {
        if (!_isBook || !_bookmarksSupported)
            return;

        Bookmark? hoveredBookmark = _bookmarks.Tabs.FirstOrDefault(bookmark => GameStateQuery.CheckConditions(
            $"PeacefulEnd.Parchment_IsHoveringTag {ParchmentBookFactory.BookmarkTag(bookmark.Page)}"));
        if (hoveredBookmark is not null)
            IClickableMenu.drawHoverText(batch, hoveredBookmark.HoverText(_translate("tab.page")), Game1.smallFont);
    }

    /// <summary>Limits animation hooks to this session's book, never another mod's reader or the signing form.</summary>
    public bool CanAnimate(object menu) => _isBook && !_showingSigningForm && _pageToRestore is null
        && Game1.activeClickableMenu == _readerMenu && ReferenceEquals(_readerMenu, menu);

    /// <summary>Draws writing on a turning leaf when this session owns the animated menu.</summary>
    public bool DrawTurn(object menu, SpriteBatch batch, ParchmentTurn turn)
        => CanAnimate(menu) && _renderer.DrawTurn(batch, _document, _editor, turn, VisiblePageCount);

    /// <summary>Draws writing on the inside faces while this session's book closes.</summary>
    public bool DrawClosing(object menu, SpriteBatch batch, ParchmentClose close)
        => CanAnimate(menu) && _renderer.DrawClosing(batch, _document, _editor, close, VisiblePageCount);

    /// <summary>Commits the active page before the game saves.</summary>
    public void Flush() => _editor.Flush();

    /// <summary>Ends local editing and releases menus and textures without discarding accepted changes.</summary>
    public void Finish()
    {
        _finished = true;
        _bookmarkMenu?.Dispose();
        _bookmarkMenu = null;
        _editor.Flush();
        _editor.Blur();
        _renderer.Dispose();
    }

    /// <summary>Stores an editable page, avoiding redundant saves after the first metadata synchronization.</summary>
    private void SavePage(int page, string text)
    {
        if (IsReadOnly)
            return;

        if (_hasSavedDocument && page >= 0 && page < _document.Pages.Count && _document.Pages[page] == text)
            return;

        if (DocumentPages.SetPage(_document, _isBook, page, text))
            Save();
    }

    /// <summary>Writes document metadata and its inventory title, then invalidates cached presentation.</summary>
    private void Save()
    {
        if (_openedReadOnly)
            return;

        _item.modData[ModEntry.DataKey] = _document.Serialize();
        if (_document.IsSigned)
            _item.modData[ModEntry.TitleKey] = _document.Title;
        else
            _item.modData.Remove(ModEntry.TitleKey);

        _renderer.Invalidate();
        _hasSavedDocument = true;
    }
}
