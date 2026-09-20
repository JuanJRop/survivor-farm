using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Tests
{
    public sealed class NoFarmingGameplayTests
    {
        GameObject player;
        PlayerInventory inventory;
        PlayerSurvivalStats stats;

        [SetUp]
        public void SetUp()
        {
            player = new GameObject("No farming test");
            inventory = player.AddComponent<PlayerInventory>();
            stats = player.AddComponent<PlayerSurvivalStats>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(player);

        [Test]
        public void LegacyZeroHungerCannotDrainHealthOverTimeOrOnStarvationCall()
        {
            stats.Restore(5, 3, 0);
            for (int i = 0; i < 100; i++) stats.AdvanceNeeds(3600);
            stats.TakeDamage(10, true);
            Assert.AreEqual(3, stats.CurrentHealth);
            Assert.AreEqual(1f, stats.HungerPercent);
            stats.TakeDamage(1);
            Assert.AreEqual(2, stats.CurrentHealth, "Combat damage must still apply.");
        }

        public static IEnumerable<string> Foods => new[] { "Food", "Fruit" }
            .Concat(SurvivalItemCatalog.All.Where(item => item.IsFood).Select(item => item.Id));

        [TestCaseSource(nameof(Foods))]
        public void EveryFoodHealsAndIsNotWastedAtFullHealth(string id)
        {
            if (id == "Food") inventory.AddFood(3);
            else if (id == "Fruit") inventory.AddFruit(3);
            else inventory.AddItem(id, 3);
            Assert.IsFalse(BackpackActions.Use(inventory, id));
            Assert.AreEqual(3, BackpackActions.Count(inventory, id));
            stats.Restore(5, 4, 0);
            Assert.IsTrue(BackpackActions.Use(inventory, id));
            Assert.AreEqual(5, stats.CurrentHealth);
            Assert.AreEqual(2, BackpackActions.Count(inventory, id));
            Assert.IsFalse(BackpackActions.Use(inventory, id));
            stats.Restore(5, 0, 0);
            Assert.IsFalse(BackpackActions.Use(inventory, id), "Food cannot revive a dead player.");
            Assert.AreEqual(2, BackpackActions.Count(inventory, id));
        }

        [Test]
        public void ToolbeltOnlyCyclesWeaponsAndRejectsRetiredTools()
        {
            var belt = player.AddComponent<PlayerToolbelt>();
            belt.SelectNext(); Assert.AreEqual(FarmTool.Sword, belt.SelectedTool, "Bow must be found before cycling to it.");
            inventory.AddEquipment("Bow");
            belt.SelectNext(); Assert.AreEqual(FarmTool.Bow, belt.SelectedTool);
            belt.SelectNext(); Assert.AreEqual(FarmTool.Sword, belt.SelectedTool);
            belt.SelectPrevious(); Assert.AreEqual(FarmTool.Bow, belt.SelectedTool);
            foreach (var tool in new[] { FarmTool.Hoe, FarmTool.Shovel, FarmTool.WateringCan })
            { belt.Select(tool); Assert.AreEqual(FarmTool.Bow, belt.SelectedTool); }
            var upgrades = player.AddComponent<PlayerToolUpgradeController>();
            inventory.AddCoins(100); inventory.AddWood(100); inventory.AddStone(100);
            Assert.IsFalse(upgrades.TryUpgrade(FarmTool.Hoe));
            Assert.AreEqual(100, inventory.Coins);
        }

        [Test]
        public void HeartPanelGrowsWhenMaximumHealthIncreases()
        {
            var root = new GameObject("Test HUD", typeof(RectTransform));
            root.transform.SetParent(player.transform);
            var hud = root.AddComponent<OriginalSpriteHud>();
            hud.Stats = stats;
            var vitals = new GameObject("Vitals", typeof(RectTransform)).GetComponent<RectTransform>();
            vitals.SetParent(root.transform);
            hud.Hearts = new UnityEngine.UI.Image[8];
            for (int i = 0; i < hud.Hearts.Length; i++)
            {
                var heart = new GameObject("Heart", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                heart.transform.SetParent(vitals);
                hud.Hearts[i] = heart.GetComponent<UnityEngine.UI.Image>();
            }
            hud.Refresh(); float width = vitals.sizeDelta.x;
            stats.IncreaseMaxHealth(2); hud.Refresh();
            Assert.Greater(vitals.sizeDelta.x, width);
            Assert.IsTrue(hud.Hearts[6].gameObject.activeSelf);
            Assert.IsFalse(hud.Hearts[7].gameObject.activeSelf);
            Assert.GreaterOrEqual(vitals.sizeDelta.x, 12 + 6 * 26 + 24);
        }

        [Test]
        public void OldSeedsArePreservedButCannotBeUsedOrBought()
        {
            inventory.AddSeeds(4); inventory.AddItem("CarrotSeeds", 3); inventory.AddCoins(100);
            Assert.IsFalse(BackpackActions.Use(inventory, "CommonSeeds"));
            Assert.IsFalse(BackpackActions.Use(inventory, "CarrotSeeds"));
            Assert.IsFalse(inventory.TryBuyCatalogItem("CarrotSeeds"));
            Assert.AreEqual(4, inventory.CommonSeeds);
            Assert.AreEqual(3, inventory.GetItemCount("CarrotSeeds"));
            Assert.AreEqual(100, inventory.Coins);
        }

        [Test]
        public void EveryCookingIngredientCanBeBoughtWithoutCultivation()
        {
            var shop = player.AddComponent<SimpleShopSystem>();
            shop.Configure(null, null, null, inventory, null, null);
            foreach (var cost in EconomyCookingRecipes.All.SelectMany(recipe => recipe.Ingredients))
            {
                Assert.IsFalse(cost.ItemId.Contains("Seeds"));
                if (cost.ItemId == "Wood" || cost.ItemId == "Fruit") continue;
                Assert.IsTrue(shop.CanBuyCatalogItem(cost.ItemId, out _), cost.ItemId);
            }
        }

        [Test]
        public void DaliaAndPortalDoNotRequireHarvestsOrConsumeSeeds()
        {
            player.SetActive(false);
            var campaign = player.AddComponent<ValleyCampaign>();
            typeof(ValleyCampaign).GetProperty("Inventory").GetSetMethod(true).Invoke(campaign, new object[] { inventory });
            campaign.Data.note = true;
            inventory.AddFruit(2); inventory.AddWood(4);
            Assert.IsTrue(campaign.AcceptVillageQuest("village:farmer"));
            Assert.IsTrue(campaign.CompleteVillageQuest("village:farmer"));
            Assert.IsTrue(campaign.Data.daliaGardenRestored);
            Assert.AreEqual(0, inventory.Fruit);
            Assert.AreEqual(0, inventory.TotalSeeds);
            Assert.IsFalse(campaign.Data.harvest);
            Assert.IsFalse(campaign.Use("portal", FarmTool.Sword));
            campaign.Data.sealStone = campaign.Data.guardian = true;
            Assert.IsTrue(campaign.Use("portal", FarmTool.Sword));
            Assert.IsTrue(campaign.Data.portal);
        }
    }
}
