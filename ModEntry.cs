
using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Tools;


namespace AutomateToolSwap
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance { get; set; } = null!;
        internal static ModConfig Config { get; private set; } = null!; // Declare static instance of ModConfig
        internal static Check check { get; private set; } = null!;
        internal static bool isTractorModInstalled;
        internal static bool monsterNearby = false;

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Config = Helper.ReadConfig<ModConfig>();
            check = new Check(Instance);

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            isTractorModInstalled = Helper.ModRegistry.IsLoaded("Pathoschild.TractorMod");
            ConfigSetup.SetupConfig(Helper, this);
        }

        IndexSwitcher switcher = new IndexSwitcher(0);

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            //Alternative "Weapon for Monsters"
            if (Config.AlternativeWeaponOnMonsters && Config.WeaponOnMonsters)
            {
                monsterNearby = true;
                Vector2 tile = Game1.player.Tile;
                foreach (var monster in Game1.currentLocation.characters)
                {
                    if (monster is RockCrab)
                        break;

                    Vector2 monsterTile = monster.Tile;
                    float distance = Vector2.Distance(tile, monsterTile);

                    if (monster.IsMonster && distance < Config.MonsterRangeDetection && Game1.player.canMove)
                    {
                        if (check.Monsters(Game1.currentLocation, tile, Game1.player))
                            return;
                    }

                }
                monsterNearby = false;
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
            if (Config.AlternativeWeaponOnMonsters && player.CurrentItem is MeleeWeapon && !player.CurrentItem.Name.Contains("Scythe") && monsterNearby)
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



