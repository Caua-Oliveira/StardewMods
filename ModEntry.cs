
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
        //Basic mod variables
        internal static ModEntry Instance { get; set; } = null!;
        internal static ModConfig Config { get; private set; } = null!;
        internal static Check check { get; private set; } = null!;
        internal static bool isTractorModInstalled;
        internal static bool monsterNearby = false;
        IndexSwitcher switcher = new IndexSwitcher(0);

        //Inicia o mod
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

            //Cria o menu de configuração do mod
            ConfigSetup.SetupConfig(Helper, Instance);
        }

        // Método para verificar se o botão apertado pelo jogador é o mesmo botão do mod
        public bool ButtonMatched(ButtonPressedEventArgs e)
        {
            //Se o jogador estiver usando uma combinação de botões custumizado, checa se foi apertado.
            if (Config.UseDifferentSwapKey)
            {
                if (Config.SwapKey.JustPressed())
                    return true;
                return false;

            }
            else
            {
                //Se não estiver usando uma combinação de botões custumizado, checa se o botão apertado faz parte da lista de botões do jogo padrão para usar ferramentas
                foreach (var button in Game1.options.useToolButton)
                {
                    if (e.Button == button.ToSButton() || e.Button == SButton.ControllerX)
                        return true;

                }
                return false;
            }
        }


        //Método usado para um modo alternativo de deteccão de monstros e para o mod de Trator
        //(Pode Ignorar essa parte pois ela aparece em outro lugar)
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.activeClickableMenu != null)
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


        // Método usado quando o jogador aperta algum botão
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            //Retorna se o jogador não está no jogo ou se o menu de opções estiver aberto
            if (!Context.IsWorldReady || Game1.activeClickableMenu != null)
                return;

            // Desliga e liga o mod
            if (Config.ToggleKey.JustPressed())
            {
                Config.Enabled = !Config.Enabled;
                if (Config.Enabled)
                {
                    Game1.addHUDMessage(new HUDMessage("AutomateToolSwap ENABLED", 2));
                }
                Game1.addHUDMessage(new HUDMessage("AutomateToolSwap DISABLED", 2));
            }

            // Troca para o último item selecionado
            if (Config.LastToolKey.JustPressed() && Game1.player.canMove)
            {
                switcher.GoToLastIndex();
            }

            // Checa se o mod deve fazer a troca automatica de item
            if (!ButtonMatched(e) || !Config.Enabled || !(Game1.player.canMove))
                return;


            // Variáveis para o método principal
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

        // Detecta o objeto que está no cursor do jogador, caso algum tipo de objeto seja detectado, retorna e não precisa checar por outros
        private void CheckTile(GameLocation location, Vector2 tile, Farmer player)
        {
            // Retorna se o modo alternativo de detecção de monstros estiver ativo e tiver um monstro por perto
            if (Config.AlternativeWeaponOnMonsters && player.CurrentItem is MeleeWeapon && !player.CurrentItem.Name.Contains("Scythe") && monsterNearby)
                return;
            // Retorna se o jogador estiver com um estilingue na mão
            if (player.CurrentItem is Slingshot)
                return;

            // Chama os métodos para checar o que tem no cursor, se houver o tipo, retorna
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

            if (check.DiggableSoil(location, tile, player))
                return;

        }

        // Método para colocar a ferramenta desejada na mão do jogador
        public void SetTool(Farmer player, Type toolType, string aux = "Scythe", bool anyTool = false)
        {
            switcher.canSwitch = Config.AutoReturnToLastTool;
            var items = player.Items;

            //Melee Weapons \/
            if (toolType == typeof(MeleeWeapon))
            {
                if (aux == "Scythe")
                {
                    // Procura pela foice no inventario do jogador, e se achar, chama o metodo "SwitchIndex" para trocar a ferramenta
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
                // Se não achar a foice, ou se o parametro aux for igual a "Weapon", procura e tenta trocar para a espada (Espada e Foice tem o mesmo Tipo, porém com funções diferentes dentro de jogo)
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

            //Qualquer outra ferramenta \/
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


        //Método para colocar o item desejado na mão do jogador. (Há repetições por que categorias diferentes requer tratamentos diferentes
        public void SetItem(Farmer player, string categorie, string item = "", string crops = "Both")
        {
            switcher.canSwitch = Config.AutoReturnToLastTool;
            var items = player.Items;

            if (categorie == "Trash" || categorie == "Fertilizer")
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

            if (categorie == "Resource")
            {
                for (int i = 0; i < player.maxItems; i++)
                {
                    Console.WriteLine(items[i] == null);
                    if (items[i] != null && items[i].getCategoryName() == categorie && items[i].Name.Contains(item) && items[i].Stack >= 5)
                    {
                        if (player.CurrentToolIndex != i)
                            switcher.SwitchIndex(i);

                        return;
                    }
                }
                return;
            }

            if (categorie == "Seed")
            {
                for (int i = 0; i < player.maxItems; i++)
                {
                    if (items[i] != null && items[i].getCategoryName() == categorie && !items[i].HasContextTag("tree_seed_item"))
                    {
                        if (player.CurrentToolIndex != i)
                            switcher.SwitchIndex(i);

                        return;
                    }
                }
                return;
            }

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


            // Qualquer outro tipo de item
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

    }
}



