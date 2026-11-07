using StardewValley;

namespace Core;

internal sealed class ScreenState
{
    public Farmer? Player { get; private set; }
    private GameLocation? location;
    public SelectionHistory<Item> History { get; private set; } = new();

    public void Observe(Farmer player)
    {
        if (!ReferenceEquals(Player, player) || !ReferenceEquals(location, player.currentLocation))
        {
            Player = player;
            location = player.currentLocation;
            History = new();
        }
        History.Observe(player.CurrentToolIndex, player.CurrentItem, History.SelectedItem is { Stack: <= 0 });
    }
}
