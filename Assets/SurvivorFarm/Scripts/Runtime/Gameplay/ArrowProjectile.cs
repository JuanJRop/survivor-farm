using UnityEngine;
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
        private int targetGeneration;
        private int damage;
        private float destroyAt;
        private bool resolved;
        private bool playerShot;

        public void Configure(IDamageable enemyTarget, int arrowDamage, PlayerInventory attacker, bool playerControlled = true)
        {
            target = enemyTarget;
            source = attacker;
            playerShot = playerControlled;
            targetGeneration = target != null ? target.SpawnGeneration : 0;
            damage = Mathf.Max(1, arrowDamage);
            destroyAt = Time.time + lifetime;
            resolved = false;
            if (TargetIsValid()) Face(target.Transform.position - transform.position);
        }

        private void Update()
        {
            if (resolved) return;
            if (!TargetIsValid() || Time.time >= destroyAt)
            {
                Destroy(gameObject);
                return;
            }

            if (Time.timeScale == 0f) return;
            Vector3 targetPosition = target.Transform.position;
            Vector3 next = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            bool impact = (targetPosition - next).sqrMagnitude <= hitDistance * hitDistance;
            // Recheck cover in flight; a moving target can step behind a wall after release.
            foreach (var hit in Physics2D.LinecastAll(transform.position, impact ? targetPosition : next))
            {
                if (hit.collider.isTrigger || hit.transform.IsChildOf(transform) ||
                    hit.transform.IsChildOf(target.Transform) || source != null && hit.transform.IsChildOf(source.transform)) continue;
                resolved = true;
                Destroy(gameObject);
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
                    feedback.ApplyDamage(target, damage, source, false);
                }
                else target.TakeDamage(damage, source);
                Destroy(gameObject);
            }
        }

        private bool TargetIsValid() => target != null && !(target is Object instance && instance == null) &&
            target.Transform != null && target.IsAlive && target.SpawnGeneration == targetGeneration;

        private void Face(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f) return;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnDisable() { resolved = true; target = null; source = null; }
    }
}
