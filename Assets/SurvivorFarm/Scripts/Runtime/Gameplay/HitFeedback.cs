using System;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public readonly struct ResolvedHit
    {
        public readonly GameObject Target;
        public readonly int Damage;
        public readonly bool Heavy, Killed, Finisher;
        public ResolvedHit(GameObject target, int damage, bool heavy, bool killed, bool finisher)
        { Target = target; Damage = damage; Heavy = heavy; Killed = killed; Finisher = finisher; }
    }

    /// <summary>Presentation of accepted damage, not health ownership or target selection.</summary>
    [DisallowMultipleComponent]
    public sealed class HitFeedback : MonoBehaviour
    {
        private AudioFeedback audioFeedback;
        private GameFeelFeedback labels;
        private CombatTimeFeedback timing;
        private IDamageable resolvingTarget;
        private bool resolvingHeavy;
        private int strike, cameraStrike = -1;
        private float nextFinisher;
        public int FinisherCount { get; private set; }
        public event Action<ResolvedHit> HitResolved;

        private void Awake()
        {
            audioFeedback = GetComponent<AudioFeedback>() ?? gameObject.AddComponent<AudioFeedback>();
            labels = GetComponent<GameFeelFeedback>();
            timing = GetComponent<CombatTimeFeedback>() ?? gameObject.AddComponent<CombatTimeFeedback>();
        }

        public void BeginStrike() => strike++;

        public void ApplyDamage(IDamageable target, int damage, PlayerInventory source, bool heavy)
        {
            var previousTarget = resolvingTarget;
            bool previousHeavy = resolvingHeavy;
            resolvingTarget = target; resolvingHeavy = heavy;
            try { target.TakeDamage(damage, source); }
            finally { resolvingTarget = previousTarget; resolvingHeavy = previousHeavy; }
        }

        public static bool IsHeavy(GameObject target, PlayerInventory source)
        {
            var feedback = source != null ? source.GetComponent<HitFeedback>() : null;
            return feedback != null && feedback.resolvingHeavy && feedback.resolvingTarget != null &&
                feedback.resolvingTarget.Transform == target.transform;
        }

        // Receivers call only after accepting damage, so immunity/cover/duplicate hits stay silent.
        public static void Report(GameObject target, PlayerInventory source, int damage, bool killed,
            bool elite = false, ImpactSurface surface = ImpactSurface.Creature)
        {
            bool heavy = IsHeavy(target, source);
            VisibleHitFeedback.Play(target, heavy ? .11f : .075f, false);
            var renderer = target.GetComponentInChildren<SpriteRenderer>();
            Vector3 point = renderer != null ? renderer.bounds.center : target.transform.position;
            CombatHitParticles.Spawn(point, target.transform.parent, surface, heavy, killed);
            var feedback = source != null ? source.GetComponent<HitFeedback>() : null;
            if (feedback != null) feedback.Accept(target, point, damage, heavy, killed, elite, surface);
            else AudioFeedback.PlayAt(surface == ImpactSurface.Stone ? CombatSound.Mine :
                surface == ImpactSurface.Wood ? (killed ? CombatSound.Break : CombatSound.Chop) : CombatSound.Impact, point);
        }

        private void Accept(GameObject target, Vector3 point, int damage, bool heavy, bool killed, bool elite, ImpactSurface surface)
        {
            bool finisher = heavy && killed && elite && Time.unscaledTime >= nextFinisher;
            if (finisher) { nextFinisher = Time.unscaledTime + 3; FinisherCount++; }
            labels?.Pulse(heavy ? "−" + damage + "!" : "−" + damage, point, true, false, false);
            CombatSound sound = finisher ? CombatSound.Finisher : surface == ImpactSurface.Wood ?
                (killed ? CombatSound.Break : CombatSound.Chop) : surface == ImpactSurface.Stone ? CombatSound.Mine :
                heavy ? CombatSound.HeavyImpact : CombatSound.Impact;
            audioFeedback.Play(sound, point);
            if (killed && !finisher && surface == ImpactSurface.Creature) audioFeedback.Play(CombatSound.Death, point, .6f);
            // One camera/time impulse per sweep, upgraded if a later target is an elite kill.
            if (resolvingTarget != null && (cameraStrike != strike || finisher))
            {
                cameraStrike = strike;
                timing.Impact(heavy, finisher);
                if (Camera.main != null)
                {
                    var cameraFeedback = Camera.main.GetComponent<CameraFeedback>() ?? Camera.main.gameObject.AddComponent<CameraFeedback>();
                    cameraFeedback.Impact(point, heavy, finisher);
                }
            }
            HitResolved?.Invoke(new ResolvedHit(target, damage, heavy, killed, finisher));
        }

        public void PlayerHurt(int damage, bool dead)
        {
            labels?.Pulse("−" + damage, transform.position, true, false, false);
            audioFeedback.Play(dead ? CombatSound.Death : CombatSound.Hurt, transform.position);
            VisibleHitFeedback.Play(gameObject, .085f, false);
            CombatHitParticles.Spawn(transform.position, null, ImpactSurface.Player, false, dead);
            if (Camera.main != null)
                (Camera.main.GetComponent<CameraFeedback>() ?? Camera.main.gameObject.AddComponent<CameraFeedback>()).Impact(transform.position, false, false);
        }
    }
}
