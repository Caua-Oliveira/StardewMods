using Microsoft.Xna.Framework;
using StardewValley;
using InteractionRules;

namespace Core;

/// <summary>
/// Aggregates all switch rules and invokes them sequentially.
/// Only the first rule that applies will trigger a tool/item switch.
/// </summary>
public class ItemSwapCoordinator
{
    public delegate bool InteractionRule(GameLocation location, Vector2 tile, Farmer player);

    /// <summary> List of registered switch rules. (Order is important) </summary>
    private static readonly List<InteractionRule> interactionRules = new()
    {
        ObjectsInteractionRules.TrySwap,
        ResourceClumpsInteractionRules.TrySwap,
        TerrainFeaturesInteractionRules.TrySwap,
        MonstersInteractionRules.TrySwap,
        WaterInteractionRules.TrySwap,
        AnimalsInteractionRules.TrySwap,
        SoilInteractionRules.TrySwap
    };

    /// <summary>
    /// Iterates over all registered switch rules to attempt a tool/item switch.
    /// </summary>
    /// <param name="location">The current game location.</param>
    /// <param name="tile">The tile that was clicked.</param>
    /// <param name="player">The current player.</param>
    /// <returns>True if any rule performed a switch; otherwise, false.</returns>
    public static bool TrySwapAll(GameLocation location, Vector2 tile, Farmer player)
    {
        foreach (var rule in interactionRules)
        {
            if (rule(location, tile, player))
                return true;
        }
        return false;
    }
}
