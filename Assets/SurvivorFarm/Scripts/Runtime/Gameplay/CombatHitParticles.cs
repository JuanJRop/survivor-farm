using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public enum ImpactSurface { Creature, Wood, Stone, Leaves, Player }

    public static class CombatHitParticles
    {
        static Material sparks;
        static int activeBursts;
        public static int ActiveBursts => activeBursts;
        public const int MaximumBursts = 18;
        public static void Spawn(Vector3 position, Transform region, Material material)
            => Spawn(position, region, ImpactSurface.Creature, false, false);

        public static void Spawn(Vector3 position, Transform region, ImpactSurface surface, bool heavy, bool killed)
        {
            if (!GameFeelFeedback.Enabled || activeBursts >= MaximumBursts) return;
            var root = new GameObject("Hit Sparks");
            root.transform.SetParent(region, true);
            root.transform.position = position;
            activeBursts++;
            root.AddComponent<ImpactBurstLifetime>();
            var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.3f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.12f, heavy ? .33f : .24f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(heavy ? 1.5f : .8f, heavy ? 4f : 2.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(.055f, heavy ? .14f : .105f);
            Color color = surface == ImpactSurface.Wood ? new Color(.75f, .48f, .23f) :
                surface == ImpactSurface.Stone ? new Color(.75f, .82f, .86f) :
                surface == ImpactSurface.Leaves ? new Color(.43f, .74f, .28f) :
                surface == ImpactSurface.Player ? new Color(1f, .49f, .35f) :
                heavy || killed ? new Color(1f, .75f, .3f) : new Color(1f, .96f, .75f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, .45f));
            main.maxParticles = 18;
            main.useUnscaledTime = true;
            main.gravityModifier = surface == ImpactSurface.Leaves ? .1f : .35f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.06f;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            if(sparks==null)sparks=new Material(Shader.Find("Sprites/Default")){hideFlags=HideFlags.DontSave};
            renderer.sharedMaterial = sparks;
            renderer.sortingOrder = 12000;
            particles.Play();
            particles.Emit(heavy ? 16 : killed ? 12 : 7);
        }

        internal static void Released() => activeBursts = Mathf.Max(0, activeBursts - 1);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        { if (sparks != null) Object.Destroy(sparks); sparks = null; activeBursts = 0; }
    }

    public sealed class ImpactBurstLifetime : MonoBehaviour
    {
        private float remaining = .48f;
        private void Update() { remaining -= Time.unscaledDeltaTime; if (remaining <= 0) Destroy(gameObject); }
        private void OnDestroy() => CombatHitParticles.Released();
    }
}
