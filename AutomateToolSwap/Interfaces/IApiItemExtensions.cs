using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;
using System.Collections.Generic;

namespace AutomateToolSwap
{
    public interface IApiItemExtensions
    {
        bool IsStone(string id);
        bool IsResource(string id, out int? health, out string itemDropped);
        bool IsClump(string qualifiedItemId);
        bool HasBehavior(string qualifiedItemId, string target);
        bool TrySpawnClump(string itemId, Vector2 position, string locationName, out string error, bool avoidOverlap = false);
        bool TrySpawnClump(string itemId, Vector2 position, GameLocation location, out string error, bool avoidOverlap = false);
        List<string> GetCustomSeeds(string itemId, bool includeSource, bool parseConditions = true);
        void CheckClumpDrops(ResourceClump clump, bool remove = false);
        void CheckObjectDrops(Object node, bool remove = false);
        bool GetResourceData(string id, bool isClump, out object data);
        bool GetBreakingTool(string id, bool isClump, out string tool);
    }
}