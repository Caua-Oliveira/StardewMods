using System;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;

namespace BeneathYou;

/// <summary>Captured push metadata passed from <c>Utility.tryToPlaceItem</c> Prefix to Postfix.</summary>
internal sealed class PendingPlacementPush
{
    public GameLocation Location { get; }
    public Rectangle Footprint { get; }
    public Point AnchorTile { get; }
    public Point DestinationTile { get; }

    public PendingPlacementPush(GameLocation location, Rectangle footprint, Point anchorTile, Point destinationTile)
    {
        this.Location = location;
        this.Footprint = footprint;
        this.AnchorTile = anchorTile;
        this.DestinationTile = destinationTile;
    }
}

/// <summary>Applies and manages all Harmony patches for Beneath You.</summary>
internal static class HarmonyPatches
{
    private static IMonitor Monitor = null!;
    private static Func<ModConfig> GetConfig = null!;
    private static PerScreen<ScreenState> ScreenState = null!;

    internal static void Apply(Harmony harmony, IMonitor monitor, Func<ModConfig> getConfig, PerScreen<ScreenState> screenState)
    {
        Monitor = monitor;
        GetConfig = getConfig;
        ScreenState = screenState;

        harmony.Patch(
            original: AccessTools.Method(typeof(Character), nameof(Character.GetToolLocation), new[] { typeof(Vector2), typeof(bool) }),
            postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Character_GetToolLocation_Postfix))
            {
                after = new[] { "vgperson.RangedTools" }
            }
        );

        harmony.Patch(
            original: AccessTools.Method(typeof(Character), nameof(Character.getGeneralDirectionTowards), new[] { typeof(Vector2), typeof(int), typeof(bool), typeof(bool) }),
            prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Character_GetGeneralDirectionTowards_Prefix))
            {
                priority = Priority.High
            }
        );

        harmony.Patch(
            original: AccessTools.Method(typeof(Farmer), nameof(Farmer.BeginUsingTool)),
            prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Farmer_BeginUsingTool_Prefix))
        );

        harmony.Patch(
            original: AccessTools.Method(typeof(Farmer), nameof(Farmer.useTool), new[] { typeof(Farmer) }),
            postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Farmer_UseTool_Postfix))
        );

        harmony.Patch(
            original: AccessTools.Method(typeof(MeleeWeapon), nameof(MeleeWeapon.getAreaOfEffect)),
            postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(MeleeWeapon_GetAreaOfEffect_Postfix))
        );

        harmony.Patch(
            original: AccessTools.Method(typeof(Utility), nameof(Utility.playerCanPlaceItemHere), new[] { typeof(GameLocation), typeof(Item), typeof(int), typeof(int), typeof(Farmer), typeof(bool) }),
            prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Utility_PlayerCanPlaceItemHere_Prefix)),
            finalizer: new HarmonyMethod(typeof(HarmonyPatches), nameof(Utility_PlayerCanPlaceItemHere_Finalizer))
        );

        harmony.Patch(
            original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.IsTileOccupiedBy), new[] { typeof(Vector2), typeof(CollisionMask), typeof(CollisionMask), typeof(bool) }),
            prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(GameLocation_IsTileOccupiedBy_Prefix))
        );

        harmony.Patch(
            original: AccessTools.Method(typeof(Utility), nameof(Utility.tryToPlaceItem), new[] { typeof(GameLocation), typeof(StardewValley.Object), typeof(int), typeof(int) }),
            prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Utility_TryToPlaceItem_Prefix)),
            postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Utility_TryToPlaceItem_Postfix)),
            finalizer: new HarmonyMethod(typeof(HarmonyPatches), nameof(Utility_TryToPlaceItem_Finalizer))
        );
    }

    private static void Character_GetToolLocation_Postfix(Character __instance, Vector2 target_position, bool ignoreClick, ref Vector2 __result)
    {
        try
        {
            if (__instance is not Farmer farmer)
                return;

            ModConfig config = GetConfig();
            ScreenState state = ScreenState.Value;
            if (!BeneathYouTargetResolver.ShouldTargetPlayerTileWithTool(farmer, config, target_position, ignoreClick, state.ActiveBeneathYouSwing))
                return;

            __result = BeneathYouTargetResolver.GetPlayerTileCenter(farmer);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed in {nameof(Character_GetToolLocation_Postfix)}:\n{ex}", LogLevel.Error);
        }
    }

    private static bool Character_GetGeneralDirectionTowards_Prefix(
        Character __instance,
        Vector2 target,
        int yBias,
        bool opposite,
        bool useTileCalculations,
        ref int __result)
    {
        try
        {
            if (opposite || __instance is not Farmer farmer || !ReferenceEquals(farmer, Game1.player))
                return true;

            ModConfig config = GetConfig();
            if (!BeneathYouTargetResolver.CanUseBeneathYouInCurrentState(farmer, config))
                return true;

            Point tile = farmer.TilePoint;
            if (BeneathYouTargetResolver.IsWorldPointInTile(target.X, target.Y, tile.X, tile.Y))
            {
                __result = farmer.FacingDirection;
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed in {nameof(Character_GetGeneralDirectionTowards_Prefix)}:\n{ex}", LogLevel.Error);
            return true;
        }
    }

    private static void Farmer_BeginUsingTool_Prefix(Farmer __instance)
    {
        try
        {
            if (!ReferenceEquals(__instance, Game1.player))
                return;

            ModConfig config = GetConfig();
            ScreenState state = ScreenState.Value;
            state.ActiveBeneathYouSwing = false;

            if (!BeneathYouTargetResolver.CanUseBeneathYouInCurrentState(__instance, config))
                return;

            if (!BeneathYouTargetResolver.IsEligibleTool(__instance.CurrentTool))
                return;

            Point tile = __instance.TilePoint;
            bool lastClickOnTile = !__instance.lastClick.Equals(Vector2.Zero)
                && (Game1.wasMouseVisibleThisFrame || Game1.mouseCursorTransparency > 0f)
                && BeneathYouTargetResolver.IsWorldPointInTile(__instance.lastClick.X, __instance.lastClick.Y, tile.X, tile.Y);

            if (lastClickOnTile)
            {
                state.ActiveBeneathYouSwing = true;
                __instance.lastClick = BeneathYouTargetResolver.GetPlayerTileCenter(__instance);
            }
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed in {nameof(Farmer_BeginUsingTool_Prefix)}:\n{ex}", LogLevel.Error);
        }
    }

    private static void Farmer_UseTool_Postfix(Farmer who)
    {
        try
        {
            if (ReferenceEquals(who, Game1.player) && !who.UsingTool)
                ScreenState.Value.ActiveBeneathYouSwing = false;
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed in {nameof(Farmer_UseTool_Postfix)}:\n{ex}", LogLevel.Error);
        }
    }

    private static void MeleeWeapon_GetAreaOfEffect_Postfix(MeleeWeapon __instance, int x, int y, ref Rectangle __result)
    {
        try
        {
            if (!__instance.isScythe())
                return;

            ModConfig config = GetConfig();
            if (!config.Enabled)
                return;

            Farmer player = Game1.player;
            if (!BeneathYouTargetResolver.CanUseBeneathYouInCurrentState(player, config))
                return;

            ScreenState state = ScreenState.Value;
            Point tile = player.TilePoint;
            if (!state.ActiveBeneathYouSwing && !BeneathYouTargetResolver.IsWorldPointInTile(x, y, tile.X, tile.Y))
                return;

            var beneathYouRect = new Rectangle(tile.X * Game1.tileSize + 4, tile.Y * Game1.tileSize + 4, Game1.tileSize - 8, Game1.tileSize - 8);
            __result = __result.IsEmpty ? beneathYouRect : Rectangle.Union(__result, beneathYouRect);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed in {nameof(MeleeWeapon_GetAreaOfEffect_Postfix)}:\n{ex}", LogLevel.Error);
        }
    }

    private static bool Utility_PlayerCanPlaceItemHere_Prefix(
        GameLocation location,
        Item item,
        int x,
        int y,
        Farmer f,
        ref bool __result,
        out Farmer? __state)
    {
        __state = ScreenState.Value.IgnoreSelfCollisionFarmer;

        try
        {
            ModConfig config = GetConfig();
            if (!config.Enabled || location is null || item is null || !ReferenceEquals(f, Game1.player))
                return true;

            PlacementPassability passability = PassabilityClassifier.Classify(item, location);
            if (passability != PlacementPassability.Impassable)
                return true;

            Point anchorTile = new Point(x / Game1.tileSize, y / Game1.tileSize);
            Rectangle footprint = PassabilityClassifier.GetPlacedFootprint(item, location, anchorTile);
            if (!PlayerPushService.DoesFarmerOverlapFootprint(f, footprint))
                return true;

            if (!config.AllowSolidPlacementUnderPlayer)
            {
                __result = false;
                return false;
            }

            if (!PlayerPushService.TryFindPushDestination(location, f, footprint, config, out _))
            {
                __result = false;
                return false;
            }

            ScreenState.Value.IgnoreSelfCollisionFarmer = f;
            return true;
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed in {nameof(Utility_PlayerCanPlaceItemHere_Prefix)}:\n{ex}", LogLevel.Error);
            return true;
        }
    }

    private static void Utility_PlayerCanPlaceItemHere_Finalizer(Farmer? __state)
    {
        ScreenState.Value.IgnoreSelfCollisionFarmer = __state;
    }

    private static void GameLocation_IsTileOccupiedBy_Prefix(
        GameLocation __instance,
        Vector2 tile,
        CollisionMask collisionMask,
        ref CollisionMask ignorePassables,
        bool useFarmerTile)
    {
        try
        {
            Farmer? ignoredFarmer = ScreenState.Value.IgnoreSelfCollisionFarmer;
            if (ignoredFarmer is null)
                return;

            if (!collisionMask.HasFlag(CollisionMask.Farmers) || ignorePassables.HasFlag(CollisionMask.Farmers))
                return;

            var tileRect = new Rectangle((int)tile.X * Game1.tileSize, (int)tile.Y * Game1.tileSize, Game1.tileSize, Game1.tileSize);
            foreach (Farmer other in __instance.farmers)
            {
                if (ReferenceEquals(other, ignoredFarmer))
                    continue;

                bool collides = useFarmerTile ? other.Tile == tile : other.GetBoundingBox().Intersects(tileRect);
                if (collides)
                    return; // Preserve blocking when another farmer occupies the tile.
            }

            ignorePassables |= CollisionMask.Farmers;
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed in {nameof(GameLocation_IsTileOccupiedBy_Prefix)}:\n{ex}", LogLevel.Error);
        }
    }

    private sealed class TryPlaceState
    {
        public Farmer? PreviousIgnoreFarmer { get; set; }
        public PendingPlacementPush? PendingPush { get; set; }
    }

    private static bool Utility_TryToPlaceItem_Prefix(
        GameLocation location,
        StardewValley.Object item,
        int x,
        int y,
        ref bool __result,
        out TryPlaceState __state)
    {
        __state = new TryPlaceState
        {
            PreviousIgnoreFarmer = ScreenState.Value.IgnoreSelfCollisionFarmer
        };

        try
        {
            ModConfig config = GetConfig();
            if (!config.Enabled || location is null || item is null || Game1.player is null)
                return true;

            PlacementPassability passability = PassabilityClassifier.Classify(item, location);
            if (passability != PlacementPassability.Impassable)
                return true;

            Point anchorTile = new Point(x / Game1.tileSize, y / Game1.tileSize);
            Rectangle footprint = PassabilityClassifier.GetPlacedFootprint(item, location, anchorTile);
            if (!PlayerPushService.DoesFarmerOverlapFootprint(Game1.player, footprint))
                return true;

            if (!config.AllowSolidPlacementUnderPlayer)
            {
                __result = false;
                return false;
            }

            if (!PlayerPushService.TryFindPushDestination(location, Game1.player, footprint, config, out Point destinationTile))
            {
                __result = false;
                return false;
            }

            __state.PendingPush = new PendingPlacementPush(location, footprint, anchorTile, destinationTile);
            ScreenState.Value.IgnoreSelfCollisionFarmer = Game1.player;
            return true;
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed in {nameof(Utility_TryToPlaceItem_Prefix)}:\n{ex}", LogLevel.Error);
            return true;
        }
    }

    private static void Utility_TryToPlaceItem_Postfix(bool __result, TryPlaceState __state)
    {
        try
        {
            if (!__result || __state.PendingPush is null || Game1.player is null)
                return;

            PendingPlacementPush push = __state.PendingPush;
            PlayerPushService.ExecutePush(
                Game1.player,
                push.Location,
                push.Footprint,
                push.AnchorTile,
                push.DestinationTile,
                GetConfig(),
                ScreenState.Value
            );
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed in {nameof(Utility_TryToPlaceItem_Postfix)}:\n{ex}", LogLevel.Error);
        }
    }

    private static void Utility_TryToPlaceItem_Finalizer(TryPlaceState __state)
    {
        ScreenState.Value.IgnoreSelfCollisionFarmer = __state.PreviousIgnoreFarmer;
    }
}



