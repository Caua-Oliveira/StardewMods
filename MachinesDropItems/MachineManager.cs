using StardewValley;
using StardewValley.Tools;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using StardewValley.GameData.Machines;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Events;

namespace MachinesDropsItems
{
    public class MachineManager
    {
        private readonly IMonitor _monitor;
        private readonly ModConfig _config;

        public MachineManager(IMonitor monitor, ModConfig config)
        {
            _monitor = monitor;
            _config = config;
        }

        public void OnButtonPressed(ButtonPressedEventArgs e)
        {

            if (!_config.Enabled || (!e.Button.IsUseToolButton() && !e.Button.IsActionButton()))
                return;
            if (!Context.IsPlayerFree || Game1.currentLocation == null)
                return;
            Vector2 tile = e.Cursor.GrabTile;
            StardewValley.Object machine = Game1.currentLocation.getObjectAtTile((int)tile.X, (int)tile.Y);
            if (machine == null)
                return;


            // Case 1: Player is placing an item into an empty/idle machine.
            if (machine.heldObject.Value == null && (e.Button.IsActionButton() || e.Button.IsUseToolButton()) && Game1.player.CurrentItem != null)
            {

                HandlePlacingItem(machine, Game1.player);
            }
            // Case 2: Machine is processing, and player hits it with a tool to get the input item back.
            else if (machine.heldObject.Value != null && machine.MinutesUntilReady > 0 && e.Button.IsUseToolButton() && Game1.player.CurrentTool is Pickaxe or Hoe or Axe)
            {
                HandleRetrievingItem(machine, tile);
            }
        }

        private void HandlePlacingItem(StardewValley.Object machine, Farmer player)
        {
            Item currentItem = player.CurrentItem;
            if (currentItem == null) return;

            MachineData machineData = machine.GetMachineData();
            if (machineData?.OutputRules == null) return;

            List<string> currentItemTags = currentItem.GetContextTags().Select(tag => tag.ToLowerInvariant()).ToList();
            foreach (MachineOutputRule rule in machineData.OutputRules)
            {
                foreach (MachineOutputTriggerRule trigger in rule.Triggers)
                {
                    if (trigger == null) continue;

                    bool itemMatches = false;
                    // Check if the rule is for a specific item ID
                    if (trigger.RequiredItemId != null)
                    {
                        if (trigger.RequiredItemId == currentItem.QualifiedItemId)
                        {
                            itemMatches = true;
                        }
                    }
                    // Otherwise, check by context tags
                    else if (trigger.RequiredTags != null)
                    {
                        var inclusionTags = trigger.RequiredTags.Where(t => !t.StartsWith("!")).ToList();
                        var exclusionTags = trigger.RequiredTags.Where(t => t.StartsWith("!")).Select(t => t.Substring(1).ToLowerInvariant()).ToList();

                        // .All() correctly returns true for an empty list, handling cases with only exclusion tags.
                        bool allRequiredMet = inclusionTags.All(t => currentItemTags.Contains(t.ToLowerInvariant()));

                        // ...AND it has NONE of the excluded tags.
                        bool noExcludedMet = !exclusionTags.Any(t => currentItemTags.Contains(t));

                        if (allRequiredMet && noExcludedMet)
                        {
                            itemMatches = true;
                        }
                    }

                    if (itemMatches)
                    {
                        // Check if player has enough items for the recipe
                        if (player.CurrentItem.Stack >= trigger.RequiredCount)
                        {
                            string dataToStore = $"{currentItem.QualifiedItemId};{currentItem.Quality};{trigger.RequiredCount};{rule.MinutesUntilReady}";
                            machine.modData["machines_drops_items"] = dataToStore;
                            _monitor.Log($"Stored recipe data for {currentItem.Name} in '{machine.Name}'. Item will be returned if machine is broken.", LogLevel.Info);

                            // Found a valid rule, no need to check further.
                            return;
                        }
                    }
                }

            }
        }

        private void HandleRetrievingItem(StardewValley.Object machine, Vector2 tile)
        {
            //TODO: SEGURAR CLICK
            if (!machine.modData.ContainsKey("machines_drops_items"))
                return;

            string rawData = machine.modData["machines_drops_items"];
            string[] itemData = rawData.Split(';');

            if (itemData.Length < 4)
            {
                _monitor.Log($"Invalid data format in modData for machine '{machine.Name}'. Removing.", LogLevel.Warn);
                machine.modData.Remove("machines_drops_items");
                return;
            }

            string qualifiedItemId = itemData[0];
            int itemQuality = int.Parse(itemData[1]);
            int itemsToCreate = int.Parse(itemData[2]);
            int timeToComplete = int.Parse(itemData[3]);

            if (_config.RetrievalTimeDivider > 0)
            {
                // If the remaining time is less than the total time divided by the divider, 
                // it means the processing is past the allowed retrieval point.
                if (timeToComplete > 0 && machine.MinutesUntilReady < timeToComplete / _config.RetrievalTimeDivider)
                {
                    _monitor.Log($"Machine '{machine.Name}' is past the retrieval point. Cannot retrieve item.", LogLevel.Info);
                    return;
                }
            }

            Item itemToDrop = ItemRegistry.Create(qualifiedItemId);
            if (itemToDrop != null)
            {
                // Drop the required number of items.
                for (int i = 0; i < itemsToCreate; i++)
                {
                    Game1.createObjectDebris(itemToDrop.ItemId, (int)tile.X, (int)tile.Y, itemQuality: itemQuality);
                }

                _monitor.Log($"Dropped {itemsToCreate} of {itemToDrop.Name} (Quality: {itemQuality}) from '{machine.Name}'.", LogLevel.Info);

                // Reset the machine's state to prevent getting both input and output
                machine.heldObject.Value = null;
                machine.MinutesUntilReady = 0;
                machine.readyForHarvest.Value = false;
            }
            else
            {
                _monitor.Log($"Invalid or missing item ID from modData: '{qualifiedItemId}'", LogLevel.Warn);
            }

            // Clean up the stored data
            machine.modData.Remove("machines_drops_items");
        }
    }
}