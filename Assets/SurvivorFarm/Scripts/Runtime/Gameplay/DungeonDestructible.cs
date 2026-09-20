using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class DungeonDestructible : MonoBehaviour, IDamageable
    {
        private bool rock;
        public int Health { get; private set; }
        public int MaximumHealth => rock ? 7 : 5;
        public int SpawnGeneration { get; private set; }
        public bool IsAlive => isActiveAndEnabled && Health > 0;
        public Transform Transform => transform;
        public void Configure(bool rubble) { rock = rubble; Restore(MaximumHealth); }
        public void Restore(int health)
        {
            Health = Mathf.Clamp(health, 0, MaximumHealth); SpawnGeneration++;
            GetComponent<Collider2D>().enabled = Health > 0;
            GetComponent<SpriteRenderer>().enabled = Health > 0;
        }
        public void TakeDamage(int amount, PlayerInventory source)
        {
            if (!IsAlive || amount <= 0 || source == null) return;
            Health = Mathf.Max(0, Health - amount);
            HitFeedback.Report(gameObject, source, amount, Health == 0, false, rock ? ImpactSurface.Stone : ImpactSurface.Wood);
            if (Health > 0) return;
            GetComponent<Collider2D>().enabled = false; GetComponent<SpriteRenderer>().enabled = false;
            if (rock)
            {
                EnemyLootPickup.Scatter(transform.position, transform.parent, ItemKind.Stone, 2);
                EnemyLootPickup.Scatter(transform.position, transform.parent, ItemKind.Iron, 1);
            }
            else EnemyLootPickup.Scatter(transform.position, transform.parent, ItemKind.Experience, Random.Range(2, 5));
        }
    }
}
