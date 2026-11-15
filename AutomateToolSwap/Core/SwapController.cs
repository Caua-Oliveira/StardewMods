using Configuration;
using Integrations;
using InteractionRules;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Core;

internal sealed class SwapController
{
    private readonly IModHelper helper;
    private readonly ModIntegrations integrations;

    public SwapController(IModHelper helper, ModIntegrations integrations)
    {
        this.helper = helper;
        this.integrations = integrations;
    }

    private SwapDecision EvaluateTarget(Farmer player, ModConfig config) => TargetResolver.Evaluate(player,
        helper.Input.GetCursorPosition(), config.DetectionMethod, integrations,
        tile => ItemSwapCoordinator.Evaluate(new(player, player.currentLocation, tile, config, integrations)));

    private SwapDecision EvaluateProximityCombat(Farmer player, ModConfig config) =>
        config.AlternativeWeaponOnMonsters && config.WeaponOnMonsters
            ? MonstersInteractionRules.Evaluate(new(player, player.currentLocation, player.Tile, config, integrations))
            : SwapDecision.None;

    /// <summary>A nearby threat takes priority even on the initial click, before any target rule runs.</summary>
    internal static SwapDecision ResolveDecision(SwapDecision combat, bool evaluateTarget, Func<SwapDecision> target)
        => combat.Kind != DecisionKind.NotApplicable ? combat : evaluateTarget ? target() : SwapDecision.None;

    /// <summary>Combat locks out unrelated returns even if its requested item is missing or different.</summary>
    internal static bool ShouldDeferReturn(SwapDecision combat, int? requestedSlot, int currentSlot)
        => combat.Kind != DecisionKind.NotApplicable || requestedSlot == currentSlot;

    // The toolbar is a HUD menu, so activeClickableMenu doesn't cover it.
    private static bool IsPointerOverToolbar() => Game1.displayHUD && Game1.wasMouseVisibleThisFrame
        && Game1.onScreenMenus.OfType<Toolbar>().Any(toolbar => toolbar.isWithinBounds(Game1.getMouseX(true), Game1.getMouseY(true)));

    public void OnAction(Farmer player, ModConfig config, ScreenState state)
    {
        state.Observe(player);
        if (IsPointerOverToolbar())
        {
            state.History.CancelReturn();
            return;
        }
        if (!SwapPolicy.CanSwap(player, config))
            return;
        var decision = ResolveDecision(EvaluateProximityCombat(player, config), true, () => EvaluateTarget(player, config));
        int? slot = InventorySelector.Find(player, decision);
        if (slot.HasValue)
            SwapExecutor.Apply(player, state, slot.Value, config.AutoReturnToLastTool);
    }

    /// <summary>RequireClick gates detection on a held input, not only its initial press.</summary>
    internal static bool ShouldEvaluateTarget(bool requireClick, bool useCustomKey, bool toolHeld, bool customKeyHeld, bool tractorActive)
        => !requireClick || (useCustomKey ? customKeyHeld : toolHeld) || tractorActive;

    public void Update(Farmer player, ModConfig config, ScreenState state, TimeSpan elapsed, bool toolHeld, bool customKeyHeld)
    {
        state.Observe(player);
        // Guard held inputs and automatic modes too, or the update immediately
        // replaces the slot vanilla selected after the initial mouse press.
        if (IsPointerOverToolbar())
        {
            state.History.CancelReturn();
            return;
        }
        if (!config.Enabled || !config.AutoReturnToLastTool || Game1.activeClickableMenu != null)
            state.History.CancelReturn();

        bool safe = SwapPolicy.CanSwap(player, config);
        // A blacklist/tractor-policy change also cancels old restoration requests.
        if (player.CurrentItem is { } held && config.ItemBlacklist.Contains(held.QualifiedItemId)
            || config.DisableTractorSwap && SwapPolicy.IsTractor(player))
            state.History.CancelReturn();

        SwapDecision desired = SwapDecision.None;
        SwapDecision combat = SwapDecision.None;
        bool tractorActive = integrations.HasTractor && SwapPolicy.IsTractor(player) && !config.DisableTractorSwap;
        bool evaluateTarget = ShouldEvaluateTarget(config.RequireClick, config.UseDifferentSwapKey, toolHeld, customKeyHeld, tractorActive);
        if (safe)
        {
            combat = EvaluateProximityCombat(player, config);
            desired = ResolveDecision(combat, evaluateTarget, () => EvaluateTarget(player, config));
        }

        int? slot = InventorySelector.Find(player, desired);
        // No target-rule or auto-return selection may override active proximity combat.
        // This also covers switching between combat tools and having no suitable weapon.
        bool stillNeeded = ShouldDeferReturn(combat, slot, player.CurrentToolIndex);
        // Tool use and a configured custom key both delay restoration, but only the selected
        // swap binding enables target detection when RequireClick is enabled.
        bool inputHeld = toolHeld || config.UseDifferentSwapKey && customKeyHeld;
        if (state.History.Ready(elapsed, !safe, inputHeld, stillNeeded))
        {
            SwapExecutor.Restore(player, state);
            return;
        }
        if (safe && slot.HasValue)
            SwapExecutor.Apply(player, state, slot.Value, config.AutoReturnToLastTool);
    }

    public void ReturnToPrevious(Farmer player, ModConfig config, ScreenState state)
    {
        state.Observe(player);
        if (SwapPolicy.CanSwap(player, config))
            SwapExecutor.Restore(player, state);
    }
}
