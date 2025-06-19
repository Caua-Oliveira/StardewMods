using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace MachinesDropsItems
{
    public class ModEntry : Mod
    {
        private ModConfig _config;
        private MachineManager _machineManager;

        public override void Entry(IModHelper helper)
        {
            _config = helper.ReadConfig<ModConfig>();
            _machineManager = new MachineManager(Monitor, _config);

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                mod: ModManifest,
                reset: () => _config = new ModConfig(),
                save: () => Helper.WriteConfig(_config)
            );

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => "Machine Drops Items Config"
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enabled",
                tooltip: () => "If checked, this mod's features are enabled.",
                getValue: () => _config.Enabled,
                setValue: value => _config.Enabled = value
            );

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Retrieval Time Limit",
                tooltip: () => "Sets the time limit for retrieving an item. The machine is blocked after 1/X of the time has passed. 1 = Always retrievable. 2 = Before halfway. 5 = Only in the first 20% of the time.",
                getValue: () => _config.RetrievalTimeDivider,
                setValue: value => _config.RetrievalTimeDivider = value,
                min: 1,
                max: 5,
                interval: 1
            );

            this.Monitor.Log("Machine checker mod launched!", LogLevel.Info);
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            _machineManager.OnButtonPressed(e);
        }
    }
}