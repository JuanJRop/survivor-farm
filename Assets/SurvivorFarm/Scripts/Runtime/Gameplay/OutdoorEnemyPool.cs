using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class OutdoorEnemyPool : MonoBehaviour
    {
        [SerializeField] private OutdoorEnemyAI prefab;
        [SerializeField] private Transform player;
        [SerializeField] private HomeSafeZone safeZone;
        [SerializeField, Range(1, 20)] private int capacity = 5;
        [SerializeField] private DayNightCycle clock;
        [SerializeField, Range(0, 20)] private int daytimeLimit = 2;
        [SerializeField, Min(1f)] private float daySpawnInterval = 12f;
        [SerializeField, Min(1f)] private float nightSpawnInterval = 3f;
        [SerializeField, Min(0f)] private float dayDetectionRange = 3.5f;
        [SerializeField, Min(0f)] private float nightDetectionRange = 10f;
        [SerializeField, Min(0.1f)] private float daySpeedMultiplier = 0.75f;
        [SerializeField, Min(0.1f)] private float nightSpeedMultiplier = 1.4f;
        [SerializeField, Min(0.1f)] private float dayAttackIntervalMultiplier = 1.5f;
        [SerializeField, Min(0.1f)] private float nightAttackIntervalMultiplier = 0.65f;
        [SerializeField] private Transform terrainRoot;
        [SerializeField] private List<OutdoorEnemyAI> enemies = new List<OutdoorEnemyAI>();
        [SerializeField] private bool includeGoblinRoster = true;
        private EnemyProjectilePool projectiles;
        private int spawnCursor;
        private EnemyCampWorld campWorld;
        public OutdoorEnemyAI EnemyPrefab => prefab;
        private readonly Collider2D[] overlaps = new Collider2D[32];
        private readonly List<Vector3> spawnPositions = new List<Vector3>();
        private PlayerSurvivalStats stats;
        private float nextSpawn;
        private bool? previousNight;
        public bool IsNight => clock != null && clock.IsNight;
        private bool FirstDayCampLocked
        {
            get
            {
                ValleyCampaign campaign = player != null ? player.GetComponent<ValleyCampaign>() : null;
                return campaign != null && !campaign.Data.camp;
            }
        }
        public int ActiveLimit => IsNight ? capacity : FirstDayCampLocked ? 0 : Mathf.Clamp(daytimeLimit, 0, capacity);
        public float DetectionRange => IsNight ? Mathf.Max(11f, nightDetectionRange) : Mathf.Max(5.5f, dayDetectionRange);
        public float SpeedMultiplier => IsNight ? nightSpeedMultiplier : daySpeedMultiplier;
        public float AttackDelayMultiplier => IsNight ? nightAttackIntervalMultiplier : Mathf.Min(1.1f, dayAttackIntervalMultiplier);
        public float SpawnInterval => IsNight ? nightSpawnInterval : daySpawnInterval;
        public int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (var enemy in enemies) if (enemy.IsAlive) count++;
                return count;
            }
        }

        public void ConfigureClock(DayNightCycle cycle)
        {
            clock = cycle;
            previousNight = null;
        }

        public void RefreshPopulation()
        {
            if (previousNight != IsNight)
            {
                previousNight = IsNight;
                nextSpawn = Time.time;
            }
            int retained = 0;
            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive) continue;
                if (retained >= ActiveLimit) enemy.ReturnToPool();
                else retained++;
            }
        }
        public HomeSafeZone SafeZone => safeZone;
        public int PoolCount => enemies.Count;
        public bool CanChase => player != null && safeZone != null && isActiveAndEnabled && (stats == null || stats.CurrentHealth > 0);

        public void Configure(OutdoorEnemyAI enemyPrefab, Transform target, HomeSafeZone zone, Transform terrain, int size = 5, bool useGoblinRoster = true)
        {
            prefab = enemyPrefab;
            player = target;
            safeZone = zone;
            terrainRoot = terrain;
            capacity = Mathf.Clamp(size, 1, 20);
            includeGoblinRoster = useGoblinRoster;
        }

        private void Start() => Initialize();

        public void Initialize()
        {
            if (prefab == null || player == null || safeZone == null) return;
            if (clock == null) clock = FindFirstObjectByType<DayNightCycle>();
            stats = player.GetComponent<PlayerSurvivalStats>();
            campWorld = player.GetComponent<EnemyCampWorld>();
            foreach (var enemy in enemies) enemy.ConfigureOutdoor(player, this);
            spawnPositions.Clear();
            var ground = terrainRoot != null ? terrainRoot.GetComponentsInChildren<Tilemap>(true).FirstOrDefault(map => map.name == "Spring Grass") : null;
            if (ground == null) ground = FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(map => map.name == "Spring Grass");
            if (ground != null)
                foreach (var cell in ground.cellBounds.allPositionsWithin)
                    if (ground.HasTile(cell)) spawnPositions.Add(ground.GetCellCenterWorld(cell));
            for (int i = enemies.Count; i < capacity; i++)
            {
                OutdoorEnemyAI enemy = Instantiate(prefab, transform);
                enemy.name = $"Outdoor Enemy {i + 1}";
                enemy.ConfigureOutdoor(player, this);
                enemy.ReturnToPool();
                enemies.Add(enemy);
            }
            if (includeGoblinRoster)
            {
                projectiles = EnemyProjectilePool.Ensure(gameObject, capacity * 2);
                for (int i = 0; i < enemies.Count; i++)
                    if (i % 3 != 2) EnemyRoster.Configure(enemies[i], i % 3 == 0 ? EnemyCombatStyle.SpearGoblin : EnemyCombatStyle.ArcherGoblin, projectiles);
                    else enemies[i].EnsureMinimumHealth(8);
            }
        }

        private void Update()
        {
            RefreshPopulation();
            if (!CanChase) return;
            foreach (var enemy in enemies)
                if (enemy.IsAlive && safeZone.Contains(enemy.transform.position, 0.3f)) enemy.ReturnToPool();
            if (Time.time < nextSpawn) return;
            nextSpawn = Time.time + SpawnInterval;
            SpawnOne();
        }

        public bool SpawnOne()
        {
            RefreshPopulation();
            if (ActiveCount >= ActiveLimit) return false;
            if (!CanChase || spawnPositions.Count == 0) return false;
            for (int offset = 0; offset < enemies.Count; offset++)
            {
                int index = includeGoblinRoster ? (spawnCursor + offset) % enemies.Count : offset;
                var enemy = enemies[index];
                if (enemy.gameObject.activeSelf) continue;
                for (int attempt = 0; attempt < 80; attempt++)
                {
                    Vector3 position = spawnPositions[Random.Range(0, spawnPositions.Count)];
                    if (Vector2.Distance(position, player.position) < 3f || !CanOccupy(position, enemy)) continue;
                    enemy.ActivateFromPool(position);
                    spawnCursor = (index + 1) % enemies.Count;
                    return true;
                }
                return false;
            }
            return false;
        }

        public bool CanOccupy(Vector2 position, OutdoorEnemyAI self)
        {
            if (safeZone == null || safeZone.Contains(position, 0.3f)) return false;
            if (campWorld != null && campWorld.ContainsCamp(position, .4f)) return false;
            var filter = new ContactFilter2D { useTriggers = false };
            int count = Physics2D.OverlapCircle(position, 0.28f, filter, overlaps);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++)
            {
                var hit = overlaps[i];
                if (self != null && hit.transform.IsChildOf(self.transform)) continue;
                if (player != null && hit.transform.IsChildOf(player)) continue;
                return false;
            }
            return true;
        }

        private void OnDisable()
        {
            foreach (var enemy in enemies) if (enemy != null) enemy.ReturnToPool();
            projectiles?.ReturnAll();
        }
    }
}
