using UnityEngine;
using UnityEngine.UI;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;

namespace SurvivorFarm.Runtime.UI
{
    public sealed class FarmNotificationCenter : MonoBehaviour
    {
        private static FarmNotificationCenter instance;

        [SerializeField] private Text promptText;
        [SerializeField] private Text toolText;
        [SerializeField] private Text interactionButtonText;
        [SerializeField] private Button interactionButton;
        [SerializeField] private Text notificationText;
        [SerializeField] private Image toolIcon;
        [SerializeField] private Image[] healthHeartIcons = new Image[0];
        [SerializeField] private Image hungerFill;
        [SerializeField] private Text hungerText;
        [SerializeField] private Image swordWeaponFrame;
        [SerializeField] private Image bowWeaponFrame;
        [SerializeField] private Image hoeToolFrame;
        [SerializeField] private float notificationDuration = 3f;
        [SerializeField] private Vector2 interactionScreenOffset = new Vector2(0f, 54f);
        [SerializeField] private bool fixedInteractionButton;

        public void UseFixedInteractionButton(bool value) => fixedInteractionButton = value;
        public void ConfigureFloatingInteraction(Button button, Text label)
        {
            interactionButton = button;
            interactionButtonText = label;
            interactionButtonRect = button.GetComponent<RectTransform>();
            canvasRect = button.transform.parent as RectTransform;
            fixedInteractionButton = false;
            interactionScreenOffset = Vector2.zero;
            ConfigureCompactInteraction();
        }

        private void ConfigureCompactInteraction()
        {
            if (interactionButtonRect == null) return;
            interactionButtonRect.sizeDelta = new Vector2(48f, 26f);
            if (interactionButton.image != null) interactionButton.image.color = Color.clear;
            var key = interactionButton.transform.Find("Keycap") as RectTransform;
            if (key == null)
            {
                key = new GameObject("Keycap", typeof(RectTransform), typeof(Image), typeof(Outline)).GetComponent<RectTransform>();
                key.SetParent(interactionButton.transform, false); key.SetAsFirstSibling();
                key.anchorMin = Vector2.zero; key.anchorMax = Vector2.one;
                key.offsetMin = new Vector2(24, 2); key.offsetMax = new Vector2(-2, -2);
                var image = key.GetComponent<Image>(); image.color = new Color32(247, 237, 205, 245); image.raycastTarget = false;
                var outline = key.GetComponent<Outline>(); outline.effectColor = new Color32(66, 45, 36, 230); outline.effectDistance = new Vector2(1, -2);
            }
            if (interactionButtonText != null)
            {
                var rect = interactionButtonText.rectTransform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(24f, 2f); rect.offsetMax = new Vector2(-2f, -2f);
                interactionButtonText.text = "E";
                interactionButtonText.fontSize = 14;
                interactionButtonText.color = new Color32(64, 44, 31, 255);
                interactionButtonText.fontStyle = FontStyle.Bold;
                interactionButtonText.resizeTextForBestFit = false;
                interactionButtonText.alignment = TextAnchor.MiddleCenter;
                interactionButtonText.raycastTarget = false;
            }
            foreach (var icon in interactionButton.GetComponentsInChildren<Image>(true))
            {
                if (icon.name != "Hand Icon") continue;
                icon.gameObject.SetActive(true);
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, .5f);
                icon.rectTransform.pivot = new Vector2(0f, .5f);
                icon.rectTransform.anchoredPosition = new Vector2(0f, 0f);
                icon.rectTransform.sizeDelta = new Vector2(20f, 20f);
                icon.raycastTarget = false;
            }
            if (interactionButton.GetComponent<InteractionPromptAnimation>() == null)
                interactionButton.gameObject.AddComponent<InteractionPromptAnimation>();
        }

        public static void PulseInteraction()
        {
            if (instance != null && instance.interactionButton != null)
                instance.interactionButton.GetComponent<InteractionPromptAnimation>()?.Press();
        }

        public static Vector2 InteractionBadgeScreenSize(bool repair)
        {
            var canvas = instance != null && instance.interactionButton != null
                ? instance.interactionButton.GetComponentInParent<Canvas>() : null;
            return new Vector2(repair ? 112f : 48f, 26f) * (canvas != null ? canvas.scaleFactor : 1f);
        }

        private float hideNotificationAt;
        private RectTransform canvasRect;
        private RectTransform interactionButtonRect;
        [SerializeField] private Text[] inventorySlotTexts = new Text[0];
        [SerializeField] private Image[] inventorySlotIcons = new Image[0];
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerToolbelt toolbelt;
        [SerializeField] private Sprite[] toolSprites = new Sprite[0];

        private void Awake()
        {
            instance = this;
            interactionButtonRect = interactionButton != null ? interactionButton.GetComponent<RectTransform>() : null;
            canvasRect = interactionButton != null ? interactionButton.GetComponentInParent<Canvas>()?.GetComponent<RectTransform>() : null;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            if (inventory != null) inventory.InventoryChanged -= RefreshInventoryCounts;
            if (toolbelt != null) toolbelt.ToolChanged -= RefreshSelectedTool;
        }

        public void ConfigureGameplayBindings(PlayerInventory sourceInventory, PlayerToolbelt sourceToolbelt,
            Text[] counts, Image[] icons, Sprite[] tools)
        {
            OnDisable();
            inventory = sourceInventory;
            toolbelt = sourceToolbelt;
            inventorySlotTexts = counts;
            inventorySlotIcons = icons;
            toolSprites = tools;
            Subscribe();
        }

        public void ConfigureWeaponFrames(Image swordFrame, Image bowFrame, Image hoeFrame = null)
        {
            swordWeaponFrame = swordFrame;
            bowWeaponFrame = bowFrame;
            hoeToolFrame = hoeFrame;
            if (toolbelt != null)
            {
                RefreshSelectedTool(toolbelt.SelectedTool);
            }
        }

        private void Subscribe()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged -= RefreshInventoryCounts;
                inventory.InventoryChanged += RefreshInventoryCounts;
                RefreshInventoryCounts();
            }
            if (toolbelt != null)
            {
                toolbelt.ToolChanged -= RefreshSelectedTool;
                toolbelt.ToolChanged += RefreshSelectedTool;
                RefreshSelectedTool(toolbelt.SelectedTool);
            }
        }

        private void RefreshSelectedTool(FarmTool tool)
        {
            if (toolText != null) toolText.text = PlayerToolbelt.GetDisplayName(tool);
            int index = (int)tool;
            if (toolIcon != null && index >= 0 && index < toolSprites.Length)
                toolIcon.sprite = toolSprites[index];

            SetWeaponFrameSelected(swordWeaponFrame, tool == FarmTool.Sword);
            SetWeaponFrameSelected(bowWeaponFrame, tool == FarmTool.Bow);
            SetWeaponFrameSelected(hoeToolFrame, tool == FarmTool.Hoe);
        }

        private static void SetWeaponFrameSelected(Image frame, bool selected)
        {
            if (frame == null)
            {
                return;
            }

            frame.color = selected
                ? new Color(1f, 0.78f, 0.36f, 1f)
                : new Color(1f, 1f, 1f, 0.82f);
            frame.rectTransform.localScale = selected ? Vector3.one * 1.06f : Vector3.one;
        }

        private void RefreshInventoryCounts()
        {
            int[] counts = { inventory.CommonSeeds, inventory.MineralSeeds, inventory.MagicSeeds,
                inventory.Wood, inventory.Stone, inventory.Fruit, inventory.Coins };
            for (int i = 0; i < inventorySlotTexts.Length && i < counts.Length; i++)
            {
                bool visible = counts[i] > 0;
                if (inventorySlotTexts[i] != null) inventorySlotTexts[i].text = visible ? counts[i].ToString() : string.Empty;
                if (i < inventorySlotIcons.Length && inventorySlotIcons[i] != null) inventorySlotIcons[i].enabled = visible;
            }
        }

        private void Update()
        {
            if(notificationText != null)
            {
                var canvas=notificationText.GetComponentInParent<Canvas>();
                if(canvas!=null){var rect=notificationText.rectTransform;rect.SetParent(canvas.transform,false);rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-104);rect.sizeDelta=new Vector2(440,110);notificationText.alignment=TextAnchor.UpperCenter;notificationText.fontSize=16;notificationText.raycastTarget=false;}
            }
            if(notificationText!=null)notificationText.enabled=Time.time<hideNotificationAt&&(!InventoryPanelSystem.IsOpen||ConstructionSystem.IsPlacing);
        }

        public static void Bind(
            Text prompt,
            Text tool,
            Text interactionText,
            Button interactButton,
            Text notification,
            Image icon,
            Text[] slotTexts,
            Image[] slotIcons,
            Image[] hearts,
            Image hungerBar,
            Text hungerLabel)
        {
            FarmNotificationCenter center = EnsureInstance();
            center.promptText = prompt;
            center.toolText = tool;
            center.interactionButtonText = interactionText;
            center.interactionButton = interactButton;
            center.interactionButtonRect = interactButton != null
                ? interactButton.GetComponent<RectTransform>()
                : null;
            center.canvasRect = interactButton != null
                ? interactButton.GetComponentInParent<Canvas>()?.GetComponent<RectTransform>()
                : null;
            center.notificationText = notification;
            center.toolIcon = icon;
            center.inventorySlotTexts = slotTexts ?? new Text[0];
            center.inventorySlotIcons = slotIcons ?? new Image[0];
            center.healthHeartIcons = hearts ?? new Image[0];
            center.hungerFill = hungerBar;
            center.hungerText = hungerLabel;
            center.SetNotificationVisible(false);
            center.SetInteractionVisible(false);
        }

        public static void SetPrompt(string message)
        {
            FarmNotificationCenter center = EnsureInstance();
            if (center.promptText != null)
            {
                center.promptText.text = message;
            }
        }

        public static void Show(string message)
        {
            FarmNotificationCenter center = EnsureInstance();
            if (center.notificationText == null)
            {
                Debug.Log(message);
                return;
            }

            center.notificationText.text = message;
            center.hideNotificationAt = Time.time + center.notificationDuration;
            center.SetNotificationVisible(true);
        }

        public static void SetTool(string toolName)
        {
            FarmNotificationCenter center = EnsureInstance();
            if (center.toolText != null)
            {
                center.toolText.text = toolName;
            }
        }

        public static void SetToolIcon(Sprite sprite)
        {
            FarmNotificationCenter center = EnsureInstance();
            if (center.toolIcon != null)
            {
                center.toolIcon.sprite = sprite;
            }
        }

        public static void SetInventory(
            int commonSeeds,
            int mineralSeeds,
            int magicSeeds,
            int wood,
            int stone,
            int fruit,
            int coins,
            Sprite seedSprite,
            Sprite mineralSeedSprite,
            Sprite magicSeedSprite,
            Sprite woodSprite,
            Sprite stoneSprite,
            Sprite fruitSprite,
            Sprite coinSprite)
        {
            FarmNotificationCenter center = EnsureInstance();
            for (int i = 0; i < center.inventorySlotTexts.Length; i++)
            {
                int amount = 0;
                Sprite sprite = null;

                if (i == 0)
                {
                    amount = commonSeeds;
                    sprite = seedSprite;
                }
                else if (i == 1)
                {
                    amount = mineralSeeds;
                    sprite = mineralSeedSprite;
                }
                else if (i == 2)
                {
                    amount = magicSeeds;
                    sprite = magicSeedSprite;
                }
                else if (i == 3)
                {
                    amount = wood;
                    sprite = woodSprite;
                }
                else if (i == 4)
                {
                    amount = stone;
                    sprite = stoneSprite;
                }
                else if (i == 5)
                {
                    amount = fruit;
                    sprite = fruitSprite;
                }
                else if (i == 6)
                {
                    amount = coins;
                    sprite = coinSprite;
                }

                bool hasItem = amount > 0 && sprite != null;

                if (i < center.inventorySlotIcons.Length && center.inventorySlotIcons[i] != null)
                {
                    center.inventorySlotIcons[i].enabled = hasItem;
                    center.inventorySlotIcons[i].sprite = sprite;
                }

                if (center.inventorySlotTexts[i] != null)
                {
                    center.inventorySlotTexts[i].text = hasItem ? amount.ToString() : string.Empty;
                }
            }
        }

        public static void SetSurvival(int health, int maxHealth, float hungerPercent)
        {
            FarmNotificationCenter center = EnsureInstance();

            for (int i = 0; i < center.healthHeartIcons.Length; i++)
            {
                Image heart = center.healthHeartIcons[i];
                if (heart == null)
                {
                    continue;
                }

                heart.enabled = i < maxHealth;
                heart.color = i < health
                    ? new Color(0.92f, 0.18f, 0.20f, 1f)
                    : new Color(0.18f, 0.08f, 0.08f, 0.72f);
            }

            if (center.hungerFill != null) center.hungerFill.transform.parent.gameObject.SetActive(false);
            if (center.hungerText != null) center.hungerText.gameObject.SetActive(false);

        }

        public static void SetInteractionButton(bool visible, string label)
        {
            FarmNotificationCenter center = EnsureInstance();
            center.SetInteractionButtonLabel(label);
            center.SetInteractionVisible(visible);
        }

        public static void SetInteractionButtonAtWorldPosition(bool visible, string label, Vector3 worldPosition, Camera camera)
        {
            FarmNotificationCenter center = EnsureInstance();
            center.SetInteractionButtonLabel(label);

            if (center.fixedInteractionButton)
            {
                center.SetInteractionVisible(visible);
                return;
            }

            if (!visible || camera == null || center.interactionButtonRect == null || center.canvasRect == null)
            {
                center.SetInteractionVisible(false);
                return;
            }

            center.canvasRect = center.interactionButtonRect.parent as RectTransform;
            Vector3 screenPosition = camera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z < 0f)
            {
                center.SetInteractionVisible(false);
                return;
            }

            screenPosition += (Vector3)center.interactionScreenOffset;
            screenPosition.y += Mathf.Sin(Time.unscaledTime * 3f) * 1.5f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                center.canvasRect,
                screenPosition,
                null,
                out Vector2 localPoint);

            Rect canvasBounds = center.canvasRect.rect;
            Vector2 halfButtonSize = center.interactionButtonRect.rect.size * 0.5f;
            localPoint.x = Mathf.Clamp(localPoint.x, canvasBounds.xMin + halfButtonSize.x, canvasBounds.xMax - halfButtonSize.x);
            localPoint.y = Mathf.Clamp(localPoint.y, canvasBounds.yMin + halfButtonSize.y, canvasBounds.yMax - halfButtonSize.y);

            center.interactionButtonRect.anchoredPosition = localPoint;
            center.SetInteractionVisible(true);
        }

        private static FarmNotificationCenter EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            FarmNotificationCenter existing = FindFirstObjectByType<FarmNotificationCenter>();
            if (existing != null)
            {
                instance = existing;
                return instance;
            }

            GameObject host = new GameObject("Farm Notification Center");
            instance = host.AddComponent<FarmNotificationCenter>();
            return instance;
        }

        private void SetInteractionButtonLabel(string label)
        {
            bool repair = label == "Reparar" || label == "Mejorar";
            if (interactionButtonRect != null) interactionButtonRect.sizeDelta = new Vector2(repair ? 112f : 48f, 26f);
            if (interactionButtonText != null)
            {
                interactionButtonText.text = repair ? "E · " + label : "E";
                interactionButtonText.fontSize = repair ? 12 : 14;
                interactionButtonText.color = new Color32(64, 44, 31, 255);
            }
        }

        private void SetNotificationVisible(bool visible)
        {
            if (notificationText != null)
            {
                notificationText.enabled = visible;
            }
        }

        private void SetInteractionVisible(bool visible)
        {
            if (interactionButton != null)
            {
                interactionButton.gameObject.SetActive(visible);
                if (fixedInteractionButton) interactionButton.interactable = visible;
            }
        }
    }
}
