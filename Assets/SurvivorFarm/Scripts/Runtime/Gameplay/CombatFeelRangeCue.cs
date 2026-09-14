using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class CombatFeelRangeCue : MonoBehaviour
    {
        private const int Segments = 48;
        private const float Duration = 0.18f;
        private LineRenderer ring;
        private Material material;
        private float until;

        public void Show(Vector3 center, float radius)
        {
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
                ring.sortingOrder = 6;
            }
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                ring.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
            until = Time.time + Duration;
            ring.enabled = true;
            Refresh();
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            if (ring == null) return;
            float fade = Mathf.Clamp01((until - Time.time) / Duration);
            ring.startColor = ring.endColor = new Color(0.92f, 0.97f, 0.82f, fade * 0.5f);
            ring.enabled = fade > 0f;
        }

        private void OnDisable() { if (ring != null) ring.enabled = false; until = 0f; }
        private void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (ring != null) Destroy(ring.gameObject);
        }
    }
}
