using AutomateToolSwap;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.Tools;
using StardewValley.TerrainFeatures;
using StardewValley.Buildings;
using StardewValley.Monsters;
using Netcode;
using System.Threading;
using StardewModdingAPI;



//Get the object at the specified tile
public class Check
{
    private ModEntry ModEntry;
    private ModConfig config;

    public Check(ModEntry modEntry)
    {
        ModEntry = modEntry;
        config = ModEntry.Config;
    }

    public bool Objects(GameLocation location, Vector2 tile, Farmer player)
    {
        var obj = location.getObjectAtTile((int)tile.X, (int)tile.Y);
        bool itemCantBreak = !(player.CurrentItem is Pickaxe or Axe);
        bool isNotItemForEscape = player.CurrentItem == null || (!player.CurrentItem.Name.Contains("Bomb") && !player.CurrentItem.Name.Contains("Staircase"));
        if (obj == null)
            return false;

        if (ModEntry.ItemExtensionsAPI != null)
        {
            bool isClump = ModEntry.ItemExtensionsAPI.IsClump(obj.ItemId);
            string tool;
            var foundTool = ModEntry.ItemExtensionsAPI.GetBreakingTool(obj.ItemId, isClump, out tool);

            if (foundTool)
            {
                if (tool == "Pickaxe")
                {
                    if (config.PickaxeForStoneAndOres && (isNotItemForEscape))
                        ModEntry.SetTool(player, typeof(Pickaxe));
                    return true;
                }
                if (tool == "Axe")
                {
                    if (config.AxeForTwigs)
                        ModEntry.SetTool(player, typeof(Axe));
                    return true;
                }
            }

        }

        // Checks for characteristics of the object, and swaps items accordlingly
        switch (obj)
        {
            case var _ when obj.IsWeeds():

                if (config.AnyToolForWeeds && location is not MineShaft)
                    ModEntry.SetTool(player, typeof(Pickaxe), anyTool: true);
                else if (config.ScytheForWeeds)
                    ModEntry.SetTool(player, typeof(MeleeWeapon), "Scythe");
                return true;

            case var _ when obj.IsBreakableStone():

                if (config.PickaxeForStoneAndOres && (isNotItemForEscape))
                    ModEntry.SetTool(player, typeof(Pickaxe));
                return true;

            case var _ when obj.IsTwig():

                if (config.AxeForTwigs)
                    ModEntry.SetTool(player, typeof(Axe));
                return true;

            case var _ when obj.isForage():

                if (config.ScytheForForage)
                    ModEntry.SetTool(player, typeof(MeleeWeapon), "ScytheOnly");
                return true;

            case CrabPot crabPot when crabPot.bait.Value == null:

                if (config.BaitForCrabPot)
                    ModEntry.SetItem(player, "Bait", "Bait", aux: -21);
                return true;
        }

        // Checks for the name of the objects, and swaps items accordlingly
        switch (obj.Name)
        {
            case "Furnace":
                if (config.OresForFurnaces && itemCantBreak && (player.CurrentItem == null || !player.CurrentItem.Name.Contains("Ore")))
                    ModEntry.SetItem(player, "Resource", "Ore");
                return true;

            case "Heavy Furnace":
                if (config.OresForFurnaces && itemCantBreak && (player.CurrentItem == null || !player.CurrentItem.Name.Contains("Ore")))
                    ModEntry.SetItem(player, "Resource", "Ore");
                return true;

            case "Cheese Press":
                if (config.MilkForCheesePress && itemCantBreak)
                    ModEntry.SetItem(player, "Animal Product", "Milk", aux: -6);
                return true;

            case "Mayonnaise Machine":
                if (config.EggsForMayoMachine && itemCantBreak)
                    ModEntry.SetItem(player, "Animal Product", "Egg", aux: -5);
                return true;

            case "Artifact Spot":
                if (config.HoeForArtifactSpots)
                    ModEntry.SetTool(player, typeof(Hoe));
                return true;

            case "Garden Pot":
                if (config.WateringCanForGardenPot && (player.CurrentItem == null || (itemCantBreak && player.CurrentItem.category.Value != -74)))
                    ModEntry.SetTool(player, typeof(WateringCan));
                return true;

            case "Seed Spot":
                if (config.HoeForArtifactSpots)
                    ModEntry.SetTool(player, typeof(Hoe));
                return true;

            case "Barrel":
                if (config.WeaponForMineBarrels)
                    ModEntry.SetTool(player, typeof(MeleeWeapon), "Weapon");
                return true;

            case "Supply Crate":
                if (config.AnyToolForSupplyCrates)
                    ModEntry.SetTool(player, typeof(Hoe), anyTool: true);
                return true;

            case "Recycling Machine":
                if (config.TrashForRecycling && itemCantBreak)
                    ModEntry.SetItem(player, "Trash", "Joja", aux: -20);
                return true;

            case "Bone Mill":
                if (config.BoneForBoneMill && itemCantBreak)
                    ModEntry.SetItem(player, "Resource", "Bone Fragment");
                return true;

            case "Loom":
                if (config.WoolForLoom && itemCantBreak)
                    ModEntry.SetItem(player, "Animal Product", "Wool", aux: -18);
                return true;

            case "Fish Smoker":
                if (config.FishForSmoker && player.CurrentItem.category.Value != -4 && itemCantBreak)
                    ModEntry.SetItem(player, "Fish", aux: -4);
                return true;

            case "Bait Maker":
                if (config.FishForBaitMaker && itemCantBreak)
                    ModEntry.SetItem(player, "Fish", aux: -4);
                return true;

            case "Crystalarium":
                if (config.MineralsForCrystalarium && (player.CurrentItem == null || (itemCantBreak && player.CurrentItem.category.Value != -2)))
                    ModEntry.SetItem(player, "Mineral", aux: -2);
                return true;

            case "Seed Maker":
                if (config.SwapForSeedMaker && itemCantBreak)
                    ModEntry.SetItem(player, "Crops");
                return true;

            case "Keg":
                if (itemCantBreak && config.SwapForKegs != "None")
                    ModEntry.SetItem(player, "Crops", crops: config.SwapForKegs);
                return true;

            case "Preserves Jar":
                if (itemCantBreak && config.SwapForPreservesJar != "None")
                    ModEntry.SetItem(player, "Crops", crops: config.SwapForPreservesJar);
                return true;

            case "Dehydrator":
                if (itemCantBreak && config.SwapForDehydrator != "None")
                    ModEntry.SetItem(player, "Dehydratable", crops: config.SwapForDehydrator);
                return true;

            case "Oil Maker":
                if (itemCantBreak && config.TruffleForOilMaker)
                    ModEntry.SetItem(player, "Oil Maker", "Truffle", aux: -17);
                return true;

        }
        return true;
    }


    // TerrainFeatures are trees, bushes, grass and tilled dirt
    public bool TerrainFeatures(GameLocation location, Vector2 tile, Farmer player)
    {
        // bushes are a special case because they occupie more than one tile
        foreach (var terrainFeature in location.largeTerrainFeatures)
        {
            if (terrainFeature is not Bush || !config.ScytheForBushes)
                break;

            // gets the bounding box of the bush and transforms our tile value in pixels to see if the pixel is inside the bush
            // obtem a area delimitadora do arbusto e transforma o valor do bloco em pixels para ver se o pixel está dentro do arbusto
            var bush = terrainFeature as Bush;
            var bushBox = bush.getBoundingBox();
            var tilePixel = new Vector2(tile.X * Game1.tileSize, tile.Y * Game1.tileSize);

            if (bushBox.Contains((int)tilePixel.X, (int)tilePixel.Y) && bush.inBloom())
            {
                ModEntry.SetTool(player, typeof(MeleeWeapon), "Scythe");
                return true;
            }
        }

        if (!location.terrainFeatures.ContainsKey(tile))
            return false;

        var feature = location.terrainFeatures[tile];

        if (feature is Tree tree)
        {
            // Remove moss if needed swapping to Scythe
            if (tree.hasMoss.Value && tree.growthStage.Value >= Tree.stageForMossGrowth && config.ScytheForMossOnTrees)
            {
                ModEntry.SetTool(player, typeof(MeleeWeapon), "Scythe");
                return true;
            }

            // Return if the player has a tapper in hand (item that can be put in tree)
            if (!config.AxeForTrees || (player.CurrentItem != null && (player.CurrentItem.Name == "Tapper" || player.CurrentItem.Name == "Tree Fertilizer")))
                return true;

            // If the tree is not fully grown and the config to ignore it is enabled, skips, otherwise swaps to Axe 
            if (!(tree.growthStage.Value < Tree.treeStage && config.IgnoreGrowingTrees))
            {
                ModEntry.SetTool(player, typeof(Axe));
                return true;
            }

            return true;
        }

        // It does not swap to Scythe if the player is holding a animal tool, because it could break grass by mistake when trying to "harvest" a animal
        if (feature is Grass && !(player.CurrentTool is MilkPail || player.CurrentTool is Shears) && config.ScytheForGrass)
        {
            ModEntry.SetTool(player, typeof(MeleeWeapon), "ScytheOnly");
            return true;
        }


        // Tilled dirt
        if (feature is HoeDirt dirt)
        {
            bool dirtHasCrop = dirt.crop != null;

            if (!dirtHasCrop && config.SeedForTilledDirt)
            {
                if (!(config.PickaxeOverWateringCan && player.CurrentTool is Pickaxe))
                    if (player.CurrentItem == null || player.CurrentItem.category.Value != -74 || player.CurrentItem.HasContextTag("tree_seed_item"))
                        ModEntry.SetItem(player, "Seed");

                return true;
            }

            if (dirtHasCrop && (dirt.readyForHarvest() || dirt.crop.dead.Value) && config.ScytheForCrops)
            {
                ModEntry.SetTool(player, typeof(MeleeWeapon), "ScytheOnly");
                return true;
            }

            if (dirtHasCrop && !dirt.HasFertilizer() && dirt.CanApplyFertilizer("(O)369") && config.FertilizerForCrops)
            {
                if (!(config.PickaxeOverWateringCan) && player.CurrentTool is not Pickaxe && player.CurrentItem.category.Value != -19)
                    ModEntry.SetItem(player, "Fertilizer", "Tree", aux: -19);
                return true;
            }


            if (dirtHasCrop && dirt.crop.whichForageCrop.Value == "2" && config.HoeForGingerCrop)
            {
                ModEntry.SetTool(player, typeof(Hoe));
                return true;
            }

            // Swap to Watering Can if plant is not watered
            if (dirtHasCrop && !dirt.isWatered() && !dirt.readyForHarvest() && config.WateringCanForUnwateredCrop && !(player.isRidingHorse() && player.mount.Name.ToLower().Contains("tractor") && player.CurrentTool is Hoe))
            {
                if (!(config.PickaxeOverWateringCan && player.CurrentTool is Pickaxe))
                    ModEntry.SetTool(player, typeof(WateringCan));

                return true;
            }

            return true;
        }

        return false;
    }


    // ResourceClumps are stumps, logs, boulders and giant crops (they occupie more than one tile)
    public bool ResourceClumps(GameLocation location, Vector2 tilePosition, Farmer player)
    {
        bool IsStumpOrLog(ResourceClump resourceClump)
        {
            return new List<int> { 602, 600 }.Contains(resourceClump.parentSheetIndex.Value);
        }
        bool IsBoulder(ResourceClump resourceClump)
        {
            return new List<int> { 758, 756, 754, 752, 672, 622, 148 }.Contains(resourceClump.parentSheetIndex.Value);
        }


        foreach (var resourceClump in location.resourceClumps)
        {
            if (resourceClump.occupiesTile((int)tilePosition.X, (int)tilePosition.Y))
            {
                // Modded clumps
                if (ModEntry.ItemExtensionsAPI != null)
                {
                    string tool;
                    string itemId;
                    try { itemId = resourceClump.modDataForSerialization.FirstOrDefault().Values.First(); }
                    catch { itemId = ""; }
                    bool isClump = ModEntry.ItemExtensionsAPI.IsClump(itemId);
                    var foundTool = ModEntry.ItemExtensionsAPI.GetBreakingTool(itemId, isClump, out tool);

                    if (foundTool)
                    {
                        if (tool == "Pickaxe")
                        {
                            if (config.PickaxeForBoulders)
                                ModEntry.SetTool(player, typeof(Pickaxe));
                            return true;
                        }
                        if (tool == "Axe")
                        {
                            if (config.AxeForStumpsAndLogs)
                                ModEntry.SetTool(player, typeof(Axe));
                            return true;
                        }
                    }

                }

                if (config.AxeForGiantCrops && resourceClump is GiantCrop)
                {
                    if (player.CurrentItem == null || player.CurrentItem.Name != "Tapper")
                        ModEntry.SetTool(player, typeof(Axe));
                    return true;
                }

                if (config.AxeForStumpsAndLogs && IsStumpOrLog(resourceClump))
                {
                    ModEntry.SetTool(player, typeof(Axe));
                    return true;
                }

                if (config.PickaxeForBoulders && IsBoulder(resourceClump))
                {
                    ModEntry.SetTool(player, typeof(Pickaxe));
                    return true;
                }
            }
        }

        return false;
    }


    public bool Monsters(GameLocation location, Vector2 tile, Farmer player)
    {
        foreach (var character in location.characters)
        {
            if (character.IsMonster && Vector2.Distance(tile, character.Tile) < config.MonsterRangeDetection)
            {
                // Crabs are a special case
                if (character is RockCrab crab)
                {
                    if (config.IgnoreCrabs)
                        return true;

                    var isShellLess = ModEntry.Helper.Reflection.GetField<NetBool>(crab, "shellGone").GetValue();
                    if (!isShellLess.Value && !crab.isMoving())
                    {
                        ModEntry.SetTool(player, typeof(Pickaxe));
                        return true;
                    }
                }

                if ((location is Farm || location is SlimeHutch) && config.IgnoreSlimesOnFarm && character is GreenSlime)
                    return true;

                // If player is holding a bomb or staircase, don't swaps to weapon because they are used as escapes
                if (player.CurrentItem == null || !player.CurrentItem.Name.Contains("Bomb") && !player.CurrentItem.Name.Contains("Staircase"))
                {
                    ModEntry.SetTool(player, typeof(MeleeWeapon), "Weapon");
                    return true;
                }

                return true;
            }
        }

        return false;
    }

    public bool Water(GameLocation location, Vector2 tile, Farmer player)
    {
        bool IsPetBowlOrStable(GameLocation location, Vector2 tile)
        {
            var building = location.getBuildingAt(tile);
            return building != null && (building.GetType() == typeof(PetBowl) || building.GetType() == typeof(Stable));
        }
        bool IsPanSpot(GameLocation location, Vector2 tile, Farmer player)
        {
            var orePanRect = new Rectangle(player.currentLocation.orePanPoint.X * 64 - 64, player.currentLocation.orePanPoint.Y * 64 - 64, 256, 256);
            return orePanRect.Contains((int)tile.X * 64, (int)tile.Y * 64) && Utility.distance((float)player.StandingPixel.X, (float)orePanRect.Center.X, (float)player.StandingPixel.Y, (float)orePanRect.Center.Y) <= 192f;
        }
        bool IsWater(GameLocation location, Vector2 tile, Farmer player)
        {
            return location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Back") != null && !(player.CurrentTool is WateringCan || player.CurrentTool is Pan);
        }
        bool IsWaterSource(GameLocation location, Vector2 tile)
        {
            return location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "WaterSource", "Back") != null;
        }
        bool shouldUseWateringCan = location is Farm || location is VolcanoDungeon || location.InIslandContext() || location.isGreenhouse.Value;


        //Checks
        if (IsPetBowlOrStable(location, tile) && config.WateringCanForPetBowl)
        {
            ModEntry.SetTool(player, typeof(WateringCan));
            return true;
        }
        if (IsPanSpot(location, tile, player) && config.PanForPanningSpots)
        {
            ModEntry.SetTool(player, typeof(Pan));
            return true;
        }
        if ((IsWaterSource(location, tile) || IsWater(location, tile, player)) && shouldUseWateringCan && config.WateringCanForWater)
        {
            if (player.CurrentItem is not FishingRod)
                ModEntry.SetTool(player, typeof(WateringCan));
            return true;
        }
        if (IsWater(location, tile, player) && config.FishingRodOnWater)
        {
            if (player.CurrentItem is not WateringCan)
                ModEntry.SetTool(player, typeof(FishingRod));
            return true;
        }
        return false;
    }


    public bool Animals(GameLocation location, Vector2 tile, Farmer player)
    {
        if (!(location is Farm or AnimalHouse))
            return false;

        string[] animalsThatCanBeMilked = { "Goat", "Cow" };
        string[] animalsThatCanBeSheared = { "Sheep" };

        foreach (FarmAnimal animal in location.getAllFarmAnimals())
        {
            float distanceToAnimal = Vector2.Distance(tile, animal.Tile);

            if (config.MilkPailForCowsAndGoats && animalsThatCanBeMilked.Any(animal.type.Contains)
                && distanceToAnimal <= 1 && animal.currentLocation == player.currentLocation)
            {
                ModEntry.SetTool(player, typeof(MilkPail));
                return true;
            }
            if (config.ShearsForSheeps && animalsThatCanBeSheared.Any(animal.type.Contains)
                && distanceToAnimal <= 1 && animal.currentLocation == player.currentLocation)
            {
                ModEntry.SetTool(player, typeof(Shears));
                return true;
            }
        }

        bool isFeedingBench = location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Trough", "Back") != null;
        if (location is AnimalHouse && isFeedingBench)
        {
            ModEntry.SetItem(player, "", "Hay", aux: 0);
            return true;
        }

        return false;
    }

    public bool DiggableSoil(GameLocation location, Vector2 tile, Farmer player)
    {
        // If the player is in a tractor it will not swap to hoe because it would tille all the ground when moving
        if (ModEntry.isTractorModInstalled && player.isRidingHorse() && player.mount.Name.ToLower().Contains("tractor"))
            return false;
        bool isNotScythe = player.CurrentItem?.category.Value == -98;
        bool isDiggable = location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Diggable", "Back") != null;
        bool isInFightingLocations = location is Mine or MineShaft or VolcanoDungeon;
        bool isPlacebleItem = player.CurrentItem != null && player.CurrentItem.isPlaceable();

        if (!config.HoeForDiggableSoil || !isDiggable || isInFightingLocations)
            return false;
        if (player.CurrentItem is MeleeWeapon && isNotScythe && Game1.spawnMonstersAtNight)
            return false;
        if (player.CurrentItem is FishingRod or GenericTool or Wand)
            return false;


        if (location.isPath(tile))
        {
            if (config.PickaxeForFloorTile && player.CurrentItem is not Pickaxe && !isPlacebleItem)
                ModEntry.SetTool(player, typeof(Pickaxe));
            return true;
        }

        if (player.CurrentItem == null || !isPlacebleItem)
        {
            ModEntry.SetTool(player, typeof(Hoe));
            return true;
        }

        return false;
    }
}
