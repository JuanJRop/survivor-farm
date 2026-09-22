using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    public sealed class CraftingWindow : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private RectTransform root, recipeContent;
        private ScrollRect scroll;
        private PlayerInventory inventory;
        private PlayerCraftingController crafting;
        private PlayerToolUpgradeController upgrades;
        private readonly List<RecipeRow> rows = new List<RecipeRow>();
        private readonly List<MaterialCostBadge> stock = new List<MaterialCostBadge>();
        private readonly List<Button> tabs = new List<Button>();
        private string category = "All";
        private float refreshAt;
        private static readonly string[] Categories = { "All", "Cooking", "Equipment", "Home" };

        private sealed class Ingredient
        {
            public string id;
            public int owned, required;
        }

        private sealed class RecipeState
        {
            public string name, output, category, reason;
            public int amount = 1;
            public bool available, canCraft;
            public readonly List<Ingredient> ingredients = new List<Ingredient>();
        }

        private sealed class RecipeRow
        {
            public string id, signature;
            public RectTransform root, ingredientRoot;
            public Text title, status;
            public Image output;
            public Button button;
            public readonly List<MaterialCostBadge> costs = new List<MaterialCostBadge>();
        }

        public void Configure(PlayerInventory source, Transform canvas)
        {
            inventory = source;
            crafting = inventory.GetComponent<PlayerCraftingController>();
            upgrades = inventory.GetComponent<PlayerToolUpgradeController>();
            root = AdventureWindow.Rect(canvas, "Taller", 0, 0, 900, 630);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f, .5f);
            root.anchoredPosition = Vector2.zero;
            FarmUiStyle.Frame(root.gameObject.AddComponent<Image>());
            Label(root, "TALLER", 24, 16, 470, 36, 24);
            FarmUiStyle.CloseButton(Button(root, "Cerrar [Esc]", 832, 16, 44, 40, Close));
            var bar = AdventureWindow.Rect(root, "Materiales disponibles", 24, 64, 852, 40);
            string[] resources = { "Wood", "Stone", "Iron", "Fruit", Core.PortfolioSession.Active?"GoldOre":"Coin" };
            for (int i = 0; i < resources.Length; i++) stock.Add(MaterialCostBadge.Create(bar, resources[i], i * 170, 0, 160));
            string[] captions = { "Todo", "Cocina", "Equipo", "Hogar" };
            for (int i = 0; i < Categories.Length; i++)
            {
                string selected = Categories[i];
                tabs.Add(Button(root, captions[i], 24 + i * 214, 114, 208, 38, () => SelectCategory(selected)));
            }
            scroll = FarmUiStyle.Scroll(root, "Recetas", 24, 168, 852, 438);
            recipeContent = scroll.content;
            BuildRows();
            var hud = canvas.GetComponentInChildren<OriginalSpriteHud>(true);
            if (hud != null)
            {
                var entry = Button(hud.transform, "Abrir taller", 0, 0, 132, 34, Open);
                var rect = (RectTransform)entry.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
                rect.anchoredPosition = new Vector2(156, 64);
                hud.RequestStyleRefresh();
            }
            inventory.InventoryChanged += Refresh;
            crafting.CraftingChanged += Refresh;
            if (upgrades != null) upgrades.ToolUpgradesChanged += Refresh;
            root.gameObject.SetActive(false);
            Refresh();
        }

        private void BuildRows()
        {
            var ids = new List<string> { "Campfire", "Bed", "Axe", "Pickaxe" };
            ids.AddRange(crafting.GetRecipeDescriptors().Select(recipe => recipe.Id));
            foreach (string id in ids.Distinct())
            {
                if(BackpackActions.IsRetiredDefense(id))continue;
                if(Core.PortfolioSession.Active&&!Core.PortfolioSession.IsDemoRecipe(id))continue;
                var rect = AdventureWindow.Rect(recipeContent, id, 0, 0, 840, 116);
                var output = AdventureWindow.Rect(rect, "Resultado", 8, 16, 48, 48).gameObject.AddComponent<Image>();
                output.preserveAspect = true;
                output.raycastTarget = false;
                var row = new RecipeRow
                {
                    id = id,
                    root = rect,
                    output = output,
                    title = Label(rect, "", 72, 6, 620, 28, 19),
                    status = Label(rect, "", 72, 40, 748, 28, 16),
                    button = Button(rect, "Fabricar", 708, 4, 120, 34, () => Craft(id))
                };
                var line = AdventureWindow.Rect(rect, "Separador", 0, 114, 840, 1).gameObject.AddComponent<Image>();
                line.color = FarmUiStyle.Control;
                line.raycastTarget = false;
                rows.Add(row);
            }
        }

        private void Craft(string id)
        {
            bool done = TryTool(id, out FarmTool tool) ? upgrades != null && upgrades.TryUpgrade(tool) : crafting.Craft(id);
            if (!done && BackpackActions.IsBuilding(id))
            {
                var current = ReadRecipe(id);
                FarmNotificationCenter.Show(string.IsNullOrEmpty(current.reason) ? "No se pudo fabricar ese objeto." : current.reason);
            }
            Refresh();
        }

        public void SelectCategory(string selected)
        {
            if (!Categories.Contains(selected)) return;
            category = selected;
            recipeContent.anchoredPosition = Vector2.zero;
            Refresh();
        }

        public void Refresh()
        {
            if (inventory == null || crafting == null || root == null) return;
            stock[0].Set(inventory.Wood, 0, true);
            stock[1].Set(inventory.Stone, 0, true);
            stock[2].Set(inventory.GetComponent<AdventureProgress>()?.Data.iron ?? 0, 0, true);
            stock[3].Set(inventory.Fruit, 0, true);
            stock[4].Set(Core.PortfolioSession.Active?inventory.GetAvailableItemCount("GoldOre"):inventory.Coins, 0, true);
            float y = 0;
            foreach (var row in rows)
            {
                RecipeState state = ReadRecipe(row.id);
                bool visible = category == "All" || state.category == category;
                row.root.gameObject.SetActive(visible);
                if (!visible) continue;
                row.root.anchoredPosition = new Vector2(0, -y);
                row.title.text = state.name + (state.amount > 1 ? " x" + state.amount : "");
                row.output.sprite = ConstructionSystem.SpriteFor(state.output) ??
                    FarmUiStyle.ItemIcon(EquipmentItems.Find(state.output)?.Icon ?? SurvivalItemCatalog.Find(state.output)?.Icon ?? state.output);
                row.status.text = state.canCraft ? "Disponible" : state.reason;
                row.status.color = state.canCraft ? FarmUiStyle.Positive : state.available ? FarmUiStyle.Negative : FarmUiStyle.Muted;
                row.button.interactable = state.canCraft;
                row.button.GetComponentInChildren<Text>().text = state.category == "Cooking" ? "Cocinar" : "Fabricar";
                float statusHeight = Mathf.Max(28, Mathf.Ceil(row.status.preferredHeight) + 4);
                row.status.rectTransform.sizeDelta = new Vector2(748, statusHeight);
                float costY = 44 + statusHeight;
                string signature = string.Join("|", state.ingredients.Select(ingredient => ingredient.id));
                if (row.signature != signature)
                {
                    if (row.ingredientRoot != null)
                    {
                        row.ingredientRoot.gameObject.SetActive(false);
                        Destroy(row.ingredientRoot.gameObject);
                    }
                    row.costs.Clear();
                    row.ingredientRoot = AdventureWindow.Rect(row.root, "Ingredientes", 72, costY, 756, 36);
                    for (int i = 0; i < state.ingredients.Count; i++)
                        row.costs.Add(MaterialCostBadge.Create(row.ingredientRoot, state.ingredients[i].id, i % 5 * 150, i / 5 * 38, 142));
                    row.signature = signature;
                }
                row.ingredientRoot.anchoredPosition = new Vector2(72, -costY);
                float costHeight = Mathf.Max(36, Mathf.Ceil(state.ingredients.Count / 5f) * 38);
                row.ingredientRoot.sizeDelta = new Vector2(756, costHeight);
                for (int i = 0; i < state.ingredients.Count; i++)
                    row.costs[i].Set(state.ingredients[i].owned, state.ingredients[i].required);
                float height = costY + costHeight + 12;
                row.root.sizeDelta = new Vector2(840, height);
                var separator = (RectTransform)row.root.Find("Separador");
                separator.anchoredPosition = new Vector2(0, -height + 2);
                y += height + 8;
            }
            recipeContent.sizeDelta = new Vector2(840, Mathf.Max(438, y));
            recipeContent.anchoredPosition = new Vector2(0, Mathf.Clamp(recipeContent.anchoredPosition.y, 0, recipeContent.sizeDelta.y - 438));
            for (int i = 0; i < tabs.Count; i++) FarmUiStyle.Button(tabs[i], Categories[i] == category);
        }

        private RecipeState ReadRecipe(string id)
        {
            var state = new RecipeState { name = id, output = id, category = "Equipment", available = true };
            if (crafting.TryGetRecipeDescriptor(id, out EconomyRecipeDescriptor descriptor))
            {
                state.name = descriptor.Name;
                state.output = descriptor.OutputId;
                state.amount = descriptor.OutputAmount;
                state.available = descriptor.IsAvailable;
                state.canCraft = descriptor.CanCraft;
                state.reason = descriptor.UnavailableReason;
                state.category = BackpackActions.IsBuilding(descriptor.OutputId) ? "Home" :
                    descriptor.OutputId == "Food" || SurvivalItemCatalog.IsFood(descriptor.OutputId) ? "Cooking" : "Equipment";
                foreach (var ingredient in descriptor.Ingredients)
                    state.ingredients.Add(new Ingredient { id = ingredient.ItemId, owned = ingredient.Owned, required = ingredient.Required });
                return state;
            }
            if (TryTool(id, out FarmTool tool))
            {
                state.name = "Mejorar " + PlayerToolbelt.GetDisplayName(tool).ToLowerInvariant();
                if (upgrades == null || !upgrades.GetNextCost(tool, out int wood, out int stone, out int coins))
                {
                    state.available = false;
                    state.reason = "No hay otra mejora disponible.";
                    return state;
                }
                Add(state, "Wood", inventory.Wood, wood);
                Add(state, "Stone", inventory.Stone, stone);
                Add(state, "Coin", inventory.Coins, coins);
            }
            else
            {
                state.available = false;
                state.reason = "Receta no disponible.";
                return state;
            }
            state.canCraft = state.ingredients.All(ingredient => ingredient.owned >= ingredient.required);
            state.reason = state.canCraft ? "" : "Faltan materiales";
            return state;
        }

        private static void Add(RecipeState state, string id, int owned, int required)
        {
            if (required > 0) state.ingredients.Add(new Ingredient { id = id, owned = owned, required = required });
        }

        private static bool TryTool(string id, out FarmTool tool)
        {
            tool = id == "Axe" ? FarmTool.Axe : id == "Pickaxe" ? FarmTool.Pickaxe : FarmTool.Hoe;
            return id == "Axe" || id == "Pickaxe";
        }

        private static Text Label(Transform parent, string value, float x, float y, float width, float height, int size)
        {
            var text = AdventureWindow.Rect(parent, "Etiqueta", x, y, width, height).gameObject.AddComponent<Text>();
            FarmUiStyle.Text(text, size);
            text.text = value;
            return text;
        }

        private static Button Button(Transform parent, string caption, float x, float y, float width, float height, UnityEngine.Events.UnityAction action)
        {
            var rect = AdventureWindow.Rect(parent, caption, x, y, width, height);
            rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>();
            FarmUiStyle.Button(button);
            button.onClick.AddListener(action);
            var label = Label(rect, caption, 6, 2, width - 12, height - 4, 17);
            label.alignment = TextAnchor.MiddleCenter;
            return button;
        }

        public void Close()
        {
            CloseLegacy();
            GetComponent<WorkshopProgressionWindow>()?.Close();
        }

        /// <summary>Hides the legacy recipe surface without closing the unified workshop.</summary>
        public void CloseLegacy() { IsOpen = false; if (root != null) root.gameObject.SetActive(false); }
        public void Open()
        {
            if(FarmIntroduction.IsOpen)return;
            WorkshopProgressionWindow unified = GetComponent<WorkshopProgressionWindow>();
            if (unified != null) { unified.Open(); return; }
            VillageDialogueWindow.CloseActive();
            SimpleShopSystem.CloseActive();
            GetComponent<AdventureWindow>()?.Close();
            inventory.GetComponent<ConstructionSystem>()?.Cancel();
            GetComponent<InventoryPanelSystem>()?.Close();
            GetComponent<PlayerEquipmentWindow>()?.Close();
            IsOpen = true;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            inventory.GetComponent<PlayerMovementController>()?.StopMovement();
            Refresh();
            FarmUiStyle.FitWindow(root);
        }
        private void Update()
        {
            if (PlayerRespawnController.MenuOpen) { Close(); return; }
            if (Input.GetKeyDown(KeyCode.F))
            {
                WorkshopProgressionWindow unified = GetComponent<WorkshopProgressionWindow>();
                if (unified != null)
                {
                    if (WorkshopProgressionWindow.IsOpen) unified.Close(); else unified.Open();
                    return;
                }
                if (IsOpen) CloseLegacy(); else Open();
            }
            if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
            if (!IsOpen) return;
            FarmUiStyle.FitWindow(root);
            // Campaign services can change without an inventory event while the window is open.
            if (Time.unscaledTime >= refreshAt) { refreshAt = Time.unscaledTime + .5f; Refresh(); }
        }
        private void OnDisable() => Close();
        private void OnDestroy()
        {
            if (inventory != null) inventory.InventoryChanged -= Refresh;
            if (upgrades != null) upgrades.ToolUpgradesChanged -= Refresh;
            if (crafting != null) crafting.CraftingChanged -= Refresh;
            if (root != null) Destroy(root.gameObject);
        }
    }
}
