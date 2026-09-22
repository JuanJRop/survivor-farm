using System;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>One owner for pause, navigation and embedded progression pages.</summary>
    [DefaultExecutionOrder(-1100), DisallowMultipleComponent]
    public sealed class GameMenuWindow : MonoBehaviour
    {
        public enum Page { Equipment, Workshop, Upgrades, Skills, Settings }
        public static GameMenuWindow Instance { get; private set; }
        public static bool IsOpen { get; private set; }
        public Page CurrentPage { get; private set; } = Page.Skills;
        public RectTransform Content => content;
        private PlayerInventory inventory;
        private PlayerQuickSlots quickSlots;
        private SkillTreeManager skills;
        private WorkshopProgressionWindow workshop;
        private SkillTreeWindow tree;
        private GameObject canvasRoot;
        private RectTransform frame, content, equipmentPage, settingsPage, confirmation;
        private Text status, settingsStatus;
        private Button[] tabs;
        private float previousTimeScale = 1;
        private bool wasPausedBefore;
        private bool dirty;
        private int selectedQuickSlot;

        public static GameMenuWindow Ensure(PlayerInventory source)
        {
            if (source == null) return null;
            return source.GetComponent<GameMenuWindow>() ?? source.gameObject.AddComponent<GameMenuWindow>();
        }
        public static void OpenSkillsActive() => Active()?.Open(Page.Skills);
        public static void OpenWorkshopActive() => Active()?.Open(Page.Workshop);
        public static void OpenUpgradesActive() => Active()?.Open(Page.Upgrades);
        public static void OpenEquipmentActive() => Active()?.Open(Page.Equipment);
        private static GameMenuWindow Active() => Instance != null ? Instance : Ensure(FindFirstObjectByType<PlayerInventory>());

        private void Awake()
        {
            Instance = this; IsOpen = false;
            inventory = GetComponent<PlayerInventory>();
            skills = GetComponent<SkillTreeManager>();
            if (skills != null) skills.Changed += MarkDirty;
            AudioListener.volume = PlayerPrefs.GetFloat("FarmVolume", 1);
            CombatTimeFeedback.ReducedMotion = PlayerPrefs.GetInt("FarmReducedMotion", 0) == 1;
            GameFeelFeedback.Enabled = PlayerPrefs.GetInt("Menu.Effects", 1) == 1;
            Application.targetFrameRate = PlayerPrefs.GetInt("Menu.Fps", 60);
            if (!Application.isEditor && PlayerPrefs.HasKey("Menu.Fullscreen")) Screen.fullScreen = PlayerPrefs.GetInt("Menu.Fullscreen") == 1;
            if (inventory != null) inventory.InventoryChanged += MarkDirty;
        }
        private void MarkDirty() => dirty = true;
        private void Update()
        {
            if (PlayerRespawnController.MenuOpen) return;
            var session = PortfolioSession.Instance;
            if (session != null && (!session.HasBegun || session.Phase == SlicePhase.Defeat || session.Phase == SlicePhase.Victory)) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsOpen) { if (confirmation != null) Destroy(confirmation.gameObject); else Close(); }
                else Open(CurrentPage);
            }
            else if (Input.GetKeyDown(KeyCode.K)) Toggle(Page.Skills);
            else if (Input.GetKeyDown(KeyCode.F)) Toggle(Page.Workshop);
            else if (Input.GetKeyDown(KeyCode.C)) Toggle(Page.Equipment);
            if (!IsOpen) return;
            Fit();
            if (dirty) { dirty = false; RefreshStatus(); if (CurrentPage == Page.Equipment) BuildEquipment(); }
        }
        private void Toggle(Page page) { if (IsOpen && CurrentPage == page) Close(); else Open(page); }
        public void Open(Page page = Page.Skills)
        {
            if (inventory == null || PlayerRespawnController.MenuOpen) return;
            var session = PortfolioSession.Instance;
            if (session != null && (!session.HasBegun || session.Phase == SlicePhase.Victory || session.Phase == SlicePhase.Defeat)) return;
            if (canvasRoot == null) Build();
            if (!IsOpen)
            {
                FindFirstObjectByType<InventoryPanelSystem>()?.Close();
                FindFirstObjectByType<PlayerEquipmentWindow>()?.Close();
                FindFirstObjectByType<MasteryWindow>()?.Close();
                FindFirstObjectByType<CraftingWindow>()?.CloseLegacy();
                VillageDialogueWindow.CloseActive(); SimpleShopSystem.CloseActive();
                FindFirstObjectByType<AdventureWindow>()?.Close();
                FindFirstObjectByType<VillageUpgradeWindow>()?.Close();
                inventory.GetComponent<ConstructionSystem>()?.Cancel();
                inventory.GetComponent<CombatTimeFeedback>()?.Cancel();
                previousTimeScale = Time.timeScale;
                wasPausedBefore = session != null && session.IsPaused;
                inventory.GetComponent<PlayerCombatController>()?.CancelMelee();
                inventory.GetComponent<PlayerMovementController>()?.StopMovement();
                IsOpen = true;
                if (session != null) session.Pause(true); else Time.timeScale = 0;
                canvasRoot.SetActive(true);
            }
            SelectPage(page); Fit();
        }
        public void Close()
        {
            if (!IsOpen) return;
            workshop?.HideEmbedded(); tree?.HideEmbedded();
            if (confirmation != null) Destroy(confirmation.gameObject);
            if (canvasRoot != null) canvasRoot.SetActive(false);
            IsOpen = false;
            var session = PortfolioSession.Instance;
            if (session != null) session.Pause(wasPausedBefore); else Time.timeScale = previousTimeScale;
        }
        public void SelectPage(Page page)
        {
            CurrentPage = page;
            if (confirmation != null) Destroy(confirmation.gameObject);
            workshop.HideEmbedded(); tree.HideEmbedded();
            equipmentPage.gameObject.SetActive(page == Page.Equipment);
            settingsPage.gameObject.SetActive(page == Page.Settings);
            for (int i = 0; i < tabs.Length; i++) JourneyMenuStyle.Select(tabs[i], i == (int)page);
            switch (page)
            {
                case Page.Workshop: workshop.ShowEmbedded(WorkshopProgressionWindow.Section.Shop); break;
                case Page.Upgrades: workshop.ShowEmbedded(WorkshopProgressionWindow.Section.Upgrades); break;
                case Page.Skills: tree.ShowEmbedded(); break;
                case Page.Equipment: BuildEquipment(); break;
                case Page.Settings: BuildSettings(); break;
            }
            RefreshStatus();
        }
        private void Build()
        {
            quickSlots = inventory.GetComponent<PlayerQuickSlots>(); skills = inventory.GetComponent<SkillTreeManager>();
            canvasRoot = MasteryWindow.CreateCanvas("Menú de viaje", 180);
            var background = JourneyMenuStyle.Block(canvasRoot.transform, "Fondo de tinta", 0, 0, 1280, 720, JourneyMenuStyle.Ink);
            var br = background.rectTransform; br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one; br.offsetMin = br.offsetMax = Vector2.zero; background.raycastTarget = true;
            var ink = JourneyMenuStyle.Rect(background.transform, "Montañas de tinta", 0, 0, 1280, 720);
            ink.anchorMin = Vector2.zero; ink.anchorMax = Vector2.one; ink.offsetMin = ink.offsetMax = Vector2.zero;
            ink.gameObject.AddComponent<JourneyInkBackdrop>().raycastTarget = false;
            frame = JourneyMenuStyle.Rect(canvasRoot.transform, "Viaje", 0, 0, 1280, 720);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f, .5f); frame.anchoredPosition = Vector2.zero;
            JourneyMenuStyle.Label(frame, "R A Í Z C L A R A", 50, 20, 420, 32, 25, true);
            status = JourneyMenuStyle.Label(frame, "", 540, 22, 680, 30, 15); status.alignment = TextAnchor.MiddleRight;
            string[] names = { "Equipo", "Taller", "Mejoras", "Habilidades", "Ajustes" };
            tabs = new Button[names.Length];
            for (int i = 0; i < tabs.Length; i++) { int index = i; tabs[i] = JourneyMenuStyle.Button(frame, names[i], 50 + i * 237, 65, 225, 40, () => SelectPage((Page)index)); }
            content = JourneyMenuStyle.Rect(frame, "Contenido", 50, 116, 1180, 552);
            equipmentPage = JourneyMenuStyle.Rect(content, "Equipo y cajones", 0, 0, 1180, 552);
            settingsPage = JourneyMenuStyle.Rect(content, "Ajustes de partida", 0, 0, 1180, 552);
            workshop = FindFirstObjectByType<WorkshopProgressionWindow>() ?? gameObject.AddComponent<WorkshopProgressionWindow>();
            workshop.Configure(inventory, null); workshop.Mount(content);
            tree = FindFirstObjectByType<SkillTreeWindow>() ?? gameObject.AddComponent<SkillTreeWindow>();
            tree.Configure(inventory, null); tree.Mount(content);
            JourneyMenuStyle.Block(frame, "Línea inferior", 50, 681, 1180, 1, JourneyMenuStyle.Gold);
            JourneyMenuStyle.Label(frame, "F  Taller     K  Habilidades     C  Equipo", 52, 687, 770, 23, 13).color = JourneyMenuStyle.Muted;
            JourneyMenuStyle.Button(frame, "ESC   Volver al viaje", 1000, 687, 230, 27, Close);
            canvasRoot.SetActive(false);
        }
        public void FitToViewport() { if (canvasRoot != null) Fit(); }
        private void Fit()
        {
            var r = canvasRoot.GetComponent<RectTransform>().rect;
            frame.localScale = Vector3.one * Mathf.Min(r.width / 1280f, r.height / 720f);
        }
        private void RefreshStatus()
        {
            if (status != null) status.text = $"Nivel {skills?.PlayerLevel ?? 1}    ·    {skills?.SkillPoints ?? 0} puntos de habilidad    ·    {inventory.Coins} oro";
        }
        private static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--) { var child = parent.GetChild(i); child.gameObject.SetActive(false); Destroy(child.gameObject); }
        }
        private void BuildEquipment()
        {
            Clear(equipmentPage);
            JourneyMenuStyle.Label(equipmentPage, "Preparación del viajero", 0, 4, 620, 42, 30, true);
            var health = inventory.GetComponent<PlayerSurvivalStats>();
            JourneyMenuStyle.Label(equipmentPage, $"Vida {health?.CurrentHealth ?? 0}/{health?.MaxHealth ?? 0}     Defensa {inventory.ArmorReduction:P0}     Daño de equipo +{inventory.EquipmentDamage}", 0, 49, 1180, 26, 16);
            for (int i = 0; i < EquipmentItems.SlotNames.Length; i++)
            {
                int slot = i; string id = i < inventory.EquippedEquipment.Length ? inventory.EquippedEquipment[i] : "";
                var def = EquipmentItems.Find(id);
                var button = JourneyMenuStyle.Button(equipmentPage, EquipmentItems.SlotNames[i] + "\n" + (def?.Name ?? "Vacío"), i % 4 * 290, 89 + i / 4 * 65, 275, 56, () => { inventory.Unequip(slot); });
                button.GetComponentInChildren<Text>().fontSize = 14;
                if (def != null) ItemIcon(button, def.Icon);
            }
            JourneyMenuStyle.Label(equipmentPage, "Equipo disponible · pulsa para equipar", 0, 224, 1000, 25, 18, true);
            var owned = FarmUiStyle.Scroll(equipmentPage, "Equipo disponible", 0, 258, 1180, 103);
            int n = 0;
            foreach (string id in inventory.OwnedEquipment ?? Array.Empty<string>())
            {
                var def = EquipmentItems.Find(id); if (def == null) continue; string chosen = id;
                var button = JourneyMenuStyle.Button(owned.content, def.Name, n % 4 * 286, n / 4 * 46, 273, 38, () => inventory.Equip(chosen, def.Slot));
                ItemIcon(button, def.Icon); n++;
            }
            owned.content.sizeDelta = new Vector2(1160, Mathf.Max(103, (n + 3) / 4 * 46));
            JourneyMenuStyle.Label(equipmentPage, "Cinco cajones rápidos", 0, 367, 1000, 28, 22, true);
            for (int i = 0; i < PlayerQuickSlots.SlotCount; i++)
            {
                int slot = i; string id = quickSlots?.Get(i);
                var button = JourneyMenuStyle.Button(equipmentPage, $"{i + 1}   {quickSlots?.DisplayName(id) ?? "Vacío"}", i * 236, 407, 220, 43,
                    () => { selectedQuickSlot = slot; BuildEquipment(); }, selectedQuickSlot == i);
                if (!string.IsNullOrEmpty(id)) ItemIcon(button, quickSlots.Icon(id));
            }
            JourneyMenuStyle.Label(equipmentPage, $"Asignar al cajón {selectedQuickSlot + 1}", 0, 462, 200, 30, 15);
            var picker = FarmUiStyle.Scroll(equipmentPage, "Elegir objeto", 204, 460, 960, 86);
            JourneyMenuStyle.Button(picker.content, "Vaciar", 0, 0, 170, 35, () => { quickSlots?.Clear(selectedQuickSlot); BuildEquipment(); });
            n = 1;
            if (quickSlots != null) foreach (string id in quickSlots.GetCandidates())
            {
                string chosen = id;
                JourneyMenuStyle.Button(picker.content, quickSlots.DisplayName(id), n % 5 * 186, n / 5 * 41, 174, 35,
                    () => { quickSlots.Set(selectedQuickSlot, chosen); BuildEquipment(); }); n++;
            }
            picker.content.sizeDelta = new Vector2(940, Mathf.Max(86, (n + 4) / 5 * 41));

        }
        private static void ItemIcon(Button button, string id)
        {
            Sprite sprite = FarmUiStyle.ItemIcon(id); if (sprite == null) return;
            var rect = button.GetComponent<RectTransform>();
            var icon = JourneyMenuStyle.Block(button.transform, "Objeto", 10, (rect.sizeDelta.y - 30) * .5f, 30, 30, Color.white);
            icon.sprite = sprite; icon.preserveAspect = true;
            var label = button.GetComponentInChildren<Text>().rectTransform;
            label.anchoredPosition = new Vector2(46, -2); label.sizeDelta = new Vector2(rect.sizeDelta.x - 56, rect.sizeDelta.y - 4);
        }
        private void BuildSettings()
        {
            Clear(settingsPage);
            JourneyMenuStyle.Label(settingsPage, "Ajustes", 0, 0, 620, 45, 32, true);
            JourneyMenuStyle.Label(settingsPage, "SONIDO Y PRESENTACIÓN", 0, 65, 580, 30, 18, true);
            JourneyMenuStyle.Button(settingsPage, $"Volumen  {Mathf.RoundToInt(AudioListener.volume * 100)}%  ·  Cambiar", 0, 110, 540, 46, () => { AudioListener.volume = AudioListener.volume <= 0 ? 1 : Mathf.Max(0, AudioListener.volume - .25f); PlayerPrefs.SetFloat("FarmVolume", AudioListener.volume); PlayerPrefs.Save(); BuildSettings(); });
            Button fullscreenButton = null;
            fullscreenButton = JourneyMenuStyle.Button(settingsPage, Screen.fullScreen ? "Pantalla completa · Activada" : "Pantalla completa · Desactivada", 0, 168, 540, 46, () =>
            {
                bool enabled = !Screen.fullScreen; Screen.fullScreen = enabled;
                PlayerPrefs.SetInt("Menu.Fullscreen", enabled ? 1 : 0);
                fullscreenButton.GetComponentInChildren<Text>().text = "Pantalla completa · " + (enabled ? "Activada" : "Desactivada");
            });
            JourneyMenuStyle.Button(settingsPage, $"Límite de imágenes por segundo · {Application.targetFrameRate}", 0, 226, 540, 46, () => { QualitySettings.vSyncCount = 0; Application.targetFrameRate = Application.targetFrameRate == 60 ? 120 : 60; PlayerPrefs.SetInt("Menu.Fps", Application.targetFrameRate); BuildSettings(); });
            JourneyMenuStyle.Button(settingsPage, "Movimiento de cámara · " + (CombatTimeFeedback.ReducedMotion ? "Reducido" : "Normal"), 0, 284, 540, 46, () => { CombatTimeFeedback.ReducedMotion = !CombatTimeFeedback.ReducedMotion; PlayerPrefs.SetInt("FarmReducedMotion", CombatTimeFeedback.ReducedMotion ? 1 : 0); PlayerPrefs.Save(); BuildSettings(); });
            JourneyMenuStyle.Button(settingsPage, "Efectos adicionales · " + (GameFeelFeedback.Enabled ? "Activados" : "Desactivados"), 0, 342, 540, 46, () => { GameFeelFeedback.Enabled = !GameFeelFeedback.Enabled; PlayerPrefs.SetInt("Menu.Effects", GameFeelFeedback.Enabled ? 1 : 0); BuildSettings(); });
            JourneyMenuStyle.Label(settingsPage, "PARTIDA", 640, 65, 520, 30, 18, true);
            bool canSave = PortfolioSession.Instance == null || PortfolioSession.Instance.CanSave;
            var save = FindFirstObjectByType<GameSaveSystem>();
            var saveButton = JourneyMenuStyle.Button(settingsPage, "Guardar partida", 640, 110, 520, 46, () => { save?.SaveGame(false); settingsStatus.text = save == null ? "No se encontró el sistema de guardado." : string.IsNullOrEmpty(save.LastSaveError) ? "Partida guardada." : save.LastSaveError; });
            saveButton.interactable = canSave && save != null && !save.IsSavingBlocked;
            settingsStatus = JourneyMenuStyle.Label(settingsPage, canSave ? "Guarda tu progreso antes de salir." : "Puedes guardar de día, fuera de combate y de la mazmorra.", 640, 165, 520, 64, 15);
            JourneyMenuStyle.Button(settingsPage, "Volver al menú principal", 640, 244, 520, 46, () => ConfirmExit(false));
            JourneyMenuStyle.Button(settingsPage, "Salir del juego", 640, 304, 520, 46, () => ConfirmExit(true));
            JourneyMenuStyle.Label(settingsPage, "CONTROLES\nWASD  Moverse   ·   Shift  Correr   ·   Espacio  Dash\nClic  Atacar   ·   Mantener clic  Cargar   ·   E  Interactuar\n1–5  Cajones rápidos   ·   I  Mochila", 0, 424, 1160, 111, 16);
        }
        private void ConfirmExit(bool desktop)
        {
            if (confirmation != null) Destroy(confirmation.gameObject);
            confirmation = JourneyMenuStyle.Block(frame, "Confirmar salida", 300, 225, 680, 255, JourneyMenuStyle.Ink).rectTransform;
            confirmation.GetComponent<Image>().raycastTarget = true;
            JourneyMenuStyle.Label(confirmation, desktop ? "¿Salir del juego?" : "¿Volver al menú principal?", 24, 22, 630, 44, 28, true);
            JourneyMenuStyle.Label(confirmation, "Conservarás la última partida guardada.\nEl progreso sin guardar se perderá.", 24, 82, 630, 66, 18);
            JourneyMenuStyle.Button(confirmation, "Cancelar", 24, 180, 290, 44, () => Destroy(confirmation.gameObject));
            JourneyMenuStyle.Button(confirmation, "Salir", 354, 180, 290, 44, () =>
            {
                PlayerPrefs.Save(); Close();
                if (desktop) { Application.Quit(); }
                else if (PortfolioSession.Instance != null) PortfolioSession.Instance.ReturnToTitle();
                else SceneManager.LoadScene("Main");
            });
        }
        private void OnDestroy()
        {
            if (inventory != null) inventory.InventoryChanged -= MarkDirty;
            if (skills != null) skills.Changed -= MarkDirty;
            if (Instance == this) { if (IsOpen) Time.timeScale = previousTimeScale; IsOpen = false; Instance = null; }
            if (canvasRoot != null) Destroy(canvasRoot);
        }
    }
}
