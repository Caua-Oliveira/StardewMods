using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace OnlyWhenItCounts;

/// <summary>Read-only checks for useful targets. Never invoke tool actions to test them.</summary>
internal static class DidWork
{
    internal static bool OnTile(Tool tool, GameLocation location, Vector2 tile)
    {
        if (tool is WateringCan)
            return WateringCan(location, tile);

        if (Detects.Monster(location, tile) || Detects.BushFruits(location, tile))
            return true;

        // Prefer Item Extensions' declared tool over vanilla object heuristics.
        if (Detects.RequiredModdedTool(location, tile, out string? requiredTool))
            return Detects.ToolMatches(tool, requiredTool);

        if (tool is Pickaxe)
        {
            if (Detects.Boulders(location, tile))
                return true;
            if (Detects.Grass(location, tile))
                return false;
            return Detects.Crops(location, tile)
                || Detects.MossOrGrowingTree(location, tile)
                || Detects.Weeds(location, tile)
                || Detects.Objects(location, tile, tool)
                || Detects.HoeDirt(location, tile)
                || location.isPath(tile)
                || Detects.UnknownTerrain(location, tile);
        }

        if (tool is Axe)
        {
            if (Detects.StumpOrLog(location, tile) || Detects.GiantCrops(location, tile))
                return true;
            if (Detects.Grass(location, tile))
                return false;
            return Detects.Crops(location, tile)
                || Detects.Trees(location, tile)
                || Detects.Weeds(location, tile)
                || Detects.Objects(location, tile, tool)
                || location.isPath(tile)
                || Detects.UnknownTerrain(location, tile);
        }

        if (tool is Hoe)
        {
            if (Detects.Grass(location, tile) || Detects.GiantCrops(location, tile))
                return false;
            if (Detects.MossOrGrowingTree(location, tile))
                return true;
            if (Detects.Trees(location, tile) || location.isPath(tile) || Detects.HoeDirt(location, tile))
                return false;
            if (Detects.Weeds(location, tile) || Detects.HoeSpots(location, tile)
                || Detects.BigCraftables(location, tile) || Detects.UnknownTerrain(location, tile))
                return true;
            if (location.getObjectAtTile((int)tile.X, (int)tile.Y) != null)
                return false;

            return location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Diggable", "Back") != null
                && location.isTilePassable(new xTile.Dimensions.Location((int)tile.X, (int)tile.Y), Game1.viewport);
        }

        return true; // unknown tools retain their normal cost
    }

    private static bool WateringCan(GameLocation location, Vector2 tile)
    {
        if (location.terrainFeatures.TryGetValue(tile, out var feature) && feature is HoeDirt dirt)
            return !dirt.isWatered();

        if (location.getObjectAtTile((int)tile.X, (int)tile.Y) is IndoorPot pot)
            return !pot.hoeDirt.Value.isWatered();

        return Detects.WaterPlaces(location, tile) || Detects.UnknownTerrain(location, tile);
    }
}
