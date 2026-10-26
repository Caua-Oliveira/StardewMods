using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;

namespace OnlyWhenItCounts;

/// <summary>Filter only the original tool costs, without skipping actions or restoring old resource values.</summary>
[HarmonyPatch]
internal static class ToolUsePatches
{
    private static IMonitor? Monitor;
    private static readonly HashSet<Type> ReportedDetectionErrors = new();
    private static readonly Func<Tool, Vector2, int, Farmer, List<Vector2>> TilesAffected =
        AccessTools.MethodDelegate<Func<Tool, Vector2, int, Farmer, List<Vector2>>>(
            AccessTools.Method(typeof(Tool), "tilesAffected"));

    internal static void Initialize(IMonitor monitor) => Monitor = monitor;

    private static IEnumerable<MethodBase> TargetMethods()
    {
        foreach (Type type in new[] { typeof(Axe), typeof(Pickaxe), typeof(Hoe), typeof(WateringCan), typeof(FishingRod) })
            yield return AccessTools.DeclaredMethod(type, nameof(Tool.DoFunction),
                new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(int), typeof(Farmer) });
    }

    // Run after other transpilers so their cost-formula changes are retained.
    [HarmonyTranspiler]
    [HarmonyPriority(Priority.Last)]
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
    {
        var code = instructions.ToList();
        int staminaIndex = FindDecrement(code, typeof(Farmer), nameof(Farmer.Stamina));
        int waterIndex = __originalMethod.DeclaringType == typeof(WateringCan)
            ? FindDecrement(code, typeof(WateringCan), nameof(WateringCan.WaterLeft))
            : -1;

        // Don't guess if a game update or another transpiler replaced the expected calculation.
        if (staminaIndex < 0 || (__originalMethod.DeclaringType == typeof(WateringCan) && waterIndex < 0))
        {
            Monitor?.Log($"Couldn't identify the tool costs in {__originalMethod.DeclaringType?.Name}.DoFunction. Leaving this tool unchanged.", LogLevel.Warn);
            return code;
        }

        LocalBuilder didWork = generator.DeclareLocal(typeof(bool));
        // Insert backwards to keep indices valid. Only the cost operand is filtered;
        // the existing getter, subtraction and setter (including other mods' patches) remain.
        foreach (var entry in new[] { (Index: staminaIndex, Filter: nameof(FilterStaminaCost)), (Index: waterIndex, Filter: nameof(FilterWaterCost)) }
                     .Where(entry => entry.Index >= 0).OrderByDescending(entry => entry.Index))
        {
            var load = new CodeInstruction(OpCodes.Ldloc, didWork);
            load.MoveLabelsFrom(code[entry.Index]);
            load.MoveBlocksFrom(code[entry.Index]);
            code.InsertRange(entry.Index, new[]
            {
                load,
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ToolUsePatches), entry.Filter))
            });
        }

        // A method-local decision is safe for split-screen, nested calls, and tool switches.
        // Evaluate before base.DoFunction can destroy the target (e.g. killing a monster).
        code.InsertRange(0, new[]
        {
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldarg_1),
            new CodeInstruction(OpCodes.Ldarg_2),
            new CodeInstruction(OpCodes.Ldarg_3),
            new CodeInstruction(OpCodes.Ldarg_S, (byte)5),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ToolUsePatches), nameof(IsUsefulUse))),
            new CodeInstruction(OpCodes.Stloc, didWork)
        });
        return code;
    }

    private static int FindDecrement(List<CodeInstruction> code, Type type, string property)
    {
        MethodInfo getter = AccessTools.PropertyGetter(type, property);
        MethodInfo setter = AccessTools.PropertySetter(type, property);
        int result = -1;
        for (int i = 1; i < code.Count; i++)
        {
            if (!code[i].Calls(setter) || code[i - 1].opcode != OpCodes.Sub)
                continue;

            // Require a matching read-modify-write, not an unrelated assignment.
            bool foundGetter = false;
            for (int j = i - 2; j >= Math.Max(0, i - 40); j--)
            {
                if (code[j].Calls(setter))
                    break;
                if (code[j].Calls(getter))
                {
                    foundGetter = true;
                    break;
                }
            }
            if (!foundGetter)
                continue;
            if (result >= 0)
                return -1; // ambiguous: never suppress an arbitrary stamina change
            result = i - 1;
        }
        return result;
    }

    internal static float FilterStaminaCost(float cost, bool didWork) => didWork ? cost : 0f;

    internal static int FilterWaterCost(int cost, bool didWork) =>
        !didWork && ModEntry.Config.ReturnWaterToWateringCan ? 0 : cost;

    internal static bool IsUsefulUse(Tool tool, GameLocation location, int x, int y, Farmer? who)
    {
        who ??= tool.getLastFarmerToUse();
        if (!ModEntry.Config.Enabled || !Context.IsWorldReady || who == null || !who.IsLocalPlayer)
            return true;

        try
        {
            if (tool is FishingRod rod)
            {
                Vector2 bobber = new(rod.bobber.X / Game1.tileSize, rod.bobber.Y / Game1.tileSize);
                return location.canFishHere() && location.isTileFishable((int)bobber.X, (int)bobber.Y);
            }

            Vector2 tile = new(x / Game1.tileSize, y / Game1.tileSize);
            if (tool is Hoe or WateringCan)
            {
                if (tool is WateringCan && location.CanRefillWateringCanOnTile((int)tile.X, (int)tile.Y))
                    return true;

                return TilesAffected(tool, tile, who.toolPower.Value, who)
                    .Any(target => DidWork.OnTile(tool, location, target));
            }

            if (DidWork.OnTile(tool, location, tile))
                return true;

            // Pickaxes also look for objects just across a tile boundary.
            if (tool is Pickaxe && location.getObjectAtTile((int)tile.X, (int)tile.Y) == null)
            {
                foreach (int offset in new[] { -8, 8 })
                {
                    Vector2 adjacent = who.FacingDirection is 0 or 2
                        ? new Vector2((x + offset) / Game1.tileSize, tile.Y)
                        : new Vector2(tile.X, (y + offset) / Game1.tileSize);
                    if (Detects.Objects(location, adjacent, tool))
                        return true;
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            // A detection/API failure should never break the tool or grant free actions.
            if (ReportedDetectionErrors.Add(tool.GetType()))
                Monitor?.Log($"Couldn't check {tool.GetType().Name} use; keeping its normal cost. {ex}", LogLevel.Warn);
            return true;
        }
    }
}
