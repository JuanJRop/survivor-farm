using System.Collections.Generic;
using SurvivorFarm.Runtime.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>One bounded impact service per loaded scene. No persistent references survive scene teardown.</summary>
    public sealed class CombatImpactPool : MonoBehaviour
    {
        private static readonly Dictionary<int, CombatImpactPool> scenes = new Dictionary<int, CombatImpactPool>();
        private readonly Queue<ImpactBurstLifetime> disabled = new Queue<ImpactBurstLifetime>(CombatHitParticles.MaximumBursts);
        private SceneComponentPool<ImpactBurstLifetime> pool;
        private int sceneHandle;
        public int CreatedCount => pool != null ? pool.CreatedCount : 0;
        public int ActiveCount => pool != null ? pool.ActiveCount : 0;
        public int InactiveCount => pool != null ? pool.InactiveCount : 0;

        public static CombatImpactPool For(Transform region)
        {
            var scene = region != null ? region.gameObject.scene : SceneManager.GetActiveScene();
            if (scenes.TryGetValue(scene.handle, out var existing) && existing != null) return existing;
            var storage = new GameObject("Combat impacts · reusable");
            SceneManager.MoveGameObjectToScene(storage, scene);
            var created = storage.AddComponent<CombatImpactPool>();
            scenes[scene.handle] = created;
            return created;
        }

        private void Awake()
        {
            sceneHandle = gameObject.scene.handle;
            pool = new SceneComponentPool<ImpactBurstLifetime>(transform, CreateBurst, 6,
                CombatHitParticles.MaximumBursts, CombatHitParticles.MaximumBursts);
        }
        private ImpactBurstLifetime CreateBurst(Transform storage)
        {
            var go = new GameObject("Impacto · Combat FX");
            go.SetActive(false); go.transform.SetParent(storage, false);
            go.AddComponent<SpriteRenderer>(); go.AddComponent<CombatSpriteEffect>();
            return go.AddComponent<ImpactBurstLifetime>();
        }
        public ImpactBurstLifetime Rent()
        {
            DrainDisabled();
            var burst = pool.Rent();
            if (burst != null) burst.Lease(this);
            return burst;
        }
        internal void Return(ImpactBurstLifetime burst) { if (pool != null) pool.Return(burst); }
        internal void Deactivated(ImpactBurstLifetime burst) => disabled.Enqueue(burst);
        internal void Forget(ImpactBurstLifetime burst) { if (pool != null) pool.Forget(burst); }
        private void Update() => DrainDisabled();
        private void DrainDisabled()
        {
            while (disabled.Count > 0)
            {
                var burst = disabled.Dequeue();
                if (burst != null) burst.ReturnToPool();
            }
        }
        private void OnDestroy()
        {
            if (scenes.TryGetValue(sceneHandle, out var owner) && owner == this) scenes.Remove(sceneHandle);
            disabled.Clear(); pool?.Dispose();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetScenes() => scenes.Clear();
    }
}
