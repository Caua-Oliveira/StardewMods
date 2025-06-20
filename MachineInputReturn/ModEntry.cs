using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Microsoft.Xna.Framework;


namespace MachineInputReturn;

public class ModEntry : Mod
{
    private ModConfig _config;
    private MachineManager _machineManager;

    public override void Entry(IModHelper helper)
    {
        _config = helper.ReadConfig<ModConfig>();
        _machineManager = new MachineManager(Monitor, _config);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.World.ObjectListChanged += OnObjectListChanged;
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
            text: () => "Machine Input Return"
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Enabled",
            tooltip: () => "If checked, this mod's features are enabled.",
            getValue: () => _config.Enabled,
            setValue: value => _config.Enabled = value
        );

        configMenu.AddTextOption(
            mod: ModManifest,
            name: () => "Retrieval Time Restriction",
            tooltip: () => "When can you retrieve input items from a machine after removing it?\n- Anytime: Always able to retrieve\n- Below 50%: Only if more than half the time remains\n- Below 30%: Only if more than 70% of the time remains.",
            getValue: () => _config.RetrievalTime.ToString(),
            setValue: value => _config.RetrievalTime = Enum.TryParse<RetrievalTimeOption>(value, out var result) ? result : RetrievalTimeOption.Anytime,
            allowedValues: new[] { "Anytime", "Below50Percent", "Below30Percent" },
            formatAllowedValue: value =>
            {
                return value switch
                {
                    "Anytime" => "Anytime",
                    "Below50Percent" => "Below 50% Remaining",
                    "Below30Percent" => "Below 30% Remaining",
                    _ => value
                };
            }
        );
    }

    private void OnObjectListChanged(object sender, ObjectListChangedEventArgs e)
    {
        _machineManager.OnObjectListChanged(e);
    }

    private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
    {
        if (!_config.Enabled || !Context.IsWorldReady || Game1.player == null)
            return;

        // Only react to right mouse button or controller equivalent for using tools
        if (e.Button != SButton.MouseRight && !e.Button.IsActionButton())
            return;

        var player = Game1.player;
        Vector2? tile = null;

        if (e.Button == SButton.MouseRight)
        {
            // Mouse: Use cursor position
            tile = Helper.Input.GetCursorPosition().GrabTile;
        }
        else if (e.Button.IsActionButton())
        {
            // Controller: Use front of player tile
            tile = new Vector2(
                (int)(player.GetToolLocation().X / Game1.tileSize),
                (int)(player.GetToolLocation().Y / Game1.tileSize)
            );
        }

        if (tile.HasValue)
        {
            _machineManager.TryRetrieveInputItemAtTile(tile.Value, Game1.currentLocation, player);
        }
    }
}