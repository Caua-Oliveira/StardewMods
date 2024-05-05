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
        // Get the object at the specified tile
        StardewValley.Object obj = location.getObjectAtTile((int)tile.X, (int)tile.Y);
        bool itemCantBreak = !(player.CurrentItem is Pickaxe or Axe);
        // If obj is null, return false immediately
        if (obj == null)
        {
            return false;
        }

        switch (obj)
        {
            case var _ when obj.IsWeeds():
                if (config.AnyToolForWeeds && !(location is MineShaft))
                    ModEntry.SetTool(player, typeof(Pickaxe), anyTool: true);

                else if (config.ScytheForWeeds)
                    ModEntry.SetTool(player, typeof(MeleeWeapon));
                break;

            case CrabPot crabPot when crabPot.bait.Value == null:
                if (config.BaitForCrabPot)
                    ModEntry.SetItem(player, "Bait", "Bait");
                break;

            case var _ when obj.IsBreakableStone():
                if (config.PickaxeForStoneAndOres && (player.CurrentItem == null || !player.CurrentItem.Name.Contains("Bomb") && !player.CurrentItem.Name.Contains("Staircase")))
                    ModEntry.SetTool(player, typeof(Pickaxe));
                break;

            case var _ when obj.IsTwig():
                if (config.AxeForTwigs)
                    ModEntry.SetTool(player, typeof(Axe));
                break;

            case var _ when obj.Name == "Furnace":
                if (config.OresForFurnaces && itemCantBreak && (player.CurrentItem == null || !player.CurrentItem.Name.Contains("Ore")))
                    ModEntry.SetItem(player, "Resource", "Ore");
                break;

            case var _ when obj.Name == "Cheese Press":
                if (config.MilkForCheesePress && itemCantBreak)
                    ModEntry.SetItem(player, "Animal Product", "Milk");
                break;

            case var _ when obj.Name == "Mayonnaise Machine":
                if (config.EggsForMayoMachine && itemCantBreak)
                    ModEntry.SetItem(player, "Animal Product", "Egg");
                break;

            case var _ when obj.Name == "Artifact Spot":
                if (config.HoeForArtifactSpots)
                    ModEntry.SetTool(player, typeof(Hoe));
                break;

            case var _ when obj.Name == "Garden Pot":
                if (config.WateringCanForGardenPot && (player.CurrentItem == null || (itemCantBreak && player.CurrentItem.getCategoryName() != "Seed")))
                    ModEntry.SetTool(player, typeof(WateringCan));
                break;

            case var _ when obj.Name == "Seed Spot":
                if (config.HoeForArtifactSpots)
                    ModEntry.SetTool(player, typeof(Hoe));
                break;

            case var _ when obj.Name == "Barrel":
                if (config.WeaponForMineBarrels)
                    ModEntry.SetTool(player, typeof(MeleeWeapon), "Weapon");
                break;

            case var _ when obj.Name == "Supply Crate":
                if (config.AnyToolForSupplyCrates)
                    ModEntry.SetTool(player, typeof(Hoe), anyTool: true);
                break;

            case var _ when obj.Name == "Recycling Machine":
                if (config.TrashForRecycling && itemCantBreak)
                    ModEntry.SetItem(player, "Trash", "Joja");
                break;

            case var _ when obj.Name == "Bone Mill":
                if (config.BoneForBoneMill && itemCantBreak)
                    ModEntry.SetItem(player, "Resource", "Bone Fragment");
                break;

            case var _ when obj.Name == "Loom":
                if (config.WoolForLoom && itemCantBreak)
                    ModEntry.SetItem(player, "Animal Product", "Wool");
                break;

            case var _ when obj.Name == "Fish Smoker":
                if (config.FishForSmoker && itemCantBreak)
                    ModEntry.SetItem(player, "Fish");
                break;

            case var _ when obj.Name == "Bait Maker":
                if (config.FishForBaitMaker && itemCantBreak)
                    ModEntry.SetItem(player, "Fish");

                break;

            case var _ when obj.Name == "Crystalarium":
                if (config.MineralsForCrystalarium && (player.CurrentItem == null || (itemCantBreak && player.CurrentItem.getCategoryName() != "Mineral")))
                    ModEntry.SetItem(player, "Mineral");
                break;

            case var _ when obj.Name == "Seed Maker":
                if (config.SwapForSeedMaker && itemCantBreak)
                    ModEntry.SetItem(player, "Crops");

                break;

            case var _ when obj.Name == "Keg":
                if (itemCantBreak && config.SwapForKegs != "None")
                    ModEntry.SetItem(player, "Crops", crops: config.SwapForKegs);

                break;

            case var _ when obj.Name == "Preserves Jar":
                if (itemCantBreak && config.SwapForPreservesJar != "None")
                    ModEntry.SetItem(player, "Crops", crops: config.SwapForPreservesJar);
                break;
        }
        return true;
    }

    public bool TerrainFeatures(GameLocation location, Vector2 tile, Farmer player)
    {
        //Check for Bushes
        foreach (var features in location.largeTerrainFeatures)
        {
            if (!(features is Bush) || !config.ScytheForBushes)
                break;

            Bush bush = features as Bush;
            var bushBox = bush.getBoundingBox();
            Vector2 tilePixel = new Vector2(tile.X * Game1.tileSize, tile.Y * Game1.tileSize);

            // Check if the tile is within the bounding box
            if (bushBox.Contains((int)tilePixel.X, (int)tilePixel.Y) && bush.inBloom())
            {
                ModEntry.SetTool(player, typeof(MeleeWeapon));

                return true;
            }
        }

        //Returns if there is no TerrainFeatures
        bool hasTerrainFeature = location.terrainFeatures.ContainsKey(tile);
        if (!hasTerrainFeature)
            return false;

        var feature = location.terrainFeatures[tile];

        //Check if need Axe
        if (feature is Tree tree)
        {
            if (tree.hasMoss && tree.growthStage >= Tree.stageForMossGrowth && config.ScytheForMossOnTrees)
            {
                ModEntry.SetTool(player, typeof(MeleeWeapon));

                return true;
            }

            if (!config.AxeForTrees || player.CurrentItem != null && player.CurrentItem.Name == "Tapper") { return true; }

            if (!(tree.growthStage < Tree.treeStage && config.IgnoreGrowingTrees))
            {
                ModEntry.SetTool(player, typeof(Axe));

                return true;
            }

            return true;
        }

        //Check if need to harvest Grass
        if (feature is Grass && !(player.CurrentTool is MilkPail or Shears) && config.ScytheForGrass)
        {
            Console.WriteLine(0);
            ModEntry.SetTool(player, typeof(MeleeWeapon));
            return true;
        }

        //Check for tillable soil or crops
        if (feature is HoeDirt)
        {
            HoeDirt dirt = feature as HoeDirt;

            if (dirt.crop == null && config.SeedForTilledDirt)
            {
                if (player.CurrentItem == null || player.CurrentItem.getCategoryName() != "Seed")
                    ModEntry.SetItem(player, "Seed");

                return true;
            }
            //Check if can harvest the crop
            if (dirt.crop != null && (dirt.readyForHarvest() || (bool)dirt.crop.dead) && config.ScytheForCrops)
            {
                ModEntry.SetTool(player, typeof(MeleeWeapon));

                return true;

            }

            // Check if it is ginger crop
            if (dirt.crop != null && (string)dirt.crop.whichForageCrop == "2" && config.HoeForGingerCrop)
            {
                ModEntry.SetTool(player, typeof(Hoe));

                return true;
            }

            //Check if need to water the crop
            if ((dirt.crop != null && !dirt.isWatered() && !dirt.readyForHarvest()) && !(player.isRidingHorse() && player.mount.Name.Contains("tractor") && player.CurrentTool is Hoe))
            {
                if (!(config.PickaxeOverWateringCan && player.CurrentTool is Pickaxe) && config.HoeForGingerCrop)
                {
                    ModEntry.SetTool(player, typeof(WateringCan));

                    return true;
                }
            }
            return true;
        }
        return true;
    }

    public bool ResourceClumps(GameLocation location, Vector2 tile, Farmer player)
    {

        //Check if it is an boulder or logs and stumps
        for (int i = 0; i < location.resourceClumps.Count; i++)
        {
            var clump = location.resourceClumps[i];
            bool tileHasClump = clump.occupiesTile((int)tile.X, (int)tile.Y);
            if (tileHasClump)
            {
                if (clump is GiantCrop && config.AxeForGiantCrops)
                {
                    ModEntry.SetTool(player, typeof(Axe));

                    return true;
                }

                switch (clump.parentSheetIndex)
                {
                    //Id's for logs and stumps
                    case 602 or 600:
                        if (config.AxeForStumpsAndLogs)
                            ModEntry.SetTool(player, typeof(Axe));

                        return true;

                    //Id's for boulders
                    case 758 or 756 or 754 or 752 or 672 or 622 or 148:
                        if (config.PickaxeForBoulders)
                            ModEntry.SetTool(player, typeof(Pickaxe));

                        return true;
                }
                return true;
            }
        }
        return false;
    }

    public bool Monsters(GameLocation location, Vector2 tile, Farmer player)
    {

        //Check for monsters 
        foreach (var monster in location.characters)
        {
            Vector2 monsterTile = monster.Tile;
            float distance = Vector2.Distance(tile, monsterTile);

            if (monster.IsMonster && distance < config.MonsterRangeDetection)
            {
                //Pickaxe for non-moving cave crab
                if (monster is RockCrab)
                {
                    if (config.IgnoreCrabs)
                        return true;

                    RockCrab crab = monster as RockCrab;
                    var isShellLess = ModEntry.Helper.Reflection.GetField<NetBool>(crab, "shellGone").GetValue();
                    if (!isShellLess && !crab.isMoving())
                    {
                        ModEntry.SetTool(player, typeof(Pickaxe));
                        return true;

                    }
                }

                if (player.CurrentItem == null || (!player.CurrentItem.Name.Contains("Bomb") && !player.CurrentItem.Name.Contains("Staircase")))
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

        //Check for pet bowls
        bool isPetBowlOrStable = false;
        bool hasBuilding = location.getBuildingAt(tile) != null;
        bool hasWaterSource = location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "WaterSource", "Back") != null;
        bool hasWater = location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Back") != null;

        if (hasBuilding)
            isPetBowlOrStable = location.getBuildingAt(tile).GetType() == typeof(PetBowl) || location.getBuildingAt(tile).GetType() == typeof(Stable);

        if (isPetBowlOrStable && config.WateringCanForPetBowl)
        {
            ModEntry.SetTool(player, typeof(WateringCan));
            return true;

        }

        //Check for pan spots
        var dtile = player.GetToolLocation(false) / 64;
        dtile = new Vector2((int)tile.X, (int)tile.Y);
        Rectangle orePanRect = new Rectangle(player.currentLocation.orePanPoint.X * 64 - 64, player.currentLocation.orePanPoint.Y * 64 - 64, 256, 256);
        if (orePanRect.Contains((int)dtile.X * 64, (int)dtile.Y * 64) && Utility.distance((float)player.StandingPixel.X, (float)orePanRect.Center.X, (float)player.StandingPixel.Y, (float)orePanRect.Center.Y) <= 192f)
        {
            if (config.PanForPanningSpots)
            {
                ModEntry.SetTool(player, typeof(Pan));
                return true;
            }

        }

        //Check water for fishing
        if (config.FishingRodOnWater && (!(location is Farm or VolcanoDungeon || location.InIslandContext() || location.isGreenhouse) && hasWater && !(player.CurrentTool is WateringCan or Pan)))
        {
            if (config.FishingRodOnWater)
            {
                ModEntry.SetTool(player, typeof(FishingRod));
                return true;
            }

        }

        //Check for water source to refil watering can
        if (config.WateringCanForWater && ((hasWaterSource || hasWater) && !(player.CurrentTool is FishingRod or Pan)))
        {
            if (config.WateringCanForWater)
            {
                ModEntry.SetTool(player, typeof(WateringCan));
                return true;
            }

        }
        return false;
    }

    public bool Animals(GameLocation location, Vector2 tile, Farmer player)
    {

        //Check for animals to milk or shear
        if (!(location is Farm or AnimalHouse))
            return false;

        foreach (FarmAnimal animal in location.getAllFarmAnimals())
        {
            string[] canMilk = { "Goat", "Cow" };
            string[] canShear = { "Sheep" };
            float distance = Vector2.Distance(tile, animal.Tile);

            if (config.MilkPailForCowsAndGoats && canMilk.Any(animal.displayType.Contains) && distance <= 1 && animal.currentLocation == player.currentLocation)
            {
                ModEntry.SetTool(player, typeof(MilkPail));

                return true;
            }

            if (config.ShearsForSheeps && canShear.Any(animal.displayType.Contains) && distance <= 1 && animal.currentLocation == player.currentLocation)
            {
                ModEntry.SetTool(player, typeof(Shears));

                return true;
            }
        }

        //Check for feeding bench
        bool hasFeedingBench = location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Trough", "Back") != null;
        if (location is AnimalHouse && hasFeedingBench)
        {
            ModEntry.SetItem(player, "", "Hay");

            return true;
        }

        return false;

    }

    public bool ShouldSwapToHoe(GameLocation location, Vector2 tile, Farmer player)
    {
        if (!ModEntry.isTractorModInstalled || (player.isRidingHorse() && player.mount.Name.Contains("tractor")))
            return false;
        bool isNotScythe = true;
        bool isDiggable = location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Diggable", "Back") != null;
        bool isFightingLocations = location is Mine or MineShaft or VolcanoDungeon;
        if (player.CurrentItem != null)
            isNotScythe = player.CurrentItem.getCategoryName().Contains("Level");

        //Check if it should swap to Hoe
        if (!config.HoeForDiggableSoil || !isDiggable || isFightingLocations || location.isPath(tile))
            return false;
        if (player.CurrentItem is MeleeWeapon && isNotScythe && Game1.spawnMonstersAtNight)
            return false;
        if (player.CurrentItem is FishingRod or GenericTool or Wand)
            return false;

        if (player.CurrentItem == null)
        {
            ModEntry.SetTool(player, typeof(Hoe));
            return true;
        }

        if (!player.CurrentItem.canBePlacedHere(location, tile, CollisionMask.All, true))
        {
            ModEntry.SetTool(player, typeof(Hoe));
            return true;
        }
        return false;
    }
}
