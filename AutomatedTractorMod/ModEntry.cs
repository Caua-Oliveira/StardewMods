using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using xTile.Tiles;


namespace AutomatedTractorMod
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance { get; set; } = null!;
        internal static string modsPath;
        public override void Entry(IModHelper helper)
        {
            Instance = this;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdateTicked;

            Console.WriteLine("TEST");

        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            modsPath = Path.Combine(AppContext.BaseDirectory, "Mods");
            Console.WriteLine("ENTRAMO");
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            string folderPath = Path.Combine(modsPath, "TractorMod");
            string configFilePath = Path.Combine(folderPath, "config.json");
            int distance = 1;
            if (File.Exists(configFilePath))
            {
                string jsonString = File.ReadAllText(configFilePath);
                using (JsonDocument doc = JsonDocument.Parse(jsonString))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("Distance", out JsonElement distanceElement))
                    {
                        distance = distanceElement.GetInt32();
                    }
                }
            }
            if (HasTree(Game1.currentLocation, Game1.player.Tile, Game1.player, distance))
            {
                Console.WriteLine("AE PORRA");
            }

        }
        public bool HasTree(GameLocation location, Vector2 playerTile, Farmer player, int distance)
        {
            for (int x = -distance; x <= distance; x++)
            {
                for (int y = -distance; y <= distance; y++)
                {
                    Vector2 surroundingTile = new Vector2(playerTile.X + x, playerTile.Y + y);
                    if (location.terrainFeatures.TryGetValue(surroundingTile, out TerrainFeature feature))
                    {
                        if (feature is Tree tree)
                        {
                            // Remove moss if needed swapping to Scythe
                            if (tree.hasMoss && tree.growthStage >= Tree.stageForMossGrowth)
                            {

                                return true;
                            }

                            // Return if the player has a tapper in hand (item that can be put in tree)
                            if ((player.CurrentItem != null && (player.CurrentItem.Name == "Tapper" || player.CurrentItem.Name == "Tree Fertilizer")))
                                return true;

                            // If the tree is not fully grown and the config to ignore it is enabled, skips, otherwise swaps to Axe 
                            if (!(tree.growthStage < Tree.treeStage))
                            {

                                return true;
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {

        }

        private void OnOneSecondUpdateTicked(object sender, OneSecondUpdateTickedEventArgs e)
        {

        }
    }
}
