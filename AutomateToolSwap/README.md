# AutomateToolSwap

[![Nexus Mods](https://img.shields.io/badge/Nexus%20Mods-⬇%20110k-blue?logo=nexusmods&logoColor=white&style=for-the-badge)](https://www.nexusmods.com/stardewvalley/mods/21050)
[![CurseForge](https://img.shields.io/badge/CurseForge-⬇%2015k-blue?logo=curseforge&logoColor=white&style=for-the-badge)](https://www.curseforge.com/stardewvalley/mods/automate-tool-swap)
[![Ko-Fi](https://img.shields.io/badge/Ko--fi-Support%20Me-ff7700?style=for-the-badge&logo=ko-fi&logoColor=white)](http://ko-fi.com/trapyy)

> **Automatically select the best tool or item for any interaction in Stardew Valley.**  
> Spend less time swapping tools and more time farming!

---

## Overview

**AutomateToolSwap** is a powerful, highly configurable Stardew Valley mod that streamlines your gameplay by intelligently picking the right tool or item from your inventory when you interact with objects in the world.

Examples:
- Click a **tree**: your **Axe** is auto-selected.
- Click a **rock**: your **Pickaxe** is auto-selected.
- Click a **Furnace**: the best available ore is selected.
- Click **tilled soil**: seeds or fertilizer are selected.

No more inventory juggling—just play!

---

## Features

- **Automatic Tool/Item Selection**  
  Instantly equips the best tool or item for the object you click or interact with. Supports a wide range of in-game objects and tools.

- **Fully Configurable Interactions**  
  Enable or disable auto-switching for *each* supported interaction type from the in-game config menu or `config.json`.

- **Return to Previous Item**  
  - Instantly switch back to your previous tool/item with a hotkey.
  - Optionally auto-return after an action completes.

- **Customizable Detection Methods**  
  Choose how the mod detects your target:
    - Cursor-based (default)
    - Cursor-only
    - Player-facing

- **Compatibility**  
  - [Generic Mod Config Menu (GMCM)](https://www.nexusmods.com/stardewvalley/mods/5098) for easy configuration.
  - Supports mods such as Item Extensions and Ranged Tools.
  - Designed to work well with Tractor Mod and similar mods.

---

## Installation

1. **Install [SMAPI](https://smapi.io/).**
2. *(Recommended)* Install [Generic Mod Config Menu (GMCM)](https://www.nexusmods.com/stardewvalley/mods/5098) for easy configuration.
3. **Download AutomateToolSwap** from [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/21050) or [CurseForge](https://www.curseforge.com/stardewvalley/mods/automate-tool-swap).
4. **Extract** the contents into your `Stardew Valley/Mods` directory.
5. **Launch** Stardew Valley via SMAPI.

---

## Configuration

- **In-Game (Recommended):**  
  Open the config menu with GMCM via the gear icon on the title screen or from the in-game options.

- **Manual Edit:**  
  Edit `config.json` in `Stardew Valley/Mods/AutomateToolSwap` *(not recommended)*.

### Key Settings

- **Interaction Toggles:**  
  Enable/disable auto-selection for each interaction (e.g. trees, rocks, furnaces, soil).

- **Return Behavior:**  
  - `AutoReturnToLastTool`: Automatically returns to last holded item after completing an action.
  - `ReturnToPreviousItemKey`: Defines the hotkey for a manual return function.

- **Item Detection Method:**  
  - `Cursor` (default): Uses mouse tile if in range, else player-facing.
  - `CursorOnly`: Always uses the tile under your cursor.
  - `Player`: Always uses the tile the player is facing (best for controller users).

For a detailed explanation, see the **Item Detection Methods** section below.

---

## Item Detection Methods

- **Cursor**:  
  Targets the tile under your mouse if in range; otherwise, uses the player’s facing tile. *(Recommended for most players)*

- **CursorOnly**:  
  Always uses the mouse tile, regardless of range or direction. *(For those who want precision targeting)*

- **Player**:  
  Always uses the tile directly in front of your character. *(Best for controller users)*

---

## Mod Compatibility

- **Generic Mod Config Menu:** Fully supported.
- **Tractor Mod:** Works with vanilla tools; custom tractor-only tools may not be supported.
- **Item Extension Mods:** Recognizes new ores and clumps, swaps tools accordingly.
- **Ranged Tools:** Uses axe range as default for tool range checks.

---

## Support

- **Bug Reports:**  
  Report issues on the [Nexus Mods Bugs Tab](https://www.nexusmods.com/stardewvalley/mods/21050?tab=bugs).

- **Questions / Suggestions:**  
  Use the [Nexus Mods Posts Tab](https://www.nexusmods.com/stardewvalley/mods/21050?tab=posts).

When reporting bugs, please include:
1. A clear description of the problem.
2. Steps to reproduce.
3. Your [SMAPI log](https://smapi.io/log).

---

*Entry point: `ModEntry.cs`*
