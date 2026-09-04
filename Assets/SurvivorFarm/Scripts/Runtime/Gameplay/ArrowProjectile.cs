using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class ArrowProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private float hitDistance = 0.28f;
        [SerializeField] private float lifetime = 1.2f;

        private BasicEnemyAI target;
        private int damage;
        private float destroyAt;

        public void Configure(BasicEnemyAI enemyTarget, int arrowDamage)
        {
            target = enemyTarget;
            damage = Mathf.Max(1, arrowDamage);
            destroyAt = Time.time + lifetime;
        }

        private void Update()
        {
            if (target == null || !target.gameObject.activeInHierarchy || Time.time >= destroyAt)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            if (toTarget.sqrMagnitude <= hitDistance * hitDistance)
            {
                target.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }

            Vector3 direction = toTarget.normalized;
            transform.position += direction * speed * Time.deltaTime;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
