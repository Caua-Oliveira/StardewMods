using Configuration;
using Core;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using StardewValley.Tools;

namespace InteractionRules;

internal enum AnimalHarvestToolKind
{
    None,
    MilkPail,
    Shears
}

internal static class AnimalsInteractionRules
{
    private const string MilkPailBaseName = "Milk Pail";
    private const string ShearsBaseName = "Shears";

    /// <summary>Matches ready-to-harvest animals directly on the targeted tile before ground terrain rules (such as grass).</summary>
    public static SwapDecision EvaluateDirectHarvest(SwapContext context)
        => EvaluateHarvestableAnimals(context, directHitOnly: true);

    public static SwapDecision Evaluate(SwapContext context)
    {
        var harvestDecision = EvaluateHarvestableAnimals(context, directHitOnly: false);
        if (harvestDecision.Kind != DecisionKind.NotApplicable)
            return harvestDecision;

        if (context.Location is AnimalHouse && context.Location.doesTileHaveProperty(
            (int)context.Tile.X, (int)context.Tile.Y, "Trough", "Back") != null)
            return context.Config.HayForFeedingBench ? SwapDecision.Item(item => item.QualifiedItemId == "(O)178") : SwapDecision.Keep;
        return SwapDecision.None;
    }

    internal static AnimalHarvestToolKind ClassifyHarvestTool(FarmAnimalData? data)
    {
        if (data == null || data.HarvestType == FarmAnimalHarvestType.DigUp)
            return AnimalHarvestToolKind.None;

        string? tool = data.HarvestTool?.Trim();
        if (string.Equals(tool, MilkPailBaseName, StringComparison.OrdinalIgnoreCase))
            return AnimalHarvestToolKind.MilkPail;
        if (string.Equals(tool, ShearsBaseName, StringComparison.OrdinalIgnoreCase))
            return AnimalHarvestToolKind.Shears;
        return AnimalHarvestToolKind.None;
    }

    internal static bool HasReadyProduce(string? currentProduce)
        => !string.IsNullOrWhiteSpace(currentProduce) && currentProduce != "-1";

    internal static SwapDecision DecisionForTool(AnimalHarvestToolKind toolKind, ModConfig config) => toolKind switch
    {
        AnimalHarvestToolKind.MilkPail when config.SwapToMilkPail => SwapDecision.Item(IsMilkPailTool),
        AnimalHarvestToolKind.Shears when config.SwapToShears => SwapDecision.Item(IsShearsTool),
        _ => SwapDecision.None
    };

    private static bool IsMilkPailTool(Item item)
        => item is MilkPail || item is Tool tool && string.Equals(tool.BaseName, MilkPailBaseName, StringComparison.Ordinal);

    private static bool IsShearsTool(Item item)
        => item is Shears || item is Tool tool && string.Equals(tool.BaseName, ShearsBaseName, StringComparison.Ordinal);

    private static SwapDecision EvaluateHarvestableAnimals(SwapContext context, bool directHitOnly)
    {
        if (context.Location.animals.Count() == 0)
            return SwapDecision.None;

        var tileRect = new Rectangle(
            (int)context.Tile.X * Game1.tileSize,
            (int)context.Tile.Y * Game1.tileSize,
            Game1.tileSize,
            Game1.tileSize);

        var candidates = context.Location.animals.Values
            .Where(animal => animal.isAdult() && HasReadyProduce(animal.currentProduce.Value))
            .Select(animal => (Animal: animal, DirectHit: IntersectsTile(animal, context.Tile, tileRect), DistanceSq: Vector2.DistanceSquared(context.Tile, animal.Tile)))
            .Where(entry => directHitOnly ? entry.DirectHit : (entry.DirectHit || entry.DistanceSq <= 1f))
            .OrderByDescending(entry => entry.DirectHit)
            .ThenBy(entry => entry.DistanceSq);

        foreach (var entry in candidates)
        {
            var toolKind = ResolveHarvestTool(entry.Animal, context.Player);
            var decision = DecisionForTool(toolKind, context.Config);
            if (decision.Kind != DecisionKind.NotApplicable)
                return decision;
        }

        return SwapDecision.None;
    }

    private static AnimalHarvestToolKind ResolveHarvestTool(FarmAnimal animal, Farmer player)
    {
        FarmAnimalData? data = animal.GetAnimalData();
        AnimalHarvestToolKind byData = ClassifyHarvestTool(data);
        if (byData != AnimalHarvestToolKind.None || data == null || data.HarvestType == FarmAnimalHarvestType.DigUp)
            return byData;

        // Fallback for C# mods that patch FarmAnimal.CanGetProduceWithTool without setting Data/FarmAnimals.HarvestTool.
        if (CanHarvestWithInventoryTool<MilkPail>(animal, player))
            return AnimalHarvestToolKind.MilkPail;
        if (CanHarvestWithInventoryTool<Shears>(animal, player))
            return AnimalHarvestToolKind.Shears;
        return AnimalHarvestToolKind.None;
    }

    private static bool CanHarvestWithInventoryTool<TTool>(FarmAnimal animal, Farmer player) where TTool : Tool
    {
        foreach (Item? item in player.Items)
        {
            if (item is TTool tool)
                return animal.CanGetProduceWithTool(tool);
        }
        return false;
    }

    private static bool IntersectsTile(FarmAnimal animal, Vector2 tile, Rectangle tileRect)
        => animal.Tile == tile
            || animal.Sprite != null
                && (animal.GetHarvestBoundingBox().Intersects(tileRect)
                    || animal.GetCursorPetBoundingBox().Intersects(tileRect));
}