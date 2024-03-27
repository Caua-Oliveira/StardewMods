using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace AutomateToolSwap
{
    internal class ModConfig
    {
        public bool Enabled { get; set; } = true;

        public SButton ToggleKey { get; set; } = SButton.CapsLock;

        public bool Pickaxe_greater_wcan { get; set; } = false;

        public bool Pickaxe_over_melee { get; set; } = false;
    }
}