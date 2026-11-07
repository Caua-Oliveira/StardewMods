using Core;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Tools;

namespace InteractionRules;

internal static class MonstersInteractionRules
{
    public static SwapDecision Evaluate(SwapContext context) => Evaluate(context, context.Player?.CurrentItem);

    internal static SwapDecision Evaluate(SwapContext context, Item? heldItem)
    {
        if (!context.Config.WeaponOnMonsters)
            return SwapDecision.None;

        Monster? nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (var character in context.Location.characters)
        {
            if (character is not Monster monster || monster.Health <= 0)
                continue;
            if (monster is RockCrab && context.Config.IgnoreCrabs)
                continue;
            if (monster is GreenSlime && context.Config.IgnoreSlimesOnFarm && context.Location is Farm or SlimeHutch)
                continue;
            float distance = Microsoft.Xna.Framework.Vector2.Distance(context.Tile, monster.Tile);
            if (distance < Math.Max(0, context.Config.MonsterRangeDetection) && distance < nearestDistance)
            {
                nearest = monster;
                nearestDistance = distance;
            }
        }

        if (nearest == null)
            return SwapDecision.None;
        // Alternate mode protects the player's chosen combat weapon only while an
        // eligible threat is in range. Scythes still need to switch to a real weapon.
        if (context.Config.AlternativeWeaponOnMonsters && heldItem is MeleeWeapon weapon && !weapon.isScythe())
            return SwapDecision.Keep;

        if (nearest is RockCrab crab && !crab.isStickBug.Value && !crab.shellGone.Value && !crab.isMoving())
            return SwapDecision.Tool<Pickaxe>();
        return SwapDecision.Weapon();
    }
}
