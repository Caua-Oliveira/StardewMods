using AutomateToolSwap;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System.Collections.Generic;

namespace AutomateToolSwap.SwitchRules
{
    /// <summary>
    /// Implements the switch rule for terrain features (trees, bushes, grass, and tilled dirt).
    /// </summary>
    public class TerrainFeatureSwapRule : ISwapRule
    {
        ModConfig config = ModEntry.Config;
        /// <summary>
        /// Checks the terrain features at the given tile and swaps tools if necessary.
        /// </summary>
        /// <param name="location">The current game location.</param>
        /// <param name="tile">The tile being checked.</param>
        /// <param name="player">The current player.</param>
        /// <returns>True if a switch was performed; otherwise, false.</returns>
        public bool TrySwap(GameLocation location, Vector2 tile, Farmer player)
        {
            bool currentItemIsNull = player.CurrentItem == null;
            string currentItemName = player.CurrentItem?.Name ?? "";
            int currentItemCategory = player.CurrentItem?.category.Value ?? 0;
            // Check for large terrain features (e.g. bushes that occupies multiple tiles)
            foreach (var terrainFeature in location.largeTerrainFeatures)
            {
                if (terrainFeature is Bush bush && ModEntry.Config.ScytheForBushes)
                {
                    // Calculate bounding box of the bush and check if the clicked tile falls within it.
                    Rectangle bushBox = bush.getBoundingBox();
                    Vector2 tilePixel = new Vector2(tile.X * Game1.tileSize, tile.Y * Game1.tileSize);
                    if (bushBox.Contains((int)tilePixel.X, (int)tilePixel.Y) && bush.inBloom())
                    {
                        ModEntry.SetTool(player, typeof(MeleeWeapon), "Scythe");
                        return true;
                    }
                }
            }

            // Check normal terrain features.
            if (!location.terrainFeatures.ContainsKey(tile))
                return false;

            var feature = location.terrainFeatures[tile];

            if (feature is Tree tree)
            {
                // If the tree has moss and the config enables scythe for moss, switch tool.
                if (tree.hasMoss.Value && tree.growthStage.Value >= Tree.stageForMossGrowth && ModEntry.Config.ScytheForMossOnTrees)
                {
                    ModEntry.SetTool(player, typeof(MeleeWeapon), "Scythe");
                    return true;
                }
                // If the player is holding a tapper or fertilizer tool, do not change.
                if (!ModEntry.Config.AxeForTrees || (!currentItemIsNull && 
                   (currentItemName == "Tapper" || currentItemName == "Tree Fertilizer")))
                    return false;
                // If the tree is grown (or config does not ignore growing trees), switch to axe.
                if (!(tree.growthStage.Value < Tree.treeStage && ModEntry.Config.IgnoreGrowingTrees))
                {
                    ModEntry.SetTool(player, typeof(Axe));
                    return true;
                }
                return false;
            }

            //Ignore swap to Scythe if player is holding Animal Tool so they dont accidentally cut the grass
            if (feature is Grass && !(player.CurrentTool is MilkPail || player.CurrentTool is StardewValley.Tools.Shears) && ModEntry.Config.ScytheForGrass)
            {
                ModEntry.SetTool(player, typeof(MeleeWeapon), "ScytheOnly");
                return true;
            }

            if (feature is HoeDirt dirt)
            {
                bool dirtHasCrop = dirt.crop != null;
                if (!dirtHasCrop && ModEntry.Config.SeedForTilledDirt)
                {
                    if (!(ModEntry.Config.PickaxeOverWateringCan && player.CurrentTool is Pickaxe))
                    {
                        if (currentItemIsNull || currentItemCategory != -74 || 
                            player.CurrentItem.HasContextTag("tree_seed_item"))
                        {
                            ModEntry.SetItem(player, "Seed");
                        }
                    }
                    return true;
                }
                if (dirtHasCrop && (dirt.readyForHarvest() || dirt.crop.dead.Value) && ModEntry.Config.ScytheForCrops)
                {
                    ModEntry.SetTool(player, typeof(MeleeWeapon), "ScytheOnly");
                    return true;
                }
                if (dirtHasCrop && !dirt.HasFertilizer() && dirt.CanApplyFertilizer("(O)369") && config.FertilizerForCrops)
                {
                    if (!(config.PickaxeOverWateringCan) && player.CurrentTool is not Pickaxe && currentItemCategory != -19)
                    {
                        ModEntry.SetItem(player, "Fertilizer", "Tree", aux: -19);
                    }
                    return true;
                }
                if (dirtHasCrop && dirt.crop.whichForageCrop.Value == "2" && ModEntry.Config.HoeForGingerCrop)
                {
                    ModEntry.SetTool(player, typeof(Hoe));
                    return true;
                }
                if (dirtHasCrop && !dirt.isWatered() && !dirt.readyForHarvest() && ModEntry.Config.WateringCanForUnwateredCrop &&
                    !(player.isRidingHorse() && player.mount.Name.ToLower().Contains("tractor") && player.CurrentTool is Hoe))
                {
                    if (!(config.PickaxeOverWateringCan && player.CurrentTool is Pickaxe))
                    {
                        ModEntry.SetTool(player, typeof(WateringCan));
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
