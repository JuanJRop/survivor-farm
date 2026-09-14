using System;
using System.Collections.Generic;
using System.IO;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEngine;

namespace SurvivorFarm.Runtime.Core
{
    public sealed class GameSaveSystem : MonoBehaviour
    {
        private const int SaveVersion = 20;

        [SerializeField] private DayNightCycle dayNightCycle;

        private DayNightCycle Clock => dayNightCycle != null ? dayNightCycle :
            (dayNightCycle = FindFirstObjectByType<DayNightCycle>());

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
        [SerializeField] private bool loadOnStart = true;
        [SerializeField, Tooltip("Restore the saved location and interior, instead of the scene's starting position.")]
        private bool restorePlayerLocation = true;

        private float nextAutoSaveTime;
        private bool loadedOnce;
        private bool persistenceBusy;
        private SaveFileStore saveStore;
        private string lastReportedError;
        private readonly List<PlotSaveData> unmatchedPlots = new List<PlotSaveData>();
        private readonly List<ResourceSpawnSaveData> unmatchedSpawns = new List<ResourceSpawnSaveData>();

        public bool IsSavingBlocked => saveStore != null && saveStore.IsWriteBlocked;
        public string LastSaveError => saveStore?.LastError ?? lastReportedError;
        public SaveLoadSource LastLoadSource { get; private set; } = SaveLoadSource.Missing;

        public static bool IsQa => Array.IndexOf(Environment.GetCommandLineArgs(),"--qa")>=0;
        public static string CurrentSlot => IsQa?"qa":PlayerPrefs.GetString("SurvivorFarmSlot","");
        public string SaveFile => SavePath;
        private string SavePath
        {
            get
            {
                string slot = CurrentSlot;
                if (slot.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || slot.Contains("/") || slot.Contains("\\"))
                    throw new IOException("Invalid save slot name.");
                return Path.Combine(IsQa ? Path.GetFullPath(Path.Combine(Application.dataPath, "../QA")) : Application.persistentDataPath,
                    "survivor_farm_save" + (slot == "" ? "" : "_" + slot) + ".json");
            }
        }

        private SaveFileStore Store
        {
            get
            {
                string path = Path.GetFullPath(SavePath);
                if (saveStore == null || saveStore.Path != path)
                {
                    saveStore = new SaveFileStore(path, ValidateSaveJson);
                    loadedOnce = false;
                    unmatchedPlots.Clear();
                    unmatchedSpawns.Clear();
                }
                return saveStore;
            }
        }
        public static void StartNewSlot()
        {
            var old=FindFirstObjectByType<GameSaveSystem>();if(old!=null){old.player=null;old.enabled=false;}
            PlayerPrefs.SetString("SurvivorFarmSlot",DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff"));PlayerPrefs.Save();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main");
        }

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

        private void Start()
        {
            if (loadOnStart && !loadedOnce)
            {
                TryLoadGame();
            }
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
            if (player == null || inventory == null || persistenceBusy)
            {
                return;
            }

            try
            {
                SaveFileStore store = Store;
                // Restore before an early save callback can overwrite an existing slot.
                if (!loadedOnce) TryLoadGame();
                persistenceBusy = true;
                if (store.IsWriteBlocked || !store.TryWrite(JsonUtility.ToJson(BuildSaveData(), true)))
                {
                    ReportPersistenceError(store.LastError, notify);
                    return;
                }
                lastReportedError = null;
                if (notify) FarmNotificationCenter.Show("Partida guardada.");
            }
            catch (Exception exception) { ReportPersistenceError(exception.Message, notify); }
            finally { persistenceBusy = false; }
        }

        public bool TryLoadGame()
        {
            if (persistenceBusy) return false;
            persistenceBusy = true;
            try
            {
                SaveFileStore store = Store;
                loadedOnce = true;
                LastLoadSource = store.TryLoad(out string json);
                if (LastLoadSource == SaveLoadSource.Missing) return false;
                if (LastLoadSource == SaveLoadSource.Failed)
                {
                    ReportPersistenceError(store.LastError, true);
                    return false;
                }
                if (!TryReadSaveJson(json, out GameSaveData data, out string error))
                {
                    store.BlockWrites(error);
                    ReportPersistenceError(error, true);
                    return false;
                }
                RestoreSaveData(data);
                lastReportedError = null;
                FarmNotificationCenter.Show(LastLoadSource == SaveLoadSource.Primary ? "Partida cargada." : "Partida recuperada desde una copia de seguridad.");
                return true;
            }
            catch (Exception exception)
            {
                LastLoadSource = SaveLoadSource.Failed;
                saveStore?.BlockWrites(exception.Message);
                ReportPersistenceError(exception.Message, true);
                return false;
            }
            finally { persistenceBusy = false; }
        }

        private void ReportPersistenceError(string error, bool notify)
        {
            bool changed = lastReportedError != error;
            if (changed) Debug.LogWarning("Save unavailable: " + error, this);
            if (notify || changed) FarmNotificationCenter.Show("No se pudo guardar o recuperar la partida. Tus archivos se conservan; reintenta cargar o usa una ranura nueva.");
            lastReportedError = error;
        }

        public static bool ValidateSaveJson(string json, out string error) => TryReadSaveJson(json, out _, out error);

        private static bool TryReadSaveJson(string json, out GameSaveData data, out string error)
        {
            data = null;
            if (!SaveJsonValidation.TryValidate(json, typeof(GameSaveData), out error)) return false;
            try
            {
                data = new GameSaveData();
                JsonUtility.FromJsonOverwrite(json, data);
                if (data == null || data.version < 5 || data.version > SaveVersion)
                {
                    error = "Unsupported save version (expected 5..20).";
                    return false;
                }
                NormalizeSaveData(data);
                return ValidateSaveData(data, out error);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                error = exception.Message;
                return false;
            }
        }

        private static void NormalizeSaveData(GameSaveData data)
        {

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

            data.resourceSpawns ??= new List<ResourceSpawnSaveData>();
            data.groundLoot ??= new List<GroundLootSaveData>();
            data.buildings ??= new List<BuildingData>();
            data.inventory.packedBuildings ??= new List<PackedBuilding>();
            data.inventory.itemStacks ??= new List<InventoryStack>();
            // These fields did not exist in the earliest supported layouts.
            if (data.version < 7) { data.day = 1; data.hour = 8f; }
            if (data.version < 17) data.inventory.preferredSeed = -1;
            // Earlier mobile control enum values all restore to today's PC mode.
            data.movementMode = 0;
        }

        private static bool ValidateSaveData(GameSaveData data, out string error)
        {
            error = "Invalid save values.";
            InventorySaveData items = data.inventory;
            if (items.seeds < 0 || items.commonSeeds < 0 || items.mineralSeeds < 0 || items.magicSeeds < 0 ||
                items.wood < 0 || items.stone < 0 || items.fruit < 0 || items.food < 0 || items.coins < 0 ||
                items.totalGathered < 0 || items.maxSeedsPerSlot < 0 || items.preferredSeed < -1 || items.preferredSeed > 2)
                return false;
            if (data.day < 1 || data.hour < 0 || data.hour >= 24 ||
                data.survival.maxHealth < 0 || data.survival.hungerPercent < 0 || data.survival.hungerPercent > 1 ||
                data.survival.health > (data.survival.maxHealth > 0 ? data.survival.maxHealth : 5) ||
                !Enum.IsDefined(typeof(FarmTool), data.selectedTool)) return false;
            if (data.toolUpgrades.axeLevel < 0 || data.toolUpgrades.pickaxeLevel < 0 || data.toolUpgrades.shovelLevel < 0 ||
                data.crafting.storageLevel < 0 || data.crafting.campLevel < 0 || data.crafting.mealsCooked < 0 ||
                data.crafting.weaponLevel < 0 || data.tutorialQuest.questIndex < 0 || data.tutorialQuest.questProgress < 0 ||
                data.tutorialQuest.firstNightDay < 0) return false;

            var plotIds = new HashSet<string>(StringComparer.Ordinal);
            var legacyIndices = new HashSet<int>();
            foreach (PlotSaveData plot in data.plots)
            {
                if (plot == null || plot.index < 0 || !Enum.IsDefined(typeof(FarmingPlot.PlotState), plot.state) ||
                    !Enum.IsDefined(typeof(SeedRarity), plot.seedRarity) || plot.remainingGrowTime < 0) return false;
                if (string.IsNullOrEmpty(plot.id) ? !legacyIndices.Add(plot.index) : !plotIds.Add(plot.id))
                { error = "Duplicate plot identity."; return false; }
            }
            var spawnIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (ResourceSpawnSaveData spawn in data.resourceSpawns)
                if (spawn == null || string.IsNullOrWhiteSpace(spawn.id) || spawn.health < -1 || !spawnIds.Add(spawn.id))
                { error = "Invalid or duplicate resource identity."; return false; }
            foreach (ResourceSaveData resource in data.resources)
                if (resource == null || resource.index < 0) return false;
            foreach (UnlockZoneSaveData zone in data.unlockZones)
                if (zone == null || zone.index < 0) return false;
            foreach (DungeonChestSaveData chest in data.dungeonChests)
                if (chest == null || chest.index < 0) return false;
            foreach (GroundLootSaveData drop in data.groundLoot)
                if (drop == null || drop.amount < 0 || !Enum.IsDefined(typeof(ItemKind), drop.item)) return false;
            foreach (BuildingData building in data.buildings)
                if (building == null || string.IsNullOrWhiteSpace(building.kind) || building.wood < 0 || building.stone < 0 ||
                    building.food < 0 || building.iron < 0) return false;
            foreach (PackedBuilding building in items.packedBuildings)
                if (building == null || string.IsNullOrWhiteSpace(building.kind) || building.count < 0) return false;
            foreach (InventoryStack stack in items.itemStacks)
                if (stack == null || string.IsNullOrWhiteSpace(stack.id) || stack.count < 0) return false;
            if (data.adventure != null && data.adventure.iron < 0) return false;
            if (data.house != null && (data.house.level < 0 || data.house.level > 3 || data.house.style < 0 || data.house.style > 2)) return false;
            if (data.valley != null && (data.valley.zone < 0 || data.valley.zone > 5 || data.valley.influence < 0 || data.valley.startDay < 0)) return false;
            error = null;
            return true;
        }

        private GameSaveData BuildSaveData()
        {
            inventory?.GetComponent<ValleyCampaign>()?.SyncLocation();
            GameSaveData data = new GameSaveData
            {
                version = SaveVersion,
                bridgeRepaired = FindFirstObjectByType<RepairableBridge>(FindObjectsInactive.Include)?.IsRepaired ?? false,
                adventure = inventory.GetComponent<AdventureProgress>()?.Data,
                valley = inventory.GetComponent<ValleyCampaign>()?.Data,
                house = inventory.GetComponent<HouseSystem>()?.Snapshot(),
                buildings = inventory.GetComponent<ConstructionSystem>()?.Buildings,
                petEquipped = player != null && (player.GetComponent<PlayerPetController>()?.Equipped ?? true),
                day = Clock != null ? Clock.Day : 1,
                hour = Clock != null ? Clock.Hour : 8f,
                playerPosition = SaveVector.From(player.position),
                hasCheckpoint = player.GetComponent<PlayerRespawnController>() != null,
                checkpoint = SaveVector.From(player.GetComponent<PlayerRespawnController>()?.Checkpoint ?? player.position),
                insideShop = shopEntrance != null && shopEntrance.IsInsideShop,
                insideDungeon = dungeonEntrance != null && dungeonEntrance.IsInsideDungeon,
                dungeonExpedition = dungeonEntrance != null ? dungeonEntrance.Expedition?.Capture() : null,
                selectedTool = toolbelt != null ? (int)toolbelt.SelectedTool : 0,
                movementMode = movement != null ? (int)movement.MovementMode : 0,
                inventory = new InventorySaveData
                {
                    commonSeeds = inventory.CommonSeeds,
                    totalGathered = inventory.TotalGathered,
                    mineralSeeds = inventory.MineralSeeds,
                    magicSeeds = inventory.MagicSeeds,
                    wood = inventory.Wood,
                    stone = inventory.Stone,
                    fruit = inventory.Fruit,
                    food = inventory.Food,
                    coins = inventory.Coins,
                    backpackOrder = inventory.BackpackOrder,
                    packedBuildings = inventory.PackedBuildings,
                    preferredSeed = inventory.PreferredSeed,
                    ownedEquipment = inventory.OwnedEquipment,
                    equippedEquipment = inventory.EquippedEquipment,
                    itemStacks = inventory.ItemStacks,
                    preferredCatalogSeed = inventory.PreferredCatalogSeed,
                    maxSeedsPerSlot = 99
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
                    campLevel = crafting != null ? crafting.CampLevel : 0,
                    bedBuilt = crafting != null && crafting.BedBuilt,
                    campfireBuilt = crafting != null && crafting.CampfireBuilt, mealsCooked = crafting != null ? crafting.MealsCooked : 0, weaponLevel = crafting != null ? crafting.WeaponLevel : 1
                },
                tutorialQuest = new TutorialQuestSaveData
                {
                    questIndex = questSystem != null ? questSystem.QuestIndex : 0,
                    rewardedSteps = questSystem != null ? questSystem.RewardedSteps : 0,
                    questProgress = questSystem != null ? questSystem.QuestProgress : 0,
                    completedSteps = questSystem != null ? questSystem.CompletedSteps : 0, firstNightDay = questSystem != null ? questSystem.FirstNightDay : 0
                }
            };

            List<FarmingPlot> plots = GetSortedPlots();
            for (int i = 0; i < plots.Count; i++)
            {
                data.plots.Add(new PlotSaveData
                {
                    index = i,
                    id = plots[i].PersistentId,
                    state = plots[i].StateId,
                    remainingGrowTime = plots[i].RemainingGrowTime,
                    seedRarity = plots[i].PlantedSeedRarityId,
                    cropItemId = plots[i].PlantedCropItemId
                });
            }

            foreach (ResourceSpawnPoint spawn in FindObjectsByType<ResourceSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                spawn.EnsureSpawned();
                if (spawn.Instance == null) continue;
                data.resourceSpawns.Add(new ResourceSpawnSaveData
                {
                    id = spawn.PersistentId,
                    harvested = spawn.Instance.IsHarvested,
                    health = spawn.Instance.CurrentHealth,
                    regrowth = spawn.CaptureRegrowth()
                });
            }

            // Keep states for temporarily absent map objects until their identity can
            // be resolved again. A map edit must not silently erase their progress.
            data.plots.AddRange(unmatchedPlots);
            data.resourceSpawns.AddRange(unmatchedSpawns);

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
                    opened = dungeonChests[i].IsOpened,
                    id = dungeonChests[i].PersistentId
                });
            }

            foreach (var drop in FindObjectsByType<EnemyLootPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (drop.IsUncollected && drop.Item != null)
                    data.groundLoot.Add(new GroundLootSaveData { position = SaveVector.From(drop.transform.position),
                        amount = drop.Amount, item = (int)drop.Item.Kind, dungeon = drop.IsDungeon });
            return data;
        }

        private void RestoreSaveData(GameSaveData data)
        {
            VillageDialogueWindow.CloseActive();
            var currentHouse=inventory?.GetComponent<HouseSystem>();if(currentHouse!=null&&currentHouse.IsInside)currentHouse.Exit(false);
            player?.GetComponent<PlayerPetController>()?.SetEquipped(data.version < 10 || data.petEquipped);
            Vector3 sceneStartPosition = player != null ? player.position : Vector3.zero;
            Clock?.Restore(data.version >= 7 ? data.day : 1, data.version >= 7 ? data.hour : 8f);
            bool recoverDeath = data.survival.health <= 0;
            var respawn = player != null ? player.GetComponent<PlayerRespawnController>() : null;
            if (data.hasCheckpoint) respawn?.SetCheckpoint(data.checkpoint.ToVector3());
            bool restoreDungeon = restorePlayerLocation && data.insideDungeon && !recoverDeath;
            bool restoreShop = restorePlayerLocation && data.insideShop && !restoreDungeon && !recoverDeath;
            dungeonEntrance?.EnsureExpedition();
            dungeonEntrance?.Expedition?.Restore(data.dungeonExpedition);

            if (shopEntrance != null)
            {
                shopEntrance.RestoreInsideState(false);
            }

            if (dungeonEntrance != null && (restoreDungeon || dungeonEntrance.IsInsideDungeon))
            {
                dungeonEntrance.RestoreInsideState(restoreDungeon);
            }

            if (player != null)
            {
                player.position = recoverDeath ? (respawn != null ? respawn.Checkpoint : sceneStartPosition) : restoreShop ? (shopEntrance != null ? shopEntrance.OutsidePosition : sceneStartPosition) : restorePlayerLocation ? data.playerPosition.ToVector3() : sceneStartPosition;
                if (restoreDungeon && dungeonEntrance?.Expedition != null)
                {
                    Physics2D.SyncTransforms();
                    player.position = dungeonEntrance.Expedition.RecoverPosition(player.position);
                }
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

            if (inventory != null) { inventory.RestorePacked(data.inventory.packedBuildings);
                inventory.PreferredSeed=data.version>=17?data.inventory.preferredSeed:-1;
                inventory.PreferredCatalogSeed=data.version>=20?data.inventory.preferredCatalogSeed:null;
                inventory.RestoreItemStacks(data.version>=20?data.inventory.itemStacks:null);
                inventory.BackpackOrder = data.inventory.backpackOrder ?? new string[0]; }
            inventory?.GetComponent<AdventureProgress>()?.Restore(data.adventure);
            inventory?.GetComponent<HouseSystem>()?.LoadLayout(data.house);
            inventory?.GetComponent<ConstructionSystem>()?.Restore(data.buildings);
            inventory?.RestoreEquipment(data.inventory.ownedEquipment, data.inventory.equippedEquipment);
            inventory?.RestoreGathered(data.inventory.totalGathered);
            inventory?.Restore(
                data.inventory.commonSeeds > 0 ? data.inventory.commonSeeds : data.inventory.seeds,
                data.inventory.mineralSeeds,
                data.inventory.magicSeeds,
                data.inventory.wood,
                data.inventory.stone,
                data.inventory.fruit,
                data.inventory.coins,
                data.inventory.maxSeedsPerSlot,
                data.inventory.food);

            survivalStats?.Restore(data.survival.maxHealth > 0 ? data.survival.maxHealth : 5, data.survival.health, data.survival.hungerPercent);
            if (recoverDeath) survivalStats?.Revive();
            player?.GetComponent<PlayerCharacterAnimator>()?.CancelAction();
            toolUpgrades?.Restore(data.toolUpgrades.axeLevel, data.toolUpgrades.pickaxeLevel, data.toolUpgrades.shovelLevel);
            bool legacyFire=data.version<17&&data.crafting.campfireBuilt&&inventory!=null&&!(data.buildings??new List<BuildingData>()).Exists(b=>b.kind=="Campfire");
            if(legacyFire)inventory.AddPacked("Campfire");
            bool legacyBed=data.version<19&&data.crafting.bedBuilt&&inventory!=null&&!(data.buildings??new List<BuildingData>()).Exists(b=>b.kind=="Bed");
            if(legacyBed)inventory.AddPacked("Bed");
            crafting?.Restore(data.crafting.storageLevel, data.crafting.campLevel, data.crafting.bedBuilt&&!legacyBed, data.crafting.campfireBuilt&&!legacyFire, data.crafting.mealsCooked, data.crafting.weaponLevel);
            questSystem?.Restore(data.tutorialQuest.questIndex, data.tutorialQuest.questProgress, data.version >= 11 ? data.tutorialQuest.completedSteps : -1, data.tutorialQuest.firstNightDay, data.version >= 13 ? data.tutorialQuest.rewardedSteps : -1);

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
            unmatchedPlots.Clear();
            foreach (PlotSaveData saved in data.plots)
            {
                FarmingPlot plot;
                if (string.IsNullOrEmpty(saved.id))
                {
                    plot = plots.Find(p => p.TryGetComponent<StableSaveId>(out var stable) && stable.LegacyPlotIndex == saved.index);
                    if (plot == null && saved.index < plots.Count &&
                        (!plots[saved.index].TryGetComponent<StableSaveId>(out var assigned) || string.IsNullOrEmpty(assigned.Id)))
                        plot = plots[saved.index];
                }
                else plot = plots.Find(p => p.PersistentId == saved.id || StableSaveId.Matches(p, saved.id));
                if(plot != null) plot.Restore(saved.state, saved.remainingGrowTime, saved.seedRarity, data.version >= 20 ? saved.cropItemId : null);
                else unmatchedPlots.Add(saved);
            }

            unmatchedSpawns.Clear();
            if (data.version == 5)
            {
                // Version 5 used positional indices, including the inactive prefab shelf.
                List<HarvestableResource> resources = GetSortedResources();
                resources.RemoveAll(resource => resource is AnimalResource);
                int resourceCount = Mathf.Min(resources.Count, data.resources.Count);
                for (int i = 0; i < resourceCount; i++)
                {
                    resources[i].Restore(data.resources[i].harvested);
                }
            }
            else
            {
                Dictionary<string, ResourceSpawnSaveData> states = new Dictionary<string, ResourceSpawnSaveData>();
                foreach (ResourceSpawnSaveData saved in data.resourceSpawns ?? new List<ResourceSpawnSaveData>())
                {
                    if (!string.IsNullOrEmpty(saved.id)) states[saved.id] = saved;
                }

                foreach (ResourceSpawnPoint spawn in FindObjectsByType<ResourceSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (states.TryGetValue(spawn.PersistentId, out ResourceSpawnSaveData saved))
                    {
                        spawn.Restore(saved.harvested, saved.health);
                        spawn.RestoreRegrowth(saved.regrowth);
                        states.Remove(saved.id);
                    }
                    else
                    {
                        foreach (ResourceSpawnSaveData candidate in new List<ResourceSpawnSaveData>(states.Values))
                            if (spawn.MatchesPersistentId(candidate.id))
                            {
                                spawn.Restore(candidate.harvested, candidate.health);
                                spawn.RestoreRegrowth(candidate.regrowth);
                                states.Remove(candidate.id);
                                break;
                            }
                    }
                }
                unmatchedSpawns.AddRange(states.Values);
            }

            List<LandUnlockZone> unlockZones = GetSortedUnlockZones();
            int unlockZoneCount = Mathf.Min(unlockZones.Count, data.unlockZones.Count);
            bool legacyNorthAccess = data.insideDungeon;
            for (int i = 0; i < unlockZoneCount; i++)
            {
                unlockZones[i].Restore(data.unlockZones[i].unlocked);
                if (unlockZones[i].name.StartsWith("North", StringComparison.Ordinal) && data.unlockZones[i].unlocked)
                    legacyNorthAccess = true;
            }
            var bridge = FindFirstObjectByType<RepairableBridge>(FindObjectsInactive.Include);
            bridge?.Restore(data.version >= 18 ? data.bridgeRepaired : legacyNorthAccess);
            if (data.version < 18 && !restoreShop && !restoreDungeon) bridge?.RecoverLegacyPosition(player);

            List<DungeonChest> dungeonChests = GetSortedDungeonChests();
            for (int i = 0; i < dungeonChests.Count; i++)
            {
                var saved = data.dungeonChests.Find(c => c.id == dungeonChests[i].PersistentId);
                if (saved == null) saved = data.dungeonChests.Find(c => string.IsNullOrEmpty(c.id) && c.index == i);
                dungeonChests[i].Restore(saved != null && saved.opened);
            }
            RestoreGroundLoot(data.groundLoot);
            inventory?.GetComponent<ValleyCampaign>()?.Restore(data.valley);
            if (restoreShop) shopEntrance?.RecoverLegacyInterior();
            inventory?.GetComponent<HouseSystem>()?.RestorePresence(restorePlayerLocation&&data.house!=null&&data.house.inside&&!recoverDeath&&!restoreDungeon);
        }

        private static void RestoreGroundLoot(List<GroundLootSaveData> savedDrops)
        {
            EnemyLootPickup prefab = null;
            foreach (var enemy in FindObjectsByType<EnemyAIBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (enemy.LootPrefab != null) { prefab = enemy.LootPrefab; break; }
            if (prefab == null) return;
            foreach (var old in FindObjectsByType<EnemyLootPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None)) old.Discard();
            var outside = FindFirstObjectByType<OutdoorEnemyPool>(FindObjectsInactive.Include);
            var dungeon = FindFirstObjectByType<DungeonEnemyPool>(FindObjectsInactive.Include);
            foreach (var saved in savedDrops ?? new List<GroundLootSaveData>())
            {
                if (saved.amount <= 0 || saved.item != (int)ItemKind.Coins) continue;
                Transform parent = saved.dungeon ? dungeon != null ? dungeon.transform : null : outside != null ? outside.transform : null;
                Vector3 position = saved.position.ToVector3();
                if (saved.dungeon && dungeon != null && dungeon.Expedition != null && !DungeonLayout.Walkable(position)) position = DungeonLayout.Entry;
                if (parent != null) EnemyLootPickup.Spawn(prefab, position, parent, ResourceFlyweights.Item(ItemKind.Coins), saved.amount);
            }
        }

        private static List<FarmingPlot> GetSortedPlots()
        {
            List<FarmingPlot> plots = new List<FarmingPlot>(FindObjectsByType<FarmingPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            plots.Sort((left, right) => string.CompareOrdinal(left.LegacySortKey, right.LegacySortKey));
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
            public HouseData house;
            public List<GroundLootSaveData> groundLoot = new List<GroundLootSaveData>();
            public AdventureData adventure;
            public ValleyData valley;
            public List<BuildingData> buildings;
            public int day = 1;
            public float hour = 8f;
            public int version;
            public bool bridgeRepaired;
            public bool petEquipped = true;
            public SaveVector playerPosition;
            public bool hasCheckpoint;
            public SaveVector checkpoint;
            public bool insideShop;
            public bool insideDungeon;
            public DungeonExpeditionState dungeonExpedition;
            public int selectedTool;
            public int movementMode;
            public InventorySaveData inventory = new InventorySaveData();
            public SurvivalSaveData survival = new SurvivalSaveData();
            public ToolUpgradeSaveData toolUpgrades = new ToolUpgradeSaveData();
            public CraftingSaveData crafting = new CraftingSaveData();
            public TutorialQuestSaveData tutorialQuest = new TutorialQuestSaveData();
            public List<PlotSaveData> plots = new List<PlotSaveData>();
            public List<ResourceSaveData> resources = new List<ResourceSaveData>();
            public List<ResourceSpawnSaveData> resourceSpawns = new List<ResourceSpawnSaveData>();
            public List<UnlockZoneSaveData> unlockZones = new List<UnlockZoneSaveData>();
            public List<DungeonChestSaveData> dungeonChests = new List<DungeonChestSaveData>();
        }

        [Serializable]
        private sealed class GroundLootSaveData
        {
            public SaveVector position;
            public int amount, item;
            public bool dungeon;
        }

        [Serializable]
        private sealed class InventorySaveData
        {
            public List<PackedBuilding> packedBuildings=new List<PackedBuilding>();
            public int preferredSeed=-1;
            public int totalGathered;
            public int seeds;
            public int commonSeeds;
            public int mineralSeeds;
            public int magicSeeds;
            public int wood;
            public int stone;
            public int fruit;
            public int food;
            public int coins;
            public int maxSeedsPerSlot = 20;
            public string[] backpackOrder;
            public string[] ownedEquipment;
            public string[] equippedEquipment;
            public List<InventoryStack> itemStacks = new List<InventoryStack>();
            public string preferredCatalogSeed;
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
            public bool bedBuilt, campfireBuilt;
            public int mealsCooked, weaponLevel = 1;
            public int storageLevel;
            public int campLevel;
        }

        [Serializable]
        private sealed class TutorialQuestSaveData
        {
            public int rewardedSteps;
            public int questIndex;
            public int questProgress, completedSteps, firstNightDay;
        }

        [Serializable]
        private sealed class PlotSaveData
        {
            public string id;
            public int index;
            public int state;
            public float remainingGrowTime;
            public int seedRarity;
            public string cropItemId;
        }

        [Serializable]
        private sealed class ResourceSaveData
        {
            public int index;
            public bool harvested;
        }

        [Serializable]
        private sealed class ResourceSpawnSaveData
        {
            public string id;
            public bool harvested;
            public int health;
            public ResourceRegrowthState regrowth;
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
            public string id;
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
