using NUnit.Framework;
using SurvivorFarm.Runtime.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Tests
{
    public sealed class DayNightCycleTests
    {
        private GameObject root;
        private DayNightCycle clock;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Clock Test");
            clock = root.AddComponent<DayNightCycle>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void StartsInDaylightAndWrapsFullCycles()
        {
            Assert.AreEqual(8f, clock.Hour);
            clock.Advance(clock.CycleDurationSeconds * 2f);
            Assert.AreEqual(3, clock.Day);
            Assert.AreEqual(8f, clock.Hour, 0.001f);
        }

        [Test]
        public void NightAndDawnAreGradualAndMidnightIsContinuous()
        {
            Assert.AreEqual(0f, clock.EvaluateTint(12f).a);
            Assert.Greater(clock.EvaluateTint(20f).a, clock.EvaluateTint(18f).a);
            Assert.Greater(clock.EvaluateTint(5f).a, clock.EvaluateTint(7f).a);
            Assert.AreEqual(clock.EvaluateTint(0f), clock.EvaluateTint(24f));
            Assert.Less(clock.EvaluateTint(0f).a, 0.6f);
        }

        [Test]
        public void RestoresTimeAndIgnoresInvalidElapsedTime()
        {
            clock.Restore(4, 23f);
            clock.Advance(float.NaN);
            clock.Advance(-1f);
            Assert.AreEqual(23f, clock.Hour);
            clock.Advance(clock.CycleDurationSeconds / 24f * 2f);
            Assert.AreEqual(5, clock.Day);
            Assert.AreEqual(1f, clock.Hour, 0.001f);
        }

        [Test]
        public void OverlayDoesNotBlockInputAndClearsWhenDisabled()
        {
            var overlay = new GameObject("Overlay", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            overlay.transform.SetParent(root.transform);
            clock.Configure(overlay, null, null);
            clock.Restore(1, 0f);
            Assert.IsFalse(overlay.raycastTarget);
            Assert.Greater(overlay.color.a, 0f);
            clock.enabled = false;
            Assert.AreEqual(0f, overlay.color.a);
        }
    }
}
