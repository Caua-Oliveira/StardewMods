using Core;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace InteractionRules;

internal static class ObjectsInteractionRules
{
    public static SwapDecision Evaluate(SwapContext context)
    {
        var obj = context.Location.getObjectAtTile((int)context.Tile.X, (int)context.Tile.Y);
        if (obj == null)
            return SwapDecision.None;
        var config = context.Config;
        var held = context.Player.CurrentItem;

        string? breakingTool = context.Integrations.GetBreakingTool(obj.ItemId, false);
        if (breakingTool == "Pickaxe")
            return config.PickaxeForStoneAndOres ? SwapDecision.Tool<Pickaxe>() : SwapDecision.Keep;
        if (breakingTool == "Axe")
            return config.AxeForTwigs ? SwapDecision.Tool<Axe>() : SwapDecision.Keep;
        if (obj.IsWeeds())
            return config.AnyToolForWeeds && context.Location is not MineShaft ? SwapDecision.BreakingTool()
                : config.ScytheForWeeds ? SwapDecision.Weapon(WeaponKind.PreferScythe) : SwapDecision.Keep;
        if (obj.IsBreakableStone())
            return config.PickaxeForStoneAndOres ? SwapDecision.Tool<Pickaxe>() : SwapDecision.Keep;
        if (obj.IsTwig())
            return config.AxeForTwigs ? SwapDecision.Tool<Axe>() : SwapDecision.Keep;
        if (obj.QualifiedItemId is "(O)590" or "(O)SeedSpot")
            return config.HoeForArtifactSpots ? SwapDecision.Tool<Hoe>() : SwapDecision.Keep;
        if (obj.isForage())
            return config.ScytheForForage ? SwapDecision.Weapon(WeaponKind.ScytheOnly) : SwapDecision.Keep;
        if (obj is CrabPot pot)
            return config.BaitForCrabPot && pot.bait.Value == null && !pot.readyForHarvest.Value
                ? SwapDecision.Item(item => item.Category == SObject.baitCategory) : SwapDecision.Keep;
        if (obj is IndoorPot gardenPot)
            return config.WateringCanForGardenPot && held is not (Axe or Pickaxe) && held?.Category != -74
                && !gardenPot.hoeDirt.Value.isWatered() ? SwapDecision.Tool<WateringCan>() : SwapDecision.Keep;
        if (obj is BreakableContainer)
            return config.WeaponForMineBarrels ? SwapDecision.Weapon() : SwapDecision.Keep;

        // Non-machine utility objects. Keep a name fallback only for the legacy optional water-bowl integration.
        if (obj.Name == "Supply Crate")
            return config.AnyToolForSupplyCrates ? SwapDecision.BreakingTool() : SwapDecision.Keep;
        if (obj.Name == "Water Bowl")
            return config.WateringCanForPetBowl && held is not (Axe or Pickaxe) ? SwapDecision.Tool<WateringCan>() : SwapDecision.Keep;

        // Respect an explicitly held breaking tool when targeting machines.
        if (held is Axe or Pickaxe)
            return SwapDecision.Keep;

        bool enabled = obj.QualifiedItemId switch
        {
            "(BC)13" or "(BC)HeavyFurnace" => config.OresForFurnace,
            "(BC)16" => config.MilkForCheesePress,
            "(BC)24" => config.EggsForMayoMachine,
            "(BC)20" => config.TrashForRecycling,
            "(BC)90" => config.BoneForBoneMill,
            "(BC)17" => config.WoolForLoom,
            "(BC)FishSmoker" => config.FishForSmoker,
            "(BC)BaitMaker" => config.FishForBaitMaker,
            "(BC)21" => config.MineralsForCrystalarium,
            "(BC)25" => config.SwapForSeedMaker,
            "(BC)19" => config.SwapForOilMaker,
            "(BC)12" => config.SwapForKegs != "None",
            "(BC)15" => config.SwapForPreservesJar != "None",
            "(BC)Dehydrator" => config.SwapForDehydrator != "None",
            "(BC)182" => config.GeodeForCrusher,
            "(BC)114" => config.WoodForCharcoalKiln,
            "(BC)163" => config.GoodsForCask,
            "(BC)211" => config.HardwoodForChipper,
            _ => false
        };
        if (!enabled)
            return SwapDecision.Keep; // Don't fall through to hoe/monster logic on a placed object.

        return MachineInput(context, obj);
    }

    private static SwapDecision MachineInput(SwapContext context, SObject machine)
    {
        var data = machine.GetMachineData();
        if (data == null || machine.heldObject.Value != null && !data.AllowLoadWhenFull
            || machine is Cask cask && !cask.IsValidCaskLocation()
            || !MachineDataUtility.HasAdditionalRequirements(context.Player.Items, data.AdditionalConsumedItems, out _))
            return SwapDecision.Keep;

        return SwapDecision.Item(item => MatchesPreference(context, machine, item)
            && !(machine is Cask && item.Quality >= 4)
            && !(machine.heldObject.Value != null && item.QualifiedItemId == machine.lastInputItem.Value?.QualifiedItemId)
            && MachineDataUtility.TryGetMachineOutputRule(machine, data, MachineOutputTrigger.ItemPlacedInMachine,
                item, context.Player, context.Location, out _, out _, out _, out _));
    }

    private static bool MatchesPreference(SwapContext context, SObject machine, Item item)
    {
        // Preserve the opt-in crop preferences, but let the game's machine data validate actual inputs/counts.
        string mode = machine.QualifiedItemId switch
        {
            "(BC)12" => context.Config.SwapForKegs,
            "(BC)15" => context.Config.SwapForPreservesJar,
            "(BC)Dehydrator" => context.Config.SwapForDehydrator,
            _ => "Any"
        };
        return mode switch
        {
            "Any" => true,
            "Fruit" => item.Category == -79,
            "Vegetable" => item.Category == -75,
            "Mushroom" => item.HasContextTag("edible_mushroom"),
            "Both" => item.Category == -79 || (machine.ItemId == "Dehydrator" ? item.HasContextTag("edible_mushroom") : item.Category == -75),
            _ => false
        };
    }
}
