using StardewValley.Menus;

namespace BooksAndNotes.Integrations;

/// <summary>The StardewUI 0.6.1 API subset used for bookmark assets and menus through SMAPI.</summary>
public interface IViewEngine
{
    /// <summary>Registers the asset prefix and folder containing this mod's StarML views.</summary>
    void RegisterViews(string assetPrefix, string modDirectory);
    /// <summary>Registers the sprite definitions used by those views.</summary>
    void RegisterSprites(string assetPrefix, string modDirectory);
    /// <summary>Creates a menu from a registered view and its bound model.</summary>
    IMenuController CreateMenuControllerFromAsset(string assetName, object? context = null);
}

/// <summary>Controls the bookmark menu's lifetime and screen-edge spacing.</summary>
public interface IMenuController : IDisposable
{
    event Action Closed;
    IClickableMenu Menu { get; }
    /// <summary>Closes the menu and lets its Closed event arrange the return to the reader.</summary>
    void Close();
    /// <summary>Sets the minimum space between the menu and the screen edges.</summary>
    void SetGutters(int left, int top, int right = -1, int bottom = -1);
}
