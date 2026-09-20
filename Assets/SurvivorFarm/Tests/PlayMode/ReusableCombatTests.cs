using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class ReusableCombatTarget : MonoBehaviour, IDamageable
    {
        public Transform Transform => transform;
        public bool IsAlive => isActiveAndEnabled;
        public int SpawnGeneration { get; set; }
        public int Damage { get; private set; }
        public void TakeDamage(int amount, PlayerInventory source) => Damage += amount;
    }

    public sealed class ReusableCombatTests
    {
        private GameObject root;
        private PlayerInventory inventory;
        private PlayerCombatController combat;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [SetUp] public void SetUp()
        {
            Time.timeScale = 1; GameFeelFeedback.Enabled = true;
            root = new GameObject("Reusable combat fixture");
            inventory = Child("Player").AddComponent<PlayerInventory>();
            inventory.AddEquipment("Bow"); inventory.AddItem("Arrow", 100);
            inventory.gameObject.AddComponent<PlayerToolbelt>().Select(FarmTool.Bow);
            combat = inventory.gameObject.AddComponent<PlayerCombatController>();
        }
        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(root);
            foreach (var pool in Object.FindObjectsByType<CombatImpactPool>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(pool.gameObject);
            Time.timeScale = 1;
        }
        private GameObject Child(string name) { var go = new GameObject(name); go.transform.SetParent(root.transform, false); return go; }
        private void Ready() => typeof(PlayerCombatController).GetField("nextAttackTime", Private).SetValue(combat, Time.time - 1);
        private ReusableCombatTarget Target(Vector2 at)
        {
            var go = Child("Receiver"); go.transform.position = at;
            go.AddComponent<CircleCollider2D>().radius = .025f;
            return go.AddComponent<ReusableCombatTarget>();
        }

        [Test] public void RepeatedShotsReuseTheSameComponentAndClearFlightState()
        {
            Assert.IsTrue(combat.TryShootBowAt(Vector3.right * 8));
            var pool = root.GetComponentInChildren<PlayerProjectilePool>();
            var arrow = pool.GetComponentInChildren<ArrowProjectile>();
            Assert.AreEqual(4, pool.CreatedCount);
            for (int i = 0; i < 64; i++)
            {
                arrow.ReturnToPool(); arrow.ReturnToPool();
                Assert.AreEqual(Vector2.zero, arrow.Direction);
                Assert.IsNull(typeof(ArrowProjectile).GetField("source", Private).GetValue(arrow));
                Ready(); Assert.IsTrue(combat.TryShootBowAt(Vector3.up * 8));
                Assert.AreSame(arrow, pool.GetComponentInChildren<ArrowProjectile>());
                Assert.AreEqual(Vector2.up, arrow.Direction);
            }
            Assert.AreEqual(4, pool.CreatedCount);
            Assert.AreEqual(1, pool.ActiveCount);
            Assert.AreEqual(35, inventory.GetItemCount("Arrow"));
        }

        [Test] public void FullProjectileBudgetDoesNotSpendAmmoAndReturnsCapacity()
        {
            for (int i = 0; i < PlayerProjectilePool.MaximumProjectiles; i++)
            { Ready(); Assert.IsTrue(combat.TryShootBowAt(Vector3.right * 8)); }
            var pool = root.GetComponentInChildren<PlayerProjectilePool>();
            int arrowsBefore = inventory.GetItemCount("Arrow");
            Ready(); Assert.IsFalse(combat.TryShootBowAt(Vector3.right * 8));
            Assert.AreEqual(arrowsBefore, inventory.GetItemCount("Arrow"));
            Assert.AreEqual(PlayerProjectilePool.MaximumProjectiles, pool.CreatedCount);
            pool.GetComponentInChildren<ArrowProjectile>().ReturnToPool();
            Assert.IsTrue(combat.TryShootBowAt(Vector3.up * 8));
            Assert.AreEqual(arrowsBefore - 1, inventory.GetItemCount("Arrow"));
            Assert.AreEqual(PlayerProjectilePool.MaximumProjectiles, pool.ActiveCount);
        }

        [UnityTest] public IEnumerator ExpiredArrowCanBeReusedWithoutDamagingItsPreviousTarget()
        {
            var target = Target(Vector2.right * 4);
            var pool = PlayerProjectilePool.Create(inventory.transform);
            var arrow = pool.Rent();
            arrow.Configure(target, 4, inventory);
            arrow.gameObject.SetActive(true);
            arrow.ReturnToPool();
            var reused = pool.Rent(); Assert.AreSame(arrow, reused);
            reused.transform.position = Vector3.zero;
            reused.ConfigureDirection(Vector2.up, 8, inventory, .1f); reused.gameObject.SetActive(true);
            yield return new WaitForSeconds(.1f);
            Assert.AreEqual(0, target.Damage); Assert.AreEqual(0, pool.ActiveCount);
            Assert.AreEqual(4, pool.CreatedCount); Assert.IsFalse(reused.gameObject.activeSelf);
        }

        [UnityTest] public IEnumerator DisablingARegionReleasesArrowsWithoutStaleLeases()
        {
            combat.TryShootBowAt(Vector3.right * 8);
            var pool = root.GetComponentInChildren<PlayerProjectilePool>();
            var arrow = pool.GetComponentInChildren<ArrowProjectile>();
            arrow.gameObject.SetActive(false);
            yield return null;
            Assert.AreEqual(0, pool.ActiveCount);
            Ready(); Assert.IsTrue(combat.TryShootBowAt(Vector3.up * 8));
            Assert.AreEqual(4, pool.CreatedCount);
            Object.Destroy(arrow.gameObject); yield return null;
            Assert.AreEqual(0, pool.ActiveCount);
        }

        [UnityTest] public IEnumerator ReusedDefenseArrowIgnoresItsEmitterButStopsAtOtherSolidCover()
        {
            var turret = Child("Turret body"); turret.AddComponent<BoxCollider2D>().size = Vector2.one;
            var target = Target(Vector2.right * 2);
            var pool = PlayerProjectilePool.Create(turret.transform, 1, 2, 4);
            var arrow = pool.Rent(); arrow.transform.position = turret.transform.position;
            arrow.Configure(target, 2, inventory, false, turret.transform); arrow.gameObject.SetActive(true);
            Physics2D.SyncTransforms(); yield return new WaitForSeconds(.35f);
            Assert.AreEqual(2, target.Damage); Assert.AreEqual(0, pool.ActiveCount);
            var wall = Child("Cover beyond the turret"); wall.transform.position = Vector3.right;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.1f, 2);
            var reused = pool.Rent(); Assert.AreSame(arrow, reused);
            reused.transform.position = turret.transform.position;
            reused.Configure(target, 2, inventory, false, turret.transform); reused.gameObject.SetActive(true);
            Physics2D.SyncTransforms(); yield return new WaitForSeconds(.35f);
            Assert.AreEqual(2, target.Damage); Assert.AreEqual(0, pool.ActiveCount);
            Assert.AreEqual(1, pool.CreatedCount);
        }

        [Test] public void MeleeBufferGrowsForDenseGroupsAndDeduplicatesEveryReceiver()
        {
            inventory.GetComponent<PlayerToolbelt>().Select(FarmTool.Sword);
            var targets = new ReusableCombatTarget[48];
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i] = Target(new Vector2(.6f + i * .004f, .01f));
                targets[i].GetComponent<Collider2D>().isTrigger = true;
                targets[i].gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
            }
            Physics2D.SyncTransforms(); combat.AttackTarget(targets[0]);
            foreach (var target in targets) Assert.AreEqual(1, target.Damage);
        }

        [UnityTest] public IEnumerator ImpactsAreCappedThenReusedWithFreshTintAndTiming()
        {
            for (int i = 0; i < 40; i++) CombatHitParticles.Spawn(Vector3.zero, root.transform, ImpactSurface.Leaves, false, false);
            var pool = CombatImpactPool.For(root.transform);
            Assert.AreEqual(CombatHitParticles.MaximumBursts, CombatHitParticles.ActiveBursts);
            Assert.AreEqual(CombatHitParticles.MaximumBursts, pool.CreatedCount);
            var burst = root.GetComponentInChildren<ImpactBurstLifetime>();
            var original = burst.GetComponent<SpriteRenderer>();
            Assert.Less(original.color.r, 1);
            yield return new WaitForSecondsRealtime(.55f);
            Assert.AreEqual(0, pool.ActiveCount); Assert.AreEqual(0, CombatHitParticles.ActiveBursts);
            CombatHitParticles.Spawn(Vector3.right, root.transform, ImpactSurface.Stone, true, false);
            burst = root.GetComponentInChildren<ImpactBurstLifetime>();
            Assert.AreEqual(Color.white, burst.GetComponent<SpriteRenderer>().color);
            Assert.IsTrue(burst.GetComponent<CombatSpriteEffect>().IsPlaying);
            Assert.AreEqual(CombatHitParticles.MaximumBursts, pool.CreatedCount);
            yield return new WaitForSecondsRealtime(.55f);
            Assert.AreEqual(0, CombatHitParticles.ActiveBursts);
        }

        [UnityTest] public IEnumerator UnloadingSceneDestroysActiveAndIdleImpactPool()
        {
            var scene = SceneManager.CreateScene("Reusable combat teardown");
            var region = new GameObject("Temporary region"); SceneManager.MoveGameObjectToScene(region, scene);
            CombatHitParticles.Spawn(Vector3.zero, region.transform, ImpactSurface.Creature, true, true);
            var pool = CombatImpactPool.For(region.transform);
            Assert.Greater(pool.InactiveCount, 0); Assert.AreEqual(1, pool.ActiveCount);
            yield return SceneManager.UnloadSceneAsync(scene);
            Assert.IsTrue(pool == null); Assert.AreEqual(0, CombatHitParticles.ActiveBursts);
        }
    }
}
