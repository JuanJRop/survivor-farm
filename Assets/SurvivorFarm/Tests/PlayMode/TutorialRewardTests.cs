using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Tests
{
    public sealed class TutorialRewardTests
    {
        private GameObject root;
        private PlayerInventory inventory;
        private TutorialQuestSystem quests;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Tutorial Reward Test");
            inventory = root.AddComponent<PlayerInventory>();
            quests = root.AddComponent<TutorialQuestSystem>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void FirstGatheringObjectivesPayOnlyOnce()
        {
            FarmGameEvents.RaiseTreeHarvested();
            FarmGameEvents.RaiseTreeHarvested();
            FarmGameEvents.RaiseRockHarvested();
            FarmGameEvents.RaiseGrassDug();
            FarmGameEvents.RaiseGrassDug();
            Assert.AreEqual(2, inventory.Wood);
            Assert.AreEqual(2, inventory.Stone);
            Assert.AreEqual(0, inventory.CommonSeeds);
            Assert.AreEqual(127, quests.RewardedSteps);
        }

        [Test]
        public void RestoreAndLegacyMigrationDoNotPayCompletedObjectivesAgain()
        {
            quests.Restore(3, 0, 7, 0);
            FarmGameEvents.RaiseTreeHarvested();
            FarmGameEvents.RaiseGrassDug();
            Assert.AreEqual(0, inventory.Wood);
            Assert.AreEqual(0, inventory.CommonSeeds);
            FarmGameEvents.RaiseCropHarvested();
            Assert.AreEqual(0, inventory.Coins);
            Assert.AreEqual(0, inventory.Food);
            int mask = quests.CompletedSteps;
            int paid = quests.RewardedSteps;
            quests.Restore(0, 0, mask, 0, paid);
            FarmGameEvents.RaiseCropHarvested();
            Assert.AreEqual(0, inventory.Coins);
        }

        [Test]
        public void RetiredFarmingEventsCannotGrantRewards()
        {
            FarmGameEvents.RaiseCropHarvested();
            quests.Restore(0, 0, 0, 0, quests.RewardedSteps);
            FarmGameEvents.RaiseCropHarvested();
            Assert.AreEqual(0, inventory.Coins);
        }
    }
}
