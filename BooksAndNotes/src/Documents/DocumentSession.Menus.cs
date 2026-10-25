using BooksAndNotes.Integrations;
using StardewValley;
using StardewValley.Menus;

namespace BooksAndNotes;

/// <summary>Bookmark and signing menu handoffs, including resize recovery, for a document session.</summary>
internal sealed partial class DocumentSession
{
    /// <summary>Commits writing and replaces the reader with the bookmark form for the current spread.</summary>
    private void OpenBookmarks()
    {
        if (!GameStateQuery.CheckConditions(ReaderReadyCondition))
            return;

        _editor.Flush();
        _editor.Blur();
        _controllerCaretMode = false;
        _autoFocusPage = false;

        IMenuController controller = null!;
        var model = new BookmarksMenuModel(_bookmarks, _firstVisiblePage, _openedReadOnly, _translate,
            (page, name, color) => DeferBookmarkAction(controller, () => SaveBookmark(page, name, color)),
            page => DeferBookmarkAction(controller, () => RemoveBookmark(page)),
            () => CloseBookmarkMenu(controller));

        // Both frameworks capture the HUD state. Close Parchment first so StardewUI
        // captures the restored HUD, not the reader's temporary hidden-HUD state.
        _readerMenu!.exitFunction = null;
        _readerMenu.exitThisMenu(playSound: false);
        controller = _viewEngine.CreateMenuControllerFromAsset("Mods/Trapyy.BooksAndNotes/Views/Bookmarks", model);
        controller.SetGutters(24, 24);
        controller.Closed += () => DeferBookmarkAction(controller, ReturnFromBookmarks, afterClose: true);
        _bookmarkMenu = controller;
        Game1.activeClickableMenu = controller.Menu;
    }

    /// <summary>Saves a valid bookmark without touching signed writing, then rebuilds the reader.</summary>
    private void SaveBookmark(int page, string name, int color)
    {
        if (_openedReadOnly || !_bookmarks.Set(page, name, color))
            return;

        _item.modData[Bookmarks.DataKey] = _bookmarks.Serialize();
        ReturnFromBookmarks();
    }

    /// <summary>Removes the current page's bookmark and returns to the reader.</summary>
    private void RemoveBookmark(int page)
    {
        if (_openedReadOnly)
            return;

        _bookmarks.Tabs.RemoveAll(bookmark => bookmark.Page == page);
        _item.modData[Bookmarks.DataKey] = _bookmarks.Serialize();
        ReturnFromBookmarks();
    }

    /// <summary>Closes only the bookmark menu that is still active for this session.</summary>
    private void CloseBookmarkMenu(IMenuController controller)
    {
        if (_bookmarkMenu == controller && Game1.activeClickableMenu == controller.Menu)
            controller.Close();
    }

    /// <summary>Queues a bookmark callback without allowing stale events to replace an unrelated menu.</summary>
    private void DeferBookmarkAction(IMenuController expectedMenu, Action action, bool afterClose = false)
    {
        if (_finished || _bookmarkMenu != expectedMenu)
            return;

        _deferAction(() =>
        {
            // Closed also fires during disposal; those callbacks must not revive an ended session.
            if (_finished || _bookmarkMenu != expectedMenu)
                return;

            bool expectedMenuIsActive = Game1.activeClickableMenu == expectedMenu.Menu
                || (afterClose && Game1.activeClickableMenu is null);
            if (expectedMenuIsActive)
                action();
        });
    }

    /// <summary>Disposes the bookmark form and opens a fresh reader on the previous spread.</summary>
    private void ReturnFromBookmarks()
    {
        IMenuController? outgoingMenu = _bookmarkMenu;
        _bookmarkMenu = null;
        outgoingMenu?.Dispose();
        Open();
    }

    /// <summary>Opens the native title/signing form; dismissing it returns to the unsigned draft.</summary>
    private void OpenSigningConfirmation()
    {
        _controllerCaretMode = false;
        _editor.Flush();
        _editor.Blur();
        _showingSigningForm = true;
        _pageToRestore = null;
        OpenBook(_bookFactory.Confirmation(_isBook, Game1.uiViewport.Width, Game1.uiViewport.Height));

        IClickableMenu expectedMenu = _readerMenu!;
        _readerMenu!.exitFunction = () => _deferAction(() =>
        {
            if (!_finished && (Game1.activeClickableMenu is null || Game1.activeClickableMenu == expectedMenu))
                Open();
        });
    }

    /// <summary>Rebuilds the reader at the new size, or refreshes signing without losing its title input.</summary>
    public void Resize()
    {
        _renderer.Clear();
        if (Game1.activeClickableMenu != _readerMenu)
            return;

        IClickableMenu expectedMenu = _readerMenu;
        _deferAction(() =>
        {
            if (_finished || Game1.activeClickableMenu != expectedMenu)
                return;

            // Give the native keyboard time to commit its final characters before refreshing.
            if (_showingSigningForm && (Game1.textEntry is not null || !GameStateQuery.CheckConditions(ReaderReadyCondition)))
            {
                Resize();
                return;
            }

            if (_showingSigningForm)
            {
                IBookBuilder signingForm = _bookFactory.Confirmation(_isBook, Game1.uiViewport.Width, Game1.uiViewport.Height);
                if (!signingForm.TryRefresh(out string error))
                    throw new InvalidOperationException("Parchment could not resize the signing form: " + error);
            }
            else
            {
                Open();
            }
        });
    }
}
