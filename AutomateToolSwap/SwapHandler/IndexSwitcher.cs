using AutomateToolSwap;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
public class IndexSwitcher
{
    public int currentIndex;
    public int lastIndex;
    public int auxIndex;
    public bool canSwitch;
    public List<KeybindList> keys = new List<KeybindList>();


    public IndexSwitcher(int initialIndex)
    {
        currentIndex = initialIndex;
        lastIndex = initialIndex;
        auxIndex = initialIndex;

        // Default game keys
        keys.Add(KeybindList.Parse("ControllerX"));
        keys.Add(KeybindList.Parse("C"));
        keys.Add(KeybindList.Parse("MouseLeft"));
    }

    // Changes the index of the player inventory, so it holds the desired item
    public async Task SwitchIndex(int newIndex, Farmer player)
    {
        lastIndex = player.CurrentToolIndex;
        player.CurrentToolIndex = newIndex;
        currentIndex = newIndex;

        if (canSwitch)
        {
            await Waiter(player);
        }
    }

    // Waits until the player can move or stops using the item
    public async Task Waiter(Farmer player)
    {

        await Task.Delay(700);
        if (ModEntry.Config.UseDifferentSwapKey)
        {
            while (ModEntry.Config.SwapKey.IsDown())
                if (!ModEntry.Config.SwapKey.IsDown())
                    break;
        }
        else
        {
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i].IsDown())
                    while (keys[i].IsDown())
                        if (!keys[i].IsDown())
                            break;
            }
        }


        while (!player.canMove)
            await Task.Delay(1);

        GoToLastIndex(player);


    }

    // Goes back to last used index
    public void GoToLastIndex(Farmer player)
    {
        auxIndex = player.CurrentToolIndex;
        player.CurrentToolIndex = lastIndex;
        currentIndex = lastIndex;
        lastIndex = auxIndex;

    }
}
