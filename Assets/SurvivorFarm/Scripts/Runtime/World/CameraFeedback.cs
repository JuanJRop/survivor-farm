using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    /// <summary>Additive camera motion: never changes the follow target or arena framing.</summary>
    [DefaultExecutionOrder(1000), DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class CameraFeedback : MonoBehaviour
    {
        private Camera lens;
        private float startedAt, until, amplitude, zoomUntil;
        private Vector3 actionPoint, appliedOffset;
        private float appliedZoom;
        public float MaximumOffset => amplitude;
        public bool IsFinishing => Time.unscaledTime < zoomUntil;

        private void Awake() => lens = GetComponent<Camera>();

        public void Impact(Vector3 point, bool heavy, bool finisher)
        {
            if (!GameFeelFeedback.Enabled || CombatTimeFeedback.ReducedMotion) return;
            float now = Time.unscaledTime;
            float requested = finisher ? .105f : heavy ? .075f : .035f;
            if (now >= until || requested >= amplitude) { amplitude = requested; startedAt = now; }
            until = Mathf.Max(until, now + (heavy ? .14f : .09f));
            if (finisher) { zoomUntil = now + .32f; actionPoint = point; }
        }

        public void RestoreBasePose()
        {
            transform.position -= appliedOffset;
            if (lens != null) lens.orthographicSize += appliedZoom;
            appliedOffset = Vector3.zero; appliedZoom = 0;
        }

        private void LateUpdate()
        {
            RestoreBasePose();
            if (!GameFeelFeedback.Enabled || CombatTimeFeedback.ReducedMotion || Time.timeScale == 0) return;
            float now = Time.unscaledTime;
            float fade = Mathf.Clamp01((until - now) / Mathf.Max(.01f, until - startedAt));
            float t = (now - startedAt) * 95f;
            appliedOffset = new Vector3(Mathf.Sin(t), Mathf.Sin(t * 1.37f)) * (amplitude * fade * fade);
            if (now < zoomUntil)
            {
                float envelope = Mathf.Sin(Mathf.Clamp01((zoomUntil - now) / .32f) * Mathf.PI);
                Vector3 toward = actionPoint - transform.position; toward.z = 0;
                appliedOffset += Vector3.ClampMagnitude(toward, .5f) * envelope;
                appliedZoom = lens.orthographicSize * .035f * envelope;
                lens.orthographicSize -= appliedZoom;
            }
            transform.position += appliedOffset;
        }

        private void OnDisable() => RestoreBasePose();
    }
}
