using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;

namespace SurvivorFarm.Runtime.World
{
    public static class FarmPrototypeBuilder
    {
        private static Sprite squareSprite;
        private static Sprite circleSprite;
        private static Sprite seedSprite;
        private static Sprite mineralSeedSprite;
        private static Sprite magicSeedSprite;
        private static Sprite woodSprite;
        private static Sprite stoneSprite;
        private static Sprite fruitSprite;
        private static Sprite coinSprite;
        private static Sprite heartSprite;
        private static Sprite uiPanelSprite;
        private static Sprite uiButtonSprite;
        private static Sprite uiSlotSprite;
        private static Sprite uiBarSprite;
        private static Sprite uiHudSprite;
        private static Dictionary<FarmTool, Sprite> toolSprites;

        private sealed class FarmSceneSections
        {
            public Transform Root { get; set; }
            public Transform Controllers { get; set; }
            public Transform Player { get; set; }
            public Transform World { get; set; }
            public Transform Ui { get; set; }
            public Transform Prefabs { get; set; }
        }

        public static void Build(bool loadSavedGame = true)
        {
            FarmSceneSections sections = CreateSceneSections();
            Camera camera = EnsureCamera(sections.Controllers);
            GameObject outdoorRoot = CreateChild(sections.World, "Outdoor World");
            BuildGround(outdoorRoot.transform);
            BuildStartingSquare(outdoorRoot.transform);
            BuildBaseHouse(outdoorRoot.transform);
            BuildTillableGrass(outdoorRoot.transform);
            BuildResources(outdoorRoot.transform);
            LandUnlockZone dungeonZone = BuildLandUnlockZones(outdoorRoot.transform);
            DungeonEntrance dungeonEntrance = BuildDungeonExterior(outdoorRoot.transform);
            ShopEntrance shopEntrance = BuildShopExterior(outdoorRoot.transform);
            GameObject shopInterior = BuildShopInterior(sections.World, out Transform outsideSpawn, out Transform insideSpawn);
            GameObject player = BuildPlayer(sections.Player);
            GameObject dungeonInterior = BuildDungeonInterior(sections.World, player.transform, dungeonEntrance, out Transform dungeonOutsideSpawn, out Transform dungeonInsideSpawn, out DungeonEnemyPool dungeonEnemyPool);
            camera.GetComponent<CameraFollowTarget>()?.SetTarget(player.transform);
            if (camera.GetComponent<CameraFollowTarget>() == null)
            {
                camera.gameObject.AddComponent<CameraFollowTarget>().SetTarget(player.transform);
            }
            BuildUi(
                sections.Ui,
                sections.Controllers,
                player.GetComponent<PlayerToolbelt>(),
                player.GetComponent<PlayerInventory>(),
                player.GetComponent<PlayerToolUpgradeController>(),
                player.GetComponent<PlayerCraftingController>(),
                player.GetComponent<PlayerSurvivalStats>(),
                player.GetComponent<PlayerMovementController>(),
                player.GetComponent<PlayerCombatController>(),
                player.GetComponent<FarmPlayerInteractor>(),
                out SimpleShopSystem shopSystem,
                out TutorialQuestSystem questSystem);
            shopEntrance.Configure(
                outdoorRoot,
                shopInterior,
                player.transform,
                outsideSpawn,
                insideSpawn,
                shopSystem);
            shopSystem.SetEntrance(shopEntrance);
            dungeonEntrance.Configure(
                outdoorRoot,
                dungeonInterior,
                player.transform,
                dungeonOutsideSpawn,
                dungeonInsideSpawn,
                dungeonZone,
                dungeonEnemyPool);

            BuildSaveSystem(
                sections.Controllers,
                player.transform,
                player.GetComponent<PlayerInventory>(),
                player.GetComponent<PlayerSurvivalStats>(),
                player.GetComponent<PlayerMovementController>(),
                player.GetComponent<PlayerToolbelt>(),
                player.GetComponent<PlayerToolUpgradeController>(),
                player.GetComponent<PlayerCraftingController>(),
                questSystem,
                shopEntrance,
                dungeonEntrance,
                loadSavedGame);
        }

        private static FarmSceneSections CreateSceneSections()
        {
            GameObject root = new GameObject("Survivor Farm - Main");
            return new FarmSceneSections
            {
                Root = root.transform,
                Controllers = CreateSection(root.transform, "00 Controladores"),
                Player = CreateSection(root.transform, "01 Player"),
                World = CreateSection(root.transform, "02 Mundo"),
                Ui = CreateSection(root.transform, "03 UI"),
                Prefabs = CreateSection(root.transform, "04 Prefabs del Mundo")
            };
        }

        private static Transform CreateSection(Transform parent, string name)
        {
            GameObject section = new GameObject(name);
            section.transform.SetParent(parent, false);
            return section.transform;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static Camera EnsureCamera(Transform parent)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.SetParent(parent, true);

            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.39f, 0.56f, 0.34f);
            return camera;
        }

        private static void BuildGround(Transform parent)
        {
            GameObject ground = new GameObject("Farm Ground");
            ground.transform.SetParent(parent);
            ground.transform.position = new Vector3(0f, 0f, 1f);
            ground.transform.localScale = new Vector3(25f, 25f, 1f);

            SpriteRenderer renderer = ground.AddComponent<SpriteRenderer>();
            renderer.sprite = SquareSprite;
            renderer.color = new Color(0.48f, 0.62f, 0.37f);
            renderer.sortingOrder = -10;
        }

        private static void BuildStartingSquare(Transform parent)
        {
            GameObject startSquare = new GameObject("Starting Unlocked Square");
            startSquare.transform.SetParent(parent);
            startSquare.transform.position = new Vector3(0f, 0f, 0.5f);
            startSquare.transform.localScale = new Vector3(7.85f, 7.85f, 1f);

            SpriteRenderer renderer = startSquare.AddComponent<SpriteRenderer>();
            renderer.sprite = SquareSprite;
            renderer.color = new Color(0.42f, 0.60f, 0.35f);
            renderer.sortingOrder = -6;
        }

        private static void BuildBaseHouse(Transform parent)
        {
            GameObject house = new GameObject("Player Base House");
            house.transform.SetParent(parent);
            house.transform.position = new Vector3(0f, -1.25f, 0f);

            GameObject body = new GameObject("House Body");
            body.transform.SetParent(house.transform);
            body.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            body.transform.localScale = new Vector3(1.75f, 1.25f, 1f);
            SpriteRenderer bodyRenderer = body.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = SquareSprite;
            bodyRenderer.color = new Color(0.64f, 0.43f, 0.25f);
            bodyRenderer.sortingOrder = 2;

            GameObject roof = new GameObject("House Roof");
            roof.transform.SetParent(house.transform);
            roof.transform.localPosition = new Vector3(0f, 0.54f, -0.01f);
            roof.transform.localScale = new Vector3(2.05f, 0.78f, 1f);
            SpriteRenderer roofRenderer = roof.AddComponent<SpriteRenderer>();
            roofRenderer.sprite = SquareSprite;
            roofRenderer.color = new Color(0.45f, 0.16f, 0.12f);
            roofRenderer.sortingOrder = 3;

            GameObject door = new GameObject("House Door");
            door.transform.SetParent(house.transform);
            door.transform.localPosition = new Vector3(0f, -0.56f, -0.02f);
            door.transform.localScale = new Vector3(0.42f, 0.72f, 1f);
            SpriteRenderer doorRenderer = door.AddComponent<SpriteRenderer>();
            doorRenderer.sprite = SquareSprite;
            doorRenderer.color = new Color(0.18f, 0.10f, 0.06f);
            doorRenderer.sortingOrder = 4;

            BoxCollider2D collider = house.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(2.05f, 1.85f);
            collider.offset = new Vector2(0f, -0.1f);

            BaseHouse baseHouse = house.AddComponent<BaseHouse>();
            baseHouse.Configure(bodyRenderer, roofRenderer, doorRenderer);
        }

        private static void BuildTillableGrass(Transform parentTransform)
        {
            GameObject parent = new GameObject("Tillable Grass");
            parent.transform.SetParent(parentTransform);

            Vector2[] zoneCenters =
            {
                Vector2.zero,
                new Vector2(0f, 8f),
                new Vector2(0f, -8f),
                new Vector2(-8f, 0f),
                new Vector2(8f, 0f),
                new Vector2(-8f, 8f),
                new Vector2(8f, 8f),
                new Vector2(-8f, -8f),
                new Vector2(8f, -8f)
            };

            foreach (Vector2 center in zoneCenters)
            {
                CreateTillableGrid(parent.transform, center);
            }
        }

        private static void CreateTillableGrid(Transform parent, Vector2 center)
        {
            const int cellsPerSide = 5;
            const float spacing = 1.42f;
            Vector2 start = center - new Vector2(spacing * 2f, spacing * 2f);

            for (int y = 0; y < cellsPerSide; y++)
            {
                for (int x = 0; x < cellsPerSide; x++)
                {
                    Vector2 position = start + new Vector2(spacing * x, spacing * y);
                    CreateTillableCell(parent, position);
                }
            }
        }

        private static void CreateTillableCell(Transform parent, Vector2 position)
        {
            if (Vector2.Distance(position, Vector2.zero) < 1.7f)
            {
                return;
            }

            GameObject plot = new GameObject($"Grass Cell {position.x:0.0},{position.y:0.0}");
            plot.transform.SetParent(parent);
            plot.transform.position = position;
            plot.transform.localScale = new Vector3(1.08f, 1.08f, 1f);

            SpriteRenderer groundRenderer = plot.AddComponent<SpriteRenderer>();
            groundRenderer.sprite = SquareSprite;
            groundRenderer.sortingOrder = -1;

            GameObject crop = new GameObject("Crop Visual");
            crop.transform.SetParent(plot.transform);
            crop.transform.localPosition = new Vector3(0f, 0.08f, -0.01f);
            crop.transform.localScale = Vector3.one * 0.25f;

            SpriteRenderer cropRenderer = crop.AddComponent<SpriteRenderer>();
            cropRenderer.sprite = CircleSprite;
            cropRenderer.sortingOrder = 2;
            cropRenderer.enabled = false;

            FarmingPlot farmingPlot = plot.AddComponent<FarmingPlot>();
            farmingPlot.Configure(groundRenderer, cropRenderer, FarmingPlot.StartingSurface.Grass);
        }

        private static void BuildResources(Transform parentTransform)
        {
            GameObject parent = new GameObject("Harvestable Resources");
            parent.transform.SetParent(parentTransform);

            CreateTree(parent.transform, new Vector2(-2.85f, 1.9f), 2, 4);
            CreateTree(parent.transform, new Vector2(-2.75f, -1.95f), 2, 4);
            CreateRock(parent.transform, new Vector2(2.7f, 1.95f), 2, 4);
            CreateRock(parent.transform, new Vector2(2.85f, -1.85f), 2, 4);

            CreateTree(parent.transform, new Vector2(-9.25f, 1.5f), 4, 7);
            CreateTree(parent.transform, new Vector2(-8.6f, -1.55f), 5, 8);
            CreateTree(parent.transform, new Vector2(-6.55f, 0.15f), 4, 7);

            CreateRock(parent.transform, new Vector2(8.85f, 1.75f), 4, 7);
            CreateRock(parent.transform, new Vector2(9.65f, -1.55f), 5, 8);
            CreateRock(parent.transform, new Vector2(6.65f, -1.1f), 4, 7);

            CreateTree(parent.transform, new Vector2(-1.6f, 9.1f), 3, 6);
            CreateRock(parent.transform, new Vector2(1.75f, 9.25f), 3, 6);

            CreateTree(parent.transform, new Vector2(-9.2f, 8.85f), 6, 10);
            CreateTree(parent.transform, new Vector2(-6.7f, 6.7f), 5, 9);
            CreateRock(parent.transform, new Vector2(8.7f, 8.9f), 6, 10);
            CreateRock(parent.transform, new Vector2(6.65f, 6.65f), 5, 9);
            CreateRock(parent.transform, new Vector2(-8.7f, -8.6f), 5, 9);
            CreateTree(parent.transform, new Vector2(-6.6f, -6.75f), 4, 8);
            CreateRock(parent.transform, new Vector2(8.6f, -8.7f), 5, 9);
            CreateTree(parent.transform, new Vector2(6.55f, -6.75f), 4, 8);
        }

        private static LandUnlockZone BuildLandUnlockZones(Transform parent)
        {
            GameObject zoneRoot = new GameObject("Land Unlock Zones");
            zoneRoot.transform.SetParent(parent);

            CreateUnlockZone(zoneRoot.transform, "North Zone", new Vector2(0f, 8f), new Vector2(0f, 4.15f), 20, null);
            LandUnlockZone west = CreateUnlockZone(zoneRoot.transform, "West Zone", new Vector2(-8f, 0f), new Vector2(-4.15f, 0f), 20, null);
            LandUnlockZone east = CreateUnlockZone(zoneRoot.transform, "East Zone", new Vector2(8f, 0f), new Vector2(4.15f, 0f), 20, null);
            LandUnlockZone south = CreateUnlockZone(zoneRoot.transform, "South Zone", new Vector2(0f, -8f), new Vector2(0f, -4.15f), 20, null);
            CreateUnlockZone(zoneRoot.transform, "North West Zone", new Vector2(-8f, 8f), new Vector2(-4.15f, 4.15f), 35, west);
            LandUnlockZone dungeonZone = CreateUnlockZone(zoneRoot.transform, "North East Zone", new Vector2(8f, 8f), new Vector2(4.15f, 4.15f), 45, east);
            CreateUnlockZone(zoneRoot.transform, "South West Zone", new Vector2(-8f, -8f), new Vector2(-4.15f, -4.15f), 35, south);
            CreateUnlockZone(zoneRoot.transform, "South East Zone", new Vector2(8f, -8f), new Vector2(4.15f, -4.15f), 35, east);
            return dungeonZone;
        }

        private static LandUnlockZone CreateUnlockZone(
            Transform parent,
            string name,
            Vector2 center,
            Vector2 interactionPosition,
            int cost,
            LandUnlockZone prerequisite)
        {
            GameObject zone = new GameObject(name);
            zone.transform.SetParent(parent);
            zone.transform.position = center;

            SpriteRenderer blockerRenderer = zone.AddComponent<SpriteRenderer>();
            blockerRenderer.sprite = SquareSprite;
            blockerRenderer.color = new Color(0.05f, 0.06f, 0.05f, 0.68f);
            blockerRenderer.sortingOrder = 8;
            zone.transform.localScale = new Vector3(7.85f, 7.85f, 1f);

            BoxCollider2D collider = zone.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;

            GameObject marker = new GameObject("Unlock Point");
            marker.transform.SetParent(parent);
            marker.transform.position = interactionPosition;
            marker.transform.localScale = Vector3.one;

            GameObject markerVisual = new GameObject("Unlock Marker");
            markerVisual.transform.SetParent(marker.transform);
            markerVisual.transform.localPosition = Vector3.zero;
            markerVisual.transform.localScale = new Vector3(0.52f, 0.52f, 1f);
            SpriteRenderer markerRenderer = markerVisual.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = CircleSprite;
            markerRenderer.color = new Color(1f, 0.78f, 0.20f, 0.95f);
            markerRenderer.sortingOrder = 9;

            LandUnlockZone unlockZone = zone.AddComponent<LandUnlockZone>();
            unlockZone.Configure(cost, false, blockerRenderer, collider, marker.transform, prerequisite);
            return unlockZone;
        }

        private static DungeonEntrance BuildDungeonExterior(Transform parent)
        {
            GameObject entrance = new GameObject("Dungeon Entrance");
            entrance.transform.SetParent(parent);
            entrance.transform.position = new Vector3(8f, 8.6f, 0f);

            GameObject doorway = new GameObject("Dungeon Doorway");
            doorway.transform.SetParent(entrance.transform);
            doorway.transform.localPosition = Vector3.zero;
            doorway.transform.localScale = new Vector3(1.35f, 1.15f, 1f);
            SpriteRenderer doorwayRenderer = doorway.AddComponent<SpriteRenderer>();
            doorwayRenderer.sprite = SquareSprite;
            doorwayRenderer.color = new Color(0.09f, 0.08f, 0.10f);
            doorwayRenderer.sortingOrder = 4;

            GameObject glow = new GameObject("Dungeon Glow");
            glow.transform.SetParent(entrance.transform);
            glow.transform.localPosition = new Vector3(0f, -0.12f, -0.01f);
            glow.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
            SpriteRenderer glowRenderer = glow.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = CircleSprite;
            glowRenderer.color = new Color(0.46f, 0.20f, 0.76f, 0.75f);
            glowRenderer.sortingOrder = 5;

            BoxCollider2D collider = entrance.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.45f, 1.25f);

            return entrance.AddComponent<DungeonEntrance>();
        }

        private static GameObject BuildDungeonInterior(
            Transform parent,
            Transform player,
            DungeonEntrance entrance,
            out Transform outsideSpawn,
            out Transform insideSpawn,
            out DungeonEnemyPool enemyPool)
        {
            GameObject dungeon = new GameObject("Dungeon Interior");
            dungeon.transform.SetParent(parent, false);
            dungeon.SetActive(false);

            GameObject floor = new GameObject("Dungeon Floor");
            floor.transform.SetParent(dungeon.transform);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(15f, 13f, 1f);
            SpriteRenderer floorRenderer = floor.AddComponent<SpriteRenderer>();
            floorRenderer.sprite = SquareSprite;
            floorRenderer.color = new Color(0.12f, 0.11f, 0.14f);
            floorRenderer.sortingOrder = -8;

            CreateDungeonRoomFloor(dungeon.transform, "Entry Room Floor", new Vector2(0f, -2.5f), new Vector2(6.1f, 5.2f), new Color(0.16f, 0.14f, 0.18f));
            CreateDungeonRoomFloor(dungeon.transform, "North Room Floor", new Vector2(0f, 3.05f), new Vector2(6.1f, 4.8f), new Color(0.13f, 0.16f, 0.19f));
            CreateDungeonRoomFloor(dungeon.transform, "East Room Floor", new Vector2(5.35f, -0.2f), new Vector2(4.8f, 5.1f), new Color(0.17f, 0.13f, 0.16f));

            CreateDungeonWall(dungeon.transform, "Outer North Wall", new Vector2(1.75f, 5.65f), new Vector2(11.8f, 0.32f));
            CreateDungeonWall(dungeon.transform, "Outer South Wall", new Vector2(1.75f, -5.65f), new Vector2(11.8f, 0.32f));
            CreateDungeonWall(dungeon.transform, "Outer West Wall", new Vector2(-3.25f, 0f), new Vector2(0.32f, 11.6f));
            CreateDungeonWall(dungeon.transform, "Outer East Wall", new Vector2(7.35f, 0f), new Vector2(0.32f, 11.6f));
            CreateDungeonWall(dungeon.transform, "North Divider Left", new Vector2(-2f, 0.25f), new Vector2(2.5f, 0.28f));
            CreateDungeonWall(dungeon.transform, "North Divider Right", new Vector2(2f, 0.25f), new Vector2(2.5f, 0.28f));
            CreateDungeonWall(dungeon.transform, "East Divider Top", new Vector2(3.05f, 1.95f), new Vector2(0.28f, 3f));
            CreateDungeonWall(dungeon.transform, "East Divider Bottom", new Vector2(3.05f, -3.25f), new Vector2(0.28f, 2.3f));
            CreateDungeonWall(dungeon.transform, "Entry Pillar A", new Vector2(-1.65f, -2.25f), new Vector2(0.58f, 1.15f));
            CreateDungeonWall(dungeon.transform, "Entry Pillar B", new Vector2(1.65f, -2.25f), new Vector2(0.58f, 1.15f));

            GameObject exit = new GameObject("Dungeon Exit");
            exit.transform.SetParent(dungeon.transform);
            exit.transform.localPosition = new Vector3(0f, -3.7f, 0f);
            exit.transform.localScale = new Vector3(1.3f, 0.5f, 1f);
            SpriteRenderer exitRenderer = exit.AddComponent<SpriteRenderer>();
            exitRenderer.sprite = SquareSprite;
            exitRenderer.color = new Color(0.46f, 0.20f, 0.76f, 0.9f);
            exitRenderer.sortingOrder = 2;
            exit.AddComponent<BoxCollider2D>().size = Vector2.one;
            DungeonExit dungeonExit = exit.AddComponent<DungeonExit>();
            dungeonExit.Configure(entrance);

            GameObject outsidePoint = new GameObject("Dungeon Outside Spawn");
            outsidePoint.transform.SetParent(parent, false);
            outsidePoint.transform.position = new Vector3(8f, 6.45f, 0f);
            outsideSpawn = outsidePoint.transform;

            GameObject insidePoint = new GameObject("Dungeon Inside Spawn");
            insidePoint.transform.SetParent(dungeon.transform);
            insidePoint.transform.localPosition = new Vector3(0f, -3.05f, 0f);
            insideSpawn = insidePoint.transform;

            CreateDungeonChest(dungeon.transform, "Entry Chest", new Vector2(-2.15f, -4.25f), 8, 2, 2, 1, 0, 0);
            CreateDungeonChest(dungeon.transform, "Mineral Chest", new Vector2(5.75f, 1.45f), 14, 0, 5, 0, 1, 0);
            CreateDungeonChest(dungeon.transform, "Magic Chest", new Vector2(0f, 4.55f), 24, 3, 3, 0, 1, 1);

            Transform[] spawnPoints =
            {
                CreateDungeonSpawnPoint(dungeon.transform, "Entry Limo Spawn", new Vector2(-2.05f, -1.05f)),
                CreateDungeonSpawnPoint(dungeon.transform, "Entry Bat Spawn", new Vector2(2.05f, -1.1f)),
                CreateDungeonSpawnPoint(dungeon.transform, "North Bat Spawn", new Vector2(-2.05f, 3.25f)),
                CreateDungeonSpawnPoint(dungeon.transform, "North Golem Spawn", new Vector2(2.05f, 3.35f)),
                CreateDungeonSpawnPoint(dungeon.transform, "East Limo Spawn", new Vector2(4.85f, -1.8f)),
                CreateDungeonSpawnPoint(dungeon.transform, "East Golem Spawn", new Vector2(6.1f, 1.05f))
            };

            EnemyAIBase[] enemyPrefabs =
            {
                CreateEnemyPrefab("Limo", new Color(0.72f, 0.16f, 0.18f), 3, 1, 2.1f, 0.75f, 1.25f, 2),
                CreateEnemyPrefab("Murcielago", new Color(0.36f, 0.24f, 0.62f), 2, 1, 2.9f, 0.68f, 0.95f, 3),
                CreateEnemyPrefab("Golem", new Color(0.46f, 0.47f, 0.42f), 5, 2, 1.35f, 0.85f, 1.65f, 6)
            };
            GameObject poolObject = new GameObject("Dungeon Enemy Pool");
            poolObject.transform.SetParent(dungeon.transform);
            enemyPool = poolObject.AddComponent<DungeonEnemyPool>();
            enemyPool.Configure(enemyPrefabs, player, spawnPoints, 9);
            return dungeon;
        }

        private static void CreateDungeonRoomFloor(Transform parent, string name, Vector2 position, Vector2 scale, Color color)
        {
            GameObject room = new GameObject(name);
            room.transform.SetParent(parent);
            room.transform.localPosition = position;
            room.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            SpriteRenderer renderer = room.AddComponent<SpriteRenderer>();
            renderer.sprite = SquareSprite;
            renderer.color = color;
            renderer.sortingOrder = -7;
        }

        private static void CreateDungeonWall(Transform parent, string name, Vector2 position, Vector2 scale)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(parent);
            wall.transform.localPosition = position;
            wall.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            SpriteRenderer renderer = wall.AddComponent<SpriteRenderer>();
            renderer.sprite = SquareSprite;
            renderer.color = new Color(0.05f, 0.05f, 0.07f);
            renderer.sortingOrder = 1;
            wall.AddComponent<BoxCollider2D>().size = Vector2.one;
        }

        private static Transform CreateDungeonSpawnPoint(Transform parent, string name, Vector2 position)
        {
            GameObject point = new GameObject(name);
            point.transform.SetParent(parent);
            point.transform.localPosition = position;
            return point.transform;
        }

        private static DungeonChest CreateDungeonChest(
            Transform parent,
            string name,
            Vector2 position,
            int coins,
            int wood,
            int stone,
            int commonSeeds,
            int mineralSeeds,
            int magicSeeds)
        {
            GameObject chest = new GameObject(name);
            chest.transform.SetParent(parent);
            chest.transform.localPosition = position;
            chest.transform.localScale = new Vector3(0.75f, 0.6f, 1f);

            SpriteRenderer bodyRenderer = chest.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = SquareSprite;
            bodyRenderer.color = new Color(0.54f, 0.30f, 0.12f);
            bodyRenderer.sortingOrder = 3;

            GameObject lid = new GameObject("Chest Lid");
            lid.transform.SetParent(chest.transform);
            lid.transform.localPosition = new Vector3(0f, 0.55f, -0.01f);
            lid.transform.localScale = new Vector3(1.05f, 0.32f, 1f);
            SpriteRenderer lidRenderer = lid.AddComponent<SpriteRenderer>();
            lidRenderer.sprite = SquareSprite;
            lidRenderer.color = new Color(0.76f, 0.52f, 0.18f);
            lidRenderer.sortingOrder = 4;

            BoxCollider2D collider = chest.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.25f, 1.1f);
            collider.isTrigger = true;

            DungeonChest dungeonChest = chest.AddComponent<DungeonChest>();
            dungeonChest.Configure(bodyRenderer, lidRenderer, coins, wood, stone, commonSeeds, mineralSeeds, magicSeeds);
            return dungeonChest;
        }

        private static BasicEnemyAI CreateEnemyPrefab(
            string enemyName,
            Color color,
            int health,
            int damage,
            float speed,
            float attackRange,
            float attackInterval,
            int coinReward)
        {
            GameObject enemy = new GameObject($"{enemyName} Enemy");
            enemy.transform.localScale = GetEnemyScale(enemyName);
            SpriteRenderer renderer = enemy.AddComponent<SpriteRenderer>();
            renderer.sprite = CircleSprite;
            renderer.color = color;
            renderer.sortingOrder = 4;

            GameObject healthBackground = new GameObject("Enemy Health Background");
            healthBackground.transform.SetParent(enemy.transform);
            healthBackground.transform.localPosition = new Vector3(0f, 0.82f, -0.01f);
            healthBackground.transform.localScale = new Vector3(1.18f, 0.14f, 1f);
            SpriteRenderer healthBackgroundRenderer = healthBackground.AddComponent<SpriteRenderer>();
            healthBackgroundRenderer.sprite = SquareSprite;
            healthBackgroundRenderer.color = new Color(0.08f, 0.03f, 0.03f, 0.9f);
            healthBackgroundRenderer.sortingOrder = 7;

            GameObject healthFill = new GameObject("Enemy Health Fill");
            healthFill.transform.SetParent(healthBackground.transform);
            healthFill.transform.localPosition = Vector3.zero;
            SpriteRenderer healthFillRenderer = healthFill.AddComponent<SpriteRenderer>();
            healthFillRenderer.sprite = SquareSprite;
            healthFillRenderer.color = new Color(0.25f, 0.92f, 0.32f);
            healthFillRenderer.sortingOrder = 8;

            CircleCollider2D collider = enemy.AddComponent<CircleCollider2D>();
            collider.radius = 0.52f;
            collider.isTrigger = true;

            BasicEnemyAI ai = enemy.AddComponent<BasicEnemyAI>();
            ai.ConfigureStats(enemyName, health, damage, speed, attackRange, attackInterval, coinReward);
            ai.ConfigureVisuals(renderer, healthFillRenderer, color, enemyName);
            enemy.SetActive(false);
            return ai;
        }

        private static Vector3 GetEnemyScale(string enemyName)
        {
            if (enemyName == "Murcielago")
            {
                return new Vector3(0.72f, 0.48f, 1f);
            }

            if (enemyName == "Golem")
            {
                return new Vector3(0.82f, 0.82f, 1f);
            }

            return new Vector3(0.62f, 0.62f, 1f);
        }

        private static ShopEntrance BuildShopExterior(Transform parent)
        {
            GameObject shop = new GameObject("Shop Entrance");
            shop.transform.SetParent(parent);
            shop.transform.position = new Vector3(8f, -0.9f, 0f);

            GameObject baseObject = new GameObject("Shop Square");
            baseObject.transform.SetParent(shop.transform);
            baseObject.transform.localPosition = Vector3.zero;
            baseObject.transform.localScale = new Vector3(2.2f, 1.55f, 1f);
            SpriteRenderer baseRenderer = baseObject.AddComponent<SpriteRenderer>();
            baseRenderer.sprite = SquareSprite;
            baseRenderer.color = new Color(0.58f, 0.35f, 0.20f);
            baseRenderer.sortingOrder = 1;

            GameObject roof = new GameObject("Shop Roof");
            roof.transform.SetParent(shop.transform);
            roof.transform.localPosition = new Vector3(0f, 0.55f, -0.01f);
            roof.transform.localScale = new Vector3(2.45f, 0.48f, 1f);
            SpriteRenderer roofRenderer = roof.AddComponent<SpriteRenderer>();
            roofRenderer.sprite = SquareSprite;
            roofRenderer.color = new Color(0.72f, 0.19f, 0.15f);
            roofRenderer.sortingOrder = 2;

            GameObject door = new GameObject("Door");
            door.transform.SetParent(shop.transform);
            door.transform.localPosition = new Vector3(0f, -0.46f, -0.02f);
            door.transform.localScale = new Vector3(0.45f, 0.62f, 1f);
            SpriteRenderer doorRenderer = door.AddComponent<SpriteRenderer>();
            doorRenderer.sprite = SquareSprite;
            doorRenderer.color = new Color(0.20f, 0.12f, 0.08f);
            doorRenderer.sortingOrder = 3;

            BoxCollider2D collider = shop.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(2.35f, 1.7f);

            return shop.AddComponent<ShopEntrance>();
        }

        private static GameObject BuildShopInterior(Transform parent, out Transform outsideSpawn, out Transform insideSpawn)
        {
            GameObject interior = new GameObject("Shop Interior");
            interior.transform.SetParent(parent, false);
            interior.SetActive(false);

            GameObject floor = new GameObject("Shop Floor");
            floor.transform.SetParent(interior.transform);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(8f, 8f, 1f);
            SpriteRenderer floorRenderer = floor.AddComponent<SpriteRenderer>();
            floorRenderer.sprite = SquareSprite;
            floorRenderer.color = new Color(0.45f, 0.31f, 0.20f);
            floorRenderer.sortingOrder = -8;

            GameObject counter = new GameObject("Shop Counter");
            counter.transform.SetParent(interior.transform);
            counter.transform.localPosition = new Vector3(0f, 1.8f, -0.01f);
            counter.transform.localScale = new Vector3(4.4f, 0.72f, 1f);
            SpriteRenderer counterRenderer = counter.AddComponent<SpriteRenderer>();
            counterRenderer.sprite = SquareSprite;
            counterRenderer.color = new Color(0.25f, 0.14f, 0.08f);
            counterRenderer.sortingOrder = 1;

            GameObject clerk = new GameObject("Shop Clerk");
            clerk.transform.SetParent(interior.transform);
            clerk.transform.localPosition = new Vector3(0f, 2.35f, -0.02f);
            clerk.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
            SpriteRenderer clerkRenderer = clerk.AddComponent<SpriteRenderer>();
            clerkRenderer.sprite = CircleSprite;
            clerkRenderer.color = new Color(0.92f, 0.72f, 0.48f);
            clerkRenderer.sortingOrder = 2;

            GameObject exit = new GameObject("Shop Exit");
            exit.transform.SetParent(interior.transform);
            exit.transform.localPosition = new Vector3(0f, -3.35f, -0.02f);
            exit.transform.localScale = new Vector3(1.3f, 0.42f, 1f);
            SpriteRenderer exitRenderer = exit.AddComponent<SpriteRenderer>();
            exitRenderer.sprite = SquareSprite;
            exitRenderer.color = new Color(0.20f, 0.12f, 0.08f);
            exitRenderer.sortingOrder = 1;

            GameObject outsidePoint = new GameObject("Shop Outside Spawn");
            outsidePoint.transform.SetParent(parent, false);
            outsidePoint.transform.position = new Vector3(6.55f, -0.9f, 0f);
            outsideSpawn = outsidePoint.transform;

            GameObject insidePoint = new GameObject("Shop Inside Spawn");
            insidePoint.transform.SetParent(interior.transform);
            insidePoint.transform.localPosition = new Vector3(0f, -2.55f, 0f);
            insideSpawn = insidePoint.transform;

            return interior;
        }

        private static void CreateTree(Transform parent, Vector2 position, int amount = 3, int coins = 5)
        {
            GameObject tree = new GameObject("Tree");
            tree.transform.SetParent(parent);
            tree.transform.position = position;

            GameObject trunk = new GameObject("Trunk");
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = new Vector3(0f, -0.28f, 0f);
            trunk.transform.localScale = new Vector3(0.34f, 0.78f, 1f);
            SpriteRenderer trunkRenderer = trunk.AddComponent<SpriteRenderer>();
            trunkRenderer.sprite = SquareSprite;
            trunkRenderer.color = new Color(0.45f, 0.24f, 0.12f);
            trunkRenderer.sortingOrder = 2;

            GameObject canopy = new GameObject("Canopy");
            canopy.transform.SetParent(tree.transform);
            canopy.transform.localPosition = new Vector3(0f, 0.24f, -0.02f);
            canopy.transform.localScale = new Vector3(1.25f, 1.15f, 1f);
            SpriteRenderer canopyRenderer = canopy.AddComponent<SpriteRenderer>();
            canopyRenderer.sprite = CircleSprite;
            canopyRenderer.color = new Color(0.16f, 0.45f, 0.18f);
            canopyRenderer.sortingOrder = 3;
            WorldSpriteDepth trunkDepth = tree.AddComponent<WorldSpriteDepth>();
            trunkDepth.Visual = trunkRenderer;
            WorldSpriteDepth canopyDepth = tree.AddComponent<WorldSpriteDepth>();
            canopyDepth.Visual = canopyRenderer;
            canopyDepth.GroundOffset = -0.05f;

            CircleCollider2D collider = tree.AddComponent<CircleCollider2D>();
            collider.radius = 0.72f;

            TreeResource resource = tree.AddComponent<TreeResource>();
            resource.Configure(canopyRenderer, trunkRenderer, amount, coins);
            TreeOcclusionFader.Ensure(tree);
            ResourceSpawnPoint.Attach(resource);
        }

        private static void CreateRock(Transform parent, Vector2 position, int amount = 3, int coins = 5)
        {
            GameObject rock = new GameObject("Rock");
            rock.transform.SetParent(parent);
            rock.transform.position = position;
            rock.transform.localScale = new Vector3(0.95f, 0.72f, 1f);

            SpriteRenderer renderer = rock.AddComponent<SpriteRenderer>();
            renderer.sprite = CircleSprite;
            renderer.color = new Color(0.45f, 0.47f, 0.48f);
            renderer.sortingOrder = 2;
            WorldSpriteDepth depth = rock.AddComponent<WorldSpriteDepth>();
            depth.Visual = renderer;

            CircleCollider2D collider = rock.AddComponent<CircleCollider2D>();
            collider.radius = 0.65f;

            RockResource resource = rock.AddComponent<RockResource>();
            resource.Configure(renderer, null, amount, coins);
            ResourceSpawnPoint.Attach(resource);
        }

        private static GameObject BuildPlayer(Transform parent)
        {
            GameObject player = new GameObject("Player Base");
            player.transform.SetParent(parent, false);
            player.tag = "Player";
            player.transform.position = new Vector3(-0.47f, 0.83f, 0f);
            player.transform.localScale = new Vector3(1f, 1f, 1f);

            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = CircleSprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 5;
            WorldSpriteDepth depth = player.AddComponent<WorldSpriteDepth>();
            depth.Visual = renderer;
            depth.GroundOffset = -0.4f;

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
            collider.radius = 0.55f;

            player.AddComponent<PlayerMovementController>();
            player.AddComponent<PlayerInventory>();
            player.AddComponent<PlayerSurvivalStats>();
            player.AddComponent<PlayerToolbelt>();
            player.AddComponent<PlayerCharacterAnimator>().Configure(renderer, "George", "Josh");
            player.AddComponent<PlayerToolUpgradeController>();
            player.AddComponent<PlayerCraftingController>().Configure(SquareSprite, CircleSprite);
            player.AddComponent<PlayerCombatController>();
            player.AddComponent<FarmPlayerInteractor>();

            return player;
        }

        private static void BuildSaveSystem(
            Transform parent,
            Transform player,
            PlayerInventory inventory,
            PlayerSurvivalStats survivalStats,
            PlayerMovementController movement,
            PlayerToolbelt toolbelt,
            PlayerToolUpgradeController toolUpgrades,
            PlayerCraftingController crafting,
            TutorialQuestSystem questSystem,
            ShopEntrance shopEntrance,
            DungeonEntrance dungeonEntrance,
            bool loadSavedGame)
        {
            GameObject saveObject = new GameObject("Game Save System");
            saveObject.transform.SetParent(parent, false);
            GameSaveSystem saveSystem = saveObject.AddComponent<GameSaveSystem>();
            saveSystem.Configure(player, inventory, survivalStats, movement, toolbelt, toolUpgrades, crafting, questSystem, shopEntrance, dungeonEntrance);
            if (loadSavedGame)
            {
                saveSystem.TryLoadGame();
            }
        }

        private static void BuildUi(
            Transform parent,
            Transform controllersParent,
            PlayerToolbelt toolbelt,
            PlayerInventory inventory,
            PlayerToolUpgradeController toolUpgrades,
            PlayerCraftingController crafting,
            PlayerSurvivalStats survivalStats,
            PlayerMovementController movement,
            PlayerCombatController combat,
            FarmPlayerInteractor interactor,
            out SimpleShopSystem shopSystem,
            out TutorialQuestSystem questSystem)
        {
            EnsureEventSystem(controllersParent);

            GameObject canvasObject = new GameObject("Farm HUD");
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(450f, 800f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            Text prompt = CreateText(canvasObject.transform, "Prompt", 22, TextAnchor.LowerCenter);
            RectTransform promptRect = prompt.rectTransform;
            promptRect.anchorMin = new Vector2(0.5f, 0f);
            promptRect.anchorMax = new Vector2(0.5f, 0f);
            promptRect.anchoredPosition = new Vector2(0f, 30f);
            promptRect.sizeDelta = new Vector2(760f, 64f);

            Text notification = CreateText(canvasObject.transform, "Notification", 28, TextAnchor.UpperCenter);
            RectTransform notificationRect = notification.rectTransform;
            notificationRect.anchorMin = new Vector2(0.5f, 1f);
            notificationRect.anchorMax = new Vector2(0.5f, 1f);
            notificationRect.anchoredPosition = new Vector2(0f, -32f);
            notificationRect.sizeDelta = new Vector2(850f, 80f);

            BuildToolSelector(canvasObject.transform, toolbelt, out Text tool, out Image toolIcon, out Image swordFrame, out Image bowFrame, out Image hoeFrame);
            BuildSurvivalHud(canvasObject.transform, out Image[] heartIcons, out Image hungerFill, out Text hungerText);
            BuildInventory(canvasObject.transform, inventory, out Text[] slotTexts, out Image[] slotIcons);
            BuildInventoryPanel(canvasObject.transform, inventory, toolUpgrades, crafting);
            BuildInteractionButton(canvasObject.transform, interactor, out Text interactionText, out Button interactionButton);
            BuildAttackButton(canvasObject.transform, combat);
            questSystem = BuildQuestHud(canvasObject.transform);
            Image brightnessOverlay = BuildBrightnessOverlay(canvasObject.transform);
            movement?.SetMovementMode(PlayerMovementController.MobileMovementMode.KeyboardAndMouse);
            BuildOptionsMenu(canvasObject.transform, movement, brightnessOverlay);
            shopSystem = BuildShopPanel(canvasObject.transform, inventory, toolUpgrades, crafting);

            FarmNotificationCenter center = canvasObject.AddComponent<FarmNotificationCenter>();
            FarmNotificationCenter.Bind(
                prompt,
                tool,
                interactionText,
                interactionButton,
                notification,
                toolIcon,
                slotTexts,
                slotIcons,
                heartIcons,
                hungerFill,
                hungerText);
            center.ConfigureFloatingInteraction(interactionButton, interactionText);
            center.enabled = true;

            if (inventory != null)
            {
                RefreshInventory(inventory);
            }
            center.ConfigureGameplayBindings(inventory, toolbelt, slotTexts, slotIcons,
                System.Array.ConvertAll((FarmTool[])System.Enum.GetValues(typeof(FarmTool)), GetToolSprite));
            center.ConfigureWeaponFrames(swordFrame, bowFrame, hoeFrame);

            if (survivalStats != null)
            {
                survivalStats.StatsChanged += () => RefreshSurvival(survivalStats);
                RefreshSurvival(survivalStats);
            }

            FarmNotificationCenter.SetPrompt("Explora y reconstruye el pueblo.");
        }

        private static void EnsureEventSystem(Transform parent)
        {
            EventSystem existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing != null)
            {
                existing.transform.SetParent(parent, true);
                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.transform.SetParent(parent, false);
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static void BuildToolSelector(Transform parent, PlayerToolbelt toolbelt, out Text toolName, out Image toolIcon, out Image swordFrame, out Image bowFrame, out Image hoeFrame)
        {
            GameObject selector = new GameObject("Mobile Weapon Selector");
            selector.transform.SetParent(parent, false);
            RectTransform selectorRect = selector.AddComponent<RectTransform>();
            selectorRect.anchorMin = new Vector2(0.5f, 0f);
            selectorRect.anchorMax = new Vector2(0.5f, 0f);
            selectorRect.pivot = new Vector2(0.5f, 0f);
            selectorRect.anchoredPosition = new Vector2(0f, 26f);
            selectorRect.sizeDelta = new Vector2(160f, 108f);

            Image background = selector.AddComponent<Image>();
            ApplyPanelSprite(background);
            background.color = new Color(0.05f, 0.05f, 0.04f, 0.82f);

            Button swordButton = CreateWeaponSlotButton(selector.transform, "Sword Slot", FarmTool.Sword, new Vector2(-36f, 18f), toolbelt, out swordFrame);
            Button bowButton = CreateWeaponSlotButton(selector.transform, "Bow Slot", FarmTool.Bow, new Vector2(36f, 18f), toolbelt, out bowFrame);
            hoeFrame = null;
            SetButtonTextSize(swordButton, 15);
            SetButtonTextSize(bowButton, 15);

            GameObject iconObject = new GameObject("Selected Tool Icon");
            iconObject.transform.SetParent(selector.transform, false);
            toolIcon = iconObject.AddComponent<Image>();
            toolIcon.color = Color.white;
            RectTransform iconRect = toolIcon.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = Vector2.zero;
            iconObject.SetActive(false);

            toolName = CreateText(selector.transform, "Selected Tool Name", 17, TextAnchor.UpperCenter);
            RectTransform nameRect = toolName.rectTransform;
            nameRect.anchorMin = new Vector2(0.5f, 1f);
            nameRect.anchorMax = new Vector2(0.5f, 1f);
            nameRect.anchoredPosition = new Vector2(0f, -8f);
            nameRect.sizeDelta = new Vector2(170f, 28f);
        }

        private static Button CreateWeaponSlotButton(Transform parent, string name, FarmTool tool, Vector2 position, PlayerToolbelt toolbelt, out Image frame)
        {
            GameObject slotObject = new GameObject(name);
            slotObject.transform.SetParent(parent, false);

            frame = slotObject.AddComponent<Image>();
            ApplySlotSprite(frame);
            frame.color = new Color(1f, 1f, 1f, 0.82f);

            Button button = slotObject.AddComponent<Button>();
            button.targetGraphic = frame;
            if (toolbelt != null)
            {
                button.onClick.AddListener(() => toolbelt.Select(tool));
            }

            RectTransform rect = frame.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(74f, 74f);

            GameObject iconObject = new GameObject("Weapon Icon");
            iconObject.transform.SetParent(slotObject.transform, false);
            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = GetToolSprite(tool);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 5f);
            iconRect.sizeDelta = new Vector2(44f, 44f);

            Text hotkey = CreateText(slotObject.transform, tool + " Key", 14, TextAnchor.LowerRight);
            hotkey.text = tool == FarmTool.Sword ? "1" : tool == FarmTool.Bow ? "2" : "3";
            RectTransform keyRect = hotkey.rectTransform;
            keyRect.anchorMin = Vector2.zero;
            keyRect.anchorMax = Vector2.one;
            keyRect.offsetMin = new Vector2(4f, 2f);
            keyRect.offsetMax = new Vector2(-6f, -4f);

            return button;
        }

        private static void BuildInventory(Transform parent, PlayerInventory inventory, out Text[] slotTexts, out Image[] slotIcons)
        {
            int slotCount = inventory != null ? inventory.SlotCount : 6;
            slotTexts = new Text[slotCount];
            slotIcons = new Image[slotCount];

            GameObject inventoryRoot = new GameObject("Inventory Slots");
            inventoryRoot.transform.SetParent(parent, false);
            RectTransform inventoryRect = inventoryRoot.AddComponent<RectTransform>();
            inventoryRect.anchorMin = new Vector2(1f, 0f);
            inventoryRect.anchorMax = new Vector2(1f, 0f);
            inventoryRect.anchoredPosition = new Vector2(-226f, 64f);
            inventoryRect.sizeDelta = new Vector2(440f, 72f);

            for (int i = 0; i < slotCount; i++)
            {
                GameObject slot = new GameObject($"Slot {i + 1}");
                slot.transform.SetParent(inventoryRoot.transform, false);

                Image slotBackground = slot.AddComponent<Image>();
                ApplySlotSprite(slotBackground);
                slotBackground.color = new Color(1f, 1f, 1f, 0.96f);

                RectTransform slotRect = slotBackground.rectTransform;
                slotRect.anchorMin = new Vector2(0f, 0.5f);
                slotRect.anchorMax = new Vector2(0f, 0.5f);
                slotRect.anchoredPosition = new Vector2(32f + i * 62f, 0f);
                slotRect.sizeDelta = new Vector2(54f, 54f);

                GameObject icon = new GameObject("Item Icon");
                icon.transform.SetParent(slot.transform, false);
                slotIcons[i] = icon.AddComponent<Image>();
                slotIcons[i].sprite = SeedSprite;
                slotIcons[i].color = Color.white;
                RectTransform iconRect = slotIcons[i].rectTransform;
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = new Vector2(0f, 4f);
                iconRect.sizeDelta = new Vector2(34f, 34f);

                slotTexts[i] = CreateText(slot.transform, "Item Count", 16, TextAnchor.LowerRight);
                RectTransform countRect = slotTexts[i].rectTransform;
                countRect.anchorMin = new Vector2(0f, 0f);
                countRect.anchorMax = new Vector2(1f, 1f);
                countRect.offsetMin = new Vector2(4f, 2f);
                countRect.offsetMax = new Vector2(-6f, -4f);
            }
        }

        private static void BuildInventoryPanel(
            Transform parent,
            PlayerInventory inventory,
            PlayerToolUpgradeController toolUpgrades,
            PlayerCraftingController crafting)
        {
            GameObject controllerObject = new GameObject("Inventory Panel System");
            controllerObject.transform.SetParent(parent, false);
            InventoryPanelSystem inventoryPanelSystem = controllerObject.AddComponent<InventoryPanelSystem>();

            Button inventoryButton = CreateButton(parent, "Inventory Button", "Inv", new Vector2(-108f, -308f), new Vector2(76f, 42f));
            RectTransform buttonRect = (RectTransform)inventoryButton.transform;
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            SetButtonTextSize(inventoryButton, 17);
            inventoryButton.onClick.AddListener(inventoryPanelSystem.Toggle);

            GameObject panel = new GameObject("Full Inventory Panel");
            panel.transform.SetParent(parent, false);
            Image panelImage = panel.AddComponent<Image>();
            ApplyInventoryPanelSprite(panelImage);
            panelImage.color = new Color(1f, 1f, 1f, 0.98f);

            RectTransform panelRect = panelImage.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(390f, 560f);

            Text title = CreateText(panel.transform, "Inventory Title", 28, TextAnchor.UpperCenter);
            title.text = "Inventario";
            SetPanelChildRect(title.rectTransform, new Vector2(0f, 238f), new Vector2(330f, 44f));

            Text content = CreateText(panel.transform, "Inventory Content", 19, TextAnchor.UpperLeft);
            content.color = new Color(0.95f, 0.95f, 0.88f);
            SetPanelChildRect(content.rectTransform, new Vector2(0f, 18f), new Vector2(315f, 390f));

            Button eatButton = CreateButton(panel.transform, "Eat Food", "Comer", new Vector2(-90f, -232f), new Vector2(155f, 48f));
            eatButton.onClick.AddListener(inventoryPanelSystem.EatFood);
            Button closeButton = CreateButton(panel.transform, "Close Inventory", "Cerrar", new Vector2(90f, -232f), new Vector2(155f, 48f));
            SetButtonTextSize(closeButton, 21);
            closeButton.onClick.AddListener(inventoryPanelSystem.Close);

            inventoryPanelSystem.Configure(panel, content, inventory, toolUpgrades, crafting);
        }

        private static void BuildSurvivalHud(Transform parent, out Image[] heartIcons, out Image hungerFill, out Text hungerText)
        {
            GameObject root = new GameObject("Survival HUD");
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(18f, -18f);
            rootRect.sizeDelta = new Vector2(372f, 112f);

            Image hudBackground = root.AddComponent<Image>();
            ApplyHudSprite(hudBackground);
            hudBackground.color = new Color(0.06f, 0.055f, 0.045f, 0.92f);
            hudBackground.raycastTarget = false;

            Text healthLabel = CreateText(root.transform, "Health Label", 17, TextAnchor.MiddleLeft);
            healthLabel.text = "Vida";
            healthLabel.fontStyle = FontStyle.Bold;
            AddTextOutline(healthLabel);
            RectTransform healthLabelRect = healthLabel.rectTransform;
            healthLabelRect.anchorMin = new Vector2(0f, 1f);
            healthLabelRect.anchorMax = new Vector2(0f, 1f);
            healthLabelRect.anchoredPosition = new Vector2(82f, -27f);
            healthLabelRect.sizeDelta = new Vector2(74f, 28f);

            heartIcons = new Image[8];
            for (int i = 0; i < heartIcons.Length; i++)
            {
                GameObject heartObject = new GameObject($"Heart {i + 1}");
                heartObject.transform.SetParent(root.transform, false);
                Image heart = heartObject.AddComponent<Image>();
                heart.sprite = HeartSprite;
                heart.color = new Color(0.92f, 0.18f, 0.20f, 1f);
                heart.preserveAspect = true;
                heartIcons[i] = heart;

                RectTransform heartRect = heart.rectTransform;
                heartRect.anchorMin = new Vector2(0f, 1f);
                heartRect.anchorMax = new Vector2(0f, 1f);
                heartRect.anchoredPosition = new Vector2(148f + i * 26f, -19f);
                heartRect.sizeDelta = new Vector2(24f, 24f);
            }

            hungerFill = null;
            hungerText = null;
        }

        private static void BuildInteractionButton(
            Transform parent,
            FarmPlayerInteractor interactor,
            out Text interactionText,
            out Button interactionButton)
        {
            interactionButton = CreateButton(parent, "Interact Button", "Interactuar", new Vector2(0f, -122f), new Vector2(128f, 60f));
            RectTransform buttonRect = (RectTransform)interactionButton.transform;
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);

            Image buttonImage = interactionButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = new Color(0.08f, 0.06f, 0.05f, 0.94f);
            }

            interactionText = interactionButton.GetComponentInChildren<Text>();
            if (interactionText != null)
            {
                interactionText.fontSize = 18;
                interactionText.resizeTextForBestFit = true;
                interactionText.resizeTextMinSize = 12;
                interactionText.resizeTextMaxSize = 18;
                interactionText.alignment = TextAnchor.MiddleCenter;
                interactionText.color = new Color(1f, 0.88f, 0.68f, 1f);
                interactionText.fontStyle = FontStyle.Bold;
                AddTextOutline(interactionText);
            }

            if (interactor != null)
            {
                interactionButton.onClick.AddListener(interactor.PerformInteraction);
            }

            interactionButton.gameObject.SetActive(false);
        }

        private static void BuildAttackButton(Transform parent, PlayerCombatController combat)
        {
            Button attackButton = CreateButton(parent, "Attack Button", "Atacar", new Vector2(-92f, 146f), new Vector2(122f, 62f));
            RectTransform attackRect = (RectTransform)attackButton.transform;
            attackRect.anchorMin = new Vector2(1f, 0f);
            attackRect.anchorMax = new Vector2(1f, 0f);

            Image attackImage = attackButton.GetComponent<Image>();
            if (attackImage != null)
            {
                attackImage.color = new Color(0.08f, 0.06f, 0.05f, 0.92f);
            }

            Text text = attackButton.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.fontSize = 22;
                text.color = new Color(1f, 0.88f, 0.68f, 1f);
                text.fontStyle = FontStyle.Bold;
                AddTextOutline(text);
            }

            GameObject cooldownObject = new GameObject("Attack Cooldown Fill");
            cooldownObject.transform.SetParent(attackButton.transform, false);
            Image cooldownFill = cooldownObject.AddComponent<Image>();
            cooldownFill.sprite = SquareSprite;
            cooldownFill.type = Image.Type.Filled;
            cooldownFill.fillMethod = Image.FillMethod.Horizontal;
            cooldownFill.fillOrigin = 1;
            cooldownFill.color = new Color(0f, 0f, 0f, 0.32f);
            RectTransform fillRect = cooldownFill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.SetAsFirstSibling();

            if (combat != null)
            {
                attackButton.onClick.AddListener(combat.Attack);
                combat.ConfigureAttackUi(text, cooldownFill);
            }
        }

        private static TutorialQuestSystem BuildQuestHud(Transform parent)
        {
            GameObject questObject = new GameObject("Tutorial Quest System");
            questObject.transform.SetParent(parent, false);
            TutorialQuestSystem questSystem = questObject.AddComponent<TutorialQuestSystem>();

            GameObject panel = new GameObject("Mission");
            panel.transform.SetParent(parent, false);
            Image panelImage = panel.AddComponent<Image>();
            ApplyHudSprite(panelImage);
            panelImage.color = new Color(0.045f, 0.055f, 0.038f, 0.95f);

            RectTransform panelRect = panelImage.rectTransform;
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-12f, -104f);
            panelRect.sizeDelta = new Vector2(384f, 126f);

            GameObject iconObject = new GameObject("Quest Icon");
            iconObject.transform.SetParent(panel.transform, false);
            Image icon = iconObject.AddComponent<Image>();
            icon.sprite = LoadBackpackIcon("Quest");
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            RectTransform iconRect = icon.rectTransform;
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(12f, -12f);
            iconRect.sizeDelta = new Vector2(32f, 32f);

            Text questText = CreateText(panel.transform, "Quest Text", 15, TextAnchor.UpperLeft);
            questText.color = new Color(1f, 0.93f, 0.76f);
            questText.fontStyle = FontStyle.Bold;
            questText.resizeTextForBestFit = true;
            questText.resizeTextMinSize = 11;
            questText.resizeTextMaxSize = 15;
            questText.lineSpacing = 0.92f;
            AddTextOutline(questText);
            RectTransform textRect = questText.rectTransform;
            textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(0f, 1f);
            textRect.anchoredPosition = new Vector2(52f, -10f);
            textRect.sizeDelta = new Vector2(318f, 108f);

            questSystem.Configure(questText);
            return questSystem;
        }

        private static Image BuildBrightnessOverlay(Transform parent)
        {
            GameObject overlayObject = new GameObject("Brightness Overlay");
            overlayObject.transform.SetParent(parent, false);

            Image overlay = overlayObject.AddComponent<Image>();
            overlay.sprite = SquareSprite;
            overlay.color = new Color(0f, 0f, 0f, 0f);
            overlay.raycastTarget = false;

            RectTransform rect = overlay.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            overlayObject.transform.SetAsLastSibling();
            return overlay;
        }

        private static void BuildOptionsMenu(
            Transform parent,
            PlayerMovementController movement,
            Image brightnessOverlay)
        {
            GameObject controllerObject = new GameObject("Mobile Options Controller");
            controllerObject.transform.SetParent(parent, false);
            MobileOptionsMenu options = controllerObject.AddComponent<MobileOptionsMenu>();

            Button optionsButton = CreateButton(parent, "Options Button", "Opc", new Vector2(-36f, -308f), new Vector2(66f, 42f));
            RectTransform optionsButtonRect = (RectTransform)optionsButton.transform;
            optionsButtonRect.anchorMin = new Vector2(1f, 1f);
            optionsButtonRect.anchorMax = new Vector2(1f, 1f);
            SetButtonTextSize(optionsButton, 17);

            GameObject panel = new GameObject("Options Panel");
            panel.transform.SetParent(parent, false);
            Image panelImage = panel.AddComponent<Image>();
            ApplyPanelSprite(panelImage);
            panelImage.color = new Color(1f, 1f, 1f, 0.98f);

            RectTransform panelRect = panelImage.rectTransform;
            panelRect.anchorMin = new Vector2(1f, 0.5f);
            panelRect.anchorMax = new Vector2(1f, 0.5f);
            panelRect.anchoredPosition = new Vector2(-250f, 0f);
            panelRect.sizeDelta = new Vector2(430f, 470f);

            Text title = CreateText(panel.transform, "Title", 28, TextAnchor.UpperCenter);
            title.text = "Opciones";
            SetPanelChildRect(title.rectTransform, new Vector2(0f, 202f), new Vector2(360f, 48f));

            CreateOptionRow(panel.transform, "Volumen", 142f, out Text volumeValue, out Button volumeDown, out Button volumeUp);
            volumeDown.onClick.AddListener(options.DecreaseVolume);
            volumeUp.onClick.AddListener(options.IncreaseVolume);

            CreateOptionRow(panel.transform, "Brillo", 82f, out Text brightnessValue, out Button brightnessDown, out Button brightnessUp);
            brightnessDown.onClick.AddListener(options.DecreaseBrightness);
            brightnessUp.onClick.AddListener(options.IncreaseBrightness);

            Text languageLabel = CreateText(panel.transform, "Language Label", 19, TextAnchor.MiddleLeft);
            languageLabel.text = "Idioma";
            SetPanelChildRect(languageLabel.rectTransform, new Vector2(-118f, 22f), new Vector2(130f, 42f));

            Text languageValue = CreateText(panel.transform, "Language Value", 18, TextAnchor.MiddleCenter);
            SetPanelChildRect(languageValue.rectTransform, new Vector2(50f, 22f), new Vector2(135f, 42f));

            Button languageButton = CreateButton(panel.transform, "Language Toggle", "Cambiar", new Vector2(142f, 22f), new Vector2(108f, 42f));
            languageButton.onClick.AddListener(options.ToggleLanguage);

            Text mobilityTitle = CreateText(panel.transform, "Mobility Label", 19, TextAnchor.MiddleLeft);
            mobilityTitle.text = "Movilidad";
            SetPanelChildRect(mobilityTitle.rectTransform, new Vector2(-105f, -50f), new Vector2(155f, 38f));

            Text mobilityValue = CreateText(panel.transform, "Mobility Value", 18, TextAnchor.MiddleCenter);
            SetPanelChildRect(mobilityValue.rectTransform, new Vector2(92f, -50f), new Vector2(205f, 38f));

            Button closeButton = CreateButton(panel.transform, "Close Options", "Cerrar", new Vector2(0f, -150f), new Vector2(150f, 44f));
            closeButton.onClick.AddListener(options.TogglePanel);

            options.Configure(panel, brightnessOverlay, movement, volumeValue, brightnessValue, languageValue, mobilityValue);
            optionsButton.onClick.AddListener(options.TogglePanel);
            panel.SetActive(false);
        }

        private static SimpleShopSystem BuildShopPanel(Transform parent, PlayerInventory inventory, PlayerToolUpgradeController toolUpgrades, PlayerCraftingController crafting)
        {
            GameObject controllerObject = new GameObject("Simple Shop System");
            controllerObject.transform.SetParent(parent, false);
            SimpleShopSystem shopSystem = controllerObject.AddComponent<SimpleShopSystem>();

            GameObject panel = new GameObject("Shop Panel");
            panel.transform.SetParent(parent, false);
            Image panelImage = panel.AddComponent<Image>();
            ApplyInventoryPanelSprite(panelImage);
            panelImage.color = new Color(1f, 1f, 1f, 0.98f);

            RectTransform panelRect = panelImage.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(410f, 740f);

            Text title = CreateText(panel.transform, "Shop Title", 30, TextAnchor.UpperCenter);
            title.text = "Tienda";
            SetPanelChildRect(title.rectTransform, new Vector2(0f, 314f), new Vector2(350f, 44f));

            Text wallet = CreateText(panel.transform, "Wallet", 22, TextAnchor.MiddleCenter);
            wallet.color = new Color(1f, 0.80f, 0.28f);
            SetPanelChildRect(wallet.rectTransform, new Vector2(0f, 274f), new Vector2(350f, 36f));

            Text stock = CreateText(panel.transform, "Player Stock", 18, TextAnchor.MiddleCenter);
            SetPanelChildRect(stock.rectTransform, new Vector2(0f, 226f), new Vector2(380f, 82f));

            Text buyTitle = CreateText(panel.transform, "Buy Title", 22, TextAnchor.MiddleCenter);
            buyTitle.text = "Comprar";
            SetPanelChildRect(buyTitle.rectTransform, new Vector2(-96f, 164f), new Vector2(160f, 34f));

            Text sellTitle = CreateText(panel.transform, "Sell Title", 22, TextAnchor.MiddleCenter);
            sellTitle.text = "Vender";
            SetPanelChildRect(sellTitle.rectTransform, new Vector2(96f, 164f), new Vector2(160f, 34f));

            Button buySeeds = CreateButton(panel.transform, "Buy Seeds", "Comun -5", new Vector2(-96f, 120f), new Vector2(168f, 42f));
            SetButtonTextSize(buySeeds, 18);
            buySeeds.onClick.AddListener(shopSystem.BuySeeds);

            Button buyMineralSeeds = CreateButton(panel.transform, "Buy Mineral Seeds", "Mineral -12", new Vector2(-96f, 74f), new Vector2(168f, 42f));
            SetButtonTextSize(buyMineralSeeds, 18);
            buyMineralSeeds.onClick.AddListener(shopSystem.BuyMineralSeeds);

            Button buyMagicSeeds = CreateButton(panel.transform, "Buy Magic Seeds", "Magica -25", new Vector2(-96f, 28f), new Vector2(168f, 42f));
            SetButtonTextSize(buyMagicSeeds, 18);
            buyMagicSeeds.onClick.AddListener(shopSystem.BuyMagicSeeds);

            Button buyWood = CreateButton(panel.transform, "Buy Wood", "Madera -4", new Vector2(-96f, -18f), new Vector2(168f, 42f));
            SetButtonTextSize(buyWood, 18);
            buyWood.onClick.AddListener(shopSystem.BuyWood);

            Button sellSeeds = CreateButton(panel.transform, "Sell Seeds", "Comun +1", new Vector2(96f, 120f), new Vector2(168f, 42f));
            SetButtonTextSize(sellSeeds, 18);
            sellSeeds.onClick.AddListener(shopSystem.SellSeeds);

            Button sellMineralSeeds = CreateButton(panel.transform, "Sell Mineral Seeds", "Mineral +5", new Vector2(96f, 74f), new Vector2(168f, 42f));
            SetButtonTextSize(sellMineralSeeds, 18);
            sellMineralSeeds.onClick.AddListener(shopSystem.SellMineralSeeds);

            Button sellMagicSeeds = CreateButton(panel.transform, "Sell Magic Seeds", "Magica +12", new Vector2(96f, 28f), new Vector2(168f, 42f));
            SetButtonTextSize(sellMagicSeeds, 18);
            sellMagicSeeds.onClick.AddListener(shopSystem.SellMagicSeeds);

            Button sellFruit = CreateButton(panel.transform, "Sell Fruit", "Fruta +6", new Vector2(96f, -18f), new Vector2(168f, 42f));
            SetButtonTextSize(sellFruit, 18);
            sellFruit.onClick.AddListener(shopSystem.SellFruit);

            Text hint = CreateText(panel.transform, "Shop Hint", 17, TextAnchor.MiddleCenter);
            hint.text = "Mejora herramientas para sacar mas recursos.";
            SetPanelChildRect(hint.rectTransform, new Vector2(0f, -64f), new Vector2(350f, 34f));

            Text upgradesTitle = CreateText(panel.transform, "Upgrades Title", 21, TextAnchor.MiddleCenter);
            upgradesTitle.text = "Mejoras";
            SetPanelChildRect(upgradesTitle.rectTransform, new Vector2(0f, -98f), new Vector2(350f, 32f));

            Button upgradeAxe = CreateButton(panel.transform, "Upgrade Axe", "Hacha", new Vector2(-128f, -140f), new Vector2(112f, 42f));
            SetButtonTextSize(upgradeAxe, 18);
            upgradeAxe.onClick.AddListener(shopSystem.UpgradeAxe);

            Button upgradePickaxe = CreateButton(panel.transform, "Upgrade Pickaxe", "Pico", new Vector2(0f, -140f), new Vector2(112f, 42f));
            SetButtonTextSize(upgradePickaxe, 18);
            upgradePickaxe.onClick.AddListener(shopSystem.UpgradePickaxe);

            Button upgradeHoe = CreateButton(panel.transform, "Upgrade Hoe", "Azada", new Vector2(128f, -140f), new Vector2(112f, 42f));
            SetButtonTextSize(upgradeHoe, 18);
            upgradeHoe.onClick.AddListener(shopSystem.UpgradeHoe);

            Text upgradeHint = CreateText(panel.transform, "Upgrade Hint", 15, TextAnchor.MiddleCenter);
            upgradeHint.text = "Nv.2: 15 oro, 4 madera, 4 piedra | Nv.3: 35 oro, 8 madera, 8 piedra";
            SetPanelChildRect(upgradeHint.rectTransform, new Vector2(0f, -182f), new Vector2(370f, 38f));

            Text craftingTitle = CreateText(panel.transform, "Crafting Title", 21, TextAnchor.MiddleCenter);
            craftingTitle.text = "Crafting";
            SetPanelChildRect(craftingTitle.rectTransform, new Vector2(0f, -224f), new Vector2(350f, 32f));

            Button craftStorage = CreateButton(panel.transform, "Craft Storage", "Almacen", new Vector2(-96f, -268f), new Vector2(168f, 42f));
            SetButtonTextSize(craftStorage, 18);
            craftStorage.onClick.AddListener(shopSystem.CraftStorage);

            Button craftCamp = CreateButton(panel.transform, "Craft Camp", "Campamento", new Vector2(96f, -268f), new Vector2(168f, 42f));
            SetButtonTextSize(craftCamp, 18);
            craftCamp.onClick.AddListener(shopSystem.CraftCamp);

            Button exit = CreateButton(panel.transform, "Exit Shop", "Salir", new Vector2(0f, -326f), new Vector2(190f, 48f));
            SetButtonTextSize(exit, 22);
            exit.onClick.AddListener(shopSystem.ExitShop);

            shopSystem.Configure(panel, wallet, stock, inventory, toolUpgrades, crafting);
            return shopSystem;
        }

        private static void SetButtonTextSize(Button button, int size)
        {
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.fontSize = size;
            }
        }

        private static void CreateOptionRow(
            Transform parent,
            string label,
            float y,
            out Text valueText,
            out Button decreaseButton,
            out Button increaseButton)
        {
            Text labelText = CreateText(parent, $"{label} Label", 19, TextAnchor.MiddleLeft);
            labelText.text = label;
            SetPanelChildRect(labelText.rectTransform, new Vector2(-118f, y), new Vector2(130f, 42f));

            decreaseButton = CreateButton(parent, $"{label} Down", "-", new Vector2(16f, y), new Vector2(44f, 42f));

            valueText = CreateText(parent, $"{label} Value", 18, TextAnchor.MiddleCenter);
            SetPanelChildRect(valueText.rectTransform, new Vector2(82f, y), new Vector2(76f, 42f));

            increaseButton = CreateButton(parent, $"{label} Up", "+", new Vector2(150f, y), new Vector2(44f, 42f));
        }

        private static void SetPanelChildRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Text CreateText(Transform parent, string name, int size, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void AddTextOutline(Text text)
        {
            if (text == null || text.GetComponent<Outline>() != null) return;
            Outline outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.04f, 0.025f, 0.015f, 0.92f);
            outline.effectDistance = new Vector2(1.25f, -1.25f);
            outline.useGraphicAlpha = true;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();
            ApplyButtonSprite(image);
            image.color = new Color(1f, 1f, 1f, 0.96f);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text text = CreateText(buttonObject.transform, "Label", 32, TextAnchor.MiddleCenter);
            text.color = new Color(0.12f, 0.12f, 0.10f);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.text = label;

            return button;
        }

        private static void RefreshInventory(PlayerInventory inventory)
        {
            FarmNotificationCenter.SetInventory(
                inventory.CommonSeeds,
                inventory.MineralSeeds,
                inventory.MagicSeeds,
                inventory.Wood,
                inventory.Stone,
                inventory.Fruit,
                inventory.Coins,
                SeedSprite,
                MineralSeedSprite,
                MagicSeedSprite,
                WoodSprite,
                StoneSprite,
                FruitSprite,
                CoinSprite);
        }

        private static void RefreshSurvival(PlayerSurvivalStats survivalStats)
        {
            FarmNotificationCenter.SetSurvival(
                survivalStats.CurrentHealth,
                survivalStats.MaxHealth,
                survivalStats.HungerPercent);
        }

        private static Sprite UiPanelSprite
        {
            get
            {
                if (uiPanelSprite == null)
                {
                    uiPanelSprite = LoadBackpackIcon("Panel") ??
                        LoadPackageSprite("Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/UI/dialogue box.png", "dialogue box_0") ??
                        SquareSprite;
                }

                return uiPanelSprite;
            }
        }

        private static Sprite UiButtonSprite
        {
            get
            {
                if (uiButtonSprite == null)
                {
                    uiButtonSprite = LoadBackpackIcon("Panel") ??
                        LoadPackageSprite("Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/UI/button.png", "button_0") ??
                        SquareSprite;
                }

                return uiButtonSprite;
            }
        }

        private static Sprite UiSlotSprite
        {
            get
            {
                if (uiSlotSprite == null)
                {
                    uiSlotSprite = LoadBackpackIcon("Slot") ??
                        LoadPackageSprite("Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/UI/Inventory/Slots.png", "Slots_0") ??
                        SquareSprite;
                }

                return uiSlotSprite;
            }
        }

        private static Sprite UiBarSprite
        {
            get
            {
                if (uiBarSprite == null)
                {
                    uiBarSprite = LoadBackpackIcon("HungerFrame") ??
                        LoadPackageSprite("Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/UI/Bars.png", "Bars_0") ??
                        SquareSprite;
                }

                return uiBarSprite;
            }
        }

        private static Sprite UiHudSprite
        {
            get
            {
                if (uiHudSprite == null)
                {
                    uiHudSprite = UiPanelSprite;
                }

                return uiHudSprite;
            }
        }

        private static void ApplyPanelSprite(Image image)
        {
            ApplyUiSprite(image, UiPanelSprite);
        }

        private static void ApplyInventoryPanelSprite(Image image)
        {
            ApplyUiSprite(image, UiPanelSprite);
        }

        private static void ApplyButtonSprite(Image image)
        {
            ApplyUiSprite(image, UiButtonSprite);
        }

        private static void ApplySlotSprite(Image image)
        {
            ApplyUiSprite(image, UiSlotSprite);
        }

        private static void ApplyBarSprite(Image image)
        {
            ApplyUiSprite(image, UiBarSprite);
        }

        private static void ApplyHudSprite(Image image)
        {
            ApplyUiSprite(image, UiHudSprite);
        }

        private static void ApplyUiSprite(Image image, Sprite sprite)
        {
            image.sprite = sprite;
            Vector4 border = sprite.border;
            image.type = border.x + border.y + border.z + border.w > 0f ? Image.Type.Sliced : Image.Type.Simple;
        }

        private static Sprite LoadBackpackIcon(string iconName)
        {
            return Resources.Load<Sprite>($"BackpackIcons/{iconName}");
        }

        private static Sprite LoadPackageSprite(string assetPath, string spriteName = null)
        {
#if UNITY_EDITOR
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            Sprite firstSprite = null;
            Sprite firstSubSprite = null;

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    if (!string.IsNullOrEmpty(spriteName) && sprite.name == spriteName)
                    {
                        return sprite;
                    }

                    if (firstSprite == null)
                    {
                        firstSprite = sprite;
                    }

                    if (firstSubSprite == null && IsTextureSubSprite(sprite))
                    {
                        firstSubSprite = sprite;
                    }
                }
            }

            return firstSubSprite != null ? firstSubSprite : firstSprite;
#else
            return null;
#endif
        }

        private static bool IsTextureSubSprite(Sprite sprite)
        {
            Texture2D texture = sprite.texture;
            Rect rect = sprite.rect;
            return texture != null && (rect.width < texture.width || rect.height < texture.height);
        }

        private static Sprite SquareSprite
        {
            get
            {
                if (squareSprite == null)
                {
                    squareSprite = CreateSprite("Runtime Square", 8, (x, y) => Color.white);
                }

                return squareSprite;
            }
        }

        private static Sprite CircleSprite
        {
            get
            {
                if (circleSprite == null)
                {
                    circleSprite = CreateSprite("Runtime Circle", 32, (x, y) =>
                    {
                        Vector2 center = new Vector2(15.5f, 15.5f);
                        float distance = Vector2.Distance(new Vector2(x, y), center);
                        return distance <= 15.5f ? Color.white : Color.clear;
                    });
                }

                return circleSprite;
            }
        }

        private static Sprite SeedSprite
        {
            get
            {
                if (seedSprite == null)
                {
                    seedSprite = LoadBackpackIcon("CommonSeeds") ?? CreateSprite("Seed Icon", 48, (x, y) =>
                    {
                        bool seedA = Vector2.Distance(new Vector2(x, y), new Vector2(17f, 18f)) <= 6f;
                        bool seedB = Vector2.Distance(new Vector2(x, y), new Vector2(29f, 27f)) <= 6f;
                        bool seedC = Vector2.Distance(new Vector2(x, y), new Vector2(22f, 33f)) <= 5f;
                        return seedA || seedB || seedC ? new Color(0.76f, 0.52f, 0.22f) : Color.clear;
                    });
                }

                return seedSprite;
            }
        }

        private static Sprite MineralSeedSprite
        {
            get
            {
                if (mineralSeedSprite == null)
                {
                    mineralSeedSprite = LoadBackpackIcon("MineralSeeds") ?? CreateSprite("Mineral Seed Icon", 48, (x, y) =>
                    {
                        Vector2 point = new Vector2(x, y);
                        bool seed = Vector2.Distance(point, new Vector2(24f, 24f)) <= 12f;
                        bool shine = Mathf.Abs(x - 24) <= 2 || Mathf.Abs(y - 24) <= 2;
                        return seed ? (shine ? new Color(0.86f, 0.95f, 1f) : new Color(0.42f, 0.62f, 0.72f)) : Color.clear;
                    });
                }

                return mineralSeedSprite;
            }
        }

        private static Sprite MagicSeedSprite
        {
            get
            {
                if (magicSeedSprite == null)
                {
                    magicSeedSprite = LoadBackpackIcon("MagicSeeds") ?? CreateSprite("Magic Seed Icon", 48, (x, y) =>
                    {
                        Vector2 point = new Vector2(x, y);
                        bool core = Vector2.Distance(point, new Vector2(24f, 24f)) <= 10f;
                        bool aura = Mathf.Abs(Vector2.Distance(point, new Vector2(24f, 24f)) - 15f) <= 2f;
                        if (core) return new Color(0.76f, 0.36f, 1f);
                        return aura ? new Color(1f, 0.76f, 0.28f, 0.9f) : Color.clear;
                    });
                }

                return magicSeedSprite;
            }
        }

        private static Sprite WoodSprite
        {
            get
            {
                if (woodSprite == null)
                {
                    woodSprite = LoadBackpackIcon("Wood") ?? CreateSprite("Wood Icon", 48, (x, y) =>
                    {
                        bool log = x >= 11 && x <= 37 && y >= 17 && y <= 30;
                        bool ring = Vector2.Distance(new Vector2(x, y), new Vector2(13f, 23f)) <= 7f;
                        return log || ring ? new Color(0.55f, 0.30f, 0.13f) : Color.clear;
                    });
                }

                return woodSprite;
            }
        }

        private static Sprite StoneSprite
        {
            get
            {
                if (stoneSprite == null)
                {
                    stoneSprite = LoadBackpackIcon("Stone") ?? CreateSprite("Stone Icon", 48, (x, y) =>
                    {
                        Vector2 point = new Vector2(x, y);
                        bool body = Vector2.Distance(point, new Vector2(24f, 23f)) <= 15f;
                        bool chip = x > 32 && y > 28;
                        return body && !chip ? new Color(0.52f, 0.54f, 0.55f) : Color.clear;
                    });
                }

                return stoneSprite;
            }
        }

        private static Sprite FruitSprite
        {
            get
            {
                if (fruitSprite == null)
                {
                    fruitSprite = LoadBackpackIcon("Fruit") ?? CreateSprite("Fruit Icon", 48, (x, y) =>
                    {
                        Vector2 point = new Vector2(x, y);
                        bool fruit = Vector2.Distance(point, new Vector2(24f, 21f)) <= 13f;
                        bool leaf = Vector2.Distance(point, new Vector2(30f, 35f)) <= 6f && x >= 27;
                        if (leaf)
                        {
                            return new Color(0.28f, 0.70f, 0.25f);
                        }

                        return fruit ? new Color(0.92f, 0.16f, 0.12f) : Color.clear;
                    });
                }

                return fruitSprite;
            }
        }

        private static Sprite CoinSprite
        {
            get
            {
                if (coinSprite == null)
                {
                    coinSprite = LoadBackpackIcon("Coin") ?? CreateSprite("Coin Icon", 48, (x, y) =>
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(24f, 24f));
                        if (distance <= 15f)
                        {
                            return distance <= 10f ? new Color(1f, 0.77f, 0.20f) : new Color(0.88f, 0.55f, 0.08f);
                        }

                        return Color.clear;
                    });
                }

                return coinSprite;
            }
        }

        private static Sprite HeartSprite
        {
            get
            {
                if (heartSprite == null)
                {
                    heartSprite = LoadBackpackIcon("Heart") ?? CreateSprite("Heart Icon", 48, (x, y) =>
                    {
                        Vector2 point = new Vector2(x, y);
                        bool leftLobe = Vector2.Distance(point, new Vector2(16f, 31f)) <= 10f;
                        bool rightLobe = Vector2.Distance(point, new Vector2(32f, 31f)) <= 10f;
                        bool body = y <= 31 && Mathf.Abs(x - 24f) <= (31f - y) * 0.78f + 3f && y >= 8;
                        return leftLobe || rightLobe || body ? Color.white : Color.clear;
                    });
                }

                return heartSprite;
            }
        }

        public static Sprite GetToolSprite(FarmTool tool)
        {
            if (toolSprites == null)
            {
                toolSprites = new Dictionary<FarmTool, Sprite>();
            }

            if (!toolSprites.TryGetValue(tool, out Sprite sprite))
            {
                sprite = CreateToolSprite(tool);
                toolSprites.Add(tool, sprite);
            }

            return sprite;
        }

        private static Sprite CreateToolSprite(FarmTool tool)
        {
            Sprite packageIcon = LoadBackpackIcon(tool.ToString());
            if (packageIcon != null)
            {
                return packageIcon;
            }

            return CreateSprite($"{tool} Icon", 48, (x, y) =>
            {
                switch (tool)
                {
                    case FarmTool.Sword:
                        if (x >= 22 && x <= 25 && y >= 9 && y <= 36) return new Color(0.82f, 0.88f, 0.92f);
                        if (x >= 15 && x <= 32 && y >= 12 && y <= 15) return new Color(0.45f, 0.28f, 0.16f);
                        if (x >= 21 && x <= 26 && y >= 2 && y <= 12) return new Color(0.35f, 0.18f, 0.10f);
                        break;
                    case FarmTool.Bow:
                        if (Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(18f, 24f)) - 16f) <= 2f && x <= 24) return new Color(0.55f, 0.31f, 0.14f);
                        if (x >= 31 && x <= 33 && y >= 9 && y <= 39) return new Color(0.86f, 0.84f, 0.72f);
                        if (Mathf.Abs(y - 24) <= 1 && x >= 15 && x <= 38) return new Color(0.76f, 0.76f, 0.68f);
                        break;
                    case FarmTool.Axe:
                        if (x >= 20 && x <= 24 && y >= 6 && y <= 39) return new Color(0.50f, 0.27f, 0.14f);
                        if (x >= 22 && x <= 38 && y >= 29 && y <= 39) return new Color(0.76f, 0.80f, 0.82f);
                        if (x >= 32 && x <= 40 && y >= 24 && y <= 32) return new Color(0.76f, 0.80f, 0.82f);
                        break;
                    case FarmTool.Pickaxe:
                        if (Mathf.Abs(x - y + 1) <= 2 && y >= 8 && y <= 38) return new Color(0.50f, 0.27f, 0.14f);
                        if (x >= 10 && x <= 38 && y >= 35 && y <= 39) return new Color(0.72f, 0.75f, 0.76f);
                        if (x >= 8 && x <= 16 && y >= 31 && y <= 36) return new Color(0.72f, 0.75f, 0.76f);
                        if (x >= 34 && x <= 41 && y >= 31 && y <= 36) return new Color(0.72f, 0.75f, 0.76f);
                        break;
                    case FarmTool.Hoe:
                        if (Mathf.Abs(x - y + 5) <= 2 && y >= 7 && y <= 38) return new Color(0.50f, 0.27f, 0.14f);
                        if (x >= 24 && x <= 41 && y >= 36 && y <= 39) return new Color(0.76f, 0.80f, 0.82f);
                        break;
                    case FarmTool.Shovel:
                        if (x >= 22 && x <= 25 && y >= 15 && y <= 42) return new Color(0.50f, 0.27f, 0.14f);
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(24f, 11f)) <= 10f && y <= 16) return new Color(0.66f, 0.70f, 0.72f);
                        break;
                    case FarmTool.WateringCan:
                        if (x >= 12 && x <= 32 && y >= 13 && y <= 28) return new Color(0.34f, 0.56f, 0.78f);
                        if (Vector2.Distance(new Vector2(x, y), new Vector2(34f, 25f)) <= 8f && x > 31) return new Color(0.34f, 0.56f, 0.78f);
                        if (x >= 32 && x <= 43 && y >= 28 && y <= 31) return new Color(0.34f, 0.56f, 0.78f);
                        if (x >= 7 && x <= 13 && y >= 22 && y <= 25) return new Color(0.34f, 0.56f, 0.78f);
                        break;
                }

                return Color.clear;
            });
        }

        private static Sprite CreateSprite(string name, int size, System.Func<int, int, Color> pixelColor)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.filterMode = FilterMode.Point;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, pixelColor(x, y));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
