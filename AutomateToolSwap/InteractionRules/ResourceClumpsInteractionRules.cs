using AutomateToolSwap;
using Core;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace InteractionRules;

/// <summary>
/// Implements the swap rules for monsters.
/// </summary>
public class ResourceClumpsInteractionRules
{

    /// <summary>
    /// Checks Resource Clumps conditions and tries to swap to the correct tool/item appropriately.
    /// </summary>
    /// <param name="location">The current game location.</param>
    /// <param name="tile">The tile being checked.</param>
    /// <param name="player">The current player.</param>
    /// <returns>True if a swap was performed; otherwise, false.</returns>
    public static bool TrySwap(GameLocation location, Vector2 tile, Farmer player)
    {
        bool currentItemIsNull = player.CurrentItem == null;
        string currentItemName = player.CurrentItem?.Name ?? "";
        bool currentItemIsNotForMine = currentItemIsNull ||
                                  !currentItemName.Contains("Bomb") && !currentItemName.Contains("Staircase");

        foreach (var resourceClump in location.resourceClumps)
        {
            if (resourceClump.occupiesTile((int)tile.X, (int)tile.Y))
            {
                // Modded clumps
                if (ModEntry.ItemExtensionsAPI != null)
                {
                    string itemId = "";
                    foreach (var pair in resourceClump.modDataForSerialization.Pairs)
                    {
                        if (pair.Key.ToLower().Contains("clumpid"))
                        {
                            itemId = pair.Value;
                        }
                    }
                    bool isClump = ModEntry.ItemExtensionsAPI.IsClump(itemId);
                    bool foundToolRequired = ModEntry.ItemExtensionsAPI.GetBreakingTool(itemId, isClump, out string tool);
                    if (foundToolRequired)
                    {
                        if (tool == "Pickaxe")
                        {
                            if (ModEntry.Config.PickaxeForBoulders && currentItemIsNotForMine)
                                InventoryHandler.SetTool(player, typeof(Pickaxe));
                            return true;
                        }
                        if (tool == "Axe")
                        {
                            if (ModEntry.Config.AxeForStumpsAndLogs)
                                InventoryHandler.SetTool(player, typeof(Axe));
                            return true;
                        }
                    }
                }

                if (ModEntry.Config.AxeForGiantCrops && resourceClump is GiantCrop)
                {
                    if (currentItemIsNull || currentItemName != "Tapper")
                        InventoryHandler.SetTool(player, typeof(Axe));
                    return true;
                }
                if (ModEntry.Config.AxeForStumpsAndLogs && IsStumpOrLog(resourceClump))
                {
                    InventoryHandler.SetTool(player, typeof(Axe));
                    return true;
                }
                if (ModEntry.Config.PickaxeForBoulders && IsBoulder(resourceClump))
                {
                    InventoryHandler.SetTool(player, typeof(Pickaxe));
                    return true;
                }
            }
        }
        return false;
    }

    private static bool IsStumpOrLog(ResourceClump resourceClump)
    {
        return new List<int> { 602, 600 }.Contains(resourceClump.parentSheetIndex.Value);
    }

    private static bool IsBoulder(ResourceClump resourceClump)
    {
        return new List<int> { 758, 756, 754, 752, 672, 622, 148 }.Contains(resourceClump.parentSheetIndex.Value);
    }
}
