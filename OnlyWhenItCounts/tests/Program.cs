using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using HarmonyLib;
using OnlyWhenItCounts;
using StardewValley;
using StardewValley.Tools;

// No save or game process is needed. Resolve the game's dependencies from the install directory.
string gamePath = args.FirstOrDefault() ?? @"C:\Games\Stardew Valley";
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    foreach (string directory in new[] { gamePath, Path.Combine(gamePath, "smapi-internal") })
    {
        string file = Path.Combine(directory, name.Name + ".dll");
        if (File.Exists(file))
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(file));
    }
    return null;
};
RunTests();

static void RunTests()
{
    Type patches = typeof(ModEntry).Assembly.GetType("OnlyWhenItCounts.ToolUsePatches", throwOnError: true)!;
    MethodInfo transpiler = AccessTools.Method(patches, "Transpiler");
    MethodInfo staminaFilter = AccessTools.Method(patches, "FilterStaminaCost");
    MethodInfo waterFilter = AccessTools.Method(patches, "FilterWaterCost");
    MethodInfo detection = AccessTools.Method(patches, "IsUsefulUse");
    int assertions = 0;

    void Check(bool condition, string message)
    {
        assertions++;
        if (!condition)
            throw new Exception(message);
    }

    List<CodeInstruction> Transpile(MethodBase method, List<CodeInstruction> original)
    {
        ILGenerator generator = new DynamicMethod("test", typeof(void), Type.EmptyTypes).GetILGenerator();
        return ((IEnumerable<CodeInstruction>)transpiler.Invoke(null, new object[] { original, generator, method })!).ToList();
    }

    foreach (Type type in new[] { typeof(Axe), typeof(Pickaxe), typeof(Hoe), typeof(WateringCan), typeof(FishingRod) })
    {
        MethodInfo method = AccessTools.DeclaredMethod(type, nameof(Tool.DoFunction));
        var original = PatchProcessor.GetOriginalInstructions(method);
        var result = Transpile(method, original);
        Check(result.Count(i => i.Calls(staminaFilter)) == 1, $"{type.Name}: expected one stamina filter");
        Check(result.Count(i => i.Calls(waterFilter)) == (type == typeof(WateringCan) ? 1 : 0), $"{type.Name}: wrong water filters");
        Check(result.Count(i => i.Calls(detection)) == 1, $"{type.Name}: decision must be invocation-local");
        Check(result.Count(i => i.opcode == OpCodes.Ret) == original.Count(i => i.opcode == OpCodes.Ret), $"{type.Name}: must preserve all return paths");

        MethodInfo setter = AccessTools.PropertySetter(typeof(Farmer), nameof(Farmer.Stamina));
        var missingCost = original.Where(i => !i.Calls(setter)).ToList();
        Check(Transpile(method, missingCost).SequenceEqual(missingCost), $"{type.Name}: missing pattern must fail open");
        var ambiguous = original.Concat(PatchProcessor.GetOriginalInstructions(method)).ToList();
        Check(Transpile(method, ambiguous).SequenceEqual(ambiguous), $"{type.Name}: ambiguous pattern must fail open");
    }

    Check((float)staminaFilter.Invoke(null, new object[] { 7.5f, false })! == 0f, "Miss must cost no stamina");
    Check((float)staminaFilter.Invoke(null, new object[] { 7.5f, true })! == 7.5f, "Useful use must preserve modified costs");
    Check((int)waterFilter.Invoke(null, new object[] { 4, false })! == 4, "Default must still consume water");
    ModEntry.Config.ReturnWaterToWateringCan = true;
    Check((int)waterFilter.Invoke(null, new object[] { 4, false })! == 0, "Water option must suppress only wasted water");
    Check((int)waterFilter.Invoke(null, new object[] { 4, true })! == 4, "Useful watering must still consume water");
    ModEntry.Config.ReturnWaterToWateringCan = false;

    // Execute a transformed cost calculation with stand-in resource accessors. Changes
    // made by the simulated tool action must survive even when the initial swing misses.
    MethodInfo wateringMethod = AccessTools.DeclaredMethod(typeof(WateringCan), nameof(Tool.DoFunction));
    MethodInfo staminaGetter = AccessTools.PropertyGetter(typeof(Farmer), nameof(Farmer.Stamina));
    MethodInfo staminaSetter = AccessTools.PropertySetter(typeof(Farmer), nameof(Farmer.Stamina));
    MethodInfo waterGetter = AccessTools.PropertyGetter(typeof(WateringCan), nameof(WateringCan.WaterLeft));
    MethodInfo waterSetter = AccessTools.PropertySetter(typeof(WateringCan), nameof(WateringCan.WaterLeft));
    var fixtureCode = new List<CodeInstruction>
    {
        new(OpCodes.Ldarg_S, (byte)5), new(OpCodes.Dup), new(OpCodes.Call, staminaGetter),
        new(OpCodes.Ldc_R4, 3.5f), new(OpCodes.Sub), new(OpCodes.Call, staminaSetter),
        new(OpCodes.Ldarg_S, (byte)5), new(OpCodes.Dup), new(OpCodes.Call, staminaGetter),
        new(OpCodes.Ldc_R4, 5f), new(OpCodes.Add), new(OpCodes.Call, staminaSetter),
        new(OpCodes.Ldarg_0), new(OpCodes.Castclass, typeof(WateringCan)), new(OpCodes.Dup), new(OpCodes.Call, waterGetter),
        new(OpCodes.Ldc_I4_2), new(OpCodes.Sub), new(OpCodes.Call, waterSetter),
        new(OpCodes.Ldarg_0), new(OpCodes.Castclass, typeof(WateringCan)), new(OpCodes.Dup), new(OpCodes.Call, waterGetter),
        new(OpCodes.Ldc_I4_7), new(OpCodes.Add), new(OpCodes.Call, waterSetter),
        new(OpCodes.Ret)
    };
    var fixture = new DynamicMethod("ToolCostFixture", typeof(void),
        new[] { typeof(Tool), typeof(GameLocation), typeof(int), typeof(int), typeof(int), typeof(Farmer) },
        typeof(ModEntry).Module, skipVisibility: true);
    ILGenerator fixtureIL = fixture.GetILGenerator();
    var transformed = (IEnumerable<CodeInstruction>)transpiler.Invoke(null, new object[] { fixtureCode, fixtureIL, wateringMethod })!;
    var replacements = new Dictionary<MethodInfo, string>
    {
        [detection] = nameof(Resources.IsUsefulUse),
        [staminaGetter] = nameof(Resources.GetStamina),
        [staminaSetter] = nameof(Resources.SetStamina),
        [waterGetter] = nameof(Resources.GetWater),
        [waterSetter] = nameof(Resources.SetWater)
    };
    foreach (CodeInstruction instruction in transformed)
    {
        if (instruction.operand is MethodInfo called && replacements.TryGetValue(called, out string? replacement))
            instruction.operand = AccessTools.Method(typeof(Resources), replacement);
        if (instruction.operand == null)
            fixtureIL.Emit(instruction.opcode);
        else if (instruction.operand is MethodInfo method)
            fixtureIL.Emit(instruction.opcode, method);
        else if (instruction.operand is LocalBuilder local)
            fixtureIL.Emit(instruction.opcode, local);
        else if (instruction.operand is byte argument)
            fixtureIL.Emit(instruction.opcode, argument);
        else if (instruction.operand is float value)
            fixtureIL.Emit(instruction.opcode, value);
        else if (instruction.operand is Type type)
            fixtureIL.Emit(instruction.opcode, type);
        else
            throw new Exception($"Unexpected fixture operand: {instruction}");
    }
    var useTool = fixture.CreateDelegate<Action<Tool, GameLocation, int, int, int, Farmer>>();
    foreach (bool useful in new[] { false, true })
    foreach (bool saveWater in new[] { false, true })
    {
        Resources.Useful = useful;
        Resources.Stamina = 100f;
        Resources.Water = 20;
        ModEntry.Config.ReturnWaterToWateringCan = saveWater;
        useTool(null!, null!, 0, 0, 0, null!);
        Check(Resources.Stamina == (useful ? 101.5f : 105f), "Tool cost must not erase unrelated stamina changes");
        Check(Resources.Water == (!useful && saveWater ? 27 : 25), "Tool cost must not erase unrelated water changes");
    }
    ModEntry.Config.ReturnWaterToWateringCan = false;

    // Actually generate and install the Harmony replacements: catches invalid IL, delegate
    // binding errors, patch discovery errors, and changes to the installed game's methods.
    var harmony = new Harmony("OnlyWhenItCounts.Tests");
    try
    {
        harmony.PatchAll(typeof(ModEntry).Assembly);
        foreach (Type type in new[] { typeof(Axe), typeof(Pickaxe), typeof(Hoe), typeof(WateringCan), typeof(FishingRod) })
        {
            var info = Harmony.GetPatchInfo(AccessTools.DeclaredMethod(type, nameof(Tool.DoFunction)));
            Check(info?.Transpilers.Any(p => p.owner == harmony.Id) == true, $"{type.Name}: Harmony patch not installed");
        }
    }
    finally
    {
        harmony.UnpatchAll(harmony.Id);
    }

    Console.WriteLine($"PASS: {assertions} assertions, including Harmony installation on all five game tools.");
}

internal static class Resources
{
    internal static bool Useful;
    internal static float Stamina;
    internal static int Water;
    internal static bool IsUsefulUse(Tool tool, GameLocation location, int x, int y, Farmer who) => Useful;
    internal static float GetStamina(Farmer who) => Stamina;
    internal static void SetStamina(Farmer who, float value) => Stamina = value;
    internal static int GetWater(WateringCan tool) => Water;
    internal static void SetWater(WateringCan tool, int value) => Water = value;
}
