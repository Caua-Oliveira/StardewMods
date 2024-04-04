﻿
using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System.Runtime.Remoting;
using xTile.Dimensions;


namespace AutomateToolSwap
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance { get; private set; } = null!;
        internal static ModConfig Config { get; set; }

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Config = Helper.ReadConfig<ModConfig>();
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.GameLaunched += new EventHandler<GameLaunchedEventArgs>(this.OnGameLaunched);
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

            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => "Toggle On/Off Keybind",
                tooltip: () => "What key you will use to toggle the mod on/off",
                getValue: () => Config.ToggleKey,
                setValue: value => Config.ToggleKey = value
            );

            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => "Swap Tools Keybind",
                tooltip: () => "What keybind you will use to swap tools (Recommended: Key you use to break things)",
                getValue: () => Config.SwapKey,
                setValue: value => Config.SwapKey = value
            );

            configMenu.AddKeybind(
                mod: this.ModManifest,
                name: () => "Return to last tool used",
                tooltip: () => "What key you will use to return to last tool used",
                getValue: () => Config.LastToolButton,
                setValue: value => Config.LastToolButton = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Auto switch to last tool",
                tooltip: () => "Whenever the mod swaps a tool to break something, it automatically swaps back again to the previous tool",
                getValue: () => Config.Auto_switch_last_tool,
                setValue: value => Config.Auto_switch_last_tool = value
            );


            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Switch to Hoe",
                tooltip: () => "Switch to Hoe when clicking empty soil",
                getValue: () => Config.Hoe_in_empty_soil,
                setValue: value => Config.Hoe_in_empty_soil = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Scythe on Grass",
                tooltip: () => "If it should switch to Scythe when clicking grass",
                getValue: () => Config.Scythe_on_grass,
                setValue: value => Config.Scythe_on_grass = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Prioritize Pickaxe over Watercan",
                tooltip: () => "Prioritizes the Pickaxe, if you are holding a pickaxe when clicking an dry soil, it will not change for the watercan",
                getValue: () => Config.Pickaxe_greater_wcan,
                setValue: value => Config.Pickaxe_greater_wcan = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Pickaxe isntead of Scythe",
                tooltip: () => "When clicking on weeds(fibers), it will change for the pickaxe instead",
                getValue: () => Config.Pickaxe_over_melee,
                setValue: value => Config.Pickaxe_over_melee = value
            );
        }

        IndexSwitcher switcher = new IndexSwitcher(0);

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady) { return; }

            // turns mod on/off
            if (e.Button == Config.ToggleKey)
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
            if (e.Button == Config.LastToolButton && Game1.player.canMove) { switcher.GoToLastIndex(); }

            // ignore if player didnt left-click or mod is disabled
            if (e.Button != Config.SwapKey || !Config.Enabled || !(Game1.player.canMove)) { return; }


            Farmer player = Game1.player;
            GameLocation currentLocation = Game1.currentLocation;
            ICursorPosition cursorPos = this.Helper.Input.GetCursorPosition();
            Vector2 cursorTile = cursorPos.GrabTile;
            Vector2 toolLocation = new Vector2((int)Game1.player.GetToolLocation().X / Game1.tileSize, (int)Game1.player.GetToolLocation().Y / Game1.tileSize);
            if (Game1.input.GetGamePadState().IsConnected)
            {
                GetTool(currentLocation, toolLocation, player);
            }
            else if (!Game1.input.GetGamePadState().IsConnected)
            {
                GetTool(currentLocation, cursorTile, player);
            }

        }

        // detects what is in the tile that the player is looking at and calls the function to swap tools
        private void GetTool(GameLocation location, Vector2 tile, Farmer player)
        {
            Tool currentTool = player.CurrentTool;
            StardewValley.Object obj = location.getObjectAtTile((int)tile.X, (int)tile.Y);
            //Check for objects
            if (obj != null)
            {
                if (obj.IsBreakableStone()) { SetTool(player, typeof(Pickaxe)); return; }
                if (obj.IsTwig()) { SetTool(player, typeof(Axe)); return; }
                if (obj.IsWeeds()) 
                {
                    if (Config.Pickaxe_over_melee && !(location is MineShaft))
                    {
                        SetTool(player, typeof(Pickaxe));
                        return;
                    }
                    SetTool(player, typeof(MeleeWeapon)); return;
                }
                if (obj.IsFenceItem()) { SetTool(player, typeof(Axe)); return; }
                if (obj.Name.Equals("Artifact Spot")) { SetTool(player, typeof(Hoe)); return; }
                if (obj.Name.Equals("Garden Pot")) { SetTool(player, typeof(WateringCan)); return; }
                if (obj.Name.Equals("Artifact Spot")) { SetTool(player, typeof(Hoe)); return; }
                if (obj.Name.Equals("Barrel")) { SetTool(player, typeof(MeleeWeapon), "Weapon"); return; }
                return;

            }


            // Check for terrain features
            if (location.terrainFeatures.ContainsKey(tile))
            {
                if (location.terrainFeatures[tile] is GiantCrop or FruitTree) { SetTool(player, typeof(Axe)); return; }
                if (location.terrainFeatures[tile] is Grass && Config.Scythe_on_grass) { SetTool(player, typeof(MeleeWeapon)); return; }
                if (location.terrainFeatures[tile] is Tree tree)
                {
                    if (tree.hasMoss)
                    {
                        SetTool(player, typeof(MeleeWeapon)); return;
                    }
                    SetTool(player, typeof(Axe)); return;
                }
                if (location.terrainFeatures[tile] is HoeDirt)
                {
                    HoeDirt dirt = location.terrainFeatures[tile] as HoeDirt;
                    if (dirt.crop != null && dirt.readyForHarvest()) { SetTool(player, typeof(MeleeWeapon)); return; }
                    if (dirt.crop != null && (bool)dirt.crop.dead) { SetTool(player, typeof(MeleeWeapon)); return; }

                    if (dirt.crop != null && !dirt.isWatered())
                    {
                        if (!(Config.Pickaxe_greater_wcan && currentTool is Pickaxe)) { SetTool(player, typeof(WateringCan)); return; }
                    }
                    return;
                }
                return;
            }

            //Check if it is an boulder or logs and stumps
            for (int i = 0; i < location.resourceClumps.Count; i++)
            {
                if (location.resourceClumps[i].occupiesTile((int)tile.X, (int)tile.Y))
                {
                    switch (location.resourceClumps[i].parentSheetIndex)
                    {
                        //Id's for logs and stumps
                        case 602 or 600:
                            SetTool(player, typeof(Axe));
                            return;

                        //Id's for boulders
                        case 758 or 756 or 754 or 752 or 672 or 622 or 148:
                            SetTool(player, typeof(Pickaxe));
                            return;
                    }
                    return;
                }
            }

            //Check for monsters 
            foreach (var monster in location.characters)
            {
                Vector2 monsterTile = monster.Tile;
                float distance = Vector2.Distance(tile, monsterTile);

                if (monster.IsMonster && distance < 3)
                {
                    //Pickaxe for non-moving cave crab
                    if (monster.displayName.Contains("Crab") && !monster.isMoving()) { SetTool(player, typeof(Pickaxe)); ; return; }

                    SetTool(player, typeof(MeleeWeapon), "Weapon");
                    return;
                }
            }

            //Check for pet bowls
            try
            {
                if (location.getBuildingAt(tile) != null && location.getBuildingAt(tile).GetType() == typeof(PetBowl))
                {
                    SetTool(player, typeof(WateringCan)); return;
                }
            }
            catch { }

            //Check for water source to refil watering can
            if (((location is Farm or VolcanoDungeon || location.isGreenhouse)) && (location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "WaterSource", "Back") != null || location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Back") != null) && !(player.CurrentTool is FishingRod or Pan))
            {
                SetTool(player, typeof(WateringCan)); return;
            }

            //Check for water for fishing
            if (location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Back") != null && !(player.CurrentTool is WateringCan or Pan))
            {
                SetTool(player, typeof(FishingRod)); return;
            }

            //Check for animals to milk or shear
            if (location is Farm or AnimalHouse)
            {
                foreach (var animal in location.getAllFarmAnimals())
                {
                    Vector2 animalTile = animal.Tile;
                    string[] canMilk = { "Goat", "Cow" };
                    string[] canShear = { "Rabbit", "Sheep" };
                    float distance = Vector2.Distance(tile, animalTile);
                    if (canMilk.Any(animal.displayType.Contains) && distance <= 1) { SetTool(player, typeof(MilkPail)); return; }

                    if (canShear.Any(animal.displayType.Contains) && distance < 2) { SetTool(player, typeof(Shears)); return; }
                }
            }

            //Check for feeding bench
            if (location is AnimalHouse && location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Trough", "Back") != null)
            {
                for (int i = 0; i < player.maxItems; i++)
                {
                    if (player.Items[i] != null && player.Items[i].Name == "Hay")
                    {
                        player.CurrentToolIndex = i;
                        return;
                    }
                }
            }

            //Check if it should swap to Hoe
            if (!Config.Hoe_in_empty_soil) { return; }
            if (location is FarmHouse or Shed or AnimalHouse or MineShaft) { return; }
            if (location.isPath(tile)) { return; }
            if (player.CurrentItem is WateringCan) { return; }
            if (player.CurrentItem is Wand && player.CurrentItem.Name.Equals("Return Scepter")) { return; }
            {

            }

            try
            {
                var thing = player.CurrentItem; ;
                if (!(thing.canBePlacedHere(location, tile, CollisionMask.All, true)) && !(thing is MeleeWeapon))
                {
                    SetTool(player, typeof(Hoe));
                }
                return;
            }
            catch { SetTool(player, typeof(Hoe)); return; }

        }

        //Looks for the tool necessary for the action
        private void SetTool(Farmer player, Type toolType, string aux = "Scythe")
        {
            switcher.canSwitch = Config.Auto_switch_last_tool;
            //Melee Weapon \/
            if (toolType == typeof(MeleeWeapon))
            {
                if (aux == "Scythe")
                {
                    for (int i = 0; i < player.maxItems; i++)
                    {
                        if (player.Items[i] != null && player.Items[i].Name.Contains(aux))
                        {
                            if (!(player.CurrentToolIndex == i))
                            {
                                switcher.SwitchIndex(i);
                            }
                            return;
                        }
                    }

                    for (int i = 0; i < player.maxItems; i++)
                    {
                        if (player.Items[i] != null && player.Items[i].GetType() == typeof(MeleeWeapon))
                        {
                            if (!(player.CurrentToolIndex == i))
                            {
                                switcher.SwitchIndex(i);
                            }
                            return;
                        }
                    }
                }

                for (int i = 0; i < player.maxItems; i++)
                {
                    if (player.Items[i] != null && player.Items[i].GetType() == toolType && !(player.Items[i].Name.Contains("Scythe")))
                    {
                        if (!(player.CurrentToolIndex == i))
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
                if (player.Items[i] != null && player.Items[i].GetType() == toolType)
                {
                    if (!(player.CurrentToolIndex == i))
                    {
                        switcher.SwitchIndex(i);
                    }
                    return;
                }
            }
        }

    }
}



