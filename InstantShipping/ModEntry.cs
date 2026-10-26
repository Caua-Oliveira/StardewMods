using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewValley.Locations;
using System;
using System.Collections.Generic;
using System.Linq;
using xTile.Dimensions;
using xTile.Tiles;

namespace InstantShipping
{
    /// <summary>
    /// Instant shipping mod (simplified). Ships the hovered item (or cursor item) directly to the farm shipping bin.
    /// Inventory container abstraction removed; works directly with IList&lt;Item&gt;.
    /// </summary>
    public class ModEntry : Mod
    {
        private ModConfig Config;
        private IGenericModConfigMenuApi Gmcm;

        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            TryRegisterGmcm();
        }

        private void TryRegisterGmcm()
        {
            Gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (Gmcm is null)
                return;

            Gmcm.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            Gmcm.AddBoolOption(
                ModManifest,
                name: () => "Enable Mod",
                tooltip: () => "Enable or disable this mod.",
                getValue: () => Config.Enabled,
                setValue: v => Config.Enabled = v
            );

            Gmcm.AddTextOption(
                ModManifest,
                name: () => "Default Ship Mode",
                tooltip: () => "What to ship with no modifiers. one | all | custom stack",
                getValue: () => Config.DefaultShipMode,
                setValue: v => Config.DefaultShipMode = (v ?? "all").Trim(),
                allowedValues: new[] { "one", "all", "custom stack" }
            );

            Gmcm.AddNumberOption(
                ModManifest,
                name: () => "Custom Ship Amount",
                tooltip: () => "Used only if Default Ship Mode = custom stack.",
                getValue: () => Config.DefaultShipStackAmount,
                setValue: v => Config.DefaultShipStackAmount = Math.Max(1, v),
                min: 1,
                max: 999
            );

            Gmcm.AddKeybind(
                ModManifest,
                name: () => "Shipping Key",
                tooltip: () => "Press this while hovering an item in an inventory/chest to ship it.",
                getValue: () => Config.SendToShippingBinKeybind,
                setValue: v => Config.SendToShippingBinKeybind = v
            );

            // New: Restrict shipping to presence / distance of a shipping bin
            Gmcm.AddBoolOption(
                ModManifest,
                name: () => "Require Nearby Shipping Bin",
                tooltip: () => "If enabled, you can only instant-ship when a shipping bin exists in the current location (and is within the distance below).",
                getValue: () => Config.AllowShippingOnlyIfMapThereIsBin,
                setValue: v => Config.AllowShippingOnlyIfMapThereIsBin = v
            );

            Gmcm.AddBoolOption(
                ModManifest,
                name: () => "Update shipped collection isntantly",
                tooltip: () => "Immediately flag item as 'shipped' in the Collections page when sent.",
                getValue: () => Config.ImmediateCollectionUpdate,
                setValue: v => Config.ImmediateCollectionUpdate = v
            );
            Gmcm.AddNumberOption(
                ModManifest,
                name: () => "Allowed Distance To Bin",
                tooltip: () => "Maximum tile distance to a shipping bin (if 'Require Nearby Shipping Bin' is enabled).",
                getValue: () => Config.AllowedDistanceToBin,
                setValue: v => Config.AllowedDistanceToBin = Math.Max(1, v),
                min: 1,
                max: 200
            );

            Gmcm.AddParagraph(ModManifest, () =>
                "Modifiers:\n" +
                "  Shift: = Ship entire stack\n" +
                "  Ctrl  = Ship exactly one\n" +
                "  Alt   = Ship custom amount"
            );
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || !Config.Enabled)
                return;
            if (e.Button != Config.SendToShippingBinKeybind)
                return;

            var (item, sourceInventory) = GetHoveredItemContext();
            if (item is null)
                return;

            if (!(item is StardewValley.Object o && o.canBeShipped()))
            {
                Game1.showRedMessage("That item can't be shipped.");
                return;
            }

            int toShip = DetermineShipCount(item);
            TryShipItem(item, toShip, sourceInventory);
        }

        private bool TryShipItem(Item originalItem, int count, IList<Item> sourceInventory)
        {
            if (originalItem is null || count <= 0)
                return false;

            if (Config.AllowShippingOnlyIfMapThereIsBin && !IsNearShippingBin(out string failReason))
            {
                Game1.showRedMessage(failReason ?? "No shipping bin nearby.");
                return false;
            }

            Item toSend;
            if (count >= originalItem.Stack)
            {
                count = originalItem.Stack;
                toSend = originalItem;
            }
            else
            {
                var clone = originalItem.getOne();
                if (clone is null)
                {
                    Game1.showRedMessage("Clone failed.");
                    return false;
                }
                clone.Stack = count;
                toSend = clone;
            }

            try
            {
                Game1.getFarm().shipItem(toSend, Game1.player);

                if (toSend == originalItem)
                    RemoveOriginalItem(sourceInventory, originalItem);
                else
                {
                    originalItem.Stack -= count;
                    if (originalItem.Stack <= 0)
                        RemoveOriginalItem(sourceInventory, originalItem);
                }

                if (Config.ImmediateCollectionUpdate)
                    Game1.player.shippedBasic(originalItem.ItemId, 0);

                Game1.playSound("Ship");
                return true;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Ship error: {ex}", LogLevel.Error);
                Game1.showRedMessage("Shipping failed.");
                return false;
            }
        }

        private void ConsolidateShippingBin(Farmer who)
        {
            try
            {
                var bin = Game1.getFarm().getShippingBin(who);
                for (int i = 0; i < bin.Count; i++)
                {
                    if (bin[i] is null) continue;
                    for (int j = bin.Count - 1; j > i; j--)
                    {
                        if (bin[j] is null) continue;
                        if (bin[i].canStackWith(bin[j]))
                        {
                            int space = bin[i].maximumStackSize() - bin[i].Stack;
                            if (space <= 0) break;

                            int move = Math.Min(space, bin[j].Stack);
                            bin[i].Stack += move;
                            bin[j].Stack -= move;
                            if (bin[j].Stack <= 0)
                                bin.RemoveAt(j);
                            if (bin[i].Stack >= bin[i].maximumStackSize())
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to consolidate shipping bin: {ex}", LogLevel.Trace);
            }
        }

        private void RemoveOriginalItem(IList<Item> sourceInventory, Item item)
        {
            if (sourceInventory == null)
            {
                if (Game1.player.CursorSlotItem == item)
                    Game1.player.CursorSlotItem = null;
                return;
            }

            int index = sourceInventory.IndexOf(item);
            if (index < 0) return;

            if (ReferenceEquals(sourceInventory, Game1.player.Items))
                sourceInventory[index] = null;
            else
                sourceInventory.RemoveAt(index);
        }

        private int DetermineShipCount(Item item)
        {
            bool shift = Helper.Input.IsDown(SButton.LeftShift) || Helper.Input.IsDown(SButton.RightShift);
            bool ctrl = Helper.Input.IsDown(SButton.LeftControl) || Helper.Input.IsDown(SButton.RightControl);
            bool alt = Helper.Input.IsDown(SButton.LeftAlt) || Helper.Input.IsDown(SButton.RightAlt);

            if (alt && !ctrl && !shift)
                return Math.Min(item.Stack, Math.Max(1, Config.DefaultShipStackAmount));
            if (shift && !ctrl && !alt)
                return item.Stack;
            if (ctrl && !shift && !alt)
                return 1;

            return Config.DefaultShipMode switch
            {
                "one" => 1,
                "custom stack" => Math.Min(item.Stack, Math.Max(1, Config.DefaultShipStackAmount)),
                _ => item.Stack
            };
        }

        private (Item item, IList<Item> sourceInventory) GetHoveredItemContext()
        {
            IClickableMenu active = Game1.activeClickableMenu;
            if (active is GameMenu gm)
                active = gm.GetCurrentPage();

            if (Game1.player.CursorSlotItem != null && active == null)
                return (Game1.player.CursorSlotItem, null);

            Item hovered = null;

            if (active is ItemGrabMenu igm)
            {
                hovered = Game1.player.CursorSlotItem ?? SafeReflect<Item>(igm, "hoveredItem");
                if (hovered != null)
                {
                    if (igm.ItemsToGrabMenu?.actualInventory?.Contains(hovered) == true)
                    {
                        if (active.ToString() == "StardewValley.Menus.ItemGrabMenu")
                            return (hovered, igm.ItemsToGrabMenu.actualInventory);
                        return (null, null);
                    }

                    if (Game1.player.Items.Contains(hovered))
                        return (hovered, Game1.player.Items);
                    if (Game1.player.CursorSlotItem == hovered)
                        return (hovered, null);
                }
            }
            else if (active is InventoryPage invPage)
            {
                hovered = Game1.player.CursorSlotItem ?? SafeReflect<Item>(invPage, "hoveredItem");
                if (hovered != null)
                {
                    if (Game1.player.Items.Contains(hovered))
                        return (hovered, Game1.player.Items);
                    if (Game1.player.CursorSlotItem == hovered)
                        return (hovered, null);
                }
            }

            return (null, null);
        }

        private T SafeReflect<T>(object source, string field) where T : class
        {
            if (source is null) return null;
            try
            {
                return Helper.Reflection.GetField<T>(source, field, required: false)?.GetValue();
            }
            catch
            {
                return null;
            }
        }


        /// <summary>
        /// Determines if there's a shipping bin (vanilla shipping bin BUILDING or mini-shipping-bin object)
        /// in the current location within Config.AllowedDistanceToBin tiles of the player.
        /// </summary>
        private bool IsNearShippingBin(out string failReason)
        {
            failReason = null;

            GameLocation loc = Game1.currentLocation;
            if (loc is null)
            {
                failReason = "No location.";
                return false;
            }

            int maxDist = Math.Max(1, Config.AllowedDistanceToBin);
            Vector2 playerTile = Game1.player.Tile;

            bool found = false;

            // 1. Check for ShippingBin buildings (player-placeable or farm default).

            foreach (Building b in loc.buildings)
            {
                if (b is ShippingBin)
                {
                    // Compute closest distance from player to the rectangle of the building footprint.
                    int startX = b.tileX.Value;
                    int startY = b.tileY.Value;
                    int endX = startX + b.tilesWide.Value - 1;
                    int endY = startY + b.tilesHigh.Value - 1;

                    // Clamp player tile inside rectangle to get nearest tile.
                    float clampedX = Math.Max(startX, Math.Min(playerTile.X, endX));
                    float clampedY = Math.Max(startY, Math.Min(playerTile.Y, endY));

                    float dist = Vector2.Distance(playerTile, new Vector2(clampedX, clampedY));
                    if (dist <= maxDist)
                    {
                        found = true;
                        break;
                    }
                }
            }


            // 2. Check for mini shipping bin objects.
            if (!found && loc.Objects?.Count() > 0)
            {
                foreach (var obj in loc.Objects.Values)
                {
                    if (obj is null)
                        continue;

                    if (obj.bigCraftable.Value && obj.Name.Equals("Mini-Shipping Bin", StringComparison.OrdinalIgnoreCase))
                    {
                        float dist = Vector2.Distance(playerTile, obj.TileLocation);
                        if (dist <= maxDist)
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }

            if (!found)
            {
                failReason = "No shipping bin nearby.";
                return false;
            }

            return true;
        }
    }
}