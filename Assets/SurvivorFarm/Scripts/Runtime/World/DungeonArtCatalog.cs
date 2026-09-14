using System.Collections.Generic;
using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    public sealed class DungeonArtCatalog : ScriptableObject
    {
        public Texture2D Tiles, Boxes, Door, Statue;
        public Sprite Story(string name)
        {
            var texture = Resources.Load<Texture2D>("StoryArt/" + name);
            return texture == null ? null : name == "Portal" ? Slice(texture, 96, 0, 48, 48) : Slice(texture, 0, 0, texture.width, texture.height);
        }
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        public Sprite Slice(Texture2D atlas, int x, int y, int w, int h)
        {
            if (atlas == null) return null;
            string id = atlas.name + ":" + x + ":" + y + ":" + w + ":" + h;
            if (sprites.TryGetValue(id, out var sprite) && sprite != null) return sprite;
            atlas.filterMode = FilterMode.Point;
            sprite = Sprite.Create(atlas, new Rect(x, atlas.height - y - h, w, h), new Vector2(.5f, .5f), 16);
            sprites[id] = sprite;
            return sprite;
        }
        private void OnDisable() { foreach (var sprite in sprites.Values) if (sprite != null) Destroy(sprite); sprites.Clear(); }
    }
}
