using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    [CreateAssetMenu(menuName = "Survivor Farm/Player Animation Library")]
    public sealed class PlayerAnimationLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Clip
        {
            public string Name;
            public Texture2D Atlas;
            public int Width = 32, Height = 32, Frames = 1, Directions = 3;
            public bool HorizontalDirections;
            public Vector2 Pivot = new Vector2(.5f,.5f);
            public float FramesPerSecond = 10;
            public bool Loop;
        }
        public Clip[] Clips = Array.Empty<Clip>();
        private readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        public Clip Find(string name) => Array.Find(Clips, c => c.Name == name);
        public Sprite Frame(Clip clip, int direction, int frame)
        {
            if (clip == null || clip.Atlas == null) return null;
            int row = clip.Directions == 1 ? 0 : clip.Directions == 2 ? (direction == 2 ? 1 : 0) : direction;
            frame = Mathf.Clamp(frame, 0, clip.Frames - 1);
            string key = clip.Name + ":" + row + ":" + frame;
            if (cache.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            int x = (clip.HorizontalDirections ? row * clip.Frames + frame : frame) * clip.Width;
            int y = clip.Atlas.height - (clip.HorizontalDirections ? 1 : row + 1) * clip.Height;
            sprite = Sprite.Create(clip.Atlas, new Rect(x,y,clip.Width,clip.Height), clip.Pivot, 16, 0, SpriteMeshType.FullRect);
            sprite.name = key; cache[key] = sprite; return sprite;
        }
        private void OnDisable()
        {
            foreach (var sprite in cache.Values)
                if (sprite != null) { if (Application.isPlaying) Destroy(sprite); else DestroyImmediate(sprite); }
            cache.Clear();
        }
    }
}
