using NUnit.Framework;
using System.Reflection;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Tests
{
    public sealed class OpenExplorationTests
    {
        private GameObject root;
        private RepairableBridge bridge;
        private PlayerInventory inventory;
        private BoxCollider2D blocker;
        [SetUp] public void SetUp()
        {
            root = new GameObject("Bridge Test");
            inventory = root.AddComponent<PlayerInventory>();
            bridge = root.AddComponent<RepairableBridge>();
            blocker = root.AddComponent<BoxCollider2D>();
            bridge.Configure(blocker, null, null, root.transform);
        }
        [TearDown] public void TearDown() => Object.DestroyImmediate(root);

        [Test] public void MissingMaterialsConsumesNothing()
        {
            inventory.AddWood(20);
            inventory.AddStone(9);
            inventory.AddCoins(100);
            bridge.Interact(FarmTool.Axe, inventory);
            Assert.IsFalse(bridge.IsRepaired);
            Assert.IsTrue(blocker.enabled);
            Assert.AreEqual(20, inventory.Wood);
            Assert.AreEqual(9, inventory.Stone);
            Assert.AreEqual(100, inventory.Coins);
        }

        [Test] public void RepairChargesOnceAndRestoresCrossing()
        {
            inventory.AddWood(40);
            inventory.AddStone(20);
            inventory.AddCoins(100);
            bridge.Interact(FarmTool.Axe, inventory);
            bridge.Interact(FarmTool.Axe, inventory);
            Assert.IsTrue(bridge.IsRepaired);
            Assert.IsFalse(blocker.enabled);
            Assert.AreEqual(20, inventory.Wood);
            Assert.AreEqual(10, inventory.Stone);
            Assert.AreEqual(100, inventory.Coins);
            bridge.Restore(false);
            Assert.IsTrue(blocker.enabled);
            bridge.Restore(true);
            Assert.IsFalse(blocker.enabled);
        }

        [Test] public void ExplorationNeverOffersLandForSaleAndNorthFollowsBridge()
        {
            var zone = root.AddComponent<LandUnlockZone>();
            zone.ConfigureExploration(null);
            zone.Restore(false);
            Assert.IsTrue(zone.IsUnlocked);
            Assert.IsFalse(zone.IsAvailable);
            zone.ConfigureExploration(bridge);
            Assert.IsFalse(zone.IsUnlocked);
            bridge.Restore(true);
            Assert.IsTrue(zone.IsUnlocked);
            Assert.IsEmpty(zone.GetInteractionLabel(FarmTool.Sword));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BridgeStateSurvivesSaveRoundTrip(bool repaired)
        {
            var save = root.AddComponent<GameSaveSystem>();
            save.Configure(root.transform, inventory, null, null, null, null, null, null, null, null);
            bridge.Restore(repaired);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var data = typeof(GameSaveSystem).GetMethod("BuildSaveData", flags).Invoke(save, null);
            data = JsonUtility.FromJson(JsonUtility.ToJson(data), data.GetType());
            bridge.Restore(!repaired);
            typeof(GameSaveSystem).GetMethod("RestoreSaveData", flags).Invoke(save, new[] { data });
            Assert.AreEqual(repaired, bridge.IsRepaired);
        }

        [Test] public void LegacyNorthAccessRepairsBridgeWithoutCharging()
        {
            root.name = "North East Zone";
            var zone = root.AddComponent<LandUnlockZone>();
            zone.ConfigureExploration(bridge);
            bridge.Restore(true);
            var save = root.AddComponent<GameSaveSystem>();
            save.Configure(root.transform, inventory, null, null, null, null, null, null, null, null);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var data = typeof(GameSaveSystem).GetMethod("BuildSaveData", flags).Invoke(save, null);
            data.GetType().GetField("version").SetValue(data, 17);
            bridge.Restore(false);
            typeof(GameSaveSystem).GetMethod("RestoreSaveData", flags).Invoke(save, new[] { data });
            Assert.IsTrue(bridge.IsRepaired);
            Assert.AreEqual(0, inventory.Wood);
        }
    }
}
