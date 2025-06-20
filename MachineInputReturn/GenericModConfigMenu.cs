using StardewModdingAPI;
using StardewModdingAPI.Utilities;


namespace MachineInputReturn;

/// <summary>The API which lets other mods add a config UI through Generic Mod Config Menu.</summary>
public interface IGenericModConfigMenuApi
{
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

    void AddSectionTitle(IManifest mod, Func<string> text, Func<string> tooltip = null);

    void AddParagraph(IManifest mod, Func<string> text);

    void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);

    void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string> tooltip = null, int? min = null, int? max = null, int? interval = null, Func<int, string> formatValue = null, string fieldId = null);

    void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string> tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string> formatValue = null, string fieldId = null);

    void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string> tooltip = null, string[] allowedValues = null, Func<string, string> formatAllowedValue = null, string fieldId = null);

    void AddKeybind(IManifest mod, Func<SButton> getValue, Action<SButton> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);

    void AddKeybindList(IManifest mod, Func<KeybindList> getValue, Action<KeybindList> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);

    void AddPage(IManifest mod, string pageId, Func<string> pageTitle = null);

    void AddPageLink(IManifest mod, string pageId, Func<string> text, Func<string> tooltip = null);

    void OnFieldChanged(IManifest mod, Action<string, object> onChange);

    void Unregister(IManifest mod);
}