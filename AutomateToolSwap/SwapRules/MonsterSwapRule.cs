using AutomateToolSwap;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Tools;

namespace AutomateToolSwap.SwitchRules
{
    /// <summary>
    /// Implements the switch rule for monsters.
    /// If a monster is detected within a configured range, the mod swaps to a weapon.
    /// </summary>
    public class MonsterSwapRule : ISwapRule
    {
        ModConfig config = ModEntry.Config;
        /// <summary>
        /// Checks for monsters near the specified tile and switches to an appropriate weapon.
        /// </summary>
        /// <param name="location">The current game location.</param>
        /// <param name="tile">The tile being checked.</param>
        /// <param name="player">The current player.</param>
        /// <returns>True if a switch occurred; otherwise, false.</returns>
        public bool TrySwap(GameLocation location, Vector2 tile, Farmer player)
        {
            bool currentItemIsNull = player.CurrentItem == null;
            string currentItemName = player.CurrentItem?.Name ?? "";
            int currentItemCategory = player.CurrentItem?.category.Value ?? 0;
          
            // Check if the player's current item is not a bomb or staircase. (So they can use it in the mines without problem)
            bool currentItemIsNotForMine = currentItemIsNull ||
                                      (!currentItemName.Contains("Bomb") && !currentItemName.Contains("Staircase"));

            foreach (var character in location.characters)
            {
                if (character.IsMonster && Vector2.Distance(tile, character.Tile) < config.MonsterRangeDetection)
                {
                    // Special case: RockCrab handling.
                    if (character is RockCrab crab)
                    {
                        if (config.IgnoreCrabs)
                            return true;
                        var isShellLess = ModEntry.Instance.Helper.Reflection.GetField<NetBool>(crab, "shellGone").GetValue();
                        if (!isShellLess.Value && !crab.isMoving())
                        {
                            ModEntry.SetTool(player, typeof(Pickaxe));
                            return true;
                        }
                    }

                    // Ignore slimes on farm if configured.
                    if ((location is Farm || location is SlimeHutch) &&
                        config.IgnoreSlimesOnFarm && character is GreenSlime)
                        return true;

                    // Switch to weapon if not holding Bombs or Stairs.
                    if (currentItemIsNull || currentItemIsNotForMine)
                    {
                        ModEntry.SetTool(player, typeof(MeleeWeapon), "Weapon");
                        return true;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
