using StardewModdingAPI;
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

        public override void Entry(IModHelper helper)
        {
            DidWork.SetMonitor(this.Monitor);
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
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

                ICursorPosition cursorPos = Helper.Input.GetCursorPosition();
                Vector2 frontOfPlayerTile = new(
                    (int)(player.GetToolLocation().X / Game1.tileSize),
                    (int)(player.GetToolLocation().Y / Game1.tileSize)
                );
                
                switch (player.CurrentTool)
                {
                    case Pickaxe _:
                        didWork = DidWork.Pickaxe(frontOfPlayerTile);
                        break;
                    case Axe _:
                        didWork = DidWork.Axe(frontOfPlayerTile);
                        break;
                    case Hoe _:
                        didWork = DidWork.Hoe(frontOfPlayerTile);
                        break;
                    case FishingRod _:
                        didWork = DidWork.FishingRod(frontOfPlayerTile);
                        break;
                    case WateringCan _:
                        didWork = DidWork.WateringCan(frontOfPlayerTile);
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
