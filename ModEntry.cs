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

namespace AutomateToolSwap
{

    internal sealed class ModEntry : Mod
    {
        bool mod_activated = true;
        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
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
                    SetTool(player, "MeleeWeapon");
                    break;

                case "Scythe Crop":
                    SetTool(player, "Scythe");
                    break;
                case "Not Watered":
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
                    return "Axe";
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
                        return "Not Watered";
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
