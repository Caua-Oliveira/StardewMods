﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.TerrainFeatures;
using System.Runtime.Intrinsics.X86;
using xTile.Tiles;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace OnlyWhenItCounts;

public class Detects
{
    private static bool IsStumpOrLog(ResourceClump resourceClump)
    {
        return new List<int> { 602, 600 }.Contains(resourceClump.parentSheetIndex.Value);
    }

    private static bool IsBoulder(ResourceClump resourceClump)
    {
        return new List<int> { 758, 756, 754, 752, 672, 622, 148 }.Contains(resourceClump.parentSheetIndex.Value);
    }
    public static bool Monster(Vector2 tile)
    {
        Rectangle targetTileRectangle = new Rectangle((int)tile.X * Game1.tileSize, (int)tile.Y * Game1.tileSize, Game1.tileSize, Game1.tileSize);

        foreach (var character in Game1.currentLocation.characters)
        {
            if (character is Monster monster)
            {
                if (monster.GetBoundingBox().Intersects(targetTileRectangle))
                    return true;
            }
        }
        return false;
    }

    public static bool StumpOrLog(Vector2 tile)
    {
        foreach (var resourceClump in Game1.currentLocation.resourceClumps)
        {
            if (resourceClump.occupiesTile((int)tile.X, (int)tile.Y))
            {
                if (IsStumpOrLog(resourceClump))
                    return true;
            }
        }
        return false;
    }

    public static bool Boulders(Vector2 tile)
    {
        foreach (var resourceClump in Game1.currentLocation.resourceClumps)
        {
            if (resourceClump.occupiesTile((int)tile.X, (int)tile.Y))
            {
                if (IsBoulder(resourceClump))
                    return true;
            }
        }
        return false;
    }

    public static bool GiantCrops(Vector2 tile)
    {
        foreach (var resourceClump in Game1.currentLocation.resourceClumps)
        {
            if (resourceClump.occupiesTile((int)tile.X, (int)tile.Y))
            {
                if (resourceClump is GiantCrop)
                    return true;
            }
        }
        return false;
    }

    public static bool BushFruits(Vector2 tile)
    {
        foreach (var terrainFeature in Game1.currentLocation.largeTerrainFeatures)
        {
            if (terrainFeature is Bush bush)
            {
                Rectangle bushBox = bush.getBoundingBox();
                Vector2 tilePixel = new(tile.X * Game1.tileSize, tile.Y * Game1.tileSize);

                bool hasProduce = bush.tileSheetOffset.Value == 1;

                if (bushBox.Contains((int)tilePixel.X, (int)tilePixel.Y) && hasProduce)
                    return true;
            }
        }
        return false;
    }

    public static bool Grass(Vector2 tile)
    {
        if (Game1.currentLocation.terrainFeatures.ContainsKey(tile))
        {
            var feature = Game1.currentLocation.terrainFeatures[tile];
            if (feature is Grass)
                return true;
        }
        return false;
    }

    public static bool Trees(Vector2 tile, string aux = "")
    {
        if (Game1.currentLocation.terrainFeatures.ContainsKey(tile))
        {
            var feature = Game1.currentLocation.terrainFeatures[tile];
            if (feature is Tree tree)
            {
                if (aux == "Axe")
                    return true;

                if (tree.hasMoss.Value && tree.growthStage.Value >= Tree.stageForMossGrowth)
                    return true;

                if (tree.growthStage.Value < 3)
                    return true;
                return false;
            }
        }
        return false;
    }

    public static bool Crops(Vector2 tile)
    {
        if (Game1.currentLocation.terrainFeatures.ContainsKey(tile))
        {
            var feature = Game1.currentLocation.terrainFeatures[tile];
            //==================================
            if (feature is HoeDirt dirt)
            {
                bool dirtHasCrop = dirt.crop != null;
                if (dirtHasCrop)
                    return true;
                return false;
            }
        }
        return false;
    }
    public static bool HoeDirt(Vector2 tile)
    {
        if (Game1.currentLocation.terrainFeatures.ContainsKey(tile))
        {
            var feature = Game1.currentLocation.terrainFeatures[tile];
            //==================================
            if (feature is HoeDirt dirt)
            {
                return true;
            }
        }
        return false;
    }

    public static bool Objects(Vector2 tile, string aux)
    {
        var obj = Game1.currentLocation.getObjectAtTile((int)tile.X, (int)tile.Y);
        if (obj != null)
        {
            if (obj.IsBreakableStone() && aux == "Pickaxe")
                return true;
            if (obj.IsTwig() && aux == "Axe")
                return true;
            if (obj.IsWeeds())
                return true;
            if (obj.Name == "Mushroom Box" && aux != "Hoe")
                return false;
            if (obj.Name == "Torch")
                return true;
            if (obj.HasContextTag("is_machine"))
                return true;
            if (obj is Fence fence)
            {
                if (fence.IsValidRemovalTool(Game1.player.CurrentTool))
                    return true;
                else
                    return false;
                
            }
            if (obj.IsSprinkler() && aux != "Hoe")
                return true;
            if (obj.HasContextTag("category_big_craftable"))
                return true;

            if (aux == "Hoe")
                return true;

            return false;
        }
        return false;
    }
    public static bool WateredSoil(Vector2 tile)
    {
        if (Game1.currentLocation.terrainFeatures.ContainsKey(tile))
        {
            var feature = Game1.currentLocation.terrainFeatures[tile];
            if (feature is HoeDirt dirt)
            {
                if (dirt.isWatered())
                    return true;
            }
        }
        return false;
    }
    public static bool WaterPlaces(Vector2 tile, string aux = "")
    {
        bool IsWater(GameLocation loc, Vector2 t)
        {
            return loc.doesTileHaveProperty((int)t.X, (int)t.Y, "Water", "Back") != null;
        }
        bool IsWaterSource(GameLocation loc, Vector2 t)
        {
            return loc.doesTileHaveProperty((int)t.X, (int)t.Y, "WaterSource", "Back") != null;
        }
        bool IsPetBowlOrStable(GameLocation loc, Vector2 t)
        {
            var building = loc.getBuildingAt(t);
            return building != null && (building.GetType() == typeof(StardewValley.Buildings.PetBowl) ||
                                        building.GetType() == typeof(StardewValley.Buildings.Stable));
        }

        if (IsPetBowlOrStable(Game1.currentLocation, tile) && aux != "Rod")
            return true;
        if (IsWater(Game1.currentLocation, tile))
            return true;
        if (IsWaterSource(Game1.currentLocation, tile))
            return true;

        return false;
    }
}
