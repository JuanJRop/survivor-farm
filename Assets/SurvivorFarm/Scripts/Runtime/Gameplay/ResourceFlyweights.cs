using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    // Only intrinsic data is interned. Health, timers and scene objects never enter these caches.
    public static class ResourceFlyweights
    {
        private static readonly Dictionary<ItemKind, ItemDefinition> items = new Dictionary<ItemKind, ItemDefinition>();
        private static readonly Dictionary<(ItemKind, int, int, int, ResourceAnimation), ResourceDefinition> resources = new Dictionary<(ItemKind, int, int, int, ResourceAnimation), ResourceDefinition>();
        private static readonly List<ResourceAnimation> animations = new List<ResourceAnimation>();
        private static readonly Dictionary<(string, float, float, float, float, float), CultivationDefinition> cultivations = new Dictionary<(string, float, float, float, float, float), CultivationDefinition>();
        private static readonly List<ScriptableObject> transientObjects = new List<ScriptableObject>();
        private static bool initialized;

        private static void EnsureLoaded()
        {
            if (initialized) return;
            initialized = true;
            Resources.Load<FlyweightCatalog>("FlyweightCatalog")?.Register();
        }

        public static ItemDefinition Item(ItemKind kind)
        {
            EnsureLoaded();
            if (items.TryGetValue(kind, out ItemDefinition item)) return item;
            item = Create<ItemDefinition>(kind.ToString());
            string[] labels = { "madera", "piedra", "fruta", "comida", "oro", "semilla comun", "semilla mineral", "semilla magica", "experiencia", "hierro", "oro bruto", "rubí", "esmeralda", "diamante", "esmeralda menor", "esencia de tierra" };
            item.Initialize(kind, kind == ItemKind.Arrow ? "flechas" : kind == ItemKind.Leather ? "cuero" : kind == ItemKind.Bow ? "arco antiguo" : labels[(int)kind]);
            items.Add(kind, item);
            return item;
        }

        public static ItemDefinition Seed(SeedRarity rarity) => Item((ItemKind)((int)ItemKind.CommonSeed + (int)rarity));

        public static ResourceDefinition Resource(ItemKind kind, int amount = 2, int coins = 5, int health = 1, ResourceAnimation clip = null)
        {
            EnsureLoaded();
            var key = (kind, Mathf.Max(1, amount), Mathf.Max(0, coins), Mathf.Max(1, health), clip);
            if (resources.TryGetValue(key, out ResourceDefinition definition)) return definition;
            string suffix = clip != null ? "_" + clip.name : string.Empty;
            definition = Create<ResourceDefinition>($"{kind}_{key.Item2}_{key.Item3}_{key.Item4}{suffix}");
            definition.Initialize(Item(kind), key.Item2, key.Item3, key.Item4, clip);
            resources.Add(key, definition);
            return definition;
        }

        public static ResourceAnimation Animation(Sprite[] frames, float fps = 6f)
        {
            EnsureLoaded();
            frames = frames ?? Array.Empty<Sprite>();
            fps = Mathf.Max(1f, fps);
            foreach (ResourceAnimation animation in animations)
            {
                if (animation.FrameCount != frames.Length || animation.FramesPerSecond != fps) continue;
                bool equal = true;
                for (int i = 0; i < frames.Length; i++) equal &= animation.GetFrame(i) == frames[i];
                if (equal) return animation;
            }
            ResourceAnimation clip = Create<ResourceAnimation>($"Animation_{animations.Count}");
            clip.Initialize(frames, fps);
            animations.Add(clip);
            return clip;
        }

        public static CultivationDefinition Cultivation(string label = "fruta", float grow = 8f, float dig = 1.3f, float hoe = 1.1f, float plant = 0.9f, float water = 1f)
        {
            EnsureLoaded();
            var key = (label, grow, dig, hoe, plant, water);
            if (cultivations.TryGetValue(key, out CultivationDefinition definition)) return definition;
            definition = Create<CultivationDefinition>($"Cultivation_{cultivations.Count}");
            definition.Initialize(label, grow, dig, hoe, plant, water);
            cultivations.Add(key, definition);
            return definition;
        }

        internal static void Register(ItemDefinition item) { if (item != null) items[item.Kind] = item; }
        internal static void Register(ResourceDefinition resource)
        {
            if (resource != null && resource.Reward != null)
                resources[(resource.Reward.Kind, resource.HarvestAmount, resource.CoinReward, resource.MaxHealth, resource.Animation)] = resource;
        }
        internal static void Register(ResourceAnimation clip) { if (clip != null) animations.Add(clip); }
        internal static void Register(CultivationDefinition definition)
        {
            if (definition != null)
                cultivations[(definition.CropName, definition.GrowDuration, definition.DigDuration, definition.HoeDuration, definition.PlantDuration, definition.WaterDuration)] = definition;
        }

        private static T Create<T>(string name) where T : ScriptableObject
        {
            T value = ScriptableObject.CreateInstance<T>();
            value.name = name;
            value.hideFlags = HideFlags.DontSave;
            transientObjects.Add(value);
            return value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            foreach (ScriptableObject value in transientObjects)
            {
                if (value != null && (value.hideFlags & HideFlags.DontSave) != 0)
                    UnityEngine.Object.Destroy(value);
            }
            transientObjects.Clear();
            items.Clear();
            resources.Clear();
            animations.Clear();
            cultivations.Clear();
            initialized = false;
        }
    }
}
