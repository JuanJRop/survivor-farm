using System.Collections;
using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class SkillFxIntegrationTests
    {
        [Test]
        public void SharedSpriteRowsPreserveEachEffectsTimingAndScale()
        {
            var bleed = SkillFxLibrary.ForSkill("force_bleeding_edge");
            var hit = SkillFxLibrary.ForHit(null, new SkillEventContext(null, null, Vector2.zero, 1));
            Assert.IsNotNull(bleed); Assert.IsNotNull(hit);
            Assert.AreSame(bleed.At(0), hit.At(0), "The sprite row should be allocated only once.");
            Assert.AreEqual(.26f, bleed.Duration, .001f);
            Assert.AreEqual(.22f, hit.Duration, .001f);
            Assert.AreNotEqual(bleed.Scale, hit.Scale, "Descriptors must not inherit the first cached clip's scale.");
        }

        [Test]
        public void EveryLearnableSkillHasPixelFramesWithinTheCacheBudget()
        {
            foreach (var skill in SkillTreeCatalog.All)
            {
                var clip = SkillFxLibrary.ForSkill(skill.Id);
                Assert.IsNotNull(clip, skill.Id);
                Assert.Greater(clip.FrameCount, 0, skill.Id);
                Assert.IsNotNull(clip.At(.5f), skill.Id);
                Assert.AreEqual(FilterMode.Point, clip.At(.5f).texture.filterMode);
            }
            Assert.LessOrEqual(SkillFxLibrary.CachedClipCount, 96);
        }

        [UnityTest]
        public IEnumerator UnlearnedSkillsDoNotEmitAndPooledEffectsAreCleanedWithPlayer()
        {
            var parent = new GameObject("Skill FX ownership fixture");
            var player = new GameObject("FX player"); player.transform.SetParent(parent.transform);
            var inventory = player.AddComponent<PlayerInventory>();
            var manager = player.GetComponent<SkillTreeManager>();
            GameFeelFeedback.Enabled = true;
            var context = new SkillEventContext(player, null, Vector2.one, 1, skillId: "force_heavy_hit");
            manager.Events.RaiseSkillUsed(context);
            Assert.AreEqual(1, parent.transform.childCount, "Unlearned proc should allocate nothing.");
            manager.AddSkillPoints(1); Assert.IsTrue(manager.TryUnlock("force_heavy_hit"));
            for (int i = 0; i < 60; i++) manager.Events.RaiseSkillUsed(context);
            Assert.LessOrEqual(parent.transform.childCount, 13, "Player plus at most 12 pooled effects.");
            Assert.Greater(parent.transform.childCount, 1);
            Object.Destroy(player);
            yield return null; yield return null;
            Assert.AreEqual(0, parent.transform.childCount, "World-space pool belongs to its player.");
            Object.Destroy(parent);
        }
    }
}
