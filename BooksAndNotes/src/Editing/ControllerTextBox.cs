using StardewValley;
using StardewValley.Menus;
using Microsoft.Xna.Framework.Graphics;

namespace BooksAndNotes;

/// <summary>Adapts the native controller keyboard to page editing while showing a short insertion-point preview.</summary>
internal sealed class ControllerTextBox : TextBox
{
    private readonly Action<string> _insertText;
    private readonly Action _eraseText;
    private readonly Func<string> _getTextBeforeCaret;

    /// <summary>Configures the native text box to forward edits into the page buffer.</summary>
    public ControllerTextBox(Action<string> insert, Action erase, Func<string> currentText)
        : base(Game1.content.Load<Texture2D>("LooseSprites/textBox"), null, Game1.smallFont, MenuStyle.Ink)
    {
        _insertText = insert;
        _eraseText = erase;
        _getTextBeforeCaret = currentText;
        Width = Math.Min(672, Game1.uiViewport.Width - 64);
        limitWidth = true;
        RefreshPreview();
    }

    /// <summary>Forwards a single character through the shared string-insertion path.</summary>
    public override void RecieveTextInput(char character) => RecieveTextInput(character.ToString());

    /// <summary>Inserts keyboard text using the editor's page limits, then updates the preview.</summary>
    public override void RecieveTextInput(string text)
    {
        _insertText(text);
        RefreshPreview();
    }

    /// <summary>Forwards backspace; keyboard completion only dismisses typing and never signs the document.</summary>
    public override void RecieveCommandInput(char command)
    {
        if (command == '\b')
            _eraseText();

        RefreshPreview();
    }

    /// <summary>Displays up to 32 trailing UTF-16 units before the caret without starting inside a surrogate pair.</summary>
    public void RefreshPreview()
    {
        string text = _getTextBeforeCaret();
        int start = Math.Max(0, text.Length - 32);
        if (start < text.Length && char.IsLowSurrogate(text[start]))
            start++;
        Text = text[start..].Replace('\n', ' ');
    }
}
