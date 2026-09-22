using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Tests
{
    public sealed class WorkshopProgressionTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            foreach (GameObject canvas in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                         .Where(value => value.name == "Taller completo"))
                Object.DestroyImmediate(canvas);
        }

        [Test]
        public void QuickSlotsKeepFiveAssignmentsAndOnlyAcceptUsableItems()
        {
            root = new GameObject("Quick slots fixture");
            PlayerInventory inventory = root.AddComponent<PlayerInventory>();
            PlayerQuickSlots quick = root.GetComponent<PlayerQuickSlots>();

            Assert.IsNotNull(quick);
            Assert.AreEqual(PlayerQuickSlots.SlotCount, quick.Count);
            Assert.AreEqual("Sword", quick.Get(0));
            Assert.IsFalse(quick.Set(4, "Leather"));

            inventory.AddEquipment("Bow");
            Assert.IsTrue(quick.Set(3, "Bow"));
            Assert.AreEqual("Bow", quick.Get(3));
            quick.Restore(new[] { "Bow", "Food" });
            Assert.AreEqual("Bow", quick.Get(0));
            Assert.AreEqual("Food", quick.Get(1));
            Assert.AreEqual(string.Empty, quick.Get(4));
        }

        [Test]
        public void WorkshopBuildsFullScreenTabsSkillTreePreviewAndFiveDrawers()
        {
            root = new GameObject("Workshop fixture");
            PlayerInventory inventory = root.AddComponent<PlayerInventory>();
            root.AddComponent<PlayerToolUpgradeController>();
            root.AddComponent<PlayerCraftingController>();
            root.AddComponent<ToolMastery>();
            GameObject canvas = new GameObject("Workshop canvas", typeof(RectTransform), typeof(Canvas));
            WorkshopProgressionWindow window = root.AddComponent<WorkshopProgressionWindow>();
            window.Configure(inventory, canvas.transform);
            window.Open();

            Assert.IsTrue(WorkshopProgressionWindow.IsOpen);
            Assert.AreEqual(WorkshopProgressionWindow.Section.Shop, window.CurrentSection);
            Assert.AreEqual(PlayerQuickSlots.SlotCount, window.QuickSlotViewCount);

            window.SelectSection(WorkshopProgressionWindow.Section.Skills);
            Assert.AreEqual(WorkshopProgressionWindow.Section.Skills, window.CurrentSection);
            Assert.IsTrue(window.HasSkillTreeTierLabels);
            window.Close();
            Assert.IsFalse(WorkshopProgressionWindow.IsOpen);
        }
    }
}
