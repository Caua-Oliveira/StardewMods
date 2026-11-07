using Core;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Tools;

namespace InteractionRules;

internal static class SoilInteractionRules
{
    public static SwapDecision Evaluate(SwapContext context)
    {
        var held = context.Player.CurrentItem;
        if (!context.Config.HoeForDiggableSoil || SwapPolicy.IsTractor(context.Player)
            || context.Location is Mine or MineShaft or VolcanoDungeon
            || held is FishingRod or GenericTool or Wand
            || held is MeleeWeapon weapon && !weapon.isScythe() && Game1.spawnMonstersAtNight)
            return SwapDecision.None;
        if (context.Location.doesTileHaveProperty((int)context.Tile.X, (int)context.Tile.Y, "Diggable", "Back") == null)
            return SwapDecision.None;
        if (held?.isPlaceable() == true)
            return SwapDecision.Keep;
        if (context.Location.isPath(context.Tile))
            return context.Config.PickaxeForFloorTile ? SwapDecision.Tool<Pickaxe>() : SwapDecision.Keep;
        return SwapDecision.Tool<Hoe>();
    }
}
