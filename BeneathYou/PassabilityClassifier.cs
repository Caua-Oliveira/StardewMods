using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;

namespace BeneathYou;

/// <summary>Passability classification for placeable items.</summary>
public enum PlacementPassability
{
    NotPlaceable,
    Passable,
    Impassable
}

/// <summary>Dynamically classifies whether an item is passable or solid upon placement.</summary>
internal static class PassabilityClassifier
{
    /// <summary>Classify an item dynamically using Stardew Valley 1.6 data and object methods.</summary>
    public static PlacementPassability Classify(Item? item, GameLocation? location)
    {
        if (item is not StardewValley.Object obj || !item.isPlaceable())
            return PlacementPassability.NotPlaceable;

        if (obj is Furniture furniture)
            return furniture.isPassable() ? PlacementPassability.Passable : PlacementPassability.Impassable;

        bool isWildTreeSeed = obj.IsWildTreeSapling();
        bool isFruitOrTeaSapling = obj.IsFruitTreeSapling() || obj.IsTeaSapling();
        bool isSeed = obj.Category == StardewValley.Object.SeedsCategory;
        bool isFertilizer = obj.Category == StardewValley.Object.fertilizerCategory;
        bool isRaisedCrop = false;

        if (isSeed && !isWildTreeSeed && !isFruitOrTeaSapling)
        {
            string seedId = location is not null ? Crop.ResolveSeedId(obj.ItemId, location) : obj.ItemId;
            isRaisedCrop = Crop.TryGetData(seedId, out var cropData) && cropData.IsRaised;
        }

        return ClassifyCore(
            isPlaceable: true,
            isWildTreeSeed: isWildTreeSeed,
            isFruitOrTeaSapling: isFruitOrTeaSapling,
            isSeed: isSeed,
            isRaisedCrop: isRaisedCrop,
            isFertilizer: isFertilizer,
            isObjectPassable: obj.isPassable()
        );
    }

    /// <summary>Pure classification rule suitable for unit testing without game state.</summary>
    public static PlacementPassability ClassifyCore(
        bool isPlaceable,
        bool isWildTreeSeed,
        bool isFruitOrTeaSapling,
        bool isSeed,
        bool isRaisedCrop,
        bool isFertilizer,
        bool isObjectPassable)
    {
        if (!isPlaceable)
            return PlacementPassability.NotPlaceable;

        if (isFruitOrTeaSapling)
            return PlacementPassability.Impassable;

        if (isWildTreeSeed)
            return PlacementPassability.Passable;

        if (isSeed)
            return (isRaisedCrop || !isObjectPassable) ? PlacementPassability.Impassable : PlacementPassability.Passable;

        if (isFertilizer)
            return PlacementPassability.Passable;

        return isObjectPassable ? PlacementPassability.Passable : PlacementPassability.Impassable;
    }

    /// <summary>Compute the tile footprint occupied by placing <paramref name="item"/> at <paramref name="anchorTile"/>.</summary>
    public static Rectangle GetPlacedFootprint(Item item, GameLocation? location, Point anchorTile)
    {
        if (item is Furniture furniture)
        {
            int y = anchorTile.Y;
            if (!furniture.isGroundFurniture())
                y = furniture.GetModifiedWallTilePosition(location, anchorTile.X, anchorTile.Y);

            int width = Math.Max(1, furniture.getTilesWide());
            int height = Math.Max(1, furniture.getTilesHigh());
            return new Rectangle(anchorTile.X, y, width, height);
        }

        return new Rectangle(anchorTile.X, anchorTile.Y, 1, 1);
    }

    /// <summary>Convert a tile-space footprint rectangle into world-pixel coordinates.</summary>
    public static Rectangle ToWorldRectangle(Rectangle tileFootprint)
    {
        return new Rectangle(
            tileFootprint.X * Game1.tileSize,
            tileFootprint.Y * Game1.tileSize,
            tileFootprint.Width * Game1.tileSize,
            tileFootprint.Height * Game1.tileSize
        );
    }
}

