using AutomateToolSwap;
using Core;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Tools;

namespace InteractionRules;

/// <summary>s
/// Implements the swap rules for monsters.
/// </summary>
public class MonstersInteractionRules
{
    /// <summary>
    /// Checks monster-related conditions and tries to swap to the correct tool/item appropriately.
    /// </summary>
    /// <param name="location">The current game location.</param>
    /// <param name="tile">The tile being checked.</param>
    /// <param name="player">The current player.</param>
    /// <returns>True if a swap was performed; otherwise, false.</returns>
    public static bool TrySwap(GameLocation location, Vector2 tile, Farmer player)
    {
        if (!ModEntry.Config.WeaponOnMonsters)
            return false;
        bool currentItemIsNull = player.CurrentItem == null;
        string currentItemName = player.CurrentItem?.Name ?? "";

        // Check if the player's current item is not a bomb or staircase. (So they can use it in the mines without problem)
        bool currentItemIsNotForMine = currentItemIsNull ||
                                  !currentItemName.Contains("Bomb") && !currentItemName.Contains("Staircase");

        foreach (var character in location.characters)
        {
            if (character.IsMonster && Vector2.Distance(tile, character.Tile) < ModEntry.Config.MonsterRangeDetection)
            {
                // Special case: RockCrab handling.
                if (character is RockCrab crab)
                {
                    if (ModEntry.Config.IgnoreCrabs)
                        return true;
                    var isShellLess = ModEntry.Instance.Helper.Reflection.GetField<NetBool>(crab, "shellGone").GetValue();
                    if (!isShellLess.Value && !crab.isMoving())
                    {
                        InventoryHandler.SetTool(player, typeof(Pickaxe));
                        return true;
                    }
                }

                // Ignore slimes on farm if configured.
                if ((location is Farm || location is SlimeHutch) &&
                    ModEntry.Config.IgnoreSlimesOnFarm && character is GreenSlime)
                    return true;

                // Switch to weapon if not holding Bombs or Stairs.
                if (currentItemIsNull || currentItemIsNotForMine)
                {
                    InventoryHandler.SetTool(player, typeof(MeleeWeapon), "Weapon");
                    return true;
                }
                return true;
            }
        }
        return false;
    }

    public static void AlternativeWeaponForMonsters()
    {
        Vector2 tile = Game1.player.Tile;
        foreach (var monster in Game1.currentLocation.characters)
        {

            if (monster is RockCrab)
                break;

            Vector2 monsterTile = monster.Tile;
            float distance = Vector2.Distance(tile, monsterTile);

            if (monster.IsMonster && distance < ModEntry.Config.MonsterRangeDetection && Game1.player.canMove)
            {
                if (TrySwap(Game1.currentLocation, tile, Game1.player))
                    return;
            }

        }
        return;
    }
}
