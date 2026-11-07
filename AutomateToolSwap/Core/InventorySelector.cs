using StardewValley;

namespace Core;

internal static class InventorySelector
{
    /// <summary>Prefer the held matching item, then inventory order. Fallbacks are a separate tier.</summary>
    public static int? Find(Farmer player, SwapDecision decision)
        => Find(player.Items, player.CurrentToolIndex, decision);

    internal static int? Find(IList<Item> items, int currentSlot, SwapDecision decision)
    {
        if (decision.Kind != DecisionKind.Select || decision.Matches == null)
            return null;

        return Find(items, currentSlot, decision.Matches) ?? (decision.Fallback == null ? null : Find(items, currentSlot, decision.Fallback));
    }

    private static int? Find(IList<Item> items, int currentSlot, Func<Item, bool> matches)
    {
        if (currentSlot >= 0 && currentSlot < items.Count && items[currentSlot] is { Stack: > 0 } held && matches(held))
            return currentSlot;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is { } item && item.Stack > 0 && matches(item))
                return i;
        }
        return null;
    }
}
