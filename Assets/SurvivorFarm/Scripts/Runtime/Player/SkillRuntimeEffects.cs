using System.Collections.Generic;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    /// <summary>Small, pooled-friendly bridge from skill events to existing combat systems.</summary>
    [DisallowMultipleComponent]
    public sealed class SkillRuntimeEffects : MonoBehaviour
    {
        private SkillTreeManager manager;
        private PlayerSurvivalStats stats;
        private PlayerInventory inventory;
        private readonly Collider2D[] areaHits = new Collider2D[24];
        private readonly HashSet<IDamageable> targets = new HashSet<IDamageable>(24);
        private float nextRegen;
        private float nextDeathAura;

        private void Awake()
        {
            manager = GetComponent<SkillTreeManager>() ?? gameObject.AddComponent<SkillTreeManager>();
            stats = GetComponent<PlayerSurvivalStats>();
            inventory = GetComponent<PlayerInventory>();
            manager.Events.OnHit += OnHit;
            manager.Events.OnEnemyKilled += OnEnemyKilled;
            manager.Events.OnDamageTaken += OnDamageTaken;
            manager.Events.OnDash += OnDash;
        }

        private void OnDestroy()
        {
            if (manager == null || manager.Events == null) return;
            manager.Events.OnHit -= OnHit;
            manager.Events.OnEnemyKilled -= OnEnemyKilled;
            manager.Events.OnDamageTaken -= OnDamageTaken;
            manager.Events.OnDash -= OnDash;
        }

        private void Update()
        {
            if (stats == null || stats.CurrentHealth <= 0) return;
            if (manager.GetLevel("survival_regen") > 0 && Time.time >= nextRegen)
            {
                nextRegen = Time.time + 4f;
                stats.Heal(1);
            }

            if (manager.GetLevel("chaos_death_aura") > 0 && Time.time >= nextDeathAura)
            {
                nextDeathAura = Time.time + 1.5f;
                for (int i = EnemyAIBase.ActiveEnemies.Count - 1; i >= 0; i--)
                {
                    EnemyAIBase enemy = EnemyAIBase.ActiveEnemies[i];
                    if (enemy != null && enemy.IsAlive && Vector2.Distance(transform.position, enemy.transform.position) <= 1.25f)
                        enemy.TakeDamage(1, inventory);
                }
            }
        }

        private void OnHit(SkillEventContext context)
        {
            if (context.Target == null) return;
            float lifeSteal = manager.LifeSteal;
            if (lifeSteal > 0f && stats != null) stats.Heal(Mathf.Max(1, Mathf.RoundToInt(context.Amount * lifeSteal)));
            if (manager.Events.ChainDepth == 1 && manager.GetLevel("magic_fire") > 0 && context.Charged && context.Target.TryGetComponent<EnemyAIBase>(out EnemyAIBase burningEnemy))
                burningEnemy.TakeDamage(1, inventory);
            if (manager.Events.ChainDepth == 1 && manager.GetLevel("force_shockwave") > 0 && context.Charged)
                AreaDamage(context.Position, Mathf.Max(1, Mathf.RoundToInt(context.Amount * .45f)), 1.4f, context.Target);
        }

        private void OnDash(SkillEventContext context)
        {
            if (manager.GetLevel("mobility_offensive_dash") > 0)
                AreaDamage(context.Position, 1, 1.1f, null);
        }

        private void OnEnemyKilled(SkillEventContext context)
        {
            if (stats != null && manager.GetLevel("survival_devourer") > 0) stats.Heal(2);
            if (manager.GetLevel("chaos_corpse_explosion") > 0)
                AreaDamage(context.Position, Mathf.Max(1, Mathf.RoundToInt(context.Amount * .55f)), 1.5f, context.Target);
        }

        private void OnDamageTaken(SkillEventContext context)
        {
            if (manager.GetLevel("survival_regen") > 0 && stats != null) nextRegen = Mathf.Min(nextRegen, Time.time + 1f);
        }

        private void AreaDamage(Vector2 position, int damage, float radius, GameObject ignored)
        {
            if (!manager.Events.TryEnterChain()) return;
            try
            {
                int count = Physics2D.OverlapCircleNonAlloc(position, radius, areaHits);
                targets.Clear();
                for (int i = 0; i < count; i++)
                {
                    IDamageable damageable = areaHits[i] != null ? areaHits[i].GetComponentInParent<IDamageable>() : null;
                    if (damageable == null || damageable.Transform == transform || damageable.Transform == null ||
                        (ignored != null && damageable.Transform == ignored.transform) || !DamageRules.CanPlayerHit(damageable)) continue;
                    targets.Add(damageable);
                }
                foreach (IDamageable damageable in targets)
                {
                    if (damageable is EnemyAIBase enemy) enemy.TakeDamage(damage, GetComponent<PlayerInventory>());
                    else damageable.TakeDamage(damage, GetComponent<PlayerInventory>());
                }
            }
            finally
            {
                targets.Clear();
                manager.Events.ExitChain();
            }
        }
    }
}
