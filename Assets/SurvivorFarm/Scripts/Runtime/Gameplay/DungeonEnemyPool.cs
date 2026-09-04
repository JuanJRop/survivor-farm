using System.Collections.Generic;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class DungeonEnemyPool : MonoBehaviour
    {
        [SerializeField] private BasicEnemyAI enemyPrefab;
        [SerializeField] private BasicEnemyAI[] enemyPrefabs = new BasicEnemyAI[0];
        [SerializeField] private Transform player;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private int initialSize = 6;

        private readonly List<BasicEnemyAI> enemies = new List<BasicEnemyAI>();

        public void Configure(BasicEnemyAI prefab, Transform playerTarget, Transform[] points, int poolSize)
        {
            Configure(new[] { prefab }, playerTarget, points, poolSize);
        }

        public void Configure(BasicEnemyAI[] prefabs, Transform playerTarget, Transform[] points, int poolSize)
        {
            enemyPrefabs = prefabs ?? new BasicEnemyAI[0];
            enemyPrefab = enemyPrefabs.Length > 0 ? enemyPrefabs[0] : null;
            player = playerTarget;
            spawnPoints = points;
            initialSize = Mathf.Max(1, poolSize);
            BuildPool();
        }

        public void SpawnEncounter()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return;
            }

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                BasicEnemyAI enemy = GetEnemy();
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
                enemies[i].ReturnToPool();
            }
        }

        private void BuildPool()
        {
            for (int i = enemies.Count; i < initialSize; i++)
            {
                CreateEnemy();
            }
        }

        private BasicEnemyAI GetEnemy()
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (!enemies[i].gameObject.activeSelf)
                {
                    return enemies[i];
                }
            }

            return CreateEnemy();
        }

        private BasicEnemyAI CreateEnemy()
        {
            BasicEnemyAI prefab = GetPrefabForIndex(enemies.Count);
            if (prefab == null)
            {
                return null;
            }

            BasicEnemyAI enemy = Instantiate(prefab, transform);
            enemy.Configure(player, this);
            enemy.gameObject.SetActive(false);
            enemies.Add(enemy);
            return enemy;
        }

        private BasicEnemyAI GetPrefabForIndex(int index)
        {
            if (enemyPrefabs != null && enemyPrefabs.Length > 0)
            {
                return enemyPrefabs[index % enemyPrefabs.Length];
            }

            return enemyPrefab;
        }
    }
}
