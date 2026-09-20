using System;
using SurvivorFarm.Runtime.Core;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    [Serializable]
    public sealed class ResourceRegrowthState
    {
        public Vector3 position;
        public float remaining;
    }

    [DefaultExecutionOrder(-100)]
    public sealed class ResourceSpawnPoint : MonoBehaviour
    {
        [SerializeField] private string persistentId;
        [SerializeField] private HarvestableResource prefab;
        [SerializeField] private HarvestableResource instance;
        private bool regrowthEnabled;
        private float remaining = -1, minDelay = 45, maxDelay = 90;
        public bool RegrowthEnabled => regrowthEnabled;
        public float RegrowthRemaining => Mathf.Max(0, remaining);

        public string PersistentId => StableSaveId.Resolve(this, persistentId);
        public bool MatchesPersistentId(string savedId) => StableSaveId.Matches(this, savedId, persistentId);
        public HarvestableResource Instance => instance;
        public HarvestableResource Prefab => prefab;

        public static ResourceSpawnPoint Attach(HarvestableResource resource, HarvestableResource resourcePrefab = null)
        {
            ResourceSpawnPoint existing = resource.GetComponentInParent<ResourceSpawnPoint>();
            if (existing != null)
            {
                existing.Configure(resourcePrefab != null ? resourcePrefab : existing.prefab, resource, existing.persistentId);
                return existing;
            }

            Vector3 position = resource.transform.position;
            GameObject point = new GameObject($"{resource.name} Spawn");
            point.transform.SetParent(resource.transform.parent, false);
            point.transform.position = position;
            resource.transform.SetParent(point.transform, true);
            ResourceSpawnPoint spawn = point.AddComponent<ResourceSpawnPoint>();
            string id = FormattableString.Invariant($"{resource.GetType().Name}:{position.x:F3}:{position.y:F3}:{position.z:F3}");
            spawn.Configure(resourcePrefab, resource, id);
            return spawn;
        }

        private void Awake()
        {
            EnsureSpawned();
        }

        public void Configure(HarvestableResource resourcePrefab, HarvestableResource sceneInstance, string id)
        {
            prefab = resourcePrefab;
            instance = sceneInstance;
            persistentId = id;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(persistentId))
            {
                persistentId = Guid.NewGuid().ToString("N");
            }
        }

        public void EnsureSpawned()
        {
            if (instance == null && prefab != null)
            {
                instance = Instantiate(prefab, transform);
                instance.name = prefab.name;
                instance.Spawn(transform.position);
            }
        }

        public void Restore(bool harvested, int health = -1)
        {
            EnsureSpawned();
            instance?.Restore(harvested, health);
            remaining = -1;
        }

        public void EnableRegrowth(float minimum, float maximum)
        {
            EnsureSpawned();
            if (!(instance is TreeResource) && !(instance is RockResource)) return;
            minDelay = Mathf.Max(1, minimum); maxDelay = Mathf.Max(minDelay, maximum);
            regrowthEnabled = true;
            instance.Depleted -= OnDepleted;
            instance.Depleted += OnDepleted;
            if (instance.IsHarvested && remaining < 0) ScheduleRegrowth();
        }

        private void OnDepleted(HarvestableResource resource) { if (resource == instance) ScheduleRegrowth(); }
        private void ScheduleRegrowth() => remaining = minDelay == maxDelay ? minDelay : UnityEngine.Random.Range(minDelay, maxDelay);
        public bool AdvanceRegrowth(float seconds)
        {
            if (!regrowthEnabled || instance == null || !instance.IsHarvested) return false;
            if (remaining < 0) ScheduleRegrowth();
            remaining = Mathf.Max(0, remaining - Mathf.Max(0, seconds));
            return remaining <= 0;
        }
        public void RetryRegrowthLater() => remaining = 5;
        public ResourceRegrowthState CaptureRegrowth() => !regrowthEnabled || instance == null ? null :
            new ResourceRegrowthState { position = instance.transform.position, remaining = RegrowthRemaining };

        public void RestoreRegrowth(ResourceRegrowthState saved)
        {
            if (instance == null || (!(instance is TreeResource) && !(instance is RockResource))) return;
            bool valid = saved != null && Finite(saved.position.x) && Finite(saved.position.y) && Finite(saved.position.z) &&
                Finite(saved.remaining) && saved.remaining >= 0 && Vector2.Distance(saved.position, transform.position) <= 6.1f;
            if(valid&&PortfolioSession.Active&&World.FarmExploration.IsRiver(saved.position,.5f))valid=false;
            instance.transform.position = valid ? new Vector3(saved.position.x, saved.position.y, transform.position.z) : transform.position;
            if(PortfolioSession.Active){ResourceTier.Configure(instance);World.FarmWorldPolish.StyleResource(instance);}
            remaining = valid ? Mathf.Clamp(saved.remaining, 0, 300) : -1;
        }
        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);

        [ContextMenu("Respawn Resource")]
        public void Respawn()
        {
            RespawnAt(transform.position);
        }

        public void RespawnAt(Vector3 position)
        {
            EnsureSpawned();
            instance?.Spawn(position);
            remaining = -1;
        }
        private void OnDestroy() { if (instance != null) instance.Depleted -= OnDepleted; }
    }
}
