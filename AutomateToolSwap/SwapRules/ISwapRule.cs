using Microsoft.Xna.Framework;
using StardewValley;

namespace AutomateToolSwap.SwitchRules
{
    /// <summary>
    /// Defines the interface for a swap rule which checks whether a tool or item swap is required.
    /// </summary>
    public interface ISwapRule
    {
        /// <summary>
        /// Tries to perform a swap based on the game location, tile, and player.
        /// </summary>
        /// <param name="location">The game location.</param>
        /// <param name="tile">The tile position being checked.</param>
        /// <param name="player">The current player.</param>
        /// <returns>True if a swap was performed; otherwise, false.</returns>
        bool TrySwap(GameLocation location, Vector2 tile, Farmer player);
    }
}
