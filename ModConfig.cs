using StardewModdingAPI.Utilities;

namespace AutomateToolSwap
{
    internal class ModConfig
    {
        public bool Enabled { get; set; } = true;

        public KeybindList ToggleKey { get; set; } = KeybindList.Parse("CapsLock");

        public KeybindList SwapKey { get; set; } = KeybindList.Parse("MouseLeft");

        public bool Pickaxe_greater_wcan { get; set; } = false;

        public bool Pickaxe_over_melee { get; set; } = false;

        public KeybindList LastToolButton { get; set; } = KeybindList.Parse("MouseMiddle");

        public bool Hoe_in_empty_soil { get; set; } = true;

        public bool Auto_switch_last_tool { get; set; } = false;

        public bool Scythe_on_grass { get; set; } = false;

        public string Detection_method { get; set; } = "KBM";
    }
}