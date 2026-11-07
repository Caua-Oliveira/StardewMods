using StardewValley;
using StardewValley.Tools;

namespace Core;

internal enum DecisionKind { NotApplicable, KeepCurrent, Select }
internal enum WeaponKind { Weapon, PreferScythe, ScytheOnly }

/// <summary>A rule's recommendation. Evaluating it never changes the inventory.</summary>
internal sealed record SwapDecision(DecisionKind Kind, Func<Item, bool>? Matches = null, Func<Item, bool>? Fallback = null)
{
    public static readonly SwapDecision None = new(DecisionKind.NotApplicable);
    public static readonly SwapDecision Keep = new(DecisionKind.KeepCurrent);

    public static SwapDecision Item(Func<Item, bool> matches) => new(DecisionKind.Select, matches);
    public static SwapDecision Tool<T>() where T : Tool => Item(item => item is T);
    public static SwapDecision BreakingTool() => Item(item => item is Axe or Pickaxe or Hoe);
    public static SwapDecision Weapon(WeaponKind kind = WeaponKind.Weapon) => kind switch
    {
        WeaponKind.ScytheOnly => Item(item => item is MeleeWeapon weapon && weapon.isScythe()),
        WeaponKind.PreferScythe => new(DecisionKind.Select,
            item => item is MeleeWeapon weapon && weapon.isScythe(),
            item => item is MeleeWeapon weapon && !weapon.isScythe()),
        _ => Item(item => item is MeleeWeapon weapon && !weapon.isScythe())
    };
}
