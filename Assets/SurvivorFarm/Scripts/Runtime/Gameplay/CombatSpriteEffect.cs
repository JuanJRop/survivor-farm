using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>A reusable authored sprite sequence. Unscaled timing keeps brief hit effects bounded during hit-stop.</summary>
    public sealed class CombatSpriteEffect : MonoBehaviour
    {
        private SpriteRenderer visual;
        private float startedAt, duration;
        public int ClipRow { get; private set; }
        public SpriteRenderer Visual => visual;
        public bool IsPlaying => visual != null && visual.enabled;

        public void Play(int row, float seconds, float diameter, int sortingOrder, Color tint, bool flipY = false)
        {
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            if (visual == null) visual = gameObject.AddComponent<SpriteRenderer>();
            ClipRow = row; startedAt = Time.unscaledTime; duration = Mathf.Max(.01f, seconds);
            visual.sortingOrder = sortingOrder; visual.color = tint; visual.flipY = flipY;
            // Each supplied cell is 64 px at 16 px/unit, including transparent padding.
            transform.localScale = Vector3.one * (diameter / 4f);
            Refresh();
        }

        public void Stop() { duration = 0; if (visual != null) visual.enabled = false; }
        private void Update() => Refresh();
        private void Refresh()
        {
            if (visual == null) return;
            float age = duration <= 0 ? 1 : (Time.unscaledTime - startedAt) / duration;
            visual.sprite = CombatFxLibrary.At(ClipRow, age);
            visual.enabled = age < 1 && GameFeelFeedback.Enabled && visual.sprite != null;
        }
        private void OnDisable() => Stop();
    }
}
