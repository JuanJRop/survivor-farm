using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    public sealed class MovementSpriteAnimation : MonoBehaviour
    {
        public SpriteRenderer Visual;
        public Sprite[] Idle, Walk;
        private Vector3 previous;
        private float time;
        private void OnEnable() { previous = transform.position; time = 0; }
        private void LateUpdate()
        {
            Vector3 delta = transform.position - previous;
            Sprite[] frames = delta.sqrMagnitude > .000001f ? Walk : Idle;
            if (Mathf.Abs(delta.x) > .001f) Visual.flipX = delta.x > 0;
            time += Time.deltaTime * 7;
            if (Visual != null && frames != null && frames.Length > 0) Visual.sprite = frames[(int)time % frames.Length];
            previous = transform.position;
        }
    }
}
