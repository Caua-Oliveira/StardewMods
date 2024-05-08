using AutomateToolSwap;
using StardewValley;
public class IndexSwitcher
{
    public int currentIndex;
    public int lastIndex;
    public int auxIndex;
    public bool canSwitch;
    public IndexSwitcher(int initialIndex)
    {
        currentIndex = initialIndex;
        lastIndex = initialIndex;
        auxIndex = initialIndex;
    }

    //Métodos para trocar o index do Item utilizado pelo jogador
    public void SwitchIndex(int newIndex)
    {
        //Coloca o Index do item desejado no Index do atual item do jogador, realizando assim a troca de items
        lastIndex = Game1.player.CurrentToolIndex;
        Game1.player.CurrentToolIndex = newIndex;
        currentIndex = newIndex;

        //Caso a opção de retornar para o ultimo item esteja ativada
        if (canSwitch)
        {
            Waiter();
        }
    }

    //Espera o jogador terminar de usar o item, para pode trocar para o ultimo item selecionado
    public async Task Waiter()
    {
        await Task.Delay(500);

        while (ModEntry.Config.SwapKey.IsDown())
            if (!ModEntry.Config.SwapKey.IsDown())
                break;

        while (!Game1.player.canMove)
            await Task.Delay(20);

        GoToLastIndex();


    }

    //Retorna para o ultimo item selecionado
    public void GoToLastIndex()
    {
        //Troca de valores de 2 variaveis com ajuda de uma auxiliar
        auxIndex = Game1.player.CurrentToolIndex;
        Game1.player.CurrentToolIndex = lastIndex;
        currentIndex = lastIndex;
        lastIndex = auxIndex;

    }
}
