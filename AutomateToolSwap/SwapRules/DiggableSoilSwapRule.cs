using AutomateToolSwap;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Tools;

namespace AutomateToolSwap.SwitchRules
{
    /// <summary>
    /// Implements the switch rule for diggable soil.
    /// This rule determines if the player should switch to a hoe (or pickaxe for floor tiles) when on diggable terrain.
    /// </summary>
    public class DiggableSoilSwapRule : ISwapRule
    {
        /// <summary>
        /// Checks the soil properties and player context to decide if a tool switch is needed.
        /// </summary>
        /// <param name="location">The current game location.</param>
        /// <param name="tile">The tile being checked.</param>
        /// <param name="player">The current player.</param>
        /// <returns>True if a switch occurred; otherwise, false.</returns>
        public bool TrySwap(GameLocation location, Vector2 tile, Farmer player)
        {
            // If the player is riding a tractor (from the TractorMod), do not switch.
            if (ModEntry.isTractorModInstalled && player.isRidingHorse() && player.mount.Name.ToLower().Contains("tractor"))
                return false;

            bool isDiggable = location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Diggable", "Back") != null;
            bool currentItemIsNotScythe = player.CurrentItem?.category.Value == -98;
            bool currentItemIsDamageWeapon = player.CurrentItem is MeleeWeapon && currentItemIsNotScythe;
            bool isInFightingLocations = location is Mine || location is MineShaft || location is VolcanoDungeon;
            bool isPlaceableItem = player.CurrentItem != null && player.CurrentItem.isPlaceable();

            if (!ModEntry.Config.HoeForDiggableSoil || !isDiggable || isInFightingLocations)
                return false;
            if (currentItemIsDamageWeapon && Game1.spawnMonstersAtNight)
                return false;
            if (player.CurrentItem is FishingRod || player.CurrentItem is GenericTool || player.CurrentItem is Wand)
                return false;

            if (location.isPath(tile))
            {
                if (ModEntry.Config.PickaxeForFloorTile && player.CurrentTool is not Pickaxe && !isPlaceableItem)
                {
                    ModEntry.SetTool(player, typeof(Pickaxe));
                }
                return true;
            }

            if (player.CurrentItem == null || !isPlaceableItem)
            {
                ModEntry.SetTool(player, typeof(Hoe));
                return true;
            }
            return false;
        }
    }
}
