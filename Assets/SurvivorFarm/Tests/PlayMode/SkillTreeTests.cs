using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
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
        public void CatalogIncludesFunctionalForceAndElementalChains()
        {
            string[] requiredNodes =
            {
                "force_brutal_combo", "force_earthquake", "force_carnage", "force_monster_strength",
                "magic_fire_explosion", "magic_meteor", "magic_cataclysm", "magic_thunderstorm",
                "magic_overload", "magic_thunder_god", "magic_ice_shatter", "magic_frost_nova",
                "magic_glacier", "magic_void_gravity", "magic_black_hole", "magic_singularity",
                "mobility_shadow_burst", "mobility_offensive_teleport"
            };
            foreach (string id in requiredNodes) Assert.NotNull(SkillTreeCatalog.Find(id), id);

            Assert.Greater(SkillTreeCatalog.Find("force_earthquake").Radius, 0f);
            Assert.Greater(SkillTreeCatalog.Find("magic_fire").Duration, 0f);
            Assert.Greater(SkillTreeCatalog.Find("magic_meteor").Cooldown, 0f);
            Assert.Greater(SkillTreeCatalog.Find("chaos_divine_ascension").RequiredBranchInvestment, 0);
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
            int listenersOnStack = 0;
            int maximumStackDepth = 0;
            bus.OnEnemyKilled += context =>
            {
                calls++;
                listenersOnStack++;
                maximumStackDepth = Mathf.Max(maximumStackDepth, listenersOnStack);
                bus.RaiseEnemyKilled(context);
                listenersOnStack--;
            };
            bus.RaiseEnemyKilled(new SkillEventContext(root, null, Vector2.zero, 1));
            Assert.AreEqual(SkillCombatEventBus.MaxChainDepth, calls);
            Assert.AreEqual(1, maximumStackDepth, "Nested events should drain from a queue, not recurse on the call stack.");
        }

        [Test]
        public void EventBusCapsTotalEventsInOneChain()
        {
            SkillCombatEventBus bus = manager.Events;
            int calls = 0;
            bus.OnEnemyKilled += context =>
            {
                calls++;
                bus.RaiseEnemyKilled(context);
                bus.RaiseEnemyKilled(context);
            };

            bus.RaiseEnemyKilled(new SkillEventContext(root, null, Vector2.zero, 1));

            Assert.AreEqual(SkillCombatEventBus.MaxEventsPerChain, calls);
        }

        [Test]
        public void SecondChanceProcEmitsOneUseEventAndPreventsTheLethalHit()
        {
            manager.AddSkillPoints(30);
            manager.AddExperience(10000);
            Assert.IsTrue(manager.TryUnlock("survival_regen"));
            Assert.IsTrue(manager.TryUnlock("survival_lifesteal"));
            Assert.IsTrue(manager.TryUnlock("survival_second_chance"));
            PlayerSurvivalStats stats = root.AddComponent<PlayerSurvivalStats>();
            Assert.NotNull(root.GetComponent<SkillRuntimeEffects>());
            int procs = 0;
            manager.Events.OnSkillUsed += context =>
            {
                if (context.SkillId == "survival_second_chance") procs++;
            };

            stats.Restore(5, 1, 1f);
            stats.TakeDamage(2);

            Assert.AreEqual(2, stats.CurrentHealth);
            Assert.AreEqual(1, procs, "The defensive burst must not recursively trigger itself.");
        }
    }
}
