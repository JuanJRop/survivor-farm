using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>Compact player vitals presentation that replaces the authored heart row.</summary>
    public sealed class PlayerHealthBarUI : MonoBehaviour
    {
        private PlayerSurvivalStats stats;
        private PlayerInventory inventory;
        private Image[] legacyHearts;
        private RectTransform root;
        private Image fill;
        private Text value;
        private Text armor;
        private bool subscribed;

        public void Configure(PlayerSurvivalStats source, Image[] hearts)
        {
            if (subscribed) Unsubscribe();
            stats = source;
            inventory = stats != null ? stats.GetComponent<PlayerInventory>() : null;
            legacyHearts = hearts ?? new Image[0];
            HideLegacyHearts();
            EnsureVisuals();
            Subscribe();
            Layout();
            Refresh();
        }

        public void Refresh()
        {
            if (stats == null || fill == null) return;
            fill.fillAmount = stats.MaxHealth <= 0 ? 0 : Mathf.Clamp01((float)stats.CurrentHealth / stats.MaxHealth);
            if (value != null) value.text = $"{stats.CurrentHealth}/{stats.MaxHealth}";
            if (armor != null)
            {
                float percent = inventory != null ? inventory.ArmorReduction * 100f : 0f;
                armor.text = $"ARMADURA {percent:0}%";
            }
        }

        public void Layout()
        {
            if (root == null) return;
            RectTransform parent = root.parent as RectTransform;
            if (parent == null) return;
            parent.sizeDelta = new Vector2(Mathf.Max(parent.sizeDelta.x, 238f), Mathf.Max(parent.sizeDelta.y, 48f));
            root.anchorMin = root.anchorMax = new Vector2(0f, .5f);
            root.pivot = new Vector2(0f, .5f);
            root.anchoredPosition = new Vector2(11f, -1f);
            root.sizeDelta = new Vector2(Mathf.Max(206f, parent.sizeDelta.x - 22f), 32f);
        }

        private void Awake()
        {
            // OriginalSpriteHud configures this component immediately after it
            // creates it. Keeping Awake empty also makes the component safe when
            // a scene author places it directly on a HUD canvas.
        }

        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (stats != null) stats.StatsChanged += Refresh;
            if (inventory != null) inventory.InventoryChanged += Refresh;
            subscribed = stats != null || inventory != null;
        }

        private void Unsubscribe()
        {
            if (stats != null) stats.StatsChanged -= Refresh;
            if (inventory != null) inventory.InventoryChanged -= Refresh;
            subscribed = false;
        }

        private void HideLegacyHearts()
        {
            foreach (Image heart in legacyHearts)
            {
                if (heart == null) continue;
                // Keep the authored heart objects available to the legacy HUD
                // layout/tests, but hide only their graphics. Adding a
                // CanvasGroup to scene-authored prefab children can fail during
                // a domain reload, which would otherwise surface as an
                // unhandled MissingComponentException before the bar is drawn.
                Color color = heart.color;
                color.a = 0f;
                heart.color = color;
                heart.raycastTarget = false;
            }
        }

        private void EnsureVisuals()
        {
            if (root != null) return;
            RectTransform parent = null;
            foreach (Image heart in legacyHearts)
                if (heart != null) { parent = heart.transform.parent as RectTransform; break; }
            if (parent == null) return;

            root = new GameObject("Barra de vida", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(parent, false);
            Image background = ChildImage("Fondo de vida", root);
            background.color = new Color(.12f, .07f, .08f, .95f);
            background.raycastTarget = false;
            fill = ChildImage("Vida", root);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.color = new Color(.86f, .22f, .2f, 1f);
            fill.raycastTarget = false;
            value = ChildText("Valor de vida", root, 14, TextAnchor.MiddleCenter);
            value.color = Color.white;
            value.raycastTarget = false;
            armor = ChildText("Armadura", root, 10, TextAnchor.MiddleRight);
            armor.color = new Color(1f, .83f, .52f, 1f);
            armor.raycastTarget = false;
            SetRect(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            SetRect(fill.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            SetRect(value.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            value.rectTransform.offsetMin = new Vector2(2f, 0f);
            value.rectTransform.offsetMax = new Vector2(-2f, 0f);
            armor.rectTransform.anchorMin = armor.rectTransform.anchorMax = new Vector2(1f, 0f);
            armor.rectTransform.pivot = new Vector2(1f, 0f);
            armor.rectTransform.anchoredPosition = new Vector2(0f, -13f);
            armor.rectTransform.sizeDelta = new Vector2(118f, 16f);
        }

        private static Image ChildImage(string name, Transform parent)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            return image;
        }

        private static Text ChildText(string name, Transform parent, int size, TextAnchor alignment)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(parent, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = alignment;
            text.fontStyle = FontStyle.Bold;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
