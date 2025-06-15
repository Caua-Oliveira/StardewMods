using StardewModdingAPI;

namespace OnlyWhenItCounts
{
    public class ModConfig
    {
        public enum TileDetectionMethod
        {
            PlayerDirection,
            CursorPosition
        }

        public TileDetectionMethod DetectionMethod { get; set; } = TileDetectionMethod.PlayerDirection;
    }
}