using AutomateToolSwap;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Tools;

namespace AutomateToolSwap.SwitchRules
{
    /// <summary>
    /// Implements the switch rule for water-related tiles.
    /// Depending on whether the tile represents water, a water source, or a panning spot,
    /// the appropriate tool (watering can, pan, or fishing rod) is selected.
    /// </summary>
    public class WaterSwapRule : ISwapRule
    {
        ModConfig config = ModEntry.Config;
        /// <summary>
        /// Checks water-related properties at the tile and switches tools accordingly.
        /// </summary>
        /// <param name="location">The current game location.</param>
        /// <param name="tile">The tile being checked.</param>
        /// <param name="player">The current player.</param>
        /// <returns>True if a switch was performed; otherwise, false.</returns>
        public bool TrySwap(GameLocation location, Vector2 tile, Farmer player)
        {
            bool IsPetBowlOrStable(GameLocation loc, Vector2 t)
            {
                var building = loc.getBuildingAt(t);
                return building != null && (building.GetType() == typeof(StardewValley.Buildings.PetBowl) || 
                                            building.GetType() == typeof(StardewValley.Buildings.Stable));
            }
            bool IsPanSpot(GameLocation loc, Vector2 t, Farmer p)
            {
                var orePanRect = new Rectangle(loc.orePanPoint.X * 64 - 64, loc.orePanPoint.Y * 64 - 64, 256, 256);
                return orePanRect.Contains((int)t.X * 64, (int)t.Y * 64) &&
                       Utility.distance(p.StandingPixel.X, orePanRect.Center.X, p.StandingPixel.Y, orePanRect.Center.Y) <= 192f;
            }
            bool IsWater(GameLocation loc, Vector2 t, Farmer p)
            {
                return loc.doesTileHaveProperty((int)t.X, (int)t.Y, "Water", "Back") != null &&
                       !(p.CurrentTool is WateringCan || p.CurrentTool is Pan);
            }
            bool IsWaterSource(GameLocation loc, Vector2 t)
            {
                return loc.doesTileHaveProperty((int)t.X, (int)t.Y, "WaterSource", "Back") != null;
            }
            bool shouldUseWateringCan = location is Farm || location is VolcanoDungeon ||
                                        location.InIslandContext() || location.isGreenhouse.Value;


            if (IsPetBowlOrStable(location, tile) && config.WateringCanForPetBowl)
            {
                ModEntry.SetTool(player, typeof(WateringCan));
                return true;
            }
            if (IsPanSpot(location, tile, player) && config.PanForPanningSpots)
            {
                ModEntry.SetTool(player, typeof(Pan));
                return true;
            }
            if ((IsWaterSource(location, tile) || IsWater(location, tile, player)) && shouldUseWateringCan && config.WateringCanForWater)
            {
                if (player.CurrentItem is not FishingRod)
                    ModEntry.SetTool(player, typeof(WateringCan));
                return true;
            }
            if (IsWater(location, tile, player) && config.FishingRodOnWater)
            {
                if (player.CurrentItem is not WateringCan)
                    ModEntry.SetTool(player, typeof(FishingRod));
                return true;
            }
            return false;
        }
    }
}
