using System;
using System.Collections.Generic;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>Live bindings for the original-pack portrait HUD; layout is authored in the scene.</summary>
    public sealed class OriginalSpriteHud : MonoBehaviour
    {
        public PlayerToolbelt Toolbelt;
        public PlayerInventory Inventory;
        public PlayerSurvivalStats Stats;
        public DayNightCycle Clock;
        public Text Coins;
        public Text Day;
        public Text HungerLabel;
        public Text SelectedTool;
        public Image HungerFill;
        public Image[] Hearts;
        public Image[] ToolFrames;
        public Button[] ToolButtons;
        public Sprite FullHeart;
        public Sprite EmptyHeart;
        public Sprite Panel;
        public Sprite Slot;
        public RectTransform[] ModalPanels = new RectTransform[0];
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;
        private int displayedDay = -1;
        private int displayedMinute = -1;
        private int displayedSwordTier = -1, displayedArrows = -1;
        private bool displayedBowUnlocked;
        private Button displayedSwordButton, displayedBowButton;
        private readonly List<Image> weaponIcons = new List<Image>(4);
        private readonly List<Text> weaponLabels = new List<Text>(4);
        private float restyleUntil;
        private float nextStyleRefresh;
        private float nextWeaponRefresh;
        private static readonly Color SurvivalPanel = FarmUiStyle.Surface;
        private static readonly Color SurvivalSubPanel = FarmUiStyle.Control;
        private static readonly Color SurvivalInk = FarmUiStyle.Ink;
        private static readonly Color SurvivalMuted = FarmUiStyle.Muted;
        private static readonly Color SurvivalAccent = FarmUiStyle.Accent;
        private static readonly Color SurvivalDanger = FarmUiStyle.Negative;
        private static readonly Color SurvivalSlot = new Color(0.92f, 0.77f, 0.56f, 0.94f);

        private void Awake()
        {
            PcControlsLayout.Apply(GetComponentInParent<Canvas>());
            ApplySurvivalLayout();
            restyleUntil = Time.unscaledTime + 1.5f;
            for (int i = 0; ToolButtons != null && i < ToolButtons.Length; i++)
            {
                if (ToolButtons[i] == null || Toolbelt == null) continue;
                FarmTool tool = (FarmTool)i;
                if (tool != FarmTool.Sword && tool != FarmTool.Bow) continue;
                ToolButtons[i].onClick.AddListener(() => Toolbelt.Select(tool));
            }
            ConfigureFloatingInteractionButton();
        }

        private void OnEnable()
        {
            if (Inventory != null) Inventory.InventoryChanged += RefreshCoins;
            if (Stats != null) Stats.StatsChanged += RefreshSurvival;
            if (Toolbelt != null) Toolbelt.ToolChanged += RefreshTool;
            Refresh();
        }

        private void Start()
        {
            ConfigureFloatingInteractionButton();
            Refresh();
        }

        private void OnDisable()
        {
            if (Inventory != null) Inventory.InventoryChanged -= RefreshCoins;
            if (Stats != null) Stats.StatsChanged -= RefreshSurvival;
            if (Toolbelt != null) Toolbelt.ToolChanged -= RefreshTool;
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextWeaponRefresh) { nextWeaponRefresh = Time.unscaledTime + .2f; RefreshWeaponSlots(); }
            if (Clock != null && Day != null)
            {
                int minute = Mathf.FloorToInt(Clock.Hour * 60);
                if (displayedDay != Clock.Day || displayedMinute != minute)
                {
                    displayedDay = Clock.Day; displayedMinute = minute;
                    Day.text = $"D{displayedDay} {minute / 60:00}:{minute % 60:00}";
                }
            }
            if (restyleUntil > 0f)
            {
                if (Time.unscaledTime <= restyleUntil)
                {
                    if (Time.unscaledTime >= nextStyleRefresh)
                    {
                        nextStyleRefresh = Time.unscaledTime + .1f;
                        ApplySurvivalLayout();
                    }
                }
                else restyleUntil = 0f;
            }
            if (Screen.width <= 0 || Screen.height <= 0) return;
            Rect safe = Screen.safeArea;
            var dimensions = new Vector2Int(Screen.width, Screen.height);
            if (safe == lastSafeArea && dimensions == lastScreenSize) return;
            lastSafeArea = safe;
            lastScreenSize = dimensions;
            RectTransform rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rect.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            foreach (RectTransform modal in ModalPanels)
            {
                if (modal == null) continue;
                var parent = (RectTransform)modal.parent;
                float scale = Mathf.Min(1f, (parent.rect.width - 24f) / modal.sizeDelta.x,
                    (parent.rect.height - 24f) / modal.sizeDelta.y);
                modal.localScale = Vector3.one * Mathf.Max(.1f, scale);
            }
        }

        public void Refresh()
        {
            RefreshCoins();
            RefreshSurvival();
            if (Toolbelt != null) RefreshTool(Toolbelt.SelectedTool);
            if (Day != null) Day.text = $"DÍA {(Clock != null ? Clock.Day : 1)}";
            displayedDay = displayedMinute = -1;
        }

        public void RequestStyleRefresh()
        {
            ApplySurvivalLayout();
            restyleUntil = Time.unscaledTime + 0.75f;
        }

        private void RefreshCoins()
        {
            if (Coins != null && Inventory != null) Coins.text = FarmUiStyle.Quantity(Inventory.Coins);
        }

        private void RefreshSurvival()
        {
            if (Stats == null || Hearts == null) return;
            StyleVitals();
            for (int i = 0; i < Hearts.Length; i++)
            {
                if (Hearts[i] == null) continue;
                Hearts[i].gameObject.SetActive(i < Stats.MaxHealth);
                Hearts[i].sprite = i < Stats.CurrentHealth ? FullHeart : EmptyHeart;
                Hearts[i].color = Color.white;
            }

        }

        private void RefreshTool(FarmTool tool)
        {
            if (SelectedTool != null) SelectedTool.text = PlayerToolbelt.GetDisplayName(tool).ToUpperInvariant();
            if (ToolFrames == null) return;
            for (int i = 0; i < ToolFrames.Length; i++)
            {
                if (ToolFrames[i] == null) continue;
                bool selected = i == (int)tool;
                ToolFrames[i].sprite = selected ? Panel : Slot;
                ToolFrames[i].color = selected ? SurvivalAccent : SurvivalSlot;
            }
            RefreshWeaponSlots();
        }

        private void RefreshWeaponSlots()
        {
            if (Inventory == null || ToolButtons == null) return;
            int swordTier = Inventory.GetComponent<PlayerCombatController>()?.SwordTier ?? 1;
            bool bowUnlocked = Inventory.OwnsEquipment("Bow");
            int arrows = Inventory.GetItemCount("Arrow");
            Button swordButton = ToolButtons.Length > (int)FarmTool.Sword ? ToolButtons[(int)FarmTool.Sword] : null;
            Button bowButton = ToolButtons.Length > (int)FarmTool.Bow ? ToolButtons[(int)FarmTool.Bow] : null;
            if (displayedSwordTier == swordTier && displayedArrows == arrows && displayedBowUnlocked == bowUnlocked &&
                displayedSwordButton == swordButton && displayedBowButton == bowButton) return;
            displayedSwordTier = swordTier; displayedArrows = arrows; displayedBowUnlocked = bowUnlocked;
            displayedSwordButton = swordButton; displayedBowButton = bowButton;
            for (int i = 0; i < ToolButtons.Length; i++)
            {
                if (i != (int)FarmTool.Sword && i != (int)FarmTool.Bow || ToolButtons[i] == null) continue;
                bool bow = i == (int)FarmTool.Bow; var button = ToolButtons[i];
                button.interactable = !bow || bowUnlocked;
                button.GetComponentsInChildren(true, weaponIcons);
                foreach (var icon in weaponIcons)
                    if (icon.name.Contains("Icon"))
                    {
                        if (!bow) icon.sprite = PlayerWeaponPresentation.SwordSprite(swordTier);
                        icon.color = bow && !bowUnlocked ? new Color(.4f, .45f, .45f) : Color.white;
                    }
                var hint = button.GetComponent<HudActionTooltip>();
                if (hint != null) hint.Caption = bow ? bowUnlocked ? "Arco · apunta al cursor · " + arrows + " flechas" : "Arco bloqueado · encuéntralo en las ruinas" : "Espada · nivel " + swordTier;
                button.GetComponentsInChildren(true, weaponLabels);
                foreach (var label in weaponLabels)
                    if (label.name == "Shortcut" || label.name.EndsWith(" Key", StringComparison.Ordinal))
                        label.text = bow ? bowUnlocked ? "2 · " + arrows : "?" : "1";
            }
        }

        private void ApplySurvivalLayout()
        {
            displayedSwordTier = -1; // Styling also resets shortcut labels; repaint weapon state once afterward.
            TintPanel(Hearts != null && Hearts.Length > 0 && Hearts[0] != null ? Hearts[0].transform.parent as RectTransform : null);
            TintPanel(Day != null ? Day.transform.parent as RectTransform : null);
            TintPanel(FindRect("Mission"));
            TintPanel(ToolButtons != null && ToolButtons.Length > 0 && ToolButtons[0] != null ? ToolButtons[0].transform.parent as RectTransform : null);
            SetTextColor(transform, SurvivalInk);
            StyleReadableText(transform);
            StyleVitals();
            StyleWallet();
            StyleContextPanel();
            StyleHudActionButton("HUD Backpack", "Backpack", "Mochila [I]", new Vector2(16f, 16f), new Vector2(44f, 44f));
            StyleHudActionButton("Personaje [C]", "Helmet", "Personaje [C]", new Vector2(68f, 16f), new Vector2(44f, 44f));
            StyleHudActionButton("Abrir taller", "Workbench", "Taller [F]", new Vector2(120f, 16f), new Vector2(44f, 44f));
            StyleHudActionButton("Diario [J]", "Quest", "Diario [J]", new Vector2(172f, 16f), new Vector2(44f, 44f));
            StyleHudSettingsButton();

            RectTransform toolbar = ToolButtons != null && ToolButtons.Length > 0 && ToolButtons[0] != null
                ? ToolButtons[0].transform.parent as RectTransform
                : null;
            if (toolbar != null)
            {
                toolbar.anchorMin = toolbar.anchorMax = toolbar.pivot = new Vector2(0.5f, 0f);
                toolbar.anchoredPosition = new Vector2(0f, 12f);
                toolbar.sizeDelta = new Vector2(140f, 64f);
            }

            for (int i = 0; ToolButtons != null && i < ToolButtons.Length; i++)
            {
                Button button = ToolButtons[i];
                if (button == null) continue;
                bool visibleTool = i == (int)FarmTool.Sword || i == (int)FarmTool.Bow;
                button.gameObject.SetActive(visibleTool);
                if (!visibleTool) continue;
                int visibleIndex = i == (int)FarmTool.Sword ? 0 : i == (int)FarmTool.Bow ? 1 : 2;

                RectTransform rect = button.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                    rect.anchoredPosition = new Vector2(9f + visibleIndex * 64f, -6f);
                    rect.sizeDelta = new Vector2(58f, 52f);
                }
                StyleToolSlot(button, visibleIndex + 1);
                var tooltip = button.GetComponent<HudActionTooltip>() ?? button.gameObject.AddComponent<HudActionTooltip>();
                tooltip.Caption = PlayerToolbelt.GetDisplayName((FarmTool)i);
            }

            RectTransform selected = SelectedTool != null ? SelectedTool.transform.parent as RectTransform : null;
            if (selected != null)
            {
                selected.gameObject.SetActive(false);
                selected.anchorMin = selected.anchorMax = selected.pivot = new Vector2(0.5f, 0f);
                selected.anchoredPosition = new Vector2(0f, 98f);
                selected.sizeDelta = new Vector2(192f, 30f);
                TintPanel(selected);
                SelectedTool.fontSize = 15;
                SelectedTool.alignment = TextAnchor.MiddleCenter;
                SelectedTool.resizeTextForBestFit = true;
                SelectedTool.resizeTextMinSize = 11;
                SelectedTool.resizeTextMaxSize = 15;
                SelectedTool.rectTransform.anchorMin = Vector2.zero;
                SelectedTool.rectTransform.anchorMax = Vector2.one;
                SelectedTool.rectTransform.offsetMin = new Vector2(8f, 3f);
                SelectedTool.rectTransform.offsetMax = new Vector2(-8f, -3f);
            }

            RectTransform mission = FindRect("Mission");
            if (mission != null)
            {
                mission.anchorMin = mission.anchorMax = mission.pivot = new Vector2(1f, 1f);
                mission.anchoredPosition = new Vector2(-12f, -92f);
                mission.sizeDelta = new Vector2(284f, 76f);
                StyleMissionPanel(mission);
            }
            if (Stats != null) RefreshSurvival();
            if (Toolbelt != null) RefreshTool(Toolbelt.SelectedTool);
        }

        private void ConfigureFloatingInteractionButton()
        {
            FarmNotificationCenter center = FindFirstObjectByType<FarmNotificationCenter>();
            Button button = FindButton("HUD Interact");
            if (center == null || button == null) return;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.text = "Interactuar";
                label.color = SurvivalInk;
                label.alignment = TextAnchor.MiddleCenter;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 10;
                label.resizeTextMaxSize = 14;
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(36f, 3f);
                label.rectTransform.offsetMax = new Vector2(-7f, -3f);
            }

            Image image = button.GetComponent<Image>();
            if (image != null) image.color = new Color(0.08f, 0.06f, 0.05f, 0.96f);
            Image handIcon = FindChildImage(button.transform, "Hand Icon");
            if (handIcon != null)
            {
                handIcon.gameObject.SetActive(true);
                RectTransform handRect = handIcon.rectTransform;
                handRect.anchorMin = handRect.anchorMax = handRect.pivot = new Vector2(0f, .5f);
                handRect.anchoredPosition = new Vector2(6f, 0f);
                handRect.sizeDelta = new Vector2(24f, 24f);
            }
            RectTransform rect = button.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(180f, 38f);
            }

            center.ConfigureFloatingInteraction(button, label);
        }

        private void StyleVitals()
        {
            RectTransform vitals = Hearts != null && Hearts.Length > 0 && Hearts[0] != null
                ? Hearts[0].transform.parent as RectTransform
                : null;
            if (vitals == null) return;

            vitals.anchorMin = vitals.anchorMax = vitals.pivot = new Vector2(0f, 1f);
            vitals.anchoredPosition = new Vector2(12f, -12f);
            int heartCount = Stats != null ? Mathf.Min(Hearts.Length, Stats.MaxHealth) : 5;
            vitals.sizeDelta = new Vector2(Mathf.Max(154f, 24f + heartCount * 26f), 44f);
            TintPanel(vitals);

            for (int i = 0; Hearts != null && i < Hearts.Length; i++)
            {
                if (Hearts[i] == null) continue;
                RectTransform rect = Hearts[i].rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(12f + i * 26f, -8f);
                rect.sizeDelta = new Vector2(24f, 24f);
            }

            if (HungerLabel != null) HungerLabel.gameObject.SetActive(false);
            if (HungerFill != null) HungerFill.transform.parent.gameObject.SetActive(false);
            var hungerIcon = vitals.Find("Hunger Icon");
            if (hungerIcon != null) hungerIcon.gameObject.SetActive(false);

        }

        private void StyleWallet()
        {
            RectTransform wallet = Day != null ? Day.transform.parent as RectTransform : null;
            if (wallet == null) return;

            wallet.anchorMin = wallet.anchorMax = wallet.pivot = new Vector2(1f, 1f);
            wallet.anchoredPosition = new Vector2(-12f, -12f);
            wallet.sizeDelta = new Vector2(146f, 72f);
            TintPanel(wallet);

            if (Day != null)
            {
                Day.fontSize = 17;
                Day.fontStyle = FontStyle.Bold;
                Day.alignment = TextAnchor.MiddleLeft;
                Day.rectTransform.anchorMin = Day.rectTransform.anchorMax = Day.rectTransform.pivot = new Vector2(0f, 1f);
                Day.rectTransform.anchoredPosition = new Vector2(16f, -12f);
                Day.rectTransform.sizeDelta = new Vector2(126f, 27f);
            }

            Text coins = FindText("Coins");
            if (coins != null)
            {
                coins.fontSize = 20;
                coins.fontStyle = FontStyle.Bold;
                coins.alignment = TextAnchor.MiddleLeft;
                coins.rectTransform.anchorMin = coins.rectTransform.anchorMax = coins.rectTransform.pivot = new Vector2(0f, 1f);
                coins.rectTransform.anchoredPosition = new Vector2(50f, -39f);
                coins.rectTransform.sizeDelta = new Vector2(82f, 24f);
            }

            Image coin = FindChildImage(wallet, "Coin");
            if (coin != null)
            {
                coin.rectTransform.anchorMin = coin.rectTransform.anchorMax = coin.rectTransform.pivot = new Vector2(0f, 1f);
                coin.rectTransform.anchoredPosition = new Vector2(18f, -38f);
                coin.rectTransform.sizeDelta = new Vector2(26f, 26f);
            }
        }

        private void StyleMissionPanel(RectTransform mission)
        {
            TintPanel(mission);
            Image icon = FindChildImage(mission, "Quest Icon");
            if (icon != null)
            {
                icon.color = Color.white;
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = icon.rectTransform.pivot = new Vector2(0f, 1f);
                icon.rectTransform.anchoredPosition = new Vector2(12f, -12f);
                icon.rectTransform.sizeDelta = new Vector2(24f, 24f);
            }

            Text quest = FindText("Quest Text");
            if (quest == null) return;
            quest.color = SurvivalInk;
            quest.fontSize = 14;
            quest.fontStyle = FontStyle.Normal;
            quest.lineSpacing = 1f;
            quest.supportRichText = true;
            quest.resizeTextForBestFit = false;
            quest.resizeTextMinSize = 14;
            quest.resizeTextMaxSize = 14;
            quest.alignment = TextAnchor.UpperLeft;
            quest.horizontalOverflow = HorizontalWrapMode.Wrap;
            quest.verticalOverflow = VerticalWrapMode.Truncate;
            quest.rectTransform.anchorMin = quest.rectTransform.anchorMax = quest.rectTransform.pivot = new Vector2(0f, 1f);
            quest.rectTransform.anchoredPosition = new Vector2(44f, -10f);
            quest.rectTransform.sizeDelta = new Vector2(228f, 58f);
        }

        private void StyleContextPanel()
        {
            RectTransform context = FindRect("Context");
            if (context != null)
            {
                context.gameObject.SetActive(false);
                context.anchorMin = context.anchorMax = context.pivot = new Vector2(0f, 0f);
                context.anchoredPosition = new Vector2(16f, 132f);
                context.sizeDelta = new Vector2(320f, 54f);
                TintPanel(context, SurvivalSubPanel);
            }

            Text prompt = FindText("Context Text");
            if (prompt != null)
            {
                prompt.color = SurvivalInk;
                prompt.fontSize = 14;
                prompt.fontStyle = FontStyle.Bold;
                prompt.alignment = TextAnchor.MiddleLeft;
                prompt.resizeTextForBestFit = true;
                prompt.resizeTextMinSize = 10;
                prompt.resizeTextMaxSize = 14;
                prompt.rectTransform.anchorMin = Vector2.zero;
                prompt.rectTransform.anchorMax = Vector2.one;
                prompt.rectTransform.offsetMin = new Vector2(12f, 6f);
                prompt.rectTransform.offsetMax = new Vector2(-12f, -6f);
            }
        }

        private void StyleHudActionButton(string objectName, string iconName, string caption, Vector2 anchoredPosition, Vector2 size)
        {
            Button button = FindButton(objectName);
            if (button == null) return;

            RectTransform rect = button.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0f);
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = size;
            }

            StyleButtonChrome(button, caption, iconName, 14);
            CompactButton(button, caption);
        }

        private void StyleHudSettingsButton()
        {
            Button button = FindButton("HUD Settings");
            if (button == null) return;

            RectTransform rect = button.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-16f, 16f);
                rect.sizeDelta = new Vector2(44f, 44f);
            }

            StyleButtonChrome(button, "Ajustes", "Workbench", 13);
            CompactButton(button, "Ajustes");
            var settingsIcon = FindChildImage(button.transform, "HUD Icon");
            if (settingsIcon != null) settingsIcon.sprite = HouseSprites.Slice("HudSymbols", 144, 0, 16, 16) ?? settingsIcon.sprite;
        }

        private void CompactButton(Button button, string caption)
        {
            var icon = FindChildImage(button.transform, "HUD Icon");
            if (icon != null)
            {
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = icon.rectTransform.pivot = new Vector2(.5f, .5f);
                icon.rectTransform.anchoredPosition = Vector2.zero;
                icon.rectTransform.sizeDelta = new Vector2(28, 28);
            }
            var label = button.GetComponentInChildren<Text>(true);
            if (label != null) label.gameObject.SetActive(false);
            var tooltip = button.GetComponent<HudActionTooltip>() ?? button.gameObject.AddComponent<HudActionTooltip>();
            tooltip.Caption = caption;
        }

        private void StyleButtonChrome(Button button, string caption, string iconName, int fontSize)
        {
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = Panel != null ? Panel : image.sprite;
                if (image.sprite != null) image.type = Image.Type.Sliced;
                image.color = SurvivalSubPanel;
            }

            Image icon = EnsureButtonIcon(button.transform, iconName);
            if (icon != null)
            {
                RectTransform iconRect = icon.rectTransform;
                iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(22f, 0f);
                iconRect.sizeDelta = new Vector2(25f, 25f);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.color = Color.white;
            }

            Text label = button.GetComponentInChildren<Text>(true);
            if (label == null) return;
            label.text = caption;
            label.color = SurvivalInk;
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = fontSize;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(42f, 3f);
            label.rectTransform.offsetMax = new Vector2(-8f, -3f);
            EnsureOutline(label);
        }

        private void StyleToolSlot(Button button, int shortcut)
        {
            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                if (image.sprite != null) image.type = Image.Type.Sliced;
                image.color = SurvivalSlot;
            }

            foreach (Image child in button.GetComponentsInChildren<Image>(true))
            {
                if (child.transform == button.transform) continue;
                if (!child.name.Contains("Icon")) continue;
                child.preserveAspect = true;
                child.raycastTarget = false;
                child.color = Color.white;
                child.rectTransform.anchorMin = child.rectTransform.anchorMax = child.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                child.rectTransform.anchoredPosition = new Vector2(0f, 5f);
                child.rectTransform.sizeDelta = new Vector2(38f, 38f);
            }

            Text key = null;
            foreach (Text text in button.GetComponentsInChildren<Text>(true))
            {
                if (text.name == "Shortcut" || text.name.EndsWith(" Key", StringComparison.Ordinal))
                {
                    key = text;
                    break;
                }
            }
            if (key == null) return;
            key.text = shortcut.ToString();
            key.color = SurvivalAccent;
            key.fontSize = 11;
            key.fontStyle = FontStyle.Bold;
            key.alignment = TextAnchor.LowerRight;
            key.resizeTextForBestFit = false;
            key.rectTransform.anchorMin = Vector2.zero;
            key.rectTransform.anchorMax = Vector2.one;
            key.rectTransform.offsetMin = new Vector2(4f, 2f);
            key.rectTransform.offsetMax = new Vector2(-6f, -4f);
            EnsureOutline(key);
        }

        private Image EnsureButtonIcon(Transform button, string iconName)
        {
            Image icon = FindChildImage(button, "HUD Icon");
            if (icon == null)
            {
                foreach (Image child in button.GetComponentsInChildren<Image>(true))
                {
                    if (child.transform == button) continue;
                    if (child.name.EndsWith(" Icon", StringComparison.Ordinal))
                    {
                        icon = child;
                        break;
                    }
                }
            }

            if (icon == null)
            {
                icon = new GameObject("HUD Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(button, false);
            }

            icon.name = "HUD Icon";
            icon.sprite = Resources.Load<Sprite>("BackpackIcons/" + iconName) ?? icon.sprite;
            icon.gameObject.SetActive(true);
            return icon;
        }

        private Image EnsureHudIcon(Transform parent, string objectName, string iconName, Vector2 anchoredPosition, Vector2 size)
        {
            Image icon = FindChildImage(parent, objectName);
            if (icon == null)
            {
                icon = new GameObject(objectName, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(parent, false);
            }
            icon.sprite = Resources.Load<Sprite>("BackpackIcons/" + iconName) ?? icon.sprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = Color.white;
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = icon.rectTransform.pivot = new Vector2(0f, 1f);
            icon.rectTransform.anchoredPosition = anchoredPosition;
            icon.rectTransform.sizeDelta = size;
            return icon;
        }

        private void StyleReadableText(Transform root)
        {
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                text.font = FarmUiStyle.Font;
                text.fontStyle = FontStyle.Bold;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                EnsureOutline(text);
            }
        }

        private void EnsureOutline(Text text)
        {
            if (text == null || text.GetComponent<Outline>() != null) return;
            Outline outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.04f, 0.025f, 0.015f, 0.92f);
            outline.effectDistance = new Vector2(.7f, -.7f);
            outline.useGraphicAlpha = true;
        }

        private void TintPanel(RectTransform rect)
        {
            TintPanel(rect, SurvivalPanel);
        }

        private void TintPanel(RectTransform rect, Color color)
        {
            if (rect == null) return;
            Image image = rect.GetComponent<Image>();
            if (image == null) return;
            image.sprite = Panel != null ? Panel : image.sprite;
            if (image.sprite != null) image.type = Image.Type.Sliced;
            image.color = color;
        }

        private void SetTextColor(Transform root, Color color)
        {
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                text.color = color;
            }
        }

        private RectTransform FindRect(string objectName)
        {
            foreach (RectTransform rect in GetComponentsInChildren<RectTransform>(true))
                if (rect.name == objectName) return rect;
            return null;
        }

        private Button FindButton(string objectName)
        {
            foreach (Button button in GetComponentsInChildren<Button>(true))
                if (button.name == objectName) return button;
            return null;
        }

        private Text FindText(string objectName)
        {
            foreach (Text text in GetComponentsInChildren<Text>(true))
                if (text.name == objectName) return text;
            return null;
        }

        private Image FindChildImage(Transform root, string objectName)
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
                if (image.name == objectName) return image;
            return null;
        }

        private void MoveButton(string objectName, Vector2 anchoredPosition, Vector2 size)
        {
            Button button = FindButton(objectName);
            RectTransform rect = button != null ? button.transform as RectTransform : null;
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }
    }
}
