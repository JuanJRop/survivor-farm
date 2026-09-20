using System;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Original Combat FX 1.1 rows and timing, exported from the supplied Aseprite tags.</summary>
    public static class CombatFxLibrary
    {
        [Serializable] private sealed class Manifest { public int frameSize; public Clip[] clips; }
        [Serializable] private sealed class Clip { public int row; public int[] milliseconds; }
        public const int RowCount = 29;
        // Row 25 is an opaque, three-colour blade arc. Row 12 contains an authored
        // translucent blue halo, which remains soft even with point sampling.
        public const int NormalSweep = 18, HeavySweep = 27, ChargedSweep = 25, ChargeSpark = 26;
        public const int Impact = 6, StoneImpact = 7, HeavyImpact = 22;
        private static Manifest manifest;
        private static Texture2D atlas;
        private static Sprite[][] frames;
        private static bool loaded;
        public static Texture2D Atlas { get { Ensure(); return atlas; } }

        private static void Ensure()
        {
            if (loaded) return;
            loaded = true;
            atlas = Resources.Load<Texture2D>("CombatFX/Combat-Sheet");
            if (atlas != null) { atlas.filterMode = FilterMode.Point; atlas.anisoLevel = 0; }
            var data = Resources.Load<TextAsset>("CombatFX/AnimationData");
            if (atlas == null || data == null) { Debug.LogError("Missing supplied Combat FX atlas or animation data."); return; }
            manifest = JsonUtility.FromJson<Manifest>(data.text);
            if (manifest == null || manifest.clips == null || manifest.clips.Length != RowCount ||
                manifest.frameSize != 64 || atlas.width != 640 || atlas.height != 1856)
            { Debug.LogError("Combat FX source atlas or animation data has an unexpected layout."); manifest = null; return; }
            frames = new Sprite[RowCount][];
            for (int row = 0; row < RowCount; row++)
            {
                var clip = manifest.clips[row];
                frames[row] = new Sprite[clip.milliseconds.Length];
                for (int frame = 0; frame < frames[row].Length; frame++)
                {
                    var rect = new Rect(frame * 64, atlas.height - (row + 1) * 64, 64, 64);
                    var sprite = Sprite.Create(atlas, rect, new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
                    sprite.name = $"CombatFX_{row + 1:00}_{frame:00}";
                    sprite.hideFlags = HideFlags.DontSave;
                    frames[row][frame] = sprite;
                }
            }
        }

        public static int FrameCount(int row)
        { Ensure(); return frames != null && row >= 1 && row <= RowCount ? frames[row - 1].Length : 0; }

        public static Sprite Frame(int row, int frame)
        { int count = FrameCount(row); return count == 0 ? null : frames[row - 1][Mathf.Clamp(frame, 0, count - 1)]; }

        public static Sprite At(int row, float progress)
        {
            if (FrameCount(row) == 0) return null;
            var times = manifest.clips[row - 1].milliseconds;
            // This slash's first two source cells are empty anticipation frames;
            // the character already supplies anticipation before the impact cue.
            int first = row == ChargedSweep ? 2 : 0;
            int total = 0;
            for (int frame = first; frame < times.Length; frame++) total += times[frame];
            float elapsed = Mathf.Clamp01(progress) * total;
            for (int frame = first; frame < times.Length; frame++)
            { elapsed -= times[frame]; if (elapsed < 0) return Frame(row, frame); }
            return Frame(row, times.Length - 1);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            if (frames != null)
                foreach (var row in frames)
                    if (row != null) foreach (var sprite in row) if (sprite != null) UnityEngine.Object.Destroy(sprite);
            loaded = false; manifest = null; atlas = null; frames = null;
        }
    }
}
