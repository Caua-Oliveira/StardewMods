using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace OnlyWhenItCounts;

internal static class Detects
{
    private static bool HasClump(GameLocation location, Vector2 tile, Func<ResourceClump, bool> predicate) =>
        location.resourceClumps.Any(clump => clump.occupiesTile((int)tile.X, (int)tile.Y) && predicate(clump));

    internal static bool Monster(GameLocation location, Vector2 tile)
    {
        Rectangle bounds = new((int)tile.X * Game1.tileSize, (int)tile.Y * Game1.tileSize, Game1.tileSize, Game1.tileSize);
        return location.characters.OfType<Monster>().Any(monster => monster.GetBoundingBox().Intersects(bounds));
    }

    internal static bool StumpOrLog(GameLocation location, Vector2 tile) =>
        HasClump(location, tile, clump => clump.parentSheetIndex.Value is 602 or 600);

    internal static bool Boulders(GameLocation location, Vector2 tile) =>
        HasClump(location, tile, clump => clump.parentSheetIndex.Value is 758 or 756 or 754 or 752 or 672 or 622 or 148);

    internal static bool GiantCrops(GameLocation location, Vector2 tile) =>
        HasClump(location, tile, clump => clump is GiantCrop);

    internal static bool BushFruits(GameLocation location, Vector2 tile) =>
        location.largeTerrainFeatures?.OfType<Bush>().Any(bush =>
            bush.tileSheetOffset.Value == 1 && bush.getBoundingBox().Contains((int)tile.X * Game1.tileSize, (int)tile.Y * Game1.tileSize)) == true;

    internal static bool Grass(GameLocation location, Vector2 tile) =>
        location.terrainFeatures.TryGetValue(tile, out var feature) && feature is Grass;

    internal static bool MossOrGrowingTree(GameLocation location, Vector2 tile) =>
        location.terrainFeatures.TryGetValue(tile, out var feature) && feature is Tree tree
        && ((tree.hasMoss.Value && tree.growthStage.Value >= Tree.stageForMossGrowth) || tree.growthStage.Value < 3);

    internal static bool Trees(GameLocation location, Vector2 tile) =>
        location.terrainFeatures.TryGetValue(tile, out var feature) && feature is Tree or FruitTree;

    internal static bool Crops(GameLocation location, Vector2 tile) =>
        location.terrainFeatures.TryGetValue(tile, out var feature) && feature is HoeDirt { crop: not null };

    internal static bool HoeSpots(GameLocation location, Vector2 tile) =>
        location.getObjectAtTile((int)tile.X, (int)tile.Y)?.Name is "Seed Spot" or "Artifact Spot";

    internal static bool HoeDirt(GameLocation location, Vector2 tile) =>
        location.terrainFeatures.TryGetValue(tile, out var feature) && feature is HoeDirt;

    internal static bool Weeds(GameLocation location, Vector2 tile) =>
        location.getObjectAtTile((int)tile.X, (int)tile.Y)?.IsWeeds() == true
        || HasClump(location, tile, clump => clump.parentSheetIndex.Value is 44 or 46);

    internal static bool BigCraftables(GameLocation location, Vector2 tile) =>
        location.getObjectAtTile((int)tile.X, (int)tile.Y)?.HasContextTag("category_big_craftable") == true
        && !location.terrainFeatures.ContainsKey(tile);

    internal static bool Objects(GameLocation location, Vector2 tile, Tool tool)
    {
        var obj = location.getObjectAtTile((int)tile.X, (int)tile.Y);
        if (obj == null)
            return false;
        if (RequiredModdedTool(location, tile, out string? requiredTool))
            return ToolMatches(tool, requiredTool);
        if (obj.IsBreakableStone())
            return tool is Pickaxe;
        if (obj.IsTwig())
            return tool is Axe;
        if (obj is Fence fence)
            return fence.IsValidRemovalTool(tool);
        if (obj.Name == "Mushroom Box" && tool is not Hoe)
            return false;

        // Objects from other mods can react to tools without using vanilla tags.
        return true;
    }

    internal static bool ToolMatches(Tool tool, string? requiredTool) => requiredTool switch
    {
        "Axe" => tool is Axe,
        "Pickaxe" => tool is Pickaxe,
        "Hoe" => tool is Hoe,
        "WateringCan" => tool is WateringCan,
        _ => false
    };

    internal static bool WaterPlaces(GameLocation location, Vector2 tile)
    {
        if (location.getBuildingAt(tile) is StardewValley.Buildings.PetBowl or StardewValley.Buildings.Stable)
            return true;
        if (location is StardewValley.Locations.Submarine && new Rectangle(8, 6, 14, 7).Contains((int)tile.X, (int)tile.Y))
            return true;

        return location.CanRefillWateringCanOnTile((int)tile.X, (int)tile.Y)
            || location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Back") != null
            || location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "WaterSource", "Back") != null;
    }

    // Don't assume that a custom terrain feature has no tool interaction.
    internal static bool UnknownTerrain(GameLocation location, Vector2 tile) =>
        location.terrainFeatures.TryGetValue(tile, out var feature)
        && (feature.GetType().Assembly != typeof(TerrainFeature).Assembly || feature is FruitTree);

    internal static bool RequiredModdedTool(GameLocation location, Vector2 tile, out string? requiredTool)
    {
        requiredTool = null;
        var api = ModEntry.ItemExtensionsAPI;
        if (api == null)
            return false;

        var obj = location.getObjectAtTile((int)tile.X, (int)tile.Y);
        if (obj != null && api.GetBreakingTool(obj.ItemId, false, out string objectTool))
        {
            requiredTool = objectTool;
            return true;
        }

        foreach (var clump in location.resourceClumps)
        {
            if (!clump.occupiesTile((int)tile.X, (int)tile.Y) || clump.modDataForSerialization == null)
                continue;
            foreach (var pair in clump.modDataForSerialization.Pairs)
            {
                if (pair.Key.Contains("clumpid", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(pair.Value) && api.IsClump(pair.Value)
                    && api.GetBreakingTool(pair.Value, true, out string clumpTool))
                {
                    requiredTool = clumpTool;
                    return true;
                }
            }
        }
        return false;
    }
}
