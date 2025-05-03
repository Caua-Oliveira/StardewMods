Entry point is ModEntry.cs <br> 

## Downloads
[![Nexus Mods](https://img.shields.io/badge/Nexus%20Mods-⬇%20110k-blue?logo=nexusmods&logoColor=white&style=for-the-badge)](https://www.nexusmods.com/stardewvalley/mods/21050)
[![CurseForge](https://img.shields.io/badge/CurseForge-⬇%2015k-blue?logo=curseforge&logoColor=white&style=for-the-badge)](https://www.curseforge.com/stardewvalley/mods/automate-tool-swap)
<br>



## AutomateToolSwap

[![Ko-Fi](https://img.shields.io/badge/Ko--fi-Support%20Me-ff7700?style=for-the-badge&logo=ko-fi&logoColor=white)](http://ko-fi.com/trapyy)

**PLEASE READ THE DETECTION METHODS**

### **Overview**

AutomateToolSwap is a Stardew Valley mod designed to streamline your gameplay by intelligently selecting the appropriate tool or item from your inventory when you interact with objects in the world. Spend less time scrolling through your hotbar and more time farming, mining, and exploring!

For example, clicking on a tree will automatically select your `Axe`, clicking on a `Furnace` will select the best available ore, and clicking on tilled soil might select seeds or fertilizer. This mod is highly customizable, allowing you to enable or disable automatic switching for *each* specific type of interaction.

### **Features**

* **Automatic Tool/Item Selection:** Detects your interaction target and selects the best tool/item (`Axe` for trees, `Pickaxe` for rocks, Ore for `Furnace`, Seeds for soil, etc.). Covers a wide range of common interactions.
* **Fully Configurable Interactions:** Individually enable or disable automatic switching for *each* supported interaction type via the config menu.
* **Return to Previous Item:**
    * Instantly switch back to your previously held item using a configurable hotkey.
    * Optionally enable automatic return to the previous item after an action is completed.
* **Configurable Item Detection:** Choose *how* the mod determines your target (`Cursor-based`, `Cursor-Only`, or `Player-Facing`).

### **How to Use**

1.  **Install SMAPI:** Ensure you have the latest version of [SMAPI](https://smapi.io/) installed.
2.  **Install Generic Mod Config Menu (Recommended):** Download and install [Generic Mod Config Menu (GMCM)](https://www.nexusmods.com/stardewvalley/mods/5098) for easy in-game settings adjustment.
3.  **Download AutomateToolSwap:**.
4.  **Install AutomateToolSwap:** Unzip the download and place the `AutomateToolSwap` folder into your `Stardew Valley/Mods` directory.
5.  **Launch the Game:** Run Stardew Valley via SMAPI.

* **Accessing Configuration:**
    * **With GMCM:** Click the gear icon on the title screen or find the settings button in the in-game options menu.
    * **Without GMCM:** Edit the `config.json` file located in `Stardew Valley/Mods/AutomateToolSwap` (not recommended).

### **Configuration Settings**

The following settings are available in the config menu (`GMCM`) or `config.json`:

* **Interaction Toggles:**
    * A series of checkboxes (e.g., `AutoSelectAxeForTree`, `AutoSelectPickaxeForRock`, `AutoSelectOreForFurnace`, etc.).
    * Check to enable auto-selection for that specific interaction; uncheck to disable.
* **Return Behavior:**
    * `ReturnToPreviousItemMode`: Controls how switching back works.
        * `Manual`: Requires pressing the hotkey.
        * `Automatic`: Automatically switches back after the action completes.
    * `ReturnToPreviousItemKey`: Defines the hotkey for the *manual* return function.
* **Item Detection Method:**
    * `DetectionMethod`: Choose how the interaction target is determined.
        * `Cursor`: Prioritizes the mouse cursor tile, falls back to player direction if cursor is out of range. (Recommended Default)
        * `CursorOnly`: Exclusively uses the mouse cursor tile.
        * `Player`: Exclusively uses the tile the player is facing.
    * (*See the 'Item Detection Methods' section below for a detailed explanation of each option*).

### **Item Detection Methods**

Understanding how the mod detects your target can help you choose the best setting (`DetectionMethod`) for your playstyle:

* **`Cursor` Method:**
    * **How it works:** Looks at the game tile directly under your mouse cursor. If that tile is within interaction range, it's used as the target. If the cursor is too far away (outside interaction range), the mod checks the tile directly in front of the player instead.
    * **Best for:** Most keyboard and mouse users. Offers flexibility between precise cursor targeting and quick directional interaction.
* **`CursorOnly` Method:**
    * **How it works:** *Always* uses the game tile directly under your mouse cursor as the target, no matter how far away it is or where the player is facing.
    * **Best for:** Players who want absolute precision based on cursor position, even when targeting things outside the normal interaction range (though the interaction itself will still be limited by game mechanics).
* **`Player` Method:**
    * **How it works:** *Always* uses the game tile directly in front of the player (based on their facing direction) as the target. The mouse cursor position is ignored for determining the target.
    * **Best for:** Controller users, otherwise, the interactions may not function properly.

### **Mod Compatibility**

* **Generic Mod Config Menu (GMCM):** Fully supported for easy in-game configuration.
* **Tractor Mod:** The mod will behave exactly the same way as it already does, and that means that any interactions the Tractor Mod has with tools that are not in the vanilla game will do nothing. The only difference being that when you are in a Tractor, the tools will insta-swap whenever it detects something, so there's no need to click. Also, it won't swap to the Hoe because otherwise you would be tilling all the soil whenever you move, so you have to switch manually if you want to do it.
* **Item Extension Mods:** The mod recognizes the new nodes ores and clumps and swaps tools accordingly.
* **Ranged Tools:** The mod will check only your axe range and use it as a default for the interaction range of anything.

### **Support**

* **Bug Reports:** Please report any issues and bugs on my [Nexus Mods Bugs Tab](https://www.nexusmods.com/stardewvalley/mods/21050?tab=bugs).
* **Questions & Suggestions:** You can ask questions or feature requests on the [Nexus Mods Posts Tab](https://www.nexusmods.com/stardewvalley/mods/21050?tab=posts).

When reporting bugs, please include:

1.  A clear description of the problem.
2.  Steps to reproduce the issue.
3.  Your SMAPI log ([How to find your SMAPI log](https://smapi.io/log)).
---
