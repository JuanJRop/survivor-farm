using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    /// <summary>A short authored-sprite fall remains visible after the harvestable body is depleted.</summary>
    public sealed class AnimalDeathVisual : MonoBehaviour
    {
        private SpriteRenderer visual;
        private Vector3 scale, origin;
        private float started;
        public static void Spawn(SpriteRenderer source)
        {
            var go = new GameObject("Animal · caída");
            go.transform.SetParent(source.transform.parent, false);
            go.transform.position = source.transform.position;
            go.transform.rotation = source.transform.rotation;
            go.transform.localScale = source.transform.localScale;
            // The animal's root may be disabled by the harvest contract; the death pose lives alongside its spawn.
            if (source.GetComponentInParent<Gameplay.AnimalResource>() is Gameplay.AnimalResource animal)
                go.transform.SetParent(animal.transform.parent, true);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = source.sprite; sr.flipX = source.flipX; sr.flipY = source.flipY;
            sr.sortingLayerID = source.sortingLayerID; sr.sortingOrder = source.sortingOrder;
            var fall = go.AddComponent<AnimalDeathVisual>();
            fall.visual = sr; fall.scale = go.transform.localScale; fall.origin = go.transform.position; fall.started = Time.time;
        }
        private void Update()
        {
            float t = Mathf.Clamp01((Time.time - started) / .5f);
            transform.position = origin + Vector3.up * Mathf.Sin(t * Mathf.PI) * .12f;
            transform.localScale = Vector3.Scale(scale, new Vector3(1f + t * .12f, 1f - t * .55f, 1));
            visual.color = new Color(1, 1 - t * .25f, 1 - t * .25f, 1 - Mathf.SmoothStep(0, 1, Mathf.Max(0, t - .35f) / .65f));
            if (t >= 1) Destroy(gameObject);
        }
    }
}
