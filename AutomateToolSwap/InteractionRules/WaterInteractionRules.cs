using Core;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Tools;

namespace InteractionRules;

internal static class WaterInteractionRules
{
    public static SwapDecision Evaluate(SwapContext context)
    {
        var location = context.Location;
        var config = context.Config;
        int x = (int)context.Tile.X;
        int y = (int)context.Tile.Y;
        if (location.getBuildingAt(context.Tile) is PetBowl bowl)
            return config.WateringCanForPetBowl && !bowl.watered.Value ? SwapDecision.Tool<WateringCan>() : SwapDecision.Keep;

        // Zero means there is no active panning spot; don't invent one near the map origin.
        if (location.orePanPoint.Value != Point.Zero && config.PanForPanningSpots)
        {
            var rectangle = new Rectangle(location.orePanPoint.X * Game1.tileSize - Game1.tileSize,
                location.orePanPoint.Y * Game1.tileSize - Game1.tileSize, Game1.tileSize * 4, Game1.tileSize * 4);
            if (rectangle.Contains(x * Game1.tileSize, y * Game1.tileSize)
                && Vector2.Distance(context.Player.StandingPixel.ToVector2(), rectangle.Center.ToVector2()) <= Game1.tileSize * 3)
                return SwapDecision.Tool<Pan>();
        }

        bool source = location.doesTileHaveProperty(x, y, "WaterSource", "Back") != null;
        bool water = location.doesTileHaveProperty(x, y, "Water", "Back") != null;
        if (!source && !water)
            return SwapDecision.None;
        if (context.Player.CurrentItem is FishingRod or Pan)
            return SwapDecision.Keep;
        bool preferCan = location is Farm or VolcanoDungeon || location.InIslandContext() || location.isGreenhouse.Value;
        if (config.WateringCanForWater && (source || preferCan || context.Player.CurrentItem is WateringCan))
            return SwapDecision.Tool<WateringCan>();
        return config.FishingRodOnWater && water ? SwapDecision.Tool<FishingRod>() : SwapDecision.Keep;
    }
}
