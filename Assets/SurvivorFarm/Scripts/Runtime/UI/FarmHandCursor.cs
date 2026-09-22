using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>The same pixel hand used by the proximity prompt, enlarged with nearest-neighbour sampling.</summary>
    public sealed class FarmHandCursor : MonoBehaviour
    {
        private Texture2D cursor;
        private Texture2D swordCursor;
        private bool enemyTarget;

        public bool IsEnemyTarget => enemyTarget;

        private void OnEnable()
        {
            if (Application.isBatchMode) return;
            var source = Resources.Load<Texture2D>("BackpackIcons/Hand");
            if (source == null) return;
            if (cursor == null) cursor = Enlarge(source, "Raízclara Hand Cursor");
            if (swordCursor == null)
            {
                var sword = Resources.Load<Texture2D>("BackpackIcons/Sword");
                if (sword != null) swordCursor = Enlarge(sword, "Raízclara Sword Cursor");
            }
            ApplyCursor();
        }

        private static Texture2D Enlarge(Texture2D source, string name)
        {
            var previous = RenderTexture.active;
            var buffer = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, buffer);
            RenderTexture.active = buffer;
            var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            readable.Apply();
            RenderTexture.active = previous; RenderTexture.ReleaseTemporary(buffer);
            var pixels = readable.GetPixels32();
            int size = Mathf.Max(source.width, source.height) * 2;
            var enlarged = new Color32[size * size];
            for (int y = 0; y < source.height * 2; y++)
                for (int x = 0; x < source.width * 2; x++)
                    enlarged[y * size + x] = pixels[y / 2 * source.width + x / 2];
            var result = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = name, filterMode = FilterMode.Point };
            result.SetPixels32(enlarged); result.Apply();
            Destroy(readable);
            return result;
        }

        public void SetEnemyTarget(bool value)
        {
            if (enemyTarget == value) return;
            enemyTarget = value;
            ApplyCursor();
        }

        private void ApplyCursor()
        {
            if (cursor == null) return;
            var active = enemyTarget && swordCursor != null ? swordCursor : cursor;
            Cursor.SetCursor(active, new Vector2(active.width * .5f, 3f), CursorMode.Auto);
        }

        private void OnDisable() { enemyTarget = false; Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); }
        private void OnDestroy()
        {
            if (cursor != null) Destroy(cursor);
            if (swordCursor != null) Destroy(swordCursor);
        }
    }
}
