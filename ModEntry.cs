
using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
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

            configMenu.AddKeybindList(
                mod: this.ModManifest,
                name: () => "Toggle On/Off Keybind",
                tooltip: () => "What key you will use to toggle the mod on/off",
                getValue: () => Config.ToggleKey,
                setValue: value => Config.ToggleKey = value
            );

            configMenu.AddKeybindList(
                mod: this.ModManifest,
                name: () => "Swap Tools Keybind",
                tooltip: () => "What keybind you will use to swap tools (Recommended: Key you use to break things)",
                getValue: () => Config.SwapKey,
                setValue: value => Config.SwapKey = value
            );

            configMenu.AddKeybindList(
                mod: this.ModManifest,
                name: () => "Return to last tool used",
                tooltip: () => "What key you will use to return to last tool used",
                getValue: () => Config.LastToolButton,
                setValue: value => Config.LastToolButton = value
            );

            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "Detection Method",
                tooltip: () => "KBM: will swap tool based on the object the cursor is pointing to; Controller: will swap tool based on where the player is looking at",
                allowedValues: new string[] { "KBM", "Controller" },
                getValue: () => Config.Detection_method,
                setValue: value => Config.Detection_method = value
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
            if (Config.LastToolButton.JustPressed() && Game1.player.canMove) { switcher.GoToLastIndex(); }

            // ignore if player didnt left-click or mod is disabled
            if (!Config.SwapKey.JustPressed() || !Config.Enabled || !(Game1.player.canMove)) { return; }


            Farmer player = Game1.player;
            GameLocation currentLocation = Game1.currentLocation;
            ICursorPosition cursorPos = this.Helper.Input.GetCursorPosition();
            Vector2 cursorTile = cursorPos.GrabTile;
            Vector2 toolLocation = new Vector2((int)Game1.player.GetToolLocation().X / Game1.tileSize, (int)Game1.player.GetToolLocation().Y / Game1.tileSize);
            switch (Config.Detection_method)
            {
                case "KBM":
                    GetTool(currentLocation, cursorTile, player);
                    break;
                case "Controller":
                    GetTool(currentLocation, toolLocation, player);
                    break;
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
                if (obj.IsWeeds())
                {
                    if (Config.Pickaxe_over_melee && !(location is MineShaft))
                    {
                        SetTool(player, typeof(Pickaxe), anyTool: true);

                        return;
                    }
                    SetTool(player, typeof(MeleeWeapon));

                }
                else if (obj is CrabPot crabPot)
                {
                    if (crabPot.bait.Value == null)
                        SetItem(player, "Bait", "Bait");

                }
                else if (obj.IsBreakableStone())
                    SetTool(player, typeof(Pickaxe));

                else if (obj.IsTwig())
                    SetTool(player, typeof(Axe));

                else if (obj.Name.Equals("Furnace"))
                    SetItem(player, "Resource", "Ore");

                else if (obj.Name.Equals("Cheese Press"))
                    SetItem(player, "Animal Product", "Milk");

                else if (obj.Name.Equals("Mayonnaise Machine"))
                    SetItem(player, "Animal Product", "Egg");

                else if (obj.Name.Equals("Artifact Spot"))
                    SetTool(player, typeof(Hoe));

                else if (obj.Name.Equals("Garden Pot"))
                    SetTool(player, typeof(WateringCan));

                else if (obj.Name.Equals("Seed Spot"))
                    SetTool(player, typeof(Hoe));

                else if (obj.Name.Equals("Barrel"))
                    SetTool(player, typeof(MeleeWeapon), "Weapon");

                else if (obj.Name.Equals("Supply Crate"))
                    SetTool(player, typeof(Hoe), anyTool: true);

                else if (obj.Name.Equals("Recycling Machine"))
                    SetItem(player, "Trash", "Joja");

                else if (obj.Name.Equals("Bone Mill"))
                    SetItem(player, "Resource", "Bone Fragment");

                return;

            }

            // Check for terrain features
            if (location.terrainFeatures.ContainsKey(tile))
            {
                if (location.terrainFeatures[tile] is GiantCrop or FruitTree)
                {
                    SetTool(player, typeof(Axe));

                    return;
                }
                if (location.terrainFeatures[tile] is Grass && Config.Scythe_on_grass)
                {
                    SetTool(player, typeof(MeleeWeapon));

                    return;
                }
                if (location.terrainFeatures[tile] is Tree tree)
                {
                    if (player.CurrentItem.Name.Equals("Tapper")) { return; }

                    if (tree.hasMoss && tree.growthStage >= Tree.stageForMossGrowth)
                    {
                        SetTool(player, typeof(MeleeWeapon));

                        return;
                    }
                    SetTool(player, typeof(Axe));

                    return;
                }
                if (location.terrainFeatures[tile] is HoeDirt)
                {
                    HoeDirt dirt = location.terrainFeatures[tile] as HoeDirt;
                    if (dirt.crop != null && dirt.readyForHarvest())
                    {
                        SetTool(player, typeof(MeleeWeapon));

                        return;

                    }
                    if (dirt.crop != null && (bool)dirt.crop.dead)
                    {
                        SetTool(player, typeof(MeleeWeapon));

                        return;

                    }
                    if (dirt.crop != null && !dirt.isWatered())
                    {
                        if (!(Config.Pickaxe_greater_wcan && currentTool is Pickaxe))
                        {
                            SetTool(player, typeof(WateringCan));

                            return;

                        }
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
                    if (monster.displayName.Contains("Crab") && !monster.isMoving())
                    {
                        SetTool(player, typeof(Pickaxe));

                        return;

                    }
                    SetTool(player, typeof(MeleeWeapon), "Weapon");

                    return;
                }
            }

            //Check for pet bowls

            if (location.getBuildingAt(tile) != null && (location.getBuildingAt(tile).GetType() == typeof(PetBowl) || location.getBuildingAt(tile).GetType() == typeof(Stable)))
            {
                SetTool(player, typeof(WateringCan));

                return;

            }

            //Check for water for fishing
            if (!(location is Farm or VolcanoDungeon || location.Name.Contains("Island") || location.isGreenhouse) && location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Back") != null && !(player.CurrentTool is WateringCan or Pan))
            {
                SetTool(player, typeof(FishingRod));

                return;

            }

            //Check for water source to refil watering can
            if ((location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "WaterSource", "Back") != null || location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Back") != null) && !(player.CurrentTool is FishingRod or Pan))
            {
                SetTool(player, typeof(WateringCan));

                return;

            }



            //Check for animals to milk or shear
            if (location is Farm or AnimalHouse)
            {
                foreach (FarmAnimal animal in location.getAllFarmAnimals())
                {
                    Vector2 animalTile = animal.Tile;
                    string[] canMilk = { "Goat", "Cow" };
                    string[] canShear = { "Rabbit", "Sheep" };
                    float distance = Vector2.Distance(tile, animalTile);
                    if (canMilk.Any(animal.displayType.Contains) && distance <= 1 && animal.currentLocation == player.currentLocation)
                    {
                        SetTool(player, typeof(MilkPail));

                        return;
                    }

                    if (canShear.Any(animal.displayType.Contains) && distance < 2 && animal.currentLocation == player.currentLocation)
                    {
                        SetTool(player, typeof(Shears));

                        return;
                    }
                }
            }

            //Check for feeding bench
            if (location is AnimalHouse && location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Trough", "Back") != null)
            {
                SetItem(player, "", "Hay");

                return;
            }
            //Check if it should swap to Hoe
            if (!Config.Hoe_in_empty_soil)
                return;
            if (location is FarmHouse or Shed or AnimalHouse or MineShaft)
                return;
            if (location.isPath(tile))
                return;
            if (player.CurrentItem is MeleeWeapon && player.CurrentItem.getCategoryName().Contains("Level") && Game1.spawnMonstersAtNight)
                return;
            if (player.CurrentItem is FishingRod or GenericTool)
                return;
            if (player.CurrentItem is Wand && player.CurrentItem.Name.Equals("Return Scepter"))
                return;


            if (player.CurrentItem != null)
            {
                var thing = player.CurrentItem; ;
                if (!thing.canBePlacedHere(location, tile, CollisionMask.All, true))
                {
                    SetTool(player, typeof(Hoe));
                }
                return;

            }
        }

        //Looks for the tool necessary for the action
        private void SetTool(Farmer player, Type toolType, string aux = "Scythe", bool anyTool = false)
        {
            switcher.canSwitch = Config.Auto_switch_last_tool;
            //Melee Weapon \/
            if (toolType == typeof(MeleeWeapon))
            {
                if (aux == "Scythe")
                {
                    for (int i = 0; i < player.maxItems; i++)
                    {
                        if (player.Items[i] != null && player.Items[i].GetType() == toolType && player.Items[i].Name.Contains(aux))
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
                if ((player.Items[i] != null && player.Items[i].GetType() == toolType) || (anyTool && player.Items[i] is Axe or Pickaxe or Hoe))
                {
                    if (!(player.CurrentToolIndex == i))
                    {
                        switcher.SwitchIndex(i);
                    }
                    return;
                }
            }

        }

        //Any item
        private void SetItem(Farmer player, string categorie, string item)
        {
            if (categorie == "Trash")
            {
                for (int i = 0; i < player.maxItems; i++)
                {
                    if (player.Items[i] != null && player.Items[i].getCategoryName() == categorie && !(player.Items[i].Name.Contains(item)))
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

            if (categorie == "Resource")
            {
                for (int i = 0; i < player.maxItems; i++)
                {
                    if (player.Items[i] != null && player.Items[i].getCategoryName() == categorie && player.Items[i].Name.Contains(item) && player.Items[i].Stack >= 5)
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

            for (int i = 0; i < player.maxItems; i++)
            {
                if (player.Items[i] != null && player.Items[i].getCategoryName() == categorie && player.Items[i].Name.Contains(item))
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

    }
}



