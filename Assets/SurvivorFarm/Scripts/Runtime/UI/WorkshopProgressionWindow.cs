using System;
using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>
    /// Unified, full-screen workshop. It is a presentation facade over the
    /// existing shop, crafting, tool-upgrade and mastery services.
    /// </summary>
    public sealed class WorkshopProgressionWindow : MonoBehaviour
    {
        public enum Section { Shop, Upgrades, Skills }

        public static bool IsOpen { get; private set; }
        public Section CurrentSection => section;
        public int SelectedQuickSlot => selectedQuickSlot;
        public int QuickSlotViewCount => panel == null
            ? 0
            : panel.GetComponentsInChildren<Transform>(true)
                .Count(value => value.GetComponent<Button>() != null &&
                    value.name.StartsWith("Quick Slot ", StringComparison.Ordinal));
        public bool HasSkillTreeTierLabels => panel != null &&
            panel.GetComponentsInChildren<Text>(true).Any(value => value.text == "NIVEL 1");

        private PlayerInventory inventory;
        private PlayerCraftingController crafting;
        private PlayerToolUpgradeController upgrades;
        private ToolMastery mastery;
        private PlayerQuickSlots quickSlots;
        private SimpleShopSystem shop;
        private RectTransform panel, body, quickPicker;
        private GameObject canvasRoot;
        private Text wallet, hint;
        private Button[] tabs;
        private Section section;
        private int selectedBranch;
        private int selectedQuickSlot;
        private Image previewImage;
        private Text previewLabel;

        private readonly List<GameObject> generated = new List<GameObject>();
        private bool embedded;
        private bool refreshPending = true;
        private SkillDemonstrationPreview demonstration;

        public void Mount(Transform parent)
        {
            embedded = true;
            panel.SetParent(parent, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0, 1);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(1180, 552);
            panel.GetComponent<Image>().enabled = false;
            foreach (Transform child in panel) child.gameObject.SetActive(child == body || child == wallet.transform || child == hint.transform);
            body.anchoredPosition = new Vector2(0, -52); body.sizeDelta = new Vector2(1180, 495);
            wallet.rectTransform.anchoredPosition = new Vector2(760, -4); wallet.rectTransform.sizeDelta = new Vector2(410, 30);
            hint.rectTransform.anchoredPosition = Vector2.zero; hint.rectTransform.sizeDelta = new Vector2(730, 36);
            panel.gameObject.SetActive(false);
        }
        public void ShowEmbedded(Section value)
        {
            section = value; IsOpen = true; panel.gameObject.SetActive(true); Refresh();
        }
        public void HideEmbedded()
        {
            IsOpen = false;
            if (embedded && panel != null) panel.gameObject.SetActive(false);
        }

        public void Configure(PlayerInventory source, Transform canvasParent)
        {
            inventory = source;
            crafting = source != null ? source.GetComponent<PlayerCraftingController>() : null;
            upgrades = source != null ? source.GetComponent<PlayerToolUpgradeController>() : null;
            mastery = source != null ? source.GetComponent<ToolMastery>() : null;
            quickSlots = source != null ? source.GetComponent<PlayerQuickSlots>() : null;
            shop = FindFirstObjectByType<SimpleShopSystem>();
            if (quickSlots == null && source != null) quickSlots = source.gameObject.AddComponent<PlayerQuickSlots>();
            if (canvasRoot == null) Build(canvasParent);

            if (inventory != null)
            {
                inventory.InventoryChanged -= Refresh;
                inventory.InventoryChanged += Refresh;
            }
            if (crafting != null)
            {
                crafting.CraftingChanged -= Refresh;
                crafting.CraftingChanged += Refresh;
            }
            if (upgrades != null)
            {
                upgrades.ToolUpgradesChanged -= Refresh;
                upgrades.ToolUpgradesChanged += Refresh;
            }
            if (mastery != null)
            {
                mastery.Changed -= Refresh;
                mastery.Changed += Refresh;
            }
            if (quickSlots != null)
            {
                quickSlots.Changed -= Refresh;
                quickSlots.Changed += Refresh;
            }
            Refresh();
        }

        private void Build(Transform canvasParent)
        {
            canvasRoot = MasteryWindow.CreateCanvas("Taller completo", 128);
            panel = AdventureWindow.Rect(canvasRoot.transform, "Taller completo", 0, 0, 1180, 670);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
            panel.anchoredPosition = Vector2.zero;
            FarmUiStyle.Frame(panel.gameObject.AddComponent<Image>());

            MasteryWindow.Label(panel, "TALLER DE RAÍZCLARA", 24, 16, 600, 36, 27);
            MasteryWindow.Label(panel, "Compra, fabrica, mejora y aprende en un solo lugar.", 24, 51, 690, 25, 15).color = FarmUiStyle.Muted;
            wallet = MasteryWindow.Label(panel, "", 710, 20, 375, 30, 18);
            wallet.alignment = TextAnchor.MiddleRight;
            FarmUiStyle.CloseButton(MasteryWindow.Button(panel, "×", 1120, 16, 38, 36, Close));

            tabs = new Button[3];
            string[] names = { "TIENDA Y FABRICACIÓN", "MEJORAS", "ÁRBOL DE HABILIDADES" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                tabs[i] = MasteryWindow.Button(panel, names[i], 24 + i * 378, 88, 366, 38,
                    () => { section = (Section)index; Refresh(); });
            }

            body = AdventureWindow.Rect(panel, "Contenido del taller", 24, 139, 1132, 326);
            hint = MasteryWindow.Label(panel, "", 24, 470, 1132, 27, 14);
            hint.color = FarmUiStyle.Accent;
            quickPicker = AdventureWindow.Rect(panel, "Selector de cajón", 24, 500, 1132, 54);
            BuildQuickStrip(panel);

            // The old CraftingWindow created this HUD entry. Repoint it so the
            // player never has to choose between two different workshop UIs.
            Button hudButton = null;
            if (canvasParent != null)
                hudButton = canvasParent.GetComponentsInChildren<Button>(true)
                    .FirstOrDefault(button => button.name == "Abrir taller");
            if (hudButton != null)
            {
                hudButton.onClick.RemoveAllListeners();
                hudButton.onClick.AddListener(Open);
            }
            else if (canvasParent != null)
            {
                OriginalSpriteHud hud = canvasParent.GetComponentInChildren<OriginalSpriteHud>(true);
                if (hud != null)
                {
                    Button button = MasteryWindow.Button(hud.transform, "Abrir taller", 0, 0, 132, 34, Open);
                    RectTransform rect = button.GetComponent<RectTransform>();
                    rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
                    rect.anchoredPosition = new Vector2(120, 64);
                    hud.RequestStyleRefresh();
                }
            }

            canvasRoot.SetActive(false);
        }

        private void BuildQuickStrip(Transform parent)
        {
            RectTransform strip = AdventureWindow.Rect(parent, "Cinco cajones rápidos", 24, 563, 1132, 86);
            FarmUiStyle.Frame(strip.gameObject.AddComponent<Image>(), true);
            MasteryWindow.Label(strip, "CAJONES RÁPIDOS", 14, 9, 140, 24, 13).color = FarmUiStyle.Muted;
            for (int i = 0; i < PlayerQuickSlots.SlotCount; i++)
            {
                int index = i;
                Button button = MasteryWindow.Button(strip, (i + 1).ToString(), 155 + i * 151, 8, 140, 61,
                    () => { selectedQuickSlot = index; RefreshQuickPicker(); });
                button.name = "Quick Slot " + (i + 1);
                MasteryWindow.Label(button.transform, "", 4, 4, 80, 25, 13).name = "Quick Name";
                MasteryWindow.Label(button.transform, "", 4, 31, 132, 21, 12).name = "Quick Count";
            }
            MasteryWindow.Label(strip, "Pulsa un cajón para elegir o usar su objeto.", 910, 16, 200, 46, 12).color = FarmUiStyle.Muted;
        }

        public void Open()
        {
            if (GameMenuWindow.Instance != null) { GameMenuWindow.OpenWorkshopActive(); return; }
            if (FarmIntroduction.IsOpen || canvasRoot == null) return;
            VillageDialogueWindow.CloseActive();
            SimpleShopSystem.CloseActive();
            GetComponent<AdventureWindow>()?.Close();
            GetComponent<InventoryPanelSystem>()?.Close();
            GetComponent<PlayerEquipmentWindow>()?.Close();
            GetComponent<CraftingWindow>()?.CloseLegacy();
            GetComponent<MasteryWindow>()?.Close();
            GetComponent<VillageUpgradeWindow>()?.Close();
            inventory?.GetComponent<ConstructionSystem>()?.Cancel();
            inventory?.GetComponent<PlayerMovementController>()?.StopMovement();
            inventory?.GetComponent<PlayerCombatController>()?.CancelMelee();
            section = Section.Shop;
            IsOpen = true;
            canvasRoot.SetActive(true);
            canvasRoot.transform.SetAsLastSibling();
            Refresh();
            FarmUiStyle.FitWindow(panel);
        }

        public void Close()
        {
            if (embedded)
            {
                if (GameMenuWindow.IsOpen && GameMenuWindow.Instance != null &&
                    (GameMenuWindow.Instance.CurrentPage == GameMenuWindow.Page.Workshop || GameMenuWindow.Instance.CurrentPage == GameMenuWindow.Page.Upgrades))
                    GameMenuWindow.Instance.Close();
                else HideEmbedded();
                return;
            }
            IsOpen = false;
            if (canvasRoot != null) canvasRoot.SetActive(false);
        }

        private void Update()
        {
            if (PlayerRespawnController.MenuOpen) { Close(); return; }
            if (!IsOpen) return;
            if (!embedded && Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            if (refreshPending) Refresh();
            if (!embedded) FarmUiStyle.FitWindow(panel);
            AnimatePreview();
        }

        public void Refresh()
        {
            if (panel == null || inventory == null) return;
            if (!IsOpen) { refreshPending = true; return; }
            refreshPending = false;
            if (wallet != null) wallet.text = $"Oro {inventory.Coins}   ·   Madera {inventory.Wood}   ·   Piedra {inventory.Stone}";
            for (int i = 0; i < tabs.Length; i++) FarmUiStyle.Button(tabs[i], (int)section == i);
            ClearGenerated();
            if (section == Section.Shop) DrawShop();
            else if (section == Section.Upgrades) DrawUpgrades();
            else DrawSkills();
            if (!embedded) { RefreshQuickStrip(); RefreshQuickPicker(); }
            if (embedded) JourneyMenuStyle.Restyle(panel);
        }

        private void DrawShop()
        {
            hint.text = "Tienda y fabricación · Prepara tu próxima expedición";
            ScrollRect scroll = FarmUiStyle.Scroll(body, "Tienda y recetas", 0, 0, 1170, embedded ? 495 : 326);
            generated.Add(scroll.gameObject);
            RectTransform content = scroll.content;
            float y = 0;
            AddHeading(content, "TIENDA", 0, ref y);
            AddOffer(content, "Food", "Ración preparada", "Recupera vida", "Comprar · 18 oro", BuyFood, ref y);
            AddOffer(content, "Fruit", "Fruta", "Ingrediente y curación", "Comprar · 8 oro", BuyFruit, ref y);
            AddOffer(content, "Wood", "Madera", $"Tienes {inventory.Wood}", "Comprar · 4 oro", BuyWood, ref y);
            AddOffer(content, "Stone", "Piedra", $"Tienes {inventory.Stone}", "Comprar · 5 oro", BuyStone, ref y);
            AddHeading(content, "FABRICAR", 0, ref y);

            IReadOnlyList<EconomyRecipeDescriptor> recipes = crafting != null ? crafting.GetRecipeDescriptors() : Array.Empty<EconomyRecipeDescriptor>();
            foreach (EconomyRecipeDescriptor recipe in recipes)
            {
                EconomyRecipeDescriptor saved = recipe;
                string costs = string.Join(" · ", recipe.Ingredients.Select(value => value.Name + " " + value.Owned + "/" + value.Required));
                AddRecipe(content, recipe.Name, recipe.OutputId == "Food" ? "Cocina" : costs, recipe.CanCraft,
                    recipe.UnavailableReason, () => { crafting?.Craft(saved.Id); Refresh(); }, ref y);
            }
            content.sizeDelta = new Vector2(1100, Mathf.Max(326, y + 12));
        }

        private void DrawUpgrades()
        {
            hint.text = "Mejora herramientas, armas y armaduras desde el mismo taller.";
            ScrollRect scroll = FarmUiStyle.Scroll(body, "Mejoras disponibles", 0, 0, 608, embedded ? 495 : 326);
            RectTransform list = scroll.content;
            float y = 0;
            EconomyRecipeDescriptor sword = null;
            if (crafting != null) crafting.TryGetRecipeDescriptor("Sword", out sword);
            AddHeading(list, "HERRAMIENTAS Y ARMAS", 0, ref y);
            AddUpgradeCard(list, "Sword", "Espada · nivel " + (crafting != null ? crafting.WeaponLevel : 1),
                sword == null ? "" : sword.CanCraft ? string.Join(" · ", sword.Ingredients.Select(v => v.Name + " " + v.Required)) : sword.UnavailableReason,
                PlayerWeaponPresentation.SwordSprite(crafting != null ? crafting.WeaponLevel : 1),
                sword != null && sword.CanCraft,
                "Mejorar", () => { crafting?.Craft("Sword"); Refresh(); }, ref y);
            AddUpgradeCard(list, "Axe", "Hacha", ToolUpgradeDetail(FarmTool.Axe),
                FarmUiStyle.ItemIcon("Axe"), CanUpgradeTool(FarmTool.Axe),
                "Mejorar", () => { upgrades?.TryUpgrade(FarmTool.Axe); Refresh(); }, ref y);
            AddUpgradeCard(list, "Pickaxe", "Pico", ToolUpgradeDetail(FarmTool.Pickaxe),
                FarmUiStyle.ItemIcon("Pickaxe"), CanUpgradeTool(FarmTool.Pickaxe),
                "Mejorar", () => { upgrades?.TryUpgrade(FarmTool.Pickaxe); Refresh(); }, ref y);

            if (mastery != null) foreach (FarmTool tool in new[] { FarmTool.Bow, FarmTool.Hoe, FarmTool.WateringCan })
            {
                if (!mastery.HasBranch(tool)) continue;
                FarmTool selected = tool;
                mastery.Cost(tool, out int wood, out int stone, out int coins);
                string costs = $"Madera {wood} · Piedra {stone} · Oro {coins}";
                AddUpgradeCard(list, tool.ToString(), PlayerToolbelt.GetDisplayName(tool) + " · nivel " + mastery.Level(tool),
                    mastery.CanUnlock(tool) ? costs : mastery.Requirement(tool), FarmUiStyle.ItemIcon(tool.ToString()),
                    mastery.CanUnlock(tool) && mastery.CanAfford(tool), "Mejorar", () => { mastery.Purchase(selected); Refresh(); }, ref y);
            }
            int vitalityCost = 6 + (crafting?.CampLevel ?? 0) * 5;
            AddUpgradeCard(list, "Vitality", "Vitalidad · +1 vida máxima", $"Madera {vitalityCost} · Piedra {vitalityCost}",
                FarmUiStyle.ItemIcon("Food"), crafting != null && inventory.Wood >= vitalityCost && inventory.Stone >= vitalityCost,
                "Mejorar", () => { crafting?.CraftCamp(); Refresh(); }, ref y);

            AddHeading(list, "EQUIPO Y ARMADURAS", 0, ref y);
            foreach (string id in new[] { "Helmet", "Chestplate", "Boots", "IronSword", "WoodenShield" })
            {
                if (crafting == null || !crafting.TryGetRecipeDescriptor(id, out EconomyRecipeDescriptor descriptor)) continue;
                EconomyRecipeDescriptor saved = descriptor;
                AddUpgradeCard(list, id, descriptor.Name, string.Join(" · ", descriptor.Ingredients.Select(value => value.Name + " " + value.Owned + "/" + value.Required)),
                    FarmUiStyle.ItemIcon(id), descriptor.CanCraft, "Fabricar", () => { crafting.Craft(saved.Id); Refresh(); }, ref y);
            }
            list.sizeDelta = new Vector2(598, y + 12);
            AddPreview(body, 674, 0);
        }

        private void DrawSkills()
        {
            hint.text = "Elige una rama y desbloquea los nodos al alcanzar sus usos. El arco aparece al encontrarlo en la mazmorra.";
            MasteryWindow.Button(body, "ABRIR ÁRBOL COMPLETO", 850, 0, 250, 36, SkillTreeWindow.OpenActive);
            if (mastery == null)
            {
                MasteryWindow.Label(body, "El árbol de habilidades completo usa experiencia y puntos de habilidad.", 0, 55, 800, 30, 15).color = FarmUiStyle.Muted;
                return;
            }
            for (int i = 0; i < ToolMastery.Branches.Length; i++)
            {
                int index = i;
                Button branch = MasteryWindow.Button(body, PlayerToolbelt.GetDisplayName(ToolMastery.Branches[i]), 0, i * 49, 166, 40,
                    () => { selectedBranch = index; Refresh(); });
                branch.GetComponent<Image>().color = i == selectedBranch ? new Color(.34f, .47f, .39f) : FarmUiStyle.Control;
                generated.Add(branch.gameObject);
            }

            FarmTool tool = ToolMastery.Branches[Mathf.Clamp(selectedBranch, 0, ToolMastery.Branches.Length - 1)];
            bool branchOpen = mastery.HasBranch(tool);
            int level = mastery.Level(tool);
            MasteryWindow.Label(body, PlayerToolbelt.GetDisplayName(tool) + " · " + (branchOpen ? "nivel " + level : "bloqueada"), 190, 0, 620, 30, 23);
            MasteryWindow.Label(body, branchOpen ? "Los tres nodos se leen de izquierda a derecha." : "Encuentra el arco en un cofre de mazmorra para abrir esta rama.", 190, 32, 620, 24, 14).color = FarmUiStyle.Muted;
            for (int tier = 1; tier <= 3; tier++)
            {
                bool learned = branchOpen && tier <= level;
                bool next = branchOpen && tier == level + 1;
                bool ready = next && mastery.CanUnlock(tool);
                float x = 190 + (tier - 1) * 205;
                RectTransform card = AdventureWindow.Rect(body, "Nodo " + tier, x, 70, 190, 214);
                FarmUiStyle.Frame(card.gameObject.AddComponent<Image>(), true);
                Image icon = AdventureWindow.Rect(card, "Icono", 68, 14, 54, 54).gameObject.AddComponent<Image>();
                icon.sprite = tool == FarmTool.Sword ? PlayerWeaponPresentation.SwordSprite(tier) : FarmUiStyle.ItemIcon(tool == FarmTool.Hoe ? "Shovel" : tool.ToString());
                icon.preserveAspect = true; icon.raycastTarget = false;
                MasteryWindow.Label(card, "NIVEL " + tier, 12, 78, 166, 20, 13).alignment = TextAnchor.MiddleCenter;
                MasteryWindow.Label(card, ToolMastery.TierTitle(tool, tier), 10, 101, 170, 30, 17).alignment = TextAnchor.MiddleCenter;
                Text detail = MasteryWindow.Label(card, ToolMastery.TierDescription(tool, tier), 10, 135, 170, 48, 12);
                detail.alignment = TextAnchor.UpperCenter;
                string caption = learned ? "APRENDIDO" : ready && mastery.CanAfford(tool) ? "APRENDER" : next ? mastery.Requirement(tool) : "BLOQUEADO";
                Button purchase = MasteryWindow.Button(card, caption, 10, 188, 170, 22, () => { if (mastery.Purchase(tool)) Refresh(); });
                purchase.interactable = ready && mastery.CanAfford(tool);
                if (tier < 3) MasteryWindow.Label(body, "›", x + 190, 148, 20, 30, 22).color = FarmUiStyle.Accent;
            }
            AddPreview(body, 830, 0);
        }

        private bool CanUpgradeTool(FarmTool tool) => upgrades != null && inventory != null &&
            upgrades.GetNextCost(tool, out int wood, out int stone, out int coins) &&
            (mastery == null || mastery.CanUnlock(tool)) && inventory.Wood >= wood && inventory.Stone >= stone && inventory.Coins >= coins;

        private string ToolUpgradeDetail(FarmTool tool)
        {
            if (upgrades == null) return "Herramienta no disponible";
            if (!upgrades.GetNextCost(tool, out int wood, out int stone, out int coins)) return "Nivel máximo";
            if (mastery != null && !mastery.CanUnlock(tool)) return "Nivel " + upgrades.GetToolLevel(tool) + " · " + mastery.Requirement(tool);
            return $"Madera {inventory.Wood}/{wood} · Piedra {inventory.Stone}/{stone} · Oro {inventory.Coins}/{coins}";
        }

        private void AddPreview(Transform parent, float x, float y)
        {
            if (embedded)
            {
                JourneyMenuStyle.Label(parent, "La evolución de tu espada", x, y, 480, 38, 25, true);
                var stage = JourneyMenuStyle.Rect(parent, "Demostración de la espada", x, y + 86, 480, 320);
                demonstration = stage.gameObject.AddComponent<SkillDemonstrationPreview>();
                demonstration.Configure("force_shockwave");
                for (int i = 1; i <= 3; i++)
                {
                    int tier = i;
                    JourneyMenuStyle.Button(parent, "Nivel " + i, x + (i - 1) * 160, y + 45, 148, 32,
                        () => { demonstration.SetSkill(tier == 1 ? null : tier == 2 ? "force_heavy_hit" : "force_shockwave"); });
                }
                JourneyMenuStyle.Label(parent, "I   Corte básico     II   Ataque cargado\nIII   Onda de daño en área", x, y + 426, 475, 59, 16);
                return;
            }
            RectTransform preview = AdventureWindow.Rect(parent, "Vista previa animada", x, y, 286, 280);
            FarmUiStyle.Frame(preview.gameObject.AddComponent<Image>(), true);
            MasteryWindow.Label(preview, "VISTA PREVIA", 12, 12, 260, 22, 14).color = FarmUiStyle.Accent;
            previewImage = AdventureWindow.Rect(preview, "Efecto de espada", 55, 48, 175, 150).gameObject.AddComponent<Image>();
            previewImage.preserveAspect = true; previewImage.raycastTarget = false;
            previewLabel = MasteryWindow.Label(preview, "Carga de espada · nivel 2", 16, 210, 254, 34, 15);
            previewLabel.alignment = TextAnchor.MiddleCenter;
            MasteryWindow.Label(preview, "Animación ligera para previsualizar el poder sin cargar vídeos externos.", 15, 242, 256, 32, 11).color = FarmUiStyle.Muted;
            AnimatePreview();
        }

        private void AnimatePreview()
        {
            if (previewImage == null) return;
            previewImage.sprite = PlayerWeaponPresentation.SwordSprite(2);
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5f) * .08f;
            previewImage.transform.localScale = Vector3.one * pulse;
            previewImage.color = Color.Lerp(new Color(1f, .7f, .86f), Color.white, (Mathf.Sin(Time.unscaledTime * 4f) + 1f) * .5f);
        }

        private void RefreshQuickStrip()
        {
            if (quickSlots == null) return;
            for (int i = 0; i < PlayerQuickSlots.SlotCount; i++)
            {
                Button button = panel.Find("Cinco cajones rápidos/Quick Slot " + (i + 1))?.GetComponent<Button>();
                if (button == null) continue;
                string id = quickSlots.Get(i);
                Image image = button.transform.Find("Quick Slot Icon")?.GetComponent<Image>();
                if (image == null)
                {
                    image = AdventureWindow.Rect(button.transform, "Quick Slot Icon", 82, 4, 35, 35).gameObject.AddComponent<Image>();
                    image.preserveAspect = true; image.raycastTarget = false;
                }
                image.sprite = string.IsNullOrEmpty(id) ? null : FarmUiStyle.ItemIcon(quickSlots.Icon(id));
                image.enabled = image.sprite != null;
                Text name = button.transform.Find("Quick Name")?.GetComponent<Text>();
                Text count = button.transform.Find("Quick Count")?.GetComponent<Text>();
                if (name != null) name.text = quickSlots.DisplayName(id);
                if (count != null) count.text = string.IsNullOrEmpty(id) ? "Pulsa para asignar" : "×" + quickSlots.Amount(id);
                button.GetComponent<Image>().color = selectedQuickSlot == i ? FarmUiStyle.Accent : FarmUiStyle.Control;
            }
        }

        private void RefreshQuickPicker()
        {
            if (quickPicker == null || quickSlots == null) return;
            for (int i = quickPicker.childCount - 1; i >= 0; i--) Destroy(quickPicker.GetChild(i).gameObject);
            string selected = quickSlots.Get(selectedQuickSlot);
            MasteryWindow.Label(quickPicker, "Ranura " + (selectedQuickSlot + 1) + " · " + quickSlots.DisplayName(selected), 0, 4, 230, 38, 14);
            float x = 238;
            foreach (string id in quickSlots.GetCandidates())
            {
                string saved = id;
                Button button = MasteryWindow.Button(quickPicker, quickSlots.DisplayName(id), x, 4, 142, 38,
                    () => { quickSlots.Set(selectedQuickSlot, saved); Refresh(); });
                button.GetComponentInChildren<Text>().fontSize = 12;
                x += 148;
                if (x > 1100) break;
            }
            Button clear = MasteryWindow.Button(quickPicker, "Vaciar", Mathf.Min(x, 980), 4, 100, 38,
                () => { quickSlots.Clear(selectedQuickSlot); Refresh(); });
            clear.GetComponentInChildren<Text>().fontSize = 12;
        }

        private void AddHeading(Transform parent, string text, float x, ref float y)
        {
            MasteryWindow.Label(parent, text, x, y, 1100, 26, 17).color = FarmUiStyle.Accent;
            y += 31;
        }

        private void AddOffer(Transform parent, string icon, string name, string detail, string action, UnityEngine.Events.UnityAction callback, ref float y)
        {
            Image image = AdventureWindow.Rect(parent, "Icono " + name, 0, y + 3, 40, 40).gameObject.AddComponent<Image>();
            image.sprite = FarmUiStyle.ItemIcon(icon); image.preserveAspect = true; image.raycastTarget = false;
            MasteryWindow.Label(parent, name, 51, y, 220, 22, 16);
            MasteryWindow.Label(parent, detail, 51, y + 22, 310, 20, 12).color = FarmUiStyle.Muted;
            MasteryWindow.Button(parent, action, 370, y + 4, 146, 34, callback);
            y += 51;
        }

        private void AddRecipe(Transform parent, string title, string detail, bool enabled, string reason, UnityEngine.Events.UnityAction callback, ref float y)
        {
            RectTransform row = AdventureWindow.Rect(parent, "Receta " + title, 0, y, 1040, 43);
            FarmUiStyle.Frame(row.gameObject.AddComponent<Image>(), true);
            MasteryWindow.Label(row, title, 9, 5, 240, 29, 14);
            MasteryWindow.Label(row, string.IsNullOrEmpty(reason) ? detail : reason, 252, 5, 580, 29, 12).color = enabled ? FarmUiStyle.Muted : FarmUiStyle.Negative;
            Button button = MasteryWindow.Button(row, "Fabricar", 856, 5, 164, 32, callback);
            button.interactable = enabled;
            y += 49;
        }

        private void AddUpgradeCard(Transform parent, string id, string title, string detail, Sprite icon, bool enabled, string action, UnityEngine.Events.UnityAction callback, ref float y)
        {
            RectTransform card = AdventureWindow.Rect(parent, "Mejora " + id, 0, y, 590, 60);
            FarmUiStyle.Frame(card.gameObject.AddComponent<Image>(), true);
            Image image = AdventureWindow.Rect(card, "Icono", 9, 9, 42, 42).gameObject.AddComponent<Image>();
            image.sprite = icon; image.preserveAspect = true; image.raycastTarget = false;
            MasteryWindow.Label(card, title, 62, 5, 190, 23, 16);
            MasteryWindow.Label(card, detail, 62, 28, 350, 22, 12).color = FarmUiStyle.Muted;
            Button button = MasteryWindow.Button(card, action, 438, 12, 138, 34, callback);
            button.interactable = enabled;
            y += 68;
        }

        private void BuyBasic(string id, int price)
        {
            if (inventory == null || !inventory.TrySpendCoins(price))
            {
                FarmNotificationCenter.Show("No tienes oro suficiente.");
                return;
            }
            switch (id)
            {
                case "Food": inventory.AddFood(1); break;
                case "Fruit": inventory.AddFruit(1); break;
                case "Wood": inventory.AddWood(1); break;
                case "Stone": inventory.AddStone(1); break;
            }
            FarmNotificationCenter.Show("Compra realizada.");
            Refresh();
        }

        private void BuyFood() { if (shop != null) shop.BuyFood(); else BuyBasic("Food", 18); }
        private void BuyFruit() { if (shop != null) shop.BuyFruit(); else BuyBasic("Fruit", 8); }
        private void BuyWood() { if (shop != null) shop.BuyWood(); else BuyBasic("Wood", 4); }
        private void BuyStone() { if (shop != null) shop.BuyStone(); else BuyBasic("Stone", 5); }

        private void ClearGenerated()
        {
            // The body is owned exclusively by this window. The fixed header,
            // picker and quick-slot strip live outside it and are preserved.
            if (body != null)
                for (int i = body.childCount - 1; i >= 0; i--)
                { var child = body.GetChild(i).gameObject; child.SetActive(false); Destroy(child); }
            generated.Clear();
            previewImage = null;
            previewLabel = null;
        }

        public void SelectSection(Section value)
        {
            section = value;
            Refresh();
        }

        public void SelectQuickSlot(int index)
        {
            if (index < 0 || index >= PlayerQuickSlots.SlotCount) return;
            selectedQuickSlot = index;
            RefreshQuickPicker();
        }

        private void OnDisable() => Close();

        private void OnDestroy()
        {
            if (inventory != null) inventory.InventoryChanged -= Refresh;
            if (crafting != null) crafting.CraftingChanged -= Refresh;
            if (upgrades != null) upgrades.ToolUpgradesChanged -= Refresh;
            if (mastery != null) mastery.Changed -= Refresh;
            if (quickSlots != null) quickSlots.Changed -= Refresh;
            Close();
            if (canvasRoot != null) Destroy(canvasRoot);
        }
    }
}
