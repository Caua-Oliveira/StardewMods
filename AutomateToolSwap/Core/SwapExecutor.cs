using StardewValley;

namespace Core;

internal static class SwapExecutor
{
    public static bool Apply(Farmer player, ScreenState state, int slot, bool autoReturn)
    {
        if (slot < 0 || slot >= player.Items.Count || slot == player.CurrentToolIndex || player.Items[slot] is not { } item)
            return false;

        state.History.Record(player.CurrentToolIndex, player.CurrentItem, slot, item, autoReturn);
        player.CurrentToolIndex = slot;
        return true;
    }

    public static bool Restore(Farmer player, ScreenState state)
    {
        state.History.CancelReturn();
        int? slot = state.History.FindPrevious(player.Items.ToArray());
        if (!slot.HasValue || slot.Value == player.CurrentToolIndex)
            return false;
        int oldSlot = player.CurrentToolIndex;
        Item? oldItem = player.CurrentItem;
        player.CurrentToolIndex = slot.Value;
        state.History.Record(oldSlot, oldItem, slot.Value, player.CurrentItem, false);
        return true;
    }
}
