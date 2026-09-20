using UnityEngine;
using System.Collections.Generic;
using SurvivorFarm.Runtime.Player;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class ArrowProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private float hitDistance = 0.28f;
        [SerializeField] private float lifetime = 1.2f;

        private IDamageable target;
        private PlayerInventory source;
        private Transform shooter;
        private int targetGeneration;
        private int damage;
        private float destroyAt;
        private bool resolved;
        private bool playerShot;
        private bool directional;
        private Vector2 heading;
        private PlayerProjectilePool pool;
        private bool leased;
        private bool hasAuthoredSpeed;
        private float authoredSpeed;
        private readonly List<RaycastHit2D> castHits = new List<RaycastHit2D>(16);
        private static readonly ContactFilter2D ProjectileFilter = new ContactFilter2D { useTriggers = true };
        public bool IsManual => directional;
        public Vector2 Direction => heading;

        internal void Lease(PlayerProjectilePool owner) { pool = owner; leased = true; }

        public void ReturnToPool()
        {
            if (pool != null && !leased) return;
            bool returnLease = leased && pool != null;
            leased = false;
            ResetFlight();
            if (returnLease) pool.Return(this);
            else if (gameObject != null) Destroy(gameObject);
        }

        public void ConfigureDirection(Vector2 direction, int arrowDamage, PlayerInventory attacker, float range = 9f)
        {
            CaptureAuthoredSpeed();
            ResetFlight();
            target = null; source = attacker; playerShot = true; directional = true;
            shooter = attacker != null ? attacker.transform : null;
            heading = direction.sqrMagnitude > .0001f ? direction.normalized : Vector2.down;
            damage = Mathf.Max(1, arrowDamage); speed = 12f;
            destroyAt = Time.time + Mathf.Max(.1f, range) / speed;
            resolved = false; Face(heading);
        }

        public void Configure(IDamageable enemyTarget, int arrowDamage, PlayerInventory attacker, bool playerControlled = true,
            Transform shooterTransform = null)
        {
            CaptureAuthoredSpeed();
            ResetFlight();
            target = enemyTarget;
            directional = false;
            source = attacker;
            shooter = shooterTransform != null ? shooterTransform : attacker != null ? attacker.transform : null;
            playerShot = playerControlled;
            targetGeneration = target != null ? target.SpawnGeneration : 0;
            damage = Mathf.Max(1, arrowDamage);
            speed = authoredSpeed;
            destroyAt = Time.time + lifetime;
            resolved = false;
            if (TargetIsValid()) Face(target.Transform.position - transform.position);
        }

        private void Update()
        {
            if (resolved) return;
            if (directional) { TickDirectional(); return; }
            if (!TargetIsValid() || Time.time >= destroyAt)
            {
                ReturnToPool();
                return;
            }

            if (Time.timeScale == 0f) return;
            Vector3 targetPosition = target.Transform.position;
            Vector3 next = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            bool impact = (targetPosition - next).sqrMagnitude <= hitDistance * hitDistance;
            // Recheck cover in flight; a moving target can step behind a wall after release.
            Physics2D.Linecast(transform.position, impact ? targetPosition : next, ProjectileFilter, castHits);
            foreach (var hit in castHits)
            {
                if (hit.collider.isTrigger || hit.transform.IsChildOf(transform) ||
                    hit.transform.IsChildOf(target.Transform) || shooter != null && hit.transform.IsChildOf(shooter) ||
                    source != null && hit.transform.IsChildOf(source.transform)) continue;
                resolved = true;
                ReturnToPool();
                return;
            }

            Face(targetPosition - transform.position);
            transform.position = next;
            if (impact)
            {
                resolved = true;
                var feedback = playerShot && source != null ? source.GetComponent<HitFeedback>() : null;
                if (feedback != null)
                {
                    feedback.BeginStrike();
                    feedback.ApplyDamage(target, damage, source, false,FarmTool.Bow);
                }
                else target.TakeDamage(damage, source);
                ReturnToPool();
            }
        }

        private void TickDirectional()
        {
            if (Time.time >= destroyAt) { ReturnToPool(); return; }
            if (Time.timeScale <= 0) return;
            float step = speed * Time.deltaTime;
            Physics2D.CircleCast(transform.position, .065f, heading, ProjectileFilter, castHits, step);
            foreach (var hit in castHits)
            {
                if (hit.transform.IsChildOf(transform) || shooter != null && hit.transform.IsChildOf(shooter) ||
                    source != null && hit.transform.IsChildOf(source.transform)) continue;
                var candidate = hit.collider.GetComponentInParent<IDamageable>();
                bool damageable = candidate != null && DamageRules.CanPlayerHit(candidate);
                if (!damageable && hit.collider.isTrigger) continue;
                resolved = true;
                transform.position = hit.point;
                if (damageable)
                {
                    var feedback = source != null ? source.GetComponent<HitFeedback>() : null;
                    if (feedback != null) { feedback.BeginStrike(); feedback.ApplyDamage(candidate, damage, source, false, FarmTool.Bow); }
                    else candidate.TakeDamage(damage, source);
                }
                ReturnToPool(); return;
            }
            transform.position += (Vector3)(heading * step);
        }

        private bool TargetIsValid() => target != null && !(target is Object instance && instance == null) &&
            target.Transform != null && target.IsAlive && target.SpawnGeneration == targetGeneration;

        private void Face(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f) return;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void ResetFlight()
        {
            resolved = true; target = null; source = null; shooter = null; targetGeneration = 0;
            heading = Vector2.zero; directional = false; playerShot = false;
            damage = 0; destroyAt = 0; castHits.Clear();
        }

        private void CaptureAuthoredSpeed()
        {
            if (hasAuthoredSpeed) return;
            hasAuthoredSpeed = true; authoredSpeed = speed;
        }

        private void OnDisable()
        {
            ResetFlight();
            // Reparenting during a parent's activation callback is unsafe; the owner drains next tick.
            if (leased && pool != null) pool.Deactivated(this);
        }
        private void OnDestroy() { if (pool != null) pool.Forget(this); }
    }
}
