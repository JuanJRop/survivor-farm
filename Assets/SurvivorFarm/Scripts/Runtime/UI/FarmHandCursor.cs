using UnityEngine;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>The same pixel hand used by the proximity prompt, enlarged with nearest-neighbour sampling.</summary>
    public sealed class FarmHandCursor : MonoBehaviour
    {
        private Texture2D cursor;

        private void OnEnable()
        {
            if (Application.isBatchMode) return;
            var source = Resources.Load<Texture2D>("BackpackIcons/Hand");
            if (source == null) return;
            if (cursor == null)
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
                cursor = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Raízclara Hand Cursor", filterMode = FilterMode.Point };
                cursor.SetPixels32(enlarged); cursor.Apply();
                Destroy(readable);
            }
            Cursor.SetCursor(cursor, new Vector2(cursor.width * .5f, 3f), CursorMode.Auto);
        }

        private void OnDisable() => Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        private void OnDestroy() { if (cursor != null) Destroy(cursor); }
    }
}
