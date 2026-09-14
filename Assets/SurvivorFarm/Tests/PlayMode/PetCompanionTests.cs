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
    public sealed class PetCompanionTests
    {
        private GameObject root;
        private PlayerInventory inventory;
        private PlayerPetController equipment;
        private PetCompanion cat;
        private PetDefinition definition;

        private GameObject Child(string name)
        {
            var result = new GameObject(name);
            result.transform.SetParent(root.transform);
            return result;
        }

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Pet Test");
            inventory = Child("Player").AddComponent<PlayerInventory>();
            inventory.gameObject.AddComponent<PlayerSurvivalStats>();
            equipment = inventory.gameObject.AddComponent<PlayerPetController>();
            cat = Child("Cat").AddComponent<PetCompanion>();
            var renderer = Child("Visual").AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(cat.transform);
            definition = ScriptableObject.CreateInstance<PetDefinition>();
            cat.ConfigureDefinition(definition, renderer);
            equipment.Configure(null, cat, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void EquipmentBonusDoesNotStackAndReusesCat()
        {
            var combat = inventory.gameObject.AddComponent<PlayerCombatController>();
            Assert.AreEqual(1, combat.PetDamageBonus);
            equipment.SetEquipped(true);
            equipment.SetEquipped(true);
            Assert.AreEqual(1, combat.PetDamageBonus);
            equipment.SetEquipped(false);
            Assert.AreEqual(0, combat.PetDamageBonus);
            Assert.IsFalse(cat.gameObject.activeSelf);
            equipment.SetEquipped(true);
            Assert.AreSame(cat, equipment.Companion);
        }

        [UnityTest]
        public IEnumerator CatFollowsAndRecallsAfterPortalTeleport()
        {
            inventory.transform.position = Vector3.right * 3f;
            yield return null;
            yield return null;
            Assert.Greater(cat.transform.position.x, 0f);
            Assert.Less(cat.transform.position.x, 3f);
            inventory.transform.position = new Vector3(40f, 30f, 0f);
            yield return null;
            Assert.Less(Vector3.Distance(cat.transform.position, inventory.transform.position), 1f);
        }

        [UnityTest]
        public IEnumerator PetHitsEnemiesWithCooldownButNotThroughWalls()
        {
            var enemyObject = Child("Enemy");
            enemyObject.transform.position = Vector3.right * 0.8f;
            enemyObject.AddComponent<CircleCollider2D>().radius = 0.1f;
            var enemy = enemyObject.AddComponent<BasicEnemyAI>();
            enemy.ActivateFromPool(enemyObject.transform.position);
            var wall = Child("Wall");
            wall.transform.position = Vector3.right * 0.4f;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.1f, 1f);
            Physics2D.SyncTransforms();
            yield return null;
            yield return null;
            var health = typeof(EnemyAIBase).GetField("currentHealth", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreEqual(3, health.GetValue(enemy));
            wall.SetActive(false);
            Physics2D.SyncTransforms();
            yield return null;
            yield return null;
            Assert.AreEqual(2, health.GetValue(enemy));
            equipment.SetEquipped(false);
            equipment.SetEquipped(true);
            yield return null;
            Assert.AreEqual(2, health.GetValue(enemy));
        }

        [Test]
        public void SaveRoundTripKeepsPetUnequipped()
        {
            var save = Child("Save").AddComponent<GameSaveSystem>();
            save.Configure(inventory.transform, inventory, null, null, null, null, null, null, null, null);
            equipment.SetEquipped(false);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var data = typeof(GameSaveSystem).GetMethod("BuildSaveData", flags).Invoke(save, null);
            data = JsonUtility.FromJson(JsonUtility.ToJson(data), data.GetType());
            equipment.SetEquipped(true);
            typeof(GameSaveSystem).GetMethod("RestoreSaveData", flags).Invoke(save, new[] { data });
            Assert.IsFalse(equipment.Equipped);
            Assert.AreEqual(0, equipment.DamageBonus);
        }
    }
}
