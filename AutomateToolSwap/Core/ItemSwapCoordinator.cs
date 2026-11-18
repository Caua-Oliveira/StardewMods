using InteractionRules;

namespace Core;

/// <summary>
/// Aggregates all switch rules and invokes them sequentially.
/// Only the first rule that applies will trigger a tool/item switch.
/// </summary>
internal static class ItemSwapCoordinator
{
    public delegate SwapDecision InteractionRule(SwapContext context);

    /// <summary> List of registered switch rules. (Order is important) </summary>
    private static readonly List<InteractionRule> interactionRules = new()
    {
        ObjectsInteractionRules.Evaluate,
        ResourceClumpsInteractionRules.Evaluate,
        AnimalsInteractionRules.EvaluateDirectHarvest,
        TerrainFeaturesInteractionRules.Evaluate,
        MonstersInteractionRules.Evaluate,
        WaterInteractionRules.Evaluate,
        AnimalsInteractionRules.Evaluate,
        SoilInteractionRules.Evaluate
    };

    /// <summary>
    /// Iterates over all registered switch rules to attempt a tool/item switch.
    /// </summary>
    public static SwapDecision Evaluate(SwapContext context)
    {
        foreach (var rule in interactionRules)
        {
            var decision = rule(context);
            if (decision.Kind != DecisionKind.NotApplicable)
                return decision;
        }
        return SwapDecision.None;
    }
}
