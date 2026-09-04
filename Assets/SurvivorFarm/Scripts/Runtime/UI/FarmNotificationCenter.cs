using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private float notificationDuration = 3f;
        [SerializeField] private Vector2 interactionScreenOffset = new Vector2(0f, 54f);

        private float hideNotificationAt;
        private RectTransform canvasRect;
        private RectTransform interactionButtonRect;
        private Text[] inventorySlotTexts = new Text[0];
        private Image[] inventorySlotIcons = new Image[0];

        private void Awake()
        {
            instance = this;
        }

        private void Update()
        {
            if (notificationText != null && notificationText.enabled && Time.time >= hideNotificationAt)
            {
                notificationText.enabled = false;
            }
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

            float clampedHunger = Mathf.Clamp01(hungerPercent);
            if (center.hungerFill != null)
            {
                center.hungerFill.fillAmount = clampedHunger;
                center.hungerFill.color = Color.Lerp(
                    new Color(0.78f, 0.24f, 0.12f, 1f),
                    new Color(0.30f, 0.72f, 0.30f, 1f),
                    clampedHunger);
            }

            if (center.hungerText != null)
            {
                center.hungerText.text = $"Hambre {Mathf.RoundToInt(clampedHunger * 100f)}%";
            }
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

            if (!visible || camera == null || center.interactionButtonRect == null || center.canvasRect == null)
            {
                center.SetInteractionVisible(false);
                return;
            }

            Vector3 screenPosition = camera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z < 0f)
            {
                center.SetInteractionVisible(false);
                return;
            }

            screenPosition += (Vector3)center.interactionScreenOffset;
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
            if (interactionButtonText != null)
            {
                interactionButtonText.text = label;
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
            }
        }
    }
}
