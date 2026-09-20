using System.Collections.Generic;
using System.Globalization;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    public static class FarmUiStyle
    {
        public static readonly Color Surface = new Color32(28, 35, 37, 255);
        public static readonly Color Control = new Color32(65, 82, 80, 255);
        public static readonly Color Ink = new Color32(246, 247, 238, 255);
        public static readonly Color Muted = new Color32(193, 211, 207, 255);
        public static readonly Color Accent = new Color32(255, 202, 106, 255);
        public static readonly Color Positive = new Color32(151, 228, 171, 255);
        public static readonly Color Negative = new Color32(255, 155, 146, 255);
        public static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private static readonly Dictionary<string, Sprite> cropIcons = new Dictionary<string, Sprite>();

        public static Sprite ItemIcon(string name)
        {
            if (cropIcons.TryGetValue(name, out var cached) && cached != null) return cached;
            if (name == "Arrow" || name == "Saddle") return MakeEquipmentIcon(name);
            if (name == "Well")
            {
                var atlas = Resources.Load<Texture2D>("StoryArt/Well");
                if (atlas != null)
                {
                    var well = Sprite.Create(atlas, new Rect(0, 0, atlas.width, atlas.height), Vector2.one * .5f, 16);
                    well.hideFlags = HideFlags.DontSave; cropIcons[name] = well; return well;
                }
            }
            var sprite = Resources.Load<Sprite>("BackpackIcons/" + name);
            if (sprite == null || !SurvivalItemCatalog.IsFood(name)) return sprite;
            // The pack's 16px crop strips end with the harvested item, not the seed.
            // Fruit-tree atlases have a different layout and must not use this rule.
            if (sprite.texture.height != 16) return sprite;
            foreach (var candidate in Resources.LoadAll<Sprite>("BackpackIcons/" + name))
                if (candidate.rect.x > sprite.rect.x) sprite = candidate;
            sprite.texture.filterMode = FilterMode.Point;
            cropIcons[name] = sprite;
            return sprite;
        }

        private static Sprite MakeEquipmentIcon(string name)
        {
            string[] rows = name == "Arrow" ? new[] {
                "................", "...........dddd.", "............ddd.", "...........dwd..",
                "..........dwd...", ".........dwd....", "........dbd.....", ".......dbd......",
                "......dbd.......", ".....dbd........", "....dbd.........", ".wwdbd..........",
                "..wbw...........", ".ww.w...........", "................", "................"
            } : new[] {
                "................", "..dd......dd....", "..dbd....dbd....", "..dbbddddbbd....",
                "..dbbbbbbbbd....", "...dbbbbbbd.....", "...dddddddd.....", "..dyyyyyyyyd....",
                "..dybbbbbbbd....", "..dybbbbbbbd....", "..dddddddddd....", "...d......d.....",
                "...d......d.....", "..dwd....dwd....", "..ddd....ddd....", "................"
            };
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = name + " icon", hideFlags = HideFlags.DontSave };
            var pixels = new Color32[256];
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
                pixels[(15 - y) * 16 + x] = rows[y][x] switch {
                    'd' => new Color32(54, 34, 29, 255), 'b' => new Color32(163, 91, 49, 255),
                    'w' => new Color32(225, 240, 228, 255), 'y' => new Color32(229, 183, 86, 255), _ => new Color32(0, 0, 0, 0)
                };
            texture.SetPixels32(pixels); texture.Apply(false, true);
            var result = Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.one * .5f, 16);
            result.name = name; result.hideFlags = HideFlags.DontSave; cropIcons[name] = result; return result;
        }

        public static void Frame(Image image, bool control = false)
        {
            image.sprite = Resources.Load<Sprite>("BackpackIcons/Panel");
            image.type = Image.Type.Sliced;
            image.color = control ? Control : Surface;
        }

        public static void Text(Text text, int size, bool fit = false)
        {
            text.font = Font;
            text.fontSize = size;
            text.color = Ink;
            text.fontStyle = size >= 22 ? FontStyle.Bold : FontStyle.Normal;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.lineSpacing = 1f;
            text.resizeTextForBestFit = fit;
            text.resizeTextMinSize = Mathf.Min(size, 14);
            text.resizeTextMaxSize = size;
            text.raycastTarget = false;
        }

        public static void Button(Button button, bool selected = false)
        {
            button.targetGraphic = button.GetComponent<Image>();
            Frame(button.image, true);
            button.image.color = selected ? new Color32(102, 119, 85, 255) : Control;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(.8f, .86f, .82f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(.68f, .72f, .72f, .8f);
            button.colors = colors;
        }

        public static void IconButton(Button button, string iconName, string caption)
        {
            Button(button);
            var label = button.GetComponentInChildren<Text>(true);
            if (label != null) label.gameObject.SetActive(false);
            var rect = AdventureWindow.Rect(button.transform, "Icono de accion", 0, 0, 26, 26);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            var icon = rect.gameObject.AddComponent<Image>();
            icon.sprite = Resources.Load<Sprite>("BackpackIcons/" + iconName);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var tooltip = button.GetComponent<HudActionTooltip>() ?? button.gameObject.AddComponent<HudActionTooltip>();
            tooltip.Caption = caption;
        }

        public static void CloseButton(Button button)
        {
            Button(button);
            var label = button.GetComponentInChildren<Text>(true);
            label.text = "\u00d7";
            Text(label, 26);
            label.alignment = TextAnchor.MiddleCenter;
            var tooltip = button.GetComponent<HudActionTooltip>() ?? button.gameObject.AddComponent<HudActionTooltip>();
            tooltip.Caption = "Cerrar [Esc]";
        }

        public static float WindowScale(Vector2 available, Vector2 size)
        {
            return Mathf.Clamp(Mathf.Min((available.x - 24) / size.x, (available.y - 24) / size.y), .1f, 1f);
        }

        public static void FitWindow(RectTransform root)
        {
            if (root != null && root.parent is RectTransform parent)
                root.localScale = Vector3.one * WindowScale(parent.rect.size, root.sizeDelta);
        }

        public static string Quantity(int value)
        {
            if (value < 10000) return value.ToString(CultureInfo.InvariantCulture);
            float divisor = value >= 1000000000 ? 1000000000f : value >= 1000000 ? 1000000f : 1000f;
            string suffix = value >= 1000000000 ? "G" : value >= 1000000 ? "M" : "k";
            return (Mathf.Floor(value / divisor * 10) / 10).ToString("0.#", CultureInfo.InvariantCulture) + suffix;
        }

        public static void Progress(Transform parent, string name, int completed, int total, float y, float width)
        {
            completed = Mathf.Clamp(completed, 0, total);
            float segmentWidth = (width - (total - 1) * 6) / Mathf.Max(1, total);
            for (int i = 0; i < total; i++)
            {
                var segment = AdventureWindow.Rect(parent, name + " " + (i + 1), i * (segmentWidth + 6), y, segmentWidth, 8);
                var image = segment.gameObject.AddComponent<Image>();
                image.sprite = Resources.Load<Sprite>("BackpackIcons/Panel");
                image.type = Image.Type.Sliced;
                image.color = i < completed ? Positive : Control;
                image.raycastTarget = false;
            }
        }

        public static ScrollRect Scroll(Transform parent, string name, float x, float y, float width, float height)
        {
            var viewport = AdventureWindow.Rect(parent, name, x, y, width, height);
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = AdventureWindow.Rect(viewport, "Contenido " + name, 0, 0, width - 12, height);
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32;
            var rail = AdventureWindow.Rect(viewport, "Barra vertical", width - 7, 0, 7, height);
            var track = rail.gameObject.AddComponent<Image>();
            track.color = Control;
            var bar = rail.gameObject.AddComponent<Scrollbar>();
            bar.direction = Scrollbar.Direction.BottomToTop;
            var handle = AdventureWindow.Rect(rail, "Deslizador", 0, 0, 7, height);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.sprite = Resources.Load<Sprite>("BackpackIcons/Panel");
            handleImage.type = Image.Type.Sliced;
            handleImage.color = Muted;
            handle.sizeDelta = Vector2.zero;
            bar.handleRect = handle;
            bar.targetGraphic = handleImage;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return scroll;
        }

        public static Text Paragraph(Transform parent, string value, float y, float width, int size = 18)
        {
            var text = AdventureWindow.Rect(parent, "Texto", 0, y, width, 32).gameObject.AddComponent<Text>();
            Text(text, size);
            text.text = value;
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Ceil(text.preferredHeight) + 6);
            return text;
        }
    }
}
