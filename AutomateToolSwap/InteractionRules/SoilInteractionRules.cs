using AutomateToolSwap;
using Core;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Tools;

namespace InteractionRules;

/// <summary>
/// Implements the swap rules for monsters.
/// </summary>
public sealed class SoilInteractionRules
{
    /// <summary>
    /// Checks Soil-related conditions and tries to swap to the correct tool/item appropriately.
    /// </summary>
    /// <param name="location">The current game location.</param>
    /// <param name="tile">The tile being checked.</param>
    /// <param name="player">The current player.</param>
    /// <returns>True if a swap was performed; otherwise, false.</returns>
    public static bool TrySwap(GameLocation location, Vector2 tile, Farmer player)
    {
        if (!ModEntry.Config.HoeForDiggableSoil)
            return false;

        // Check if player is on a tractor
        if (ModEntry.isTractorModInstalled &&
            player.isRidingHorse() &&
            player.mount.Name.ToLower().Contains("tractor"))
            return true;


        bool isDiggable = location.doesTileHaveProperty(
            (int)tile.X,
            (int)tile.Y,
            "Diggable",
            "Back") != null;


        bool isInFightingLocation = location is Mine or MineShaft or VolcanoDungeon;

        bool isWeaponEquipped = player.CurrentItem is MeleeWeapon && player.CurrentItem?.category.Value == -98;
        bool isNightWithMonsters = Game1.spawnMonstersAtNight;

        // Check for tool types that shouldn't be swapped
        bool isPriorityToolEquiped = player.CurrentItem is FishingRod or GenericTool or Wand;

        // Determine if we should skip swapping
        if (!isDiggable ||
            isInFightingLocation ||
            isWeaponEquipped && isNightWithMonsters ||
            isPriorityToolEquiped)
        {
            return false;
        }

        bool isPathTile = location.isPath(tile);
        bool isPlaceableItem = player.CurrentItem != null && player.CurrentItem.isPlaceable();

        // Handle path tiles - switch to pickaxe if configured
        if (isPathTile)
        {
            if (ModEntry.Config.PickaxeForFloorTile &&
                player.CurrentTool is not Pickaxe &&
                !isPlaceableItem)
            {
                InventoryHandler.SetTool(player, typeof(Pickaxe));
            }
            return true;
        }

        // Handle diggable soil - switch to hoe
        if (player.CurrentItem == null ||
            !isPlaceableItem && player.CurrentItem is not Hoe)
        {
            InventoryHandler.SetTool(player, typeof(Hoe));
            return true;
        }
        return false;
    }
}
