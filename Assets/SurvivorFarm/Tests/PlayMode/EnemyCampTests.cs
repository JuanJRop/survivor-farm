using System.Collections;
using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace SurvivorFarm.Tests
{
    public sealed class EnemyCampTests
    {
        private GameObject root;
        private PlayerInventory player;
        private EnemyCampState state;
        private Tile tile;
        private GameObject Child(string name)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform, false); return go;
        }

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Camp tests");
            player = Child("Player").AddComponent<PlayerInventory>();
            player.gameObject.AddComponent<CircleCollider2D>().radius = .25f;
            var stats = player.gameObject.AddComponent<PlayerSurvivalStats>();
            stats.Restore(50, 50, 1);
            stats.GetComponent<PlayerRespawnController>().enabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            if (tile != null) Object.DestroyImmediate(tile);
            Time.timeScale = 1;
        }

        private EnemyCamp Camp(int index = 0)
        {
            var definition = EnemyCampDefinition.All[index];
            state = new EnemyCampState { id = definition.Id, center = definition.PreferredCenter };
            var camp = Child("Camp").AddComponent<EnemyCamp>();
            camp.Configure(definition, state, player, null, null, null);
            Physics2D.SyncTransforms();
            return camp;
        }

        [Test]
        public void FourDistinctMixedCampsLeaveVillageRoadsAndRiverClear()
        {
            Assert.AreEqual(4, EnemyCampDefinition.All.Length);
            Assert.AreEqual(4, EnemyCampDefinition.All.Select(d => d.Id).Distinct().Count());
            foreach (var definition in EnemyCampDefinition.All)
            {
                Assert.IsTrue(EnemyCampWorld.ValidRegion(definition.PreferredCenter, definition), definition.Id);
                Assert.GreaterOrEqual(definition.Roster.Distinct().Count(), 2);
                Assert.That(definition.Roster.Length, Is.InRange(3, 5));
            }
            Assert.IsFalse(EnemyCampWorld.ValidRegion(Vector2.zero, EnemyCampDefinition.All[0]));
            Assert.IsFalse(EnemyCampWorld.ValidRegion(new Vector2(float.NaN, -16), EnemyCampDefinition.All[0]));
            Assert.IsFalse(EnemyCampWorld.ValidRegion(new Vector2(18, 7), EnemyCampDefinition.All[3]));
        }

        [Test]
        public void SiteSelectionUsesLandAndAvoidsExistingObjectsWithoutMovingThem()
        {
            var grid = Child("Terrain"); grid.AddComponent<Grid>();
            var go = Child("Ground"); go.transform.SetParent(grid.transform);
            var ground = go.AddComponent<Tilemap>();
            tile = ScriptableObject.CreateInstance<Tile>();
            for (int x = -37; x <= 37; x++) for (int y = -22; y <= 22; y++) ground.SetTile(new Vector3Int(x, y), tile);
            var definition = EnemyCampDefinition.All[1];
            var wall = Child("Existing tree"); wall.transform.position = definition.PreferredCenter;
            wall.AddComponent<BoxCollider2D>().size = Vector2.one;
            Physics2D.SyncTransforms();
            Assert.IsTrue(EnemyCampWorld.TryFindSite(definition, ground, null, new[] { (Vector2)wall.transform.position }, out var chosen));
            Assert.Greater(Vector2.Distance(chosen, wall.transform.position), 4.2f);
            Assert.AreEqual(definition.PreferredCenter, wall.transform.position);
            Assert.IsTrue(ground.HasTile(ground.WorldToCell(chosen)));
            Assert.IsTrue(EnemyCampWorld.TryFindSite(definition, ground, null, new[] { (Vector2)wall.transform.position }, out var again));
            Assert.AreEqual(chosen, again);
        }

        [Test]
        public void GuardRosterAndProjectilesArePrewarmedOnlyOnce()
        {
            var camp = Camp(3);
            var members = camp.Members.ToArray();
            CollectionAssert.AreEqual(camp.Definition.Roster, members.Select(e => e.CombatStyle));
            Assert.AreEqual(5, camp.PoolCount);
            Assert.AreEqual(10, camp.Projectiles.PoolCount);
            for (int i = 0; i < 10; i++) camp.Restore(state);
            CollectionAssert.AreEqual(members, camp.Members);
            Assert.AreEqual(5, camp.PoolCount);
            Assert.AreEqual(10, camp.Projectiles.PoolCount);
        }

        [UnityTest]
        public IEnumerator GuardsPatrolWhenNearbyButNotAlerted()
        {
            var camp = Camp(1);
            player.transform.position = camp.transform.position + Vector3.down * 10;
            var enemy = camp.Members[0];
            var initial = enemy.transform.position;
            yield return new WaitForSeconds(.08f);
            Assert.AreNotEqual(initial, enemy.transform.position);
            Assert.IsFalse(camp.CanEngage);
            Assert.IsFalse(enemy.IsPreparingAttack);
            Assert.AreEqual("Walk", enemy.SpriteAnimation.StateName);
        }

        [UnityTest]
        public IEnumerator GuardsChaseIntrudersButReturnInsteadOfEnteringTown()
        {
            var camp = Camp(1);
            var enemy = camp.Members[0];
            player.transform.position = camp.transform.position + new Vector3(-2, -4);
            var home = enemy.GuardPosition;
            yield return new WaitForSeconds(.3f);
            Assert.Less(enemy.transform.position.y, home.y);
            player.transform.position = Vector3.zero;
            yield return new WaitForSeconds(.6f);
            Assert.Less(Vector2.Distance(enemy.transform.position, home), .2f);
            Assert.IsFalse(enemy.IsPreparingAttack);
            Assert.IsFalse(camp.CanOccupy(Vector2.zero));
            Assert.IsFalse(camp.CanOccupy(camp.transform.position + Vector3.left * 7));
            foreach (var guard in camp.Members) Assert.IsFalse(VillageLayout.IsVillage(guard.transform.position));
        }

        [UnityTest]
        public IEnumerator LeavingCampCancelsArrowsAndOwnerDisableAlsoCancelsThem()
        {
            var camp = Camp(1);
            var archer = camp.Members.First(e => e.CombatStyle == EnemyCombatStyle.ArcherGoblin);
            player.transform.position = camp.transform.position + new Vector3(2, -4);
            Assert.IsTrue(camp.Projectiles.Fire(archer, player.transform, Vector2.down, 1, null));
            player.transform.position = Vector3.zero;
            yield return null;
            Assert.AreEqual(0, camp.Projectiles.ActiveCount);
            player.transform.position = camp.transform.position + new Vector3(2, -4);
            camp.Restore(state);
            Assert.IsTrue(camp.Projectiles.Fire(archer, player.transform, Vector2.down, 1, null));
            camp.gameObject.SetActive(false);
            Assert.AreEqual(0, camp.Projectiles.ActiveCount);
        }

        [Test]
        public void PartialClearSurvivesSerializationAndDoesNotRespawnDefeatedGuards()
        {
            var camp = Camp(1);
            camp.Members[0].TakeDamage(100, player);
            Assert.AreEqual(3, camp.Remaining);
            Assert.AreEqual(1, state.defeatedMask);
            var save = JsonUtility.FromJson<ValleyData>(JsonUtility.ToJson(new ValleyData { enemyCamps = new System.Collections.Generic.List<EnemyCampState> { state } }));
            camp.Restore(save.enemyCamps[0]);
            Assert.IsFalse(camp.Members[0].gameObject.activeSelf);
            Assert.AreEqual(3, camp.Remaining);
            Assert.AreEqual(state.center, camp.transform.position);
            camp.Defeated(0);
            Assert.AreEqual(3, camp.Remaining);
        }

        [Test]
        public void LastGuardAutomaticallyRewardsOnceWithoutReachingTheChest()
        {
            var camp = Camp();
            Assert.IsFalse(camp.TryClaim(player));
            camp.Members[0].TakeDamage(100, player);
            Assert.AreEqual(0, player.Coins);
            Assert.IsFalse(camp.Claimed);
            foreach (var guard in camp.Members.Skip(1)) guard.TakeDamage(100, player);
            Assert.IsTrue(camp.IsCleared);
            Assert.IsTrue(camp.Claimed);
            Assert.IsFalse(camp.TryClaim(player));
            Assert.AreEqual(camp.Definition.Coins, player.Coins);
            Assert.AreEqual(camp.Definition.Food, player.Food);
            Assert.AreEqual(camp.Definition.Iron, player.GetComponent<AdventureProgress>().Data.iron);
            Assert.IsFalse(camp.Chest.IsAvailable);
            camp.Restore(JsonUtility.FromJson<EnemyCampState>(JsonUtility.ToJson(state)));
            Assert.IsFalse(camp.TryClaim(player));
            Assert.AreEqual(camp.Definition.Coins, player.Coins);
            Assert.IsTrue(camp.IsCleared);
        }

        [UnityTest]
        public IEnumerator LegacyClearedChestPaysOnceAfterRestoreButClaimedChestNeverPaysAgain()
        {
            var camp = Camp();
            camp.Restore(new EnemyCampState { id = state.id, center = state.center, defeatedMask = 7 });
            Assert.AreEqual(0, player.Coins, "Restore must finish before rewards are added.");
            yield return null;
            Assert.AreEqual(camp.Definition.Coins, player.Coins);
            Assert.IsTrue(camp.Claimed);
            camp.Restore(new EnemyCampState { id = state.id, center = state.center, defeatedMask = 7, claimed = true });
            yield return null;
            Assert.AreEqual(camp.Definition.Coins, player.Coins);
        }

        [Test]
        public void ReentrantVictoryCallbacksCannotDuplicateCampSupplies()
        {
            var camp = Camp();
            player.transform.position = camp.Chest.transform.position;
            System.Action repeat = () => { camp.Defeated(2); camp.TryClaim(player); };
            player.InventoryChanged += repeat;
            try { foreach (var guard in camp.Members) guard.TakeDamage(100, player); }
            finally { player.InventoryChanged -= repeat; }
            Assert.AreEqual(camp.Definition.Coins, player.Coins);
            Assert.AreEqual(camp.Definition.Food, player.Food);
            Assert.AreEqual(camp.Definition.Iron, player.GetComponent<AdventureProgress>().Data.iron);
        }

        [Test]
        public void LivingGuardsCannotBeCountedAsDefeatedByRepeatedCallbacks()
        {
            var camp = Camp();
            for (int i = -1; i <= 4; i++) camp.Defeated(i);
            Assert.AreEqual(3, camp.Remaining);
            Assert.AreEqual(0, player.Coins);
        }

        [UnityTest]
        public IEnumerator PendingVictorySuppliesWaitUntilPlayerRevives()
        {
            var camp = Camp();
            var stats = player.GetComponent<PlayerSurvivalStats>();
            stats.Restore(5, 0, 1);
            camp.Restore(new EnemyCampState { id = state.id, center = state.center, defeatedMask = 7 });
            yield return null;
            Assert.AreEqual(0, player.Coins); Assert.IsFalse(camp.Claimed);
            stats.Restore(5, 5, 1);
            yield return null;
            Assert.AreEqual(camp.Definition.Coins, player.Coins); Assert.IsTrue(camp.Claimed);
            yield return null;
            Assert.AreEqual(camp.Definition.Coins, player.Coins);
        }

        [Test]
        public void SlimeAndChestSpritesUseSingleUnclippedFrames()
        {
            var camp = Camp();
            foreach (var slime in camp.Members.Where(e => e.CombatStyle == EnemyCombatStyle.Legacy))
            {
                Assert.NotNull(slime.SpriteAnimation);
                Assert.AreEqual(new Vector2(16, 32), slime.SpriteAnimation.Visual.sprite.rect.size);
            }
            Assert.AreEqual(new Vector2(32, 16), camp.Chest.GetComponent<SpriteRenderer>().sprite.rect.size);
        }

        [Test]
        public void EmptyLegacyCampDataAndCorruptMasksHaveSafeDefaults()
        {
            var old = JsonUtility.FromJson<ValleyData>("{\"camp\":true}");
            Assert.IsTrue(old.camp);
            var camp = Camp();
            camp.Restore(new EnemyCampState { id = state.id, center = state.center, defeatedMask = 1 << 20 });
            Assert.AreEqual(3, camp.Remaining);
            camp.Restore(new EnemyCampState { id = state.id, center = state.center, claimed = true });
            Assert.IsTrue(camp.IsCleared);
            Assert.IsTrue(camp.Claimed);
        }
    }
}
