using System;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    public sealed class ExteriorShopWindow : MonoBehaviour
    {
        SimpleShopSystem shop;
        PlayerInventory inventory;
        PlayerToolUpgradeController upgrades;
        RectTransform content;
        Text title, wallet;
        Button[] tabs;
        string category = "Food";
        readonly string[] categories = { "Food", "Materials", "Tools" };
        public string Category => category;

        public void Configure(SimpleShopSystem owner, PlayerInventory stock, PlayerToolUpgradeController tools)
        {
            shop = owner; inventory = stock; upgrades = tools;
            foreach (Transform child in transform) child.gameObject.SetActive(false);
            var rect = (RectTransform)transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(800, 570);
            FarmUiStyle.Frame(GetComponent<Image>() ?? gameObject.AddComponent<Image>());
            title = Label(transform, "MERCADO", 24, 18, 650, 36, 24);
            FarmUiStyle.CloseButton(Button(transform, "Cerrar", 732, 16, 44, shop.ExitShop));
            wallet = Label(transform, "", 24, 64, 740, 30, 18);
            string[] captions = { "Alimentos", "Materiales", "Herramientas" };
            tabs = new Button[categories.Length];
            for (int i = 0; i < tabs.Length; i++)
            {
                string id = categories[i];
                tabs[i] = Button(transform, captions[i], 24 + i * 253, 106, 245, () => SelectCategory(id));
            }
            content = FarmUiStyle.Scroll(transform, "Surtido", 24, 166, 752, 378).content;
            Refresh();
        }

        public void SelectCategory(string value)
        {
            if (value == "Seeds") value = "Food"; // Old facade bindings open provisions.
            if (!categories.Contains(value)) return;
            category = value; content.anchoredPosition = Vector2.zero; Refresh();
        }

        public void Refresh()
        {
            if (content == null) return;
            foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            title.text = category == "Food" ? "CARNICERIA Y PROVISIONES" : category == "Tools" ? "HERRAMIENTAS" : "MERCADO DE RAIZCLARA";
            wallet.text = "Oro: " + inventory.Coins;
            for (int i = 0; i < tabs.Length; i++) FarmUiStyle.Button(tabs[i], categories[i] == category);
            int row = 0;
            void Offer(string icon, string name, string detail, string buy, Action purchase, string sell = null, Action sale = null)
            {
                float y = row++ * 86;
                var image = AdventureWindow.Rect(content, "Icono " + icon, 4, y + 10, 48, 48).gameObject.AddComponent<Image>();
                image.sprite = FarmUiStyle.ItemIcon(icon); image.preserveAspect = true; image.raycastTarget = false;
                Label(content, name, 66, y + 4, 350, 28, 19);
                Label(content, detail, 66, y + 36, 350, 32, 15).color = FarmUiStyle.Muted;
                Button(content, buy, 428, y + 13, 148, purchase);
                if (sale != null) Button(content, sell, 586, y + 13, 148, sale);
            }
            if (category == "Food")
            {
                Offer("Food", "Racion preparada", "Disponibles: " + inventory.Food + " | +2 vida", "Comprar: 18", shop.BuyFood);
                Offer("Fruit", "Fruta", "Disponibles: " + inventory.Fruit, "Comprar: 8", shop.BuyFruit, "Vender: 6 oro", shop.SellFruit);
            }
            else if (category == "Materials")
            {
                Offer("Wood", "Madera", "Disponibles: " + inventory.Wood, "Comprar: 4", shop.BuyWood, "Vender: " + BackpackActions.Price("Wood"), shop.SellWood);
                Offer("Stone", "Piedra", "Disponibles: " + inventory.Stone, "Comprar: 5", shop.BuyStone, "Vender: " + BackpackActions.Price("Stone"), shop.SellStone);
            }
            else
            {
                Offer("Axe", "Hacha", upgrades?.GetNextCostText(FarmTool.Axe) ?? "", "Mejorar", shop.UpgradeAxe);
                Offer("Pickaxe", "Pico", upgrades?.GetNextCostText(FarmTool.Pickaxe) ?? "", "Mejorar", shop.UpgradePickaxe);
            }
            foreach (var item in shop.GetAvailableCatalogItems().Where(item =>
                category == "Food" ? item.IsFood :
                category == "Materials" && item.Category == SurvivalItemCategory.Material))
            {
                string id = item.Id;
                Offer(item.Icon, item.Name, "Disponibles: " + inventory.GetItemCount(id) + (item.IsFood ? " | +" + item.HealRestore + " vida" : ""),
                    "Comprar: " + item.BuyPrice, () => shop.BuyCatalogItem(id),
                    "Vender: " + item.SellPrice, () => shop.SellCatalogItem(id));
            }
            content.sizeDelta = new Vector2(740, Mathf.Max(378, row * 86));
            FarmUiStyle.FitWindow((RectTransform)transform);
        }

        static Text Label(Transform parent, string value, float x, float y, float w, float h, int size)
        {
            var text = AdventureWindow.Rect(parent, value, x, y, w, h).gameObject.AddComponent<Text>();
            FarmUiStyle.Text(text, size, true); text.text = value; return text;
        }

        static Button Button(Transform parent, string caption, float x, float y, float width, Action action)
        {
            var rect = AdventureWindow.Rect(parent, caption, x, y, width, 42);
            rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>(); FarmUiStyle.Button(button);
            button.onClick.AddListener(() => action());
            Label(rect, caption, 6, 4, width - 12, 34, 16).alignment = TextAnchor.MiddleCenter;
            return button;
        }
    }
}
