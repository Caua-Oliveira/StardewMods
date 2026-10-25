using Microsoft.Xna.Framework;

namespace BooksAndNotes.Integrations;

/// <summary>The Parchment API members proxied by SMAPI; their signatures must match the framework.</summary>
public interface IParchmentApi
{
    /// <summary>Starts a native book definition with a stable presentation ID.</summary>
    IBookBuilder CreateBook(string bookId);
    /// <summary>Gets the current reader's outer bounds in UI coordinates.</summary>
    bool TryGetBookBounds(out Rectangle bounds);
    /// <summary>Gets the current left page's content bounds.</summary>
    bool TryGetLeftPageBounds(out Rectangle bounds);
    /// <summary>Gets the current right page's content bounds when one exists.</summary>
    bool TryGetRightPageBounds(out Rectangle bounds);
}

/// <summary>Builds and opens native Parchment readers without referencing its assembly at compile time.</summary>
public interface IBookBuilder
{
    /// <summary>Sets a named book appearance or layout field.</summary>
    IBookBuilder Set(string field, object? value);
    /// <summary>Adds a page with an ID used for navigation and visibility conditions.</summary>
    IPageBuilder AddPage(string pageId);
    /// <summary>Adds a visual element behind the book artwork.</summary>
    IElementBuilder AddUnderlay(string elementType);
    /// <summary>Adds a visual element above the book artwork.</summary>
    IElementBuilder AddOverlay(string elementType);
    /// <summary>Defines a native book-level key binding.</summary>
    IKeybindBuilder OnKeyPress(string keybind);
    /// <summary>Opens the definition as the active reader, returning a framework error on failure.</summary>
    bool TryOpen(out string error);
    /// <summary>Refreshes an existing reader while preserving Parchment's input state.</summary>
    bool TryRefresh(out string error);
}

/// <summary>Builds the native elements and view callbacks on one Parchment page.</summary>
public interface IPageBuilder
{
    /// <summary>Registers an action invoked when the page becomes visible.</summary>
    IPageBuilder OnView(string action);
    /// <summary>Adds a heading to the page's stacked content.</summary>
    IElementBuilder AddHeading(string text);
    /// <summary>Adds a paragraph to the page's stacked content.</summary>
    IElementBuilder AddParagraph(string text);
    /// <summary>Adds a native button that runs a trigger action.</summary>
    IElementBuilder AddButton(string text, string action);
    /// <summary>Adds an element on the page's foreground layer.</summary>
    IElementBuilder AddForeground(string elementType);
    /// <summary>Adds a named native element type to the page.</summary>
    IElementBuilder Add(string elementType);
    /// <summary>Returns the page's remaining content height after layout margins.</summary>
    float GetAvailableHeight();
}

/// <summary>Configures an individual native Parchment element.</summary>
public interface IElementBuilder
{
    /// <summary>Sets one named element property and continues the builder chain.</summary>
    IElementBuilder Set(string field, object? value);
}

/// <summary>Connects a native reader key binding to a trigger action.</summary>
public interface IKeybindBuilder
{
    /// <summary>Sets the trigger action executed by the binding.</summary>
    IKeybindBuilder Action(string action);
}
