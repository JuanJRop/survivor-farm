using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SurvivorFarm.Runtime.World
{
    [DefaultExecutionOrder(700)]
    public sealed class WildResourceRegrowth : MonoBehaviour
    {
        [SerializeField] private float treeMinimum = 45, treeMaximum = 90, rockMinimum = 60, rockMaximum = 110;
        private readonly List<ResourceSpawnPoint> points = new List<ResourceSpawnPoint>();
        private readonly Collider2D[] overlaps = new Collider2D[64];
        private Tilemap ground, paths;
        private EnemyCampWorld camps;
        private ConstructionSystem buildings;
        private PlayerSurvivalStats stats;
        private float elapsed;
        public IReadOnlyList<ResourceSpawnPoint> Points => points;
        public int PoolCount => points.Count;
        private void Start() => Initialize();

        public void Initialize()
        {
            if (points.Count > 0) return;
            var maps = FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            ground = maps.FirstOrDefault(t => t.name == "Spring Grass");
            paths = maps.FirstOrDefault(t => t.name == "Farm Paths");
            if (ground == null) return;
            camps = GetComponent<EnemyCampWorld>(); buildings = GetComponent<ConstructionSystem>(); stats = GetComponent<PlayerSurvivalStats>();
            foreach (var point in FindObjectsByType<ResourceSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(p => p.PersistentId))
            {
                if (!(point.Instance is TreeResource) && !(point.Instance is RockResource)) continue;
                if (!ground.HasTile(ground.WorldToCell(point.transform.position))) continue;
                points.Add(point);
                bool tree = point.Instance is TreeResource;
                point.EnableRegrowth(tree ? treeMinimum : rockMinimum, tree ? treeMaximum : rockMaximum);
            }
        }

        private void Update()
        {
            if (ground == null || stats != null && stats.CurrentHealth <= 0 ||
                PlayerRespawnController.MenuOpen) return;
            elapsed += Time.deltaTime;
            if (elapsed < 1) return;
            float delta = elapsed; elapsed = 0;
            Tick(delta);
        }

        public void Tick(float seconds)
        {
            if (ground == null || stats != null && stats.CurrentHealth <= 0) return;
            int spawned = 0;
            foreach (var point in points)
            {
                if (point == null || !point.AdvanceRegrowth(seconds)) continue;
                // Time also passes in the dungeon; validate and activate resources once the outdoor map returns.
                if (!ground.gameObject.activeInHierarchy || !point.isActiveAndEnabled || spawned >= 2) continue;
                if (!TryFindSite(point, out var position)) { point.RetryRegrowthLater(); continue; }
                point.RespawnAt(position);
                Physics2D.SyncTransforms();
                spawned++;
            }
        }

        public bool TryFindSite(ResourceSpawnPoint point, out Vector3 position)
        {
            Physics2D.SyncTransforms();
            for (int attempt = 0; attempt < 48; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * 6;
                var candidate = point.transform.position + new Vector3(Mathf.Round(offset.x * 2) * .5f, Mathf.Round(offset.y * 2) * .5f);
                if (CanGrowAt(point, candidate)) { position = candidate; return true; }
            }
            position = default; return false;
        }

        public bool CanGrowAt(ResourceSpawnPoint point, Vector3 position)
        {
            if (point == null || point.Instance == null || ground == null || Vector2.Distance(position, point.transform.position) > 6.1f ||
                Vector2.Distance(position, transform.position) < 4 || Vector2.Distance(position, point.Instance.transform.position) < 1) return false;
            // Keep the plaza and shop approaches clear; only the village's outer green areas regrow.
            if (VillageLayout.IsVillage(position) && Mathf.Abs(position.x) < 8.5f) return false;
            if (camps != null && camps.ContainsCamp(position, 2)) return false;
            if (buildings != null && buildings.Buildings.Any(b => !b.indoors && Vector2.Distance(position, new Vector2(b.x, b.y)) < 2)) return false;
            for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++)
            {
                Vector3 sample = position + new Vector3(x * .8f, y * .8f);
                if (!ground.HasTile(ground.WorldToCell(sample)) || paths != null && paths.HasTile(paths.WorldToCell(sample)) || VillageLayout.IsRoad(sample)) return false;
            }
            float spacing = point.Instance is TreeResource ? 1.8f : 1.4f;
            foreach (var other in points)
                if (other != null && other != point && other.Instance != null && other.Instance.IsAvailable && Vector2.Distance(position, other.Instance.transform.position) < spacing) return false;
            int count = Physics2D.OverlapCircle(position, 1.1f, new ContactFilter2D { useTriggers = true }, overlaps);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var hit = overlaps[i];
                if (hit.transform.IsChildOf(point.transform)) continue;
                if (!hit.isTrigger || hit.GetComponentInParent<WorldInteractable>() != null || hit.GetComponentInParent<PlayerInventory>() != null) return false;
            }
            return true;
        }
    }
}
