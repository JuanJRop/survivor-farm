using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public enum ImpactSurface { Creature, Wood, Stone, Leaves, Player }

    // The existing entry point is shared by weapons, harvesting, enemies and construction.
    public static class CombatHitParticles
    {
        private static int activeBursts;
        public static int ActiveBursts => activeBursts;
        public const int MaximumBursts = 18;
        public static void Spawn(Vector3 position, Transform region, Material material)
            => Spawn(position, region, ImpactSurface.Creature, false, false);

        public static void Spawn(Vector3 position, Transform region, ImpactSurface surface, bool heavy, bool killed)
        {
            if (!GameFeelFeedback.Enabled || activeBursts >= MaximumBursts || CombatFxLibrary.Atlas == null) return;
            if (region != null && !region.gameObject.activeInHierarchy) return;
            var burst = CombatImpactPool.For(region).Rent();
            if (burst == null) return;
            burst.transform.SetParent(region, true);
            burst.transform.position = position;
            burst.transform.rotation = Quaternion.identity;
            int row = surface == ImpactSurface.Stone ? CombatFxLibrary.StoneImpact :
                (heavy || killed) && surface == ImpactSurface.Creature ? CombatFxLibrary.HeavyImpact : CombatFxLibrary.Impact;
            Color tint = surface == ImpactSurface.Leaves ? new Color(.62f, 1f, .4f) :
                surface == ImpactSurface.Player ? new Color(1f, .5f, .5f) : Color.white;
            burst.Play(row, heavy || killed ? .4f : .28f,
                heavy ? 1.7f : killed ? 1.35f : 1.05f, tint);
        }

        internal static void Activated() => activeBursts++;
        internal static void Released() => activeBursts = Mathf.Max(0, activeBursts - 1);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => activeBursts = 0;
    }

    public sealed class ImpactBurstLifetime : MonoBehaviour
    {
        private float remaining = .48f;
        private CombatImpactPool pool;
        private CombatSpriteEffect effect;
        private bool leased, counted;
        internal void Lease(CombatImpactPool owner) { pool = owner; leased = true; }
        internal void Play(int row, float duration, float diameter, Color tint)
        {
            remaining = .48f;
            if (effect == null) effect = GetComponent<CombatSpriteEffect>();
            effect.Play(row, duration, diameter, 12000, tint);
            counted = true; CombatHitParticles.Activated();
            gameObject.SetActive(true);
        }
        public void ReturnToPool()
        {
            if (pool != null && !leased) return;
            bool returnLease = leased && pool != null;
            leased = false; remaining = 0;
            ReleaseCount(); effect?.Stop();
            if (returnLease) pool.Return(this);
            else Destroy(gameObject);
        }
        private void Update() { remaining -= Time.unscaledDeltaTime; if (remaining <= 0) ReturnToPool(); }
        private void ReleaseCount() { if (!counted) return; counted = false; CombatHitParticles.Released(); }
        private void OnDisable()
        {
            ReleaseCount();
            if (leased && pool != null) pool.Deactivated(this);
        }
        private void OnDestroy() { ReleaseCount(); if (pool != null) pool.Forget(this); }
    }
}
