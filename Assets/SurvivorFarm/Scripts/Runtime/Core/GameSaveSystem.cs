using System;
using System.Collections.Generic;
using System.IO;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Core
{
    public sealed class GameSaveSystem : MonoBehaviour
    {
        private const int SaveVersion = 4;

        [SerializeField] private float autoSaveInterval = 8f;
        [SerializeField] private Transform player;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerSurvivalStats survivalStats;
        [SerializeField] private PlayerMovementController movement;
        [SerializeField] private PlayerToolbelt toolbelt;
        [SerializeField] private PlayerToolUpgradeController toolUpgrades;
        [SerializeField] private PlayerCraftingController crafting;
        [SerializeField] private TutorialQuestSystem questSystem;
        [SerializeField] private ShopEntrance shopEntrance;
        [SerializeField] private DungeonEntrance dungeonEntrance;

        private float nextAutoSaveTime;

        private string SavePath => Path.Combine(Application.persistentDataPath, "survivor_farm_save.json");

        public void Configure(
            Transform playerTransform,
            PlayerInventory playerInventory,
            PlayerSurvivalStats playerSurvivalStats,
            PlayerMovementController playerMovement,
            PlayerToolbelt playerToolbelt,
            PlayerToolUpgradeController playerToolUpgrades,
            PlayerCraftingController playerCrafting,
            TutorialQuestSystem playerQuestSystem,
            ShopEntrance entrance,
            DungeonEntrance dungeon)
        {
            player = playerTransform;
            inventory = playerInventory;
            survivalStats = playerSurvivalStats;
            movement = playerMovement;
            toolbelt = playerToolbelt;
            toolUpgrades = playerToolUpgrades;
            crafting = playerCrafting;
            questSystem = playerQuestSystem;
            shopEntrance = entrance;
            dungeonEntrance = dungeon;
            nextAutoSaveTime = Time.time + autoSaveInterval;
        }

        private void Update()
        {
            if (Time.time < nextAutoSaveTime)
            {
                return;
            }

            SaveGame(false);
            nextAutoSaveTime = Time.time + autoSaveInterval;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveGame(false);
            }
        }

        private void OnApplicationQuit()
        {
            SaveGame(false);
        }

        public void SaveGame(bool notify)
        {
            if (player == null || inventory == null)
            {
                return;
            }

            GameSaveData data = BuildSaveData();
            string json = JsonUtility.ToJson(data, true);
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            File.WriteAllText(SavePath, json);

            if (notify)
            {
                FarmNotificationCenter.Show("Partida guardada.");
            }
        }

        public bool TryLoadGame()
        {
            if (!File.Exists(SavePath))
            {
                return false;
            }

            string json = File.ReadAllText(SavePath);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
            if (data == null || data.version != SaveVersion)
            {
                return false;
            }

            if (data.inventory == null)
            {
                data.inventory = new InventorySaveData();
            }

            if (data.survival == null)
            {
                data.survival = new SurvivalSaveData();
            }

            if (data.toolUpgrades == null)
            {
                data.toolUpgrades = new ToolUpgradeSaveData();
            }

            if (data.crafting == null)
            {
                data.crafting = new CraftingSaveData();
            }

            if (data.tutorialQuest == null)
            {
                data.tutorialQuest = new TutorialQuestSaveData();
            }

            if (data.plots == null)
            {
                data.plots = new List<PlotSaveData>();
            }

            if (data.resources == null)
            {
                data.resources = new List<ResourceSaveData>();
            }

            if (data.unlockZones == null)
            {
                data.unlockZones = new List<UnlockZoneSaveData>();
            }

            if (data.dungeonChests == null)
            {
                data.dungeonChests = new List<DungeonChestSaveData>();
            }

            RestoreSaveData(data);
            FarmNotificationCenter.Show("Partida cargada.");
            return true;
        }

        private GameSaveData BuildSaveData()
        {
            GameSaveData data = new GameSaveData
            {
                version = SaveVersion,
                playerPosition = SaveVector.From(player.position),
                insideShop = shopEntrance != null && shopEntrance.IsInsideShop,
                insideDungeon = dungeonEntrance != null && dungeonEntrance.IsInsideDungeon,
                selectedTool = toolbelt != null ? (int)toolbelt.SelectedTool : 0,
                movementMode = movement != null ? (int)movement.MovementMode : 0,
                inventory = new InventorySaveData
                {
                    commonSeeds = inventory.CommonSeeds,
                    mineralSeeds = inventory.MineralSeeds,
                    magicSeeds = inventory.MagicSeeds,
                    wood = inventory.Wood,
                    stone = inventory.Stone,
                    fruit = inventory.Fruit,
                    coins = inventory.Coins,
                    maxSeedsPerSlot = inventory.MaxSeedsPerSlot
                },
                survival = new SurvivalSaveData
                {
                    maxHealth = survivalStats != null ? survivalStats.MaxHealth : 5,
                    health = survivalStats != null ? survivalStats.CurrentHealth : 5,
                    hungerPercent = survivalStats != null ? survivalStats.HungerPercent : 1f
                },
                toolUpgrades = new ToolUpgradeSaveData
                {
                    axeLevel = toolUpgrades != null ? toolUpgrades.AxeLevel : 1,
                    pickaxeLevel = toolUpgrades != null ? toolUpgrades.PickaxeLevel : 1,
                    shovelLevel = toolUpgrades != null ? toolUpgrades.ShovelLevel : 1
                },
                crafting = new CraftingSaveData
                {
                    storageLevel = crafting != null ? crafting.StorageLevel : 0,
                    campLevel = crafting != null ? crafting.CampLevel : 0
                },
                tutorialQuest = new TutorialQuestSaveData
                {
                    questIndex = questSystem != null ? questSystem.QuestIndex : 0,
                    questProgress = questSystem != null ? questSystem.QuestProgress : 0
                }
            };

            List<FarmingPlot> plots = GetSortedPlots();
            for (int i = 0; i < plots.Count; i++)
            {
                data.plots.Add(new PlotSaveData
                {
                    index = i,
                    state = plots[i].StateId,
                    remainingGrowTime = plots[i].RemainingGrowTime,
                    seedRarity = plots[i].PlantedSeedRarityId
                });
            }

            List<HarvestableResource> resources = GetSortedResources();
            for (int i = 0; i < resources.Count; i++)
            {
                data.resources.Add(new ResourceSaveData
                {
                    index = i,
                    harvested = resources[i].IsHarvested
                });
            }

            List<LandUnlockZone> unlockZones = GetSortedUnlockZones();
            for (int i = 0; i < unlockZones.Count; i++)
            {
                data.unlockZones.Add(new UnlockZoneSaveData
                {
                    index = i,
                    unlocked = unlockZones[i].IsUnlocked
                });
            }

            List<DungeonChest> dungeonChests = GetSortedDungeonChests();
            for (int i = 0; i < dungeonChests.Count; i++)
            {
                data.dungeonChests.Add(new DungeonChestSaveData
                {
                    index = i,
                    opened = dungeonChests[i].IsOpened
                });
            }

            return data;
        }

        private void RestoreSaveData(GameSaveData data)
        {
            bool restoreDungeon = data.insideDungeon;
            bool restoreShop = data.insideShop && !restoreDungeon;

            if (shopEntrance != null)
            {
                shopEntrance.RestoreInsideState(restoreShop);
            }

            if (dungeonEntrance != null && restoreDungeon)
            {
                dungeonEntrance.RestoreInsideState(true);
            }

            if (player != null)
            {
                player.position = data.playerPosition.ToVector3();
                Rigidbody2D body = player.GetComponent<Rigidbody2D>();
                if (body != null)
                {
                    body.position = player.position;
                    body.linearVelocity = Vector2.zero;
                }

                Camera camera = Camera.main;
                if (camera != null)
                {
                    camera.transform.position = player.position + new Vector3(0f, 0f, -10f);
                }
            }

            inventory?.Restore(
                data.inventory.commonSeeds > 0 ? data.inventory.commonSeeds : data.inventory.seeds,
                data.inventory.mineralSeeds,
                data.inventory.magicSeeds,
                data.inventory.wood,
                data.inventory.stone,
                data.inventory.fruit,
                data.inventory.coins,
                data.inventory.maxSeedsPerSlot);

            survivalStats?.Restore(data.survival.maxHealth > 0 ? data.survival.maxHealth : 5, data.survival.health, data.survival.hungerPercent);
            toolUpgrades?.Restore(data.toolUpgrades.axeLevel, data.toolUpgrades.pickaxeLevel, data.toolUpgrades.shovelLevel);
            crafting?.Restore(data.crafting.storageLevel, data.crafting.campLevel);
            questSystem?.Restore(data.tutorialQuest.questIndex, data.tutorialQuest.questProgress);

            if (movement != null)
            {
                movement.SetMovementMode((PlayerMovementController.MobileMovementMode)data.movementMode);
                movement.StopMovement();
            }

            if (toolbelt != null)
            {
                toolbelt.Select((FarmTool)data.selectedTool);
            }

            List<FarmingPlot> plots = GetSortedPlots();
            int plotCount = Mathf.Min(plots.Count, data.plots.Count);
            for (int i = 0; i < plotCount; i++)
            {
                plots[i].Restore(data.plots[i].state, data.plots[i].remainingGrowTime, data.plots[i].seedRarity);
            }

            List<HarvestableResource> resources = GetSortedResources();
            int resourceCount = Mathf.Min(resources.Count, data.resources.Count);
            for (int i = 0; i < resourceCount; i++)
            {
                resources[i].Restore(data.resources[i].harvested);
            }

            List<LandUnlockZone> unlockZones = GetSortedUnlockZones();
            int unlockZoneCount = Mathf.Min(unlockZones.Count, data.unlockZones.Count);
            for (int i = 0; i < unlockZoneCount; i++)
            {
                unlockZones[i].Restore(data.unlockZones[i].unlocked);
            }

            List<DungeonChest> dungeonChests = GetSortedDungeonChests();
            int chestCount = Mathf.Min(dungeonChests.Count, data.dungeonChests.Count);
            for (int i = 0; i < chestCount; i++)
            {
                dungeonChests[i].Restore(data.dungeonChests[i].opened);
            }
        }

        private static List<FarmingPlot> GetSortedPlots()
        {
            List<FarmingPlot> plots = new List<FarmingPlot>(FindObjectsByType<FarmingPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            plots.Sort((left, right) => string.CompareOrdinal(GetStableSortKey(left.transform), GetStableSortKey(right.transform)));
            return plots;
        }

        private static List<HarvestableResource> GetSortedResources()
        {
            List<HarvestableResource> resources = new List<HarvestableResource>(FindObjectsByType<HarvestableResource>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            resources.Sort((left, right) => string.CompareOrdinal(GetStableSortKey(left.transform), GetStableSortKey(right.transform)));
            return resources;
        }

        private static List<LandUnlockZone> GetSortedUnlockZones()
        {
            List<LandUnlockZone> unlockZones = new List<LandUnlockZone>(FindObjectsByType<LandUnlockZone>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            unlockZones.Sort((left, right) => string.CompareOrdinal(GetStableSortKey(left.transform), GetStableSortKey(right.transform)));
            return unlockZones;
        }

        private static List<DungeonChest> GetSortedDungeonChests()
        {
            List<DungeonChest> dungeonChests = new List<DungeonChest>(FindObjectsByType<DungeonChest>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            dungeonChests.Sort((left, right) => string.CompareOrdinal(GetStableSortKey(left.transform), GetStableSortKey(right.transform)));
            return dungeonChests;
        }

        private static string GetStableSortKey(Transform target)
        {
            Vector3 position = target.position;
            return $"{target.name}_{position.x:000.000}_{position.y:000.000}";
        }

        [Serializable]
        private sealed class GameSaveData
        {
            public int version;
            public SaveVector playerPosition;
            public bool insideShop;
            public bool insideDungeon;
            public int selectedTool;
            public int movementMode;
            public InventorySaveData inventory = new InventorySaveData();
            public SurvivalSaveData survival = new SurvivalSaveData();
            public ToolUpgradeSaveData toolUpgrades = new ToolUpgradeSaveData();
            public CraftingSaveData crafting = new CraftingSaveData();
            public TutorialQuestSaveData tutorialQuest = new TutorialQuestSaveData();
            public List<PlotSaveData> plots = new List<PlotSaveData>();
            public List<ResourceSaveData> resources = new List<ResourceSaveData>();
            public List<UnlockZoneSaveData> unlockZones = new List<UnlockZoneSaveData>();
            public List<DungeonChestSaveData> dungeonChests = new List<DungeonChestSaveData>();
        }

        [Serializable]
        private sealed class InventorySaveData
        {
            public int seeds;
            public int commonSeeds;
            public int mineralSeeds;
            public int magicSeeds;
            public int wood;
            public int stone;
            public int fruit;
            public int coins;
            public int maxSeedsPerSlot = 20;
        }

        [Serializable]
        private sealed class SurvivalSaveData
        {
            public int maxHealth = 5;
            public int health;
            public float hungerPercent;
        }

        [Serializable]
        private sealed class ToolUpgradeSaveData
        {
            public int axeLevel = 1;
            public int pickaxeLevel = 1;
            public int shovelLevel = 1;
        }

        [Serializable]
        private sealed class CraftingSaveData
        {
            public int storageLevel;
            public int campLevel;
        }

        [Serializable]
        private sealed class TutorialQuestSaveData
        {
            public int questIndex;
            public int questProgress;
        }

        [Serializable]
        private sealed class PlotSaveData
        {
            public int index;
            public int state;
            public float remainingGrowTime;
            public int seedRarity;
        }

        [Serializable]
        private sealed class ResourceSaveData
        {
            public int index;
            public bool harvested;
        }

        [Serializable]
        private sealed class UnlockZoneSaveData
        {
            public int index;
            public bool unlocked;
        }

        [Serializable]
        private sealed class DungeonChestSaveData
        {
            public int index;
            public bool opened;
        }

        [Serializable]
        private struct SaveVector
        {
            public float x;
            public float y;
            public float z;

            public static SaveVector From(Vector3 vector)
            {
                return new SaveVector
                {
                    x = vector.x,
                    y = vector.y,
                    z = vector.z
                };
            }

            public Vector3 ToVector3()
            {
                return new Vector3(x, y, z);
            }
        }
    }
}
