using UnityEngine;
using UnityEngine.EventSystems;

namespace SurvivorFarm.Runtime.UI
{
    public sealed class InteractionPromptAnimation : MonoBehaviour, IPointerDownHandler
    {
        private float shownAt;
        private float pressedAt = -10f;
        private void OnEnable()
        {
            shownAt = Time.unscaledTime;
            pressedAt = -10f;
        }
        public void Press() => pressedAt = Time.unscaledTime;
        public void OnPointerDown(PointerEventData eventData) => Press();
        private void LateUpdate()
        {
            float entry = Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - shownAt) / 0.18f);
            float press = Mathf.Clamp01((Time.unscaledTime - pressedAt) / 0.16f);
            transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, entry) * Mathf.Lerp(0.86f, 1f, press);
        }
        private void OnDisable() => transform.localScale = Vector3.one;
    }
}
