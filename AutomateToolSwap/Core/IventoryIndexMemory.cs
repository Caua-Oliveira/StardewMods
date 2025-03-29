using AutomateToolSwap;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace Core;

public class IventoryIndexMemory
{
    public int currentIndex;
    public int lastIndex;
    public int auxIndex;
    public bool canSwitch;
    public List<KeybindList> keys = new();


    public IventoryIndexMemory()
    {
        currentIndex = 0;
        lastIndex = 0;
        auxIndex = 0;

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
            {
                await Task.Delay(100);
            }
        }
        else
        {
            for (int i = 0; i < keys.Count; i++)
            {
                while (keys[i].IsDown())
                {
                    await Task.Delay(100);
                }

            }
        }


        while (!player.canMove)
            await Task.Delay(100);

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
