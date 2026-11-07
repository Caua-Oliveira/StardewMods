using Configuration;
using Integrations;
using Microsoft.Xna.Framework;
using StardewValley;

namespace Core;

internal sealed record SwapContext(Farmer Player, GameLocation Location, Vector2 Tile,
    ModConfig Config, ModIntegrations Integrations);
