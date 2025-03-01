using StardewValley;
using StardewValley.Tools;

namespace AutomateToolSwap.Core
{
    internal class InventoryHandler
    {
        /// <summary>
        /// Finds the index of the item necessary for the action and tries to swap to it.
        /// </summary>
        /// <param name="player">The player object (Farmer).</param>
        /// <param name="category">The item's category, as each one has different rules for swapping.</param>
        /// <param name="item">The item that it needs to swap to OR it needs to ignore.</param>
        /// <param name="crops">What type of crops we can swap to for Kegs, Jars and Dehydrator.</param>
        /// <param name="aux">The item.category.value (as we can't get the category name untranslated).</param>
        public static void SetItem(Farmer player, string category, string item = "", string crops = "Both", int aux = 0)
        {
            ModEntry.inventoryIndexMemory.canSwitch = ModEntry.Config.AutoReturnToLastTool;
            var items = player.Items;

            switch (category)
            {
                case "Trash":
                case "Fertilizer":
                    for (int i = 0; i < player.maxItems.Value; i++)
                    {
                        if (items[i] != null && items[i].category.Value == aux && !items[i].Name.Contains(item))
                        {
                            if (player.CurrentToolIndex != i)
                                ModEntry.inventoryIndexMemory.SwitchIndex(i, player);
                            return;
                        }
                    }
                    return;

                case "Minerals":
                    for (int i = 0; i < player.maxItems.Value; i++)
                    {
                        if (items[i] != null &&
                            items[i].category.Value == -15 &&
                            items[i].Name.Contains(item) &&
                            items[i].Stack >= 5)
                        {
                            if (player.CurrentToolIndex != i)
                                ModEntry.inventoryIndexMemory.SwitchIndex(i, player);
                            return;
                        }
                    }
                    return;

                case "Cask":
                    for (int i = 0; i < player.maxItems.Value; i++)
                    {
                        if (items[i] != null &&
                            items[i].category.Value == -26 &&
                            (items[i].Name.Contains("Cheese") || items[i].Name.Contains("Wine")))
                        {
                            if (player.CurrentToolIndex != i)
                                ModEntry.inventoryIndexMemory.SwitchIndex(i, player);
                            return;
                        }
                    }
                    return;

                case "Seed":
                    for (int i = 0; i < player.maxItems.Value; i++)
                    {
                        if (items[i] != null &&
                            items[i].category.Value == -74 &&
                            !items[i].HasContextTag("tree_seed_item"))
                        {
                            if (player.CurrentToolIndex != i)
                                ModEntry.inventoryIndexMemory.SwitchIndex(i, player);
                            return;
                        }
                    }
                    return;

                case "Crops":
                    {
                        bool canFruit = crops == "Both" || crops == "Fruit";
                        bool canVegetable = crops == "Both" || crops == "Vegetable";

                        bool isFruit(Item it) => it != null && it.category.Value == -79;
                        bool isVegetable(Item it) => it != null && it.category.Value == -75;

                        for (int i = 0; i < player.maxItems.Value; i++)
                        {
                            if (items[i] != null &&
                                ((canFruit && isFruit(items[i])) || (canVegetable && isVegetable(items[i]))))
                            {
                                // If current item is already a fruit or vegetable, do nothing.
                                if (isFruit(player.CurrentItem) || isVegetable(player.CurrentItem))
                                    return;

                                if (player.CurrentToolIndex != i)
                                    ModEntry.inventoryIndexMemory.SwitchIndex(i, player);
                                return;
                            }
                        }
                    }
                    return;

                case "Dehydratable":
                    {
                        bool canFruit = crops == "Both" || crops == "Fruit";
                        bool canMushroom = crops == "Both" || crops == "Mushroom";

                        bool isFruit(Item it) => it != null && it.category.Value == -79;
                        bool isMushroom(Item it) => it != null && it.category.Value == -81 && it.Name != "Red Mushroom";

                        for (int i = 0; i < player.maxItems.Value; i++)
                        {
                            if (items[i] != null &&
                                ((canFruit && isFruit(items[i])) || (canMushroom && isMushroom(items[i]))))
                            {
                                if (isFruit(player.CurrentItem) || isMushroom(player.CurrentItem))
                                    return;

                                if (player.CurrentToolIndex != i)
                                    ModEntry.inventoryIndexMemory.SwitchIndex(i, player);
                                return;
                            }
                        }
                    }
                    return;

                default:
                    // Handles any other item
                    for (int i = 0; i < player.maxItems.Value; i++)
                    {
                        if (items[i] != null &&
                            items[i].category.Value == aux &&
                            items[i].Name.Contains(item))
                        {
                            if (player.CurrentToolIndex != i)
                                ModEntry.inventoryIndexMemory.SwitchIndex(i, player);
                            return;
                        }
                    }
                    return;
            }
        }

        /// <summary>
        /// Finds the index of the tool necessary for the action and tries to swap to it.
        /// </summary>
        /// <param name="player">The player object (Farmer).</param>
        /// <param name="toolType">The tool type (any from StardewValley.Tools).</param>
        /// <param name="aux">
        /// When swapping to a MeleeWeapon, specify if it needs to be a weapon, a scythe or weapon/scythe combination,
        /// or only a scythe ("Weapon", "Scythe", "ScytheOnly").
        /// </param>
        /// <param name="anyTool">If true, allow swapping to any tool (such as Axe, Pickaxe, or Hoe).</param>
        public static void SetTool(Farmer player, Type toolType, string aux = "", bool anyTool = false)
        {
            ModEntry.inventoryIndexMemory.canSwitch = ModEntry.Config.AutoReturnToLastTool;
            var items = player.Items;

            // If the current tool already matches and RequireClick is disabled, do nothing.
            if (player.Items[ModEntry.inventoryIndexMemory.currentIndex] != null &&
                player.Items[ModEntry.inventoryIndexMemory.currentIndex].GetType() == toolType &&
                !ModEntry.Config.RequireClick)
                return;

            switch (toolType)
            {
                case Type t when t == typeof(MeleeWeapon):
                    switch (aux)
                    {
                        case "Scythe":
                        case "ScytheOnly":
                            for (int i = 0; i < player.maxItems.Value; i++)
                            {
                                if (items[i] != null &&
                                    items[i].GetType() == toolType &&
                                    items[i].Name.Contains("Scythe"))
                                {
                                    if (player.CurrentToolIndex != i)
                                        ModEntry.inventoryIndexMemory.SwitchIndex(i, player);
                                    return;
                                }
                            }
                            // If we only want a scythe and none is found, exit.
                            if (aux == "ScytheOnly")
                                return;
                            // Otherwise, fall through to check for non-scythe melee weapons.
                            goto default;

                        default:
                            for (int i = 0; i < player.maxItems.Value; i++)
                            {
                                if (items[i] != null &&
                                    items[i].GetType() == toolType &&
                                    !items[i].Name.Contains("Scythe"))
                                {
                                    if (player.CurrentToolIndex != i)
                                        ModEntry.inventoryIndexMemory.SwitchIndex(i, player);
                                    return;
                                }
                            }
                            return;
                    }

                default:
                    // Handles any other tool
                    for (int i = 0; i < player.maxItems.Value; i++)
                    {
                        if ((items[i] != null && items[i].GetType() == toolType) ||
                            (anyTool && items[i] is Axe or Pickaxe or Hoe))
                        {
                            if (player.CurrentToolIndex != i)
                                ModEntry.inventoryIndexMemory.SwitchIndex(i, player);
                            return;
                        }
                    }
                    break;
            }
        }
    }
}
