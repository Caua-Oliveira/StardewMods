using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using AutomateToolSwap.SwitchRules;

namespace AutomateToolSwap
{
    /// <summary>
    /// Aggregates all switch rules and invokes them sequentially.
    /// Only the first rule that applies will trigger a tool/item switch.
    /// </summary>
    public class SwapManager
    {
        /// <summary>
        /// List of registered switch rules.
        /// </summary>
        private readonly List<ISwapRule> swapRules;

        /// <summary>
        /// Initializes a new instance of the SwitchManager class and registers all rules.
        /// </summary>
        public SwapManager()
        {
            swapRules = new List<ISwapRule>
            {
                new ObjectSwapRule(),
                new ResourceClumpSwapRule(),
                new TerrainFeatureSwapRule(),
                new MonsterSwapRule(),
                new WaterSwapRule(),
                new AnimalSwapRule(),
                new DiggableSoilSwapRule()
            };
        }

        /// <summary>
        /// Iterates over all registered switch rules to attempt a tool/item switch.
        /// </summary>
        /// <param name="location">The current game location.</param>
        /// <param name="tile">The tile that was clicked.</param>
        /// <param name="player">The current player.</param>
        /// <returns>True if any rule performed a switch; otherwise, false.</returns>
        public bool TrySwapAll(GameLocation location, Vector2 tile, Farmer player)
        {
            foreach (var rule in swapRules)
            {
                if (rule.TrySwap(location, tile, player))
                    return true;
            }
            return false;
        }
    }
}
