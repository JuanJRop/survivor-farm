using System;
using System.Collections.Generic;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivorFarm.Runtime.Player
{
    /// <summary>Resolve the visible pixel under the pointer, including canopies above a small trunk collider.</summary>
    public static class WorldPointerTargeting
    {
        private static readonly Dictionary<Texture2D, byte[]> opacity = new Dictionary<Texture2D, byte[]>();
        private static readonly Dictionary<WorldInteractable, VisualEntry> visuals = new Dictionary<WorldInteractable, VisualEntry>();
        private struct VisualEntry { public List<SpriteRenderer> sprites; public float expires; }
        public static int CachedVisualCount => visuals.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            opacity.Clear(); visuals.Clear();
            SceneManager.sceneUnloaded -= SceneUnloaded;
            SceneManager.sceneUnloaded += SceneUnloaded;
        }

        private static void SceneUnloaded(Scene scene) { opacity.Clear(); visuals.Clear(); }
        public static void Forget(WorldInteractable item)
        { if (!ReferenceEquals(item, null)) visuals.Remove(item); }

        public static WorldInteractable FindAt(IReadOnlyList<WorldInteractable> candidates, Vector2 point,
            Predicate<WorldInteractable> accepts = null)
        {
            WorldInteractable best = null;
            int bestLayer = int.MinValue, bestOrder = int.MinValue;
            float bestDistance = float.PositiveInfinity;
            for (int i = 0; i < candidates.Count; i++)
            {
                var item = candidates[i];
                if (item == null || !item.isActiveAndEnabled || !item.IsAvailable || accepts != null && !accepts(item)) continue;
                if (!visuals.TryGetValue(item, out var entry) || Time.unscaledTime >= entry.expires)
                {
                    if (entry.sprites == null) entry.sprites = new List<SpriteRenderer>(4);
                    item.GetComponentsInChildren(false, entry.sprites);
                    entry.expires = Time.unscaledTime + .5f;
                    visuals[item] = entry;
                }
                foreach (var visual in entry.sprites)
                {
                    if (!ContainsVisiblePixel(visual, point)) continue;
                    int layer = SortingLayer.GetLayerValueFromID(visual.sortingLayerID);
                    float distance = ((Vector2)visual.bounds.center - point).sqrMagnitude;
                    if (layer < bestLayer || layer == bestLayer && visual.sortingOrder < bestOrder ||
                        layer == bestLayer && visual.sortingOrder == bestOrder && distance >= bestDistance) continue;
                    best = item; bestLayer = layer; bestOrder = visual.sortingOrder; bestDistance = distance;
                }
            }
            return best;
        }

        public static bool ContainsVisiblePixel(SpriteRenderer visual, Vector2 point)
        {
            if (visual == null || !visual.enabled || !visual.gameObject.activeInHierarchy || visual.sprite == null || visual.color.a <= .01f) return false;
            var sprite = visual.sprite;
            Vector3 local = visual.transform.InverseTransformPoint(new Vector3(point.x, point.y, visual.transform.position.z));
            if (visual.flipX) local.x = -local.x;
            if (visual.flipY) local.y = -local.y;
            Vector2 pixel = (Vector2)local * sprite.pixelsPerUnit + sprite.pivot;
            if (pixel.x < 0 || pixel.y < 0 || pixel.x >= sprite.rect.width || pixel.y >= sprite.rect.height) return false;
            var texture = sprite.texture;
            byte[] alpha = Alpha(texture);
            int x = Mathf.FloorToInt(sprite.rect.x + pixel.x), y = Mathf.FloorToInt(sprite.rect.y + pixel.y);
            return alpha[y * texture.width + x] > 20;
        }

        private static byte[] Alpha(Texture2D texture)
        {
            if (opacity.TryGetValue(texture, out var cached)) return cached;
            Texture2D copy = null;
            RenderTexture target = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                Texture2D readable = texture;
                if (!texture.isReadable)
                {
                    target = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
                    Graphics.Blit(texture, target);
                    RenderTexture.active = target;
                    copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                    copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                    copy.Apply(); readable = copy;
                }
                var pixels = readable.GetPixels32();
                cached = new byte[pixels.Length];
                for (int i = 0; i < pixels.Length; i++) cached[i] = pixels[i].a;
                opacity[texture] = cached;
                return cached;
            }
            finally
            {
                RenderTexture.active = previous;
                if (target != null) RenderTexture.ReleaseTemporary(target);
                if (copy != null) UnityEngine.Object.Destroy(copy);
            }
        }
    }
}
