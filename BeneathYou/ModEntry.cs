using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace BeneathYou;

/// <summary>SMAPI mod entry point for Beneath You.</summary>
public sealed class ModEntry : Mod
{
    private ModConfig Config = new();
    private readonly PerScreen<ScreenState> ScreenState = new(() => new ScreenState());

    public override void Entry(IModHelper helper)
    {
        this.Config = helper.ReadConfig<ModConfig>();

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        helper.Events.Player.Warped += this.OnWarped;

        var harmony = new Harmony(this.ModManifest.UniqueID);
        HarmonyPatches.Apply(harmony, this.Monitor, () => this.Config, this.ScreenState);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        if (this.Helper.ModRegistry.IsLoaded("vgperson.RangedTools"))
            this.Monitor.Log("Detected vgperson.RangedTools; Beneath You Harmony ordering and shared draw coordination enabled.", LogLevel.Trace);

        GenericModConfigMenuIntegration.Register(
            manifest: this.ModManifest,
            helper: this.Helper,
            getConfig: () => this.Config,
            setConfig: config => this.Config = config,
            saveConfig: () => this.Helper.WriteConfig(this.Config)
        );
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        ScreenState state = this.ScreenState.Value;
        Farmer? player = Game1.player;

        if (state.ActiveBeneathYouSwing && (player is null || !player.UsingTool))
            state.ActiveBeneathYouSwing = false;

        PlayerPushService.UpdateSlide(player, state);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.ResetScreenState();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.ResetScreenState();
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (e.IsLocalPlayer)
            this.ResetScreenState();
    }

    private void ResetScreenState()
    {
        ScreenState state = this.ScreenState.Value;
        state.ActivePush = null;
        state.ActiveBeneathYouSwing = false;
        state.IgnoreSelfCollisionFarmer = null;
    }
}

