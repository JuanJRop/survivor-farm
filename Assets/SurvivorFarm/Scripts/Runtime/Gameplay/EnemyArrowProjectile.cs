using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class EnemyArrowProjectile : MonoBehaviour
    {
        private const float Speed = 5.5f, Lifetime = 1.6f, Radius = .08f;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
        private Transform target;
        private PlayerSurvivalStats stats;
        private HomeSafeZone safeZone;
        private Vector2 direction;
        private int generation, damage;
        private float expiresAt;
        public EnemyAIBase Source { get; private set; }
        public Vector2 Direction => direction;

        public void Launch(EnemyAIBase source, Transform player, Vector2 heading, int amount, HomeSafeZone protection)
        {
            Source = source;
            generation = source.SpawnGeneration;
            target = player;
            stats = player.GetComponent<PlayerSurvivalStats>();
            safeZone = protection;
            direction = heading.sqrMagnitude > .0001f ? heading.normalized : Vector2.down;
            damage = amount;
            expiresAt = Time.time + Lifetime;
            transform.position = source.transform.position;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            gameObject.SetActive(true);
        }

        public void ReturnToPool()
        {
            Source = null;
            target = null;
            stats = null;
            safeZone = null;
            direction = Vector2.zero;
            expiresAt = 0;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Source == null || Source.SpawnGeneration != generation || !Source.CanLaunchProjectile ||
                target == null || !target.gameObject.activeInHierarchy || stats == null || stats.CurrentHealth <= 0 || Time.time >= expiresAt ||
                (safeZone != null && safeZone.Contains(target.position)))
            {
                ReturnToPool();
                return;
            }
            Vector2 start = transform.position;
            float distance = Speed * Time.deltaTime;
            Vector2 end = start + direction * distance;
            // Swept protection check prevents a long frame from crossing the entire safe zone.
            if (safeZone != null)
            {
                Vector2 segment = end - start;
                float t = segment.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(safeZone.Center - start, segment) / segment.sqrMagnitude) : 0;
                if (safeZone.Contains(start + segment * t, Radius)) { ReturnToPool(); return; }
            }
            int count = Physics2D.CircleCast(start, Radius, direction, new ContactFilter2D { useTriggers = true }, hits, distance);
            if (count == hits.Length) { ReturnToPool(); return; }
            float nearest = float.PositiveInfinity;
            Collider2D collision = null;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.transform.IsChildOf(Source.transform)) continue;
                if (hit.collider.GetComponentInParent<EnemyAIBase>() != null) continue;
                bool playerHit = hit.transform.IsChildOf(target);
                if (hit.collider.isTrigger && !playerHit) continue;
                if (hit.distance < nearest) { nearest = hit.distance; collision = hit.collider; }
            }
            if (collision != null)
            {
                bool playerHit = collision.transform.IsChildOf(target);
                var victim = stats;
                int amount = damage;
                ReturnToPool();
                if (playerHit) victim.TakeDamage(amount);
                return;
            }
            transform.position = new Vector3(end.x, end.y, transform.position.z);
            var visual = GetComponent<SpriteRenderer>();
            if (visual != null) visual.sortingOrder = Mathf.Max(2200, 1100 - Mathf.RoundToInt(end.y * 20));
        }
    }
}
