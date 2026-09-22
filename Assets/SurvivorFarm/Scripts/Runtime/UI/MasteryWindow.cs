using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>Three explicit tiers per branch; purchases use the saved mastery and crafting state.</summary>
    public sealed class MasteryWindow : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private GameObject canvasRoot;
        private RectTransform panel;
        private ToolMastery mastery;
        private int selected;
        private readonly Text[] titles = new Text[3], descriptions = new Text[3], status = new Text[3], costs = new Text[3];
        private readonly Image[] nodes = new Image[3], icons = new Image[3];
        private readonly Button[] purchases = new Button[3], branches = new Button[6];
        private Text branchTitle, branchDescription;
        private float nextRefresh;
        public void Open()
        {
            if (FarmIntroduction.IsOpen) return;
            if (canvasRoot == null) Build();
            canvasRoot.SetActive(true); IsOpen = true; Refresh();
            GetComponent<PlayerCombatController>()?.CancelMelee(); GetComponent<PlayerMovementController>()?.StopMovement();
            FarmUiStyle.FitWindow(panel);
        }
        public void Close() { IsOpen = false; if (canvasRoot != null) canvasRoot.SetActive(false); }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                if (IsOpen) Close();
                else if (PortfolioSession.Active && PortfolioSession.Instance.HasBegun && !PortfolioSession.Instance.IsPaused && !InventoryPanelSystem.IsOpen) Open();
            }
            if (!IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .2f; Refresh(); FarmUiStyle.FitWindow(panel); }
        }
        private void Build()
        {
            mastery = GetComponent<ToolMastery>();
            if (mastery == null) mastery = gameObject.AddComponent<ToolMastery>();
            canvasRoot = CreateCanvas("Árbol de habilidades", 110);
            panel = AdventureWindow.Rect(canvasRoot.transform, "Árbol de habilidades", 0, 0, 1080, 590);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f); panel.anchoredPosition = Vector2.zero;
            FarmUiStyle.Frame(panel.gameObject.AddComponent<Image>());
            Label(panel, "ÁRBOL DE HABILIDADES", 26, 18, 950, 36, 26);
            Label(panel, "Domina cada oficio, reúne materiales y desbloquea el siguiente nivel.", 26, 59, 965, 26, 16).color = FarmUiStyle.Muted;
            FarmUiStyle.CloseButton(Button(panel, "×", 1015, 18, 39, 38, Close));
            for (int i = 0; i < ToolMastery.Branches.Length; i++)
            {
                int index = i;
                branches[i] = Button(panel, PlayerToolbelt.GetDisplayName(ToolMastery.Branches[i]), 26, 112 + i * 62, 164, 50, () => { selected = index; Refresh(); });
            }
            branchTitle = Label(panel, "", 219, 105, 812, 30, 22);
            branchDescription = Label(panel, "", 219, 141, 812, 42, 14); branchDescription.color = FarmUiStyle.Muted;
            for (int t = 0; t < 3; t++)
            {
                int tier = t + 1;
                var card = AdventureWindow.Rect(panel, "Nivel " + tier, 218 + t * 278, 194, 264, 318);
                nodes[t] = card.gameObject.AddComponent<Image>(); FarmUiStyle.Frame(nodes[t], true);
                var iconRect = AdventureWindow.Rect(card, "Arma de nivel " + tier, 18, 17, 45, 45);
                icons[t] = iconRect.gameObject.AddComponent<Image>(); icons[t].preserveAspect = true; icons[t].raycastTarget = false;
                Label(card, "NIVEL " + tier, 78, 17, 170, 22, 13).color = FarmUiStyle.Muted;
                titles[t] = Label(card, "", 78, 39, 170, 38, 18);
                descriptions[t] = Label(card, "", 18, 92, 228, 76, 15);
                status[t] = Label(card, "", 18, 175, 228, 44, 14);
                costs[t] = Label(card, "", 18, 225, 228, 38, 13); costs[t].color = FarmUiStyle.Muted;
                purchases[t] = Button(card, "", 18, 269, 228, 34, () => { mastery.Purchase(ToolMastery.Branches[selected]); Refresh(); });
                if (t < 2) Label(panel, "›", 482 + t * 278, 327, 15, 35, 24).color = FarmUiStyle.Accent;
            }
            Label(panel, "K / ESC cerrar   ·   La experiencia recogida también mejora la maestría de combate.", 26, 548, 1015, 22, 14).color = FarmUiStyle.Muted;
        }
        private void Refresh()
        {
            FarmTool tool = ToolMastery.Branches[selected]; int level = mastery.Level(tool); bool branchOpen = mastery.HasBranch(tool);
            branchTitle.text = PlayerToolbelt.GetDisplayName(tool) + " · " + (branchOpen ? "nivel " + level : "por descubrir");
            string use = tool == FarmTool.Sword || tool == FarmTool.Bow ? "impactos o experiencia" : tool == FarmTool.Hoe ? "cosechas" : tool == FarmTool.WateringCan ? "riegos" : "recolecciones";
            branchDescription.text = !branchOpen ? "Encuentra el arco en la mazmorra para abrir esta rama." :
                level >= 3 ? "Has aprendido todas las habilidades de esta rama." : mastery.Uses(tool, level) + " / " + ToolMastery.Required(tool, level) + " " + use + " para el siguiente nivel.";
            for (int i = 0; i < branches.Length; i++) branches[i].GetComponent<Image>().color = i == selected ? new Color(.34f, .47f, .39f) : FarmUiStyle.Control;
            for (int t = 0; t < 3; t++)
            {
                int tier = t + 1; bool obtained = branchOpen && tier <= level;
                bool next = branchOpen && tier == level + 1; bool ready = next && mastery.CanUnlock(tool);
                bool affordable = ready && mastery.CanAfford(tool);
                titles[t].text = ToolMastery.TierTitle(tool, tier);
                descriptions[t].text = ToolMastery.TierDescription(tool, tier);
                icons[t].sprite = tool == FarmTool.Sword ? PlayerWeaponPresentation.SwordSprite(tier) : FarmUiStyle.ItemIcon(tool == FarmTool.Hoe ? "Shovel" : tool.ToString());
                icons[t].color = obtained || next ? Color.white : new Color(.55f, .6f, .6f);
                nodes[t].color = obtained ? new Color(.24f, .38f, .31f) : ready ? new Color(.4f, .34f, .22f) : new Color(.21f, .26f, .27f);
                status[t].text = obtained ? "APRENDIDO" : !branchOpen ? "Encuentra el arco" : !next ? "Requiere nivel " + (tier - 1) :
                    ready ? affordable ? "LISTO PARA APRENDER" : "Faltan materiales" : mastery.Uses(tool, level) + " / " + ToolMastery.Required(tool, level) + " " + use;
                status[t].color = obtained ? FarmUiStyle.Positive : ready ? FarmUiStyle.Accent : FarmUiStyle.Muted;
                ToolMastery.CostForTier(tool, tier, out int wood, out int stone, out int coins);
                costs[t].text = tier == 1 || obtained ? "" : wood + " madera · " + stone + " piedra" + (coins > 0 ? "\n" + coins + " oro" : "");
                purchases[t].GetComponentInChildren<Text>().text = obtained ? "DESBLOQUEADO" : affordable ? "Aprender nivel " + tier : "BLOQUEADO";
                purchases[t].interactable = affordable;
            }
        }
        public static GameObject CreateCanvas(string name, int order)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f; return root;
        }
        public static Text Label(Transform parent, string text, float x, float y, float w, float h, int size)
        { var label = AdventureWindow.Rect(parent, "Texto", x, y, w, h).gameObject.AddComponent<Text>(); FarmUiStyle.Text(label, size); label.text = text; label.raycastTarget = false; return label; }
        public static Button Button(Transform parent, string caption, float x, float y, float w, float h, UnityEngine.Events.UnityAction action)
        { var root = AdventureWindow.Rect(parent, caption, x, y, w, h); root.gameObject.AddComponent<Image>(); var button = root.gameObject.AddComponent<Button>(); FarmUiStyle.Button(button); button.onClick.AddListener(action); Label(root, caption, 4, 2, w - 8, h - 4, 15).alignment = TextAnchor.MiddleCenter; return button; }
        private void OnDisable() => Close();
        private void OnDestroy() { Close(); if (canvasRoot != null) Destroy(canvasRoot); }
    }
}
