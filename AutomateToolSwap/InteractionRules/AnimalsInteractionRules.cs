using Core;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Tools;

namespace InteractionRules;

internal static class AnimalsInteractionRules
{
    public static SwapDecision Evaluate(SwapContext context)
    {
        // Use animal data, not substrings in the breed's name (supports modded breeds too).
        var animals = context.Location.getAllFarmAnimals()
            .Where(animal => animal.currentLocation == context.Location && animal.isAdult()
                && animal.currentProduce.Value != null && Vector2.Distance(context.Tile, animal.Tile) <= 1)
            .OrderBy(animal => Vector2.DistanceSquared(context.Tile, animal.Tile));
        foreach (var animal in animals)
        {
            string? tool = animal.GetAnimalData()?.HarvestTool;
            if (tool == "Milk Pail" && context.Config.MilkPailForCowsAndGoats)
                return SwapDecision.Tool<MilkPail>();
            if (tool == "Shears" && context.Config.ShearsForSheeps)
                return SwapDecision.Tool<Shears>();
        }
        if (context.Location is AnimalHouse && context.Location.doesTileHaveProperty(
            (int)context.Tile.X, (int)context.Tile.Y, "Trough", "Back") != null)
            return context.Config.HayForFeedingBench ? SwapDecision.Item(item => item.QualifiedItemId == "(O)178") : SwapDecision.Keep;
        return SwapDecision.None;
    }
}
