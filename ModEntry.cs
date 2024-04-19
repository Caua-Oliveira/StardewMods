
using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;



namespace AutomateToolSwap
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance { get; set; } = null!;
        internal static ModConfig Config { get; private set; } = null!; // Declare static instance of ModConfig
        internal static Check check { get; private set; } = null!;
        internal static bool isTractorModInstalled;
        public override void Entry(IModHelper helper)
        {
            isTractorModInstalled = Helper.ModRegistry.IsLoaded("Pathoschild.TractorMod");
            Instance = this;
            Config = Helper.ReadConfig<ModConfig>();
            check = new Check(this);

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            Config.Enabled = true;
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu == null) { return; }

            configMenu.Register(
                mod: this.ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(Config)
            );

            // Add the general settings
            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => "General Settings"
            );

            // Keybind for toggling mod on/off
            configMenu.AddKeybindList(
                mod: this.ModManifest,
                name: () => "Toggle Mod Keybind",
                tooltip: () => "The keybind to toggle the mod on or off.",
                getValue: () => Config.ToggleKey,
                setValue: keybinds => Config.ToggleKey = keybinds
            );

            // Keybind to switch back to last used tool
            configMenu.AddKeybindList(
                mod: this.ModManifest,
                name: () => "Last Tool Keybind",
                tooltip: () => "The keybind to switch back to the last used tool.",
                getValue: () => Config.LastToolKey,
                setValue: keybinds => Config.LastToolKey = keybinds
            );

            // Detection method for tool switching
            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "Tool Selection Mode",
                tooltip: () => "Choose how tools are switched: 'Cursor' uses the mouse pointer, 'Player' uses the player's orientation. (USE THIS FOR CONTROLLER)",
                allowedValues: new string[] { "Cursor", "Player" },
                getValue: () => Config.DetectionMethod,
                setValue: method => Config.DetectionMethod = method
            );

            // Auto-return to last tool after switching
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Auto-Return to Last Tool",
                tooltip: () => "Automatically return to the previously used tool after swapping.",
                getValue: () => Config.AutoReturnToLastTool,
                setValue: isEnabled => Config.AutoReturnToLastTool = isEnabled
            );

            // Switch to hoe when clicking on empty soil
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Hoe for Empty Soil",
                tooltip: () => "Automatically switch to the hoe when clicking on empty soil.",
                getValue: () => Config.HoeForEmptySoil,
                setValue: isEnabled => Config.HoeForEmptySoil = isEnabled
            );

            // Switch to scythe when clicking on grass
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Scythe for Grass",
                tooltip: () => "Automatically switch to the scythe when clicking on grass.",
                getValue: () => Config.ScytheForGrass,
                setValue: isEnabled => Config.ScytheForGrass = isEnabled
            );

            // Prioritize pickaxe over watering can
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Pickaxe Over Watering Can",
                tooltip: () => "Prioritize using the pickaxe instead of switching to the watering can on dry soil.",
                getValue: () => Config.PickaxeOverWateringCan,
                setValue: isEnabled => Config.PickaxeOverWateringCan = isEnabled
            );

            // Use pickaxe for weeds instead of scythe
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Pickaxe for Weeds",
                tooltip: () => "Automatically switch to the pickaxe instead of the scythe when clicking on weeds (fibers).",
                getValue: () => Config.AnyToolForWeeds,
                setValue: isEnabled => Config.AnyToolForWeeds = isEnabled
            );

            // Add the Tractor settings
            if (isTractorModInstalled)
            {
                configMenu.AddSectionTitle(
                    mod: this.ModManifest,
                    text: () => "Tractor Settings"
                );

                // Prioritize pickaxe over watering can
                configMenu.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => "Disable Auto Swap in Tractor",
                    tooltip: () => "Disables Auto Swap in Tractor ALWAYS, otherwise you can use the Toggle Keybind to disable it",
                    getValue: () => Config.DisableTractorSwap,
                    setValue: isEnabled => Config.DisableTractorSwap = isEnabled
                );

            }

        }

        IndexSwitcher switcher = new IndexSwitcher(0);

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!isTractorModInstalled || Config.DisableTractorSwap || (!Config.Enabled && !Config.DisableTractorSwap))
            {
                return;
            }
            //Code for Tractor Mod
            if (Game1.player.isRidingHorse() && Game1.player.mount.Name.Contains("tractor"))
            {
                Farmer player = Game1.player;
                GameLocation currentLocation = Game1.currentLocation;
                ICursorPosition cursorPos = this.Helper.Input.GetCursorPosition();
                Vector2 cursorTile = cursorPos.GrabTile;
                Vector2 toolLocation = new Vector2((int)Game1.player.GetToolLocation().X / Game1.tileSize, (int)Game1.player.GetToolLocation().Y / Game1.tileSize);

                switch (Config.DetectionMethod)
                {
                    case "Cursor":
                        CheckTile(currentLocation, cursorTile, player);
                        break;
                    case "Player":
                        CheckTile(currentLocation, toolLocation, player);
                        break;
                }
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
            {
                return;
            }

            // turns mod on/off
            if (Config.ToggleKey.JustPressed())
            {
                Config.Enabled = !Config.Enabled;
                if (Config.Enabled)
                {
                    Game1.addHUDMessage(new HUDMessage("AutomateToolSwap ENABLED", 2));
                }
                Game1.addHUDMessage(new HUDMessage("AutomateToolSwap DISABLED", 2));
                Game1.hudMessages.First().timeLeft = 1200;
            }

            // swap to the last item
            if (Config.LastToolKey.JustPressed() && Game1.player.canMove)
            {
                switcher.GoToLastIndex();
            }

            // check if the mod should try to swap
            bool buttonMatched = false;
            foreach (var button in Game1.options.useToolButton)
            {
                if (e.Button == button.ToSButton() || e.Button == SButton.ControllerX)
                {
                    buttonMatched = true;
                    break;
                }
            }
            if (!buttonMatched || !Config.Enabled || !(Game1.player.canMove))
                return;



            Farmer player = Game1.player;
            GameLocation currentLocation = Game1.currentLocation;
            ICursorPosition cursorPos = this.Helper.Input.GetCursorPosition();
            Vector2 cursorTile = cursorPos.GrabTile;
            Vector2 toolLocation = new Vector2((int)Game1.player.GetToolLocation().X / Game1.tileSize, (int)Game1.player.GetToolLocation().Y / Game1.tileSize);

            switch (Config.DetectionMethod)
            {
                case "Cursor":
                    CheckTile(currentLocation, cursorTile, player);
                    break;
                case "Player":
                    CheckTile(currentLocation, toolLocation, player);
                    break;
            }

        }

        // detects what is in the tile that the player is looking at and calls the function to swap tools
        private void CheckTile(GameLocation location, Vector2 tile, Farmer player)
        {
            if (player.CurrentItem is Slingshot)
                return;

            if (check.Objects(location, tile, player))
                return;

            if (check.TerrainFeatures(location, tile, player))
                return;

            if (check.ResourceClumps(location, tile, player))
                return;

            if (check.Water(location, tile, player))
                return;

            if (check.Monsters(location, tile, player))
                return;

            if (check.Animals(location, tile, player))
                return;

            if (check.ShouldSwapToHoe(location, tile, player))
                return;

        }

        //Looks for the tool necessary for the action
        public void SetTool(Farmer player, Type toolType, string aux = "Scythe", bool anyTool = false)
        {
            switcher.canSwitch = Config.AutoReturnToLastTool;
            var items = player.Items;
            //Melee Weapons \/
            if (toolType == typeof(MeleeWeapon))
            {
                if (aux == "Scythe")
                {
                    for (int i = 0; i < player.maxItems; i++)
                    {
                        if (items[i] != null && items[i].GetType() == toolType && items[i].Name.Contains(aux))
                        {
                            if (player.CurrentToolIndex != i)
                            {
                                switcher.SwitchIndex(i);
                            }
                            return;
                        }
                    }
                }

                for (int i = 0; i < player.maxItems; i++)
                {
                    if (items[i] != null && items[i].GetType() == toolType && !(items[i].Name.Contains("Scythe")))
                    {
                        if (player.CurrentToolIndex != i)
                        {
                            switcher.SwitchIndex(i);
                        }
                        return;
                    }
                }
                return;
            }

            //Any other tool \/

            for (int i = 0; i < player.maxItems; i++)
            {
                if ((items[i] != null && items[i].GetType() == toolType) || (anyTool && items[i] is Axe or Pickaxe or Hoe))
                {
                    if (player.CurrentToolIndex != i)
                    {
                        switcher.SwitchIndex(i);
                    }
                    return;
                }
            }

        }



        //Any item \/
        public void SetItem(Farmer player, string categorie, string item)
        {
            switcher.canSwitch = Config.AutoReturnToLastTool;
            var items = player.Items;
            //Handles trash
            if (categorie == "Trash")
            {
                for (int i = 0; i < player.maxItems; i++)
                {

                    if (items[i] != null && items[i].getCategoryName() == categorie && !(items[i].Name.Contains(item)))
                    {
                        if (player.CurrentToolIndex != i)
                        {
                            switcher.SwitchIndex(i);
                        }
                        return;
                    }
                }
                return;
            }

            //Handles resources
            if (categorie == "Resource")
            {
                for (int i = 0; i < player.maxItems; i++)
                {

                    if (items[i] != null && items[i].getCategoryName() == categorie && items[i].Name.Contains(item) && items[i].Stack >= 5)
                    {
                        if (player.CurrentToolIndex != i)
                        {
                            switcher.SwitchIndex(i);
                        }
                        return;
                    }
                }
                return;
            }

            //Handles any other item
            for (int i = 0; i < player.maxItems; i++)
            {

                if (items[i] != null && items[i].getCategoryName() == categorie && items[i].Name.Contains(item))
                {
                    if (player.CurrentToolIndex != i)
                    {
                        switcher.SwitchIndex(i);
                    }
                    return;
                }
            }
            return;
        }

    }
}



