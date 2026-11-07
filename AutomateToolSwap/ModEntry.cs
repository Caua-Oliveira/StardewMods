using Configuration;
using Core;
using Integrations;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace AutomateToolSwap;

public class ModEntry : Mod
{
    internal static ModConfig Config { get; private set; } = null!;
    private readonly PerScreen<ScreenState> screens = new(() => new ScreenState());
    private ModIntegrations integrations = null!;
    private SwapController controller = null!;

    public override void Entry(IModHelper helper)
    {
        SetConfig(helper.ReadConfig<ModConfig>());
        integrations = new(helper, Monitor);
        controller = new(helper, integrations);
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.Input.ButtonPressed += OnButtonPressed;
        helper.Events.Input.ButtonsChanged += OnButtonsChanged;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => screens.ResetAllScreens();
        helper.Events.GameLoop.SaveLoaded += (_, _) => integrations.RefreshRanges();
        helper.Events.GameLoop.DayStarted += (_, _) => integrations.RefreshRanges();
        helper.Events.Display.MenuChanged += (_, e) =>
        {
            if (e.NewMenu == null)
                integrations.RefreshRanges();
        };
        helper.Events.Player.Warped += (_, e) =>
        {
            if (ReferenceEquals(e.Player, Game1.player))
                screens.Value = new();
        };
    }

    internal static void SetConfig(ModConfig config)
    {
        config.ItemBlacklist ??= new();
        config.ToggleKey ??= KeybindList.Parse("CapsLock");
        config.LastToolKey ??= KeybindList.Parse("MouseMiddle");
        config.BlacklistKey ??= KeybindList.Parse("LeftControl + L");
        config.SwapKey ??= KeybindList.Parse("MouseLeft");
        config.MonsterRangeDetection = Math.Clamp(config.MonsterRangeDetection, 1, 10);
        Config = config;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        integrations.Initialize();
        ConfigSetup.SetupConfig(Helper, this);
    }

    private static bool TryGetPlayer(out Farmer player)
    {
        player = Game1.player;
        return Context.IsWorldReady && player?.currentLocation != null;
    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!TryGetPlayer(out var player) || Game1.activeClickableMenu != null)
            return;
        if (Config.ToggleKey.JustPressed())
        {
            Config.Enabled = !Config.Enabled;
            screens.Value.History.CancelReturn();
            Game1.addHUDMessage(new HUDMessage("AutomateToolSwap " + Helper.Translation.Get(Config.Enabled ? "mod.Enabled" : "mod.Disabled"), 2));
            return;
        }
        if (Config.BlacklistKey.JustPressed())
        {
            screens.Value.History.CancelReturn();
            if (player.CurrentItem is { } item)
            {
                bool removed = Config.ItemBlacklist.Remove(item.QualifiedItemId);
                if (!removed)
                    Config.ItemBlacklist.Add(item.QualifiedItemId);
                Game1.addHUDMessage(new HUDMessage(removed ? $"Removed {item.DisplayName} from blacklist" : $"Added {item.DisplayName} to blacklist", 2));
                Helper.WriteConfig(Config);
            }
            else
                Game1.addHUDMessage(new HUDMessage("No item held to add/remove", 3));
            return;
        }
        if (Config.LastToolKey.JustPressed())
        {
            controller.ReturnToPrevious(player, Config, screens.Value);
            return;
        }
        if (Config.RequireClick && Config.UseDifferentSwapKey && Config.SwapKey.JustPressed())
            controller.OnAction(player, Config, screens.Value);
    }

    [EventPriority(EventPriority.High)]
    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!TryGetPlayer(out var player) || !Config.RequireClick || Config.UseDifferentSwapKey
            || Config.ToggleKey.JustPressed() || Config.BlacklistKey.JustPressed() || Config.LastToolKey.JustPressed())
            return;
        if (e.Button.IsUseToolButton())
            controller.OnAction(player, Config, screens.Value);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!TryGetPlayer(out var player))
            return;
        bool toolHeld = Helper.Input.IsDown(SButton.ControllerX)
            || Game1.options.useToolButton.Any(button => Helper.Input.IsDown(button.ToSButton()));
        bool customKeyHeld = Config.UseDifferentSwapKey && Config.SwapKey.IsDown();
        controller.Update(player, Config, screens.Value, Game1.currentGameTime.ElapsedGameTime, toolHeld, customKeyHeld);
    }
}
