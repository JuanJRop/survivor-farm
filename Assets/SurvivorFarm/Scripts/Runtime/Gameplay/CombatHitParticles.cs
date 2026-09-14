using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public static class CombatHitParticles
    {
        static Material sparks;
        public static void Spawn(Vector3 position, Transform region, Material material)
        {
            if (material == null) return;
            var root = new GameObject("Hit Sparks");
            root.transform.SetParent(region, true);
            root.transform.position = position;
            var particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.3f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.16f);
            main.startColor = Color.white;
            main.maxParticles = 12;
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
            if(sparks==null)sparks=new Material(Shader.Find("Sprites/Default"));
            renderer.sharedMaterial = sparks;
            renderer.sortingOrder = 20001;
            particles.Play();
            particles.Emit(12);
            Object.Destroy(root, 0.5f);
        }
    }
}
