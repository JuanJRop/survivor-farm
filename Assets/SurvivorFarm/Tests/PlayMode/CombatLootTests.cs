using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class CombatLootTests
    {
        private GameObject root;
        private PlayerInventory inventory;
        private BasicEnemyAI enemy;
        private EnemyLootPickup template;
        private Transform world;
        private GameObject Child(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(root.transform);
            return child;
        }
        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Combat Loot Test");
            inventory = Child("Player").AddComponent<PlayerInventory>();
            world = Child("World").transform;
            world.gameObject.AddComponent<OutdoorEnemyPool>();
            var lootObject = Child("Template");
            lootObject.SetActive(false);
            lootObject.AddComponent<CircleCollider2D>().isTrigger = true;
            template = lootObject.AddComponent<EnemyLootPickup>();
            template.Configure(ResourceFlyweights.Item(ItemKind.Coins), 1, null);
            var enemyObject = Child("Enemy");
            enemyObject.transform.SetParent(world);
            enemyObject.AddComponent<CircleCollider2D>().radius = 0.2f;
            var renderer = enemyObject.AddComponent<SpriteRenderer>();
            enemy = enemyObject.AddComponent<BasicEnemyAI>();
            enemy.Configure(inventory.transform, null);
            enemy.ConfigureVisuals(renderer, null, Color.red, "Test");
            enemy.ConfigureLoot(template);
            enemy.ActivateFromPool(Vector3.right);
            Physics2D.SyncTransforms();
        }
        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [UnityTest]
        public IEnumerator DefeatDropsCoinsAndPickupPaysOnlyOnce()
        {
            enemy.TakeDamage(10, inventory);
            enemy.TakeDamage(10, inventory);
            var drops = world.GetComponentsInChildren<EnemyLootPickup>();
            Assert.AreEqual(1, drops.Length);
            Assert.AreEqual(0, inventory.Coins);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(drops[0].TryCollect(inventory));
            Assert.IsFalse(drops[0].TryCollect(inventory));
            Assert.AreEqual(2, inventory.Coins);
        }

        [UnityTest]
        public IEnumerator HitFlashesAndKnockbackCannotCrossWall()
        {
            var wall = Child("Wall");
            wall.transform.position = Vector3.right * 1.6f;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.1f, 2f);
            Physics2D.SyncTransforms();
            enemy.TakeDamage(1, inventory);
            Assert.Greater(enemy.GetComponent<SpriteRenderer>().color.g, 0.5f);
            yield return new WaitForSeconds(0.12f);
            Assert.Greater(enemy.transform.position.x, 1f);
            Assert.Less(enemy.transform.position.x, 1.3f);
            enemy.ReturnToPool();
            enemy.ActivateFromPool(Vector3.right);
            var remaining = typeof(EnemyAIBase).GetField("knockbackRemaining", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreEqual(0f, remaining.GetValue(enemy));
        }

        [Test]
        public void ReturningToPoolWithoutDefeatDropsNothing()
        {
            enemy.ReturnToPool();
            Assert.AreEqual(0, world.GetComponentsInChildren<EnemyLootPickup>().Length);
        }

        [Test]
        public void UncollectedLootSurvivesSaveRoundTrip()
        {
            enemy.TakeDamage(10, inventory);
            var save = Child("Save").AddComponent<GameSaveSystem>();
            save.Configure(inventory.transform, inventory, null, null, null, null, null, null, null, null);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var data = typeof(GameSaveSystem).GetMethod("BuildSaveData", flags).Invoke(save, null);
            data = JsonUtility.FromJson(JsonUtility.ToJson(data), data.GetType());
            typeof(GameSaveSystem).GetMethod("RestoreSaveData", flags).Invoke(save, new[] { data });
            var restored = world.GetComponentsInChildren<EnemyLootPickup>();
            Assert.AreEqual(1, restored.Length);
            Assert.AreEqual(2, restored[0].Amount);
            Assert.AreEqual(0, inventory.Coins);
        }

        [Test]
        public void HiddenDungeonLootKeepsItsRegionForSaving()
        {
            var dungeon = Child("Dungeon").AddComponent<DungeonEnemyPool>();
            var drop = EnemyLootPickup.Spawn(template, Vector3.zero, dungeon.transform, ResourceFlyweights.Item(ItemKind.Coins), 2);
            dungeon.gameObject.SetActive(false);
            Assert.IsTrue(drop.IsUncollected);
            Assert.IsTrue(drop.IsDungeon);
        }
    }
}
