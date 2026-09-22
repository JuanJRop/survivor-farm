using System;
using System.Collections.Generic;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    /// <summary>
    /// Lazily slices the supplied pixel FX sheets. A sheet is loaded and sliced only
    /// when a skill actually asks for it, so the 15-part pack does not become a
    /// permanent per-scene allocation.
    /// </summary>
    public static class SkillFxLibrary
    {
        private const int CellSize = 64;
        private const int PixelsPerUnit = 16;
        private const int MaxCachedClips = 96;

        private sealed class Descriptor
        {
            public readonly string Path;
            public readonly int Row;
            public readonly float Duration;
            public readonly float Scale;
            public readonly int SortingOrder;

            public Descriptor(string path, int row, float duration, float scale, int sortingOrder = 12100)
            {
                Path = path;
                Row = row;
                Duration = duration;
                Scale = scale;
                SortingOrder = sortingOrder;
            }
        }

        public sealed class Clip
        {
            private readonly Sprite[] frames;
            public readonly float Duration;
            public readonly float Scale;
            public readonly int SortingOrder;
            public int FrameCount { get { return frames != null ? frames.Length : 0; } }

            internal Clip(Sprite[] frames, float duration, float scale, int sortingOrder)
            {
                this.frames = frames;
                Duration = duration;
                Scale = scale;
                SortingOrder = sortingOrder;
            }

            public Sprite At(float progress)
            {
                if (frames == null || frames.Length == 0) return null;
                int index = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(progress) * frames.Length), 0, frames.Length - 1);
                return frames[index];
            }
        }

        // A small set of stable action clips is shared by every player and every
        // event, avoiding a new descriptor or sprite array per attack.
        private static readonly Descriptor swordHit = new Descriptor("SkillFx/Part08/375", 0, .24f, .42f);
        private static readonly Descriptor swordHeavy = new Descriptor("SkillFx/Part15/700", 0, .34f, .66f);
        private static readonly Descriptor bowShot = new Descriptor("SkillFx/Part02/62", 2, .20f, .34f);
        private static readonly Descriptor hit = new Descriptor("SkillFx/Part03/113", 7, .22f, .36f);
        private static readonly Descriptor hurt = new Descriptor("SkillFx/Part06/273", 7, .18f, .25f);
        private static readonly Descriptor kill = new Descriptor("SkillFx/Part09/426", 7, .28f, .42f);
        private static readonly Descriptor dash = new Descriptor("SkillFx/Part08/375", 8, .25f, .40f);
        private static readonly Descriptor offensiveDash = new Descriptor("SkillFx/Part07/313", 8, .30f, .52f);
        private static readonly Descriptor shockwave = new Descriptor("SkillFx/Part14/652", 0, .42f, .78f);
        private static readonly Descriptor corpseExplosion = new Descriptor("SkillFx/Part12/566", 7, .48f, .90f);

        private static readonly Dictionary<Descriptor, Clip> cache = new Dictionary<Descriptor, Clip>(MaxCachedClips);
        private static readonly Dictionary<string, Sprite[]> framesBySheet = new Dictionary<string, Sprite[]>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Descriptor> skillDescriptors = BuildSkillDescriptors();

        public static int CachedClipCount { get { return cache.Count; } }

        public static Clip ForSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return null;
            SkillDefinition definition = SkillTreeCatalog.Find(skillId);
            if (definition == null) return null;
            if (skillDescriptors.TryGetValue(definition.EffectTag, out Descriptor descriptor)) return Get(descriptor);
            return Get(BranchFallback(definition.Branch));
        }

        public static Clip ForAttack(FarmTool tool, bool charged, bool heavy)
        {
            if (tool == FarmTool.Bow) return Get(bowShot);
            return Get(charged || heavy ? swordHeavy : swordHit);
        }

        public static Clip ForHit(SkillTreeManager manager, SkillEventContext context)
        {
            if (manager != null && context.Charged && manager.GetLevel("force_shockwave") > 0) return Get(shockwave);
            if (manager != null && context.Charged && manager.GetLevel("magic_fire") > 0) return Get(skillDescriptors["Burn"]);
            if (manager != null && context.Critical && manager.GetLevel("magic_lightning") > 0) return Get(skillDescriptors["ChainLightning"]);
            return Get(context.Charged || context.Heavy ? swordHeavy : hit);
        }

        public static Clip ForDash(SkillTreeManager manager)
        {
            return manager != null && manager.GetLevel("mobility_offensive_dash") > 0
                ? Get(offensiveDash)
                : Get(dash);
        }

        public static Clip ForEnemyKilled(SkillTreeManager manager)
        {
            return manager != null && manager.GetLevel("chaos_corpse_explosion") > 0
                ? Get(corpseExplosion)
                : Get(kill);
        }

        public static Clip ForDamageTaken() { return Get(hurt); }

        private static Clip Get(Descriptor descriptor)
        {
            if (descriptor == null || string.IsNullOrEmpty(descriptor.Path)) return null;
            string key = descriptor.Path + "#" + descriptor.Row;
            if (cache.TryGetValue(descriptor, out Clip existing) && existing != null) return existing;
            if (cache.Count >= MaxCachedClips) return null;

            if (framesBySheet.TryGetValue(key, out Sprite[] shared))
            {
                var sharedClip = new Clip(shared, descriptor.Duration, descriptor.Scale, descriptor.SortingOrder);
                cache.Add(descriptor, sharedClip);
                return sharedClip;
            }

            Texture2D texture = Resources.Load<Texture2D>(descriptor.Path);
            if (texture == null)
            {
                Debug.LogWarning("Missing skill FX sheet: " + descriptor.Path);
                return null;
            }
            texture.filterMode = FilterMode.Point;
            texture.anisoLevel = 0;
            int columns = texture.width / CellSize;
            int rows = texture.height / CellSize;
            if (columns <= 0 || rows <= 0) return null;
            int row = Mathf.Clamp(descriptor.Row, 0, rows - 1);
            Sprite[] frames = new Sprite[columns];
            for (int column = 0; column < columns; column++)
            {
                Rect rect = new Rect(column * CellSize, texture.height - (row + 1) * CellSize, CellSize, CellSize);
                Sprite frame = Sprite.Create(texture, rect, new Vector2(.5f, .5f), PixelsPerUnit, 0, SpriteMeshType.FullRect);
                frame.name = "SkillFx_" + key.Replace('/', '_').Replace('#', '_') + "_" + column.ToString("00");
                frame.hideFlags = HideFlags.DontSave;
                frames[column] = frame;
            }
            Clip clip = new Clip(frames, descriptor.Duration, descriptor.Scale, descriptor.SortingOrder);
            framesBySheet.Add(key, frames);
            cache.Add(descriptor, clip);
            return clip;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            foreach (Sprite[] frames in framesBySheet.Values)
                foreach (Sprite sprite in frames) if (sprite != null) UnityEngine.Object.Destroy(sprite);
            framesBySheet.Clear(); cache.Clear();
        }

        private static Descriptor BranchFallback(SkillBranch branch)
        {
            switch (branch)
            {
                case SkillBranch.Fuerza: return swordHeavy;
                case SkillBranch.Magia: return skillDescriptors["MagicPower"];
                case SkillBranch.Supervivencia: return skillDescriptors["Regen"];
                case SkillBranch.Movilidad: return dash;
                case SkillBranch.Caos: return corpseExplosion;
                default: return hit;
            }
        }

        private static Dictionary<string, Descriptor> BuildSkillDescriptors()
        {
            var result = new Dictionary<string, Descriptor>(StringComparer.Ordinal)
            {
                { "HeavyDamage", new Descriptor("SkillFx/Part15/700", 0, .34f, .66f) },
                { "Bleed", new Descriptor("SkillFx/Part03/113", 7, .26f, .40f) },
                { "ArmorBreak", new Descriptor("SkillFx/Part04/174", 0, .34f, .52f) },
                { "Shockwave", shockwave },
                { "Execution", new Descriptor("SkillFx/Part12/566", 7, .44f, .78f) },
                { "TitanWrath", new Descriptor("SkillFx/Part14/652", 7, .52f, 1.00f) },
                { "MagicPower", new Descriptor("SkillFx/Part01/03", 2, .46f, .60f) },
                { "Burn", new Descriptor("SkillFx/Part10/464", 7, .48f, .64f) },
                { "ChainLightning", new Descriptor("SkillFx/Part11/506", 2, .40f, .58f) },
                { "Freeze", new Descriptor("SkillFx/Part13/612", 2, .44f, .68f) },
                { "Void", new Descriptor("SkillFx/Part13/612", 1, .50f, .78f) },
                { "Apotheosis", new Descriptor("SkillFx/Part01/03", 5, .56f, .94f) },
                { "Regen", new Descriptor("SkillFx/Part14/652", 3, .42f, .52f) },
                { "Lifesteal", new Descriptor("SkillFx/Part09/426", 7, .30f, .44f) },
                { "SecondChance", new Descriptor("SkillFx/Part01/03", 3, .52f, .68f) },
                { "Devourer", new Descriptor("SkillFx/Part09/426", 3, .34f, .50f) },
                { "Immortal", new Descriptor("SkillFx/Part14/652", 5, .48f, .68f) },
                { "GodOfWar", new Descriptor("SkillFx/Part14/652", 0, .56f, .90f) },
                { "Dash", dash },
                { "DoubleDash", new Descriptor("SkillFx/Part02/62", 8, .24f, .42f) },
                { "Spectral", new Descriptor("SkillFx/Part06/273", 8, .28f, .46f) },
                { "OffensiveDash", offensiveDash },
                { "GhostStep", new Descriptor("SkillFx/Part08/375", 8, .32f, .50f) },
                { "Omnipresence", new Descriptor("SkillFx/Part02/62", 2, .42f, .64f) },
                { "DeathAura", new Descriptor("SkillFx/Part04/174", 7, .48f, .82f) },
                { "CorpseExplosion", corpseExplosion },
                { "ChainReaction", new Descriptor("SkillFx/Part03/113", 7, .46f, .74f) },
                { "DeathMark", new Descriptor("SkillFx/Part01/03", 7, .38f, .56f) },
                { "Domination", new Descriptor("SkillFx/Part10/464", 7, .42f, .62f) },
                { "DivineAscension", new Descriptor("SkillFx/Part14/652", 7, .64f, 1.10f) }
            };
            result["BrutalCombo"] = new Descriptor("SkillFx/Part15/700", 0, .28f, .62f);
            result["Earthquake"] = new Descriptor("SkillFx/Part14/652", 0, .58f, 1.15f);
            result["Carnage"] = new Descriptor("SkillFx/Part03/113", 7, .24f, .48f);
            result["MonsterStrength"] = new Descriptor("SkillFx/Part04/174", 0, .36f, .85f);
            result["FireExplosion"] = new Descriptor("SkillFx/Part10/464", 7, .52f, .88f);
            result["Meteor"] = new Descriptor("SkillFx/Part12/566", 7, .62f, 1.1f);
            result["Cataclysm"] = new Descriptor("SkillFx/Part10/464", 7, .72f, 1.35f);
            result["Thunderstorm"] = new Descriptor("SkillFx/Part11/506", 2, .48f, .88f);
            result["Overload"] = new Descriptor("SkillFx/Part11/506", 2, .28f, .60f);
            result["ThunderGod"] = new Descriptor("SkillFx/Part11/506", 2, .58f, 1.15f);
            result["IceShatter"] = new Descriptor("SkillFx/Part13/612", 2, .30f, .55f);
            result["FrostNova"] = new Descriptor("SkillFx/Part13/612", 2, .54f, .96f);
            result["Glacier"] = new Descriptor("SkillFx/Part13/612", 2, .68f, 1.25f);
            result["VoidGravity"] = new Descriptor("SkillFx/Part13/612", 1, .50f, .90f);
            result["BlackHole"] = new Descriptor("SkillFx/Part13/612", 1, .72f, 1.18f);
            result["Singularity"] = new Descriptor("SkillFx/Part13/612", 1, .84f, 1.40f);
            result["ShadowBurst"] = new Descriptor("SkillFx/Part06/273", 8, .30f, .70f);
            result["OffensiveTeleport"] = new Descriptor("SkillFx/Part07/313", 8, .32f, .92f);
            return result;
        }

    }
}
