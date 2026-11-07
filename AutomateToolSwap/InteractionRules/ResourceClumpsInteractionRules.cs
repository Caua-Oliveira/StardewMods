using Core;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace InteractionRules;

internal static class ResourceClumpsInteractionRules
{
    public static SwapDecision Evaluate(SwapContext context)
    {
        foreach (var clump in context.Location.resourceClumps)
        {
            if (!clump.occupiesTile((int)context.Tile.X, (int)context.Tile.Y))
                continue;
            var config = context.Config;
            string? id = clump.modDataForSerialization.Pairs
                .FirstOrDefault(pair => pair.Key.Contains("clumpid", StringComparison.OrdinalIgnoreCase)).Value;
            string? tool = id == null ? null : context.Integrations.GetBreakingTool(id, true);
            if (tool == "Pickaxe")
                return config.PickaxeForBoulders ? SwapDecision.Tool<Pickaxe>() : SwapDecision.Keep;
            if (tool == "Axe")
                return config.AxeForStumpsAndLogs ? SwapDecision.Tool<Axe>() : SwapDecision.Keep;
            if (clump is GiantCrop)
                return config.AxeForGiantCrops && context.Player.CurrentItem?.QualifiedItemId is not ("(BC)105" or "(BC)264")
                    ? SwapDecision.Tool<Axe>() : SwapDecision.Keep;
            return clump.parentSheetIndex.Value switch
            {
                600 or 602 when config.AxeForStumpsAndLogs => SwapDecision.Tool<Axe>(),
                758 or 756 or 754 or 752 or 672 or 622 or 148 when config.PickaxeForBoulders => SwapDecision.Tool<Pickaxe>(),
                44 or 46 when config.ScytheForBushes => SwapDecision.Weapon(WeaponKind.PreferScythe),
                _ => SwapDecision.Keep
            };
        }
        return SwapDecision.None;
    }
}
