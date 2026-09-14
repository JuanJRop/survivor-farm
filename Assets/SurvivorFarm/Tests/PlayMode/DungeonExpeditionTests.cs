using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class DungeonExpeditionTests
    {
        private GameObject root;
        private PlayerInventory player;
        private PlayerSurvivalStats stats;
        private DungeonEntrance entrance;
        private DungeonExpedition dungeon;
        private GameObject Child(string name) { var go = new GameObject(name); go.transform.SetParent(root.transform, false); return go; }
        [SetUp] public void SetUp()
        {
            root = new GameObject("Dungeon expedition tests");
            player = Child("Player").AddComponent<PlayerInventory>(); player.gameObject.AddComponent<AdventureProgress>();
            player.gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
            stats = player.gameObject.AddComponent<PlayerSurvivalStats>(); stats.GetComponent<PlayerRespawnController>().enabled = false;
            var template = Child("Template"); template.SetActive(false); template.AddComponent<CircleCollider2D>().isTrigger = true;
            var visual = template.AddComponent<SpriteRenderer>(); var enemy = template.AddComponent<BasicEnemyAI>();
            enemy.ConfigureVisuals(visual, null, Color.white, "Limo");
            var outside = Child("Outside"); var inside = Child("Dungeon"); inside.SetActive(false);
            var poolGo = new GameObject("Pool"); poolGo.transform.SetParent(inside.transform, false);
            var pool = poolGo.AddComponent<DungeonEnemyPool>(); pool.Configure(enemy, player.transform, new Transform[0], 9);
            var entry = Child("Entrance"); entrance = entry.AddComponent<DungeonEntrance>();
            var outsideSpawn = Child("Outside spawn").transform; outsideSpawn.position = Vector3.right * 3;
            var insideSpawn = new GameObject("Inside spawn").transform; insideSpawn.SetParent(inside.transform, false);
            entrance.Configure(outside, inside, player.transform, outsideSpawn, insideSpawn, null, pool);
            dungeon = entrance.Expedition;
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(root); Time.timeScale = 1; }

        [Test] public void LayoutIsConnectedLargeAndHasExplorationLoop()
        {
            HashSet<Vector2Int> Traverse(HashSet<Vector2Int> cells)
            {
                var seen = new HashSet<Vector2Int>(); var queue = new Queue<Vector2Int>(); queue.Enqueue(Vector2Int.zero); seen.Add(Vector2Int.zero);
                while (queue.Count > 0)
                {
                    var p = queue.Dequeue();
                    foreach (var step in new[] { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down })
                        if (cells.Contains(p + step) && seen.Add(p + step)) queue.Enqueue(p + step);
                }
                return seen;
            }
            Assert.AreEqual(DungeonLayout.Floor.Count, Traverse(DungeonLayout.Floor).Count);
            var blockedMainCorridor = new HashSet<Vector2Int>(DungeonLayout.Floor);
            for (int x = -2; x < 2; x++) blockedMainCorridor.Remove(new Vector2Int(x, 26));
            Assert.IsTrue(Traverse(blockedMainCorridor).Contains(new Vector2Int(0, 53)), "Western gallery must provide a second route.");
        }

        [Test] public void InteriorContainsOnlyDungeonObjectsAndBoundedEnemyPool()
        {
            Assert.AreEqual(6, DungeonLayout.Rooms.Length); Assert.Greater(DungeonLayout.Floor.Count, 900);
            Assert.AreEqual(12, dungeon.GuardCount); Assert.AreEqual(12, dungeon.Pool.PoolCount);
            Assert.AreEqual(4, dungeon.Chests.Count); Assert.AreEqual(17, dungeon.Destructibles.Count);
            Assert.IsEmpty(dungeon.GetComponentsInChildren<VillageNpcRoutine>(true));
            entrance.EnterDungeon();
            Assert.AreEqual(DungeonLayout.Entry, player.transform.position);
            Assert.IsFalse(dungeon.Pool.Enemies.Any(e => dungeon.CanEngage(e)));
            var identities = dungeon.Pool.Enemies.Select(e => e.GetInstanceID()).ToArray();
            for (int i = 0; i < 3; i++) { entrance.ExitDungeon(); entrance.EnterDungeon(); }
            CollectionAssert.AreEqual(identities, dungeon.Pool.Enemies.Select(e => e.GetInstanceID()));
        }

        [Test] public void DestructibleNeedsMultipleHitsAndGrantsLootOnlyOnce()
        {
            entrance.EnterDungeon(); var box = dungeon.Destructibles[0]; int wood = player.Wood;
            box.TakeDamage(3, player); Assert.IsTrue(box.IsAlive); Assert.AreEqual(wood, player.Wood);
            var saved = dungeon.Capture(); dungeon.Restore(saved); Assert.AreEqual(2, box.Health);
            box.TakeDamage(3, player); Assert.IsFalse(box.IsAlive); Assert.AreEqual(wood + 2, player.Wood);
            var depleted = JsonUtility.FromJson<DungeonExpeditionState>(JsonUtility.ToJson(dungeon.Capture()));
            dungeon.Restore(depleted); box.TakeDamage(99, player); Assert.AreEqual(wood + 2, player.Wood);
            Assert.IsFalse(box.GetComponent<Collider2D>().enabled);
        }

        [Test] public void GuardDeathsPersistAndOtherRoomsDoNotAggroTogether()
        {
            entrance.EnterDungeon(); var guard = dungeon.Pool.Enemies[0];
            player.transform.position = DungeonLayout.At(0, 17);
            Assert.IsTrue(dungeon.CanEngage(guard)); Assert.IsFalse(dungeon.CanEngage(dungeon.Pool.Enemies[9]));
            guard.TakeDamage(100, player); Assert.IsTrue(dungeon.IsDefeated(0));
            var state = dungeon.Capture(); entrance.ExitDungeon(); entrance.EnterDungeon();
            Assert.IsFalse(guard.IsAlive); Assert.IsTrue(dungeon.Pool.Enemies[1].IsAlive);
            dungeon.Restore(state); Assert.IsFalse(guard.IsAlive);
        }

        [Test] public void BossGateAndReliquaryRequireVictoryAndOldPositionsRecover()
        {
            entrance.EnterDungeon(); var chest = dungeon.Chests[3]; int coins = player.Coins;
            chest.Interact(FarmTool.Sword, player); Assert.AreEqual(coins, player.Coins); Assert.IsFalse(chest.IsOpened);
            Assert.IsFalse(dungeon.BeginBoss());
            Assert.AreEqual(DungeonLayout.Entry, dungeon.RecoverPosition(Vector3.zero));
            Assert.AreEqual(DungeonLayout.At(0, 41), dungeon.RecoverPosition(DungeonLayout.BossCenter));
            for (int i = 9; i < 12; i++) dungeon.Pool.Enemies[i].TakeDamage(100, player);
            Assert.IsTrue(dungeon.BossDoorReady); Assert.IsTrue(dungeon.BeginBoss()); Assert.IsFalse(dungeon.BeginBoss());
            dungeon.Boss.TakeDamage(3, player); Assert.IsTrue(dungeon.Boss.IsAlive);
            dungeon.Boss.TakeDamage(100, player); Assert.IsTrue(dungeon.State.bossDefeated); Assert.IsFalse(dungeon.BossFightActive);
            chest.Interact(FarmTool.Sword, player); Assert.AreEqual(coins + 80, player.Coins);
            Assert.AreEqual(8, player.GetComponent<AdventureProgress>().Data.iron);
            chest.Interact(FarmTool.Sword, player); Assert.AreEqual(coins + 80, player.Coins);
            var state = dungeon.Capture(); entrance.ExitDungeon(); entrance.EnterDungeon(); dungeon.Restore(state);
            Assert.IsFalse(dungeon.Boss.IsAlive); Assert.IsFalse(dungeon.BeginBoss());
        }

        [UnityTest] public IEnumerator BossAnnouncesFixedImpactAndCanBeDodged()
        {
            entrance.EnterDungeon(); for (int i = 9; i < 12; i++) dungeon.Defeated(i);
            Assert.IsTrue(dungeon.BeginBoss());
            yield return new WaitForSeconds(1.3f);
            Assert.IsTrue(dungeon.Boss.IsTelegraphing); Vector3 impact = dungeon.Boss.ImpactPosition;
            player.transform.position = DungeonLayout.At(7, 48); int hp = stats.CurrentHealth;
            yield return new WaitForSeconds(.4f);
            Assert.AreEqual(impact, dungeon.Boss.ImpactPosition);
            yield return new WaitForSeconds(1.1f);
            Assert.AreEqual(hp, stats.CurrentHealth); Assert.IsTrue(dungeon.Boss.IsExposed);
            entrance.ExitDungeon(); Assert.IsFalse(dungeon.BossFightActive); Assert.IsFalse(dungeon.Boss.IsAlive);
            entrance.EnterDungeon(); Assert.IsTrue(dungeon.BeginBoss()); Assert.AreEqual(56, dungeon.Boss.CurrentHealth);
        }

        [UnityTest] public IEnumerator BossCameraFramesActorsAndReturnsToExploration()
        {
            var go = Child("Main Camera"); go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>(); camera.orthographic = true; camera.aspect = 16f / 9;
            var follow = go.AddComponent<CameraFollowTarget>(); follow.SetTarget(player.transform);
            entrance.EnterDungeon(); for (int i = 9; i < 12; i++) dungeon.Defeated(i);
            Assert.IsTrue(dungeon.BeginBoss()); yield return new WaitForSeconds(.6f);
            var bounds = dungeon.Boss.SpriteAnimation.Visual.bounds;
            Assert.Greater(camera.WorldToViewportPoint(bounds.min).y, .08f);
            Assert.Less(camera.WorldToViewportPoint(bounds.max).y, .9f);
            Assert.Greater(camera.WorldToViewportPoint(player.transform.position).y, .1f);
            dungeon.Boss.TakeDamage(100, player); yield return null;
            Assert.AreEqual(5.6f, camera.orthographicSize, .01f);
        }
    }
}
