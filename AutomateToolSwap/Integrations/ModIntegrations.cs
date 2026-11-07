using System.Text.Json;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;

namespace Integrations;

/// <summary>Optional integrations. No configuration file I/O occurs in the update loop.</summary>
internal sealed class ModIntegrations
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private IItemExtensionsApi? itemExtensions;
    private string? rangedConfigPath;
    private string? lastRangeError;
    private Dictionary<string, int> ranges = new();
    public bool HasRangedTools { get; private set; }
    public bool HasTractor { get; private set; }

    public ModIntegrations(IModHelper helper, IMonitor monitor)
    {
        this.helper = helper;
        this.monitor = monitor;
    }

    public void Initialize()
    {
        HasTractor = helper.ModRegistry.IsLoaded("Pathoschild.TractorMod");
        itemExtensions = helper.ModRegistry.GetApi<IItemExtensionsApi>("mistyspring.ItemExtensions");
        HasRangedTools = helper.ModRegistry.IsLoaded("vgperson.RangedTools");
        if (HasRangedTools)
        {
            // SMAPI's public IModInfo doesn't expose install paths. Isolate the optional metadata
            // lookup here, once at launch; manifest discovery is a fallback if SMAPI changes it.
            try
            {
                var info = helper.ModRegistry.Get("vgperson.RangedTools");
                string? directory = info == null ? null : helper.Reflection.GetProperty<string>(info, "DirectoryPath", required: false)?.GetValue();
                if (directory != null)
                    rangedConfigPath = Path.Combine(directory, "config.json");
            }
            catch (Exception ex)
            {
                monitor.Log($"Ranged Tools path lookup unavailable: {ex.Message}", LogLevel.Trace);
            }
            rangedConfigPath ??= FindRangedConfig(Path.GetDirectoryName(helper.DirectoryPath)!);
            rangedConfigPath ??= FindRangedConfig(Path.Combine(Constants.GamePath, "Mods"));
        }
        RefreshRanges();
    }

    private string? FindRangedConfig(string directory)
    {
        try
        {
            string manifestPath = Path.Combine(directory, "manifest.json");
            if (File.Exists(manifestPath))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath), JsonOptions);
                if (doc.RootElement.TryGetProperty("UniqueID", out var id)
                    && string.Equals(id.GetString(), "vgperson.RangedTools", StringComparison.OrdinalIgnoreCase))
                    return Path.Combine(directory, "config.json");
                return null; // Don't search inside another mod.
            }
            foreach (string child in Directory.EnumerateDirectories(directory))
            {
                var info = new DirectoryInfo(child);
                if (info.Name.StartsWith('.') || (info.Attributes & FileAttributes.ReparsePoint) != 0)
                    continue;
                if (FindRangedConfig(child) is { } path)
                    return path;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            monitor.Log($"Couldn't inspect integration folder '{directory}': {ex.Message}", LogLevel.Trace);
        }
        return null;
    }

    private static readonly JsonDocumentOptions JsonOptions = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };

    public void RefreshRanges()
    {
        if (!HasRangedTools || rangedConfigPath == null)
            return;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(rangedConfigPath), JsonOptions);
            var updated = new Dictionary<string, int>();
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (property.Name.EndsWith("Range", StringComparison.Ordinal) && property.Value.ValueKind == JsonValueKind.Number
                    && property.Value.TryGetInt32(out int range) && range >= -1)
                    updated[property.Name] = range;
            }
            ranges = updated;
            lastRangeError = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            if (lastRangeError != ex.Message)
                monitor.Log($"Couldn't read Ranged Tools settings; keeping the last known ranges: {ex.Message}", LogLevel.Warn);
            lastRangeError = ex.Message;
        }
    }

    public int GetRange(Item? item)
    {
        string key = item switch
        {
            Axe => "AxeRange",
            Pickaxe => "PickaxeRange",
            Hoe => "HoeRange",
            WateringCan => "WateringCanRange",
            MeleeWeapon weapon when weapon.isScythe() => "ScytheRange",
            MeleeWeapon => "WeaponRange",
            { Category: -74 } => "SeedRange",
            _ => "ObjectPlaceRange"
        };
        return ranges.GetValueOrDefault(key, 1);
    }

    public string? GetBreakingTool(string id, bool isClump)
    {
        if (itemExtensions == null || string.IsNullOrWhiteSpace(id))
            return null;
        try
        {
            return itemExtensions.GetBreakingTool(id, isClump, out string tool) ? tool : null;
        }
        catch (Exception ex)
        {
            monitor.Log($"Item Extensions integration failed and has been disabled for this session: {ex.Message}", LogLevel.Warn);
            itemExtensions = null;
            return null;
        }
    }
}
