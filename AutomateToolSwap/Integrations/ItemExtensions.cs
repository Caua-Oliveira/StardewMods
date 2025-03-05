namespace Integrations;

/// <summary>The API that interacts with the objects from the mod Item Extensions.</summary>
public interface IItemExtensionsApi
{
    bool IsStone(string id);
    bool IsResource(string id, out int? health, out string itemDropped);
    bool IsClump(string qualifiedItemId);
    bool HasBehavior(string qualifiedItemId, string target);
    bool GetResourceData(string id, bool isClump, out object data);
    bool GetBreakingTool(string id, bool isClump, out string tool);
}