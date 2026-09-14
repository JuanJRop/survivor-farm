using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Tests
{
    public sealed class ExteriorBuildingTests
    {
        GameObject root, outside, interior, panel;
        PlayerInventory inventory;
        SimpleShopSystem shop;
        ShopEntrance entrance;
        Camera camera;

        GameObject Create(string name)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform); return go;
        }

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Exterior services test");
            outside = Create("Outdoor World"); interior = Create("Old shop interior");
            var player = Create("Player");
            inventory = player.AddComponent<PlayerInventory>();
            player.AddComponent<PlayerSurvivalStats>(); player.AddComponent<ConstructionSystem>();
            var crafting = player.AddComponent<PlayerCraftingController>();
            var upgrades = player.AddComponent<PlayerToolUpgradeController>();
            camera = Create("Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.orthographic = true; camera.orthographicSize = 6.7f; camera.transform.position = new Vector3(2, 4, -10);
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)); canvas.transform.SetParent(root.transform);
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(1280, 720);
            panel = new GameObject("Shop Panel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(canvas.transform);
            shop = Create("Shop controller").AddComponent<SimpleShopSystem>();
            shop.Configure(panel, null, null, inventory, upgrades, crafting);
            entrance = Create("Shop door").AddComponent<ShopEntrance>();
            var point = Create("Outside spawn").transform; point.position = new Vector3(7, -3);
            entrance.Configure(outside, interior, player.transform, point, interior.transform, shop);
        }

        [TearDown]
        public void TearDown()
        {
            SimpleShopSystem.CloseActive(); Object.DestroyImmediate(root);
        }

        [Test]
        public void OpeningAndClosingShopNeverChangesWorldPositionOrCamera()
        {
            inventory.transform.position = new Vector3(4, -2);
            var position = inventory.transform.position; var cameraPosition = camera.transform.position;
            entrance.Interact(FarmTool.Sword, inventory);
            Assert.IsTrue(SimpleShopSystem.IsOpen); Assert.IsTrue(InventoryPanelSystem.IsOpen);
            Assert.IsFalse(entrance.IsInsideShop); Assert.IsFalse(interior.activeSelf); Assert.IsTrue(outside.activeSelf);
            Assert.AreEqual(position, inventory.transform.position); Assert.AreEqual(cameraPosition, camera.transform.position);
            Assert.AreEqual(6.7f, camera.orthographicSize);
            shop.ExitShop();
            Assert.IsFalse(SimpleShopSystem.IsOpen); Assert.IsFalse(panel.activeSelf);
            Assert.AreEqual(position, inventory.transform.position); Assert.AreEqual(6.7f, camera.orthographicSize);
        }

        [Test]
        public void CloseButtonDoesNotRequireAnEntranceAndRepeatedUseStillCloses()
        {
            shop.SetEntrance(null);
            for (int i = 0; i < 3; i++)
            {
                shop.OpenService("Food");
                Assert.AreEqual("Food", panel.GetComponent<ExteriorShopWindow>().Category);
                var close = panel.GetComponentsInChildren<Button>().Single(button => button.name == "Cerrar");
                close.onClick.Invoke();
                Assert.IsFalse(panel.activeSelf); Assert.IsFalse(SimpleShopSystem.IsOpen);
            }
        }

        [Test]
        public void SavedShopPresenceRecoversOutsideInsteadOfReopeningInterior()
        {
            inventory.transform.position = new Vector3(0, 100);
            outside.SetActive(false); interior.SetActive(true);
            entrance.RestoreInsideState(true);
            Assert.AreEqual(entrance.OutsidePosition, inventory.transform.position);
            Assert.IsTrue(outside.activeSelf); Assert.IsFalse(interior.activeSelf);
            Assert.IsFalse(entrance.IsInsideShop); Assert.IsFalse(SimpleShopSystem.IsOpen);
        }

        [Test]
        public void ShopPurchasesRefreshDisplayedWalletAndUseRealSellPrices()
        {
            inventory.AddCoins(50); shop.OpenService("Materials");
            shop.BuyWood();
            Assert.AreEqual(46, inventory.Coins);
            Assert.IsTrue(panel.GetComponentsInChildren<Text>().Any(text => text.text == "Oro: 46"));
            Assert.IsTrue(panel.GetComponentsInChildren<Text>().Any(text => text.text == "Vender: " + BackpackActions.Price("Wood")));
        }

        [Test]
        public void HomeEntryIsDisabledAndLegacyFurnitureRemainsAccessibleOutside()
        {
            var house = Create("Refuge").AddComponent<BaseHouse>(); house.transform.position = new Vector3(1, 0);
            var home = inventory.gameObject.AddComponent<HouseSystem>();
            var position = inventory.transform.position;
            house.Interact(FarmTool.Sword, inventory); home.Enter(false);
            Assert.AreEqual(position, inventory.transform.position); Assert.IsFalse(home.IsInside);
            Assert.IsFalse(home.RoomRoot.gameObject.activeSelf); Assert.IsFalse(home.Snapshot().inside);
            var chest = new BuildingData { kind = "Chest", indoors = true, x = 0, y = 100, wood = 12 };
            var build = inventory.GetComponent<ConstructionSystem>(); build.Restore(new System.Collections.Generic.List<BuildingData> { chest });
            Assert.IsTrue(build.CanAccess(chest));
            int before = inventory.Wood;
            Assert.IsTrue(build.Transfer(chest, "Wood", false));
            Assert.AreEqual(before + 10, inventory.Wood); Assert.AreEqual(2, chest.wood);
            Assert.IsFalse(build.Store(chest));
            Assert.IsTrue(build.BeginMove(chest)); build.Cancel();
            Assert.IsTrue(chest.indoors); Assert.AreEqual(100, chest.y); Assert.AreEqual(2, chest.wood);
            inventory.transform.position = new Vector3(20, 0);
            Assert.IsFalse(build.CanAccess(chest)); Assert.IsFalse(home.Buy("Campfire"));
            inventory.transform.position = new Vector3(0, 100);
            home.RestorePresence(true);
            Assert.AreEqual(home.ReturnPosition, inventory.transform.position);
            Assert.IsFalse(home.IsInside); Assert.IsFalse(home.Data.inside);
        }
    }
}
