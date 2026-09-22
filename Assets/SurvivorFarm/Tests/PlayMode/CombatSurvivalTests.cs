using System.Collections;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class CombatSurvivalTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            root = new GameObject("Combat survival fixture");
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (root != null) Object.DestroyImmediate(root);
            foreach (LootPickupPool pool in Object.FindObjectsByType<LootPickupPool>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(pool.gameObject);
            foreach (FarmNotificationCenter center in Object.FindObjectsByType<FarmNotificationCenter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(center.gameObject);
        }

        [UnityTest]
        public IEnumerator DeathDropsCarriedResourcesAndClearsThePortableState()
        {
            GameObject player = new GameObject("Player");
            player.transform.SetParent(root.transform);
            PlayerInventory inventory = player.AddComponent<PlayerInventory>();
            PlayerSurvivalStats stats = player.AddComponent<PlayerSurvivalStats>();
            yield return null;

            inventory.AddWood(7);
            inventory.AddStone(3);
            inventory.AddItem("Arrow", 5);
            inventory.AddItem("Diamond", 2);
            stats.Restore(5, 5, 1f);
            stats.TakeDamage(999);
            yield return null;

            Assert.That(inventory.Wood, Is.Zero);
            Assert.That(inventory.Stone, Is.Zero);
            Assert.That(inventory.GetItemCount("Arrow"), Is.Zero);
            Assert.That(inventory.GetItemCount("Diamond"), Is.Zero);
            Assert.That(Object.FindObjectsByType<EnemyLootPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                Is.GreaterThanOrEqualTo(3));
            Assert.That(player.GetComponent<PlayerDeathDrops>().DroppedForCurrentLife, Is.True);
        }

        [Test]
        public void ArmorReductionAndHealthUpgradeRemainReadableToTheSurvivalLayer()
        {
            GameObject player = new GameObject("Armored player");
            player.transform.SetParent(root.transform);
            PlayerInventory inventory = player.AddComponent<PlayerInventory>();
            PlayerSurvivalStats stats = player.AddComponent<PlayerSurvivalStats>();
            inventory.AddEquipment("Chestplate");
            Assert.IsTrue(inventory.Equip("Chestplate", 1));
            Assert.That(inventory.ArmorReduction, Is.EqualTo(.25f).Within(.001f));
            stats.Restore(7, 7, 1f);
            Assert.That(stats.MaxHealth, Is.EqualTo(7));
            Assert.That(stats.CurrentHealth, Is.EqualTo(7));
        }
    }
}
