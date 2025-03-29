using AutomateToolSwap;
using Core;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Tools;



namespace InteractionRules;

/// <summary>
/// Implements the swap rules for animals.
/// </summary>
public class AnimalsInteractionRules
{

    private static readonly string[] animalsThatCanBeMilked = { "Goat", "Cow" };
    private static readonly string[] animalsThatCanBeSheared = { "Sheep" };

    /// <summary>
    /// Checks animal-related conditions and tries to swap to the correct tool/item appropriately.
    /// </summary>
    /// <param name="location">The current game location.</param>
    /// <param name="tile">The tile being checked.</param>
    /// <param name="player">The current player.</param>
    /// <returns>True if a swap was performed; otherwise, false.</returns>
    public static bool TrySwap(GameLocation location, Vector2 tile, Farmer player)
    {
        // Only check for animals on a farm or inside an animal house.
        if (location is not (Farm or AnimalHouse))
            return false;

        foreach (FarmAnimal animal in location.getAllFarmAnimals())
        {
            float distanceToAnimal = Vector2.Distance(tile, animal.Tile);
            if (ModEntry.Config.MilkPailForCowsAndGoats &&
                animalsThatCanBeMilked.Any(animalType => animal.type.Contains(animalType)) &&
                distanceToAnimal <= 1 && animal.currentLocation == player.currentLocation)
            {
                InventoryHandler.SetTool(player, typeof(MilkPail));
                return true;
            }
            if (ModEntry.Config.ShearsForSheeps &&
                animalsThatCanBeSheared.Any(animalType => animal.type.Contains(animalType)) &&
                distanceToAnimal <= 1 && animal.currentLocation == player.currentLocation)
            {
                InventoryHandler.SetTool(player, typeof(Shears));
                return true;
            }
        }

        bool tileIsFeedingBench = location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Trough", "Back") != null;
        if (location is AnimalHouse && tileIsFeedingBench)
        {
            InventoryHandler.SetItem(player, "", "Hay", aux: 0);
            return true;
        }
        return false;
    }
}
