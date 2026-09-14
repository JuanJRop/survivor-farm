using System.Collections;
using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace SurvivorFarm.Tests
{
    public sealed class PassiveProjectileTestEnemy : EnemyAIBase
    {
        protected override void TickEnemy() { }
    }

    public sealed class EnemyRosterTests
    {
        private GameObject root;
        private PlayerInventory player;
        private PlayerSurvivalStats stats;
        private EnemyProjectilePool arrows;
        private Tile tile;

        private GameObject Child(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            return go;
        }

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Enemy roster tests");
            player = Child("Player").AddComponent<PlayerInventory>();
            player.gameObject.AddComponent<CircleCollider2D>().radius = .25f;
            stats = player.gameObject.AddComponent<PlayerSurvivalStats>();
            stats.GetComponent<PlayerRespawnController>().enabled = false;
            arrows = EnemyProjectilePool.Ensure(Child("Arrows"), 4);
            player.transform.position = Vector3.right * 3;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            if (tile != null) Object.DestroyImmediate(tile);
            Time.timeScale = 1;
        }

        private EnemyAIBase Enemy(EnemyCombatStyle style, Vector3 position, bool passive = false)
        {
            var go = Child("Enemy");
            go.SetActive(false);
            go.AddComponent<CircleCollider2D>().radius = .25f;
            EnemyAIBase enemy = passive ? go.AddComponent<PassiveProjectileTestEnemy>() : go.AddComponent<BasicEnemyAI>();
            enemy.Configure(player.transform, null);
            Assert.IsTrue(EnemyRoster.Configure(enemy, style, arrows));
            enemy.ActivateFromPool(position);
            Physics2D.SyncTransforms();
            return enemy;
        }

        private GameObject Wall(Vector2 position, Vector2 size)
        {
            var wall = Child("Wall");
            wall.transform.position = position;
            wall.AddComponent<BoxCollider2D>().size = size;
            Physics2D.SyncTransforms();
            return wall;
        }

        [TestCase("SpearGoblinAnimations", 6)]
        [TestCase("ArcherGoblinAnimations", 7)]
        public void OriginalAtlasesHaveAllStatesDirectionsAndCachedFrames(string name, int attackFrames)
        {
            var library = Resources.Load<PlayerAnimationLibrary>(name);
            Assert.NotNull(library);
            CollectionAssert.AreEquivalent(new[] { "Idle", "Walk", "Attack", "Damage", "Dead" }, library.Clips.Select(c => c.Name));
            Assert.AreEqual(attackFrames, library.Find("Attack").Frames);
            foreach (var clip in library.Clips)
                for (int direction = 0; direction < 3; direction++)
                    for (int frame = 0; frame < clip.Frames; frame++)
                    {
                        var sprite = library.Frame(clip, direction, frame);
                        Assert.NotNull(sprite);
                        Assert.AreEqual(32, sprite.rect.width);
                        Assert.LessOrEqual(sprite.rect.xMax, clip.Atlas.width);
                        Assert.LessOrEqual(sprite.rect.yMax, clip.Atlas.height);
                        Assert.AreSame(sprite, library.Frame(clip, direction, frame));
                    }
        }

        [UnityTest]
        public IEnumerator MeleeChasesWithWalkAnimationAndRespectsWalls()
        {
            var enemy = Enemy(EnemyCombatStyle.SpearGoblin, Vector3.zero);
            yield return new WaitForSeconds(.15f);
            Assert.Greater(enemy.transform.position.x, .05f);
            Assert.AreEqual("Walk", enemy.SpriteAnimation.StateName);
            Wall(new Vector2(.8f, 0), new Vector2(.3f, 3));
            yield return new WaitForSeconds(.65f);
            Assert.Less(enemy.transform.position.x, .45f);
            Assert.AreEqual(5, stats.CurrentHealth);
        }

        [UnityTest]
        public IEnumerator MeleeTelegraphsAndHitsOnlyAfterWindup()
        {
            player.transform.position = Vector3.right * .8f;
            var enemy = Enemy(EnemyCombatStyle.SpearGoblin, Vector3.zero);
            yield return null;
            Assert.IsTrue(enemy.IsPreparingAttack);
            Assert.AreEqual("Attack", enemy.SpriteAnimation.StateName);
            Assert.AreEqual(5, stats.CurrentHealth);
            yield return new WaitForSeconds(enemy.AttackWindup + .1f);
            Assert.AreEqual(4, stats.CurrentHealth);
        }

        [UnityTest]
        public IEnumerator MeleeWhiffsWhenPlayerDodges()
        {
            player.transform.position = Vector3.right * .8f;
            var enemy = Enemy(EnemyCombatStyle.SpearGoblin, Vector3.zero);
            yield return null;
            player.transform.position = Vector3.left * .8f;
            yield return new WaitForSeconds(enemy.AttackWindup + .1f);
            Assert.AreEqual(5, stats.CurrentHealth, "Moving behind the committed spear strike must dodge it.");
        }

        [UnityTest]
        public IEnumerator ArcherApproachesThenHoldsDistanceAndRetreatsUpClose()
        {
            player.transform.position = Vector3.right * 7;
            var enemy = Enemy(EnemyCombatStyle.ArcherGoblin, Vector3.zero);
            yield return new WaitForSeconds(.15f);
            Assert.Greater(enemy.transform.position.x, .05f);
            enemy.ActivateFromPool(Vector3.zero);
            player.transform.position = Vector3.right * 3;
            yield return new WaitForSeconds(.15f);
            Assert.AreEqual(Vector3.zero, enemy.transform.position);
            Assert.IsTrue(enemy.IsPreparingAttack);
            enemy.ActivateFromPool(Vector3.zero);
            player.transform.position = Vector3.right;
            yield return new WaitForSeconds(.15f);
            Assert.Less(enemy.transform.position.x, -.05f);
            Assert.IsFalse(enemy.IsPreparingAttack);
        }

        [UnityTest]
        public IEnumerator CorneredArcherCanDefendItselfWithoutWalkingThroughTheWall()
        {
            player.transform.position = Vector3.right;
            Wall(new Vector2(-.4f, 0), new Vector2(.3f, 4));
            var enemy = Enemy(EnemyCombatStyle.ArcherGoblin, Vector3.zero);
            yield return new WaitForSeconds(.1f);
            Assert.Less(enemy.transform.position.sqrMagnitude, .0001f, "Only sub-pixel clearance at the wall is allowed.");
            Assert.IsTrue(enemy.IsPreparingAttack);
            yield return new WaitForSeconds(enemy.AttackWindup + .3f);
            Assert.AreEqual(4, stats.CurrentHealth);
        }

        [UnityTest]
        public IEnumerator ArcherDoesNotShootThroughWalls()
        {
            Wall(new Vector2(1.5f, 0), new Vector2(.4f, 5));
            var enemy = Enemy(EnemyCombatStyle.ArcherGoblin, Vector3.zero);
            yield return new WaitForSeconds(1.1f);
            Assert.IsFalse(enemy.IsPreparingAttack);
            Assert.AreEqual(0, arrows.ActiveCount);
            Assert.AreEqual(5, stats.CurrentHealth);
        }

        [UnityTest]
        public IEnumerator ArcherReleasesAnActualDodgeableArrow()
        {
            var enemy = Enemy(EnemyCombatStyle.ArcherGoblin, Vector3.zero);
            yield return null;
            Assert.IsTrue(enemy.IsPreparingAttack);
            player.transform.position = new Vector3(3, 1.5f);
            yield return new WaitForSeconds(enemy.AttackWindup + .1f);
            Assert.AreEqual(1, arrows.ActiveCount);
            var arrow = arrows.GetComponentInChildren<EnemyArrowProjectile>();
            Assert.AreEqual(Vector2.right, arrow.Direction);
            yield return new WaitForSeconds(.6f);
            Assert.AreEqual(5, stats.CurrentHealth);
        }

        [UnityTest]
        public IEnumerator ArcherStillReleasesItsCommittedShotWhenPlayerLeavesAttackRange()
        {
            var enemy = Enemy(EnemyCombatStyle.ArcherGoblin, Vector3.zero);
            yield return null;
            player.transform.position = new Vector3(6, 2);
            yield return new WaitForSeconds(enemy.AttackWindup + .1f);
            Assert.AreEqual(1, arrows.ActiveCount);
            var arrow = arrows.GetComponentInChildren<EnemyArrowProjectile>();
            Assert.AreEqual(Vector2.right, arrow.Direction);
            Assert.Greater(arrow.transform.position.x, 0);
            Assert.Less(Mathf.Abs(arrow.transform.position.y), .001f);
            Assert.IsNotNull(arrow.GetComponent<SpriteRenderer>().sprite);
        }

        [UnityTest]
        public IEnumerator CommittedShotHitsNewCoverAndDoesNotTurnAfterLaunch()
        {
            var enemy = Enemy(EnemyCombatStyle.ArcherGoblin, Vector3.zero);
            yield return null;
            Wall(new Vector2(2, 0), new Vector2(.3f, 3));
            yield return new WaitForSeconds(enemy.AttackWindup + .08f);
            Assert.AreEqual(1, arrows.ActiveCount);
            player.transform.position = new Vector3(3, 2);
            yield return new WaitForSeconds(.5f);
            Assert.AreEqual(0, arrows.ActiveCount); Assert.AreEqual(5, stats.CurrentHealth);
        }

        [UnityTest]
        public IEnumerator AlliedGuardsDoNotSilentlyAbsorbTheArchersArrows()
        {
            var ally = Enemy(EnemyCombatStyle.SpearGoblin, Vector3.right, true);
            var enemy = Enemy(EnemyCombatStyle.ArcherGoblin, Vector3.zero);
            yield return new WaitForSeconds(enemy.AttackWindup + .75f);
            Assert.AreEqual(4, stats.CurrentHealth);
            Assert.AreEqual(ally.MaximumHealth, ally.CurrentHealth);
        }

        [TestCase(EnemyCombatStyle.SpearGoblin, 12)]
        [TestCase(EnemyCombatStyle.ArcherGoblin, 10)]
        public void GoblinsSurviveSeveralIronSwordHits(EnemyCombatStyle style, int health)
        {
            player.AddEquipment("IronSword"); player.Equip("IronSword", 3);
            var combat = player.gameObject.AddComponent<PlayerCombatController>();
            var enemy = Enemy(style, Vector3.zero, true);
            Assert.AreEqual(health, enemy.MaximumHealth);
            int damage = combat.GetAttackDamage(FarmTool.Sword);
            enemy.TakeDamage(damage, player); enemy.TakeDamage(damage, player);
            Assert.IsTrue(enemy.IsAlive);
            Assert.AreEqual(health - damage * 2, enemy.CurrentHealth);
        }

        [UnityTest]
        public IEnumerator PooledArrowHitsOnceThenIsReusable()
        {
            var enemy = Enemy(EnemyCombatStyle.ArcherGoblin, Vector3.zero, true);
            Assert.IsTrue(arrows.Fire(enemy, player.transform, Vector2.right, 1, null));
            yield return new WaitForSeconds(.7f);
            Assert.AreEqual(4, stats.CurrentHealth);
            Assert.AreEqual(0, arrows.ActiveCount);
            Assert.IsTrue(arrows.Fire(enemy, player.transform, Vector2.right, 1, null));
            yield return new WaitForSeconds(.7f);
            Assert.AreEqual(3, stats.CurrentHealth);
            Assert.AreEqual(4, arrows.PoolCount);
        }

        [UnityTest]
        public IEnumerator SweptArrowHitsThinWallBeforePlayer()
        {
            var enemy = Enemy(EnemyCombatStyle.ArcherGoblin, Vector3.zero, true);
            Wall(new Vector2(1.5f, 0), new Vector2(.015f, 2));
            arrows.Fire(enemy, player.transform, Vector2.right, 1, null);
            yield return new WaitForSeconds(.7f);
            Assert.AreEqual(5, stats.CurrentHealth);
            Assert.AreEqual(0, arrows.ActiveCount);
        }

        [UnityTest]
        public IEnumerator ArrowsCannotCrossOrHurtInsideSafeZone()
        {
            var zone = Child("Protection").AddComponent<HomeSafeZone>();
            zone.transform.position = new Vector3(6, 0);
            var enemy = Enemy(EnemyCombatStyle.ArcherGoblin, Vector3.zero, true);
            player.transform.position = new Vector3(10, 0);
            arrows.Fire(enemy, player.transform, Vector2.right, 1, zone);
            yield return new WaitForSeconds(.8f);
            Assert.AreEqual(0, arrows.ActiveCount, "Arrows must be absorbed at the boundary even if the player is beyond it.");
            player.transform.position = zone.transform.position;
            arrows.Fire(enemy, player.transform, Vector2.right, 1, zone);
            yield return null;
            Assert.AreEqual(0, arrows.ActiveCount);
            Assert.AreEqual(5, stats.CurrentHealth);
        }

        [UnityTest]
        public IEnumerator DeathShowsAnimationDropsLootOnceAndResetsAllPoolState()
        {
            var enemy = Enemy(EnemyCombatStyle.ArcherGoblin, Vector3.zero, true);
            var loot = Child("Loot template");
            loot.SetActive(false);
            loot.AddComponent<CircleCollider2D>().isTrigger = true;
            var template = loot.AddComponent<EnemyLootPickup>();
            template.Configure(ResourceFlyweights.Item(ItemKind.Coins), 1, null);
            enemy.ConfigureLoot(template);
            arrows.Fire(enemy, player.transform, Vector2.right, 1, null);
            enemy.TakeDamage(100, player);
            enemy.TakeDamage(100, player);
            Assert.IsFalse(enemy.IsAlive);
            Assert.IsTrue(enemy.IsDying);
            Assert.IsTrue(enemy.gameObject.activeSelf);
            Assert.IsFalse(enemy.GetComponent<Collider2D>().enabled);
            Assert.AreEqual("Dead", enemy.SpriteAnimation.StateName);
            Assert.AreEqual(0, arrows.ActiveCount);
            Assert.AreEqual(1, root.GetComponentsInChildren<EnemyLootPickup>().Length);
            yield return new WaitForSeconds(.9f);
            Assert.IsFalse(enemy.gameObject.activeSelf);
            int generation = enemy.SpawnGeneration;
            enemy.ActivateFromPool(Vector3.left);
            Assert.AreEqual(generation + 1, enemy.SpawnGeneration);
            Assert.IsTrue(enemy.IsAlive);
            Assert.IsFalse(enemy.IsDying);
            Assert.IsFalse(enemy.IsPreparingAttack);
            Assert.IsTrue(enemy.GetComponent<Collider2D>().enabled);
            Assert.AreEqual("Idle", enemy.SpriteAnimation.StateName);
            Assert.AreEqual(0, enemy.SpriteAnimation.CurrentFrame);
            Assert.AreEqual(Color.white, enemy.SpriteAnimation.Visual.color);
        }

        [UnityTest]
        public IEnumerator DamageInterruptsTheShotAndPlaysHurtAnimation()
        {
            var enemy = Enemy(EnemyCombatStyle.ArcherGoblin, Vector3.zero);
            yield return null;
            Assert.IsTrue(enemy.IsPreparingAttack);
            enemy.TakeDamage(1, player);
            Assert.AreEqual("Damage", enemy.SpriteAnimation.StateName);
            Assert.IsFalse(enemy.IsPreparingAttack);
            yield return new WaitForSeconds(.4f);
            Assert.AreEqual(0, arrows.ActiveCount);
        }

        [Test]
        public void PoolHasBoundedCapacityAndCancelsOldGenerationShots()
        {
            var enemy = Enemy(EnemyCombatStyle.ArcherGoblin, Vector3.zero, true);
            for (int i = 0; i < 4; i++) Assert.IsTrue(arrows.Fire(enemy, player.transform, Vector2.right, 1, null));
            Assert.IsFalse(arrows.Fire(enemy, player.transform, Vector2.right, 1, null));
            enemy.ReturnToPool();
            Assert.AreEqual(0, arrows.ActiveCount);
            enemy.ActivateFromPool(Vector3.zero);
            Assert.IsTrue(arrows.Fire(enemy, player.transform, Vector2.up, 1, null));
            Assert.AreEqual(Vector2.up, arrows.GetComponentInChildren<EnemyArrowProjectile>().Direction);
            arrows.enabled = false;
            Assert.AreEqual(0, arrows.ActiveCount);
            Assert.AreEqual(4, arrows.PoolCount);
        }

        [UnityTest]
        public IEnumerator PlayerDeathCancelsProjectiles()
        {
            var enemy = Enemy(EnemyCombatStyle.ArcherGoblin, Vector3.zero, true);
            yield return null;
            arrows.Fire(enemy, player.transform, Vector2.right, 1, null);
            stats.TakeDamage(100);
            yield return null;
            Assert.AreEqual(0, arrows.ActiveCount);
        }

        [Test]
        public void DungeonReusesSceneInstancesAndNeverGrowsAcrossRepeatedEncounters()
        {
            var template = Enemy(EnemyCombatStyle.SpearGoblin, Vector3.zero);
            template.ReturnToPool();
            var pool = Child("Dungeon").AddComponent<DungeonEnemyPool>();
            var authored = Object.Instantiate(template, pool.transform);
            authored.ReturnToPool();
            var points = new Transform[6];
            for (int i = 0; i < points.Length; i++) { points[i] = Child("Spawn").transform; points[i].position = new Vector3(i * 2, 5); }
            pool.Configure(template, player.transform, points, 6);
            var initial = pool.GetComponentsInChildren<EnemyAIBase>(true);
            Assert.Contains(authored, initial);
            Assert.AreEqual(6, pool.PoolCount);
            Assert.IsTrue(initial.Any(e => e.CombatStyle == EnemyCombatStyle.ArcherGoblin));
            for (int i = 0; i < 10; i++) { pool.SpawnEncounter(); pool.DespawnAll(); }
            CollectionAssert.AreEquivalent(initial, pool.GetComponentsInChildren<EnemyAIBase>(true));
            Assert.AreEqual(6, pool.PoolCount);
            Assert.AreEqual(12, pool.GetComponent<EnemyProjectilePool>().PoolCount);
            Assert.AreEqual(0, pool.GetComponent<EnemyProjectilePool>().ActiveCount);
        }

        [Test]
        public void OutdoorRosterContainsBothRolesAndRotatesReusedSlots()
        {
            var template = Child("Template"); template.SetActive(false);
            template.AddComponent<CircleCollider2D>().radius = .25f;
            var prefab = template.AddComponent<OutdoorEnemyAI>();
            var terrain = Child("Terrain"); terrain.AddComponent<Grid>();
            var ground = Child("Spring Grass"); ground.transform.SetParent(terrain.transform);
            var map = ground.AddComponent<Tilemap>();
            tile = ScriptableObject.CreateInstance<Tile>();
            for (int i = 6; i < 12; i++) map.SetTile(new Vector3Int(i, 0, 0), tile);
            var zone = Child("Zone").AddComponent<HomeSafeZone>();
            var pool = Child("Outdoor Pool").AddComponent<OutdoorEnemyPool>();
            pool.Configure(prefab, player.transform, zone, terrain.transform, 5);
            pool.Initialize();
            var enemies = pool.GetComponentsInChildren<OutdoorEnemyAI>(true);
            CollectionAssert.IsSubsetOf(new[] { EnemyCombatStyle.SpearGoblin, EnemyCombatStyle.ArcherGoblin, EnemyCombatStyle.Legacy }, enemies.Select(e => e.CombatStyle));
            Assert.IsTrue(pool.SpawnOne());
            var first = enemies.Single(e => e.IsAlive);
            first.ReturnToPool();
            Physics2D.SyncTransforms();
            Assert.IsTrue(pool.SpawnOne());
            Assert.AreNotSame(first, enemies.Single(e => e.IsAlive));
            pool.Initialize();
            Assert.AreEqual(5, pool.PoolCount);
            Assert.AreEqual(10, pool.GetComponent<EnemyProjectilePool>().PoolCount);
        }
    }
}
