using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Tests
{
    public sealed class CombatFeelTarget : MonoBehaviour, IDamageable
    {
        public Transform Transform => transform;
        public bool IsAlive => isActiveAndEnabled;
        public int SpawnGeneration { get; set; }
        public int Damage { get; private set; }
        public Action Hit;
        public void TakeDamage(int amount, PlayerInventory source) { Damage += amount; Hit?.Invoke(); }
    }

    public sealed class CombatFeelTests
    {
        private GameObject root;
        private PlayerInventory inventory;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("CombatFeel tests");
            inventory = Child("Player").AddComponent<PlayerInventory>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var arrow in Object.FindObjectsByType<ArrowProjectile>(FindObjectsSortMode.None))
                if (Get<PlayerInventory>(arrow, "source") == inventory) Object.DestroyImmediate(arrow.gameObject);
            Object.DestroyImmediate(root);
        }

        [TestCase("IronSword", 6, 11)]
        [TestCase("RubySword", 8, 11)]
        [TestCase("HunterBow", 4, 13)]
        [TestCase("DiamondBow", 4, 15)]
        public void ActiveWeaponKeepsSharedBonusesWithoutBorrowingOtherWeapon(string weapon, int sword, int bow)
        {
            inventory.EquippedEquipment = new[] { "", "", "", weapon, "", "Gem", "DiamondAmulet", "FireElement" };
            var combat = inventory.gameObject.AddComponent<PlayerCombatController>();
            Assert.That(combat.GetAttackDamage(FarmTool.Sword), Is.EqualTo(sword));
            Assert.That(combat.GetAttackDamage(FarmTool.Bow), Is.EqualTo(bow));
            Assert.That(inventory.EquipmentDamage, Is.EqualTo(EquipmentItems.Find(weapon).DamageBonus + 3));
            Assert.That(combat.ProgressionDamage, Is.EqualTo(sword - 1));
        }

        [Test]
        public void LegacySwordProgressionStaysExclusiveToSword()
        {
            inventory.EquippedEquipment[3] = "HunterBow";
            // PlayerInventory.Awake already installs the progression component used by combat.
            var adventure = inventory.GetComponent<AdventureProgress>() ?? inventory.gameObject.AddComponent<AdventureProgress>();
            adventure.Restore(new AdventureData { temperedBlade = true });
            var crafting = inventory.GetComponent<PlayerCraftingController>() ?? inventory.gameObject.AddComponent<PlayerCraftingController>();
            crafting.Restore(savedStorageLevel: 0, savedCampLevel: 0, savedWeapon: 3);
            var combat = inventory.gameObject.AddComponent<PlayerCombatController>();
            Assert.That(inventory.GetComponents<AdventureProgress>(), Has.Length.EqualTo(1));
            Assert.That(adventure.Data.temperedBlade, Is.True);
            Assert.That(crafting.WeaponLevel, Is.EqualTo(3));
            Assert.That(combat.ProgressionDamage, Is.EqualTo(4));
            Assert.That(combat.GetAttackDamage(FarmTool.Sword), Is.EqualTo(5));
            Assert.That(combat.GetAttackDamage(FarmTool.Bow), Is.EqualTo(10));
        }

        [Test]
        public void EmptyAndUnknownEquipmentRemainSafe()
        {
            inventory.EquippedEquipment = new[] { null, "missing", "Gem" };
            Assert.That(EquipmentItems.DamageBonusFor(inventory, FarmTool.Bow), Is.EqualTo(1));
            inventory.EquippedEquipment = null;
            Assert.That(EquipmentItems.DamageBonusFor(inventory, FarmTool.Sword), Is.Zero);
            Assert.That(EquipmentItems.DamageBonusFor(null, FarmTool.Bow), Is.Zero);
        }

        [Test]
        public void SwordSweepHitsFacingArcOnlyAndDeduplicatesColliders()
        {
            var combat = inventory.gameObject.AddComponent<PlayerCombatController>();
            var front = Target(new Vector3(.8f, 0));
            var behind = Target(new Vector3(-.8f, 0));
            var distant = Target(new Vector3(1.8f, 0));
            front.gameObject.AddComponent<CircleCollider2D>().isTrigger = true;
            Physics2D.SyncTransforms();
            combat.AttackTarget(front);
            combat.AttackTarget(front);
            Assert.That(front.Damage, Is.EqualTo(1));
            Assert.That(behind.Damage, Is.Zero);
            Assert.That(distant.Damage, Is.Zero);
            var sweep = inventory.GetComponent<CombatFeelRangeCue>();
            Assert.That(sweep.Radius, Is.EqualTo(1.25f).Within(.001f));
            Assert.That(sweep.IsShowing, Is.True);
            Assert.That(sweep.SweepVisual.sprite.texture, Is.SameAs(CombatFxLibrary.Atlas));
        }

        [Test]
        public void ArrowSpritesAreTheSameExistingAssetAcrossShots()
        {
            inventory.AddEquipment("Bow"); inventory.AddItem("Arrow", 2);
            inventory.gameObject.AddComponent<PlayerToolbelt>().Select(FarmTool.Bow);
            var combat = inventory.gameObject.AddComponent<PlayerCombatController>();
            var target = Target(Vector3.right * 3f);
            Physics2D.SyncTransforms();
            combat.TryShootBowAt(target.Transform.position);
            Set(combat, "nextAttackTime", Time.time - 1f);
            combat.TryShootBowAt(target.Transform.position);
            Sprite sprite = CombatFeelVisuals.Arrow;
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.name, Is.EqualTo("Arrow_17"));
            int count = 0;
            foreach (var arrow in Object.FindObjectsByType<ArrowProjectile>(FindObjectsSortMode.None))
            {
                if (Get<PlayerInventory>(arrow, "source") != inventory) continue;
                count++;
                Assert.That(arrow.GetComponent<SpriteRenderer>().sprite, Is.SameAs(sprite));
                arrow.transform.SetParent(root.transform);
            }
            Assert.That(count, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BowCanFireWithoutACameraAndDoesNotNeedToAcquireATarget(bool blocked)
        {
            Assert.That(Camera.main, Is.Null, "This fixture exercises targeting without a camera.");
            inventory.AddEquipment("Bow"); inventory.AddItem("Arrow", 1);
            inventory.gameObject.AddComponent<PlayerToolbelt>().Select(FarmTool.Bow);
            var combat = inventory.gameObject.AddComponent<PlayerCombatController>();
            var animalObject = Child("Animal");
            animalObject.transform.position = Vector3.right * 3f;
            animalObject.AddComponent<CircleCollider2D>().isTrigger = true;
            animalObject.AddComponent<AnimalResource>();
            if (blocked)
            {
                var wall = Child("Cover");
                wall.transform.position = Vector3.right * 1.5f;
                wall.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 2f);
            }
            Physics2D.SyncTransforms();
            combat.TryShootBowAt(Vector3.right * 3);
            int count = 0;
            foreach (var arrow in Object.FindObjectsByType<ArrowProjectile>(FindObjectsSortMode.None))
            {
                if (Get<PlayerInventory>(arrow, "source") != inventory) continue;
                count++;
                arrow.transform.SetParent(root.transform);
            }
            Assert.That(count, Is.EqualTo(1), "Cover resolves in flight; a fired arrow is still consumed.");
            Assert.AreEqual(0, inventory.GetItemCount("Arrow"));
        }

        [Test]
        public void ArrowImpactIsReservedBeforeReentrantDamageCallbacks()
        {
            var target = Target(Vector3.right * .2f);
            var arrow = Arrow(target);
            target.Hit = () => Invoke(arrow, "Update");
            Physics2D.SyncTransforms();
            Invoke(arrow, "Update");
            Invoke(arrow, "Update");
            Assert.That(target.Damage, Is.EqualTo(1));
        }

        [Test]
        public void CoverAddedAfterReleaseBlocksArrowImpact()
        {
            var target = Target(Vector3.right * .2f);
            var arrow = Arrow(target);
            var wall = Child("Cover");
            wall.transform.position = Vector3.right * .1f;
            wall.AddComponent<BoxCollider2D>().size = new Vector2(.025f, 1f);
            Physics2D.SyncTransforms();
            Invoke(arrow, "Update");
            Assert.That(target.Damage, Is.Zero);
        }

        [Test]
        public void ReusedAndDestroyedArrowTargetsCannotTakeDamage()
        {
            var target = Target(Vector3.right * .2f);
            var arrow = Arrow(target);
            target.SpawnGeneration++;
            Invoke(arrow, "Update");
            Assert.That(target.Damage, Is.Zero);
            Object.DestroyImmediate(target.gameObject);
            Assert.DoesNotThrow(() => Invoke(arrow, "Update"));
        }

        [UnityTest]
        public IEnumerator BowReleaseWaitsForAnimationAndCanBeCancelled()
        {
            inventory.AddEquipment("Bow"); inventory.AddItem("Arrow", 2);
            var animator = Animator();
            inventory.gameObject.AddComponent<PlayerToolbelt>().Select(FarmTool.Bow);
            var combat = inventory.gameObject.AddComponent<PlayerCombatController>();
            var target = Target(Vector3.right * 3f);
            Physics2D.SyncTransforms();
            combat.TryShootBowAt(target.Transform.position);
            Assert.That(Get<Coroutine>(combat, "bowRelease"), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<ArrowProjectile>(), Is.Null);
            animator.CancelAction();
            yield return new WaitForSeconds(.35f);
            Assert.That(target.Damage, Is.Zero);
            Assert.That(Object.FindFirstObjectByType<ArrowProjectile>(), Is.Null);
            Set(combat, "nextAttackTime", Time.time - 1f);
            combat.TryShootBowAt(target.Transform.position);
            yield return new WaitForSeconds(.85f);
            Assert.That(target.Damage, Is.EqualTo(8));
            Assert.AreEqual(1, inventory.GetItemCount("Arrow"), "An interrupted draw must not consume an arrow.");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TimedGatheringCancelsOutsideReachAndOnDisable(bool leaveReach)
        {
            var animator = Animator();
            var tree = Tree();
            tree.Interact(FarmTool.Axe, inventory);
            Assert.That(tree.IsGathering, Is.True);
            if (leaveReach)
            {
                inventory.transform.position = Vector3.right * 4f;
                InvokeGatherUpdate(tree);
            }
            else tree.gameObject.SetActive(false);
            Assert.That(tree.IsGathering, Is.False);
            Assert.That(animator.MovementLocked, Is.False);
            Assert.That(inventory.Wood, Is.Zero);
            Assert.That(inventory.Coins, Is.Zero);
            Assert.That(tree.IsHarvested, Is.False);
        }

        [Test]
        public void DeathCancelsBeforeSameFrameReviveCanResumeHarvest()
        {
            var stats = inventory.gameObject.AddComponent<PlayerSurvivalStats>();
            Animator();
            var tree = Tree();
            tree.Interact(FarmTool.Axe, inventory);
            stats.Restore(0, 1f);
            stats.Revive();
            Assert.That(tree.IsGathering, Is.False);
            Assert.That(inventory.Wood, Is.Zero);
        }

        [Test]
        public void CancellingGatheringDoesNotReplaceDamageAnimation()
        {
            var animator = Animator();
            var tree = Tree();
            tree.Interact(FarmTool.Axe, inventory);
            animator.PlayNamedAction("Damage");
            InvokeGatherUpdate(tree);
            Assert.That(tree.IsGathering, Is.False);
            Assert.That(animator.CurrentClip, Is.EqualTo("Damage"));
            Assert.That(inventory.Wood, Is.Zero);
        }

        [Test]
        public void HarvestHitsDoNotPayUntilCompletionAndCompletionPaysOnce()
        {
            var animator = Animator();
            var tree = Tree();
            int depleted = 0;
            tree.Depleted += _ => depleted++;
            tree.Interact(FarmTool.Axe, inventory);
            var flags = Private;
            typeof(HarvestableResource).GetField("nextGatherHitAt", flags).SetValue(tree, Time.time);
            InvokeGatherUpdate(tree);
            Assert.That(inventory.Wood, Is.Zero);
            Assert.That(tree.CurrentHealth, Is.EqualTo(1));
            Assert.That(GetGather<int>(tree, "completedGatherHits"), Is.EqualTo(1));
            typeof(HarvestableResource).GetField("gatherEndsAt", flags).SetValue(tree, Time.time - .01f);
            InvokeGatherUpdate(tree);
            tree.Interact(FarmTool.Axe, inventory);
            Assert.That(inventory.Wood, Is.EqualTo(2));
            Assert.That(inventory.Coins, Is.EqualTo(4));
            Assert.That(depleted, Is.EqualTo(1));
            Assert.That(animator.MovementLocked, Is.False);
        }

        [Test]
        public void ToolUpgradeShortensGatheringWhileImpactsFollowClipCycles()
        {
            var animator = Animator();
            var tree = Tree();
            tree.Interact(FarmTool.Axe, inventory);
            float before = GetGather<float>(tree, "activeGatherDuration");
            tree.CancelGathering();
            var upgrades = inventory.gameObject.AddComponent<PlayerToolUpgradeController>();
            Set(upgrades, "axeLevel", 3);
            tree.Interact(FarmTool.Axe, inventory);
            Assert.That(GetGather<float>(tree, "activeGatherDuration"), Is.LessThan(before));
            var clip = animator.Library.Find("Axe");
            Assert.That(GetGather<float>(tree, "gatherHitInterval"), Is.EqualTo(clip.Frames / clip.FramesPerSecond));
            Assert.That(GetGather<int>(tree, "activeGatherHitCount"), Is.GreaterThan(1));
            tree.Restore(false);
            Assert.That(tree.IsGathering, Is.False);
            Assert.That(animator.MovementLocked, Is.False);
        }

        [Test]
        public void BossRetainsAirborneImmunityAndFullDoubleDamageWindow()
        {
            var enemy = Enemy(true);
            Invoke(enemy, "SetPhase", 2, .55f);
            enemy.TakeDamage(3, inventory);
            Assert.That(enemy.Health, Is.EqualTo(36));
            Invoke(enemy, "SetPhase", 3, 2.5f);
            float deadline = Get<float>(enemy, "until");
            enemy.TakeDamage(3, inventory);
            Assert.That(enemy.Health, Is.EqualTo(30));
            Assert.That(enemy.Vulnerable, Is.True);
            Assert.That(Get<float>(enemy, "until"), Is.EqualTo(deadline));
            Assert.That(enemy.PhaseRemaining, Is.EqualTo(2.5f).Within(.01f));
        }

        [Test]
        public void BudHealingStillOccursOnlyOncePerAttackCycle()
        {
            var enemy = Enemy(true);
            enemy.TakeDamage(6, inventory);
            Invoke(enemy, "SetPhase", 0, -1f);
            Invoke(enemy, "Update");
            Assert.That(enemy.Health, Is.EqualTo(32));
            Invoke(enemy, "Update");
            Assert.That(enemy.Health, Is.EqualTo(32));
            Assert.That(Get<int>(enemy, "state"), Is.EqualTo(1));
        }

        [TestCase(true, 1.7f)]
        [TestCase(false, 1.1f)]
        public void WarningMatchesImpactRadiusAndResetClearsLeapPose(bool boss, float radius)
        {
            var enemy = Enemy(boss);
            Set(enemy, "engaged", true);
            Set(enemy, "landing", enemy.transform.position);
            Invoke(enemy, "SetPhase", 1, 1.3f);
            Invoke(enemy, "RefreshPresentation", true);
            var ring = Get<LineRenderer>(enemy, "warning");
            Assert.That(ring.enabled, Is.True);
            for (int i = 0; i < ring.positionCount; i++)
                Assert.That(Vector2.Distance(ring.GetPosition(i), enemy.transform.position), Is.EqualTo(radius).Within(.001f));
            var sprite = enemy.GetComponentInChildren<SpriteRenderer>();
            sprite.transform.localPosition = Vector3.up * 2f;
            int generation = enemy.SpawnGeneration;
            enemy.ResetEncounter();
            Assert.That(sprite.transform.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(enemy.SpawnGeneration, Is.EqualTo(generation + 1));
            Assert.That(ring.enabled, Is.False);
        }

        private GameObject Child(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform);
            return go;
        }

        private CombatFeelTarget Target(Vector3 position)
        {
            var go = Child("Target");
            go.transform.position = position;
            go.AddComponent<CircleCollider2D>().isTrigger = true;
            return go.AddComponent<CombatFeelTarget>();
        }

        private ArrowProjectile Arrow(IDamageable target)
        {
            var arrow = Child("Arrow").AddComponent<ArrowProjectile>();
            arrow.Configure(target, 1, inventory);
            return arrow;
        }

        private PlayerCharacterAnimator Animator()
        {
            inventory.gameObject.AddComponent<SpriteRenderer>();
            var animator = inventory.gameObject.AddComponent<PlayerCharacterAnimator>();
            Assert.That(animator.Library, Is.Not.Null);
            return animator;
        }

        private TreeResource Tree()
        {
            var go = Child("Tree");
            go.transform.position = Vector3.right;
            var visual = go.AddComponent<SpriteRenderer>();
            go.AddComponent<CircleCollider2D>();
            var tree = go.AddComponent<TreeResource>();
            tree.Configure(visual, null, 2, 4);
            tree.ConfigureHealth(1);
            return tree;
        }

        private ValleyEnemy Enemy(bool boss)
        {
            // An inactive campaign gives this fixture data without constructing the world in Awake.
            var campaignObject = Child("Campaign data");
            campaignObject.SetActive(false);
            var campaign = campaignObject.AddComponent<ValleyCampaign>();
            Set(campaign, "<Inventory>k__BackingField", inventory);
            campaignObject.transform.position = ValleyWorld.Center(5) + Vector3.right;
            var map = Child("World").AddComponent<ValleyWorld>();
            var go = Child("Enemy");
            go.transform.SetParent(map.transform);
            go.transform.position = ValleyWorld.Center(5);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            visual.AddComponent<SpriteRenderer>();
            go.AddComponent<CircleCollider2D>().isTrigger = true;
            var enemy = go.AddComponent<ValleyEnemy>();
            enemy.Configure(campaign, map, 5, false, boss);
            return enemy;
        }

        private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
        private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
        private static void Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
        private static T GetGather<T>(HarvestableResource target, string field) => (T)typeof(HarvestableResource).GetField(field, Private).GetValue(target);
        private static void InvokeGatherUpdate(HarvestableResource resource) => typeof(HarvestableResource).GetMethod("UpdateGathering", Private).Invoke(resource, null);
    }
}
