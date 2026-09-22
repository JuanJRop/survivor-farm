using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Tests
{
    public sealed class UiConsistencyTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        public void WindowsFitTheCanvasAtBothQaResolutions(int width, int height)
        {
            float canvasScale = height / 720f;
            var canvas = new Vector2(width / canvasScale, height / canvasScale);
            foreach (var size in new[] { new Vector2(850, 600), new Vector2(900, 630), new Vector2(900, 640) })
            {
                float scale = FarmUiStyle.WindowScale(canvas, size);
                Assert.Greater(scale, 0);
                Assert.LessOrEqual(size.x * scale + 24, canvas.x);
                Assert.LessOrEqual(size.y * scale + 24, canvas.y);
            }
        }

        [Test]
        public void CropIconsShowHarvestedFoodAndReuseOriginalPixelArt()
        {
            int checkedCrops = 0;
            foreach (var food in SurvivalItemCatalog.All.Where(item => item.IsFood))
            {
                var frames = Resources.LoadAll<Sprite>("BackpackIcons/" + food.Icon);
                Assert.IsNotEmpty(frames, food.Id);
                if (frames[0].texture.height != 16) continue;
                var icon = FarmUiStyle.ItemIcon(food.Icon);
                Assert.AreEqual(frames.Max(frame => frame.rect.x), icon.rect.x, food.Id);
                Assert.AreSame(frames[0].texture, icon.texture, food.Id);
                Assert.AreEqual(FilterMode.Point, icon.texture.filterMode, food.Id);
                Assert.AreSame(icon, FarmUiStyle.ItemIcon(food.Icon), food.Id);
                checkedCrops++;
            }
            Assert.GreaterOrEqual(checkedCrops, 20);
        }

        [Test]
        public void EquipmentPreviewUsesTheSameAccessorySlotAsEquip()
        {
            string[] slots = { "", "", "", "Sword", "", "", "Ring", "" };
            var amulet = EquipmentItems.Find("Amulet");
            Assert.AreEqual(7, UiEquipmentComparison.TargetSlot(slots, amulet, -1));
            Assert.AreEqual(6, UiEquipmentComparison.TargetSlot(slots, amulet, 6));
            slots[7] = "FireElement";
            Assert.AreEqual(6, UiEquipmentComparison.TargetSlot(slots, amulet, -1));
        }

        [Test]
        public void ComparisonShowsBothGainsAndLosses()
        {
            string comparison = UiEquipmentComparison.Differences(EquipmentItems.Find("IronBoots"), EquipmentItems.Find("RangerBoots"));
            StringAssert.Contains("-8 pp", comparison);
            StringAssert.Contains("+12%", comparison);
            StringAssert.Contains("#FF9B92", comparison);
            StringAssert.Contains("#97E4AB", comparison);
        }

        [Test]
        public void IdenticalEquipmentDoesNotClaimAnUpgrade()
        {
            var item = EquipmentItems.Find("IronSword");
            Assert.AreEqual("Sin cambios en bonificaciones", UiEquipmentComparison.Differences(item, item));
        }

        [Test]
        public void BowSlotAppearsOnlyAfterTheBowIsFound()
        {
            root = new GameObject("HUD bow discovery test", typeof(RectTransform));
            root.SetActive(false);
            var inventory = root.AddComponent<PlayerInventory>();
            inventory.RestoreEquipment(new[] { "Sword" }, new[] { "", "", "", "Sword", "", "", "", "" });
            var belt = root.AddComponent<PlayerToolbelt>();
            var canvasObject = new GameObject("HUD canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(root.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var toolbar = new GameObject("Tools", typeof(RectTransform));
            toolbar.transform.SetParent(canvasObject.transform, false);
            var sword = CreateHudButton("Sword", toolbar.transform);
            var bow = CreateHudButton("Bow", toolbar.transform);
            var hudObject = new GameObject("HUD", typeof(RectTransform));
            hudObject.transform.SetParent(canvasObject.transform, false);
            var hud = hudObject.AddComponent<OriginalSpriteHud>();
            hud.Inventory = inventory;
            hud.Toolbelt = belt;
            hud.ToolButtons = new[] { sword, bow };

            root.SetActive(true);
            Assert.That(bow.gameObject.activeSelf, Is.False);

            inventory.AddEquipment("Bow");

            Assert.That(bow.gameObject.activeSelf, Is.True);
        }

        private static Button CreateHudButton(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            return go.GetComponent<Button>();
        }

        [Test]
        public void HealthBarShowsCurrentHealthAndEquippedArmor()
        {
            root = new GameObject("Health bar display test");
            var player = new GameObject("Player");
            player.transform.SetParent(root.transform, false);
            var inventory = player.AddComponent<PlayerInventory>();
            var stats = player.AddComponent<PlayerSurvivalStats>();
            inventory.AddEquipment("IronChestplate");
            Assert.That(inventory.Equip("IronChestplate", 1), Is.True);
            stats.Restore(7, 4, 1f);

            var panel = new GameObject("Vitals", typeof(RectTransform));
            panel.transform.SetParent(root.transform, false);
            var heart = new GameObject("Heart", typeof(RectTransform), typeof(Image));
            heart.transform.SetParent(panel.transform, false);
            var bar = root.AddComponent<PlayerHealthBarUI>();
            bar.Configure(stats, new[] { heart.GetComponent<Image>() });

            var text = panel.GetComponentInChildren<Text>(true);
            Assert.That(text, Is.Not.Null);
            Assert.That(text.text, Does.Contain("4/7"));
            var armor = panel.transform.Find("Barra de vida/Armadura").GetComponent<Text>();
            Assert.That(armor.text, Does.Contain("34%"));
            Assert.That(heart.GetComponent<Image>().color.a, Is.EqualTo(0f).Within(.001f));
        }

        [Test]
        public void IngredientKeepsExactQuantitiesInTooltipAndUsesTheCoinSprite()
        {
            root = new GameObject("UI badge test", typeof(RectTransform));
            var badge = MaterialCostBadge.Create(root.transform, "Coins", 0, 0, 142);
            badge.Set(int.MaxValue, 25);
            var tooltip = root.GetComponentInChildren<HudActionTooltip>();
            StringAssert.Contains(int.MaxValue.ToString(), tooltip.Caption);
            Assert.AreEqual("2.1G/25", root.GetComponentInChildren<Text>().text);
            var icon = root.GetComponentsInChildren<Image>().First(image => image.name == "Icono");
            Assert.AreEqual(Resources.Load<Sprite>("BackpackIcons/Coin"), icon.sprite);
            Assert.IsNotNull(icon.sprite);
            badge.Set(3, 25);
            StringAssert.Contains("Faltan 22", tooltip.Caption);
            Assert.AreEqual(FarmUiStyle.Negative, root.GetComponentInChildren<Text>().color);
        }

        [Test]
        public void RecipeRowsReflectControllerRequirementsAndDynamicIngredients()
        {
            root = new GameObject("UI recipe test");
            root.SetActive(false);
            var inventory = root.AddComponent<PlayerInventory>();
            root.AddComponent<PlayerSurvivalStats>();
            var crafting = root.AddComponent<PlayerCraftingController>();
            root.AddComponent<PlayerToolUpgradeController>();
            var canvas = new GameObject("UI canvas", typeof(RectTransform), typeof(Canvas));
            canvas.transform.SetParent(root.transform, false);
            var window = root.AddComponent<CraftingWindow>();
            window.Configure(inventory, canvas.transform);
            foreach (var group in canvas.GetComponentsInChildren<RectTransform>(true).Where(rect => rect.name == "Ingredientes"))
            {
                foreach (RectTransform badge in group)
                {
                    Assert.GreaterOrEqual(badge.anchoredPosition.x, 0);
                    Assert.LessOrEqual(badge.anchoredPosition.x + badge.rect.width, group.rect.width);
                    Assert.LessOrEqual(-badge.anchoredPosition.y + badge.rect.height, group.rect.height);
                }
            }
            window.SelectCategory("Cooking");
            foreach (var recipe in crafting.GetRecipeDescriptors().Where(recipe => recipe.OutputId == "Food"))
            {
                var row = canvas.GetComponentsInChildren<RectTransform>(true).First(rect => rect.name == recipe.Id);
                Assert.IsTrue(row.gameObject.activeSelf);
                Assert.AreEqual(recipe.CanCraft, row.GetComponentInChildren<Button>(true).interactable);
                if (!recipe.CanCraft)
                    Assert.IsTrue(row.GetComponentsInChildren<Text>(true).Any(text => text.text == recipe.UnavailableReason));
                foreach (var ingredient in recipe.Ingredients)
                    Assert.IsNotNull(row.Find("Ingredientes/" + ingredient.ItemId + " requerido"));
            }
            inventory.AddItem("Carrot", 2);
            window.Refresh();
            Assert.IsTrue(crafting.TryGetRecipeDescriptor("Food", out var updated));
            var food = canvas.GetComponentsInChildren<RectTransform>(true).First(rect => rect.name == "Food");
            var ingredientRoot = food.GetComponentsInChildren<RectTransform>(true)
                .First(rect => rect.name == "Ingredientes" && rect.gameObject.activeSelf);
            foreach (var ingredient in updated.Ingredients)
                Assert.IsNotNull(ingredientRoot.Find(ingredient.ItemId + " requerido"));
            window.SelectCategory("Home");
            foreach (var recipe in crafting.GetRecipeDescriptors().Where(recipe => BackpackActions.IsBuilding(recipe.OutputId)))
            {
                var row = canvas.GetComponentsInChildren<RectTransform>(true).First(rect => rect.name == recipe.Id);
                Assert.IsTrue(row.gameObject.activeSelf);
                Assert.AreEqual(recipe.CanCraft, row.GetComponentInChildren<Button>(true).interactable);
            }
            inventory.AddPacked("Bed");
            window.Refresh();
            Assert.IsTrue(crafting.TryGetRecipeDescriptor("Bed", out var bed));
            Assert.IsFalse(bed.IsAvailable);
            var bedRow = canvas.GetComponentsInChildren<RectTransform>(true).First(rect => rect.name == "Bed");
            Assert.IsFalse(bedRow.GetComponentInChildren<Button>(true).interactable);
            Assert.IsTrue(bedRow.GetComponentsInChildren<Text>(true).Any(text => text.text == bed.UnavailableReason));
            window.Close();
        }
    }
}
