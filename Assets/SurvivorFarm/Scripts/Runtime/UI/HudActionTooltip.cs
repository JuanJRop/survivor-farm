using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    public sealed class HudActionTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public string Caption;
        Text label;
        RectTransform tooltip;
        Canvas canvas;

        public void OnPointerEnter(PointerEventData data)
        {
            canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (canvas == null || string.IsNullOrEmpty(Caption)) return;
            if (label == null)
            {
                var root = new GameObject("Action tooltip", typeof(RectTransform), typeof(Image));
                root.transform.SetParent(canvas.transform, false);
                tooltip = (RectTransform)root.transform;
                tooltip.anchorMin = tooltip.anchorMax = new Vector2(.5f, .5f);
                tooltip.pivot = new Vector2(.5f, 0);
                var background = root.GetComponent<Image>();
                FarmUiStyle.Frame(background);
                background.raycastTarget = false;
                label = new GameObject("Caption", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                label.transform.SetParent(root.transform, false);
                FarmUiStyle.Text(label, 16);
                label.alignment = TextAnchor.MiddleCenter;
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(12, 6);
                label.rectTransform.offsetMax = new Vector2(-12, -6);
            }
            label.text = Caption;
            tooltip.gameObject.SetActive(true);
            tooltip.SetAsLastSibling();
            var canvasRect = (RectTransform)canvas.transform;
            float width = Mathf.Clamp(label.preferredWidth + 24, 142, Mathf.Min(360, canvasRect.rect.width - 16));
            tooltip.sizeDelta = new Vector2(width, 36);
            Canvas.ForceUpdateCanvases();
            tooltip.sizeDelta = new Vector2(width, Mathf.Ceil(label.preferredHeight) + 12);
            var corners = new Vector3[4];
            ((RectTransform)transform).GetWorldCorners(corners);
            Vector3 top = canvasRect.InverseTransformPoint((corners[1] + corners[2]) * .5f);
            Vector3 bottom = canvasRect.InverseTransformPoint((corners[0] + corners[3]) * .5f);
            float y = top.y + 8;
            if (y + tooltip.sizeDelta.y > canvasRect.rect.yMax - 8) y = bottom.y - tooltip.sizeDelta.y - 8;
            float x = Mathf.Clamp(top.x, canvasRect.rect.xMin + width * .5f + 8, canvasRect.rect.xMax - width * .5f - 8);
            y = Mathf.Clamp(y, canvasRect.rect.yMin + 8, canvasRect.rect.yMax - tooltip.sizeDelta.y - 8);
            tooltip.localPosition = new Vector3(x, y, 0);
        }

        public void OnPointerExit(PointerEventData data) => Hide();
        public void OnPointerClick(PointerEventData data) => Hide();
        void LateUpdate()
        {
            if (tooltip != null && tooltip.gameObject.activeSelf && label.text != Caption) OnPointerEnter(null);
        }
        void OnDisable() => Hide();
        void OnDestroy() { if (tooltip != null) Destroy(tooltip.gameObject); }
        void Hide() { if (label != null) label.transform.parent.gameObject.SetActive(false); }
    }
}
