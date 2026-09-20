using System.Collections.Generic;
using SurvivorFarm.Runtime.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Scene-owned arrows; the storage never follows the moving player.</summary>
    public sealed class PlayerProjectilePool : MonoBehaviour
    {
        public const int MaximumProjectiles = 32;
        private SceneComponentPool<ArrowProjectile> pool;
        private readonly Queue<ArrowProjectile> disabled = new Queue<ArrowProjectile>(MaximumProjectiles);
        public int CreatedCount => pool != null ? pool.CreatedCount : 0;
        public int ActiveCount => pool != null ? pool.ActiveCount : 0;
        public int InactiveCount => pool != null ? pool.InactiveCount : 0;

        public static PlayerProjectilePool Create(Transform owner, int prewarm = 4,
            int maxRetained = MaximumProjectiles, int maxActive = MaximumProjectiles)
        {
            var storage = new GameObject("Player arrows · reusable");
            if (owner.parent != null) storage.transform.SetParent(owner.parent, false);
            else SceneManager.MoveGameObjectToScene(storage, owner.gameObject.scene);
            var created = storage.AddComponent<PlayerProjectilePool>();
            created.pool = new SceneComponentPool<ArrowProjectile>(storage.transform, created.CreateArrow,
                prewarm, maxRetained, maxActive);
            return created;
        }

        private ArrowProjectile CreateArrow(Transform storage)
        {
            var go = new GameObject("Arrow Projectile");
            go.SetActive(false);
            go.transform.SetParent(storage, false);
            var visual = go.AddComponent<SpriteRenderer>();
            visual.sprite = CombatFeelVisuals.Arrow;
            return go.AddComponent<ArrowProjectile>();
        }

        public ArrowProjectile Rent()
        {
            DrainDisabled();
            var arrow = pool.Rent();
            if (arrow != null) arrow.Lease(this);
            return arrow;
        }

        internal void Return(ArrowProjectile arrow) { if (pool != null) pool.Return(arrow); }
        internal void Deactivated(ArrowProjectile arrow) => disabled.Enqueue(arrow);
        internal void Forget(ArrowProjectile arrow) { if (pool != null) pool.Forget(arrow); }
        private void Update() => DrainDisabled();
        private void DrainDisabled()
        {
            while (disabled.Count > 0)
            {
                var arrow = disabled.Dequeue();
                if (arrow != null) arrow.ReturnToPool();
            }
        }
        private void OnDestroy() { disabled.Clear(); pool?.Dispose(); }
    }
}
