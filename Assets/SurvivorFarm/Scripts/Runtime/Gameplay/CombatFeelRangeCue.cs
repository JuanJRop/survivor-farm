using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class CombatFeelRangeCue : MonoBehaviour
    {
        private const int Segments = 48;
        private const float Duration = 0.18f;
        private LineRenderer ring;
        private LineRenderer slash;
        private Vector3 center;
        private float radius;
        private bool heavy;
        private int step;
        private Material material;
        private float until;

        public void Show(Vector3 center, float radius, bool heavy = false, int step = 1)
        {
            this.center = center; this.radius = radius; this.heavy = heavy; this.step = step;
            if (ring == null)
            {
                var child = new GameObject("Barrido de espada");
                child.transform.SetParent(transform, false);
                ring = child.AddComponent<LineRenderer>();
                material = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontSave };
                ring.sharedMaterial = material;
                ring.useWorldSpace = true;
                ring.loop = true;
                ring.positionCount = Segments;
                ring.widthMultiplier = 0.035f;
                ring.sortingOrder = 11998;
                var stroke = new GameObject("Trazo de espada");
                stroke.transform.SetParent(transform, false);
                slash = stroke.AddComponent<LineRenderer>();
                slash.sharedMaterial = material; slash.useWorldSpace = true;
                slash.positionCount = 28; slash.sortingOrder = 11999;
                slash.widthCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(.7f, 1), new Keyframe(1, .15f));
            }
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                ring.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
            until = Time.unscaledTime + Duration;
            ring.enabled = true;
            Refresh();
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            if (ring == null) return;
            float fade = Mathf.Clamp01((until - Time.unscaledTime) / Duration);
            Color tint = heavy ? new Color(1f, .76f, .32f) : new Color(.94f, .99f, .86f);
            ring.startColor = ring.endColor = new Color(tint.r, tint.g, tint.b, fade * (heavy ? .35f : .17f));
            ring.enabled = fade > 0f;
            slash.enabled = ring.enabled;
            slash.widthMultiplier = heavy ? .145f : .075f;
            slash.startColor = new Color(tint.r, tint.g, tint.b, 0);
            slash.endColor = new Color(tint.r, tint.g, tint.b, fade * .9f);
            float direction = step == 2 ? -1 : 1;
            for (int i = 0; i < slash.positionCount; i++)
            {
                float angle = direction * ((1 - fade) * 5.5f + i / 27f * 2.3f);
                slash.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius * .96f);
            }
        }

        private void OnDisable() { if (ring != null) ring.enabled = false; if(slash != null)slash.enabled=false; until = 0f; }
        private void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (ring != null) Destroy(ring.gameObject);
            if (slash != null) Destroy(slash.gameObject);
        }
    }
}
