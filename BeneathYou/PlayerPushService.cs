using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using xTile.Dimensions;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace BeneathYou;

/// <summary>Active smooth-push interpolation state for a single screen.</summary>
internal sealed class ActivePushSlide
{
    public GameLocation Location { get; }
    public Rectangle PlacedWorldBounds { get; }
    public Vector2 StartPosition { get; }
    public Vector2 TargetPosition { get; }
    public int FacingAfterPush { get; }
    public bool FacePlacedItem { get; }
    public int TicksElapsed { get; set; }
    public int TotalTicks { get; }

    public ActivePushSlide(
        GameLocation location,
        Rectangle placedWorldBounds,
        Vector2 startPosition,
        Vector2 targetPosition,
        int facingAfterPush,
        bool facePlacedItem,
        int totalTicks = 8)
    {
        this.Location = location;
        this.PlacedWorldBounds = placedWorldBounds;
        this.StartPosition = startPosition;
        this.TargetPosition = targetPosition;
        this.FacingAfterPush = facingAfterPush;
        this.FacePlacedItem = facePlacedItem;
        this.TotalTicks = totalTicks;
    }
}

/// <summary>Validates adjacent push tiles and safely displaces the farmer off newly placed solid items.</summary>
internal static class PlayerPushService
{
    private static readonly Point[] DiagonalOffsets =
    {
        new(-1, -1),
        new(1, -1),
        new(-1, 1),
        new(1, 1)
    };

    /// <summary>Convert a Stardew direction (0=Up, 1=Right, 2=Down, 3=Left) to a tile delta.</summary>
    public static Point DirectionToOffset(int direction)
    {
        return direction switch
        {
            0 => new Point(0, -1),
            1 => new Point(1, 0),
            2 => new Point(0, 1),
            3 => new Point(-1, 0),
            _ => new Point(0, 1)
        };
    }

    /// <summary>Get the opposite cardinal direction.</summary>
    public static int OppositeDirection(int direction) => (direction + 2) % 4;

    /// <summary>Build the ordered, deduplicated list of candidate tile offsets around the player.</summary>
    public static List<Point> BuildCandidateOffsets(
        PushDirectionPreference preference,
        int facingDirection,
        IReadOnlyList<int> heldMovementDirections,
        bool allowDiagonalPush)
    {
        var offsets = new List<Point>(8);
        var seen = new HashSet<Point>();

        void AddDirection(int dir)
        {
            if (dir is < 0 or > 3)
                return;
            Point offset = DirectionToOffset(dir);
            if (seen.Add(offset))
                offsets.Add(offset);
        }

        int back = OppositeDirection(facingDirection);
        int left = (facingDirection + 3) % 4;
        int right = (facingDirection + 1) % 4;

        switch (preference)
        {
            case PushDirectionPreference.MovementThenStepBack:
                foreach (int moveDir in heldMovementDirections)
                    AddDirection(moveDir);
                AddDirection(back);
                AddDirection(facingDirection);
                AddDirection(left);
                AddDirection(right);
                break;

            case PushDirectionPreference.StepBackFirst:
                AddDirection(back);
                AddDirection(facingDirection);
                AddDirection(left);
                AddDirection(right);
                break;

            case PushDirectionPreference.StepForwardFirst:
                AddDirection(facingDirection);
                AddDirection(back);
                AddDirection(left);
                AddDirection(right);
                break;

            case PushDirectionPreference.FacingDirectionOnly:
                AddDirection(facingDirection);
                return offsets;
        }

        if (allowDiagonalPush)
        {
            foreach (Point diag in DiagonalOffsets)
            {
                if (seen.Add(diag))
                    offsets.Add(diag);
            }
        }

        return offsets;
    }

    /// <summary>Compute which cardinal direction faces from <paramref name="fromTile"/> toward <paramref name="toTile"/>.</summary>
    public static int ComputeFacingDirectionToward(Point fromTile, Point toTile)
    {
        int dx = toTile.X - fromTile.X;
        int dy = toTile.Y - fromTile.Y;

        if (dx == 0 && dy == 0)
            return 2;

        if (Math.Abs(dx) >= Math.Abs(dy))
            return dx > 0 ? 1 : 3;

        return dy > 0 ? 2 : 0;
    }

    /// <summary>Check whether the farmer currently overlaps the placed tile footprint.</summary>
    public static bool DoesFarmerOverlapFootprint(Farmer player, Rectangle placedFootprint)
    {
        if (placedFootprint.Contains(player.TilePoint))
            return true;

        Rectangle worldBounds = PassabilityClassifier.ToWorldRectangle(placedFootprint);
        return player.GetBoundingBox().Intersects(worldBounds);
    }

    /// <summary>Find the highest-priority valid empty adjacent tile for pushing <paramref name="player"/>.</summary>
    public static bool TryFindPushDestination(
        GameLocation location,
        Farmer player,
        Rectangle placedFootprint,
        ModConfig config,
        out Point destinationTile)
    {
        destinationTile = Point.Zero;
        Point originTile = player.TilePoint;
        var heldDirections = GetHeldMovementDirections(player);
        var candidates = BuildCandidateOffsets(
            config.GetPushDirectionPreference(),
            player.FacingDirection,
            heldDirections,
            config.AllowDiagonalPush
        );

        foreach (Point offset in candidates)
        {
            Point candidate = new Point(originTile.X + offset.X, originTile.Y + offset.Y);
            if (placedFootprint.Contains(candidate))
                continue;

            Vector2 candidateVec = new Vector2(candidate.X, candidate.Y);
            if (!location.isTileOnMap(candidateVec))
                continue;

            if (!location.isTilePassable(new Location(candidate.X, candidate.Y), Game1.viewport))
                continue;

            Vector2 targetPos = GetCenteredPositionForTile(player, candidate);
            Rectangle targetBb = GetBoundingBoxAtPosition(player, targetPos);

            if (location.isCollidingPosition(targetBb, Game1.viewport, isFarmer: true, damagesFarmer: 0, glider: false, character: player))
                continue;
            destinationTile = candidate;
            return true;
        }

        return false;
    }

    /// <summary>Execute the player push immediately upon solid placement.</summary>
    public static void ExecutePush(
        Farmer player,
        GameLocation location,
        Rectangle placedFootprint,
        Point placedAnchorTile,
        Point destinationTile,
        ModConfig config,
        ScreenState screenState)
    {
        EnsureTemporaryPassableTiles(player, placedFootprint);

        Vector2 targetPos = GetCenteredPositionForTile(player, destinationTile);
        int facingTowardPlaced = ComputeFacingDirectionToward(destinationTile, placedAnchorTile);

        if (config.GetPushMovementStyle() == PushMovementStyle.InstantSnap)
        {
            player.Position = targetPos;
            if (config.FacePlacedItemAfterPush)
                player.faceDirection(facingTowardPlaced);
            screenState.ActivePush = null;
            return;
        }

        screenState.ActivePush = new ActivePushSlide(
            location: location,
            placedWorldBounds: PassabilityClassifier.ToWorldRectangle(placedFootprint),
            startPosition: player.Position,
            targetPosition: targetPos,
            facingAfterPush: facingTowardPlaced,
            facePlacedItem: config.FacePlacedItemAfterPush
        );
    }

    /// <summary>Advance an active smooth slide on each game tick.</summary>
    public static void UpdateSlide(Farmer? player, ScreenState screenState)
    {
        ActivePushSlide? slide = screenState.ActivePush;
        if (slide is null)
            return;

        if (player is null || !ReferenceEquals(player, Game1.player) || !ReferenceEquals(player.currentLocation, slide.Location))
        {
            screenState.ActivePush = null;
            return;
        }

        // If a menu/dialogue/event interrupts the short slide, snap out cleanly so the player is never trapped inside the placed object.
        if (Game1.activeClickableMenu is not null || Game1.dialogueUp || Game1.eventUp)
        {
            player.Position = slide.TargetPosition;
            if (slide.FacePlacedItem)
                player.faceDirection(slide.FacingAfterPush);
            screenState.ActivePush = null;
            return;
        }

        // Keep placed tiles passable while the farmer's bounding box clears them.
        AddBoundsTilesToTemporaryPassable(player, slide.PlacedWorldBounds);

        slide.TicksElapsed++;
        float t = Math.Clamp((float)slide.TicksElapsed / slide.TotalTicks, 0f, 1f);
        // Smooth ease-out curve for crisp, natural displacement.
        float eased = 1f - (1f - t) * (1f - t);
        player.Position = Vector2.Lerp(slide.StartPosition, slide.TargetPosition, eased);

        if (slide.TicksElapsed >= slide.TotalTicks)
        {
            player.Position = slide.TargetPosition;
            if (slide.FacePlacedItem)
                player.faceDirection(slide.FacingAfterPush);
            screenState.ActivePush = null;
        }
    }

    private static List<int> GetHeldMovementDirections(Farmer player)
    {
        var dirs = new List<int>(4);
        foreach (int dir in player.movementDirections)
        {
            if (dir is >= 0 and <= 3 && !dirs.Contains(dir))
                dirs.Add(dir);
        }

        if (dirs.Count > 0)
            return dirs;

        KeyboardState kb = Game1.GetKeyboardState();
        if (Game1.isOneOfTheseKeysDown(kb, Game1.options.moveUpButton))
            dirs.Add(0);
        if (Game1.isOneOfTheseKeysDown(kb, Game1.options.moveRightButton))
            dirs.Add(1);
        if (Game1.isOneOfTheseKeysDown(kb, Game1.options.moveDownButton))
            dirs.Add(2);
        if (Game1.isOneOfTheseKeysDown(kb, Game1.options.moveLeftButton))
            dirs.Add(3);

        return dirs;
    }

    private static Vector2 GetCenteredPositionForTile(Farmer player, Point tile)
    {
        Rectangle currentBb = player.GetBoundingBox();
        int offsetX = currentBb.X - (int)player.Position.X;
        int offsetY = currentBb.Y - (int)player.Position.Y;

        float targetCenterX = tile.X * Game1.tileSize + Game1.tileSize / 2f;
        float targetCenterY = tile.Y * Game1.tileSize + Game1.tileSize / 2f;

        return new Vector2(
            targetCenterX - (offsetX + currentBb.Width / 2f),
            targetCenterY - (offsetY + currentBb.Height / 2f)
        );
    }

    private static Rectangle GetBoundingBoxAtPosition(Farmer player, Vector2 position)
    {
        Rectangle currentBb = player.GetBoundingBox();
        int offsetX = currentBb.X - (int)player.Position.X;
        int offsetY = currentBb.Y - (int)player.Position.Y;
        return new Rectangle((int)position.X + offsetX, (int)position.Y + offsetY, currentBb.Width, currentBb.Height);
    }

    private static void EnsureTemporaryPassableTiles(Farmer player, Rectangle placedFootprint)
    {
        for (int x = placedFootprint.Left; x < placedFootprint.Right; x++)
        {
            for (int y = placedFootprint.Top; y < placedFootprint.Bottom; y++)
            {
                player.TemporaryPassableTiles.Add(new Rectangle(x * Game1.tileSize, y * Game1.tileSize, Game1.tileSize, Game1.tileSize));
            }
        }

        Point origin = player.TilePoint;
        player.TemporaryPassableTiles.Add(new Rectangle(origin.X * Game1.tileSize, origin.Y * Game1.tileSize, Game1.tileSize, Game1.tileSize));
    }

    private static void AddBoundsTilesToTemporaryPassable(Farmer player, Rectangle worldBounds)
    {
        int startX = worldBounds.Left / Game1.tileSize;
        int endX = (worldBounds.Right - 1) / Game1.tileSize;
        int startY = worldBounds.Top / Game1.tileSize;
        int endY = (worldBounds.Bottom - 1) / Game1.tileSize;

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                player.TemporaryPassableTiles.Add(new Rectangle(x * Game1.tileSize, y * Game1.tileSize, Game1.tileSize, Game1.tileSize));
            }
        }
    }
}



