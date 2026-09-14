using System.Reflection;
using NUnit.Framework;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Tests
{
    public sealed class PlayerBedTests
    {
        private GameObject root;
        private PlayerInventory inventory;
        private PlayerCraftingController crafting;
        private DayNightCycle clock;
        private PlayerBed bed;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Bed Test");
            inventory = root.AddComponent<PlayerInventory>();
            crafting = root.AddComponent<PlayerCraftingController>();
            clock = root.AddComponent<DayNightCycle>();
            var visual = root.AddComponent<SpriteRenderer>();
            bed = root.AddComponent<PlayerBed>();
            bed.Configure(visual, crafting, clock);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void CraftingRequiresBothMaterialsAndPaysOnlyOnce()
        {
            inventory.AddWood(10);
            Assert.IsFalse(crafting.CraftBed());
            Assert.AreEqual(10, inventory.Wood);
            inventory.AddStone(4);
            Assert.IsTrue(crafting.CraftBed());
            Assert.IsFalse(crafting.CraftBed());
            Assert.AreEqual(0, inventory.Wood);
            Assert.AreEqual(0, inventory.Stone);
        }

        [TestCase(22f, 3, true)]
        [TestCase(2f, 2, true)]
        [TestCase(12f, 2, false)]
        [TestCase(5f, 2, false)]
        public void SleepOnlyAtNightAndDoesNotSkipAnExtraDay(float hour, int expectedDay, bool allowed)
        {
            clock.Restore(2, hour);
            Assert.AreEqual(allowed, clock.TrySleepUntilMorning());
            Assert.AreEqual(expectedDay, clock.Day);
            Assert.AreEqual(allowed ? 8f : hour, clock.Hour);
        }

        [Test]
        public void PackingAndPlacingBedAtNightDoesNotSleepUntilNextInteraction()
        {
            inventory.AddWood(10);
            inventory.AddStone(4);
            clock.Restore(1, 23f);
            bed.Interact(FarmTool.Sword, inventory);
            Assert.IsFalse(crafting.BedBuilt);
            Assert.AreEqual(1, inventory.PackedCount("Bed"));
            Assert.AreEqual(23f, clock.Hour);
            // Construction marks a bed usable only after its placement is confirmed.
            Assert.IsTrue(inventory.RemovePacked("Bed", 1));
            crafting.RegisterPlacedBed();
            Assert.IsTrue(crafting.BedBuilt);
            Assert.AreEqual(23f, clock.Hour);
            bed.Interact(FarmTool.Sword, inventory);
            Assert.AreEqual(8f, clock.Hour);
            Assert.AreEqual(2, clock.Day);
            bed.Interact(FarmTool.Sword, inventory);
            Assert.AreEqual(2, clock.Day);
        }

        [Test]
        public void SaveDataRoundTripRestoresBedAndClock()
        {
            var save = root.AddComponent<GameSaveSystem>();
            save.Configure(root.transform, inventory, null, null, null, null, crafting, null, null, null);
            crafting.Restore(0, 0, true);
            clock.Restore(4, 23f);
            var build = typeof(GameSaveSystem).GetMethod("BuildSaveData", BindingFlags.NonPublic | BindingFlags.Instance);
            var restore = typeof(GameSaveSystem).GetMethod("RestoreSaveData", BindingFlags.NonPublic | BindingFlags.Instance);
            var data = build.Invoke(save, null);
            data = JsonUtility.FromJson(JsonUtility.ToJson(data), data.GetType());
            crafting.Restore(0, 0);
            clock.Restore(1, 8f);
            restore.Invoke(save, new[] { data });
            Assert.IsTrue(crafting.BedBuilt);
            Assert.AreEqual(4, clock.Day);
            Assert.AreEqual(23f, clock.Hour);
            Assert.AreEqual(1f, root.GetComponent<SpriteRenderer>().color.a);
        }
    }
}
