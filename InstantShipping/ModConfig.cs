using StardewModdingAPI;

namespace InstantShipping
{
    public class ModConfig
    {
        public bool Enabled { get; set; } = true;
        public SButton SendToShippingBinKeybind { get; set; } = SButton.S;

        // Shipping behavior
        // "one" | "all" | "custom stack"
        public string DefaultShipMode { get; set; } = "all";
        public int DefaultShipStackAmount { get; set; } = 5;

        public bool AllowShippingOnlyIfMapThereIsBin { get; set; } = true;
        public int AllowedDistanceToBin { get; set; } = 20;

        // NEW: Immediately flag item as 'shipped' in the Collections page when sent.
        public bool ImmediateCollectionUpdate { get; set; } = false;
    }
}