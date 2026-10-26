using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace OnlyWhenItCounts
{
    public class ModEntry : Mod
    {
        public static ModConfig Config { get; private set; } = new();
        internal static IItemExtensionsApi? ItemExtensionsAPI;

        public override void Entry(IModHelper helper)
        {
            ToolUsePatches.Initialize(this.Monitor);
            Config = this.Helper.ReadConfig<ModConfig>();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.PatchAll(typeof(ModEntry).Assembly);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            ItemExtensionsAPI = Helper.ModRegistry.GetApi<IItemExtensionsApi>("mistyspring.ItemExtensions");
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                mod: this.ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(Config)
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Enable Mod",
                tooltip: () => "Enable or disable the mod.",
                getValue: () => Config.Enabled,
                setValue: value => Config.Enabled = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Return Water To Watering Can",
                tooltip: () => "Don't consume water when the watering can doesn't work.",
                getValue: () => Config.ReturnWaterToWateringCan,
                setValue: value => Config.ReturnWaterToWateringCan = value
            );
        }
    }
}
