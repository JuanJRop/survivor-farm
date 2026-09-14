using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Unscaled, bounded impact timing. An explicit pause always wins.</summary>
    [DisallowMultipleComponent]
    public sealed class CombatTimeFeedback : MonoBehaviour
    {
        private float stopUntil, slowUntil, originalScale = 1f, appliedScale;
        private bool ownsTime;
        public bool IsActive => ownsTime;
        public static bool ReducedMotion { get; set; }

        public void Impact(bool heavy, bool finisher)
        {
            if (!GameFeelFeedback.Enabled || ReducedMotion || Time.timeScale <= 0) return;
            if (ownsTime && !Mathf.Approximately(Time.timeScale, appliedScale)) Cancel(false);
            if (!ownsTime) { originalScale = Time.timeScale; ownsTime = true; }
            float now = Time.unscaledTime;
            stopUntil = Mathf.Max(stopUntil, now + (heavy ? .065f : .032f));
            if (finisher) slowUntil = now + .27f;
            Apply();
        }

        private void Update()
        {
            if (!ownsTime) return;
            // Menus, death, scene transitions and QA speed overrides are not ours to undo.
            if (!Mathf.Approximately(Time.timeScale, appliedScale)) { Cancel(false); return; }
            if (ReducedMotion || !GameFeelFeedback.Enabled || Time.unscaledTime >= Mathf.Max(stopUntil, slowUntil))
            { Cancel(true); return; }
            Apply();
        }

        private void Apply()
        {
            appliedScale = originalScale * (Time.unscaledTime < stopUntil ? .01f : .35f);
            Time.timeScale = appliedScale;
        }

        public void Cancel(bool restore = true)
        {
            if (restore && ownsTime && Mathf.Approximately(Time.timeScale, appliedScale)) Time.timeScale = originalScale;
            ownsTime = false; stopUntil = slowUntil = 0;
        }

        private void OnDisable() => Cancel();
    }
}
