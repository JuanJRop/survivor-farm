using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SurvivorFarm.Runtime.World
{
    [DefaultExecutionOrder(600)]
    public sealed class EnemyCampWorld : MonoBehaviour
    {
        private ValleyCampaign campaign;
        private Transform campRoot;
        private readonly List<EnemyCamp> camps = new List<EnemyCamp>();
        public IReadOnlyList<EnemyCamp> Camps => camps;
        public void Configure(ValleyCampaign owner) => campaign = owner;
        private void Start() => Initialize();

        public void Initialize()
        {
            if (camps.Count > 0 || campaign == null) return;
            var outdoor = FindFirstObjectByType<OutdoorEnemyPool>(FindObjectsInactive.Include);
            var ground = FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Spring Grass");
            if (outdoor == null || outdoor.EnemyPrefab == null || ground == null) return;
            campRoot = new GameObject("Enemy camps around Raizclara").transform;
            campRoot.SetParent(outdoor.transform.parent, false);
            var paths = FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Farm Paths");
            var resourcePoints = FindObjectsByType<ResourceSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None).Select(r => (Vector2)r.transform.position).ToArray();
            Physics2D.SyncTransforms();
            foreach (var definition in EnemyCampDefinition.All)
            {
                var saved = GetState(definition.Id);
                Vector3 center;
                if (saved != null && ValidRegion(saved.center, definition)) center = saved.center;
                else if (!TryFindSite(definition, ground, paths, resourcePoints, out center))
                {
                    Debug.LogError("No clear site for enemy camp: " + definition.Id);
                    continue;
                }
                if (saved == null) { saved = new EnemyCampState { id = definition.Id }; campaign.Data.enemyCamps.Add(saved); }
                saved.center = new Vector3(center.x, center.y, 0);
                var go = new GameObject(definition.Title); go.transform.SetParent(campRoot, false);
                var camp = go.AddComponent<EnemyCamp>();
                camp.Configure(definition, saved, campaign.Inventory, outdoor.SafeZone, ground, outdoor.EnemyPrefab, campaign);
                camps.Add(camp);
            }
        }

        private EnemyCampState GetState(string id)
        {
            campaign.Data.enemyCamps ??= new List<EnemyCampState>();
            return campaign.Data.enemyCamps.FirstOrDefault(s => s != null && s.id == id);
        }

        public void Restore()
        {
            foreach (var camp in camps)
            {
                var saved = GetState(camp.Definition.Id);
                if (saved == null)
                {
                    saved = new EnemyCampState { id = camp.Definition.Id, center = camp.transform.position };
                    campaign.Data.enemyCamps.Add(saved);
                }
                if (!ValidRegion(saved.center, camp.Definition)) saved.center = camp.transform.position;
                saved.center = new Vector3(saved.center.x, saved.center.y, 0);
                camp.Restore(saved);
            }
        }

        public bool ContainsCamp(Vector2 position, float margin = 0)
        {
            foreach (var camp in camps)
                if (Mathf.Abs(position.x - camp.transform.position.x) < 4.3f + margin &&
                    Mathf.Abs(position.y - camp.transform.position.y) < 4.3f + margin) return true;
            return false;
        }

        public static bool ValidRegion(Vector2 center, EnemyCampDefinition definition)
        {
            if (float.IsNaN(center.x) || float.IsNaN(center.y) || float.IsInfinity(center.x) || float.IsInfinity(center.y)) return false;
            if (Vector2.Distance(center, definition.PreferredCenter) > 7 || Mathf.Abs(center.x) > 31.5f || Mathf.Abs(center.y) > 16.7f) return false;
            // Keep the river, bridge approach, town and the central roads outside each footprint.
            if (Mathf.Abs(center.x) < 5.5f || Mathf.Abs(center.y) < 6 || (center.y > 1 && center.y < 12.5f)) return false;
            Vector2 nearestTown = new Vector2(Mathf.Clamp(center.x, VillageLayout.Bounds.xMin, VillageLayout.Bounds.xMax),
                Mathf.Clamp(center.y, VillageLayout.Bounds.yMin, VillageLayout.Bounds.yMax));
            return Vector2.Distance(center, nearestTown) >= 8;
        }

        public static bool TryFindSite(EnemyCampDefinition definition, Tilemap ground, Tilemap paths, Vector2[] resources, out Vector3 result)
        {
            for (int ring = 0; ring <= 12; ring++) for (int x = -ring; x <= ring; x++) for (int y = -ring; y <= ring; y++)
            {
                if (ring > 0 && Mathf.Abs(x) != ring && Mathf.Abs(y) != ring) continue;
                var center = definition.PreferredCenter + new Vector3(x * .5f, y * .5f);
                if (!ValidRegion(center, definition)) continue;
                var area = new Rect((Vector2)center - new Vector2(4.2f, 4.3f), new Vector2(8.4f, 8.6f));
                if (resources.Any(p => area.Contains(p))) continue;
                bool clear = true;
                for (int dx = -4; dx <= 4 && clear; dx++) for (int dy = -4; dy <= 4 && clear; dy++)
                {
                    Vector3 p = center + new Vector3(dx, dy);
                    if (!ground.HasTile(ground.WorldToCell(p)) || paths != null && paths.HasTile(paths.WorldToCell(p))) clear = false;
                }
                if (!clear) continue;
                foreach (var hit in Physics2D.OverlapBoxAll(center, new Vector2(8.4f, 8.6f), 0))
                    if (!hit.isTrigger && hit.GetComponentInParent<EnemyAIBase>() == null)
                    { clear = false; break; }
                if (clear) { result = center; return true; }
            }
            result = default;
            return false;
        }

        private void OnDestroy() { if (campRoot != null) Destroy(campRoot.gameObject); }
    }
}
