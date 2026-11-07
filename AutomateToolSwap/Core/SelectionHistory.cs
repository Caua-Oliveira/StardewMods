namespace Core;

/// <summary>Reference-based selection bookkeeping, independent of the game and input APIs.</summary>
internal sealed class SelectionHistory<T> where T : class
{
    private T? previousItem;
    private int previousSlot = -1;
    private T? selectedItem;
    private int selectedSlot = -1;
    private TimeSpan elapsed;

    public bool HasPendingReturn { get; private set; }
    public T? SelectedItem => selectedItem;

    public void Record(int oldSlot, T? oldItem, int newSlot, T? newItem, bool autoReturn)
    {
        // A chained automatic swap must retain the original selection, not bounce between tools.
        if (!HasPendingReturn || !autoReturn)
        {
            previousSlot = oldSlot;
            previousItem = oldItem;
        }
        selectedSlot = newSlot;
        selectedItem = newItem;
        elapsed = TimeSpan.Zero;
        HasPendingReturn = autoReturn;
    }

    public void CancelReturn() => HasPendingReturn = false;

    public void Observe(int slot, T? item, bool selectedWasConsumed = false)
    {
        if (slot != selectedSlot || !ReferenceEquals(item, selectedItem) && !(item == null && selectedWasConsumed))
            CancelReturn();
    }

    public bool Ready(TimeSpan delta, bool busy, bool inputHeld, bool stillNeeded)
    {
        if (!HasPendingReturn)
            return false;
        elapsed += delta;
        return elapsed >= TimeSpan.FromMilliseconds(250) && !busy && !inputHeld && !stillNeeded;
    }

    public int? FindPrevious(IReadOnlyList<T?> items)
    {
        if (previousSlot < 0)
            return null;
        if (previousItem == null)
            return previousSlot < items.Count && items[previousSlot] == null ? previousSlot : null;
        for (int i = 0; i < items.Count; i++)
        {
            if (ReferenceEquals(items[i], previousItem))
                return i;
        }
        return null;
    }
}
