using System;
using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>Full-screen, data-driven skill tree. Nodes are generated from SkillTreeCatalog.</summary>
    public sealed class SkillTreeWindow : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private static SkillTreeWindow active;
        private PlayerInventory inventory;
        private SkillTreeManager manager;
        private GameObject canvasRoot;
        private RectTransform panel, body;
        private Text points, level, branchTitle, detailTitle, detailDescription, detailStatus;
        private Button unlockButton;
        private SkillBranch selectedBranch;
        private string selectedSkill;
        private readonly List<GameObject> generated = new List<GameObject>(48);
        private float nextRefresh;

        public void Configure(PlayerInventory source, Transform canvasParent)
        {
            inventory = source;
            manager = source != null ? SkillTreeManager.Ensure(source) : null;
            active = this;
            if (canvasRoot == null) Build(canvasParent);
            if (manager != null)
            {
                manager.Changed -= Refresh;
                manager.Changed += Refresh;
            }
            Refresh();
        }

        public static void OpenActive()
        {
            if (active == null)
            {
                PlayerInventory player = UnityEngine.Object.FindFirstObjectByType<PlayerInventory>();
                if (player != null)
                    active = player.GetComponent<SkillTreeWindow>() ?? player.gameObject.AddComponent<SkillTreeWindow>();
            }
            active?.Open();
        }

        public void Open()
        {
            if (FarmIntroduction.IsOpen || manager == null) return;
            if (canvasRoot == null) Build(transform.parent);
            canvasRoot.SetActive(true);
            IsOpen = true;
            Refresh();
            GetComponent<PlayerMovementController>()?.StopMovement();
            GetComponent<PlayerCombatController>()?.CancelMelee();
            FarmUiStyle.FitWindow(panel);
        }

        public void Close()
        {
            IsOpen = false;
            if (canvasRoot != null) canvasRoot.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + .25f;
                Refresh();
                FarmUiStyle.FitWindow(panel);
            }
        }

        private void Build(Transform canvasParent)
        {
            canvasRoot = MasteryWindow.CreateCanvas("Árbol de habilidades completo", 135);
            panel = AdventureWindow.Rect(canvasRoot.transform, "Árbol de habilidades completo", 0, 0, 1180, 670);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(.5f, .5f);
            panel.anchoredPosition = Vector2.zero;
            FarmUiStyle.Frame(panel.gameObject.AddComponent<Image>());
            MasteryWindow.Label(panel, "ÁRBOL DE HABILIDADES", 24, 15, 670, 36, 27);
            MasteryWindow.Label(panel, "Humanidad → Guerrero → Campeón → Superhumano → Semidiós → Dios", 24, 52, 760, 24, 14).color = FarmUiStyle.Muted;
            points = MasteryWindow.Label(panel, "", 790, 18, 300, 26, 17); points.alignment = TextAnchor.MiddleRight;
            level = MasteryWindow.Label(panel, "", 790, 47, 300, 24, 14); level.alignment = TextAnchor.MiddleRight; level.color = FarmUiStyle.Accent;
            FarmUiStyle.CloseButton(MasteryWindow.Button(panel, "×", 1120, 16, 38, 36, Close));

            float x = 24;
            for (int i = 0; i < SkillTreeCatalog.Branches.Count; i++)
            {
                SkillBranch branch = SkillTreeCatalog.Branches[i];
                Button tab = MasteryWindow.Button(panel, BranchName(branch), x + i * 224, 88, 210, 38,
                    () => { selectedBranch = branch; selectedSkill = null; Refresh(); });
                tab.name = "Skill Branch " + branch;
            }
            branchTitle = MasteryWindow.Label(panel, "", 24, 136, 1090, 30, 21);
            branchTitle.color = FarmUiStyle.Accent;
            body = AdventureWindow.Rect(panel, "Nodos", 24, 174, 1090, 292);
            detailTitle = MasteryWindow.Label(panel, "Selecciona un nodo", 24, 488, 370, 28, 19);
            detailDescription = MasteryWindow.Label(panel, "", 24, 520, 570, 60, 14); detailDescription.color = FarmUiStyle.Muted;
            detailStatus = MasteryWindow.Label(panel, "", 24, 586, 570, 25, 14);
            unlockButton = MasteryWindow.Button(panel, "DESBLOQUEAR", 610, 524, 240, 42, UnlockSelected);
            unlockButton.interactable = false;
            MasteryWindow.Label(panel, "Los nodos conectados muestran sus requisitos. Las sinergias se activan durante el combate y respetan un límite de cadena.", 610, 580, 485, 43, 12).color = FarmUiStyle.Muted;
            MasteryWindow.Label(panel, "ESC cerrar · abre este árbol desde Taller → Árbol de habilidades", 24, 635, 1090, 20, 12).color = FarmUiStyle.Muted;
        }

        private void Refresh()
        {
            if (manager == null || panel == null) return;
            points.text = "PUNTOS: " + manager.SkillPoints;
            level.text = "NIVEL " + manager.PlayerLevel + " · EXP " + manager.Experience + "/" + manager.ExperienceToNextLevel;
            branchTitle.text = BranchName(selectedBranch) + " · " + SkillTreeCatalog.All.Count(definition => definition.Branch == selectedBranch) + " nodos";
            ClearGenerated();
            List<SkillDefinition> branch = SkillTreeCatalog.All.Where(definition => definition.Branch == selectedBranch)
                .OrderBy(definition => definition.Tier).ThenBy(definition => definition.Id).ToList();
            for (int i = 0; i < branch.Count; i++) AddNode(branch[i], i);
            RefreshDetail();
        }

        private void AddNode(SkillDefinition definition, int index)
        {
            int column = index % 3;
            int row = index / 3;
            float x = column * 354;
            float y = row * 136;
            RectTransform card = AdventureWindow.Rect(body, "Nodo " + definition.Id, x, y, 330, 118);
            Image frame = card.gameObject.AddComponent<Image>();
            SkillStatus status = manager.GetStatus(definition.Id);
            frame.color = status == SkillStatus.Maxed ? new Color(.22f, .43f, .31f) : status == SkillStatus.Available ? new Color(.42f, .34f, .18f) : new Color(.18f, .23f, .27f);
            MasteryWindow.Label(card, "T" + definition.Tier + " · " + definition.Cost + " P", 10, 8, 90, 18, 12).color = FarmUiStyle.Muted;
            Text title = MasteryWindow.Label(card, definition.Name, 10, 28, 305, 25, 17);
            title.color = status == SkillStatus.Locked ? new Color(.58f, .62f, .62f) : Color.white;
            MasteryWindow.Label(card, definition.Description, 10, 56, 305, 34, 12).color = FarmUiStyle.Muted;
            Text state = MasteryWindow.Label(card, StatusText(status, definition), 10, 92, 305, 18, 11);
            state.color = status == SkillStatus.Maxed ? FarmUiStyle.Positive : status == SkillStatus.Available ? FarmUiStyle.Accent : FarmUiStyle.Muted;
            Button button = card.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(() => { selectedSkill = definition.Id; RefreshDetail(); });
            generated.Add(card.gameObject);
        }

        private void RefreshDetail()
        {
            SkillDefinition definition = SkillTreeCatalog.Find(selectedSkill);
            if (definition == null || definition.Branch != selectedBranch)
            {
                detailTitle.text = "Selecciona un nodo";
                detailDescription.text = "Cada nodo tiene coste, nivel mínimo y requisitos visibles.";
                detailStatus.text = "";
                unlockButton.interactable = false;
                return;
            }
            SkillStatus status = manager.GetStatus(definition.Id);
            detailTitle.text = definition.Name;
            detailDescription.text = definition.Description + "\n\n" + Requirements(definition);
            detailStatus.text = StatusText(status, definition);
            detailStatus.color = status == SkillStatus.Available ? FarmUiStyle.Accent : status == SkillStatus.Maxed ? FarmUiStyle.Positive : FarmUiStyle.Muted;
            unlockButton.interactable = status == SkillStatus.Available;
            unlockButton.GetComponentInChildren<Text>().text = status == SkillStatus.Maxed ? "DESBLOQUEADO" : "DESBLOQUEAR (" + definition.Cost + ")";
        }

        private void UnlockSelected()
        {
            if (manager != null && manager.TryUnlock(selectedSkill))
            {
                FarmNotificationCenter.Show("Habilidad desbloqueada: " + SkillTreeCatalog.Find(selectedSkill).Name);
                Refresh();
            }
        }

        private string Requirements(SkillDefinition definition)
        {
            string prerequisites = definition.Prerequisites == null || definition.Prerequisites.Length == 0 ? "Sin requisitos previos" :
                "Requiere: " + string.Join(", ", definition.Prerequisites.Select(id => SkillTreeCatalog.Find(id)?.Name ?? id).ToArray());
            return "Nivel mínimo " + definition.RequiredPlayerLevel + " · " + prerequisites;
        }

        private string StatusText(SkillStatus status, SkillDefinition definition)
        {
            if (status == SkillStatus.Maxed) return "DESBLOQUEADO";
            if (status == SkillStatus.Available) return "DISPONIBLE · pulsa para aprender";
            return "BLOQUEADO · " + Requirements(definition);
        }

        private static string BranchName(SkillBranch branch) => branch.ToString().ToUpperInvariant();

        private void ClearGenerated()
        {
            for (int i = body.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(body.GetChild(i).gameObject);
            generated.Clear();
        }

        private void OnDisable() => Close();

        private void OnDestroy()
        {
            if (manager != null) manager.Changed -= Refresh;
            if (active == this) active = null;
            Close();
            if (canvasRoot != null) Destroy(canvasRoot);
        }
    }
}
