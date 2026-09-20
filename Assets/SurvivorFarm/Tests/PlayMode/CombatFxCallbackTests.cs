using System.Collections;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class CombatFxUpdateEmitter : MonoBehaviour
    {
        public bool Emitted { get; private set; }
        private void Update()
        {
            if (Emitted) return;
            Emitted = true;
            CombatHitParticles.Spawn(transform.position, transform, ImpactSurface.Stone, false, false);
            gameObject.AddComponent<CombatFeelRangeCue>().Show(transform.position, 1.25f);
            VisibleHitFeedback.Play(gameObject, .1f, false);
        }
    }

    public sealed class CombatFxCallbackTests
    {
        private GameObject root;
        [TearDown] public void TearDown()
        { if (root != null) Object.DestroyImmediate(root); Time.timeScale = 1; GameFeelFeedback.Enabled = true; }

        [UnityTest] public IEnumerator FirstImpactFromEngineUpdateCreatesItsRendererAndCleansUp()
        {
            Time.timeScale = 1; GameFeelFeedback.Enabled = true;
            root = new GameObject("Native update FX regression");
            var emitter = root.AddComponent<CombatFxUpdateEmitter>();
            for (int frame = 0; frame < 3 && !emitter.Emitted; frame++) yield return null;
            Assert.That(emitter.Emitted, Is.True);
            var impact = root.GetComponentInChildren<ImpactBurstLifetime>();
            Assert.That(impact, Is.Not.Null);
            Assert.That(impact.GetComponent<CombatSpriteEffect>().Visual.sprite.texture, Is.SameAs(CombatFxLibrary.Atlas));
            Assert.That(root.GetComponent<CombatFeelRangeCue>().SweepVisual.sprite.texture, Is.SameAs(CombatFxLibrary.Atlas));
            Assert.That(root.GetComponent<VisibleHitFeedback>(), Is.Not.Null);
            yield return new WaitForSecondsRealtime(.65f);
            Assert.That(root.GetComponentInChildren<ImpactBurstLifetime>(), Is.Null);
        }
    }
}
