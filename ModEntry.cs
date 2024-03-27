using System;
using System.Xml.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Extensions;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using xTile.Dimensions;
using xTile.Tiles;
using GenericModConfigMenu;
using Sprinting;

namespace AutomateToolSwap
{

    public class ModEntry : Mod
    {

        internal static ModEntry Instance { get; private set; } = null!;
        internal static ModConfig Config { get; set; }
        bool mod_activated = true;
        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Config = Helper.ReadConfig<ModConfig>();
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.GameLaunched += new EventHandler<GameLaunchedEventArgs>(this.OnGameLaunched);
        }
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu == null)
            {
                Console.Out.WriteLine("Generic Mod Config Menu not installed!");
                return;
            }
            else
            {
                Console.Out.WriteLine("Successfully hooked into spacechase0.GenericModConfigMenu.");
                configMenu.Register(
                    mod: this.ModManifest,
                    reset: () => Config = new ModConfig(),
                    save: () => this.Helper.WriteConfig(Config)
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
        }


        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {

            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            if (e.Button == SButton.CapsLock)
            {
                mod_activated = !mod_activated;
                if (mod_activated) { Console.WriteLine("Mod ACTIVATED"); }
                else { Console.WriteLine("Mod DEACTIVATED"); }

            }
            // ignore if player didnt left-click or mod is deactivated
            if (e.Button != SButton.MouseLeft && e.Button != SButton.ControllerX || !mod_activated)
            {
                return;
            }


            Farmer player = Game1.player;
            ICursorPosition cursorPos = this.Helper.Input.GetCursorPosition();
            Vector2 cursorTile = cursorPos.GrabTile;
            GameLocation currentLocation = Game1.currentLocation;
            string tileContent = GetTileContents(currentLocation, cursorTile);


            //Determines what tool the player should use
            switch (tileContent)
            {
                case "Empty":
                case "Dont Need Water":
                    break;

                case "Tree":
                case "Twig":
                case "Gate":
                case "Hardwood Fence":
                case "Wood Fence":
                case "Giant Crop":
                    SetTool(player, "Axe");
                    break;
                case "Stone":
                    SetTool(player, "Pickaxe");
                    break;

                case "Weeds":
                    if (Config.Pickaxe_over_melee)
                    {
                        SetTool(player, "Pickaxe");
                        break;
                    }
                    else
                    {
                        SetTool(player, "MeleeWeapon");
                        break;
                    }

                case "Scythe Crop":
                    SetTool(player, "Scythe");
                    break;
                case "Need Water":
                    SetTool(player, "WateringCan");
                    break;
                default:
                    Console.WriteLine($"Debug: {tileContent}");
                    break;
            }


        }

        //Detects what is in the tile that the player is looking at
        private string GetTileContents(GameLocation location, Vector2 tile)
        {
            // Get the object at the specified tile
            StardewValley.Object obj = location.getObjectAtTile((int)tile.X, (int)tile.Y);

            Tool currentTool = Game1.player.CurrentTool;

            if (obj != null)
            {
                // If there's an object, return its name
                return obj.Name;
            }

            // If there's no object, check for terrain features
            if (location.terrainFeatures.ContainsKey(tile))
            {


                //Check if its tree
                if (location.terrainFeatures[tile] is Tree)
                {
                    return "Tree";
                }

                if (location.terrainFeatures[tile] is GiantCrop)
                {
                    return "Giant Crop";
                }


                if (location.terrainFeatures[tile] is HoeDirt)
                {
                    //Check if its a crop that need a scythe
                    HoeDirt dirt = location.terrainFeatures[tile] as HoeDirt;

                    if (dirt.crop != null && dirt.readyForHarvest())
                    {
                        return "Scythe Crop";

                    }

                    //Checks if crop need water
                    if (dirt.crop != null && !dirt.isWatered())
                    {
                        if (Config.Pickaxe_greater_wcan && currentTool is Pickaxe)
                        {
                            return "Dont Need Water";
                        }
                        return "Need Water";
                    }

                }
                return location.terrainFeatures[tile].NetFields.Name;

            }


            //Check if it is an large stone or large stump
            for (int i = 0; i < location.resourceClumps.Count; i++)
            {

                if (location.resourceClumps[i].occupiesTile((int)tile.X, (int)tile.Y))
                {
                    switch (location.resourceClumps[i].parentSheetIndex)
                    {
                        case 602:
                        case 600:
                            return "Tree";
                        case 758:
                        case 756:
                        case 754:
                        case 752:
                        case 672:
                            return "Stone";
                        default:
                            Console.Write("Debug Code: ");
                            Console.WriteLine(location.resourceClumps[i].parentSheetIndex);
                            break;
                    }

                }

            }

            //If nothing is found, returns empty
            return "Empty";
        }


        //Looks for the tool necessary for the action
        private void SetTool(Farmer player, String tool)
        {

            for (int i = 0; i < player.maxItems; i++)
            {
                //If the action is to break crops, use scythe
                if (tool == "Scythe" && player.Items[i] != null && player.Items[i].Name.Contains(tool))
                {
                    player.CurrentToolIndex = i;
                    break;
                }

                //If the action is break weeds, any melee weapon is okay
                if (player.Items[i] != null && player.Items[i].ToString().Contains(tool))
                {
                    player.CurrentToolIndex = i;
                    break;
                }

            }

        }
    }
}




    
