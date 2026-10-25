using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace BooksAndNotes;

/// <summary>Reader mouse, keyboard, and controller handling for a document session.</summary>
internal sealed partial class DocumentSession
{
    /// <summary>Finds a visible page's writing area using the same bounds as its displayed text.</summary>
    private bool TryGetWritingBounds(int page, out Rectangle bounds)
    {
        bounds = default;
        if (page < _firstVisiblePage || page > _firstVisiblePage + (_isBook ? 1 : 0) || page >= VisiblePageCount)
            return false;

        bool found = _isBook && page % 2 == 1
            ? _parchment.TryGetRightPageBounds(out bounds)
            : _parchment.TryGetLeftPageBounds(out bounds);
        if (found)
            bounds = PagePresentation.WritingBounds(bounds, _isBook);

        return found;
    }

    /// <summary>Focuses an editable leaf and optionally positions its caret or begins a mouse selection.</summary>
    private void FocusPage(int page, Point? click = null, bool drag = false)
    {
        if (IsReadOnly || !TryGetWritingBounds(page, out Rectangle bounds))
            return;

        _editor.Focus(page, DocumentPages.GetText(_document, page), bounds, click, drag);
        _autoFocusPage = false;
    }

    /// <summary>Lets the controller position the caret before a second confirmation opens the keyboard.</summary>
    private void EnterControllerCaret(int page)
    {
        if (IsReadOnly || !TryGetWritingBounds(page, out _))
            return;

        FocusPage(page);
        _controllerCaretMode = true;
    }

    /// <summary>Returns the controller from caret positioning to Parchment's ordinary menu navigation.</summary>
    private void LeaveControllerCaret()
    {
        _controllerCaretMode = false;
        _autoFocusPage = false;
        _editor.Blur();
        if (Game1.options.SnappyMenus)
            _readerMenu?.snapToDefaultClickableComponent();
    }

    /// <summary>Handles session-level input states and returns whether the game should suppress the button.</summary>
    public bool ButtonPressed(ButtonPressedEventArgs e)
    {
        if (Game1.activeClickableMenu != _readerMenu || _showingSigningForm)
            return false;

        UpdateInputMode(e.Button);
        if (Game1.textEntry is null && e.Button == SButton.ControllerB && _controllerCaretMode)
        {
            LeaveControllerCaret();
            return true;
        }

        if (!_isBook && Game1.textEntry is null && (e.Button is SButton.Escape or SButton.ControllerB))
        {
            _editor.Flush();
            _editor.Blur();
            _controllerCaretMode = false;
            _autoFocusPage = false;
            _readerMenu!.exitThisMenu();
            return true;
        }

        if (Game1.textEntry is not null)
        {
            // The native keyboard is single-line; X inserts into our multiline buffer instead.
            return e.Button == SButton.ControllerX && _editor.HasOnScreenKeyboard && _editor.ControllerButton(e.Button);
        }

        if (_pageToRestore is not null)
            return false;

        if (!GameStateQuery.CheckConditions(ReaderReadyCondition))
        {
            _editor.Suspend();
            // Ignore typing during a turn rather than passing it to world or menu shortcuts.
            return (_editor.HasFocus || _autoFocusPage)
                && e.Button.TryGetKeyboard(out Keys pausedKey) && pausedKey != Keys.Escape;
        }

        return HandleReaderInput(e);
    }

    /// <summary>Routes stationary-reader controls to page focus, caret movement, or normal Parchment navigation.</summary>
    private bool HandleReaderInput(ButtonPressedEventArgs e)
    {
        if (e.Button is SButton.Escape or SButton.ControllerB)
        {
            _editor.Blur();
            _controllerCaretMode = false;
            _autoFocusPage = false;
            return false;
        }

        if (e.Button == SButton.MouseLeft && !IsReadOnly)
            return HandlePageClick(UiCoordinates.Cursor(e.Cursor));

        if (e.Button == SButton.ControllerA && !IsReadOnly)
            return HandleControllerConfirm(UiCoordinates.Cursor(e.Cursor));

        if (_controllerCaretMode && PageEditor.Direction(e.Button) >= 0)
            return _editor.NavigateController(e.Button);

        if (e.Button is SButton.LeftShoulder or SButton.RightShoulder)
        {
            PreparePageTurn();
            return false;
        }

        if (PageEditor.Direction(e.Button) >= 0)
        {
            _editor.Blur();
            _controllerCaretMode = false;
            _autoFocusPage = false;
            return false;
        }

        if (e.Button == SButton.Tab && _isBook && !IsReadOnly)
        {
            FocusPage(_firstVisiblePage + (_editor.Page == _firstVisiblePage ? 1 : 0));
            return true;
        }

        if (!IsReadOnly && e.Button == SButton.ControllerX)
        {
            // Keyboard dismissal may snap the cursor to a toolbar; keep editing the last visible leaf.
            bool editedPageIsVisible = _editor.Page >= _firstVisiblePage
                && _editor.Page <= _firstVisiblePage + (_isBook ? 1 : 0);
            EnterControllerCaret(editedPageIsVisible ? _editor.Page : _firstVisiblePage);
            return _editor.ControllerButton(e.Button);
        }

        if (_editor.HasFocus && _editor.ControllerButton(e.Button))
            return true;

        if (_editor.HasFocus && e.Button.TryGetKeyboard(out Keys key))
        {
            _editor.KeyPressed(key);
            return true;
        }

        return false;
    }

    /// <summary>Tracks the last input device and cancels controller navigation when mouse or keyboard takes over.</summary>
    private void UpdateInputMode(SButton button)
    {
        if (button.TryGetController(out _))
        {
            _usingController = true;
            return;
        }

        if (button != SButton.MouseLeft && !button.TryGetKeyboard(out _))
            return;

        _usingController = false;
        _controllerCaretMode = false;
        _editor.CancelControllerNavigation();
    }

    /// <summary>Finds the visible leaf under a UI-space cursor position.</summary>
    private bool TryGetPageAt(Point point, out int page)
    {
        for (page = _firstVisiblePage; page <= _firstVisiblePage + (_isBook ? 1 : 0); page++)
        {
            if (TryGetWritingBounds(page, out Rectangle bounds) && bounds.Contains(point))
                return true;
        }

        return false;
    }

    /// <summary>Starts caret placement or selection, except where Parchment needs the click to turn a page.</summary>
    private bool HandlePageClick(Point click)
    {
        if (IsTurnCorner(click))
        {
            _editor.Suspend();
            _autoFocusPage = ShouldAutoFocus;
            return false;
        }

        if (!TryGetPageAt(click, out int page))
            return false;

        FocusPage(page, click, drag: true);
        return true;
    }

    /// <summary>Enters caret mode on the first controller confirmation and opens typing on the next.</summary>
    private bool HandleControllerConfirm(Point cursor)
    {
        if (_controllerCaretMode)
        {
            if (TryGetWritingBounds(_editor.Page, out Rectangle bounds))
                _editor.Resume(bounds);

            _editor.OpenKeyboard();
            return true;
        }

        if (IsTurnCorner(cursor) || !TryGetPageAt(cursor, out int page))
            return false;

        EnterControllerCaret(page);
        return true;
    }

    /// <summary>Tests the small-book sprite's active page-turn corners at the current UI scale.</summary>
    private bool IsTurnCorner(Point point)
    {
        if (!_isBook || !_parchment.TryGetBookBounds(out Rectangle bookBounds))
            return false;

        float scale = bookBounds.Width / 219f;
        Rectangle previousCorner = GetTurnCornerBounds(bookBounds, scale, 1, 113);
        Rectangle nextCorner = GetTurnCornerBounds(bookBounds, scale, 186, 112);
        return (_firstVisiblePage > 0 && previousCorner.Contains(point))
            || (_firstVisiblePage + 2 < VisiblePageCount && nextCorner.Contains(point));
    }

    /// <summary>Converts one 32-pixel corner from sprite coordinates into UI coordinates.</summary>
    private static Rectangle GetTurnCornerBounds(Rectangle bookBounds, float scale, int x, int y)
        => new(bookBounds.X + (int)(x * scale), bookBounds.Y + (int)(y * scale),
            (int)(32 * scale), (int)(32 * scale));
}
