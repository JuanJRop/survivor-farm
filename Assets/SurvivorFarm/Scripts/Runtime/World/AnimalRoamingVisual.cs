using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    /// <summary>Short walks and rests around the spawn, with the pack's matching direction frames.</summary>
    public sealed class AnimalRoamingVisual : MonoBehaviour
    {
        public SpriteRenderer Visual;
        public Sprite[] Idle;
        public Sprite[] Walk;
        public int IdleFrames = 2;
        public int WalkFrames = 4;
        public float Radius = 1.2f;
        public float Speed = .45f;
        public float FootRadius = .16f;
        public bool SideFacesRight;
        private Vector2 home, destination, direction = Vector2.down;
        private float nextDecision, animationTime;
        private bool walking;
        private float frightenedUntil;
        private HarvestableResource resource;
        private readonly Collider2D[] hits = new Collider2D[24];
        public bool IsWalking => walking;

        public void ReactToHit(Vector2 away)
        {
            frightenedUntil = Time.time + 1.5f;
            destination = (Vector2)transform.position + (away.sqrMagnitude > .01f ? away.normalized : Vector2.right) * 2f;
            walking = true; nextDecision = frightenedUntil;
        }

        public void ResetHome()
        {
            home = destination = transform.position;
            walking = false;
            nextDecision = Time.time + 1f;
        }

        private void OnEnable()
        {
            home = transform.position;
            nextDecision = Time.time + Random.Range(.3f, 1.6f);
            walking = false;
            resource = GetComponent<HarvestableResource>();
        }

        private void Update()
        {
            if (resource != null && !resource.IsAvailable) return;
            if (Time.time >= nextDecision)
            {
                walking = !walking;
                destination = home + Random.insideUnitCircle * Radius;
                nextDecision = Time.time + (walking ? Random.Range(1.5f, 3f) : Random.Range(1f, 2.5f));
            }
            if (!walking) return;
            Vector2 delta = destination - (Vector2)transform.position;
            if (delta.magnitude < .06f) { walking = false; nextDecision = Time.time + 1f; return; }
            direction = delta.normalized;
            Vector2 next = Vector2.MoveTowards(transform.position, destination, Speed * (Time.time < frightenedUntil ? 2.8f : 1f) * Time.deltaTime);
            if (!FarmExploration.Contains(next, FootRadius) || FarmExploration.IsRiver(next, FootRadius))
            { walking = false; nextDecision = Time.time + .8f; return; }
            int count = Physics2D.OverlapCircle(next, FootRadius, new ContactFilter2D { useTriggers = false }, hits);
            for (int i = 0; i < count; i++)
            {
                if (hits[i].transform == transform || hits[i].transform.IsChildOf(transform)) continue;
                walking = false; nextDecision = Time.time + .8f; return;
            }
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }

        private void LateUpdate()
        {
            if (Visual == null) return;
            int row = Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ? 0 : direction.y < 0 ? 1 : 2;
            Visual.flipX = row == 0 && (SideFacesRight ? direction.x < 0 : direction.x > 0);
            Sprite[] frames = walking ? Walk : Idle;
            int count = walking ? WalkFrames : IdleFrames;
            animationTime += Time.deltaTime * (walking ? (Time.time < frightenedUntil ? 12f : 8f) : 3f);
            int index = row * count + (int)animationTime % Mathf.Max(1, count);
            if (frames != null && index < frames.Length) Visual.sprite = frames[index];
        }
    }
}
