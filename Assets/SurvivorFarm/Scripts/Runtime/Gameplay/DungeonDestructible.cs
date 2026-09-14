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
            if (rock) { source.AddStone(2); source.GetComponent<AdventureProgress>()?.AddIron(1); }
            else { source.AddWood(2); source.AddCoins(3); }
            GetComponent<Collider2D>().enabled = false; GetComponent<SpriteRenderer>().enabled = false;
            FarmNotificationCenter.Show(rock ? "Escombros: +2 piedra, +1 hierro." : "Suministros: +2 madera, +3 monedas.");
        }
    }
}
