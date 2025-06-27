using StardewValley;


namespace Integrations;
public interface IAnimalsNeedWaterAPI
{
    List<long> GetAnimalsLeftThirstyYesterday();
    bool WasAnimalLeftThirstyYesterday(FarmAnimal animal);
    List<string> GetBuildingsWithWateredTrough();
    bool IsAnimalFull(FarmAnimal animal);
    bool DoesAnimalHaveAccessToWater(FarmAnimal animal);
    List<long> GetFullAnimals();
    bool IsTileWaterTrough(int tileX, int tileY);
}

