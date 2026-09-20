using System.Collections;
using NUnit.Framework;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class CombatFxTests
    {
        private GameObject root;
        private CombatFeelRangeCue cue;

        [SetUp] public void SetUp()
        {
            Time.timeScale = 1; GameFeelFeedback.Enabled = true;
            root = new GameObject("Authored combat FX fixture");
            cue = root.AddComponent<CombatFeelRangeCue>();
        }

        [TearDown] public void TearDown()
        { Object.DestroyImmediate(root); Time.timeScale = 1; GameFeelFeedback.Enabled = true; }

        [Test] public void NormalHeavyAndChargedSweepsUseDistinctAuthoredArt()
        {
            Assert.That(CombatFxLibrary.Atlas, Is.Not.Null);
            Assert.That(CombatFxLibrary.Atlas.filterMode, Is.EqualTo(FilterMode.Point));
            cue.Show(Vector3.zero, 1.25f);
            var normal = cue.SweepVisual.sprite;
            cue.Show(Vector3.zero, 1.25f, true, 3);
            var heavy = cue.SweepVisual.sprite;
            cue.Show(Vector3.zero, 1.7f, true, 1, true);
            var charged = cue.SweepVisual.sprite;
            Assert.That(normal.texture, Is.SameAs(CombatFxLibrary.Atlas));
            Assert.That(heavy.texture, Is.SameAs(normal.texture));
            Assert.That(charged.texture, Is.SameAs(normal.texture));
            Assert.That(heavy, Is.Not.SameAs(normal));
            Assert.That(charged, Is.Not.SameAs(heavy).And.Not.SameAs(normal));
            Assert.That(root.GetComponentsInChildren<MeshFilter>(), Is.Empty);
        }

        [UnityTest] public IEnumerator AuthoredSweepAdvancesAndFinishesDuringPause()
        {
            cue.Show(Vector3.zero, 1.25f);
            var first = cue.SweepVisual.sprite;
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.08f);
            Assert.That(cue.IsShowing, Is.True);
            Assert.That(cue.SweepVisual.sprite, Is.Not.SameAs(first));
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(cue.IsShowing, Is.False);
        }

        [Test] public void CancellingChargeClearsItsAuthoredVisual()
        {
            cue.ShowCharge(Vector3.zero, .8f, Vector2.right);
            Assert.That(cue.IsCharging, Is.True);
            Assert.That(cue.ChargeVisual.sprite.texture, Is.SameAs(CombatFxLibrary.Atlas));
            cue.HideCharge();
            Assert.That(cue.IsCharging, Is.False);
            cue.ShowCharge(Vector3.zero, 1f, Vector2.up);
            cue.enabled = false;
            Assert.That(cue.IsCharging, Is.False);
        }

        [UnityTest] public IEnumerator SurfaceImpactsUseSpritesAndRespectTheEffectsOption()
        {
            CombatHitParticles.Spawn(Vector3.zero, root.transform, ImpactSurface.Creature, false, false);
            CombatHitParticles.Spawn(Vector3.right, root.transform, ImpactSurface.Stone, false, false);
            var effects = root.GetComponentsInChildren<CombatSpriteEffect>();
            Assert.That(effects.Length, Is.EqualTo(2));
            Assert.That(effects[0].Visual.sprite.texture, Is.SameAs(CombatFxLibrary.Atlas));
            Assert.That(effects[1].Visual.sprite.texture, Is.SameAs(CombatFxLibrary.Atlas));
            Assert.That(effects[0].ClipRow, Is.Not.EqualTo(effects[1].ClipRow));
            Assert.That(root.GetComponentsInChildren<ParticleSystem>(), Is.Empty);
            GameFeelFeedback.Enabled = false;
            CombatHitParticles.Spawn(Vector3.zero, root.transform, ImpactSurface.Creature, true, true);
            Assert.That(root.GetComponentsInChildren<CombatSpriteEffect>().Length, Is.EqualTo(2));
            yield return null;
            foreach (var effect in effects) Assert.That(effect.IsPlaying, Is.False);
        }

        [Test] public void OneWallHitCreatesOneMaterialSpecificImpact()
        {
            var wall=new GameObject("Stone wall");wall.transform.SetParent(root.transform,false);
            var defense=wall.AddComponent<FarmDefense>();
            defense.Configure(new BuildingData {kind="StoneWall",health=10,wallVersion=1},null,null);
            defense.TakeDamage(1,null);
            var impacts=root.GetComponentsInChildren<CombatSpriteEffect>();
            Assert.That(impacts.Length,Is.EqualTo(1),"A wall hit must not double its effect or consume two burst slots.");
            Assert.That(impacts[0].ClipRow,Is.EqualTo(CombatFxLibrary.StoneImpact));
        }
    }
}
