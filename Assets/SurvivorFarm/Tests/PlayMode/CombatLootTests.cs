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
        public IEnumerator DefeatDropsOneVisibleRewardAndPickupPaysOnlyAfterLandingOnce()
        {
            enemy.TakeDamage(10, inventory);
            enemy.TakeDamage(10, inventory);
            var drops = world.GetComponentsInChildren<EnemyLootPickup>();
            Assert.AreEqual(1, drops.Length);
            Assert.AreEqual(0, inventory.Coins);
            Assert.IsFalse(drops[0].TryCollect(inventory));
            Assert.NotNull(drops[0].GetComponentInChildren<SpriteRenderer>().sprite);
            Assert.Greater(Vector2.Distance(drops[0].LandingPosition, enemy.transform.position), .8f);
            inventory.transform.position = Vector3.right * 20;
            yield return new WaitForSeconds(0.7f);
            Assert.IsTrue(drops[0].TryCollect(inventory));
            Assert.IsFalse(drops[0].TryCollect(inventory));
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
            var initial = EnemyLootPickup.Scatter(Vector3.right * 2, world, ItemKind.Ruby, 3);
            Vector3 position = initial.LandingPosition;
            var save = Child("Save").AddComponent<GameSaveSystem>();
            save.Configure(inventory.transform, inventory, null, null, null, null, null, null, null, null);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var data = typeof(GameSaveSystem).GetMethod("BuildSaveData", flags).Invoke(save, null);
            data = JsonUtility.FromJson(JsonUtility.ToJson(data), data.GetType());
            typeof(GameSaveSystem).GetMethod("RestoreSaveData", flags).Invoke(save, new[] { data });
            var restored = world.GetComponentsInChildren<EnemyLootPickup>();
            Assert.AreEqual(1, restored.Length);
            Assert.AreEqual(3, restored[0].Amount);
            Assert.AreEqual(ItemKind.Ruby, restored[0].Item.Kind);
            Assert.AreEqual(position, restored[0].transform.position);
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

        [Test]
        public void LootRollsVaryByArchetypeAndIncludeGemsExperienceAndGold()
        {
            Assert.AreEqual(ItemKind.Emerald, EnemyLootTable.Roll(EnemyCombatStyle.Legacy, false, .01f));
            Assert.AreEqual(ItemKind.Experience, EnemyLootTable.Roll(EnemyCombatStyle.Legacy, false, .5f));
            Assert.AreEqual(ItemKind.Coins, EnemyLootTable.Roll(EnemyCombatStyle.Soldier, false, .9f));
            Assert.AreEqual(ItemKind.Ruby, EnemyLootTable.Roll(EnemyCombatStyle.Demon, false, .12f));
            Assert.AreEqual(ItemKind.Diamond, EnemyLootTable.Roll(EnemyCombatStyle.Orc, true, .01f));
        }

        [UnityTest]
        public IEnumerator ChestScattersDifferentIconsWithoutGrantingUntilCollected()
        {
            var chest = Child("Chest").AddComponent<DungeonChest>();
            chest.ConfigureExpedition(null, 12, 2, 1, 1, 3);
            Assert.IsFalse(chest.GetComponent<BoxCollider2D>().isTrigger);
            chest.Interact(FarmTool.Sword, inventory);
            Assert.AreEqual(0, inventory.Coins);
            Assert.AreEqual(0, inventory.GetItemCount("Ruby"));
            var drops = root.GetComponentsInChildren<EnemyLootPickup>();
            Assert.AreEqual(5, drops.Length);
            chest.Interact(FarmTool.Sword, inventory);
            Assert.AreEqual(5, root.GetComponentsInChildren<EnemyLootPickup>().Length);
            inventory.transform.position = Vector3.right * 20;
            yield return new WaitForSeconds(.7f);
            foreach (var drop in drops) Assert.IsTrue(drop.TryCollect(inventory));
            Assert.AreEqual(12, inventory.Coins);
            Assert.AreEqual(1, inventory.GetItemCount("Ruby"));
            Assert.AreEqual(2, inventory.GetComponent<AdventureProgress>().Data.iron);
        }

        [UnityTest]
        public IEnumerator BrokenCrateDropsExperienceThatAdvancesCombatMastery()
        {
            var crateObject = Child("Crate"); crateObject.transform.position = Vector3.up * 3;
            crateObject.AddComponent<BoxCollider2D>(); crateObject.AddComponent<SpriteRenderer>();
            var crate = crateObject.AddComponent<DungeonDestructible>(); crate.Configure(false);
            crate.TakeDamage(10, inventory); crate.TakeDamage(10, inventory);
            var drops = root.GetComponentsInChildren<EnemyLootPickup>();
            Assert.AreEqual(1, drops.Length); Assert.AreEqual(ItemKind.Experience, drops[0].Item.Kind);
            int amount = drops[0].Amount;
            inventory.transform.position = Vector3.right * 20;
            yield return new WaitForSeconds(.7f);
            Assert.IsTrue(drops[0].TryCollect(inventory));
            Assert.AreEqual(amount, inventory.GetComponent<ToolMastery>().Uses(FarmTool.Sword, 1));
        }
    }
}
