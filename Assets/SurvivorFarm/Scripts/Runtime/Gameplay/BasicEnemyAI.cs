using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class BasicEnemyAI : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2.1f;
        [SerializeField] private float attackRange = 0.75f;
        [SerializeField] private float attackInterval = 1.25f;
        [SerializeField] private int damage = 1;
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private int coinReward = 2;
        [SerializeField] private string enemyName = "Limo";
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer healthFillRenderer;
        [SerializeField] private Color bodyColor = new Color(0.72f, 0.16f, 0.18f);
        [SerializeField] private Color hurtColor = new Color(1f, 0.92f, 0.35f);
        [SerializeField] private float hurtFlashDuration = 0.12f;

        private Transform target;
        private DungeonEnemyPool pool;
        private int currentHealth;
        private float nextAttackTime;
        private float hurtFlashEndsAt;

        public void Configure(Transform playerTarget, DungeonEnemyPool ownerPool)
        {
            target = playerTarget;
            pool = ownerPool;
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponent<SpriteRenderer>();
            }
        }

        public void ConfigureVisuals(SpriteRenderer body, SpriteRenderer healthFill, Color color, string displayName)
        {
            bodyRenderer = body;
            healthFillRenderer = healthFill;
            bodyColor = color;
            enemyName = displayName;
            ApplyVisuals();
        }

        public void ConfigureStats(string displayName, int health, int attackDamage, float speed, float range, float attackSeconds, int reward)
        {
            enemyName = displayName;
            maxHealth = Mathf.Max(1, health);
            damage = Mathf.Max(1, attackDamage);
            moveSpeed = Mathf.Max(0.4f, speed);
            attackRange = Mathf.Max(0.25f, range);
            attackInterval = Mathf.Max(0.25f, attackSeconds);
            coinReward = Mathf.Max(0, reward);
        }

        public void ActivateFromPool(Vector3 position)
        {
            transform.position = position;
            currentHealth = maxHealth;
            nextAttackTime = 0f;
            hurtFlashEndsAt = 0f;
            gameObject.SetActive(true);
            ApplyVisuals();
        }

        public void ReturnToPool()
        {
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (target == null)
            {
                return;
            }

            ApplyVisuals();
            Vector3 toTarget = target.position - transform.position;
            float distance = toTarget.magnitude;

            if (distance > attackRange)
            {
                transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
                return;
            }

            if (Time.time >= nextAttackTime)
            {
                PlayerSurvivalStats stats = target.GetComponent<PlayerSurvivalStats>();
                stats?.TakeDamage(damage);
                nextAttackTime = Time.time + attackInterval;
                FarmNotificationCenter.Show($"{enemyName} te golpeo: -{damage} vida.");
            }
        }

        private void OnMouseDown()
        {
            TakeDamage(1);
        }

        public void TakeDamage(int amount)
        {
            int finalDamage = Mathf.Max(1, amount);
            currentHealth -= finalDamage;
            hurtFlashEndsAt = Time.time + hurtFlashDuration;
            FarmGameEvents.RaiseEnemyDamaged();
            FarmNotificationCenter.Show($"{enemyName} recibio {finalDamage} de dano.");
            if (currentHealth <= 0)
            {
                PlayerInventory inventory = target != null ? target.GetComponent<PlayerInventory>() : null;
                inventory?.AddCoins(coinReward);
                FarmGameEvents.RaiseEnemyDefeated();
                FarmNotificationCenter.Show($"Derrotaste a {enemyName}. +{coinReward} oro.");
                ReturnToPool();
                return;
            }

            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.color = Time.time < hurtFlashEndsAt ? hurtColor : bodyColor;
            }

            if (healthFillRenderer != null)
            {
                float healthPercent = maxHealth <= 0 ? 0f : Mathf.Clamp01((float)currentHealth / maxHealth);
                healthFillRenderer.transform.localScale = new Vector3(healthPercent, 1f, 1f);
                healthFillRenderer.enabled = gameObject.activeInHierarchy;
            }
        }
    }
}
