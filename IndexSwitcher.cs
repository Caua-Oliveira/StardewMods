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
        keys.Add(KeybindList.Parse("ControllerX"));
        keys.Add(KeybindList.Parse("C"));
        keys.Add(KeybindList.Parse("MouseLeft"));
    }

    public async Task SwitchIndex(int newIndex)
    {

        lastIndex = Game1.player.CurrentToolIndex;
        Game1.player.CurrentToolIndex = newIndex;
        currentIndex = newIndex;

        if (canSwitch)
        {
            await Waiter();
        }
    }

    public async Task Waiter()
    {

        await Task.Delay(500);
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


        while (!Game1.player.canMove)
            await Task.Delay(20);

        GoToLastIndex();


    }
    public void GoToLastIndex()
    {
        auxIndex = Game1.player.CurrentToolIndex;
        Game1.player.CurrentToolIndex = lastIndex;
        currentIndex = lastIndex;
        lastIndex = auxIndex;

    }
}
