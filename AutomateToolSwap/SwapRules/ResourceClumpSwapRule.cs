using System;
using System.Collections.Generic;
using System.Linq;
using AutomateToolSwap;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace AutomateToolSwap.SwitchRules
{
    /// <summary>
    /// Implements the switch rule for resource clumps (stumps, logs, boulders, giant crops).
    /// </summary>
    public class ResourceClumpSwapRule : ISwapRule
    {
        ModConfig config = ModEntry.Config;

        private bool IsStumpOrLog(ResourceClump resourceClump)
        {
            return new List<int> { 602, 600 }.Contains(resourceClump.parentSheetIndex.Value);
        }

        private bool IsBoulder(ResourceClump resourceClump)
        {
            return new List<int> { 758, 756, 754, 752, 672, 622, 148 }.Contains(resourceClump.parentSheetIndex.Value);
        }

        /// <summary>
        /// Checks resource clumps at the specified tile and switches tools accordingly.
        /// </summary>
        /// <param name="location">The game location.</param>
        /// <param name="tile">The tile being checked.</param>
        /// <param name="player">The player.</param>
        /// <returns>True if a switch occurred; otherwise, false.</returns>
        public bool TrySwap(GameLocation location, Vector2 tile, Farmer player)
        {
            bool currentItemIsNull = player.CurrentItem == null;
            string currentItemName = player.CurrentItem?.Name ?? "";
            bool currentItemIsNotForMine = currentItemIsNull ||
                                      (!currentItemName.Contains("Bomb") && !currentItemName.Contains("Staircase"));

            foreach (var resourceClump in location.resourceClumps)
            {
                if (resourceClump.occupiesTile((int)tile.X, (int)tile.Y))
                {
                    // Modded clumps
                    if (ModEntry.ItemExtensionsAPI != null)
                    {
                        string tool;
                        string itemId;
                        try
                        {
                            itemId = resourceClump.modDataForSerialization.FirstOrDefault().Values.First();
                        }
                        catch { itemId = ""; }

                        bool isClump = ModEntry.ItemExtensionsAPI.IsClump(itemId);
                        bool foundTool = ModEntry.ItemExtensionsAPI.GetBreakingTool(itemId, isClump, out tool);
                        if (foundTool)
                        {
                            if (tool == "Pickaxe")
                            {
                                if (config.PickaxeForBoulders && currentItemIsNotForMine)
                                    ModEntry.SetTool(player, typeof(Pickaxe));
                                return true;
                            }
                            if (tool == "Axe")
                            {
                                if (config.AxeForStumpsAndLogs)
                                    ModEntry.SetTool(player, typeof(Axe));
                                return true;
                            }
                        }
                    }

                    if (config.AxeForGiantCrops && resourceClump is GiantCrop)
                    {
                        if (currentItemIsNull || currentItemName != "Tapper")
                            ModEntry.SetTool(player, typeof(Axe));
                        return true;
                    }
                    if (config.AxeForStumpsAndLogs && IsStumpOrLog(resourceClump))
                    {
                        ModEntry.SetTool(player, typeof(Axe));
                        return true;
                    }
                    if (config.PickaxeForBoulders && IsBoulder(resourceClump))
                    {
                        ModEntry.SetTool(player, typeof(Pickaxe));
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
