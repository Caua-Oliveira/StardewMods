using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace Core;

internal static class TargetResolver
{
    public static SwapDecision Evaluate(Farmer player, ICursorPosition cursor, string method,
        Integrations.ModIntegrations integrations, Func<Vector2, SwapDecision> evaluate)
    {
        if (method == "Player")
        {
            Vector2 toolPosition = player.GetToolLocation();
            return evaluate(new Vector2((int)Math.Floor(toolPosition.X / Game1.tileSize), (int)Math.Floor(toolPosition.Y / Game1.tileSize)));
        }
        if (method is "Cursor ONLY" or "CursorOnly")
            return evaluate(cursor.Tile);
        if (integrations.HasRangedTools)
        {
            var candidate = evaluate(cursor.Tile);
            int? slot = InventorySelector.Find(player, candidate);
            int range = integrations.GetRange(slot.HasValue ? player.Items[slot.Value] : player.CurrentItem);
            float distance = Math.Max(Math.Abs(player.Tile.X - cursor.Tile.X), Math.Abs(player.Tile.Y - cursor.Tile.Y));
            if (range == -1 || distance <= range)
                return candidate;
        }

        return evaluate(cursor.GrabTile);
    }
}
