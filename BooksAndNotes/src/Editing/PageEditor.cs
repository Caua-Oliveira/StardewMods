using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;

namespace BooksAndNotes;

/// <summary>Edits text directly on the reader's paper, retaining a separate buffer and undo history per page.</summary>
internal sealed class PageEditor(bool isBook, bool readOnly, IInputHelper input,
    Action<int, string> save, Func<bool> canInteract) : IKeyboardSubscriber
{
    private readonly Dictionary<int, TextBuffer> _pageBuffers = [];
    private readonly Dictionary<int, TextLineHitTest> _lineHitTests = [];
    private SpriteFont? _hitTestFont;
    private TextBuffer? _currentBuffer;
    private PageTextLayout _layout = null!;
    private ControllerTextBox? _onScreenKeyboard;
    private bool _focused;
    private bool _draggingSelection;
    private bool _active;
    private double _caretBlinkSeconds;
    private Keys _repeatingKey;
    private double _keyboardRepeatDelay;
    private int _controllerDirection = -1;
    private double _controllerRepeatDelay;
    private float? _preferredColumn;
    private int? _caretLineHint;

    public int Page { get; private set; } = -1;
    public Rectangle Bounds { get; private set; }
    public bool HasFocus => _focused;
    public bool Selected { get; set; }
    public bool HasOnScreenKeyboard => _onScreenKeyboard is not null && Game1.textEntry is not null;

    private bool CanEdit => _active && _focused && !readOnly && canInteract();
    private float FontScale => PagePresentation.Scale(Bounds, _layout, isBook);
    private bool ControlHeld => IsHeld(SButton.LeftControl) || IsHeld(SButton.RightControl)
        || IsHeld(SButton.LeftWindows) || IsHeld(SButton.RightWindows);
    private bool ShiftHeld => IsHeld(SButton.LeftShift) || IsHeld(SButton.RightShift);

    private int CaretLine
    {
        get
        {
            if (_caretLineHint is int line && line >= 0 && line < _layout.Lines.Count
                && _currentBuffer!.Caret >= _layout.Lines[line].Start && _currentBuffer.Caret <= _layout.Lines[line].End)
            {
                return line;
            }

            return TextLayout.LineAt(_layout.Lines, _currentBuffer!.Caret);
        }
    }

    /// <summary>Counts suppressed buttons as held until physical release, matching SMAPI's input lifecycle.</summary>
    private bool IsHeld(SButton button) => input.IsDown(button) || input.IsSuppressed(button);

    /// <summary>Restores a page's buffer and caret, then optionally places the caret at a mouse click.</summary>
    public void Focus(int page, string text, Rectangle bounds, Point? click = null, bool drag = false)
    {
        bool samePage = Page == page;
        if (!samePage)
            _caretLineHint = null;

        _preferredColumn = null;
        if (!_pageBuffers.TryGetValue(page, out TextBuffer? buffer))
        {
            buffer = new TextBuffer(text, Document.MaxPageLength, true);
            _pageBuffers[page] = buffer;
        }

        Page = page;
        _currentBuffer = buffer;
        Bounds = bounds;
        _layout = PagePresentation.Layout(buffer.Text, isBook);
        _lineHitTests.Clear();
        _focused = true;
        _active = true;
        _repeatingKey = Keys.None;
        CancelControllerNavigation();

        if (Game1.textEntry is null)
            Game1.keyboardDispatcher.Subscriber = this;

        if (click is Point point)
            PlaceCaret(point.X, point.Y, samePage && ShiftHeld);

        _draggingSelection = drag;
        _caretBlinkSeconds = 0;
    }

    /// <summary>Stops input and dragging during a transition without losing the page's caret or selection.</summary>
    public void Suspend()
    {
        _active = false;
        _draggingSelection = false;
        _repeatingKey = Keys.None;
        CancelControllerNavigation();
        if (Game1.keyboardDispatcher.Subscriber == this)
            Game1.keyboardDispatcher.Subscriber = null;
    }

    /// <summary>Leaves editing and dismisses any page keyboard while keeping the page buffer for later.</summary>
    public void Blur()
    {
        Suspend();
        _focused = false;
        if (_onScreenKeyboard is null)
            return;

        if (Game1.textEntry is not null)
            Game1.closeTextEntry();

        if (Game1.keyboardDispatcher.Subscriber == _onScreenKeyboard)
            Game1.keyboardDispatcher.Subscriber = null;

        _onScreenKeyboard = null;
    }

    /// <summary>Updates the writing bounds and restores keyboard input after a temporary suspension.</summary>
    public void Resume(Rectangle bounds)
    {
        Bounds = bounds;
        _active = _focused;
        if (_focused && _onScreenKeyboard is null && Game1.textEntry is null && Game1.keyboardDispatcher.Subscriber != this)
            Game1.keyboardDispatcher.Subscriber = this;
    }

    /// <summary>Passes the current editable buffer to the session's save callback.</summary>
    public void Flush()
    {
        if (!readOnly && _currentBuffer is not null)
            save(Page, _currentBuffer.Text);
    }

    /// <summary>Inserts only text that fits the page and refreshes presentation when the buffer changes.</summary>
    private void Insert(string text)
    {
        if (CanEdit && _currentBuffer!.Insert(text, candidate => TextLayout.FitsPage(candidate,
            PagePresentation.TextWidth, PagePresentation.LineCapacity(isBook), PagePresentation.Measure)))
        {
            OnTextChanged();
        }
    }

    /// <summary>Rebuilds text measurements, resets caret hints, saves the edit, and updates the keyboard preview.</summary>
    private void OnTextChanged()
    {
        _layout = PagePresentation.Layout(_currentBuffer!.Text, isBook);
        _lineHitTests.Clear();
        _caretLineHint = null;
        _preferredColumn = null;
        _caretBlinkSeconds = 0;
        Flush();
        _onScreenKeyboard?.RefreshPreview();
    }

    /// <summary>Receives a typed character, ignoring character callbacks from control-key shortcuts.</summary>
    public void RecieveTextInput(char character)
    {
        if (Selected && !ControlHeld)
            Insert(character.ToString());
    }

    /// <summary>Receives a text input batch through the same insertion rules as ordinary typing.</summary>
    public void RecieveTextInput(string text)
    {
        if (Selected)
            Insert(text);
    }

    /// <summary>Leaves special-key handling to KeyPressed, which is called by the session.</summary>
    public void RecieveSpecialInput(Keys key) { }

    /// <summary>Handles backspace and newline commands; the session reserves Tab for switching leaves.</summary>
    public void RecieveCommandInput(char command)
    {
        if (!Selected || !CanEdit)
            return;

        if (command == '\b')
            Erase();
        else if (command == '\r')
            Insert("\n");
    }

    /// <summary>Handles editor shortcuts or starts held-key navigation for an ordinary editing key.</summary>
    public void KeyPressed(Keys key)
    {
        if (!_active || !_focused)
            return;

        if (ControlHeld)
        {
            HandleShortcut(key);
            _caretBlinkSeconds = 0;
            return;
        }

        if (Navigate(key))
        {
            _repeatingKey = key;
            _keyboardRepeatDelay = 420;
        }
    }

    /// <summary>Runs select, clipboard, and undo/redo shortcuts without allowing read-only text changes.</summary>
    private void HandleShortcut(Keys key)
    {
        if (key == Keys.A)
        {
            _currentBuffer!.SelectAll();
            _caretLineHint = null;
            _preferredColumn = null;
        }
        else if (key is Keys.C or Keys.X)
        {
            Copy(cut: key == Keys.X && !readOnly);
        }
        else if (!readOnly && key == Keys.V)
        {
            Paste();
        }
        else if (!readOnly && key == Keys.Z && _currentBuffer!.Undo())
        {
            OnTextChanged();
        }
        else if (!readOnly && key == Keys.Y && _currentBuffer!.Redo())
        {
            OnTextChanged();
        }
    }

    /// <summary>Moves the caret or deletes forward, returning whether this is a repeatable editor key.</summary>
    private bool Navigate(Keys key)
    {
        int currentLine = CaretLine;
        if (key is not (Keys.Up or Keys.Down or Keys.PageUp or Keys.PageDown))
            _preferredColumn = null;

        switch (key)
        {
            case Keys.Left:
                _currentBuffer!.MoveHorizontal(-1, ShiftHeld);
                _caretLineHint = null;
                break;
            case Keys.Right:
                _currentBuffer!.MoveHorizontal(1, ShiftHeld);
                _caretLineHint = null;
                break;
            case Keys.Home:
                _currentBuffer!.MoveTo(_layout.Lines[currentLine].Start, ShiftHeld);
                _caretLineHint = currentLine;
                break;
            case Keys.End:
                _currentBuffer!.MoveTo(_layout.Lines[currentLine].End, ShiftHeld);
                _caretLineHint = currentLine;
                break;
            case Keys.Up:
                MoveVertical(-1);
                break;
            case Keys.Down:
                MoveVertical(1);
                break;
            case Keys.PageUp:
                MoveVertical(-_layout.Lines.Count);
                break;
            case Keys.PageDown:
                MoveVertical(_layout.Lines.Count);
                break;
            case Keys.Delete:
                if (!readOnly && _currentBuffer!.Delete(false))
                    OnTextChanged();
                break;
            default:
                return false;
        }

        _caretBlinkSeconds = 0;
        return true;
    }

    /// <summary>Moves between visual lines while retaining the preferred horizontal position across short lines.</summary>
    private void MoveVertical(int direction)
    {
        int currentLine = CaretLine;
        int targetLine = Math.Clamp(currentLine + direction, 0, _layout.Lines.Count - 1);
        float x = _preferredColumn ?? Measure(_currentBuffer!.Text[_layout.Lines[currentLine].Start.._currentBuffer.Caret]);
        _currentBuffer!.MoveTo(PositionAtX(_layout.Lines[targetLine], x), ShiftHeld);
        _preferredColumn = x;
        _caretLineHint = targetLine;
    }

    /// <summary>Measures text using the reader's writing font.</summary>
    private static float Measure(string text) => PagePresentation.Measure(text);

    /// <summary>Converts a horizontal text offset to a character boundary, caching each line's measurements.</summary>
    private int PositionAtX(VisualLine line, float x)
    {
        if (_hitTestFont != Game1.smallFont)
        {
            _lineHitTests.Clear();
            _hitTestFont = Game1.smallFont;
        }

        if (!_lineHitTests.TryGetValue(line.Start, out TextLineHitTest? hitTest))
        {
            hitTest = new TextLineHitTest(_currentBuffer!.Text.Substring(line.Start, line.Length), Measure);
            _lineHitTests[line.Start] = hitTest;
        }

        return line.Start + hitTest.PositionAt(x);
    }

    /// <summary>Clamps a UI-space pointer to the page and moves or extends the caret selection there.</summary>
    private void PlaceCaret(int x, int y, bool extend)
    {
        float rowHeight = PagePresentation.RowHeight(Bounds, _layout, isBook);
        int line = Math.Clamp((int)((y - Bounds.Y) / rowHeight), 0, _layout.Lines.Count - 1);
        _currentBuffer!.MoveTo(PositionAtX(_layout.Lines[line], (x - Bounds.X) / FontScale), extend);
        _caretLineHint = line;
        _preferredColumn = null;
        _caretBlinkSeconds = 0;
    }

    /// <summary>Copies the selection, deleting it for cut only after the clipboard accepts the text.</summary>
    private void Copy(bool cut)
    {
        if (!_currentBuffer!.HasSelection)
            return;

        try
        {
            string selectedText = _currentBuffer.Text[_currentBuffer.SelectionStart.._currentBuffer.SelectionEnd];
            if (DesktopClipboard.SetText(selectedText) && cut && _currentBuffer.Delete(false))
                OnTextChanged();
        }
        catch
        {
            // A clipboard failure must never turn a cut into lost writing.
        }
    }

    /// <summary>Reads the clipboard explicitly because suppressed shortcuts bypass Stardew's native paste detection.</summary>
    private void Paste()
    {
        try
        {
            string clipboardText = "";
            if (DesktopClipboard.GetText(ref clipboardText) && !string.IsNullOrEmpty(clipboardText))
                Insert(clipboardText);
        }
        catch
        {
            // Leave the buffer and selection unchanged when the clipboard is unavailable.
        }
    }

    /// <summary>Maps controller editing buttons to newline, backspace, and single-character caret movement.</summary>
    public bool ControllerButton(SButton button)
    {
        if (!CanEdit)
            return false;

        switch (button)
        {
            case SButton.ControllerX:
                Insert("\n");
                return true;
            case SButton.ControllerY:
                Erase();
                return true;
            case SButton.LeftTrigger:
                MoveControllerCaret(-1);
                return true;
            case SButton.RightTrigger:
                MoveControllerCaret(1);
                return true;
            default:
                return false;
        }
    }

    /// <summary>Moves one character with a trigger and clears the previous vertical-navigation column.</summary>
    private void MoveControllerCaret(int direction)
    {
        _currentBuffer!.MoveHorizontal(direction, false);
        _caretLineHint = null;
        _preferredColumn = null;
        _caretBlinkSeconds = 0;
    }

    /// <summary>Maps D-pad and stick buttons to clockwise direction indices, or -1 for another button.</summary>
    public static int Direction(SButton button) => button switch
    {
        SButton.DPadUp or SButton.LeftThumbstickUp => 0,
        SButton.DPadRight or SButton.LeftThumbstickRight => 1,
        SButton.DPadDown or SButton.LeftThumbstickDown => 2,
        SButton.DPadLeft or SButton.LeftThumbstickLeft => 3,
        _ => -1
    };

    /// <summary>Reuses keyboard caret movement for a controller direction.</summary>
    private static Keys DirectionKey(int direction) => direction switch
    {
        0 => Keys.Up,
        1 => Keys.Right,
        2 => Keys.Down,
        _ => Keys.Left
    };

    /// <summary>Moves once for a controller direction and starts its hold-to-repeat timer.</summary>
    public bool NavigateController(SButton button)
    {
        int direction = Direction(button);
        if (!_active || !_focused || direction < 0)
            return false;

        Navigate(DirectionKey(direction));
        _controllerDirection = direction;
        _controllerRepeatDelay = 350;
        return true;
    }

    /// <summary>Stops repeating the previous controller direction.</summary>
    public void CancelControllerNavigation() => _controllerDirection = -1;

    /// <summary>Checks both the D-pad and left stick for a held navigation direction.</summary>
    private bool IsDirectionHeld(int direction) => direction switch
    {
        0 => IsHeld(SButton.DPadUp) || IsHeld(SButton.LeftThumbstickUp),
        1 => IsHeld(SButton.DPadRight) || IsHeld(SButton.LeftThumbstickRight),
        2 => IsHeld(SButton.DPadDown) || IsHeld(SButton.LeftThumbstickDown),
        _ => IsHeld(SButton.DPadLeft) || IsHeld(SButton.LeftThumbstickLeft)
    };

    /// <summary>Deletes the selection or the character before the caret when editing is allowed.</summary>
    private void Erase()
    {
        if (CanEdit && _currentBuffer!.Delete(true))
            OnTextChanged();
    }

    /// <summary>Opens the native keyboard with a preview of the text immediately before the insertion point.</summary>
    public void OpenKeyboard()
    {
        if (!CanEdit || Game1.textEntry is not null)
            return;

        CancelControllerNavigation();
        _onScreenKeyboard = new ControllerTextBox(Insert, Erase, () => _currentBuffer!.Text[.._currentBuffer.Caret]);
        Game1.keyboardDispatcher.Subscriber = _onScreenKeyboard;
        Game1.showTextEntry(_onScreenKeyboard);
    }

    /// <summary>Advances caret blinking, restores input after the native keyboard, and updates held controls.</summary>
    public void Update(GameTime time)
    {
        if (!_active || !_focused)
            return;

        _caretBlinkSeconds += time.ElapsedGameTime.TotalSeconds;
        if (_onScreenKeyboard is not null && Game1.textEntry is null)
        {
            _onScreenKeyboard = null;
            Game1.keyboardDispatcher.Subscriber = this;
        }

        UpdateDragSelection();
        UpdateControllerRepeat(time.ElapsedGameTime.TotalMilliseconds);
        UpdateKeyboardRepeat(time.ElapsedGameTime.TotalMilliseconds);
    }

    /// <summary>Extends selection while the physical mouse button is held and stops on release.</summary>
    private void UpdateDragSelection()
    {
        if (!_draggingSelection)
            return;

        if (!IsHeld(SButton.MouseLeft))
        {
            _draggingSelection = false;
            return;
        }

        Point cursor = UiCoordinates.Cursor(input.GetCursorPosition());
        PlaceCaret(cursor.X, cursor.Y, true);
    }

    /// <summary>Repeats held controller movement with the existing horizontal and vertical timings.</summary>
    private void UpdateControllerRepeat(double elapsedMilliseconds)
    {
        if (_controllerDirection < 0 || Game1.textEntry is not null)
            return;

        if (!IsDirectionHeld(_controllerDirection))
        {
            CancelControllerNavigation();
            return;
        }

        _controllerRepeatDelay -= elapsedMilliseconds;
        if (_controllerRepeatDelay > 0)
            return;

        Navigate(DirectionKey(_controllerDirection));
        _controllerRepeatDelay = _controllerDirection is 0 or 2 ? 100 : 65;
    }

    /// <summary>Repeats the current keyboard navigation key until release or a control shortcut takes over.</summary>
    private void UpdateKeyboardRepeat(double elapsedMilliseconds)
    {
        if (_repeatingKey == Keys.None || Game1.textEntry is not null)
            return;

        if (!IsHeld(_repeatingKey.ToSButton()) || ControlHeld)
        {
            _repeatingKey = Keys.None;
            return;
        }

        _keyboardRepeatDelay -= elapsedMilliseconds;
        if (_keyboardRepeatDelay > 0)
            return;

        Navigate(_repeatingKey);
        _keyboardRepeatDelay = 45;
    }

    /// <summary>Draws only the selected ranges, for callers that draw the writing separately.</summary>
    public void DrawSelection(SpriteBatch batch)
    {
        if (!_active || !_focused || _currentBuffer is null || !_currentBuffer.HasSelection)
            return;

        float scale = FontScale;
        for (int line = 0; line < _layout.Lines.Count; line++)
            DrawLineSelection(batch, _layout.Lines[line], PagePresentation.LineOrigin(Bounds, _layout, line, isBook), scale);
    }

    /// <summary>Draws the intersection of the selection and one visual line.</summary>
    private void DrawLineSelection(SpriteBatch batch, VisualLine line, Vector2 origin, float scale)
    {
        int selectionStart = Math.Max(_currentBuffer!.SelectionStart, line.Start);
        int selectionEnd = Math.Min(_currentBuffer.SelectionEnd, line.End);
        if (selectionEnd <= selectionStart)
            return;

        float startX = Measure(_currentBuffer.Text[line.Start..selectionStart]) * scale;
        float endX = Measure(_currentBuffer.Text[line.Start..selectionEnd]) * scale;
        var highlight = new Rectangle((int)(origin.X + startX), (int)origin.Y,
            Math.Max(1, (int)(endX - startX)), Math.Max(1, (int)(Game1.smallFont.LineSpacing * scale)));
        MenuStyle.Fill(batch, highlight, MenuStyle.Selection);
    }

    /// <summary>Draws the blinking insertion caret, except while read-only or the native keyboard is open.</summary>
    private void DrawCaret(SpriteBatch batch, int line, float scale)
    {
        if (readOnly || _caretBlinkSeconds % 1 >= .55 || Game1.textEntry is not null)
            return;

        Vector2 origin = PagePresentation.LineOrigin(Bounds, _layout, line, isBook);
        float offset = Measure(_currentBuffer!.Text[_layout.Lines[line].Start.._currentBuffer.Caret]) * scale;
        var caretBounds = new Rectangle((int)(origin.X + offset), (int)origin.Y,
            Math.Max(1, (int)(2 * scale)), Math.Max(1, (int)(Game1.smallFont.LineSpacing * scale)));
        MenuStyle.Fill(batch, caretBounds, MenuStyle.Ink);
    }

    /// <summary>Draws the focused page's text, selection, and caret, with optional text/selection layers.</summary>
    public void Draw(SpriteBatch batch, bool drawText = true, bool drawSelection = true)
    {
        if (!_active || !_focused || _currentBuffer is null)
            return;

        float scale = FontScale;
        int caretLine = CaretLine;
        if (!drawText && !drawSelection)
        {
            DrawCaret(batch, caretLine, scale);
            return;
        }

        for (int lineIndex = 0; lineIndex < _layout.Lines.Count; lineIndex++)
        {
            VisualLine line = _layout.Lines[lineIndex];
            Vector2 origin = PagePresentation.LineOrigin(Bounds, _layout, lineIndex, isBook);
            if (drawSelection)
                DrawLineSelection(batch, line, origin, scale);

            if (drawText)
                MenuStyle.Label(batch, _currentBuffer.Text.Substring(line.Start, line.Length), origin, scale: scale);

            if (lineIndex == caretLine)
                DrawCaret(batch, lineIndex, scale);
        }
    }
}
