﻿using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;
using Microsoft.Xna.Framework;

namespace OnlyWhenItCounts
{
    public class ModEntry : Mod
    {
        private bool pendingRestore;
        private bool wasUsingTool;
        private float savedStamina;
        private ModConfig Config; // Add this line
        internal static IItemExtensionsApi? ItemExtensionsAPI;
        public override void Entry(IModHelper helper)
        {

            DidWork.SetMonitor(this.Monitor);
            this.Config = this.Helper.ReadConfig<ModConfig>(); // Add this line
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched; // Add this line
        }

        // Add this method
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            ItemExtensionsAPI = Helper.ModRegistry.GetApi<IItemExtensionsApi>("mistyspring.ItemExtensions");
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            //add option to toggle mod
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Enable Mod",
                tooltip: () => "Enable or disable the mod.",
                getValue: () => this.Config.Enabled,
                setValue: value => this.Config.Enabled = value
            );

        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || !Config.Enabled)
                return;

            var player = Game1.player;
            bool isSwinging = player.UsingTool;

            if (player.CurrentTool == null)
                return;

            // 1) Tool swing started this tick
            if (!wasUsingTool && isSwinging)
            {
                // Check if tool would do work (e.g., hitting stone)
                bool didWork = false;

                Vector2 targetTile;
                
                targetTile = new Vector2(
                    (int)(player.GetToolLocation().X / Game1.tileSize),
                    (int)(player.GetToolLocation().Y / Game1.tileSize)
                );

                Monitor.Log(targetTile.ToString(), LogLevel.Trace);


                switch (player.CurrentTool)
                {
                    case Pickaxe _:
                        didWork = DidWork.Pickaxe(targetTile);
                        break;
                    case Axe _:
                        didWork = DidWork.Axe(targetTile);
                        break;
                    case Hoe _:
                        didWork = DidWork.Hoe(targetTile);
                        break;
                    case FishingRod _:
                        didWork = DidWork.FishingRod(targetTile);
                        break;
                    case WateringCan _:
                        didWork = DidWork.WateringCan(targetTile);
                        break;
                }


                if (didWork)
                {
                    wasUsingTool = true;
                    return;
                }

                savedStamina = player.stamina;
                pendingRestore = true;
            }

            // 2) Tool swing ended this tick
            if (pendingRestore && wasUsingTool && !isSwinging)
            {
                player.stamina = savedStamina;
                pendingRestore = false;
            }

            // 3) Save tool usage state for next tick
            wasUsingTool = isSwinging;
        }
    }
}