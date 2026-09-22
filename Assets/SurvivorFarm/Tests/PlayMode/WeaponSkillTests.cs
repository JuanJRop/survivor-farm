using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class WeaponSkillTarget : EnemyAIBase { protected override void TickEnemy() { } }
    public sealed class WeaponSkillTests
    {
        private GameObject root;
        private PlayerInventory inventory;
        private PlayerToolbelt belt;
        private PlayerCombatController combat;
        private PlayerCraftingController crafting;
        private GameObject Child(string name) { var go = new GameObject(name); go.transform.SetParent(root.transform, false); return go; }
        [SetUp] public void SetUp()
        {
            Time.timeScale = 1;
            root = new GameObject("Weapon skills fixture");
            inventory = Child("Player").AddComponent<PlayerInventory>();
            inventory.OwnedEquipment = new[] { "Sword" };
            belt = inventory.gameObject.AddComponent<PlayerToolbelt>();
            crafting = inventory.GetComponent<PlayerCraftingController>();
            if (crafting == null) crafting = inventory.gameObject.AddComponent<PlayerCraftingController>();
            crafting.Restore(0, 0, savedWeapon: 1);
            combat = inventory.gameObject.AddComponent<PlayerCombatController>();
        }
        [TearDown] public void TearDown()
        {
            GameMenuWindow.Instance?.Close();
            foreach (var arrow in Object.FindObjectsByType<ArrowProjectile>(FindObjectsSortMode.None)) Object.DestroyImmediate(arrow.gameObject);
            foreach (GameObject canvas in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Where(value => value.name == "Menú de viaje")) Object.DestroyImmediate(canvas);
            Object.DestroyImmediate(root); Time.timeScale = 1;
        }
        private WeaponSkillTarget Target(Vector2 position, int health = 20)
        {
            var go = Child("Target"); go.AddComponent<CircleCollider2D>().radius = .16f;
            var visual = go.AddComponent<SpriteRenderer>();
            var target = go.AddComponent<WeaponSkillTarget>(); target.Configure(inventory.transform, null);
            target.ConfigureVisuals(visual, null, Color.white, "Target"); target.ConfigureStats("Target", health, 1, 1, .5f, 1, 0);
            target.ActivateFromPool(position); Physics2D.SyncTransforms(); return target;
        }
        private void Vulnerable(EnemyAIBase enemy) => typeof(EnemyAIBase).GetField("currentHealth", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 4);
        private void UnlockBow(int arrows) { inventory.AddEquipment("Bow"); inventory.AddItem("Arrow", arrows); belt.Select(FarmTool.Bow); }

        [Test] public void BowRequiresDiscoveryAndEveryActualShotConsumesOneArrow()
        {
            belt.Select(FarmTool.Bow); Assert.AreEqual(FarmTool.Sword, belt.SelectedTool);
            inventory.AddItem("Arrow", 2); Assert.IsFalse(combat.TryShootBowAt(Vector3.right * 8));
            inventory.AddEquipment("Bow"); belt.Select(FarmTool.Bow);
            Assert.IsTrue(combat.TryShootBowAt(Vector3.right * 8));
            Assert.AreEqual(1, inventory.GetItemCount("Arrow"));
            Assert.IsFalse(combat.TryShootBowAt(Vector3.up * 8)); Assert.AreEqual(1, inventory.GetItemCount("Arrow"));
            var arrow = Object.FindFirstObjectByType<ArrowProjectile>();
            Assert.IsTrue(arrow.IsManual); Assert.AreEqual(Vector2.right, arrow.Direction);
            Assert.Greater(combat.GetAttackDamage(FarmTool.Bow), combat.GetAttackDamage(FarmTool.Sword));
        }

        [UnityTest] public IEnumerator ManualArrowDoesNotFollowMovingTargetAndCanHitAnotherEnemyOnItsPath()
        {
            UnlockBow(2);
            var moving = Target(new Vector2(2, 0)); var inPath = Target(new Vector2(4, 0));
            Assert.IsTrue(combat.TryShootBowAt(new Vector3(8, 0)));
            moving.transform.position = new Vector3(2, 2); Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.5f);
            Assert.AreEqual(20, moving.CurrentHealth); Assert.AreEqual(12, inPath.CurrentHealth);
            Assert.AreEqual(1, inventory.GetItemCount("Arrow"));
        }

        [UnityTest] public IEnumerator ManualArrowConsumesAmmoOnMissAndSolidCoverStopsDamage()
        {
            UnlockBow(1); var target = Target(new Vector2(3, 0));
            var cover = Child("Cover"); cover.transform.position = Vector3.right * 1.5f; cover.AddComponent<BoxCollider2D>().size = new Vector2(.15f, 3);
            Physics2D.SyncTransforms(); Assert.IsTrue(combat.TryShootBowAt(Vector3.right * 10));
            Assert.AreEqual(0, inventory.GetItemCount("Arrow"));
            yield return new WaitForSeconds(.75f);
            Assert.AreEqual(20, target.CurrentHealth); Assert.IsFalse(combat.TryShootBowAt(Vector3.right * 10));
        }

        [Test] public void SwordTierOneHasNoChargeAndEachUpgradeUsesDifferentAuthoredArt()
        {
            Assert.AreEqual(1, combat.SwordTier); Assert.IsFalse(combat.BeginCharge());
            var a = PlayerWeaponPresentation.SwordSprite(1); var b = PlayerWeaponPresentation.SwordSprite(2); var c = PlayerWeaponPresentation.SwordSprite(3);
            Assert.NotNull(a); Assert.NotNull(b); Assert.NotNull(c); Assert.AreNotSame(a.texture, b.texture); Assert.AreNotSame(b.texture, c.texture);
            crafting.Restore(0, 0, savedWeapon: 2); Assert.IsTrue(combat.CanChargeSword); Assert.IsTrue(combat.BeginCharge());
            var cue = inventory.gameObject.AddComponent<CombatFeelRangeCue>(); cue.ShowCharge(Vector3.zero, .2f, Vector2.right, 2);
            Vector3 scale = cue.ChargeVisual.transform.localScale; cue.ShowCharge(Vector3.zero, 1, Vector2.right, 2);
            Assert.AreEqual(scale, cue.ChargeVisual.transform.localScale); Assert.AreEqual(FilterMode.Point, cue.ChargeVisual.sprite.texture.filterMode);
            cue.ShowCharge(Vector3.zero, 1, Vector2.right, 1); Assert.IsFalse(cue.IsCharging);
        }

        [UnityTest] public IEnumerator TierThreeChargeHitsNearbyAreaButNotTargetsOutsideTheBlast()
        {
            crafting.Restore(0, 0, savedWeapon: 3);
            var front = Target(new Vector2(1, 0)); var rear = Target(new Vector2(-1, 0)); var far = Target(new Vector2(0, 2.2f));
            Assert.IsTrue(combat.BeginCharge()); yield return new WaitForSeconds(1.15f);
            combat.ReleaseCharge(front);
            Assert.Less(front.CurrentHealth, 20); Assert.Less(rear.CurrentHealth, 20); Assert.AreEqual(20, far.CurrentHealth);
        }

        [UnityTest] public IEnumerator ExecutionUsesAShortDashAndOneReservedSwordHit()
        {
            var target = Target(new Vector2(2.5f, 0)); Vulnerable(target);
            Assert.IsTrue(combat.CanExecute(target)); Vector3 start = inventory.transform.position;
            Assert.IsTrue(combat.TryExecute(target)); Assert.IsFalse(combat.TryExecute(target));
            yield return new WaitForSeconds(.6f);
            Assert.IsFalse(target.IsAlive); Assert.IsFalse(combat.IsExecuting);
            Assert.That(Vector2.Distance(start, inventory.transform.position), Is.InRange(1.5f, 2.4f));
            var next = Target((Vector2)inventory.transform.position + Vector2.up); Vulnerable(next);
            Assert.IsFalse(combat.TryExecute(next), "The execution cooldown prevents chaining repeated dashes.");
        }

        [Test] public void ExecutionCannotStartThroughCoverOrAcrossTheMap()
        {
            var target = Target(new Vector2(8, 0)); Vulnerable(target); Assert.IsFalse(combat.CanExecute(target));
            target.transform.position = Vector3.right * 2.5f;
            var wall = Child("Wall"); wall.transform.position = Vector3.right; wall.AddComponent<BoxCollider2D>().size = new Vector2(.2f, 3);
            Physics2D.SyncTransforms(); Assert.IsFalse(combat.TryExecute(target)); Assert.AreEqual(Vector3.zero, inventory.transform.position);
        }

        [UnityTest] public IEnumerator ExecutionCancelsWhenPooledEnemyChangesGeneration()
        {
            var target = Target(new Vector2(2.5f, 0)); Vulnerable(target); Assert.IsTrue(combat.TryExecute(target));
            target.ReturnToPool(); target.ActivateFromPool(new Vector2(2.5f, 0));
            yield return new WaitForSeconds(.5f);
            Assert.AreEqual(20, target.CurrentHealth); Assert.IsFalse(combat.IsExecuting);
        }

        [Test] public void ExecutionSweepsOffsetFeetInsteadOfOnlyTheSpritePivot()
        {
            var feet = inventory.gameObject.AddComponent<CircleCollider2D>(); feet.radius = .24f; feet.offset = new Vector2(0, -.4f);
            var target = Target(new Vector2(2.5f, 0)); Vulnerable(target);
            var fence = Child("Low fence at foot height"); fence.transform.position = new Vector3(1.2f, -.5f);
            fence.AddComponent<BoxCollider2D>().size = new Vector2(.15f, .25f);
            Physics2D.SyncTransforms();
            Assert.IsFalse(combat.CanExecute(target), "The feet would intersect the fence even though a cast at the sprite pivot would miss it.");
            Assert.IsFalse(combat.TryExecute(target)); Assert.AreEqual(Vector3.zero, inventory.transform.position);
            Object.DestroyImmediate(fence); Physics2D.SyncTransforms(); Assert.IsTrue(combat.CanExecute(target));
        }

        [UnityTest] public IEnumerator ExecutionCancelsOnPauseWithoutApplyingTheFinisher()
        {
            var target = Target(new Vector2(2.5f, 0)); Vulnerable(target); Assert.IsTrue(combat.TryExecute(target));
            Time.timeScale = 0; yield return null; yield return null;
            Assert.IsFalse(combat.IsExecuting); Assert.AreEqual(4, target.CurrentHealth);
            Assert.LessOrEqual(Vector2.Distance(Vector3.zero, inventory.transform.position), 2.4f);
            Time.timeScale = 1;
        }

        [UnityTest] public IEnumerator UnifiedMenuShowsConnectedFiveBranchTreeLiveCombatPreviewAndRoutedUnlock()
        {
            GameObject eventSystemObject = Child("Menu event system");
            var eventSystem = eventSystemObject.AddComponent<EventSystem>();
            var manager = SkillTreeManager.Ensure(inventory);

            GameMenuWindow.OpenSkillsActive();
            GameMenuWindow menu = GameMenuWindow.Instance;
            Assert.IsNotNull(menu);
            yield return null;
            menu.FitToViewport();
            Canvas.ForceUpdateCanvases();
            Assert.IsTrue(GameMenuWindow.IsOpen);
            Assert.IsTrue(SkillTreeWindow.IsOpen);
            Assert.AreEqual(GameMenuWindow.Page.Skills, menu.CurrentPage);
            Assert.AreEqual(0f, Time.timeScale);

            var labels = Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Select(t => t.text).ToArray();
            CollectionAssert.Contains(labels, "FUERZA");
            CollectionAssert.Contains(labels, "MAGIA");
            CollectionAssert.Contains(labels, "SUPERVIVENCIA");
            CollectionAssert.Contains(labels, "MOVILIDAD");
            CollectionAssert.Contains(labels, "CAOS");

            SkillTreeWindow tree = inventory.GetComponent<SkillTreeWindow>();
            Assert.IsNotNull(tree);
            var treeRoot = menu.Content.Find("Árbol de habilidades · tinta y oro");
            Assert.IsNotNull(treeRoot);
            Assert.AreSame(menu.Content, treeRoot.parent);
            Assert.AreSame(menu.Content.GetComponentInParent<Canvas>(), treeRoot.GetComponentInParent<Canvas>());

            SkillDemonstrationPreview preview = treeRoot.GetComponentInChildren<SkillDemonstrationPreview>(true);
            Assert.IsNotNull(preview);
            preview.SetSkill("force_shockwave");
            Assert.IsNotNull(preview.transform.Find("Héroe en combate").GetComponent<Image>().sprite);
            Assert.IsNotNull(preview.transform.Find("Enemigo alcanzado").GetComponent<Image>().sprite);
            Assert.IsNotNull(preview.transform.Find("Efecto de la habilidad").GetComponent<Image>().sprite);

            ClickByRaycast(eventSystem, "Técnica force_bleeding_edge");
            string[] detailLabels = treeRoot.GetComponentsInChildren<Text>(true).Select(t => t.text).ToArray();
            CollectionAssert.Contains(detailLabels, "Filo sangrante");
            Assert.IsTrue(detailLabels.Any(text => text.StartsWith("SELLADA")), "Locked nodes remain selectable and explain their requirements.");
            ClickByRaycast(eventSystem, "Técnica force_shockwave");
            CollectionAssert.Contains(treeRoot.GetComponentsInChildren<Text>(true).Select(t => t.text).ToArray(), "Onda de choque");
            ClickByRaycast(eventSystem, "Técnica force_heavy_hit");
            CollectionAssert.Contains(treeRoot.GetComponentsInChildren<Text>(true).Select(t => t.text).ToArray(), "Golpe pesado");
            ClickByRaycast(eventSystem, "Aprender habilidad");
            Assert.AreEqual(SkillStatus.Maxed, manager.GetStatus("force_heavy_hit"));

            GameMenuWindow.OpenEquipmentActive();
            Assert.AreEqual(GameMenuWindow.Page.Equipment, menu.CurrentPage);
            labels = Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Select(t => t.text).ToArray();
            CollectionAssert.Contains(labels, "Cinco cajones rápidos");
            for (int slot = 1; slot <= PlayerQuickSlots.SlotCount; slot++)
                Assert.IsTrue(labels.Any(text => text.StartsWith(slot + "   ")));

            menu.Close();
            Assert.IsFalse(GameMenuWindow.IsOpen);
            Assert.IsFalse(SkillTreeWindow.IsOpen);
            Assert.AreEqual(1f, Time.timeScale);
        }

        private void ClickByRaycast(EventSystem eventSystem, string objectName)
        {
            Canvas.ForceUpdateCanvases();
            GameObject target = GameObject.Find(objectName);
            Assert.IsNotNull(target, "Missing clickable skill UI object: " + objectName);
            Image circle = target.GetComponent<Image>();
            Assert.IsNotNull(circle, "Skill node needs a circular graphic as its raycast target.");
            Assert.IsTrue(circle.raycastTarget, "The circular node must receive pointer raycasts.");
            Button button = target.GetComponent<Button>();
            Assert.IsNotNull(button);
            Assert.IsTrue(button.interactable, "Even locked skill nodes must remain selectable.");
            RectTransform rect = target.GetComponent<RectTransform>();
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(eventSystem) { position = screen, button = PointerEventData.InputButton.Left };
            var results = new List<RaycastResult>();
            eventSystem.RaycastAll(pointer, results);
            string hitNames = string.Join(", ", results.Select(result => result.gameObject.name).ToArray());
            Assert.IsTrue(results.Any(result => result.gameObject == target), "Expected a raycast hit on " + objectName + ". Hits: " + hitNames);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, pointer, ExecuteEvents.pointerClickHandler);
        }
    }
}
