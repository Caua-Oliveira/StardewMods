using StardewValley;
using StardewValley.Tools;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using StardewValley.GameData.Machines;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Events;

namespace MachineInputReturn;

public class MachineManager
{
    private readonly IMonitor _monitor;
    private readonly ModConfig _config;

    public MachineManager(IMonitor monitor, ModConfig config)
    {
        _monitor = monitor;
        _config = config;
    }

    public void OnObjectListChanged(ObjectListChangedEventArgs e)
    {
        if (!_config.Enabled || e.Location == null || !Context.IsPlayerFree)
            return;

        foreach (var obj in e.Removed)
        {
            if (obj.Value is StardewValley.Object machine)
            {
                TryDropInputItem(machine, e.Location);
            }
        }
    }

    /// <summary>
    /// Tries to retrieve the input item from a machine at a given tile.
    /// Called when the player right-clicks (mouse or controller) on a machine.
    /// </summary>
    public void TryRetrieveInputItemAtTile(Vector2 tile, GameLocation location, Farmer player)
    {
        if (location == null) return;
        if (!location.Objects.TryGetValue(tile, out var obj)) return;
        if (obj is not StardewValley.Object machine) return;

        // Must be processing, have input, and have a held object (i.e., is processing)
        if (machine.MinutesUntilReady > 0 && machine.lastInputItem.Value != null && machine.heldObject.Value != null)
        {
            // Time restriction logic
            Item lastInput = machine.lastInputItem.Value;
            int itemQuality = lastInput.Quality;
            MachineData machineData = machine.GetMachineData();
            if (machineData?.OutputRules == null)
            {
                _monitor.Log($"No machine data or output rules found for '{machine.Name}'.", LogLevel.Trace);
                return;
            }

            int itemsToReturn = 0;
            foreach (MachineOutputRule rule in machineData.OutputRules)
            {
                foreach (MachineOutputTriggerRule trigger in rule.Triggers)
                {
                    if (trigger == null) continue;
                    bool itemMatches = false;
                    if (trigger.RequiredItemId != null)
                    {
                        if (trigger.RequiredItemId == lastInput.QualifiedItemId)
                            itemMatches = true;
                    }
                    else if (trigger.RequiredTags != null)
                    {
                        var itemTags = lastInput.GetContextTags().Select(tag => tag.ToLowerInvariant()).ToList();
                        var inclusionTags = trigger.RequiredTags.Where(t => !t.StartsWith("!")).ToList();
                        var exclusionTags = trigger.RequiredTags.Where(t => t.StartsWith("!")).Select(t => t.Substring(1).ToLowerInvariant()).ToList();

                        bool allRequiredMet = inclusionTags.All(t => itemTags.Contains(t.ToLowerInvariant()));
                        bool noExcludedMet = !exclusionTags.Any(t => itemTags.Contains(t));
                        if (allRequiredMet && noExcludedMet)
                            itemMatches = true;
                    }

                    if (itemMatches)
                    {
                        itemsToReturn = trigger.RequiredCount;

                        // Time restriction logic
                        if (machine.MinutesUntilReady > 0 && rule.MinutesUntilReady > 0)
                        {
                            double percentRemaining = (double)machine.MinutesUntilReady / rule.MinutesUntilReady;
                            switch (_config.RetrievalTime)
                            {
                                case RetrievalTimeOption.Below50Percent:
                                    if (percentRemaining < 0.5)
                                    {
                                        _monitor.Log($"Machine '{machine.Name}' is past the retrieval point (below 50%). Cannot retrieve item.", LogLevel.Info);
                                        return;
                                    }
                                    break;
                                case RetrievalTimeOption.Below30Percent:
                                    if (percentRemaining < 0.3)
                                    {
                                        _monitor.Log($"Machine '{machine.Name}' is past the retrieval point (below 30%). Cannot retrieve item.", LogLevel.Info);
                                        return;
                                    }
                                    break;
                            }
                        }
                        break;
                    }
                }
                if (itemsToReturn > 0)
                    break;
            }

            if (itemsToReturn > 0)
            {
                // Give items directly to player, or drop at tile if inventory full
                int itemsGiven = 0;
                for (int i = 0; i < itemsToReturn; i++)
                {
                    var itemToGive = lastInput.getOne();
                    itemToGive.Quality = itemQuality;

                    if (!player.addItemToInventoryBool(itemToGive))
                    {
                        Game1.createObjectDebris(lastInput.ItemId, (int)tile.X, (int)tile.Y, itemQuality: itemQuality);
                    }
                    else
                    {
                        itemsGiven++;
                    }
                }
                // Stop machine processing
                machine.heldObject.Value = null;
                machine.MinutesUntilReady = 0;
                machine.readyForHarvest.Value = false;
                machine.showNextIndex.Value = false;
                machine.ResetParentSheetIndex();


                _monitor.Log($"Returned {itemsGiven} of {lastInput.Name} (Quality: {itemQuality}) to player from '{machine.Name}'.", LogLevel.Trace);
            }
            else
            {
                _monitor.Log($"No matching output rule found for {lastInput.Name} in '{machine.Name}'.", LogLevel.Trace);
            }
        }
    }

    /// <summary>
    /// Drops the input item when the machine is removed, as before.
    /// </summary>
    private void TryDropInputItem(StardewValley.Object machine, GameLocation location)
    {
        if (machine.MinutesUntilReady > 0 && machine.lastInputItem.Value != null && machine.heldObject.Value != null)
        {
            Item lastInput = machine.lastInputItem.Value;
            int itemQuality = lastInput.Quality;
            MachineData machineData = machine.GetMachineData();
            if (machineData?.OutputRules == null)
            {
                _monitor.Log($"No machine data or output rules found for '{machine.Name}'.", LogLevel.Trace);
                return;
            }

            int itemsToReturn = 0;
            foreach (MachineOutputRule rule in machineData.OutputRules)
            {
                foreach (MachineOutputTriggerRule trigger in rule.Triggers)
                {
                    if (trigger == null) continue;
                    bool itemMatches = false;
                    if (trigger.RequiredItemId != null)
                    {
                        if (trigger.RequiredItemId == lastInput.QualifiedItemId)
                            itemMatches = true;
                    }
                    else if (trigger.RequiredTags != null)
                    {
                        var itemTags = lastInput.GetContextTags().Select(tag => tag.ToLowerInvariant()).ToList();
                        var inclusionTags = trigger.RequiredTags.Where(t => !t.StartsWith("!")).ToList();
                        var exclusionTags = trigger.RequiredTags.Where(t => t.StartsWith("!")).Select(t => t.Substring(1).ToLowerInvariant()).ToList();

                        bool allRequiredMet = inclusionTags.All(t => itemTags.Contains(t.ToLowerInvariant()));
                        bool noExcludedMet = !exclusionTags.Any(t => itemTags.Contains(t));
                        if (allRequiredMet && noExcludedMet)
                            itemMatches = true;
                    }

                    if (itemMatches)
                    {
                        itemsToReturn = trigger.RequiredCount;

                        // Time restriction logic
                        if (machine.MinutesUntilReady > 0 && rule.MinutesUntilReady > 0)
                        {
                            double percentRemaining = (double)machine.MinutesUntilReady / rule.MinutesUntilReady;
                            switch (_config.RetrievalTime)
                            {
                                case RetrievalTimeOption.Below50Percent:
                                    if (percentRemaining < 0.5)
                                    {
                                        _monitor.Log($"Machine '{machine.Name}' is past the retrieval point (below 50%). Cannot retrieve item.", LogLevel.Info);
                                        return;
                                    }
                                    break;
                                case RetrievalTimeOption.Below30Percent:
                                    if (percentRemaining < 0.3)
                                    {
                                        _monitor.Log($"Machine '{machine.Name}' is past the retrieval point (below 30%). Cannot retrieve item.", LogLevel.Info);
                                        return;
                                    }
                                    break;
                            }
                        }
                        break;
                    }
                }
                if (itemsToReturn > 0)
                    break;
            }

            if (itemsToReturn > 0)
            {
                for (int i = 0; i < itemsToReturn; i++)
                {
                    Game1.createObjectDebris(lastInput.ItemId, ((int)machine.TileLocation.X), ((int)machine.TileLocation.Y), itemQuality: itemQuality);
                }
                _monitor.Log($"Dropped {itemsToReturn} of {lastInput.Name} (Quality: {itemQuality}) from '{machine.Name}'.", LogLevel.Trace);
            }
            else
            {
                _monitor.Log($"No matching output rule found for {lastInput.Name} in '{machine.Name}'.", LogLevel.Trace);
            }
        }
    }
}