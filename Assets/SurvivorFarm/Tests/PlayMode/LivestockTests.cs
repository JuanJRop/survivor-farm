using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class LivestockTests
    {
        private GameObject root;
        private PlayerInventory player;
        private PlayerMountController rider;
        private ValleyWorld world;

        [SetUp] public void Setup()
        {
            Time.timeScale = 1;
            root = new GameObject("Livestock regression");
            var playerObject = new GameObject("Player", typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer));
            playerObject.transform.SetParent(root.transform);
            playerObject.GetComponent<Rigidbody2D>().gravityScale = 0;
            var feet = playerObject.GetComponent<CircleCollider2D>(); feet.radius = .24f; feet.offset = Vector2.down * .4f;
            player = playerObject.AddComponent<PlayerInventory>();
            var worldObject = new GameObject("Pasture"); worldObject.transform.SetParent(root.transform);
            world = worldObject.AddComponent<ValleyWorld>();
            LivestockWorld.Configure(world, player);
            rider = player.GetComponent<PlayerMountController>();
            foreach (var roam in world.GetComponentsInChildren<AnimalRoamingVisual>()) roam.enabled = false;
        }
        [TearDown] public void Teardown() { Time.timeScale = 1; Object.DestroyImmediate(root); }

        [Test] public void PastureUsesOriginalArtAndStableCowSaveIdentities()
        {
            var cows = world.GetComponentsInChildren<AnimalResource>().Where(a => a.IsCow).ToArray();
            Assert.AreEqual(4, cows.Length);
            Assert.AreEqual(2, world.GetComponentsInChildren<HorseMount>().Length);
            Assert.AreEqual(4, cows.Select(c => c.GetComponentInParent<ResourceSpawnPoint>().PersistentId).Distinct().Count());
            foreach (var cow in cows)
            {
                Assert.AreEqual("PackCowBrown", cow.GetComponentInChildren<SpriteRenderer>().sprite.texture.name);
                Assert.IsFalse(cow.GetComponent<Collider2D>().isTrigger);
            }
            foreach (var horse in world.GetComponentsInChildren<HorseMount>())
            {
                Assert.AreEqual("PackHorseIdle", horse.Visual.sprite.texture.name);
                horse.ShowRider(Vector2.left, true);
                Assert.AreEqual("PackRiderRun", horse.Visual.sprite.texture.name);
            }
        }

        [Test] public void SaddleIsRequiredReusableAndMountingRestoresThePlayerOnDismount()
        {
            var horse = world.GetComponentInChildren<HorseMount>();
            PlacePlayer(horse.transform.position + new Vector3(-.8f, .4f));
            Assert.IsFalse(rider.TryMount(horse));
            player.AddItem("Saddle");
            Assert.IsTrue(rider.TryMount(horse));
            Assert.Greater(rider.SpeedMultiplier, 1);
            Assert.IsFalse(player.GetComponent<SpriteRenderer>().enabled);
            Assert.IsFalse(horse.GetComponent<Collider2D>().enabled);
            Assert.AreEqual("PackRiderIdle", horse.Visual.sprite.texture.name);
            Assert.IsTrue(rider.TryDismount());
            Assert.IsFalse(rider.IsMounted);
            Assert.AreEqual(1, player.GetItemCount("Saddle"));
            Assert.IsTrue(player.GetComponent<SpriteRenderer>().enabled);
            Assert.IsTrue(horse.GetComponent<Collider2D>().enabled);
            Assert.That(Vector2.Distance(player.transform.position, horse.transform.position), Is.GreaterThan(.7f));
        }

        [Test] public void FencePreventsMountingThroughItAndDungeonTravelLeavesHorseOutside()
        {
            var horse = world.GetComponentInChildren<HorseMount>();
            PlacePlayer(horse.transform.position + new Vector3(-1.2f, .4f)); player.AddItem("Saddle");
            var fence = new GameObject("Fence", typeof(BoxCollider2D)); fence.transform.SetParent(root.transform);
            fence.transform.position = horse.transform.position + new Vector3(-.6f, .3f);
            fence.GetComponent<BoxCollider2D>().size = new Vector2(.12f, 2f);
            Physics2D.SyncTransforms(); Assert.IsFalse(rider.TryMount(horse));
            Object.DestroyImmediate(fence); Physics2D.SyncTransforms();
            Assert.IsTrue(rider.TryMount(horse));
            Vector3 horsePosition = horse.transform.position;
            PlacePlayer(new Vector3(0, -220));
            typeof(PlayerMountController).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(rider, null);
            Assert.IsFalse(rider.IsMounted);
            Assert.That(Vector3.Distance(horse.transform.position, horsePosition), Is.LessThan(.01f));
            Assert.AreEqual(-220, player.transform.position.y);
            Assert.IsTrue(player.GetComponent<SpriteRenderer>().enabled);
        }

        [UnityTest] public IEnumerator CowHitAnimatesAndDeathScattersLeatherOnlyOnce()
        {
            var cow = world.GetComponentsInChildren<AnimalResource>().First(a => a.IsCow);
            var visual = cow.GetComponentInChildren<SpriteRenderer>();
            Vector3 originalScale = visual.transform.localScale;
            cow.TakeDamage(1, player);
            Assert.That(visual.color.g, Is.LessThan(.95f), "An accepted hit must be visible immediately.");
            yield return null;
            Assert.IsTrue(cow.IsReactingToHit);
            Assert.That(visual.color.g, Is.LessThan(.95f));
            Assert.AreNotEqual(originalScale, visual.transform.localScale);
            yield return new WaitForSeconds(.1f);
            Assert.That(visual.color.g, Is.LessThan(.95f), "Resource updates must not overwrite the animal flash.");
            yield return new WaitForSeconds(.25f);
            Assert.IsFalse(cow.IsReactingToHit);
            Assert.AreEqual(Color.white, visual.color);
            Assert.AreEqual(originalScale, visual.transform.localScale);
            cow.TakeDamage(100, player); cow.TakeDamage(100, player);
            Assert.IsFalse(cow.IsAlive);
            Assert.AreEqual(1, root.GetComponentsInChildren<AnimalDeathVisual>().Length);
            var drops = root.GetComponentsInChildren<EnemyLootPickup>();
            var leather = drops.Single(d => d.Item.Kind == ItemKind.Leather);
            Assert.AreEqual(4, leather.Amount);
            Assert.AreEqual(0, player.GetItemCount("Leather"), "Leather must be collected from the ground.");
            yield return new WaitForSeconds(.65f);
            Assert.IsTrue(leather.TryCollect(player));
            Assert.AreEqual(4, player.GetItemCount("Leather"));
            Assert.AreEqual(0, root.GetComponentsInChildren<AnimalDeathVisual>().Length);
            var point = cow.GetComponentInParent<ResourceSpawnPoint>();
            point.Restore(true, 0); Assert.IsFalse(cow.IsAlive);
        }

        private void PlacePlayer(Vector3 position)
        {
            player.transform.position = position; player.GetComponent<Rigidbody2D>().position = position;
            Physics2D.SyncTransforms();
        }
    }
}
