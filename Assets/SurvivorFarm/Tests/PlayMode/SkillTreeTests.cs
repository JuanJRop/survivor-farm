using NUnit.Framework;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Tests
{
    public sealed class SkillTreeTests
    {
        private GameObject root;
        private SkillTreeManager manager;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Skill tree fixture");
            root.AddComponent<PlayerInventory>();
            manager = root.GetComponent<SkillTreeManager>();
            Assert.NotNull(manager);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        [Test]
        public void CatalogContainsFiveBranchesAndDataDrivenRequirements()
        {
            Assert.AreEqual(5, SkillTreeCatalog.Branches.Count);
            Assert.GreaterOrEqual(SkillTreeCatalog.All.Count, 30);
            SkillDefinition finalForce = SkillTreeCatalog.Find("force_titan_wrath");
            Assert.NotNull(finalForce);
            Assert.AreEqual("force_execution", finalForce.Prerequisites[0]);
            Assert.AreEqual(12, finalForce.RequiredPlayerLevel);
        }

        [Test]
        public void UnlocksRespectPointsLevelAndPrerequisites()
        {
            Assert.IsTrue(manager.CanUnlock("force_heavy_hit"));
            Assert.IsTrue(manager.TryUnlock("force_heavy_hit"));
            Assert.IsFalse(manager.CanUnlock("force_bleeding_edge"));
            manager.AddSkillPoints(10);
            manager.AddExperience(200);
            Assert.IsTrue(manager.PlayerLevel >= 3);
            Assert.IsTrue(manager.TryUnlock("force_bleeding_edge"));
            Assert.AreEqual(SkillStatus.Maxed, manager.GetStatus("force_heavy_hit"));
        }

        [Test]
        public void CaptureAndRestorePreserveProgression()
        {
            manager.AddSkillPoints(5);
            manager.TryUnlock("survival_regen");
            manager.AddExperience(50);
            SkillTreeSaveData saved = manager.Capture();
            manager.Restore(null);
            Assert.AreEqual(0, manager.GetLevel("survival_regen"));
            manager.Restore(saved);
            Assert.AreEqual(1, manager.GetLevel("survival_regen"));
            Assert.Greater(manager.PlayerLevel, 1);
        }

        [Test]
        public void EventBusCapsNestedChains()
        {
            SkillCombatEventBus bus = manager.Events;
            int calls = 0;
            bus.OnEnemyKilled += context =>
            {
                calls++;
                bus.RaiseEnemyKilled(context);
            };
            bus.RaiseEnemyKilled(new SkillEventContext(root, null, Vector2.zero, 1));
            Assert.AreEqual(SkillCombatEventBus.MaxChainDepth, calls);
        }
    }
}
