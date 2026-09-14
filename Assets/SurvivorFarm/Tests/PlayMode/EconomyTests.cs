using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SurvivorFarm.Tests
{
    public sealed class EconomyTests
    {
        private GameObject player;
        private GameObject plotObject;
        private GameObject gridObject;
        private PlayerInventory inventory;
        private PlayerCraftingController crafting;
        private Tilemap previousPaths;
        private bool previousFeedback;

        [SetUp]
        public void SetUp()
        {
            previousFeedback = GameFeelFeedback.Enabled;
            GameFeelFeedback.Enabled = false;
            player = new GameObject("Economy Player");
            inventory = player.AddComponent<PlayerInventory>();
            player.AddComponent<PlayerSurvivalStats>();
            crafting = player.AddComponent<PlayerCraftingController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (plotObject != null) Object.DestroyImmediate(plotObject);
            if (gridObject != null)
            {
                typeof(CultivationGrid).GetField("paths", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, previousPaths);
                Object.DestroyImmediate(gridObject);
            }
            Object.DestroyImmediate(player);
            GameFeelFeedback.Enabled = previousFeedback;
        }

        [Test]
        public void CookingRequiresFireAndPreservesLegacyFruitPriority()
        {
            inventory.AddFruit(2);
            inventory.AddItem("Carrot", 2);
            Assert.That(crafting.Craft("Food"), Is.False);
            Assert.That(inventory.Fruit, Is.EqualTo(2));
            crafting.RegisterPlacedFire();
            Assert.That(crafting.Craft("Food"), Is.True);
            Assert.That(inventory.Fruit, Is.Zero);
            Assert.That(inventory.GetItemCount("Carrot"), Is.EqualTo(2));
            Assert.That(inventory.Food, Is.EqualTo(1));
            Assert.That(crafting.MealsCooked, Is.EqualTo(1));
        }

        [Test]
        public void LegacyCommandUsesRealCropsAndReportsTheSelectedCosts()
        {
            inventory.AddItem("Carrot", 2);
            crafting.RegisterPlacedFire();
            Assert.That(crafting.TryGetRecipeDescriptor("Food", out var recipe), Is.True);
            Assert.That(recipe.Ingredients.Single().ItemId, Is.EqualTo("Carrot"));
            Assert.That(recipe.CanCraft, Is.True);
            Assert.That(crafting.Craft("Food"), Is.True);
            Assert.That(inventory.GetItemCount("Carrot"), Is.Zero);
            Assert.That(inventory.Food, Is.EqualTo(recipe.OutputAmount));
            Assert.That(crafting.MealsCooked, Is.EqualTo(1));
        }

        [Test]
        public void MissingIngredientNeverPartiallyDebitsAndUiExplainsIt()
        {
            crafting.RegisterPlacedFire();
            inventory.AddItem("Potato", 2);
            Assert.That(crafting.TryGetRecipeDescriptor("TravelRations", out var recipe), Is.True);
            Assert.That(recipe.IsAvailable, Is.True);
            Assert.That(recipe.CanCraft, Is.False);
            Assert.That(recipe.Ingredients.Single(value => value.ItemId == "Wheat").Missing, Is.EqualTo(1));
            Assert.That(recipe.UnavailableReason, Does.Contain("Trigo"));
            Assert.That(crafting.Craft(recipe.Id), Is.False);
            Assert.That(inventory.GetItemCount("Potato"), Is.EqualTo(2));
            Assert.That(crafting.MealsCooked, Is.Zero);
        }

        [Test]
        public void MealTransactionPublishesCompleteIngredientsOutputAndTutorialCounter()
        {
            crafting.RegisterPlacedFire();
            inventory.AddItem("Potato", 2);
            inventory.AddItem("Wheat", 1);
            int changes = 0;
            inventory.InventoryChanged += () =>
            {
                changes++;
                Assert.That(inventory.GetItemCount("Potato"), Is.Zero);
                Assert.That(inventory.GetItemCount("Wheat"), Is.Zero);
                Assert.That(inventory.Food, Is.EqualTo(2));
                Assert.That(crafting.MealsCooked, Is.EqualTo(1));
            };
            Assert.That(crafting.Craft("TravelRations"), Is.True);
            Assert.That(changes, Is.EqualTo(1));
        }

        [Test]
        public void WorkshopUnlocksOnlyOptionalBatchAndBasicEquipmentStaysAvailable()
        {
            player.SetActive(false);
            var campaign = player.AddComponent<ValleyCampaign>();
            crafting.RegisterPlacedFire();
            inventory.AddItem("Potato", 2);
            inventory.AddItem("Wheat", 2);
            inventory.AddWood(20);
            inventory.AddStone(20);
            Assert.That(crafting.TryGetRecipeDescriptor("WorkshopRations", out var locked), Is.True);
            Assert.That(locked.UnavailableReason, Does.Contain("Nico"));
            Assert.That(crafting.Craft("WorkshopRations"), Is.False);
            Assert.That(crafting.Craft("Helmet"), Is.True);
            campaign.Data.nicoWorkshopRepaired = true;
            Assert.That(crafting.Craft("WorkshopRations"), Is.True);
            Assert.That(inventory.Food, Is.EqualTo(3));
            Assert.That(crafting.MealsCooked, Is.EqualTo(1));
        }

        [Test]
        public void ShopSellsIngredientsButNeverSeeds()
        {
            var shop = player.AddComponent<SimpleShopSystem>();
            shop.Configure(null, null, null, inventory, null, crafting);
            inventory.AddCoins(100);
            Assert.That(shop.CanBuyCatalogItem("PumpkinSeeds", out string reason), Is.False);
            Assert.That(reason, Does.Contain("retirado"));
            shop.BuyCatalogItem("CarrotSeeds", 2);
            Assert.That(inventory.Coins, Is.EqualTo(100));
            Assert.That(inventory.GetItemCount("CarrotSeeds"), Is.Zero);
            shop.BuyCatalogItem("Carrot", 2);
            Assert.That(inventory.GetItemCount("Carrot"), Is.EqualTo(2));
            shop.SellCatalogItem("Carrot", 2);
            Assert.That(inventory.Coins, Is.LessThan(100));
            crafting.Restore(0, 0, false, true, 6);
            Assert.That(shop.CanBuyCatalogItem("PumpkinSeeds", out _), Is.False);
            Assert.That(shop.GetAvailableCatalogItems().Any(item => item.IsSeed), Is.False);
        }

        [Test]
        public void TradeRejectsOverflowBeforeRemovingCoinsOrItemsAndCommonSalesStayOpen()
        {
            inventory.AddCoins(int.MaxValue);
            inventory.AddItem("Carrot", 2);
            Assert.That(inventory.TryBuyCatalogItem("Carrot", int.MaxValue), Is.False);
            Assert.That(BackpackActions.Sell(inventory, "Carrot", 2), Is.False);
            Assert.That(inventory.GetItemCount("Carrot"), Is.EqualTo(2));
            Assert.That(inventory.Coins, Is.EqualTo(int.MaxValue));
            inventory.TrySpendCoins(10);
            inventory.AddWood(1);
            Assert.That(BackpackActions.Sell(inventory, "Wood", 1), Is.True);
            Assert.That(inventory.Wood, Is.Zero);
        }

        [Test]
        public void ReservationsPreventDoubleSpendingAndReleaseExactlyOnce()
        {
            inventory.AddItem("CarrotSeeds", 1);
            inventory.PreferredCatalogSeed = "CarrotSeeds";
            Assert.That(inventory.TryReserveSeed(out var reserved), Is.True);
            Assert.That(inventory.GetItemCount("CarrotSeeds"), Is.EqualTo(1));
            Assert.That(inventory.GetAvailableItemCount("CarrotSeeds"), Is.Zero);
            Assert.That(inventory.TryConsumeSeed(), Is.False);
            Assert.That(BackpackActions.Sell(inventory, "CarrotSeeds", 1), Is.False);
            inventory.ReleaseSeedReservation(reserved);
            inventory.ReleaseSeedReservation(reserved);
            Assert.That(inventory.GetAvailableItemCount("CarrotSeeds"), Is.EqualTo(1));
            Assert.That(inventory.TryReserveSeed(out reserved), Is.True);
            Assert.That(inventory.CommitSeedReservation(reserved), Is.True);
            Assert.That(inventory.CommitSeedReservation(reserved), Is.False);
            Assert.That(inventory.GetItemCount("CarrotSeeds"), Is.Zero);
        }

        [Test]
        public void GenericReservationsKeepSaveCountsAndCannotBeSold()
        {
            inventory.AddSeeds(1);
            Assert.That(inventory.TryReserveSeed(out var reserved), Is.True);
            Assert.That(inventory.CommonSeeds, Is.EqualTo(1));
            Assert.That(inventory.TryRemoveSeeds(1), Is.False);
            inventory.Restore(inventory.CommonSeeds, 0, 0, 0, 0);
            Assert.That(reserved.IsActive, Is.False);
            Assert.That(inventory.GetAvailableSeedCount(SeedRarity.Common), Is.EqualTo(1));
        }

        [Test]
        public void RetiredPlotDoesNotReserveSeedAndKeepsLegacySnapshot()
        {
            FarmingPlot plot = CreatePlot();
            inventory.AddItem("CarrotSeeds", 1);
            inventory.PreferredCatalogSeed = "CarrotSeeds";
            plot.Interact(FarmTool.Hoe, inventory);
            Assert.That(plot.IsWorking, Is.False);
            Assert.That(plot.StateId, Is.EqualTo((int)FarmingPlot.PlotState.Tilled));
            Assert.That(plot.PlantedCropItemId, Is.Null);
            var savedStacks = inventory.ItemStacks.Select(value => new InventoryStack { id = value.id, count = value.count }).ToList();
            int savedState = plot.StateId;
            inventory.RestoreItemStacks(savedStacks);
            plot.Restore(savedState, 0f, plot.PlantedSeedRarityId, plot.PlantedCropItemId);
            Assert.That(plot.IsWorking, Is.False);
            Assert.That(inventory.GetAvailableItemCount("CarrotSeeds"), Is.EqualTo(1));
            Assert.That(plot.StateId, Is.EqualTo((int)FarmingPlot.PlotState.Tilled));
        }

        [Test]
        public void LegacyGrowingPlotIsHiddenAndCannotBeHarvested()
        {
            FarmingPlot plot = CreatePlot();
            string id = plot.PersistentId;
            plot.Restore((int)FarmingPlot.PlotState.Growing, 12f, (int)SeedRarity.Common, "Carrot");
            Assert.That(plot.gameObject.activeSelf, Is.False);
            Assert.That(plot.IsAvailable, Is.False);
            Assert.That(plot.SupportsTool(FarmTool.Hoe), Is.False);
            plot.Interact(FarmTool.Hoe, inventory);
            Assert.That(plot.IsWorking, Is.False);
            Assert.That(inventory.GetItemCount("Carrot"), Is.Zero);
            Assert.That(plot.StateId, Is.EqualTo((int)FarmingPlot.PlotState.Growing));
            Assert.That(plot.RemainingGrowTime, Is.EqualTo(12f));
            Assert.That(plot.PlantedCropItemId, Is.EqualTo("Carrot"));
            Assert.That(plot.PersistentId, Is.EqualTo(id));
        }

        [Test]
        public void FullFoodStackRejectsCookingWithoutConsumingIngredients()
        {
            crafting.RegisterPlacedFire();
            inventory.AddFood(int.MaxValue);
            inventory.AddFruit(2);
            Assert.That(crafting.Craft("Food"), Is.False);
            Assert.That(inventory.Fruit, Is.EqualTo(2));
            Assert.That(crafting.MealsCooked, Is.Zero);
        }

        [Test]
        public void DuplicateCatalogStacksPayOneRecipeWithoutLosingOtherUnits()
        {
            inventory.ItemStacks.Add(new InventoryStack { id = "Carrot", count = 1 });
            inventory.ItemStacks.Add(new InventoryStack { id = "Carrot", count = 2 });
            crafting.RegisterPlacedFire();
            Assert.That(crafting.Craft("CarrotSoup"), Is.True);
            Assert.That(inventory.GetItemCount("Carrot"), Is.EqualTo(1));
            Assert.That(inventory.Food, Is.EqualTo(1));
        }

        private FarmingPlot CreatePlot()
        {
            gridObject = new GameObject("Economy Grid", typeof(Grid));
            var paths = new GameObject("Economy Paths", typeof(Tilemap));
            paths.transform.SetParent(gridObject.transform);
            FieldInfo field = typeof(CultivationGrid).GetField("paths", BindingFlags.Static | BindingFlags.NonPublic);
            previousPaths = (Tilemap)field.GetValue(null);
            field.SetValue(null, paths.GetComponent<Tilemap>());
            plotObject = new GameObject("Economy Plot");
            return plotObject.AddComponent<FarmingPlot>();
        }
    }
}
