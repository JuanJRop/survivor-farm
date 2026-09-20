using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class PolishEliteTarget : EnemyAIBase
    {
        public override bool IsElite => true;
        protected override void TickEnemy() { }
    }

    public sealed class CombatPolishTests
    {
        private GameObject root;
        private PlayerInventory player;
        private PlayerCombatController combat;
        private PlayerCharacterAnimator animator;
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1; GameFeelFeedback.Enabled = true; CombatTimeFeedback.ReducedMotion = false;
            root = new GameObject("Combat polish fixture");
            var actor = new GameObject("Player"); actor.transform.SetParent(root.transform);
            actor.AddComponent<SpriteRenderer>();
            player = actor.AddComponent<PlayerInventory>();
            actor.AddComponent<PlayerToolbelt>().Select(FarmTool.Sword);
            animator = actor.AddComponent<PlayerCharacterAnimator>();
            combat = actor.AddComponent<PlayerCombatController>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            foreach (var burst in Object.FindObjectsByType<ImpactBurstLifetime>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(burst.gameObject);
            foreach (var text in Object.FindObjectsByType<FloatingFeedback>(FindObjectsSortMode.None)) Object.DestroyImmediate(text.gameObject);
            Time.timeScale = 1; CombatTimeFeedback.ReducedMotion = false;
        }

        private PolishEliteTarget Enemy(int health = 30)
        {
            var actor = new GameObject("Elite receiver"); actor.transform.SetParent(root.transform);
            var visual = actor.AddComponent<SpriteRenderer>();
            actor.AddComponent<CircleCollider2D>().radius = .12f;
            var enemy = actor.AddComponent<PolishEliteTarget>();
            enemy.ConfigureVisuals(visual, null, Color.white, "Elite");
            enemy.Configure(player.transform, null); enemy.ConfigureStats("Elite", health, 1, 1, .5f, 1, 0);
            enemy.ActivateFromPool(Vector3.right * .8f); Physics2D.SyncTransforms();
            return enemy;
        }

        [Test]
        public void ComboDataIsConfiguredAndThirdHitIsStronger()
        {
            var chain = combat.Combo;
            Assert.That(chain.Definition, Is.Not.Null);
            var first = chain.Begin(0); var second = chain.Begin(.45f); var third = chain.Begin(.88f);
            Assert.That(first.heavy || second.heavy, Is.False);
            Assert.That(third.heavy, Is.True);
            Assert.That(third.damageMultiplier, Is.GreaterThan(first.damageMultiplier));
            Assert.That(third.duration, Is.GreaterThan(second.duration));
            Assert.That(chain.Begin(1.5f), Is.SameAs(first));
        }

        [Test]
        public void ExpiredWindowAndWeaponSwitchRestartAtFirstAttack()
        {
            var chain = combat.Combo;
            var first = chain.Begin(10); chain.Begin(10.5f);
            Assert.That(chain.Begin(13), Is.SameAs(first));
            chain.Begin(13.5f); chain.SelectWeapon("Another sword");
            Assert.That(chain.Begin(13.6f), Is.SameAs(first));
        }

        [UnityTest]
        public IEnumerator AnimatedSwordHitsOnContactFrameAndCannotHitTwice()
        {
            var enemy = Enemy();
            combat.AttackTarget(enemy); combat.AttackTarget(enemy);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(30), "No damage before the sword descends.");
            yield return new WaitForSeconds(.23f);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(29));
            yield return new WaitForSeconds(.3f);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(29));
        }

        [UnityTest]
        public IEnumerator InterruptedWindupDealsNoDamageAndResetsChain()
        {
            var enemy = Enemy();
            combat.AttackTarget(enemy); animator.PlayNamedAction("Damage");
            combat.CancelMelee();
            yield return new WaitForSeconds(.25f);
            Assert.That(enemy.CurrentHealth, Is.EqualTo(30));
            Assert.That(combat.Combo.StepNumber, Is.Zero);
            Assert.That(animator.CurrentClip, Is.EqualTo("Damage"));
        }

        [UnityTest]
        public IEnumerator BufferedInputContinuesOnceAndPauseClearsIt()
        {
            combat.Attack();
            yield return new WaitForSeconds(.3f);
            combat.Attack();
            Assert.That(combat.HasBufferedAttack, Is.True);
            yield return new WaitForSeconds(.16f);
            Assert.That(combat.Combo.StepNumber, Is.EqualTo(2));
            Time.timeScale = 0;
            yield return null;
            Assert.That(combat.HasBufferedAttack, Is.False);
            Assert.That(combat.Combo.StepNumber, Is.Zero);
            Time.timeScale = 1;
        }

        [UnityTest]
        public IEnumerator ThreeActualStrikesDeliverOneOneThreeAndEliteFinisher()
        {
            var enemy = Enemy(5);
            var feedback = player.GetComponent<HitFeedback>();
            int accepted = 0; feedback.HitResolved += _ => accepted++;
            for (int i = 0; i < 3; i++)
            {
                enemy.transform.position = Vector3.right * .8f; Physics2D.SyncTransforms();
                combat.AttackTarget(enemy);
                yield return new WaitForSeconds(i == 2 ? .4f : .46f);
            }
            Assert.That(enemy.CurrentHealth, Is.Zero);
            Assert.That(accepted, Is.EqualTo(3));
            Assert.That(feedback.FinisherCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator TemporaryTimeEffectRestoresAndCannotUnpauseGame()
        {
            var time = player.GetComponent<CombatTimeFeedback>();
            time.Impact(true, true);
            Assert.That(Time.timeScale, Is.LessThan(.1f));
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            time.Impact(true, true); Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.4f);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(time.IsActive, Is.False);
        }

        [Test]
        public void DisablingTimeEffectRestoresPreexistingSpeedAndReducedMotionSkipsIt()
        {
            var time = player.GetComponent<CombatTimeFeedback>();
            Time.timeScale = .8f; time.Impact(true, true); time.enabled = false;
            Assert.That(Time.timeScale, Is.EqualTo(.8f));
            time.enabled = true; CombatTimeFeedback.ReducedMotion = true; time.Impact(true, true);
            Assert.That(Time.timeScale, Is.EqualTo(.8f));
        }

        [UnityTest]
        public IEnumerator CameraFeedbackReturnsExactlyToBaseWithoutDrift()
        {
            var actor = new GameObject("Camera"); actor.transform.SetParent(root.transform);
            var camera = actor.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 6.8f;
            var feedback = actor.AddComponent<CameraFeedback>();
            Vector3 original = actor.transform.position;
            feedback.Impact(Vector3.right, true, true);
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(actor.transform.position, Is.EqualTo(original));
            Assert.That(camera.orthographicSize, Is.EqualTo(6.8f).Within(.0001f));
        }

        [UnityTest]
        public IEnumerator AudioBankIsBoundedDistinctAndUsesPitchVariation()
        {
            var sound = player.GetComponent<AudioFeedback>();
            Assert.That(sound.VoiceCapacity, Is.EqualTo(8));
            foreach (CombatSound kind in System.Enum.GetValues(typeof(CombatSound)))
                Assert.That(AudioFeedback.Clip(kind), Is.Not.Null, kind.ToString());
            Assert.That(AudioFeedback.Clip(CombatSound.Impact), Is.Not.SameAs(AudioFeedback.Clip(CombatSound.HeavyImpact)));
            sound.Play(CombatSound.Impact, player.transform.position); float pitch = sound.LastPitch;
            yield return new WaitForSecondsRealtime(.04f);
            sound.Play(CombatSound.Impact, player.transform.position);
            Assert.That(sound.LastPitch, Is.InRange(.94f, 1.06f));
            Assert.That(sound.LastPitch, Is.Not.EqualTo(pitch));
        }

        [UnityTest]
        public IEnumerator DebrisIsBoundedAndCleansUpDuringPause()
        {
            for (int i = 0; i < 30; i++) CombatHitParticles.Spawn(Vector3.zero, root.transform, ImpactSurface.Leaves, true, false);
            Assert.That(CombatHitParticles.ActiveBursts, Is.LessThanOrEqualTo(CombatHitParticles.MaximumBursts));
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(CombatHitParticles.ActiveBursts, Is.Zero);
        }

        [Test]
        public void PassiveDamageDoesNotFreezeThePlayerButDirectWeaponContactDoes()
        {
            var enemy = Enemy();
            enemy.TakeDamage(1, player);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            var hit = player.GetComponent<HitFeedback>();
            hit.BeginStrike(); hit.ApplyDamage(enemy, 1, player, false);
            Assert.That(Time.timeScale, Is.LessThan(.1f));
        }

        [UnityTest]
        public IEnumerator CratesDropOneExperiencePickupAndUseBreakingSound()
        {
            var actor = new GameObject("Crate"); actor.transform.SetParent(root.transform);
            actor.AddComponent<SpriteRenderer>(); actor.AddComponent<BoxCollider2D>();
            var crate = actor.AddComponent<DungeonDestructible>(); crate.Configure(false);
            int wood = player.Wood;
            crate.TakeDamage(9, player); crate.TakeDamage(9, player);
            Assert.That(player.Wood, Is.EqualTo(wood));
            var drops = root.GetComponentsInChildren<EnemyLootPickup>();
            Assert.That(drops.Length, Is.EqualTo(1));
            Assert.That(drops[0].Item.Kind, Is.EqualTo(ItemKind.Experience));
            Assert.That(drops[0].TryCollect(player), Is.False, "Experience must finish its scattering animation first.");
            Assert.That(player.GetComponent<AudioFeedback>().LastSound, Is.EqualTo(CombatSound.Break));
            int experience = drops[0].Amount;
            var mastery = player.GetComponent<ToolMastery>();
            int before = mastery != null ? mastery.Uses(FarmTool.Sword, 1) : 0;
            player.transform.position = Vector3.right * 20;
            yield return new WaitForSeconds(.7f);
            Assert.IsTrue(drops[0].TryCollect(player));
            Assert.IsFalse(drops[0].TryCollect(player));
            Assert.AreEqual(before + experience, player.GetComponent<ToolMastery>().Uses(FarmTool.Sword, 1));
        }
    }
}
