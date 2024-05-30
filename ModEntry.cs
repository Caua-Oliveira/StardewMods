
using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Tools;


namespace AutomateToolSwap
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance { get; set; } = null!;
        internal static ModConfig Config { get; private set; } = null!;
        internal static Check tileHas { get; private set; } = null!;
        internal static ITranslationHelper i18n;
        internal static bool isTractorModInstalled;
        internal static bool monsterNearby = false;


        //Called when the mod is loading
        //Chamada quando o mod está carregando
        public override void Entry(IModHelper helper)
        {
            Instance = this;
            i18n = Helper.Translation;
            Config = Helper.ReadConfig<ModConfig>();
            tileHas = new Check(Instance);

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        }

        //Called when the game opens
        //Chamada quando o jogo abre
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            isTractorModInstalled = Helper.ModRegistry.IsLoaded("Pathoschild.TractorMod");
            ConfigSetup.SetupConfig(Helper, Instance);
        }

        IndexSwitcher switcher = new IndexSwitcher(0);


        //Called when a button is pressed
        //Chamada quando um botão é pressionado
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.activeClickableMenu != null)
                return;

            // turns mod on/off
            // desliga e liga o mod
            if (Config.ToggleKey.JustPressed())
            {
                Config.Enabled = !Config.Enabled;
                if (Config.Enabled)
                    Game1.addHUDMessage(new HUDMessage("AutomateToolSwap " + i18n.Get("mod.Enabled"), 2));
                else
                    Game1.addHUDMessage(new HUDMessage("AutomateToolSwap " + i18n.Get("mod.Disabled"), 2));
            }

            // swaps to the last used item
            // troca para o item usado mais recentemente
            if (Config.LastToolKey.JustPressed() && Game1.player.canMove)
                switcher.GoToLastIndex();


            if (!ButtonMatched(e) || !Config.Enabled || !(Game1.player.canMove))
                return;

            // variables for the main method
            // variáveis para o método principal
            Farmer player = Game1.player;
            GameLocation currentLocation = Game1.currentLocation;
            ICursorPosition cursorPos = this.Helper.Input.GetCursorPosition();
            Vector2 cursorTile = cursorPos.GrabTile;
            Vector2 frontOfPlayerTile = new Vector2((int)Game1.player.GetToolLocation().X / Game1.tileSize, (int)Game1.player.GetToolLocation().Y / Game1.tileSize);

            // different methods for detecting tiels
            // diferentes métodos para detectar blocos
            if (Config.DetectionMethod == "Cursor")
                CheckTile(currentLocation, cursorTile, player);
            else if (Config.DetectionMethod == "Player")
                CheckTile(currentLocation, frontOfPlayerTile, player);

        }

        // detects what is in the tile that the player is looking at 
        // detecta o que está no bloco que o jogador está olhando 
        private void CheckTile(GameLocation location, Vector2 tile, Farmer player)
        {
            // code at OnUpdateTicked
            // código em OnUpdateTicked
            if (Config.AlternativeWeaponOnMonsters && player.CurrentItem is MeleeWeapon && !player.CurrentItem.Name.Contains("Scythe") && monsterNearby)
                return;

            // if the player is using slingshot, it will not be swapped because it is a long range weapon
            // se o jogador estiver usando estilingue, não será trocado porque é uma arma de longo alcance
            if (player.CurrentItem is Slingshot)
                return;

            // code at Check.cs
            // código em Check.cs
            if (tileHas.Objects(location, tile, player))
                return;

            if (tileHas.ResourceClumps(location, tile, player))
                return;

            if (tileHas.TerrainFeatures(location, tile, player))
                return;

            if (tileHas.Water(location, tile, player))
                return;

            if (Config.WeaponOnMonsters && !Config.AlternativeWeaponOnMonsters)
                if (tileHas.Monsters(location, tile, player))
                    return;

            if (tileHas.Animals(location, tile, player))
                return;

            if (tileHas.DiggableSoil(location, tile, player))
                return;

        }


        //Called when the game updates the tick (60 times per second)
        //Chamada quando o jogo atualiza o tick (60 vezes por segundo)
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.activeClickableMenu != null)
                return;

            //Alternative for the option "Weapon for Monsters"
            //Alternativa para a opção "Arma para Monstros"
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
                        if (tileHas.Monsters(Game1.currentLocation, tile, Game1.player))
                            return;
                    }

                }
                monsterNearby = false;
            }

            //Code for Tractor Mod
            //Codigo para o Mod de Trator
            if (!isTractorModInstalled || Config.DisableTractorSwap || (!Config.Enabled && !Config.DisableTractorSwap))
                return;

            if (Game1.player.isRidingHorse() && Game1.player.mount.Name.ToLower().Contains("tractor"))
            {
                Farmer player = Game1.player;
                GameLocation currentLocation = Game1.currentLocation;
                ICursorPosition cursorPos = this.Helper.Input.GetCursorPosition();
                Vector2 cursorTile = cursorPos.GrabTile;
                Vector2 toolLocation = new Vector2((int)Game1.player.GetToolLocation().X / Game1.tileSize, (int)Game1.player.GetToolLocation().Y / Game1.tileSize);

                if (Config.DetectionMethod == "Cursor")
                    CheckTile(currentLocation, cursorTile, player);

                else if (Config.DetectionMethod == "Player")
                    CheckTile(currentLocation, toolLocation, player);

            }
        }


        //Looks for the index of the tool necessary for the action
        //Procura pelo index da ferramenta necessária para a ação
        public void SetTool(Farmer player, Type toolType, string aux = "", bool anyTool = false)
        {
            switcher.canSwitch = Config.AutoReturnToLastTool;
            var items = player.Items;
            //Melee Weapons (swords and scythes) \/
            //Armas de curta distância (espadas e foice) \/
            if (toolType == typeof(MeleeWeapon))
            {
                if (aux == "Scythe" || aux == "ScytheOnly")
                {
                    for (int i = 0; i < player.maxItems; i++)
                    {
                        if (items[i] != null && items[i].GetType() == toolType && items[i].Name.Contains("Scythe"))
                        {
                            if (player.CurrentToolIndex != i)
                            {
                                switcher.SwitchIndex(i);
                            }
                            return;
                        }
                    }
                    if (aux == "ScytheOnly")
                        return;
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
            //Qualquer outra ferramenta

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

        //Looks for the index of the item necessary for the action
        //Procura pelo index doitem necessário para a ação
        public void SetItem(Farmer player, string categorie, string item = "", string crops = "Both", int aux = 0)
        {
            switcher.canSwitch = Config.AutoReturnToLastTool;

            var items = player.Items;
            //Handles trash
            //Trata da categoria lixo
            if (categorie == "Trash" || categorie == "Fertilizer")
            {
                for (int i = 0; i < player.maxItems; i++)
                {

                    if (items[i] != null && items[i].category == aux && !(items[i].Name.Contains(item)))
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
            //Trata da categoria recursos
            if (categorie == "Resource")
            {
                for (int i = 0; i < player.maxItems; i++)
                {
                    if (items[i] != null && items[i].category == -15 && items[i].Name.Contains(item) && items[i].Stack >= 5)
                    {
                        if (player.CurrentToolIndex != i)
                            switcher.SwitchIndex(i);

                        return;
                    }
                }
                return;
            }

            //Handles Seeds
            //Trata da categoria sementes
            if (categorie == "Seed")
            {
                for (int i = 0; i < player.maxItems; i++)
                {

                    if (items[i] != null && items[i].category == -74 && !items[i].HasContextTag("tree_seed_item"))
                    {
                        if (player.CurrentToolIndex != i)
                            switcher.SwitchIndex(i);

                        return;
                    }
                }
                return;
            }

            //Handles Crops
            //Trata da categoria plantações
            if (categorie == "Crops")
            {
                bool canFruit = crops == "Both" || crops == "Fruit";
                bool canVegetable = crops == "Both" || crops == "Vegetable";

                for (int i = 0; i < player.maxItems; i++)
                {
                    bool isFruit(Item Item) { return Item != null && Item.category == -79; }

                    bool isVegetable(Item Item) { return Item != null && Item.category == -75; }

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
            //Trata de qualquer outro item
            for (int i = 0; i < player.maxItems; i++)
            {
                if (items[i] != null && items[i].category == aux && items[i].Name.Contains(item))
                {
                    if (player.CurrentItem != null && player.CurrentItem.category.ToString() == categorie)
                        return;

                    if (player.CurrentToolIndex != i)
                        switcher.SwitchIndex(i);

                    return;
                }
            }
            return;
        }

        //Checks if the button pressed matches the config
        //Verifica se o botão pressionado corresponde a config
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



