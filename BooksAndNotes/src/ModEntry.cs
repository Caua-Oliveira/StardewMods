using BooksAndNotes.Integrations;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Menus;
using StardewValley.Triggers;

namespace BooksAndNotes;

/// <summary>Registers the mod and routes game events to the document session for each local screen.</summary>
public sealed class ModEntry : Mod
{
    public const string BookId = WritingItemIds.Book;
    public const string NoteId = WritingItemIds.Note;
    public const string DataKey = "Trapyy.BooksAndNotes/Document";
    public const string TitleKey = "Trapyy.BooksAndNotes/Title";
    private const string ParchmentModId = "PeacefulEnd.Parchment.Core";

    internal static ITranslationHelper I18n { get; private set; } = null!;

    private IParchmentApi? _parchment;
    private IViewEngine? _viewEngine;
    private Harmony _harmony = null!;
    private readonly PerScreen<DocumentSession?> _sessions = new();
    private readonly PerScreen<Action?> _pendingActions = new();

    /// <summary>Installs item patches, registers the reader action, and subscribes to SMAPI events.</summary>
    public override void Entry(IModHelper helper)
    {
        I18n = helper.Translation;
        _harmony = new Harmony(ModManifest.UniqueID);
        ItemPatches.Apply(_harmony);
        TriggerActionManager.RegisterAction(DocumentSession.ActionName, HandleDocumentAction);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
        helper.Events.Display.WindowResized += OnWindowResized;
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
    }

    /// <summary>Connects the required UI frameworks and installs the optional animated-writing hooks.</summary>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        _parchment = Helper.ModRegistry.GetApi<IParchmentApi>(ParchmentModId);
        _viewEngine = Helper.ModRegistry.GetApi<IViewEngine>("focustense.StardewUI");
        _viewEngine?.RegisterViews("Mods/Trapyy.BooksAndNotes/Views", "views");
        _viewEngine?.RegisterSprites("Mods/Trapyy.BooksAndNotes/Sprites", "sprites");

        if (_viewEngine is null)
            Monitor.Log("StardewUI 0.6.1 or newer is required for the bookmarks menu.", LogLevel.Error);

        if (_parchment is null)
        {
            Monitor.Log("Parchment's API is unavailable. Install Parchment 1.15.1 or newer; writing items will not open until it is available.", LogLevel.Error);
            return;
        }

        ParchmentTurnPatches.Install(_harmony, Monitor,
            menu => _sessions.Value?.CanAnimate(menu) == true,
            (menu, batch, turn) => _sessions.Value?.DrawTurn(menu, batch, turn) == true,
            (menu, batch, close) => _sessions.Value?.DrawClosing(menu, batch, close) == true);
    }

    /// <summary>Passes a Parchment button action to the current screen's document session.</summary>
    private bool HandleDocumentAction(string[] args, TriggerActionContext context, out string error)
    {
        error = "No Books and Notes document is open.";
        return _sessions.Value?.Handle(args, out error) ?? false;
    }

    /// <summary>Runs one deferred menu action, cleans up replaced readers, and updates the active session.</summary>
    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        Action? pendingAction = _pendingActions.Value;
        if (pendingAction is null && _sessions.Value is null)
            return;

        _pendingActions.Value = null;
        try
        {
            pendingAction?.Invoke();
            if (_sessions.Value is { } session && _pendingActions.Value is null && !session.Owns(Game1.activeClickableMenu))
            {
                // A deferred handoff may decline to replace an unrelated menu; finish cleanup here.
                session.Finish();
                _sessions.Value = null;
            }

            _sessions.Value?.Update();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    /// <summary>Commits the current page to item metadata before Stardew saves the day.</summary>
    private void OnSaving(object? sender, SavingEventArgs e) => _sessions.Value?.Flush();

    /// <summary>Finishes sessions on every local screen and clears callbacks when leaving the save.</summary>
    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        foreach (var screenSession in _sessions.GetActiveValues())
            screenSession.Value?.Finish();

        _sessions.ResetAllScreens();
        _pendingActions.ResetAllScreens();
    }

    /// <summary>Draws this session's writing over the framework's already-rendered reader.</summary>
    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e) => _sessions.Value?.Draw(e.SpriteBatch);

    /// <summary>Asks the open document to recalculate its presentation after a window resize.</summary>
    private void OnWindowResized(object? sender, WindowResizedEventArgs e) => _sessions.Value?.Resize();

    /// <summary>Ends a session when another menu replaces it, unless an internal handoff is queued.</summary>
    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        DocumentSession? session = _sessions.Value;
        if (session is null || session.Owns(e.NewMenu) || _pendingActions.Value is not null)
            return;

        session.Finish();
        _sessions.Value = null;
    }

    /// <summary>Recognizes this mod's books and notes, including a missing held item.</summary>
    public static bool IsWritingItem(Item? item) => WritingItemIds.IsWritingItem(item?.QualifiedItemId);

    /// <summary>Distinguishes books of any cover from the two-sided note.</summary>
    public static bool IsBook(Item item) => WritingItemIds.IsBook(item.QualifiedItemId);

    /// <summary>Gives existing readers and furniture priority before opening a carried or inventory document.</summary>
    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || e.IsSuppressed())
            return;

        if (_sessions.Value?.ButtonPressed(e) == true)
        {
            Helper.Input.Suppress(e.Button);
            return;
        }

        if (Game1.activeClickableMenu is GameMenu gameMenu && gameMenu.GetCurrentPage() is InventoryPage inventoryPage)
        {
            OpenInventoryDocument(gameMenu, inventoryPage, e);
            return;
        }

        if (!Context.IsPlayerFree)
            return;

        if (FurnitureDocumentInteraction.Handle(e, Helper.Input,
            (document, controller) => OpenDocument(document, null, controller, readOnly: true)))
        {
            return;
        }

        if (!e.Button.IsActionButton())
            return;

        Item heldItem = Game1.player.CurrentItem;
        if (!IsWritingItem(heldItem))
            return;

        if (DocumentPlacement.ShouldDefer(heldItem, e))
            return; // Leave this input unsuppressed so the game performs the placement.

        Helper.Input.Suppress(e.Button);
        OpenDocument(heldItem, null, e.Button.TryGetController(out _));
    }

    /// <summary>Opens a document under the inventory cursor and remembers the inventory as its return menu.</summary>
    private void OpenInventoryDocument(GameMenu gameMenu, InventoryPage inventoryPage, ButtonPressedEventArgs e)
    {
        if (e.Button is not (SButton.MouseRight or SButton.ControllerX) || Game1.player.CursorSlotItem is not null)
            return;

        var cursorPosition = UiCoordinates.Cursor(e.Cursor);
        Item? item = inventoryPage.inventory.getItemAt(cursorPosition.X, cursorPosition.Y);
        if (!IsWritingItem(item))
            return;

        Helper.Input.Suppress(e.Button);
        OpenDocument(item!, gameMenu, e.Button == SButton.ControllerX);
    }

    /// <summary>Loads an item's writing and opens a session, reporting invalid data without overwriting it.</summary>
    private void OpenDocument(Item item, IClickableMenu? returnMenu, bool controller, bool readOnly = false)
    {
        try
        {
            if (_parchment is null || _viewEngine is null)
                throw new InvalidOperationException("Parchment 1.15.1 and StardewUI 0.6.1 or newer are required.");

            Document document = item.modData.TryGetValue(DataKey, out string json)
                ? Document.Deserialize(json, IsBook(item))
                : new Document();
            var session = new DocumentSession(_parchment, _viewEngine, item, document, returnMenu,
                action => _pendingActions.Value = action,
                key => Helper.Translation.Get(key).ToString(), Helper.Input, Monitor, controller, readOnly);

            _sessions.Value = session;
            session.Open();
        }
        catch (Exception exception)
        {
            ReportError(exception);
        }
    }

    /// <summary>Shows a translated in-game error and logs the exception details for troubleshooting.</summary>
    private void ReportError(Exception exception)
    {
        string message = Helper.Translation.Get("error.open").ToString();
        Monitor.Log($"{message}\n{exception}", LogLevel.Error);
        Game1.addHUDMessage(new HUDMessage(message, HUDMessage.error_type));
    }
}
