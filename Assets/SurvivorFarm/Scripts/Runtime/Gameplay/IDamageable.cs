using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public interface IDamageable
    {
        Transform Transform { get; }
        bool IsAlive { get; }
        int SpawnGeneration { get; }
        void TakeDamage(int amount, PlayerInventory source);
    }
}
