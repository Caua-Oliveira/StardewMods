
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
using System.Collections.Immutable;
using xTile.Dimensions;
using xTile.Tiles;



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
            if (configMenu == null)
                return;

            configMenu.Register(
                mod: this.ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(Config)
            );

            // Add the general settings
            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => Helper.Translation.Get("config.detectionSettings.title")
            );

            // If you should use the customizable SwapKey or the game default
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.useCustomSwapKey.name"),
                tooltip: () => Helper.Translation.Get("config.useCustomSwapKey.tooltip"),
                getValue: () => Config.UseDifferentSwapKey,
                setValue: isEnabled => Config.UseDifferentSwapKey = isEnabled
            );

            // Keybind for swapping tools
            configMenu.AddKeybindList(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.swapKey.name"),
                tooltip: () => Helper.Translation.Get("config.swapKey.tooltip"),
                getValue: () => Config.SwapKey,
                setValue: keybinds => Config.SwapKey = keybinds
            );

            // Keybind for toggling mod on/off
            configMenu.AddKeybindList(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.toggleKey.name"),
                tooltip: () => Helper.Translation.Get("config.toggleKey.tooltip"),
                getValue: () => Config.ToggleKey,
                setValue: keybinds => Config.ToggleKey = keybinds
            );

            // Keybind to switch back to last used tool
            configMenu.AddKeybindList(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.lastToolKey.name"),
                tooltip: () => Helper.Translation.Get("config.lastToolKey.tooltip"),
                getValue: () => Config.LastToolKey,
                setValue: keybinds => Config.LastToolKey = keybinds
            );

            // Detection method for tool switching
            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.detectionMethod.name"),
                tooltip: () => Helper.Translation.Get("config.detectionMethod.tooltip"),
                allowedValues: new string[] {
                    "Cursor",
                    "Player"
                },
                getValue: () => Config.DetectionMethod,
                setValue: method => Config.DetectionMethod = method
            );

            // Auto-return to last tool after switching
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.autoReturnToLastTool.name"),
                tooltip: () => Helper.Translation.Get("config.autoReturnToLastTool.tooltip"),
                getValue: () => Config.AutoReturnToLastTool,
                setValue: isEnabled => Config.AutoReturnToLastTool = isEnabled
            );

            // Add the general settings
            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => Helper.Translation.Get("config.customSwapsSettings.title")
            );

            // Switch to Weapon when clicking monsters
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.weaponOnMonsters.name"),
                tooltip: () => Helper.Translation.Get("config.weaponOnMonsters.tooltip"),
                getValue: () => Config.WeaponOnMonsters,
                setValue: isEnabled => Config.WeaponOnMonsters = isEnabled
            );

            // Alternative method to swapping on Monsters
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.alternativeWeaponOnMonsters.name"),
                tooltip: () => Helper.Translation.Get("config.alternativeWeaponOnMonsters.tooltip"),
                getValue: () => Config.AlternativeWeaponOnMonsters,
                setValue: isEnabled => Config.AlternativeWeaponOnMonsters = isEnabled
            );

            // Add a NumberOption for MonsterRangeDetections 
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.monsterRangeDetection.name"),
                tooltip: () => Helper.Translation.Get("config.monsterRangeDetection.tooltip"),
                getValue: () => Config.MonsterRangeDetection,
                setValue: value => Config.MonsterRangeDetection = value,
                min: 1,
                max: 10
            );

            // Switch to hoe when clicking on empty soil
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.hoeForEmptySoil.name"),
                tooltip: () => Helper.Translation.Get("config.hoeForEmptySoil.tooltip"),
                getValue: () => Config.HoeForEmptySoil,
                setValue: isEnabled => Config.HoeForEmptySoil = isEnabled
            );

            // Switch to any Seed when clicking on tilled dirt
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.seedForTilledDirt.name"),
                tooltip: () => Helper.Translation.Get("config.seedForTilledDirt.tooltip"),
                getValue: () => Config.SeedForTilledDirt,
                setValue: isEnabled => Config.SeedForTilledDirt = isEnabled
            );

            // Switch to scythe when clicking on grass
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.scytheForGrass.name"),
                tooltip: () => Helper.Translation.Get("config.scytheForGrass.tooltip"),
                getValue: () => Config.ScytheForGrass,
                setValue: isEnabled => Config.ScytheForGrass = isEnabled
            );

            // Prioritize pickaxe over watering can
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.pickaxeOverWateringCan.name"),
                tooltip: () => Helper.Translation.Get("config.pickaxeOverWateringCan.tooltip"),
                getValue: () => Config.PickaxeOverWateringCan,
                setValue: isEnabled => Config.PickaxeOverWateringCan = isEnabled
            );

            // Use pickaxe for weeds instead of scythe
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.anyToolForWeeds.name"),
                tooltip: () => Helper.Translation.Get("config.anyToolForWeeds.tooltip"),
                getValue: () => Config.AnyToolForWeeds,
                setValue: isEnabled => Config.AnyToolForWeeds = isEnabled
            );

            // Switch to fishing rod when clicking on water
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.fishingRodOnWater.name"),
                tooltip: () => Helper.Translation.Get("config.fishingRodOnWater.tooltip"),
                getValue: () => Config.FishingRodOnWater,
                setValue: isEnabled => Config.FishingRodOnWater = isEnabled
            );

            // Switch to watering can when clicking on water
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.wateringCanOnWater.name"),
                tooltip: () => Helper.Translation.Get("config.wateringCanOnWater.tooltip"),
                getValue: () => Config.WateringCanOnWater,
                setValue: isEnabled => Config.WateringCanOnWater = isEnabled
            );

            // Disable swap on growing trees
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.ignoreGrowingTrees.name"),
                tooltip: () => Helper.Translation.Get("config.ignoreGrowingTrees.tooltip"),
                getValue: () => Config.IgnoreGrowingTrees,
                setValue: isEnabled => Config.IgnoreGrowingTrees = isEnabled
            );

            // Swaps to Crops for Seed Maker
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.swapForSeedMaker.name"),
                tooltip: () => Helper.Translation.Get("config.swapForSeedMaker.tooltip"),
                getValue: () => Config.SwapForSeedMaker,
                setValue: isEnabled => Config.SwapForSeedMaker = isEnabled
            );

            // Swaps to Crops for Kegs
            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.swapForKegs.name"),
                tooltip: () => Helper.Translation.Get("config.swapForKegs.tooltip"),
                allowedValues: new string[] {
                    "None",
                    "Fruit",
                    "Vegetable",
                    "Both"
                },
                getValue: () => Config.SwapForKegs,
                setValue: type => Config.SwapForKegs = type
            );

            // Swaps to Crops for Preserves Jar
            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.swapForPreservesJar.name"),
                tooltip: () => Helper.Translation.Get("config.swapForPreservesJar.tooltip"),
                allowedValues: new string[] {
                    "None",
                    "Fruit",
                    "Vegetable",
                    "Both"
                },
                getValue: () => Config.SwapForPreservesJar,
                setValue: type => Config.SwapForPreservesJar = type
            );

            // Add the Tractor settings
            if (isTractorModInstalled)
            {
                configMenu.AddSectionTitle(
                    mod: this.ModManifest,
                    text: () => Helper.Translation.Get("config.disableTractorSwap.name")
                );

                // Disable auto swap in tractor
                configMenu.AddBoolOption(
                    mod: this.ModManifest,
                    name: () => Helper.Translation.Get("config.disableTractorSwap.name"),
                    tooltip: () => Helper.Translation.Get("config.disableTractorSwap.tooltip"),
                    getValue: () => Config.DisableTractorSwap,
                    setValue: isEnabled => Config.DisableTractorSwap = isEnabled
                );
            }
        }

        IndexSwitcher switcher = new IndexSwitcher(0);

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            //Alternative "Weapon for Monsters"
            if (Config.AlternativeWeaponOnMonsters && Config.WeaponOnMonsters)
            {
                Vector2 tile = Game1.player.Tile;
                foreach (var monster in Game1.currentLocation.characters)
                {
                    Vector2 monsterTile = monster.Tile;
                    float distance = Vector2.Distance(tile, monsterTile);

                    if (monster.IsMonster && distance < Config.MonsterRangeDetection && Game1.player.canMove)
                    {
                        if (check.Monsters(Game1.currentLocation, tile, Game1.player))
                            return;
                    }

                }

            }

            //Code for Tractor Mod
            if (!isTractorModInstalled || Config.DisableTractorSwap || (!Config.Enabled && !Config.DisableTractorSwap))
                return;

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
                return;

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
            if (!ButtonMatched(e) || !Config.Enabled || !(Game1.player.canMove))
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
            if (Config.AlternativeWeaponOnMonsters && player.CurrentItem is MeleeWeapon && !player.CurrentItem.Name.Contains("Scythe"))
                return;

            if (player.CurrentItem is Slingshot)
                return;

            if (check.Objects(location, tile, player))
                return;

            if (check.ResourceClumps(location, tile, player))
                return;

            if (check.TerrainFeatures(location, tile, player))
                return;

            if (check.Water(location, tile, player))
                return;

            if (Config.WeaponOnMonsters && !Config.AlternativeWeaponOnMonsters)
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
        public void SetItem(Farmer player, string categorie, string item = "", string crops = "Both")
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
                            switcher.SwitchIndex(i);

                        return;
                    }
                }
                return;
            }

            //Handles Crops
            if (categorie == "Crops")
            {
                bool canFruit = crops == "Both" || crops == "Fruit";
                bool canVegetable = crops == "Both" || crops == "Vegetable";

                for (int i = 0; i < player.maxItems; i++)
                {
                    bool isFruit(Item Item) { return Item != null && Item.getCategoryName() == "Fruit"; }

                    bool isVegetable(Item Item) { return Item != null && Item.getCategoryName() == "Vegetable"; }

                    if (items[i] != null && (canFruit && isFruit(items[i]) || canVegetable && isVegetable(items[i])))
                    {
                        if (isFruit(player.CurrentItem) || isVegetable(player.CurrentItem))
                            return;

                        if (player.CurrentToolIndex != i)
                            switcher.SwitchIndex(i);

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
                    if (player.CurrentItem != null && player.CurrentItem.getCategoryName() == categorie)
                        return;

                    if (player.CurrentToolIndex != i)
                        switcher.SwitchIndex(i);

                    return;
                }
            }
            return;
        }

        public bool ButtonMatched(ButtonPressedEventArgs e)
        {
            if (Config.UseDifferentSwapKey)
            {
                if (Config.SwapKey.JustPressed())
                    return true;
                return false;

            }
            else
            {
                foreach (var button in Game1.options.useToolButton)
                {
                    if (e.Button == button.ToSButton() || e.Button == SButton.ControllerX)
                        return true;

                }
                return false;
            }
        }

    }
}



