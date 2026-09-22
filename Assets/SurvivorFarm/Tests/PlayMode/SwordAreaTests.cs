using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class SwordAreaTests
    {
        private GameObject root;
        private PlayerCombatController combat;
        private PlayerInventory inventory;
        private readonly FieldInfo health = typeof(EnemyAIBase).GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Sword Area Test");
            inventory = root.AddComponent<PlayerInventory>();
            root.AddComponent<PlayerToolbelt>().Select(FarmTool.Sword);
            combat = root.AddComponent<PlayerCombatController>();
        }

        private BasicEnemyAI Enemy(Vector2 position)
        {
            var obj = new GameObject("Enemy");
            obj.AddComponent<CircleCollider2D>().radius = 0.15f;
            // Enemies cannot be player children: the combat excludes the player's own hierarchy.
            var enemy = obj.AddComponent<BasicEnemyAI>();
            obj.AddComponent<SpriteRenderer>();
            enemy.Configure(inventory.transform, null);
            enemy.ConfigureStats("Test", 10, 1, 1f, 0.5f, 1f, 0);
            enemy.ActivateFromPool(position);
            return enemy;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var enemy in Object.FindObjectsByType<BasicEnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(enemy.gameObject);
            foreach (var fx in Object.FindObjectsByType<ImpactBurstLifetime>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.DestroyImmediate(fx.gameObject);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void SwordHitsFacingEnemiesOnceButMissesBehindAndBeyondBladeReach()
        {
            var a = Enemy(new Vector2(0.65f, 0f));
            var b = Enemy(new Vector2(-0.7f, 0f));
            var c = Enemy(new Vector2(0f, 1.1f));
            var far = Enemy(new Vector2(1.55f, .1f));
            a.gameObject.AddComponent<CircleCollider2D>().radius = 0.2f;
            Physics2D.SyncTransforms();
            combat.AttackTarget(a);
            combat.AttackTarget(a);
            Assert.AreEqual(9, health.GetValue(a));
            Assert.AreEqual(10, health.GetValue(b), "Forward swings cannot hit directly behind the player.");
            Assert.AreEqual(9, health.GetValue(c));
            Assert.AreEqual(10, health.GetValue(far));
        }

        [Test]
        public void WallsStopAreaDamageButOtherEnemiesDoNot()
        {
            var a = Enemy(new Vector2(0.4f, 0f));
            var b = Enemy(new Vector2(1f, 0f));
            var blocked = Enemy(new Vector2(0f, 1f));
            var wall = new GameObject("Wall");
            wall.transform.SetParent(root.transform);
            // Keep obstacle outside player hierarchy so it is treated as a wall.
            wall.transform.SetParent(null);
            wall.transform.position = new Vector3(0f, 0.5f, 0f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.3f, 0.1f);
            Physics2D.SyncTransforms();
            try
            {
                combat.AttackTarget(a);
                Assert.AreEqual(9, health.GetValue(a));
                Assert.AreEqual(9, health.GetValue(b));
                Assert.AreEqual(10, health.GetValue(blocked));
            }
            finally { Object.DestroyImmediate(wall); }
        }

        [UnityTest]
        public IEnumerator HitPlaysAuthoredImpactAndPreservesMaterialThroughPooling()
        {
            var enemy = Enemy(Vector2.right);
            var renderer = enemy.GetComponent<SpriteRenderer>();
            var normal = renderer.sharedMaterial;
            enemy.TakeDamage(1, inventory);
            Assert.AreSame(normal, renderer.sharedMaterial);
            var effects = Object.FindObjectsByType<ImpactBurstLifetime>(FindObjectsSortMode.None);
            Assert.That(effects.Length, Is.GreaterThan(0));
            Assert.That(effects[0].GetComponent<SpriteRenderer>().sprite.texture, Is.SameAs(CombatFxLibrary.Atlas));
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(Object.FindObjectsByType<ImpactBurstLifetime>(FindObjectsSortMode.None).Length, Is.Zero);
            Assert.AreSame(normal, renderer.sharedMaterial);
            enemy.TakeDamage(1, inventory);
            enemy.ReturnToPool();
            Assert.AreSame(normal, renderer.sharedMaterial);
        }
    }
}
