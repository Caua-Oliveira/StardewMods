using Core;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.GameData.Crops;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace InteractionRules;

internal static class TerrainFeaturesInteractionRules
{
    public static SwapDecision Evaluate(SwapContext context)
    {
        var config = context.Config;
        var player = context.Player;
        var held = player.CurrentItem;
        foreach (var feature in context.Location.largeTerrainFeatures)
        {
            if (feature is Bush bush && config.ScytheForBushes && bush.inBloom()
                && bush.getBoundingBox().Contains((int)(context.Tile.X * Game1.tileSize), (int)(context.Tile.Y * Game1.tileSize)))
                return SwapDecision.Weapon(WeaponKind.PreferScythe);
        }
        if (!context.Location.terrainFeatures.TryGetValue(context.Tile, out var terrain))
            return SwapDecision.None;

        if (terrain is Tree tree)
        {
            // Placement takes precedence over moss removal too.
            if (held?.QualifiedItemId is "(BC)105" or "(BC)264" or "(O)805")
                return SwapDecision.Keep;
            if (tree.hasMoss.Value && tree.growthStage.Value >= Tree.treeStage && config.ScytheForMossOnTrees)
                return SwapDecision.Weapon(WeaponKind.PreferScythe);
            return config.AxeForTrees && !(config.IgnoreGrowingTrees && tree.growthStage.Value < Tree.treeStage)
                ? SwapDecision.Tool<Axe>() : SwapDecision.Keep;
        }
        if (terrain is FruitTree)
            return config.AxeForFruitTrees ? SwapDecision.Tool<Axe>() : SwapDecision.Keep;
        if (terrain is Grass)
            return config.ScytheForGrass && held is not (MilkPail or Shears)
                ? SwapDecision.Weapon(WeaponKind.ScytheOnly) : SwapDecision.Keep;
        if (terrain is not HoeDirt dirt)
            return SwapDecision.None;

        var crop = dirt.crop;
        if (crop?.dead.Value == true && !config.ScytheForCrops)
            return SwapDecision.Keep;
        bool preservePickaxe = config.PickaxeOverWateringCan && held is Pickaxe;
        if (crop == null)
        {
            if (held?.Category is -74 or -19)
                return SwapDecision.Keep;
            if (config.SeedForTilledDirt && !preservePickaxe)
            {
                var seeds = SwapDecision.Item(item => item.Category == -74 && !item.HasContextTag("tree_seed_item"));
                if (InventorySelector.Find(player, seeds).HasValue)
                    return seeds;
            }
            return config.PickaxeForTilledSoil && !player.isRidingHorse() ? SwapDecision.Tool<Pickaxe>() : SwapDecision.Keep;
        }

        if (config.ScytheForCrops && (dirt.readyForHarvest() || crop.dead.Value))
        {
            // Ordinary scythes cannot harvest hand-picked crops; the iridium scythe can.
            return SwapDecision.Item(item => item is MeleeWeapon weapon && weapon.isScythe()
                && (crop.dead.Value || crop.GetHarvestMethod() == HarvestMethod.Scythe || weapon.QualifiedItemId == "(W)66"));
        }
        if (config.HoeForGingerCrop && crop.whichForageCrop.Value == "2")
            return SwapDecision.Tool<Hoe>();
        if (!dirt.isWatered() && !dirt.readyForHarvest() && config.WateringCanForUnwateredCrop)
            return preservePickaxe || SwapPolicy.IsTractor(player) && held is Hoe
                ? SwapDecision.Keep : SwapDecision.Tool<WateringCan>();
        if (config.FertilizerForCrops && !dirt.HasFertilizer() && !preservePickaxe && dirt.CanApplyFertilizer("(O)369"))
            return SwapDecision.Item(item => item.Category == -19 && item.QualifiedItemId != "(O)805"
                && dirt.CanApplyFertilizer(item.QualifiedItemId));
        return SwapDecision.Keep;
    }
}
