using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Tests
{
    public sealed class LootPoolingTests
    {
        private Scene scene;
        private GameObject root;
        private PlayerInventory player;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly Vector3 Origin = new Vector3(2000, 2000);
        private LootPickupPool Pool => Object.FindObjectsByType<LootPickupPool>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(p => p.gameObject.scene == scene);

        [SetUp] public void SetUp()
        {
            scene = SceneManager.CreateScene("Loot pooling " + Guid.NewGuid().ToString("N"));
            root = new GameObject("Loot pool fixture");
            SceneManager.MoveGameObjectToScene(root, scene);
            var actor = new GameObject("Player"); actor.transform.SetParent(root.transform);
            actor.transform.position = Origin + Vector3.right * 40;
            player = actor.AddComponent<PlayerInventory>(); actor.AddComponent<PlayerSurvivalStats>();
            Time.timeScale = 1;
        }

        [UnityTearDown] public IEnumerator TearDown()
        {
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest] public IEnumerator CollectedInstanceReusesVisualsResetsFlightAndGrantsEachLeaseExactlyOnce()
        {
            var first = EnemyLootPickup.Spawn(null, Origin, root.transform, ResourceFlyweights.Item(ItemKind.Arrow), 7);
            int identity = first.GetInstanceID();
            yield return new WaitForSeconds(.7f);
            Assert.IsTrue(first.TryCollect(player));
            Assert.IsFalse(first.TryCollect(player));
            Assert.AreEqual(7, player.GetItemCount("Arrow"));
            Assert.IsFalse(first.IsUncollected);
            Assert.IsNull(first.Item);
            Assert.AreEqual(0, Pool.ActiveCount);
            Assert.AreEqual(1, Pool.InactiveCount);

            var largerParent = new GameObject("Scaled region"); largerParent.transform.SetParent(root.transform);
            largerParent.transform.localScale = Vector3.one * 2;
            var second = EnemyLootPickup.Spawn(null, Origin + Vector3.up, largerParent.transform, ResourceFlyweights.Item(ItemKind.Diamond), 3);
            Assert.AreEqual(identity, second.GetInstanceID(), "Collection should reuse the actual instance and its renderers.");
            Assert.AreEqual(Origin + Vector3.up, second.LandingPosition);
            Assert.IsFalse(second.IsAttracting);
            Assert.AreEqual(3, second.Amount);
            var icon = second.transform.Find("Loot icon");
            Assert.AreEqual(Vector3.zero, icon.localPosition, "A previous bobbing offset must not accumulate across leases.");
            Assert.That(icon.GetComponent<SpriteRenderer>().bounds.size.x, Is.EqualTo(.65f).Within(.001f));
            Assert.AreEqual(2, second.GetComponentsInChildren<SpriteRenderer>().Length, "Reuse must not add extra shadows or icons.");
            Assert.IsFalse(second.TryCollect(player), "A fresh lease has its own landing delay.");
            yield return new WaitForSeconds(.7f);
            Assert.IsTrue(second.TryCollect(player));
            Assert.IsFalse(second.TryCollect(player));
            Assert.AreEqual(3, player.GetItemCount("Diamond"));
            Assert.AreEqual(7, player.GetItemCount("Arrow"));
            Assert.AreEqual(1, Pool.CreatedCount);
            Assert.AreEqual(2, Pool.RentCount);
        }

        [UnityTest] public IEnumerator RewardBurstBeyondIdleCapacityLosesNothingAndShrinksAfterCollection()
        {
            int count = LootPickupPool.RetainedPerPrefab + 17;
            var drops = new EnemyLootPickup[count];
            int expectedCoins = 0;
            for (int i = 0; i < count; i++)
            {
                int amount = i % 5 + 1;
                expectedCoins += amount;
                drops[i] = EnemyLootPickup.Spawn(null, Origin, root.transform, ResourceFlyweights.Item(ItemKind.Coins), amount);
                Assert.NotNull(drops[i], "Idle capacity cannot refuse an uncollected reward.");
            }
            Assert.AreEqual(count, Pool.ActiveCount);
            yield return new WaitForSeconds(.7f);
            foreach (var drop in drops) Assert.IsTrue(drop.TryCollect(player));
            Assert.AreEqual(expectedCoins, player.Coins);
            Assert.AreEqual(0, Pool.ActiveCount);
            Assert.AreEqual(LootPickupPool.RetainedPerPrefab, Pool.InactiveCount);
            yield return null;
            Assert.AreEqual(LootPickupPool.RetainedPerPrefab,
                Object.FindObjectsByType<EnemyLootPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(p => p.gameObject.scene == scene));
            int created = Pool.CreatedCount;
            for (int i = 0; i < LootPickupPool.RetainedPerPrefab; i++)
                EnemyLootPickup.Spawn(null, Origin, root.transform, ResourceFlyweights.Item(ItemKind.Coins), 1);
            Assert.AreEqual(created, Pool.CreatedCount, "A second equal burst should allocate no additional loot objects.");
        }

        [Test] public void HiddenEncounterLootSurvivesSaveRestoreAndReusedOutsideLeaseClearsOwnership()
        {
            var dungeon = new GameObject("Dungeon"); dungeon.transform.SetParent(root.transform);
            dungeon.AddComponent<DungeonEnemyPool>();
            var campObject = new GameObject("Camp"); campObject.transform.SetParent(dungeon.transform);
            var camp = campObject.AddComponent<EnemyCamp>();
            var definition = new EnemyCampDefinition("pool-save-encounter", "Pool test", Origin, 0, 0, 0, 0, Array.Empty<EnemyCombatStyle>());
            camp.Configure(definition, new EnemyCampState { id = definition.Id, center = Origin }, player, null, null, null, requireChestInteraction: true, buildScenery: false);
            var drop = EnemyLootPickup.Spawn(null, Origin, camp.transform, ResourceFlyweights.Item(ItemKind.Ruby), 5);
            int identity = drop.GetInstanceID();
            dungeon.SetActive(false);
            Assert.IsTrue(drop.IsUncollected);
            Assert.IsTrue(drop.IsDungeon);
            Assert.AreEqual(definition.Id, drop.EncounterId);

            var save = root.AddComponent<GameSaveSystem>();
            save.Configure(player.transform, player, null, null, null, null, null, null, null, null);
            var data = typeof(GameSaveSystem).GetMethod("BuildSaveData", PrivateInstance).Invoke(save, null);
            data = JsonUtility.FromJson(JsonUtility.ToJson(data), data.GetType());
            var savedDrops = data.GetType().GetField("groundLoot").GetValue(data);
            typeof(GameSaveSystem).GetMethod("RestoreGroundLoot", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { savedDrops });
            var restored = camp.GetComponentsInChildren<EnemyLootPickup>(true).Single(d => d.IsUncollected);
            Assert.AreEqual(identity, restored.GetInstanceID());
            Assert.AreEqual(5, restored.Amount);
            Assert.AreEqual(ItemKind.Ruby, restored.Item.Kind);
            Assert.AreEqual(definition.Id, restored.EncounterId);
            Assert.IsTrue(restored.IsDungeon);
            Assert.AreEqual(1, Pool.ActiveCount);
            restored.Discard();
            restored.Discard();
            Assert.AreEqual(1, Pool.InactiveCount, "Discarding twice must not enqueue the same lease twice.");
            var outside = EnemyLootPickup.Spawn(null, Origin, root.transform, ResourceFlyweights.Item(ItemKind.Wood), 2);
            Assert.AreEqual(identity, outside.GetInstanceID());
            Assert.IsFalse(outside.IsDungeon);
            Assert.IsNull(outside.EncounterId);
        }

        [UnityTest] public IEnumerator DestroyingEncounterRemovesOutstandingLeaseFromAccounting()
        {
            var encounter = new GameObject("Removed encounter"); encounter.transform.SetParent(root.transform);
            EnemyLootPickup.Spawn(null, Origin, encounter.transform, ResourceFlyweights.Item(ItemKind.Coins), 2);
            var pool = Pool;
            Assert.AreEqual(1, pool.ActiveCount);
            Object.Destroy(encounter);
            yield return null;
            Assert.AreEqual(0, pool.ActiveCount);
            var next = EnemyLootPickup.Spawn(null, Origin, root.transform, ResourceFlyweights.Item(ItemKind.Coins), 2);
            Assert.NotNull(next);
            Assert.AreEqual(1, pool.ActiveCount);
        }

        [UnityTest] public IEnumerator UnloadingSceneReleasesActiveIdleAndSharedNativeShadowAssets()
        {
            var first = EnemyLootPickup.Spawn(null, Origin, root.transform, ResourceFlyweights.Item(ItemKind.Coins), 1);
            var second = EnemyLootPickup.Spawn(null, Origin, root.transform, ResourceFlyweights.Item(ItemKind.Arrow), 2);
            var sprite = first.transform.Find("Loot ground shadow").GetComponent<SpriteRenderer>().sprite;
            var texture = sprite.texture;
            first.Discard();
            var pool = Pool;
            int scenePools = LootPickupPool.ScenePoolCount;
            Assert.AreEqual(1, pool.ActiveCount); Assert.AreEqual(1, pool.InactiveCount);
            yield return SceneManager.UnloadSceneAsync(scene);
            yield return null;
            Assert.IsTrue(pool == null);
            Assert.IsTrue(first == null && second == null);
            Assert.IsTrue(sprite == null && texture == null, "Generated native assets belong to the scene and must be disposed with it.");
            Assert.AreEqual(scenePools - 1, LootPickupPool.ScenePoolCount);
            scene = SceneManager.CreateScene("Loot pool reload " + Guid.NewGuid().ToString("N"));
            root = new GameObject("Reloaded fixture"); SceneManager.MoveGameObjectToScene(root, scene);
            var replacement = EnemyLootPickup.Spawn(null, Origin, root.transform, ResourceFlyweights.Item(ItemKind.Coins), 1);
            Assert.NotNull(replacement);
            Assert.AreEqual(1, Pool.CreatedCount);
            Assert.AreEqual(1, Pool.ActiveCount);
        }
    }
}
