using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Hit feedback uses the supplied impact animation and leaves character materials intact.</summary>
    public sealed class VisibleHitFeedback : MonoBehaviour
    {
        private float until;
        public bool IsFlashing => Time.unscaledTime < until;

        public static void Play(GameObject target, float duration = .12f, bool particles = true)
        {
            var effect = target.GetComponent<VisibleHitFeedback>();
            if (effect == null) effect = target.AddComponent<VisibleHitFeedback>();
            effect.until = Time.unscaledTime + duration;
            // Receivers passing false already dispatch their material-specific authored impact.
            if (!particles) return;
            var renderer = target.GetComponentInChildren<SpriteRenderer>();
            var position = renderer != null ? renderer.bounds.center : target.transform.position;
            CombatHitParticles.Spawn(position, target.transform.parent, ImpactSurface.Creature, false, false);
        }

        public void ResetFlash() => until = 0;
        private void OnDisable() => ResetFlash();
    }
}
