using System.ComponentModel;

namespace BooksAndNotes;

/// <summary>Holds the bookmark form's draft values and validation; the session handles saving and menu changes.</summary>
internal sealed class BookmarksMenuModel : INotifyPropertyChanged
{
    private readonly Bookmarks _bookmarks;
    private readonly Func<string, string> _translate;
    private readonly Action<int, string, int> _saveBookmark;
    private readonly Action<int> _removeBookmark;
    private readonly Action _closeMenu;
    private readonly int _selectedPage;
    private readonly bool _editingExistingBookmark;
    private string _draftName = "";
    private int _draftColor = -1;

    public event PropertyChangedEventHandler? PropertyChanged;

    // The view in assets/views/Bookmarks.sml binds to these public member names.
    public int MaxNameLength => Bookmarks.MaxLabelLength;
    public string PageCaption => $"{_translate("tab.page")} {_selectedPage + 1}";
    public bool CanEdit { get; }
    public string SaveText => _translate(_editingExistingBookmark ? "tab.save" : "tab.add");
    public bool LimitReached => CanEdit && !_bookmarks.CanSet(_selectedPage);
    public bool CanSave => CanEdit && _bookmarks.CanSet(_selectedPage) && Bookmarks.Valid(_selectedPage, Name, SelectedColor);
    public bool CanRemove => CanEdit && _editingExistingBookmark;
    public IReadOnlyList<BookmarkColorChoice> Colors { get; }

    public int SelectedColor
    {
        get => _draftColor;
        set
        {
            if (_draftColor == value)
                return;

            _draftColor = value;
            foreach (BookmarkColorChoice choice in Colors)
                choice.Selected = choice.Index == value;

            Notify(nameof(SelectedColor));
            Notify(nameof(CanSave));
        }
    }

    public string Name
    {
        get => _draftName;
        set
        {
            if (_draftName == value)
                return;

            _draftName = value;
            Notify(nameof(Name));
            Notify(nameof(CanSave));
        }
    }

    /// <summary>Starts an existing bookmark edit or an empty draft for the current spread's first page.</summary>
    public BookmarksMenuModel(Bookmarks bookmarks, int currentPage, bool readOnly, Func<string, string> translate,
        Action<int, string, int> save, Action<int> remove, Action close)
    {
        _bookmarks = bookmarks;
        _translate = translate;
        _selectedPage = currentPage;
        Bookmark? existingBookmark = bookmarks.Tabs.FirstOrDefault(bookmark => bookmark.Page == currentPage);
        _editingExistingBookmark = existingBookmark is not null;
        CanEdit = !readOnly;
        _saveBookmark = save;
        _removeBookmark = remove;
        _closeMenu = close;
        Colors = BookmarkColors.Choices.Select((color, index) => new BookmarkColorChoice(
            index, translate(color.Key), color.Tint, selected => SelectedColor = selected)).ToArray();
        Name = existingBookmark?.Label ?? "";
        SelectedColor = existingBookmark?.Color ?? 0;
    }

    /// <summary>Submits the draft only when its values and the bookmark limit allow saving.</summary>
    public void Save()
    {
        if (CanSave)
            _saveBookmark(_selectedPage, Name, SelectedColor);
    }

    /// <summary>Requests removal only when the form is editing a removable existing bookmark.</summary>
    public void Remove()
    {
        if (CanRemove)
            _removeBookmark(_selectedPage);
    }

    /// <summary>Closes the form without applying draft changes.</summary>
    public void Close() => _closeMenu();

    /// <summary>Notifies StardewUI that a bound value or validation result changed.</summary>
    private void Notify(string property) => PropertyChanged?.Invoke(this, new(property));
}

/// <summary>One clickable ribbon-color preview and its observable selection state.</summary>
internal sealed record BookmarkColorChoice(int Index, string Name, string Tint, Action<int> Select) : INotifyPropertyChanged
{
    private bool _selected;
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected == value)
                return;

            _selected = value;
            PropertyChanged?.Invoke(this, new(nameof(Selected)));
        }
    }

    /// <summary>Requests selection of this palette entry in the parent form.</summary>
    public void Choose() => Select(Index);
}
