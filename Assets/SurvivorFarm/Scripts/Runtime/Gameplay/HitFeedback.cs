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
        public readonly bool Heavy, Killed, Finisher, Charged;
        public ResolvedHit(GameObject target, int damage, bool heavy, bool killed, bool finisher,bool charged=false)
        { Target = target; Damage = damage; Heavy = heavy; Killed = killed; Finisher = finisher;Charged=charged; }
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
        private bool resolvingCharged;
        private FarmTool resolvingTool;
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

        public void ApplyDamage(IDamageable target, int damage, PlayerInventory source, bool heavy, FarmTool tool=FarmTool.Sword,bool charged=false)
        {
            var previousTarget = resolvingTarget;
            bool previousHeavy = resolvingHeavy;
            bool previousCharged=resolvingCharged;resolvingCharged=charged;
            var previousTool=resolvingTool;resolvingTool=tool;
            resolvingTarget = target; resolvingHeavy = heavy;
            try { target.TakeDamage(damage, source); }
            finally { resolvingTarget = previousTarget; resolvingHeavy = previousHeavy;resolvingTool=previousTool;resolvingCharged=previousCharged; }
        }

        public static bool IsHeavy(GameObject target, PlayerInventory source)
        {
            var feedback = source != null ? source.GetComponent<HitFeedback>() : null;
            return feedback != null && feedback.resolvingHeavy && feedback.resolvingTarget != null &&
                feedback.resolvingTarget.Transform == target.transform;
        }
        public static bool IsCharged(GameObject target,PlayerInventory source)
        {
            var feedback=source!=null?source.GetComponent<HitFeedback>():null;
            return feedback!=null&&feedback.resolvingCharged&&feedback.resolvingTarget?.Transform==target.transform;
        }

        // Receivers call only after accepting damage, so immunity/cover/duplicate hits stay silent.
        public static void Report(GameObject target, PlayerInventory source, int damage, bool killed,
            bool elite = false, ImpactSurface surface = ImpactSurface.Creature)
        {
            bool heavy = IsHeavy(target, source);
            VisibleHitFeedback.Play(target, heavy ? .12f : .085f, false);
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
            if(resolvingTarget!=null&&surface==ImpactSurface.Creature&&target.GetComponent<TrainingEnemy>()==null)GetComponent<ToolMastery>()?.Earn(resolvingTool);
            bool finisher = heavy && killed && elite && Time.unscaledTime >= nextFinisher;
            if (finisher) { nextFinisher = Time.unscaledTime + 3; FinisherCount++; }
            labels?.Pulse(heavy ? "−" + damage + "!" : "−" + damage, point, true, false, false);
            bool charged=IsCharged(target,GetComponent<PlayerInventory>());
            CombatSound sound = finisher ? CombatSound.Finisher : charged?CombatSound.ChargedImpact:surface == ImpactSurface.Wood ?
                (killed ? CombatSound.Break : CombatSound.Chop) : surface == ImpactSurface.Stone ? CombatSound.Mine :
                heavy ? CombatSound.HeavyImpact : CombatSound.Impact;
            audioFeedback.Play(sound, point);
            if (killed && !finisher && surface == ImpactSurface.Creature)
                audioFeedback.Play(target.GetComponent<AnimalResource>() is AnimalResource animal?animal.DeathSound:CombatSound.Death, point, .85f);
            // One camera/time impulse per sweep, upgraded if a later target is an elite kill.
            if (resolvingTarget != null && (cameraStrike != strike || finisher))
            {
                cameraStrike = strike;
                timing.Impact(heavy, finisher,charged);
                if (Camera.main != null)
                {
                    var cameraFeedback = Camera.main.GetComponent<CameraFeedback>() ?? Camera.main.gameObject.AddComponent<CameraFeedback>();
                    cameraFeedback.Impact(point, heavy, finisher,charged);
                }
            }
            HitResolved?.Invoke(new ResolvedHit(target, damage, heavy, killed, finisher,charged));
            GetComponent<SkillTreeManager>()?.NotifyHit(target, damage, resolvingTool, heavy, charged);
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
