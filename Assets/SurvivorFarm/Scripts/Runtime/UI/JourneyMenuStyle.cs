using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>Shared ink, parchment and brass treatment for the full-screen journey menu.</summary>
    public static class JourneyMenuStyle
    {
        public static readonly Color Ink = new Color32(16, 17, 17, 255);
        public static readonly Color Paper = new Color32(219, 211, 190, 255);
        public static readonly Color Gold = new Color32(181, 154, 101, 255);
        public static readonly Color Muted = new Color32(148, 146, 135, 255);
        public static readonly Color Card = new Color32(30, 31, 29, 240);
        private static Font titleFont;
        public static Font TitleFont => titleFont != null ? titleFont :
            (titleFont = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman" }, 24));
        public static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
            => AdventureWindow.Rect(parent, name, x, y, w, h);
        public static Image Block(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false; return image;
        }
        public static Text Label(Transform parent, string value, float x, float y, float w, float h, int size = 16, bool heading = false)
        {
            Text text = MasteryWindow.Label(parent, value, x, y, w, h, size);
            text.color = Paper; text.fontStyle = FontStyle.Normal;
            if (heading && TitleFont != null) text.font = TitleFont;
            return text;
        }
        public static Button Button(Transform parent, string value, float x, float y, float w, float h, UnityAction action, bool selected = false)
        {
            Image image = Block(parent, value, x, y, w, h, selected ? Paper : Card);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1.2f, 1.15f, 1.05f);
            colors.pressedColor = new Color(.65f, .62f, .56f); colors.disabledColor = new Color(.45f, .45f, .45f, .7f);
            button.colors = colors; button.onClick.AddListener(action);
            var label = Label(image.transform, value, 10, 2, w - 20, h - 4, 16, true);
            label.alignment = TextAnchor.MiddleCenter; label.color = selected ? Ink : Paper;
            Block(image.transform, "Brass edge", 0, h - 1, w, 1, new Color(Gold.r, Gold.g, Gold.b, .38f));
            return button;
        }
        public static void Select(Button button, bool selected)
        {
            button.image.color = selected ? Paper : Color.clear;
            button.GetComponentInChildren<Text>().color = selected ? Ink : Paper;
        }
        public static void Restyle(Transform parent)
        {
            foreach (Image image in parent.GetComponentsInChildren<Image>(true))
            {
                if (image.type != Image.Type.Sliced) continue;
                image.sprite = null; image.type = Image.Type.Simple;
                image.color = image.GetComponent<Button>() != null ? new Color32(51, 48, 40, 245) : Card;
            }
            foreach (Text text in parent.GetComponentsInChildren<Text>(true))
            {
                text.color = text.fontSize >= 17 ? Paper : Muted;
                if (text.fontSize >= 20 && TitleFont != null) { text.font = TitleFont; text.fontStyle = FontStyle.Normal; }
            }
        }
    }

    /// <summary>Code-native silhouettes: no video, full-screen texture or per-frame allocation.</summary>
    public sealed class JourneyInkBackdrop : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect r = rectTransform.rect;
            for (int layer = 0; layer < 4; layer++)
            {
                Color32 shade = new Color32((byte)(27 + layer * 3), (byte)(28 + layer * 3), (byte)(26 + layer * 2), (byte)(85 - layer * 12));
                const int segments = 42;
                for (int i = 0; i < segments; i++)
                {
                    float x1 = r.xMin + r.width * i / segments, x2 = r.xMin + r.width * (i + 1) / segments;
                    float y1 = Height(i, layer, r), y2 = Height(i + 1, layer, r);
                    int start = vh.currentVertCount;
                    vh.AddVert(new Vector3(x1, r.yMin), shade, Vector2.zero); vh.AddVert(new Vector3(x1, y1), shade, Vector2.zero);
                    vh.AddVert(new Vector3(x2, y2), shade, Vector2.zero); vh.AddVert(new Vector3(x2, r.yMin), shade, Vector2.zero);
                    vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
                }
            }
        }
        private static float Height(int i, int layer, Rect r) => r.yMin + r.height *
            (.15f + layer * .12f + Mathf.Abs(Mathf.Sin(i * .34f + layer * 1.8f)) * .13f + Mathf.Sin(i * .81f) * .025f);
    }
}
