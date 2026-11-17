using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;

namespace BeneathYou;

/// <summary>Determines when beneath-you targeting is active and which tools are eligible.</summary>
internal static class BeneathYouTargetResolver
{
    /// <summary>Get whether the tool is eligible for beneath-you targeting.</summary>
    public static bool IsEligibleTool(Tool? tool)
    {
        if (tool is null)
            return false;

        if (tool is Hoe or WateringCan or Pickaxe or Axe)
            return true;

        if (tool is MeleeWeapon weapon && weapon.isScythe())
            return true;

        // Explicitly exclude FishingRod, Pan, MilkPail, Shears, Slingshot, and combat weapons.
        return false;
    }

    /// <summary>Get whether a world pixel coordinate falls inside the given tile.</summary>
    public static bool IsWorldPointInTile(float worldX, float worldY, int tileX, int tileY)
    {
        int cursorTileX = (int)Math.Floor(worldX / Game1.tileSize);
        int cursorTileY = (int)Math.Floor(worldY / Game1.tileSize);
        return cursorTileX == tileX && cursorTileY == tileY;
    }

    /// <summary>Get whether world-space gameplay allows beneath-you targeting for the local player.</summary>
    public static bool CanUseBeneathYouInCurrentState(Farmer? player, ModConfig config)
    {
        if (!config.Enabled || !Context.IsWorldReady || player is null || !ReferenceEquals(player, Game1.player))
            return false;

        if (Game1.activeClickableMenu is not null || Game1.eventUp || Game1.dialogueUp || Game1.currentMinigame is not null)
            return false;

        if (player.isRidingHorse() || player.IsSitting() || player.onBridge.Value)
            return false;

        return true;
    }

    /// <summary>Get whether the player's cursor is visibly hovering over their own standing tile.</summary>
    public static bool IsCursorOnPlayerTile(Farmer player, Vector2? overrideWorldPosition = null)
    {
        if (!Game1.wasMouseVisibleThisFrame && Game1.mouseCursorTransparency <= 0f)
            return false;

        Vector2 worldPos = overrideWorldPosition
            ?? new Vector2(Game1.getOldMouseX() + Game1.viewport.X, Game1.getOldMouseY() + Game1.viewport.Y);

        if (worldPos.Equals(Vector2.Zero))
            return false;

        Point tile = player.TilePoint;
        return IsWorldPointInTile(worldPos.X, worldPos.Y, tile.X, tile.Y);
    }

    /// <summary>Get whether tool targeting should redirect to the center of <paramref name="player"/>'s standing tile.</summary>
    public static bool ShouldTargetPlayerTileWithTool(
        Farmer player,
        ModConfig config,
        Vector2? targetPosition,
        bool ignoreClick,
        bool isActiveBeneathYouSwing)
    {
        if (!CanUseBeneathYouInCurrentState(player, config))
            return false;

        if (!IsEligibleTool(player.CurrentTool))
            return false;

        if (isActiveBeneathYouSwing)
            return true;

        if (ignoreClick)
            return false;

        if (targetPosition.HasValue && !targetPosition.Value.Equals(Vector2.Zero))
        {
            if (!Game1.wasMouseVisibleThisFrame && Game1.mouseCursorTransparency <= 0f)
                return false;

            Point tile = player.TilePoint;
            return IsWorldPointInTile(targetPosition.Value.X, targetPosition.Value.Y, tile.X, tile.Y);
        }

        return IsCursorOnPlayerTile(player);
    }

    /// <summary>Get the world-pixel center of the farmer's current standing tile.</summary>
    public static Vector2 GetPlayerTileCenter(Farmer player)
    {
        Point tile = player.TilePoint;
        return new Vector2(tile.X * Game1.tileSize + Game1.tileSize / 2f, tile.Y * Game1.tileSize + Game1.tileSize / 2f);
    }
}
