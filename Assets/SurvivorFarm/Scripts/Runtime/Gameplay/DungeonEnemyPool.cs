using System.Collections.Generic;
using UnityEngine;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class DungeonEnemyPool : MonoBehaviour
    {
        [SerializeField] private EnemyAIBase enemyPrefab;
        [SerializeField] private EnemyAIBase[] enemyPrefabs = new EnemyAIBase[0];
        [SerializeField] private Transform player;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private int initialSize = 6;
        [SerializeField] private bool includeGoblinRoster = true;
        private EnemyProjectilePool projectiles;
        public int PoolCount => enemies.Count;
        public IReadOnlyList<EnemyAIBase> Enemies => enemies;
        public DungeonExpedition Expedition { get; private set; }

        public void ConfigureExpedition(DungeonExpedition expedition, Transform[] points)
        {
            Expedition = expedition;
            spawnPoints = points;
            initialSize = Mathf.Max(initialSize, points.Length);
            Initialize();
        }

        public void NotifyDefeated(EnemyAIBase enemy) => Expedition?.Defeated(enemies.IndexOf(enemy));

        private readonly List<EnemyAIBase> enemies = new List<EnemyAIBase>();

        public void Configure(EnemyAIBase prefab, Transform playerTarget, Transform[] points, int poolSize)
        {
            Configure(new[] { prefab }, playerTarget, points, poolSize);
        }

        public void Configure(EnemyAIBase[] prefabs, Transform playerTarget, Transform[] points, int poolSize)
        {
            enemyPrefabs = prefabs ?? new EnemyAIBase[0];
            enemyPrefab = enemyPrefabs.Length > 0 ? enemyPrefabs[0] : null;
            player = playerTarget;
            spawnPoints = points;
            initialSize = Mathf.Max(1, poolSize);
            BuildPool();
        }

        private void Start() => Initialize();

        public void SpawnEncounter()
        {
            Initialize();
            DespawnAll();
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return;
            }

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (Expedition != null && Expedition.IsDefeated(i)) continue;
                EnemyAIBase enemy = Expedition != null ? enemies[i] : GetEnemy();
                if (enemy != null)
                {
                    enemy.ActivateFromPool(spawnPoints[i].position);
                }
            }
        }

        public void DespawnAll()
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null) enemies[i].ReturnToPool();
            }
            projectiles?.ReturnAll();
        }

        private void OnDisable() => DespawnAll();

        private void BuildPool() => Initialize();

        public void Initialize()
        {
            if (player == null || GetPrefabForIndex(0) == null) return;
            // Scene-authored inactive instances belong to this pool too.
            if (enemies.Count == 0)
                enemies.AddRange(GetComponentsInChildren<EnemyAIBase>(true));
            int capacity = Mathf.Max(initialSize, spawnPoints != null ? spawnPoints.Length : 0);
            if (includeGoblinRoster) projectiles = EnemyProjectilePool.Ensure(gameObject, capacity * 2);
            foreach (var enemy in enemies) enemy.Configure(player, this);
            while (enemies.Count < capacity) if (CreateEnemy() == null) break;
            if (includeGoblinRoster)
                for (int i = 0; i < enemies.Count; i++)
                {
                    int slot = i % (Expedition != null ? 3 : 6);
                    if (slot < 2) EnemyRoster.Configure(enemies[i], slot == 0 ? EnemyCombatStyle.SpearGoblin : EnemyCombatStyle.ArcherGoblin, projectiles);
                    else
                    {
                        enemies[i].EnsureMinimumHealth(8);
                        if (Expedition != null && enemies[i].SpriteAnimation == null) ConfigureLegacyArt(enemies[i], i);
                    }
                }
        }

        private void ConfigureLegacyArt(EnemyAIBase enemy, int index)
        {
            var library = Resources.Load<PlayerAnimationLibrary>("SproutSlimeAnimations");
            if (library == null) return;
            foreach (var sr in enemy.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false;
            var go = new GameObject("Animated ruin creature"); go.transform.SetParent(enemy.transform, false);
            var visual = go.AddComponent<SpriteRenderer>(); visual.sprite = library.Frame(library.Find("Idle"), 0, 0);
            bool golem = index == 5 || index == 11;
            go.transform.localScale = Vector3.one * ((golem ? 1.25f : .8f) / enemy.transform.lossyScale.x);
            go.AddComponent<WorldSpriteDepth>().Visual = visual;
            enemy.name = golem ? "Golem de las ruinas" : "Limo de las ruinas";
            enemy.ConfigureVisuals(visual, null, Color.white, golem ? "Golem" : "Limo");
            enemy.ConfigureStats(golem ? "Golem" : "Limo", golem ? 16 : 8, golem ? 2 : 1, 1.2f, .85f, 1.7f, golem ? 8 : 3);
            enemy.ConfigureAnimation(library);
        }

        private EnemyAIBase GetEnemy()
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (!enemies[i].gameObject.activeSelf)
                {
                    return enemies[i];
                }
            }

            return null;
        }

        private EnemyAIBase CreateEnemy()
        {
            EnemyAIBase prefab = GetPrefabForIndex(enemies.Count);
            if (prefab == null)
            {
                return null;
            }

            EnemyAIBase enemy = Instantiate(prefab, transform);
            enemy.Configure(player, this);
            enemy.ReturnToPool();
            enemies.Add(enemy);
            return enemy;
        }

        private EnemyAIBase GetPrefabForIndex(int index)
        {
            if (enemyPrefabs != null && enemyPrefabs.Length > 0)
            {
                return enemyPrefabs[index % enemyPrefabs.Length];
            }

            return enemyPrefab;
        }
    }
}
