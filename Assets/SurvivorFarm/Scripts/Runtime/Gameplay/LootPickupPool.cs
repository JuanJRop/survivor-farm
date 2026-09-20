using System.Collections.Generic;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>
    /// One scene-owned service shares loot instances, physics scratch buffers and player lookup.
    /// Only idle retention is bounded: a busy battle must never discard a player's rewards.
    /// Active instances stay beneath their encounter so region visibility and saves remain intact.
    /// </summary>
    public sealed class LootPickupPool : MonoBehaviour
    {
        public const int RetainedPerPrefab = 64;
        private static readonly Dictionary<int, LootPickupPool> scenes = new Dictionary<int, LootPickupPool>();
        private readonly Dictionary<int, SceneComponentPool<EnemyLootPickup>> pools = new Dictionary<int, SceneComponentPool<EnemyLootPickup>>();
        private readonly Dictionary<ItemKind, Sprite> icons = new Dictionary<ItemKind, Sprite>();
        private readonly List<Collider2D> overlaps = new List<Collider2D>(32);
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(32);
        private static readonly ContactFilter2D solidFilter = new ContactFilter2D { useTriggers = false };
        private Transform storage;
        private PlayerInventory player;
        private PlayerSurvivalStats playerStats;
        private float nextPlayerLookup;
        private int sceneHandle;
        private Sprite shadowSprite;
        private Texture2D shadowTexture;
        private bool disposing;

        public int CreatedCount { get { int count = 0; foreach (var pool in pools.Values) count += pool.CreatedCount; return count; } }
        public int RentCount { get { int count = 0; foreach (var pool in pools.Values) count += pool.RentCount; return count; } }
        public int ActiveCount { get { int count = 0; foreach (var pool in pools.Values) count += pool.ActiveCount; return count; } }
        public int InactiveCount { get { int count = 0; foreach (var pool in pools.Values) count += pool.InactiveCount; return count; } }
        public static int ScenePoolCount => scenes.Count;

        internal static LootPickupPool For(Transform parent)
        {
            Scene scene = parent != null ? parent.gameObject.scene : SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded) scene = SceneManager.GetActiveScene();
            if (scenes.TryGetValue(scene.handle, out var current) && current != null && !current.disposing) return current;
            var owner = new GameObject("Loot reuse service");
            SceneManager.MoveGameObjectToScene(owner, scene);
            current = owner.AddComponent<LootPickupPool>();
            current.sceneHandle = scene.handle;
            var idle = new GameObject("Inactive loot");
            idle.transform.SetParent(owner.transform, false);
            idle.SetActive(false);
            current.storage = idle.transform;
            scenes[scene.handle] = current;
            return current;
        }

        internal EnemyLootPickup Rent(EnemyLootPickup prefab, Vector3 position, Transform parent, ItemDefinition item, int amount)
        {
            int key = prefab != null ? prefab.GetInstanceID() : 0;
            if (!pools.TryGetValue(key, out var pool))
            {
                // No active cap: overflowing the idle budget is reclaimed only after collection.
                pool = new SceneComponentPool<EnemyLootPickup>(storage, owner => Create(prefab, owner), 0, RetainedPerPrefab);
                pools.Add(key, pool);
            }
            var drop = pool.Rent();
            if (drop == null) return null; // Possible only during scene teardown.
            drop.PrepareLease(this, pool, parent, position);
            drop.Configure(item, amount, null);
            drop.gameObject.SetActive(true);
            return drop;
        }

        private static EnemyLootPickup Create(EnemyLootPickup prefab, Transform owner)
        {
            EnemyLootPickup drop;
            if (prefab != null) drop = Instantiate(prefab, owner);
            else
            {
                var go = new GameObject("Pooled loot");
                go.SetActive(false);
                go.transform.SetParent(owner, false);
                go.AddComponent<CircleCollider2D>().isTrigger = true;
                drop = go.AddComponent<EnemyLootPickup>();
            }
            drop.RememberAuthoredScale();
            return drop;
        }

        internal PlayerInventory GetPlayer(out PlayerSurvivalStats stats)
        {
            if (player == null && Time.time >= nextPlayerLookup)
            {
                nextPlayerLookup = Time.time + .5f;
                player = PortfolioSession.Instance != null ? PortfolioSession.Instance.Player : FindFirstObjectByType<PlayerInventory>();
                playerStats = player != null ? player.GetComponent<PlayerSurvivalStats>() : null;
            }
            stats = playerStats;
            return player;
        }

        internal bool HasClearPath(Vector2 start, Vector2 end, bool ignoreLaunchObstacle = false)
        {
            Physics2D.Linecast(start, end, solidFilter, hits);
            for (int i = 0; i < hits.Count; i++)
            {
                Collider2D obstacle = hits[i].collider;
                if (obstacle == null || (ignoreLaunchObstacle && obstacle.OverlapPoint(start))) continue;
                if (IsBlocking(obstacle)) return false;
            }
            return true;
        }

        internal bool CanLand(Vector2 origin, Vector2 destination)
        {
            Physics2D.OverlapCircle(destination, .22f, solidFilter, overlaps);
            for (int i = 0; i < overlaps.Count; i++) if (IsBlocking(overlaps[i])) return false;
            return HasClearPath(origin, destination, true);
        }

        private static bool IsBlocking(Collider2D obstacle) => obstacle != null && !obstacle.isTrigger &&
            obstacle.GetComponentInParent<PlayerInventory>() == null &&
            obstacle.GetComponentInParent<EnemyAIBase>() == null &&
            obstacle.GetComponentInParent<EnemyLootPickup>() == null;

        internal Sprite Icon(ItemKind kind)
        {
            if (icons.TryGetValue(kind, out var sprite) && sprite != null) return sprite;
            sprite = FarmUiStyle.ItemIcon(EnemyLootPickup.IconFor(kind));
            if (sprite == null) sprite = FarmUiStyle.ItemIcon("Gem");
            icons[kind] = sprite;
            return sprite;
        }

        internal Sprite Shadow
        {
            get
            {
                if (shadowSprite != null) return shadowSprite;
                shadowTexture = new Texture2D(16, 8, TextureFormat.RGBA32, false) { name = "Shared loot shadow", filterMode = FilterMode.Point, hideFlags = HideFlags.DontSave };
                var pixels = new Color32[128];
                for (int y = 0; y < 8; y++) for (int x = 0; x < 16; x++)
                {
                    float dx = (x - 7.5f) / 7.5f, dy = (y - 3.5f) / 3.5f;
                    if (dx * dx + dy * dy < 1) pixels[y * 16 + x] = new Color32(16, 18, 24, 80);
                }
                shadowTexture.SetPixels32(pixels);
                shadowTexture.Apply(false, true);
                shadowSprite = Sprite.Create(shadowTexture, new Rect(0, 0, 16, 8), Vector2.one * .5f, 16);
                shadowSprite.hideFlags = HideFlags.DontSave;
                return shadowSprite;
            }
        }

        private void OnDestroy()
        {
            disposing = true;
            if (scenes.TryGetValue(sceneHandle, out var current) && current == this) scenes.Remove(sceneHandle);
            foreach (var pool in pools.Values) pool.Dispose();
            pools.Clear();
            overlaps.Clear(); hits.Clear(); icons.Clear(); player = null; playerStats = null;
            if (shadowSprite != null) Destroy(shadowSprite);
            if (shadowTexture != null) Destroy(shadowTexture);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetScenes()
        {
            foreach (var instance in scenes.Values)
                if (instance != null) { instance.disposing = true; Destroy(instance.gameObject); }
            scenes.Clear();
        }
    }
}
