using System;
using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>Embedded, data-driven combat skill tree with a standalone compatibility view.</summary>
    public sealed class SkillTreeWindow : MonoBehaviour
    {
        private sealed class NodeView
        {
            public SkillDefinition Definition;
            public RectTransform Root;
            public Image Ring;
            public Image Face;
            public Image Icon;
            public Text Tier;
            public Button Button;
        }

        private sealed class ConnectorView
        {
            public string ParentId;
            public string ChildId;
            public Image Image;
        }

        private const float TreeWidth = 780f;
        private const float DetailWidth = 356f;
        private const float HeaderHeight = 70f;
        private const float CardTop = 82f;
        private const float LaneGap = 4f;
        private const float NodeSize = 42f;
        private const float NodeStep = 78f;
        private static SkillTreeWindow active;
        private static Sprite circleSprite;
        private static Sprite mountainSprite;

        public static bool IsOpen { get; private set; }

        private PlayerInventory inventory;
        private SkillTreeManager manager;
        private GameObject standaloneCanvas;
        private RectTransform uiRoot;
        private RectTransform treeCard;
        private RectTransform detailCard;
        private RectTransform viewport;
        private RectTransform treeContent;
        private ScrollRect treeScroll;
        private Text pointsText;
        private Text levelText;
        private Text experienceText;
        private Image experienceFill;
        private Text selectionHint;
        private Text detailTitle;
        private Text detailDescription;
        private Text detailRequirements;
        private Text detailState;
        private Text unlockFeedback;
        private SkillDemonstrationPreview demonstrationPreview;
        private Button unlockButton;
        private bool embedded;
        private string selectedSkill;
        private readonly Dictionary<string, NodeView> nodes = new Dictionary<string, NodeView>(StringComparer.Ordinal);
        private readonly List<ConnectorView> connectors = new List<ConnectorView>(64);

        /// <summary>Connects this view to a player's skill progression.</summary>
        public void Configure(PlayerInventory source, Transform canvasParent)
        {
            if (manager != null) manager.Changed -= Refresh;
            inventory = source;
            manager = source != null ? SkillTreeManager.Ensure(source) : null;
            active = this;
            if (manager != null)
            {
                manager.Changed -= Refresh;
                manager.Changed += Refresh;
            }

            // Old scenes configured a separate overlay. Keep that path usable while
            // the unified menu passes null and mounts this view under its content.
            if (canvasParent != null && uiRoot == null)
                BuildStandalone(canvasParent);
            if (uiRoot != null) Refresh();
        }

        /// <summary>Places the tree under a menu content RectTransform, without creating a Canvas.</summary>
        public void Mount(Transform parent)
        {
            if (parent == null) return;
            if (uiRoot == null) BuildLayout(parent, true);
            else if (uiRoot.parent != parent) uiRoot.SetParent(parent, false);

            embedded = true;
            Stretch(uiRoot);
            Image backdrop = uiRoot.GetComponent<Image>();
            backdrop.sprite = null;
            backdrop.color = Color.clear;
            backdrop.raycastTarget = false;
            uiRoot.gameObject.SetActive(false);
            if (standaloneCanvas != null)
            {
                standaloneCanvas.SetActive(false);
                Destroy(standaloneCanvas);
                standaloneCanvas = null;
            }
            Refresh();
        }

        /// <summary>Shows the already-mounted tree when the unified menu selects its skill tab.</summary>
        public void ShowEmbedded()
        {
            if ((FarmIntroduction.IsOpen && !GameMenuWindow.IsOpen) || manager == null || uiRoot == null) return;
            uiRoot.gameObject.SetActive(true);
            IsOpen = true;
            inventory?.GetComponent<PlayerMovementController>()?.StopMovement();
            inventory?.GetComponent<PlayerCombatController>()?.CancelMelee();
            Refresh();
        }

        /// <summary>Hides the mounted tree when the unified menu leaves its skill tab.</summary>
        public void HideEmbedded()
        {
            if (uiRoot != null) uiRoot.gameObject.SetActive(false);
            if (embedded) IsOpen = false;
        }

        public static void OpenActive()
        {
            if (active == null)
            {
                PlayerInventory player = UnityEngine.Object.FindFirstObjectByType<PlayerInventory>();
                if (player == null) return;
                active = player.GetComponent<SkillTreeWindow>() ?? player.gameObject.AddComponent<SkillTreeWindow>();
                active.Configure(player, null);
            }

            GameMenuWindow.OpenSkillsActive();
        }

        /// <summary>Directly opens the view for legacy callers; unified entry points use OpenActive.</summary>
        public void Open()
        {
            if (FarmIntroduction.IsOpen || manager == null) return;
            if (uiRoot == null) BuildStandalone(null);
            if (uiRoot == null) return;
            uiRoot.gameObject.SetActive(true);
            IsOpen = true;
            inventory?.GetComponent<PlayerMovementController>()?.StopMovement();
            inventory?.GetComponent<PlayerCombatController>()?.CancelMelee();
            Refresh();
            if (!embedded) FarmUiStyle.FitWindow(uiRoot);
        }

        public void Close()
        {
            if (uiRoot != null) uiRoot.gameObject.SetActive(false);
            IsOpen = false;
        }

        /// <summary>Updates player progress and node states without rebuilding the graph.</summary>
        public void Refresh()
        {
            // Skill progression emits changes during combat. The host menu keeps this
            // view mounted while hidden, so do no UI work until the player opens it.
            if (manager == null || uiRoot == null || !uiRoot.gameObject.activeInHierarchy) return;
            if (nodes.Count != SkillTreeCatalog.All.Count)
                BuildGraph();

            pointsText.text = "PUNTOS DE DESTINO   " + manager.SkillPoints.ToString("00");
            levelText.text = "NIVEL " + manager.PlayerLevel.ToString("00");
            experienceText.text = "EXP " + manager.Experience + " / " + manager.ExperienceToNextLevel;
            experienceFill.fillAmount = Mathf.Clamp01(manager.Experience / (float)Mathf.Max(1, manager.ExperienceToNextLevel));
            selectionHint.text = "Arrastra para explorar   ·   Cada hilo señala un requisito real";

            foreach (NodeView node in nodes.Values) RefreshNode(node);
            foreach (ConnectorView connector in connectors)
                connector.Image.color = ConnectorColor(connector.ParentId);
            RefreshDetail();
        }

        private void BuildStandalone(Transform canvasParent)
        {
            if (uiRoot != null) return;
            standaloneCanvas = MasteryWindow.CreateCanvas("Árbol de habilidades · vista clásica", 135);
            BuildLayout(standaloneCanvas.transform, false);
            uiRoot.anchorMin = uiRoot.anchorMax = uiRoot.pivot = new Vector2(.5f, .5f);
            uiRoot.sizeDelta = new Vector2(1180f, 620f);
            uiRoot.anchoredPosition = Vector2.zero;
            if (canvasParent != null) uiRoot.SetAsLastSibling();
            uiRoot.gameObject.SetActive(false);
        }

        private void BuildLayout(Transform parent, bool stretchToParent)
        {
            GameObject root = new GameObject("Árbol de habilidades · tinta y oro", typeof(RectTransform), typeof(Image));
            uiRoot = root.GetComponent<RectTransform>();
            uiRoot.SetParent(parent, false);
            if (stretchToParent) Stretch(uiRoot);
            else
            {
                uiRoot.anchorMin = uiRoot.anchorMax = uiRoot.pivot = new Vector2(.5f, .5f);
                uiRoot.sizeDelta = new Vector2(1180f, 620f);
            }

            Image backdrop = root.GetComponent<Image>();
            backdrop.sprite = stretchToParent ? null : MountainBackdrop();
            backdrop.type = Image.Type.Simple;
            backdrop.color = stretchToParent ? Color.clear : Color.white;
            backdrop.raycastTarget = false;

            InkLabel(uiRoot, "SENDAS DEL VIAJERO", 22, 12, 440, 28, 23, true).color = JourneyMenuStyle.Paper;
            InkLabel(uiRoot, "Elige una técnica y forja tu camino a través del combate.", 23, 42, 500, 19, 12).color = JourneyMenuStyle.Muted;
            pointsText = InkLabel(uiRoot, "", 560, 12, 270, 26, 14);
            pointsText.alignment = TextAnchor.MiddleRight;
            levelText = InkLabel(uiRoot, "", 842, 12, 88, 24, 13);
            levelText.alignment = TextAnchor.MiddleRight;
            experienceText = InkLabel(uiRoot, "", 943, 12, 210, 24, 12);
            experienceText.alignment = TextAnchor.MiddleRight;
            RectTransform expRail = AdventureWindow.Rect(uiRoot, "Progreso de experiencia", 843, 42, 310, 5);
            Image railImage = expRail.gameObject.AddComponent<Image>();
            railImage.color = new Color32(63, 71, 70, 255);
            RectTransform expFill = AdventureWindow.Rect(expRail, "Experiencia actual", 0, 0, 310, 5);
            expFill.anchorMin = Vector2.zero;
            expFill.anchorMax = Vector2.one;
            expFill.offsetMin = expFill.offsetMax = Vector2.zero;
            experienceFill = expFill.gameObject.AddComponent<Image>();
            experienceFill.type = Image.Type.Filled;
            experienceFill.fillMethod = Image.FillMethod.Horizontal;
            experienceFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            experienceFill.color = new Color32(203, 166, 91, 255);
            experienceFill.raycastTarget = false;

            treeCard = AdventureWindow.Rect(uiRoot, "Mapa de sendas", 16, CardTop, TreeWidth, 464);
            Image treePanel = treeCard.gameObject.AddComponent<Image>();
            treePanel.color = JourneyMenuStyle.Card;

            detailCard = AdventureWindow.Rect(uiRoot, "Detalle de técnica", 808, CardTop, DetailWidth, 464);
            Image detailPanel = detailCard.gameObject.AddComponent<Image>();
            detailPanel.color = JourneyMenuStyle.Card;

            InkLabel(treeCard, "SENDAS", 15, 12, 135, 22, 13, true).color = JourneyMenuStyle.Paper;
            selectionHint = InkLabel(treeCard, "", 160, 13, 598, 18, 11);
            selectionHint.alignment = TextAnchor.MiddleRight;

            viewport = AdventureWindow.Rect(treeCard, "Área de sendas", 10, 39, 758, 412);
            Image viewportShade = viewport.gameObject.AddComponent<Image>();
            viewportShade.color = new Color(0.045f, 0.065f, 0.067f, .28f);
            viewportShade.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            treeScroll = viewport.gameObject.AddComponent<ScrollRect>();
            treeScroll.viewport = viewport;
            treeScroll.horizontal = true;
            treeScroll.vertical = true;
            treeScroll.movementType = ScrollRect.MovementType.Clamped;
            treeScroll.scrollSensitivity = 28f;
            treeContent = new GameObject("Árbol conectado", typeof(RectTransform)).GetComponent<RectTransform>();
            treeContent.SetParent(viewport, false);
            treeContent.anchorMin = treeContent.anchorMax = treeContent.pivot = new Vector2(0f, 1f);
            treeContent.anchoredPosition = Vector2.zero;
            treeContent.sizeDelta = new Vector2(758f, 412f);
            treeScroll.content = treeContent;

            BuildDetailPanel();
            BuildGraph();
            if (SkillTreeCatalog.All.Count > 0)
                selectedSkill = SkillTreeCatalog.All[0].Id;
            Refresh();
        }

        private void BuildDetailPanel()
        {
            InkLabel(detailCard, "TÉCNICA SELECCIONADA", 16, 13, 320, 18, 12, true).color = JourneyMenuStyle.Paper;
            RectTransform preview = AdventureWindow.Rect(detailCard, "Vista de combate", 14, 40, 328, 143);
            demonstrationPreview = preview.gameObject.AddComponent<SkillDemonstrationPreview>();
            demonstrationPreview.Configure(null);

            detailTitle = InkLabel(detailCard, "", 16, 196, 320, 30, 20, true);
            detailDescription = InkLabel(detailCard, "", 16, 229, 322, 60, 14);
            detailDescription.color = JourneyMenuStyle.Paper;
            detailRequirements = InkLabel(detailCard, "", 16, 296, 322, 49, 12);
            detailRequirements.color = JourneyMenuStyle.Muted;
            detailState = InkLabel(detailCard, "", 16, 350, 322, 28, 13);
            unlockButton = JourneyMenuStyle.Button(detailCard, "", 16, 384, 322, 42, UnlockSelected);
            unlockButton.name = "Aprender habilidad";
            unlockFeedback = InkLabel(detailCard, "", 16, 430, 322, 22, 12);
            unlockFeedback.alignment = TextAnchor.MiddleCenter;
            unlockFeedback.color = JourneyMenuStyle.Gold;
        }

        private void BuildGraph()
        {
            if (treeContent == null) return;
            for (int i = treeContent.childCount - 1; i >= 0; i--)
                Destroy(treeContent.GetChild(i).gameObject);
            nodes.Clear();
            connectors.Clear();

            List<SkillBranch> branches = SkillTreeCatalog.Branches.ToList();
            List<SkillDefinition> definitions = SkillTreeCatalog.All.ToList();
            var depths = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (SkillDefinition definition in definitions)
                GetDepth(definition, depths, new HashSet<string>(StringComparer.Ordinal));

            var laneWidths = new float[branches.Count];
            float contentWidth = 18f;
            for (int branchIndex = 0; branchIndex < branches.Count; branchIndex++)
            {
                SkillBranch branch = branches[branchIndex];
                int widestRow = definitions.Where(d => d.Branch == branch)
                    .GroupBy(d => depths[d.Id]).Select(group => group.Count()).DefaultIfEmpty(1).Max();
                // A four-node tier can fit the 758px tree viewport without clipping
                // the fifth branch. Wider tiers expand proportionally and scroll.
                laneWidths[branchIndex] = Mathf.Max(130f, 18f + widestRow * 40f);
                contentWidth += laneWidths[branchIndex] + LaneGap;
            }
            contentWidth = Mathf.Max(758f, contentWidth + 8f);

            int maxDepth = depths.Count == 0 ? 0 : depths.Values.Max();
            float contentHeight = Mathf.Max(412f, 67f + maxDepth * NodeStep + NodeSize + 20f);
            treeContent.sizeDelta = new Vector2(contentWidth, contentHeight);

            var laneStarts = new float[branches.Count];
            float cursor = 13f;
            for (int i = 0; i < branches.Count; i++)
            {
                laneStarts[i] = cursor;
                BuildLaneHeader(branches[i], cursor, laneWidths[i], definitions.Count(d => d.Branch == branches[i]));
                if (i > 0) AddDivider(cursor - LaneGap * .5f, contentHeight);
                cursor += laneWidths[i] + LaneGap;
            }

            var nodePositions = new Dictionary<string, Vector2>(StringComparer.Ordinal);
            for (int branchIndex = 0; branchIndex < branches.Count; branchIndex++)
            {
                SkillBranch branch = branches[branchIndex];
                float laneX = laneStarts[branchIndex];
                float laneWidth = laneWidths[branchIndex];
                foreach (IGrouping<int, SkillDefinition> row in definitions.Where(d => d.Branch == branch)
                             .OrderBy(d => depths[d.Id]).ThenBy(d => d.Tier).ThenBy(d => d.Id)
                             .GroupBy(d => depths[d.Id]).OrderBy(group => group.Key))
                {
                    List<SkillDefinition> rowDefinitions = row.OrderBy(d => d.Tier).ThenBy(d => d.Id).ToList();
                    float spread = rowDefinitions.Count <= 1 ? 0f : Mathf.Min(40f, (laneWidth - NodeSize - 4f) / (rowDefinitions.Count - 1));
                    float center = laneX + laneWidth * .5f;
                    float y = 59f + row.Key * NodeStep;
                    for (int i = 0; i < rowDefinitions.Count; i++)
                    {
                        float x = center + (i - (rowDefinitions.Count - 1) * .5f) * spread;
                        nodePositions[rowDefinitions[i].Id] = new Vector2(x, y);
                    }
                }
            }

            // Connections render below every circular node and reflect catalog prerequisites.
            foreach (SkillDefinition definition in definitions)
            {
                if (definition.Prerequisites == null) continue;
                foreach (string prerequisiteId in definition.Prerequisites)
                {
                    if (!nodePositions.TryGetValue(prerequisiteId, out Vector2 from) || !nodePositions.TryGetValue(definition.Id, out Vector2 to))
                        continue;
                    Image line = CreateLine(treeContent, from, to, new Color32(204, 184, 139, 132), 2f);
                    connectors.Add(new ConnectorView { ParentId = prerequisiteId, ChildId = definition.Id, Image = line });
                }
            }

            foreach (SkillDefinition definition in definitions)
            {
                if (!nodePositions.TryGetValue(definition.Id, out Vector2 center)) continue;
                AddNode(definition, center);
            }

            if (treeScroll != null)
            {
                treeScroll.horizontalNormalizedPosition = 0f;
                treeScroll.verticalNormalizedPosition = 1f;
            }
        }

        private void BuildLaneHeader(SkillBranch branch, float x, float width, int count)
        {
            Text title = InkLabel(treeContent, BranchName(branch), x, 8, width, 25, 14, true);
            title.alignment = TextAnchor.MiddleCenter;
            title.color = BranchTint(branch);
            Text countText = InkLabel(treeContent, count + " técnicas", x, 33, width, 18, 10);
            countText.alignment = TextAnchor.MiddleCenter;
            countText.color = new Color32(126, 146, 143, 255);
        }

        private static Text InkLabel(Transform parent, string value, float x, float y, float width, float height, int size, bool heading = false)
        {
            Text label = JourneyMenuStyle.Label(parent, value, x, y, width, height, size, heading);
            if (JourneyMenuStyle.TitleFont != null) label.font = JourneyMenuStyle.TitleFont;
            label.fontStyle = FontStyle.Normal;
            return label;
        }

        private void AddDivider(float x, float height)
        {
            RectTransform rect = AdventureWindow.Rect(treeContent, "Separador de senda", x, 10, 1, height - 16f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color32(162, 178, 167, 44);
            image.raycastTarget = false;
        }

        private void AddNode(SkillDefinition definition, Vector2 center)
        {
            float x = center.x - NodeSize * .5f;
            float y = center.y - NodeSize * .5f;
            RectTransform root = AdventureWindow.Rect(treeContent, "Técnica " + definition.Id, x, y, NodeSize, NodeSize);

            Image ring = root.gameObject.AddComponent<Image>();
            ring.sprite = Circle();
            ring.color = BranchTint(definition.Branch);
            ring.raycastTarget = true;

            RectTransform faceRect = AdventureWindow.Rect(root, "Centro del nodo", 3f, 3f, NodeSize - 6f, NodeSize - 6f);
            Image face = faceRect.gameObject.AddComponent<Image>();
            face.sprite = Circle();
            face.color = new Color32(22, 30, 31, 255);
            face.raycastTarget = false;

            RectTransform iconRect = AdventureWindow.Rect(root, "Icono de técnica", 9f, 9f, 23f, 23f);
            Image icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = NodeIcon(definition);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            RectTransform tierRect = AdventureWindow.Rect(root, "Rango de técnica", 26f, 26f, 15f, 13f);
            Text tier = tierRect.gameObject.AddComponent<Text>();
            FarmUiStyle.Text(tier, 8);
            if (JourneyMenuStyle.TitleFont != null) tier.font = JourneyMenuStyle.TitleFont;
            tier.fontStyle = FontStyle.Normal;
            tier.text = "T" + Mathf.Clamp(definition.Tier, 1, 99);
            tier.alignment = TextAnchor.MiddleCenter;
            tier.color = new Color32(236, 222, 183, 255);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = ring;
            button.transition = Selectable.Transition.None;
            string id = definition.Id;
            button.onClick.AddListener(() => SelectSkill(id));
            nodes.Add(definition.Id, new NodeView
            {
                Definition = definition,
                Root = root,
                Ring = ring,
                Face = face,
                Icon = icon,
                Tier = tier,
                Button = button
            });
        }

        private void RefreshNode(NodeView node)
        {
            SkillStatus status = manager.GetStatus(node.Definition.Id);
            bool selected = string.Equals(node.Definition.Id, selectedSkill, StringComparison.Ordinal);
            Color tint = status == SkillStatus.Maxed
                ? new Color32(222, 191, 119, 255)
                : status == SkillStatus.Available ? new Color32(231, 191, 112, 255)
                : new Color32(104, 121, 119, 210);
            if (selected) tint = new Color32(251, 222, 152, 255);
            node.Ring.color = tint;
            node.Face.color = status == SkillStatus.Maxed
                ? new Color32(47, 40, 27, 255)
                : status == SkillStatus.Available ? new Color32(54, 45, 29, 255)
                : new Color32(22, 30, 31, 255);
            node.Icon.color = status == SkillStatus.Locked
                ? new Color(.5f, .58f, .56f, .7f)
                : new Color(1f, .92f, .73f, 1f);
            node.Tier.color = status == SkillStatus.Locked
                ? new Color32(145, 156, 151, 255)
                : new Color32(239, 220, 177, 255);
            node.Button.interactable = true;
        }

        private void SelectSkill(string id)
        {
            if (selectedSkill == id) return;
            selectedSkill = id;
            unlockFeedback.text = string.Empty;
            Refresh();
        }

        private void RefreshDetail()
        {
            SkillDefinition definition = SkillTreeCatalog.Find(selectedSkill);
            if (definition == null || manager == null)
            {
                detailTitle.text = "Elige una técnica";
                detailDescription.text = "Selecciona un nodo conectado para consultar su efecto.";
                detailRequirements.text = string.Empty;
                detailState.text = string.Empty;
                unlockButton.interactable = false;
                unlockButton.GetComponentInChildren<Text>(true).text = "SELECCIONA UN NODO";
                SetPreviewSkill(null);
                return;
            }

            SkillStatus status = manager.GetStatus(definition.Id);
            detailTitle.text = definition.Name;
            detailDescription.text = definition.Description;
            detailRequirements.text = BuildRequirementText(definition);
            detailState.text = StatusText(status, definition);
            detailState.color = status == SkillStatus.Maxed
                ? JourneyMenuStyle.Gold
                : status == SkillStatus.Available ? new Color32(245, 207, 129, 255)
                : new Color32(157, 176, 171, 255);

            Text buttonText = unlockButton.GetComponentInChildren<Text>(true);
            buttonText.text = status == SkillStatus.Maxed
                ? "APRENDIDA"
                : status == SkillStatus.Available
                    ? "DESBLOQUEAR  ·  " + definition.Cost + " P"
                    : "REQUISITOS PENDIENTES";
            unlockButton.interactable = status == SkillStatus.Available;
            unlockFeedback.color = JourneyMenuStyle.Paper;
            if (string.IsNullOrEmpty(unlockFeedback.text))
                unlockFeedback.text = status == SkillStatus.Maxed ? "Técnica dominada" : "";
            SetPreviewSkill(definition.Id);
        }

        private string BuildRequirementText(SkillDefinition definition)
        {
            string level = "NIVEL MÍNIMO " + definition.RequiredPlayerLevel;
            string prerequisites = definition.Prerequisites == null || definition.Prerequisites.Length == 0
                ? "Sin requisitos previos"
                : "Requiere " + string.Join(" · ", definition.Prerequisites.Select(id =>
                    SkillTreeCatalog.Find(id)?.Name ?? id).ToArray());
            return level + "   ·   " + definition.Cost + " PUNTOS\n" + prerequisites;
        }

        private string StatusText(SkillStatus status, SkillDefinition definition)
        {
            if (status == SkillStatus.Maxed) return "DOMINADA  ·  rango " + Mathf.Max(1, definition.MaxLevel);
            if (status == SkillStatus.Available) return "DISPONIBLE PARA APRENDER";
            var missing = new List<string>();
            if (manager != null && manager.PlayerLevel < definition.RequiredPlayerLevel)
                missing.Add("nivel " + definition.RequiredPlayerLevel);
            if (manager != null && manager.SkillPoints < definition.Cost)
                missing.Add("" + definition.Cost + " puntos");
            if (definition.Prerequisites != null)
                foreach (string id in definition.Prerequisites)
                    if (manager == null || manager.GetLevel(id) <= 0)
                        missing.Add(SkillTreeCatalog.Find(id)?.Name ?? id);
            return missing.Count > 0 ? "SELLADA  ·  " + string.Join(" / ", missing) : "SELLADA  ·  reúne puntos de destino";
        }

        private void UnlockSelected()
        {
            SkillDefinition definition = SkillTreeCatalog.Find(selectedSkill);
            if (manager == null || definition == null || !manager.TryUnlock(selectedSkill)) return;
            unlockFeedback.text = "✦  Técnica aprendida: " + definition.Name;
            unlockFeedback.color = JourneyMenuStyle.Gold;
            FarmNotificationCenter.Show("Habilidad desbloqueada: " + definition.Name);
            Refresh();
        }

        private void SetPreviewSkill(string id)
        {
            if (demonstrationPreview != null) demonstrationPreview.SetSkill(id);
        }

        private void Update()
        {
            if (IsOpen && !embedded && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }
        }

        private Color ConnectorColor(string prerequisiteId)
        {
            SkillStatus status = manager != null ? manager.GetStatus(prerequisiteId) : SkillStatus.Locked;
            return status == SkillStatus.Maxed
                ? new Color32(221, 188, 116, 220)
                : new Color32(189, 199, 179, 91);
        }

        private Sprite NodeIcon(SkillDefinition definition)
        {
            Sprite specific = Resources.Load<Sprite>("SkillIcons/" + definition.IconId);
            if (specific != null) return specific;
            switch (definition.Branch)
            {
                case SkillBranch.Fuerza: return FarmUiStyle.ItemIcon("Sword");
                case SkillBranch.Magia: return FarmUiStyle.ItemIcon("Amulet");
                case SkillBranch.Supervivencia: return FarmUiStyle.ItemIcon("Heart");
                case SkillBranch.Movilidad: return FarmUiStyle.ItemIcon("Boots");
                case SkillBranch.Caos: return FarmUiStyle.ItemIcon("Gem");
                default: return null;
            }
        }

        private static int GetDepth(SkillDefinition definition, Dictionary<string, int> cache, HashSet<string> visiting)
        {
            if (cache.TryGetValue(definition.Id, out int cached)) return cached;
            if (!visiting.Add(definition.Id)) return Mathf.Max(0, definition.Tier - 1);
            int depth = 0;
            if (definition.Prerequisites != null)
                foreach (string prerequisite in definition.Prerequisites)
                {
                    SkillDefinition parent = SkillTreeCatalog.Find(prerequisite);
                    if (parent != null) depth = Mathf.Max(depth, GetDepth(parent, cache, visiting) + 1);
                }
            visiting.Remove(definition.Id);
            cache[definition.Id] = depth;
            return depth;
        }

        private static Image CreateLine(Transform parent, Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 delta = end - start;
            GameObject root = new GameObject("Requisito conectado", typeof(RectTransform), typeof(Image));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2((start.x + end.x) * .5f, -(start.y + end.y) * .5f);
            rect.sizeDelta = new Vector2(delta.magnitude, thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-delta.y, delta.x) * Mathf.Rad2Deg);
            Image image = root.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RectTransform Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static string BranchName(SkillBranch branch)
        {
            switch (branch)
            {
                case SkillBranch.Fuerza: return "FUERZA";
                case SkillBranch.Magia: return "MAGIA";
                case SkillBranch.Supervivencia: return "SUPERVIVENCIA";
                case SkillBranch.Movilidad: return "MOVILIDAD";
                case SkillBranch.Caos: return "CAOS";
                default: return branch.ToString().ToUpperInvariant();
            }
        }

        private static Color BranchTint(SkillBranch branch)
        {
            switch (branch)
            {
                case SkillBranch.Fuerza: return new Color32(218, 151, 94, 255);
                case SkillBranch.Magia: return new Color32(156, 164, 226, 255);
                case SkillBranch.Supervivencia: return new Color32(145, 196, 142, 255);
                case SkillBranch.Movilidad: return new Color32(126, 193, 196, 255);
                case SkillBranch.Caos: return new Color32(201, 137, 161, 255);
                default: return new Color32(221, 195, 134, 255);
            }
        }

        private static Sprite Circle()
        {
            if (circleSprite != null) return circleSprite;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "SkillTree_Circle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1) * .5f, (size - 1) * .5f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(size * .5f - distance) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 64);
            circleSprite.name = "SkillTree circle";
            circleSprite.hideFlags = HideFlags.HideAndDontSave;
            return circleSprite;
        }

        private static Sprite MountainBackdrop()
        {
            if (mountainSprite != null) return mountainSprite;
            const int width = 512;
            const int height = 256;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "SkillTree_InkMountains",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    float nx = x / (float)width;
                    float ny = y / (float)height;
                    float farRidge = .29f + Mathf.Sin(nx * 9.1f + .3f) * .075f + Mathf.Sin(nx * 21.7f) * .027f;
                    float nearRidge = .18f + Mathf.Sin(nx * 8.0f + 2.1f) * .065f + Mathf.Sin(nx * 18.4f + 1.1f) * .035f;
                    float grain = (Mathf.Sin(x * 1.13f + y * 1.79f) + Mathf.Sin(x * .31f - y * .77f)) * 1.2f;
                    Color32 color;
                    if (ny < nearRidge)
                        color = new Color32((byte)Mathf.Clamp(20 + grain, 0, 255), (byte)Mathf.Clamp(29 + grain, 0, 255), (byte)Mathf.Clamp(31 + grain, 0, 255), 255);
                    else if (ny < farRidge)
                        color = new Color32((byte)Mathf.Clamp(28 + grain, 0, 255), (byte)Mathf.Clamp(40 + grain, 0, 255), (byte)Mathf.Clamp(43 + grain, 0, 255), 255);
                    else
                    {
                        float glow = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(nx, ny), new Vector2(.76f, .78f)) * 1.2f);
                        color = new Color32((byte)(31 + glow * 17 + grain), (byte)(40 + glow * 18 + grain), (byte)(43 + glow * 14 + grain), 255);
                    }
                    pixels[y * width + x] = color;
                }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            mountainSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(.5f, .5f), 100);
            mountainSprite.name = "SkillTree ink mountains";
            mountainSprite.hideFlags = HideFlags.HideAndDontSave;
            return mountainSprite;
        }

        private void OnDisable()
        {
            if (IsOpen) Close();
        }

        private void OnDestroy()
        {
            if (manager != null) manager.Changed -= Refresh;
            if (active == this) { active = null; IsOpen = false; }
            if (standaloneCanvas != null) Destroy(standaloneCanvas);
        }
    }
}
