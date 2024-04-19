using StardewModdingAPI.Utilities;

namespace AutomateToolSwap
{
    internal class ModConfig
    {
        public bool Enabled { get; set; } = true;

        // Keybind to toggle mod on/off
        public KeybindList ToggleKey { get; set; } = KeybindList.Parse("CapsLock");

        //If you should use the custumizable SwapKey or the game default
        public bool UseDifferentSwapKey { get; set; } = false;

        // Keybind to swap tools
        public KeybindList SwapKey { get; set; } = KeybindList.Parse("MouseLeft");

        // Keybind to switch back to the last used tool
        public KeybindList LastToolKey { get; set; } = KeybindList.Parse("MouseMiddle");

        // Detection method for tool switching (cursor or player orientation)
        public string DetectionMethod { get; set; } = "Cursor";

        // Auto-return to the last used tool after switching
        public bool AutoReturnToLastTool { get; set; } = false;

        // Automatically switch to hoe when clicking on empty soil
        public bool HoeForEmptySoil { get; set; } = true;

        // Automatically switch to scythe when clicking on grass
        public bool ScytheForGrass { get; set; } = false;

        // Prioritize using the pickaxe over the watering can on dry soil
        public bool PickaxeOverWateringCan { get; set; } = false;

        // Automatically switch to tools that are not a Scythe when clicking on weeds (fibers)
        public bool AnyToolForWeeds { get; set; } = false;

        public bool DisableTractorSwap { get; set; } = false;
    }
}