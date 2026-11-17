using System;
using StardewModdingAPI;

namespace BeneathYou;

/// <summary>Generic Mod Config Menu API subset used by Beneath You.</summary>
public interface IGenericModConfigMenuApi
{
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
    void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);
    void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string>? tooltip = null, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null);
}

/// <summary>Registers Beneath You settings with Generic Mod Config Menu when installed.</summary>
internal static class GenericModConfigMenuIntegration
{
    private static readonly string[] PushDirectionValues =
    {
        nameof(PushDirectionPreference.MovementThenStepBack),
        nameof(PushDirectionPreference.StepBackFirst),
        nameof(PushDirectionPreference.StepForwardFirst),
        nameof(PushDirectionPreference.FacingDirectionOnly)
    };

    private static readonly string[] PushMovementStyleValues =
    {
        nameof(PushMovementStyle.SmoothSlide),
        nameof(PushMovementStyle.InstantSnap)
    };

    public static void Register(
        IManifest manifest,
        IModHelper helper,
        Func<ModConfig> getConfig,
        Action<ModConfig> setConfig,
        Action saveConfig)
    {
        var api = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api is null)
            return;

        api.Register(
            mod: manifest,
            reset: () => setConfig(new ModConfig()),
            save: saveConfig
        );

        api.AddSectionTitle(
            mod: manifest,
            text: () => helper.Translation.Get("config.section.general")
        );

        api.AddBoolOption(
            mod: manifest,
            name: () => helper.Translation.Get("config.enabled.name"),
            tooltip: () => helper.Translation.Get("config.enabled.tooltip"),
            getValue: () => getConfig().Enabled,
            setValue: value => getConfig().Enabled = value
        );

        api.AddSectionTitle(
            mod: manifest,
            text: () => helper.Translation.Get("config.section.solid-push")
        );

        api.AddBoolOption(
            mod: manifest,
            name: () => helper.Translation.Get("config.solid-placement.name"),
            tooltip: () => helper.Translation.Get("config.solid-placement.tooltip"),
            getValue: () => getConfig().AllowSolidPlacementUnderPlayer,
            setValue: value => getConfig().AllowSolidPlacementUnderPlayer = value
        );

        api.AddTextOption(
            mod: manifest,
            name: () => helper.Translation.Get("config.push-direction.name"),
            tooltip: () => helper.Translation.Get("config.push-direction.tooltip"),
            getValue: () => getConfig().GetPushDirectionPreference().ToString(),
            setValue: value => getConfig().PreferredPushDirection = value,
            allowedValues: PushDirectionValues,
            formatAllowedValue: value => helper.Translation.Get($"config.push-direction.{value}")
        );

        api.AddBoolOption(
            mod: manifest,
            name: () => helper.Translation.Get("config.diagonal-push.name"),
            tooltip: () => helper.Translation.Get("config.diagonal-push.tooltip"),
            getValue: () => getConfig().AllowDiagonalPush,
            setValue: value => getConfig().AllowDiagonalPush = value
        );

        api.AddTextOption(
            mod: manifest,
            name: () => helper.Translation.Get("config.push-style.name"),
            tooltip: () => helper.Translation.Get("config.push-style.tooltip"),
            getValue: () => getConfig().GetPushMovementStyle().ToString(),
            setValue: value => getConfig().PushMovementStyle = value,
            allowedValues: PushMovementStyleValues,
            formatAllowedValue: value => helper.Translation.Get($"config.push-style.{value}")
        );

        api.AddBoolOption(
            mod: manifest,
            name: () => helper.Translation.Get("config.face-placed.name"),
            tooltip: () => helper.Translation.Get("config.face-placed.tooltip"),
            getValue: () => getConfig().FacePlacedItemAfterPush,
            setValue: value => getConfig().FacePlacedItemAfterPush = value
        );
    }
}




