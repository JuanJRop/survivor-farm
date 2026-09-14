using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class OutdoorEnemyTests
    {
        private GameObject root;
        private PlayerInventory inventory;
        private HomeSafeZone zone;
        private OutdoorEnemyPool pool;
        private OutdoorEnemyAI prefab;
        private Tile groundTile;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Outdoor Test");
            var player = Child("Player");
            inventory = player.AddComponent<PlayerInventory>();
            player.AddComponent<PlayerSurvivalStats>();
            zone = Child("Zone").AddComponent<HomeSafeZone>();
            zone.Configure(root.transform, inventory, null);
            var template = Child("Template");
            template.SetActive(false);
            template.AddComponent<CircleCollider2D>();
            prefab = template.AddComponent<OutdoorEnemyAI>();
            var terrain = Child("Terrain");
            terrain.AddComponent<Grid>();
            var ground = Child("Spring Grass");
            ground.transform.SetParent(terrain.transform);
            var map = ground.AddComponent<Tilemap>();
            map.tileAnchor = Vector3.zero;
            groundTile = ScriptableObject.CreateInstance<Tile>();
            map.SetTile(new Vector3Int(6, 0, 0), groundTile);
            pool = Child("Pool").AddComponent<OutdoorEnemyPool>();
            pool.Configure(prefab, player.transform, zone, terrain.transform, 2, useGoblinRoster: false);
            pool.Initialize();
            Physics2D.SyncTransforms();
        }

        private GameObject Child(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform);
            return child;
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(root); Object.DestroyImmediate(groundTile); }

        [Test]
        public void GatheringGrowsProtectionAndSpendingDoesNotReduceIt()
        {
            float before = zone.Radius;
            inventory.AddWood(20);
            Assert.AreEqual(before, zone.Radius);
            inventory.RecordGathered(20);
            Assert.AreEqual(before + 1f, zone.Radius, 0.001f);
            inventory.TryRemoveWood(20);
            Assert.AreEqual(before + 1f, zone.Radius, 0.001f);
            inventory.RestoreGathered(10000);
            Assert.AreEqual(7f, zone.Radius);
        }

        [Test]
        public void PoolReusesInstancesAndCannotSpawnInsideProtectionOrWalls()
        {
            Assert.AreEqual(2, pool.PoolCount);
            Assert.IsFalse(pool.CanOccupy(Vector2.zero, null));
            Assert.IsTrue(pool.SpawnOne());
            var active = pool.GetComponentInChildren<OutdoorEnemyAI>();
            int generation = active.SpawnGeneration;
            active.TakeDamage(100, inventory);
            Assert.IsTrue(pool.SpawnOne());
            Assert.AreSame(active, pool.GetComponentInChildren<OutdoorEnemyAI>());
            Assert.AreEqual(generation + 1, active.SpawnGeneration);
            active.ReturnToPool();
            var wall = Child("Wall");
            wall.transform.position = Vector3.right * 6f;
            wall.AddComponent<BoxCollider2D>();
            Physics2D.SyncTransforms();
            Assert.IsFalse(pool.SpawnOne());
            Assert.AreEqual(2, pool.PoolCount);
        }

        [UnityTest]
        public IEnumerator EnemyChasesOutsideButStopsWhenPlayerIsSafe()
        {
            inventory.transform.position = Vector3.right * 4f;
            var enemy = pool.GetComponentsInChildren<OutdoorEnemyAI>(true)[0];
            enemy.ActivateFromPool(Vector3.right * 7f);
            yield return null;
            yield return null;
            Assert.Less(enemy.transform.position.x, 7f);
            inventory.transform.position = Vector3.zero;
            Vector3 position = enemy.transform.position;
            int health = inventory.GetComponent<PlayerSurvivalStats>().CurrentHealth;
            yield return null;
            yield return null;
            Assert.AreEqual(position, enemy.transform.position);
            Assert.AreEqual(health, inventory.GetComponent<PlayerSurvivalStats>().CurrentHealth);
            inventory.RecordGathered(1000);
            yield return null;
            Assert.IsFalse(enemy.gameObject.activeSelf);
        }

        [Test]
        public void HarvestCountsOnceAndSavedProgressRestoresRadius()
        {
            var rock = Child("Rock").AddComponent<RockResource>();
            rock.Configure(null, null, 2, 0);
            rock.Interact(FarmTool.Pickaxe, inventory);
            rock.Interact(FarmTool.Pickaxe, inventory);
            Assert.AreEqual(2, inventory.TotalGathered);
            inventory.RecordGathered(38);
            var save = Child("Save").AddComponent<GameSaveSystem>();
            save.Configure(inventory.transform, inventory, null, null, null, null, null, null, null, null);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var data = typeof(GameSaveSystem).GetMethod("BuildSaveData", flags).Invoke(save, null);
            data = JsonUtility.FromJson(JsonUtility.ToJson(data), data.GetType());
            inventory.RestoreGathered(0);
            typeof(GameSaveSystem).GetMethod("RestoreSaveData", flags).Invoke(save, new[] { data });
            Assert.AreEqual(40, inventory.TotalGathered);
            Assert.AreEqual(4.8f, zone.Radius, 0.001f);
        }

        [Test]
        public void NightIncreasesPopulationAndSleepingReturnsExcessToPool()
        {
            var clock = Child("Clock").AddComponent<DayNightCycle>();
            pool.Configure(prefab, inventory.transform, zone, root.transform, 5, useGoblinRoster: false);
            pool.ConfigureClock(clock);
            pool.Initialize();
            Assert.AreEqual(2, pool.ActiveLimit);
            float daySpeed = pool.SpeedMultiplier;
            float dayDelay = pool.AttackDelayMultiplier;
            float daySpawn = pool.SpawnInterval;
            clock.Restore(1, 22f);
            Assert.AreEqual(5, pool.ActiveLimit);
            Assert.Greater(pool.SpeedMultiplier, daySpeed);
            Assert.Less(pool.AttackDelayMultiplier, dayDelay);
            Assert.Less(pool.SpawnInterval, daySpawn);
            var instances = pool.GetComponentsInChildren<OutdoorEnemyAI>(true);
            for (int i = 0; i < instances.Length; i++) instances[i].ActivateFromPool(Vector3.right * (5f + i * 2f));
            pool.RefreshPopulation();
            Assert.AreEqual(5, pool.ActiveCount);
            clock.TrySleepUntilMorning();
            pool.RefreshPopulation();
            Assert.AreEqual(2, pool.ActiveCount);
            Assert.IsFalse(pool.SpawnOne());
            Assert.AreEqual(5, pool.PoolCount);
            Assert.AreEqual(0, inventory.Coins);
        }

        [UnityTest]
        public IEnumerator DistantPlayerIsIgnoredInDaylightAndPursuedAtNight()
        {
            var clock = Child("Clock").AddComponent<DayNightCycle>();
            pool.ConfigureClock(clock);
            inventory.transform.position = Vector3.right * 4f;
            var enemy = pool.GetComponentsInChildren<OutdoorEnemyAI>(true)[0];
            enemy.ActivateFromPool(Vector3.right * 9f);
            yield return null;
            yield return null;
            Assert.AreEqual(9f, enemy.transform.position.x);
            clock.Restore(1, 22f);
            yield return null;
            yield return null;
            Assert.Less(enemy.transform.position.x, 9f);
            clock.Restore(2, 8f);
            var position = enemy.transform.position;
            yield return null;
            yield return null;
            Assert.AreEqual(position, enemy.transform.position);
        }
    }
}
