using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    [CreateAssetMenu(menuName = "Survivor Farm/Village NPC Art")]
    public sealed class VillageNpcArtCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string Id;
            public Texture2D Idle, Walk, Work;
        }

        public Entry[] Entries = Array.Empty<Entry>();
        readonly Dictionary<string, Sprite> frames = new Dictionary<string, Sprite>();

        public Sprite Frame(string id, bool walking, bool working, int direction, int frame)
        {
            var entry = Array.Find(Entries, value => value != null && value.Id == id);
            if (entry == null) return null;
            var atlas = walking ? entry.Walk : working ? entry.Work : entry.Idle;
            if (atlas == null) atlas = entry.Idle;
            if (atlas == null || atlas.width < 32 || atlas.height < 32) return null;
            int column = Mathf.Abs(frame) % (atlas.width / 32);
            int row = Mathf.Clamp(direction, 0, Mathf.Min(2, atlas.height / 32 - 1));
            string key = atlas.GetInstanceID() + ":" + row + ":" + column;
            if (frames.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            sprite = Sprite.Create(atlas, new Rect(column * 32, atlas.height - (row + 1) * 32, 32, 32), new Vector2(.5f, 0), 16, 0, SpriteMeshType.FullRect);
            sprite.name = id + ":" + key;
            frames[key] = sprite;
            return sprite;
        }

        void OnDisable()
        {
            foreach (var sprite in frames.Values)
                if (sprite != null) { if (Application.isPlaying) Destroy(sprite); else DestroyImmediate(sprite); }
            frames.Clear();
        }
    }
}
