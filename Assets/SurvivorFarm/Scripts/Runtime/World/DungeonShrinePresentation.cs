using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    /// <summary>Uses the pack's four candle frames and keeps the stone plinth solid.</summary>
    public sealed class DungeonShrinePresentation : MonoBehaviour
    {
        private Material glowMaterial;
        private Texture2D glowTexture;
        private Sprite glowSprite;
        private SpriteRenderer shrine;
        private readonly SpriteRenderer[] candles = new SpriteRenderer[2];

        public static void Configure(SpriteRenderer visual, DungeonArtCatalog art)
        {
            var presentation = visual.GetComponent<DungeonShrinePresentation>();
            if (presentation != null) return;
            presentation = visual.gameObject.AddComponent<DungeonShrinePresentation>();
            presentation.shrine = visual;
            var collider = visual.GetComponent<BoxCollider2D>();
            if (collider == null) collider = visual.gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;
            collider.size = new Vector2(2.65f, 1.5f);
            collider.offset = new Vector2(0, -.63f);
            var depth = visual.GetComponent<WorldSpriteDepth>();
            if (depth != null) depth.GroundOffset = -.9f * visual.transform.lossyScale.y;
            var animation = visual.gameObject.AddComponent<EnvironmentSpriteAnimation>();
            animation.Visual = visual;
            animation.FramesPerSecond = 6;
            animation.Frames = new Sprite[4];
            for (int i = 0; i < 4; i++) animation.Frames[i] = art.Slice(art.Statue, 7 + i * 64, 0, 48, 48);
            presentation.BuildLights();
        }

        private void BuildLights()
        {
            var shader = Resources.Load<Shader>("CandleGlow");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            glowMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
            // A soft radial fallback also works if a target platform cannot load the glow shader.
            glowTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                float falloff = Mathf.Clamp01(1 - Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 16);
                pixels[y * 32 + x] = new Color(1, 1, 1, falloff * falloff);
            }
            glowTexture.SetPixels(pixels); glowTexture.Apply();
            glowSprite = Sprite.Create(glowTexture, new Rect(0, 0, 32, 32), Vector2.one * .5f, 16);
            for (int i = 0; i < candles.Length; i++)
            {
                var go = new GameObject("Resplandor de vela"); go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(i == 0 ? -1.18f : 1.12f, .87f);
                go.transform.localScale = Vector3.one * 1.25f;
                var visual = go.AddComponent<SpriteRenderer>(); visual.sprite = glowSprite;
                visual.sharedMaterial = glowMaterial; candles[i] = visual;
            }
        }

        private void LateUpdate()
        {
            float pulse = .92f + .055f * Mathf.Sin(Time.time * 5.7f + transform.position.x) + .025f * Mathf.Sin(Time.time * 13);
            for (int i = 0; i < candles.Length; i++) if (candles[i] != null)
            {
                candles[i].color = new Color(1, .63f, .22f, .48f * pulse);
                candles[i].sortingOrder = shrine.sortingOrder + 1;
            }
        }

        private void OnDestroy()
        {
            if (glowMaterial != null) Destroy(glowMaterial);
            if (glowSprite != null) Destroy(glowSprite);
            if (glowTexture != null) Destroy(glowTexture);
        }
    }
}
