using System;
using System.Collections.Generic;
using System.Linq;
using AutomateToolSwap;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.Tools;

namespace AutomateToolSwap.SwitchRules
{
    /// <summary>
    /// Implements the switch rule for in-game objects (buildings, crafting stations, etc.).
    /// Checks object characteristics and names to swap to the appropriate tool or item.
    /// </summary>
    public class ObjectSwapRule : ISwapRule
    {
        ModConfig config = ModEntry.Config;
        /// <summary>
        /// Checks the object at the specified tile and swaps the player's tool/item accordingly.
        /// </summary>
        /// <param name="location">The current game location.</param>
        /// <param name="tile">The tile position that was clicked.</param>
        /// <param name="player">The player performing the action.</param>
        /// <returns>True if a switch occurred; otherwise, false.</returns>
        public bool TrySwap(GameLocation location, Vector2 tile, Farmer player)
        {

            // Retrieve the object at the specified tile.
            var obj = location.getObjectAtTile((int)tile.X, (int)tile.Y);
            if (obj == null)
                return false;

            bool currentItemIsNull = player.CurrentItem == null;
            string currentItemName = player.CurrentItem?.Name ?? "";
            int currentItemCategory = player.CurrentItem?.category.Value ?? 0;


            // Check if the player's current item is not a breaking tool. (So they can break objects without swapping to something else)
            bool currentItemCantBreak = player.CurrentItem is not Pickaxe or Axe;
            // Check if the player's current item is not a bomb or staircase. (So they can use it in the mines without problem)
            bool currentItemIsNotForMine = currentItemIsNull || 
                                      (!currentItemName.Contains("Bomb") && !currentItemName.Contains("Staircase"));

            // If the ItemExtensions API is available, try to get the breaking tool for modded objects.
            if (ModEntry.ItemExtensionsAPI != null)
            {
                bool isClump = ModEntry.ItemExtensionsAPI.IsClump(obj.ItemId);
                bool foundTool = ModEntry.ItemExtensionsAPI.GetBreakingTool(obj.ItemId, isClump, out string tool);
                if (foundTool)
                {
                    if (tool == "Pickaxe")
                    {
                        if (config.PickaxeForStoneAndOres && currentItemIsNotForMine)
                        {
                            ModEntry.SetTool(player, typeof(Pickaxe));
                        }
                    }
                    else if (tool == "Axe")
                    {
                        if (config.AxeForTwigs)
                        {
                            ModEntry.SetTool(player, typeof(Axe));
                        }
                    }
                    return true; // Indicate that a tool was found and potentially set.
                }
                return false; // Indicate that no tool was found.
            }

            // Check object characteristics using its methods.
            if (obj.IsWeeds())
            {
                if (config.AnyToolForWeeds && location is not MineShaft)
                {
                    ModEntry.SetTool(player, typeof(Pickaxe), anyTool: true);
                }
                else if (config.ScytheForWeeds)
                {
                    ModEntry.SetTool(player, typeof(MeleeWeapon), "Scythe");
                }
                return true;
            }
            if (obj.IsBreakableStone())
            {
                if (config.PickaxeForStoneAndOres && currentItemIsNotForMine)
                {
                    ModEntry.SetTool(player, typeof(Pickaxe));
                }
                return true;
            }
            if (obj.IsTwig())
            {
                if (config.AxeForTwigs)
                {
                    ModEntry.SetTool(player, typeof(Axe));
                }
                return true;
            }
            if (obj.isForage())
            {
                if (config.ScytheForForage)
                {
                    ModEntry.SetTool(player, typeof(MeleeWeapon), "ScytheOnly");
                }
                return true;
            }
            if (obj is CrabPot crabPot && crabPot.bait.Value is null)
            {
                if (config.BaitForCrabPot)
                {
                    ModEntry.SetItem(player, "Bait", "Bait", aux: -21);
                }
                return true;
            }

            // Check object names to swap to specific items or tools.
            switch (obj.Name)
            {
                case "Furnace":
                    if (config.OresForFurnaces && currentItemCantBreak && 
                       (currentItemIsNull || !currentItemName.Contains("Ore")))
                    {
                        ModEntry.SetItem(player, "Resource", "Ore");
                    }
                    return true;
                case "Heavy Furnace":
                    if (config.OresForFurnaces && currentItemCantBreak && 
                       (currentItemIsNull || !currentItemName.Contains("Ore")))
                    {
                        ModEntry.SetItem(player, "Resource", "Ore");
                    }
                    return true;
                case "Cheese Press":
                    if (config.MilkForCheesePress && currentItemCantBreak)
                    {
                        ModEntry.SetItem(player, "Animal Product", "Milk", aux: -6);
                    }
                    return true;
                case "Mayonnaise Machine":
                    if (config.EggsForMayoMachine && currentItemCantBreak)
                    {
                        ModEntry.SetItem(player, "Animal Product", "Egg", aux: -5);
                    }
                    return true;
                case "Artifact Spot":
                    if (config.HoeForArtifactSpots)
                    {
                        ModEntry.SetTool(player, typeof(Hoe));
                    }
                    return true;
                case "Garden Pot":
                    if (config.WateringCanForGardenPot &&
                        (currentItemIsNull || currentItemCantBreak && currentItemCategory != -74))
                    {
                        ModEntry.SetTool(player, typeof(WateringCan));
                    }
                    return true;
                case "Seed Spot":
                    if (config.HoeForArtifactSpots)
                    {
                        ModEntry.SetTool(player, typeof(Hoe));
                    }
                    return true;
                case "Barrel":
                    if (config.WeaponForMineBarrels)
                    {
                        ModEntry.SetTool(player, typeof(MeleeWeapon), "Weapon");
                    }
                    return true;
                case "Supply Crate":
                    if (config.AnyToolForSupplyCrates)
                    {
                        ModEntry.SetTool(player, typeof(Hoe), anyTool: true);
                    }
                    return true;
                case "Recycling Machine":
                    if (config.TrashForRecycling && currentItemCantBreak)
                    {
                        ModEntry.SetItem(player, "Trash", "Joja", aux: -20);
                    }
                    return true;
                case "Bone Mill":
                    if (config.BoneForBoneMill && currentItemCantBreak)
                    {
                        ModEntry.SetItem(player, "Resource", "Bone Fragment");
                    }
                    return true;
                case "Loom":
                    if (config.WoolForLoom && currentItemCantBreak)
                    {
                        ModEntry.SetItem(player, "Animal Product", "Wool", aux: -18);
                    }
                    return true;
                case "Fish Smoker":
                    if (config.FishForSmoker && currentItemCategory != -4 && currentItemCantBreak)
                    {
                        ModEntry.SetItem(player, "Fish", aux: -4);
                    }
                    return true;
                case "Bait Maker":
                    if (config.FishForBaitMaker && currentItemCantBreak)
                    {
                        ModEntry.SetItem(player, "Fish", aux: -4);
                    }
                    return true;
                case "Crystalarium":
                    if (config.MineralsForCrystalarium &&
                        (currentItemIsNull || currentItemCantBreak && currentItemCategory != -2))
                    {
                        ModEntry.SetItem(player, "Mineral", aux: -2);
                    }
                    return true;
                case "Seed Maker":
                    if (config.SwapForSeedMaker && currentItemCantBreak)
                    {
                        ModEntry.SetItem(player, "Crops");
                    }
                    return true;
                case "Keg":
                    if (currentItemCantBreak && config.SwapForKegs != "None")
                    {
                        ModEntry.SetItem(player, "Crops", crops: config.SwapForKegs);
                    }
                    return true;
                case "Preserves Jar":
                    if (currentItemCantBreak && config.SwapForPreservesJar != "None")
                    {
                        ModEntry.SetItem(player, "Crops", crops: config.SwapForPreservesJar);
                    }
                    return true;
                case "Dehydrator":
                    if (currentItemCantBreak && config.SwapForDehydrator != "None")
                    {
                        ModEntry.SetItem(player, "Dehydratable", crops: config.SwapForDehydrator);
                    }
                    return true;
                case "Oil Maker":
                    if (currentItemCantBreak && config.TruffleForOilMaker)
                    {
                        ModEntry.SetItem(player, "Oil Maker", "Truffle", aux: -17);
                    }
                    return true;
                default:
                    return true;
            }
        }
    }
}
