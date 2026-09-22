using System.Collections;
using System.Collections.Generic;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    /// <summary>Resolves learned skill procs against the existing enemy and health systems.</summary>
    [DisallowMultipleComponent]
    public sealed class SkillRuntimeEffects : MonoBehaviour
    {
        private struct EnemyStatus
        {
            public EnemyAIBase Enemy;
            public int SpawnGeneration;
            public float BleedUntil, NextBleedAt;
            public float BurnUntil, NextBurnAt;
            public float ShockedUntil, FrozenUntil, ArmorBrokenUntil, MarkedUntil;
        }

        private struct PendingBurst
        {
            public Vector2 Position;
            public float FireAt;
            public float Radius;
            public int Damage;
            public string SkillId;
            public float Knockback;
            public bool Freeze;
        }

        private struct CollisionPair
        {
            public Collider2D Player;
            public Collider2D Enemy;
        }

        private const int MaxAreaTargets = 24;
        private static readonly ContactFilter2D SkillTargetFilter = new ContactFilter2D { useTriggers = true };
        private static readonly ContactFilter2D TeleportBlockFilter = new ContactFilter2D { useTriggers = false };
        private readonly Collider2D[] areaHits = new Collider2D[MaxAreaTargets];
        private readonly Collider2D[] teleportHits = new Collider2D[8];
        private readonly IDamageable[] areaTargets = new IDamageable[MaxAreaTargets];
        private readonly EnemyStatus[] enemyStatuses = new EnemyStatus[128];
        private readonly EnemyAIBase[] blinkTargets = new EnemyAIBase[4];
        private readonly EnemyAIBase[] lightningTargets = new EnemyAIBase[8];
        private readonly List<Collider2D> enemyColliders = new List<Collider2D>(8);
        private readonly List<PendingBurst> pendingBursts = new List<PendingBurst>(12);
        private readonly List<CollisionPair> phasedCollisionPairs = new List<CollisionPair>(64);
        private SkillTreeManager manager;
        private SkillCombatEventBus events;
        private PlayerSurvivalStats stats;
        private PlayerInventory inventory;
        private Rigidbody2D body;
        private Collider2D[] playerColliders;
        private float nextRegen;
        private float nextAilmentTick;
        private float nextGodOfWarHeal;
        private float nextDeathAura;
        private float nextAscensionAura;
        private float nextThunderstorm;
        private float comboResetAt;
        private int comboHits;
        private float phaseUntil;
        private Coroutine phaseRoutine;

        private void Awake()
        {
            manager = GetComponent<SkillTreeManager>() ?? gameObject.AddComponent<SkillTreeManager>();
            events = manager.Events;
            stats = GetComponent<PlayerSurvivalStats>();
            inventory = GetComponent<PlayerInventory>();
            body = GetComponent<Rigidbody2D>();
            playerColliders = GetComponentsInChildren<Collider2D>(true);
            events.OnHit += OnHit;
            events.OnEnemyKilled += OnEnemyKilled;
            events.OnEnemyExploded += OnEnemyExploded;
            events.OnDamageTaken += OnDamageTaken;
            events.OnDash += OnDash;
            events.OnSkillUsed += OnSkillUsed;
        }

        private void OnDestroy()
        {
            if (phaseRoutine != null) StopCoroutine(phaseRoutine);
            RestoreEnemyCollision();
            if (events == null) return;
            events.OnHit -= OnHit;
            events.OnEnemyKilled -= OnEnemyKilled;
            events.OnEnemyExploded -= OnEnemyExploded;
            events.OnDamageTaken -= OnDamageTaken;
            events.OnDash -= OnDash;
            events.OnSkillUsed -= OnSkillUsed;
        }

        private void Update()
        {
            if (stats == null) stats = GetComponent<PlayerSurvivalStats>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (playerColliders == null || playerColliders.Length == 0) playerColliders = GetComponentsInChildren<Collider2D>(true);
            if (stats == null || stats.CurrentHealth <= 0) return;
            TickAilments();
            TickBursts();

            if (manager.GetLevel("survival_regen") > 0 && Time.time >= nextRegen)
            {
                nextRegen = Time.time + NodeCooldown("survival_regen", 4f);
                stats.Heal(1);
            }

            bool godOfWar = manager.GetLevel("survival_god_of_war") > 0;
            bool ascended = manager.GetLevel("chaos_divine_ascension") > 0;
            if ((godOfWar || ascended) && Time.time >= nextGodOfWarHeal)
            {
                int nearby = manager.NearbyEnemyCount(4f);
                if (nearby > 0)
                {
                    nextGodOfWarHeal = Time.time + (ascended ? .85f : 2.5f);
                    stats.Heal(1 + nearby / 4);
                }
            }

            if (manager.GetLevel("chaos_death_aura") > 0 && Time.time >= nextDeathAura)
            {
                nextDeathAura = Time.time + NodeCooldown("chaos_death_aura", 1.5f);
                AreaDamage(transform.position, 1, NodeRadius("chaos_death_aura", 1.25f),
                    "chaos_death_aura", null, .15f);
            }

            if (manager.GetLevel("magic_thunderstorm") > 0 && Time.time >= nextThunderstorm)
            {
                nextThunderstorm = Time.time + (manager.GetLevel("magic_thunder_god") > 0 ? NodeCooldown("magic_thunder_god", 2.3f) : NodeCooldown("magic_thunderstorm", 4f));
                EnemyAIBase nearest = FindNearestEnemy(transform.position, manager.GetLevel("magic_thunder_god") > 0 ? NodeRange("magic_thunder_god", 4f) : NodeRange("magic_thunderstorm", 3f), null);
                if (nearest != null) ProcLightning(nearest, 2, "magic_thunderstorm");
            }

            if (ascended && Time.time >= nextAscensionAura)
            {
                // The ascension aura is broader and more forceful than the early chaos aura.
                nextAscensionAura = Time.time + NodeCooldown("chaos_divine_ascension", .85f);
                AreaDamage(transform.position, 2, NodeRadius("chaos_divine_ascension", 1.7f), "chaos_divine_ascension", null, .35f);
            }
        }

        private void OnHit(SkillEventContext context)
        {
            if (stats == null) stats = GetComponent<PlayerSurvivalStats>();
            if (context.Target == null) return;
            if (stats != null && manager.LifeSteal > 0f)
            {
                int before = stats.CurrentHealth;
                stats.Heal(Mathf.Max(1, Mathf.RoundToInt(context.Amount * manager.LifeSteal)));
                if (stats.CurrentHealth > before && manager.TryStartSkillCooldown("survival.lifesteal.fx", .65f))
                {
                    if (manager.GetLevel("survival_lifesteal") > 0) manager.ProcSkill("survival_lifesteal", context.Position, context.Target);
                    else if (manager.GetLevel("magic_void") > 0) manager.ProcSkill("magic_void", context.Position, context.Target);
                }
            }

            // Damage caused by a skill can heal the player, but it does not start another
            // full set of attack procs. Its kill event still feeds corpse reactions below.
            if (!string.IsNullOrEmpty(context.SkillId)) return;

            EnemyAIBase enemy = context.Target.GetComponent<EnemyAIBase>();
            if (enemy == null) return;
            int stateIndex = GetStatusIndex(enemy);
            if (stateIndex < 0) return;
            EnemyStatus state = enemyStatuses[stateIndex];

            if (state.ArmorBrokenUntil > Time.time && enemy.IsAlive)
                ApplySkillDamage(enemy, Mathf.Max(1, Mathf.CeilToInt(context.Amount * .25f)), "force_armor_break");
            if (state.MarkedUntil > Time.time && enemy.IsAlive)
                ApplySkillDamage(enemy, Mathf.Max(1, Mathf.CeilToInt(context.Amount * .20f)), "chaos_death_mark");

            if (manager.GetLevel("force_brutal_combo") > 0)
            {
                if (Time.time > comboResetAt) comboHits = 0;
                comboHits = Mathf.Min(8, comboHits + 1);
                comboResetAt = Time.time + NodeDuration("force_brutal_combo", 3f);
                if (comboHits > 1 && enemy.IsAlive)
                {
                    int bonus = Mathf.Max(1, Mathf.RoundToInt(context.Amount * Mathf.Min(.72f, NodeValue("force_brutal_combo", .12f) * (comboHits - 1))));
                    ApplySkillDamage(enemy, bonus, "force_brutal_combo");
                    if (comboHits == 5) manager.ProcSkill("force_brutal_combo", enemy.transform.position, enemy.gameObject, comboHits);
                }
            }

            if (manager.GetLevel("force_bleeding_edge") > 0)
            {
                bool newlyBleeding = state.BleedUntil <= Time.time;
                state.BleedUntil = Time.time + NodeDuration("force_bleeding_edge", 2.5f);
                state.NextBleedAt = Mathf.Min(state.NextBleedAt <= 0f ? Time.time + 1f : state.NextBleedAt, Time.time + 1f);
                if (newlyBleeding) manager.ProcSkill("force_bleeding_edge", context.Position, enemy.gameObject);
            }
            if ((context.Heavy || context.Charged) && manager.GetLevel("force_armor_break") > 0)
            {
                bool newlyBroken = state.ArmorBrokenUntil <= Time.time;
                state.ArmorBrokenUntil = Time.time + NodeDuration("force_armor_break", 3f);
                if (newlyBroken) manager.ProcSkill("force_armor_break", context.Position, enemy.gameObject);
            }

            if (manager.GetLevel("magic_fire") > 0 && context.Charged)
            {
                bool newlyBurning = state.BurnUntil <= Time.time;
                state.BurnUntil = Time.time + NodeDuration("magic_fire", 3f);
                state.NextBurnAt = Time.time + 1f;
                if (newlyBurning) manager.ProcSkill("magic_fire", context.Position, enemy.gameObject);
            }
            if (manager.GetLevel("magic_ice") > 0)
            {
                bool newlySlowed = state.FrozenUntil <= Time.time;
                float duration = NodeDuration("magic_ice", 1.5f);
                state.FrozenUntil = Mathf.Max(state.FrozenUntil, Time.time + duration);
                enemy.ApplySkillSlow(duration, .55f);
                if (newlySlowed) manager.ProcSkill("magic_ice", context.Position, enemy.gameObject);
            }
            if (manager.GetLevel("chaos_death_mark") > 0)
            {
                bool newlyMarked = state.MarkedUntil <= Time.time;
                state.MarkedUntil = Time.time + NodeDuration("chaos_death_mark", 5f);
                if (newlyMarked) manager.ProcSkill("chaos_death_mark", context.Position, enemy.gameObject);
            }
            enemyStatuses[stateIndex] = state;

            if (context.Charged && manager.GetLevel("force_heavy_hit") > 0 && manager.TryStartSkillCooldown("force.heavy-hit.fx", .15f))
                manager.ProcSkill("force_heavy_hit", context.Position, enemy.gameObject, context.Amount);
            if (manager.GetLevel("magic_spark") > 0 && (context.Critical || context.Charged) && manager.TryStartSkillCooldown("magic.spark.fx", .65f))
                manager.ProcSkill("magic_spark", context.Position, enemy.gameObject, context.Amount);

            if (manager.GetLevel("chaos_domination") > 0 && enemy.CanBeExecuted)
            {
                enemy.CalmDown();
                enemy.ApplySkillStun(.7f);
                manager.ProcSkill("chaos_domination", enemy.transform.position, enemy.gameObject);
            }

            if (context.Charged && manager.GetLevel("force_shockwave") > 0)
                AreaDamage(context.Position, Mathf.Max(1, Mathf.RoundToInt(context.Amount * NodeValue("force_shockwave", .45f))), NodeRadius("force_shockwave", 1.4f),
                    "force_shockwave", enemy.gameObject, .65f);
            if (context.Charged && manager.GetLevel("force_earthquake") > 0 && manager.TryStartSkillCooldown("force.earthquake", NodeCooldown("force_earthquake", 1.2f)))
                AreaDamage(context.Position, Mathf.Max(2, Mathf.RoundToInt(context.Amount * NodeValue("force_earthquake", .8f))), NodeRadius("force_earthquake", 2.6f),
                    "force_earthquake", null, 1.5f);
            if (context.Charged && manager.GetLevel("force_titan_wrath") > 0)
            {
                AreaDamage(context.Position, Mathf.Max(1, Mathf.RoundToInt(context.Amount * NodeValue("force_titan_wrath", .65f))), NodeRadius("force_titan_wrath", 2.2f),
                    "force_titan_wrath", enemy.gameObject, 1.15f);
                ScheduleBurst(context.Position, .22f, 1.25f, Mathf.Max(1, context.Amount / 2),
                    "force_titan_wrath", .45f, false);
            }
            if (manager.GetLevel("force_execution") > 0 && enemy.IsAlive &&
                enemy.CurrentHealth <= Mathf.CeilToInt(enemy.MaximumHealth * NodeSecondary("force_execution", .20f)))
            {
                manager.ProcSkill("force_execution", context.Position, enemy.gameObject);
                ApplySkillDamage(enemy, Mathf.Max(2, Mathf.CeilToInt(enemy.MaximumHealth * NodeValue("force_execution", 1.5f))), "force_execution");
            }

            if (manager.GetLevel("magic_lightning") > 0) ProcLightning(enemy, context.Amount, "magic_lightning");
            if (context.Charged && manager.GetLevel("magic_meteor") > 0 && manager.TryStartSkillCooldown("magic.meteor", NodeCooldown("magic_meteor", 5f)))
            {
                AreaDamage(context.Position, Mathf.Max(2, Mathf.RoundToInt(context.Amount * NodeValue("magic_meteor", 1.5f))), NodeRadius("magic_meteor", 2.1f),
                    "magic_meteor", enemy.gameObject, .45f);
            }
            if (context.Charged && manager.GetLevel("magic_frost_nova") > 0)
                FreezeArea(context.Position, NodeRadius("magic_frost_nova", 2f), "magic_frost_nova", NodeDuration("magic_frost_nova", 1.5f));
            if (state.FrozenUntil > Time.time && manager.GetLevel("magic_ice_shatter") > 0 && enemy.IsAlive)
                AreaDamage(enemy.transform.position, Mathf.Max(1, Mathf.RoundToInt(context.Amount * NodeValue("magic_ice_shatter", .7f))), NodeRadius("magic_ice_shatter", 1.4f),
                    "magic_ice_shatter", enemy.gameObject, .3f);

            if (context.Charged && manager.GetLevel("magic_void_gravity") > 0)
                PullEnemies(context.Position, NodeRadius("magic_void_gravity", 2.1f), .4f, "magic_void_gravity");
            if (context.Charged && manager.GetLevel("magic_black_hole") > 0)
            {
                PullEnemies(context.Position, NodeRadius("magic_black_hole", 2.7f), .8f, "magic_black_hole");
                if (manager.GetLevel("magic_singularity") > 0)
                    ScheduleBurst(context.Position, NodeCooldown("magic_singularity", .4f), NodeRadius("magic_singularity", 3.2f), Mathf.Max(3, Mathf.RoundToInt(context.Amount * NodeValue("magic_singularity", 1.25f))),
                        "magic_singularity", 1.1f, false);
                else
                    AreaDamage(context.Position, Mathf.Max(1, context.Amount / 2), 2.7f, "magic_black_hole", enemy.gameObject, .5f);
            }

            if (manager.GetLevel("mobility_omnipresence") > 0) TryOmnipresence(enemy, context.Amount);
        }

        private void OnDash(SkillEventContext context)
        {
            Vector2 direction = context.Direction.sqrMagnitude > .001f ? context.Direction.normalized : Vector2.down;
            Vector2 start = context.Position;
            Vector2 end = start + direction * 2.1f * manager.DashSpeedMultiplier;

            if (manager.HasSpectralDash) BeginEnemyPhase(.24f);
            if (manager.GetLevel("mobility_ghost_step") > 0 && stats != null) stats.GrantInvulnerability(.16f);

            if (manager.HasOffensiveTeleport && manager.TryStartSkillCooldown("mobility.offensive-teleport", 1.1f))
            {
                EnemyAIBase nearest = FindNearestEnemy(start, 4f, null);
                if (nearest != null)
                {
                    end = TeleportBeside(nearest, start);
                    start = context.Position;
                    AreaDamage(nearest.transform.position, 1, 1f, "mobility_offensive_teleport", null, .3f);
                }
            }

            if (manager.GetLevel("mobility_offensive_dash") > 0)
                DamageDashPath(start, end, 1, "mobility_offensive_dash");
            if (manager.GetLevel("mobility_shadow_burst") > 0)
            {
                Vector2 shadow = context.Position - direction * .45f;
                ScheduleBurst(shadow, .28f, 1.3f, 1, "mobility_shadow_burst", .35f, false);
            }
        }

        private void OnEnemyKilled(SkillEventContext context)
        {
            if (stats == null) stats = GetComponent<PlayerSurvivalStats>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (stats != null)
            {
                if (manager.GetLevel("survival_devourer") > 0) stats.Heal(2);
                if (manager.GetLevel("survival_god_of_war") > 0) stats.Heal(1);
            }
            if (manager.GetLevel("force_carnage") > 0)
            {
                GetComponent<PlayerMovementController>()?.RefundDashCooldown(manager.CarnageDashCooldownRefund);
                GetComponent<PlayerCombatController>()?.RefundAttackCooldown(manager.CarnageAttackCooldownRefund);
                manager.ProcSkill("force_carnage", context.Position, context.Target, 1);
            }

            EnemyAIBase enemy = context.Target != null ? context.Target.GetComponent<EnemyAIBase>() : null;
            EnemyStatus state = default;
            if (enemy != null)
            {
                int index = FindStatusIndex(enemy);
                if (index >= 0) state = enemyStatuses[index];
            }

            bool chainKill = context.SkillId == "chaos_corpse_explosion" || context.SkillId == "chaos_chain_reaction";
            bool canChain = !chainKill || manager.GetLevel("chaos_chain_reaction") > 0 && context.ChainDepth < SkillCombatEventBus.MaxChainDepth - 1;
            if (manager.GetLevel("chaos_corpse_explosion") > 0 && canChain)
            {
                string skillId = chainKill ? "chaos_chain_reaction" : "chaos_corpse_explosion";
                float radius = manager.GetLevel("chaos_divine_ascension") > 0 ? 2.1f : NodeRadius("chaos_corpse_explosion", 1.5f);
                int damage = Mathf.Max(1, Mathf.RoundToInt(context.Amount * (chainKill ? NodeValue("chaos_chain_reaction", .35f) : NodeValue("chaos_corpse_explosion", .55f))));
                Explode(context.Position, damage, radius, skillId, context.Target, .5f);
            }

            if (state.BurnUntil > Time.time && manager.GetLevel("magic_fire_explosion") > 0)
            {
                bool cataclysm = manager.GetLevel("magic_cataclysm") > 0;
                Explode(context.Position, Mathf.Max(2, Mathf.RoundToInt(context.Amount * (cataclysm ? NodeValue("magic_cataclysm", 1.1f) : NodeValue("magic_fire_explosion", .5f)))),
                    cataclysm ? NodeRadius("magic_cataclysm", 2.5f) : NodeRadius("magic_fire_explosion", 1.5f), cataclysm ? "magic_cataclysm" : "magic_fire_explosion", context.Target, .6f);
            }
            if (state.ShockedUntil > Time.time && manager.GetLevel("magic_overload") > 0)
                Explode(context.Position, Mathf.Max(1, Mathf.RoundToInt(context.Amount * NodeValue("magic_overload", .65f))), NodeRadius("magic_overload", 1.7f),
                    "magic_overload", context.Target, .55f);
            if (state.FrozenUntil > Time.time && manager.GetLevel("magic_glacier") > 0)
            {
                Explode(context.Position, Mathf.Max(2, Mathf.RoundToInt(context.Amount * NodeValue("magic_glacier", .9f))), NodeRadius("magic_glacier", 2.2f),
                    "magic_glacier", context.Target, .4f);
                FreezeArea(context.Position, NodeRadius("magic_glacier", 2.2f), "magic_glacier", NodeDuration("magic_glacier", 1.4f));
            }
            if (state.MarkedUntil > Time.time && manager.GetLevel("chaos_death_mark") > 0)
                Explode(context.Position, Mathf.Max(2, Mathf.RoundToInt(context.Amount * NodeValue("chaos_death_mark", 1.2f))), NodeRadius("chaos_death_mark", 2f),
                    "chaos_death_mark", context.Target, .65f);
        }

        private void OnEnemyExploded(SkillEventContext context)
        {
            // Kept as a separate hook for future skills; current reactions are queued from
            // OnEnemyKilled and inherit the same depth and event budget.
        }

        private void OnDamageTaken(SkillEventContext context)
        {
            if (manager.GetLevel("survival_regen") > 0) nextRegen = Mathf.Min(nextRegen, Time.time + 1f);
        }

        private void OnSkillUsed(SkillEventContext context)
        {
            if (context.SkillId == "survival_immortal")
                Explode(context.Position, 3, 2.2f, "survival_immortal", null, .8f, false);
            else if (context.SkillId == "survival_second_chance")
                AreaDamage(context.Position, 1, 1.4f, "survival_second_chance", null, .3f, notify: false);
        }

        private void ProcLightning(EnemyAIBase first, int sourceDamage, string skillId)
        {
            if (first == null) return;
            int maximum = Mathf.Max(1, Mathf.RoundToInt(NodeSecondary("magic_lightning", 3f)));
            if (manager.GetLevel("magic_thunderstorm") > 0) maximum++;
            if (manager.GetLevel("magic_overload") > 0) maximum++;
            if (manager.GetLevel("magic_thunder_god") > 0) maximum = Mathf.Max(maximum, Mathf.RoundToInt(NodeSecondary("magic_thunder_god", 5f)));
            maximum = Mathf.Min(7, maximum);
            int damage = Mathf.Max(1, Mathf.RoundToInt(sourceDamage * (manager.GetLevel("magic_thunder_god") > 0 ? NodeValue("magic_thunder_god", .7f) : NodeValue("magic_lightning", .35f))));
            EnemyAIBase current = first;
            int visitedCount = 0;
            for (int jump = 0; jump < maximum && current != null; jump++)
            {
                bool seen = false;
                for (int i = 0; i < visitedCount; i++) if (lightningTargets[i] == current) { seen = true; break; }
                if (seen) break;
                lightningTargets[visitedCount++] = current;
                int stateIndex = GetStatusIndex(current);
                if (stateIndex >= 0)
                {
                    EnemyStatus state = enemyStatuses[stateIndex];
                    state.ShockedUntil = Time.time + NodeDuration("magic_overload", 4f);
                    enemyStatuses[stateIndex] = state;
                }
                if ((jump > 0 || skillId == "magic_thunderstorm") && current.IsAlive) ApplySkillDamage(current, damage, skillId);
                EnemyAIBase next = FindNearestEnemy(current.transform.position, NodeRange("magic_lightning", 2.6f), lightningTargets, visitedCount);
                current = next;
            }
            manager.ProcSkill(skillId, first.transform.position, first.gameObject, maximum);
            for (int i = 0; i < visitedCount; i++) lightningTargets[i] = null;
        }

        private void Explode(Vector2 position, int damage, float radius, string skillId, GameObject ignored,
            float knockback, bool notifySkill = true)
        {
            int targets = AreaDamage(position, damage, radius, skillId, ignored, knockback, notify: notifySkill);
            if (targets > 0) events.RaiseEnemyExploded(new SkillEventContext(gameObject, ignored, position, damage, skillId: skillId));
        }

        private int AreaDamage(Vector2 position, int damage, float radius, string skillId,
            GameObject ignored, float knockback, bool freeze = false, bool notify = true)
        {
            if (damage <= 0 || radius <= 0f || !events.TryEnterChain()) return 0;
            int targetCount = 0;
            try
            {
                using (events.BeginBatch())
                {
                    targetCount = CollectAreaTargets(position, radius, ignored);
                    if (notify && targetCount > 0) manager.ProcSkill(skillId, position, ignored, damage);
                    for (int i = 0; i < targetCount; i++)
                    {
                        IDamageable target = areaTargets[i];
                        if (!(target is EnemyAIBase enemy) || !enemy.IsAlive) continue;
                        if (knockback > 0f)
                            enemy.ApplySkillKnockback((Vector2)enemy.transform.position - position, knockback, .22f);
                        if (freeze) ApplyFreeze(enemy, .9f);
                        ApplySkillDamage(enemy, damage, skillId);
                    }
                }
            }
            finally { events.ExitChain(); }
            return targetCount;
        }

        private void FreezeArea(Vector2 position, float radius, string skillId, float duration)
        {
            if (!events.TryEnterChain()) return;
            try
            {
                using (events.BeginBatch())
                {
                    int count = CollectAreaTargets(position, radius, null);
                    if (count > 0) manager.ProcSkill(skillId, position);
                    for (int i = 0; i < count; i++)
                    {
                        if (!(areaTargets[i] is EnemyAIBase enemy) || !enemy.IsAlive) continue;
                        ApplyFreeze(enemy, duration);
                    }
                }
            }
            finally { events.ExitChain(); }
        }

        private void PullEnemies(Vector2 position, float radius, float distance, string skillId)
        {
            if (!events.TryEnterChain()) return;
            try
            {
                using (events.BeginBatch())
                {
                    int count = CollectAreaTargets(position, radius, null);
                    if (count > 0) manager.ProcSkill(skillId, position);
                    for (int i = 0; i < count; i++)
                    {
                        if (!(areaTargets[i] is EnemyAIBase enemy) || !enemy.IsAlive) continue;
                        enemy.ApplySkillKnockback(position - (Vector2)enemy.transform.position, distance, .3f);
                    }
                }
            }
            finally { events.ExitChain(); }
        }

        private void DamageDashPath(Vector2 start, Vector2 end, int damage, string skillId)
        {
            if (!events.TryEnterChain()) return;
            try
            {
                using (events.BeginBatch())
                {
                    int targetCount = 0;
                    Vector2 delta = end - start;
                    int samples = Mathf.Clamp(Mathf.CeilToInt(delta.magnitude / .45f), 1, 8);
                    for (int sample = 0; sample <= samples; sample++)
                    {
                        Vector2 point = start + delta * (sample / (float)samples);
                        int count = Physics2D.OverlapCircle(point, .6f, SkillTargetFilter, areaHits);
                        for (int i = 0; i < count; i++)
                        {
                            IDamageable target = areaHits[i] != null ? areaHits[i].GetComponentInParent<IDamageable>() : null;
                            if (!(target is EnemyAIBase enemy) || !enemy.IsAlive || !DamageRules.CanPlayerHit(enemy)) continue;
                            bool duplicate = false;
                            for (int j = 0; j < targetCount; j++) if (ReferenceEquals(areaTargets[j], enemy)) { duplicate = true; break; }
                            if (!duplicate && targetCount < areaTargets.Length) areaTargets[targetCount++] = enemy;
                        }
                    }
                    if (targetCount > 0) manager.ProcSkill(skillId, start, amount: damage);
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (areaTargets[i] is EnemyAIBase enemy && enemy.IsAlive)
                        {
                            enemy.ApplySkillKnockback((Vector2)enemy.transform.position - start, .25f, .16f);
                            ApplySkillDamage(enemy, damage, skillId);
                        }
                        areaTargets[i] = null;
                    }
                }
            }
            finally { events.ExitChain(); }
        }

        private int CollectAreaTargets(Vector2 position, float radius, GameObject ignored)
        {
            int count = Physics2D.OverlapCircle(position, radius, SkillTargetFilter, areaHits);
            int targetCount = 0;
            for (int i = 0; i < count; i++)
            {
                IDamageable target = areaHits[i] != null ? areaHits[i].GetComponentInParent<IDamageable>() : null;
                if (!(target is EnemyAIBase enemy) || !enemy.IsAlive || !DamageRules.CanPlayerHit(enemy) ||
                    ignored != null && enemy.gameObject == ignored) continue;
                bool duplicate = false;
                for (int j = 0; j < targetCount; j++) if (ReferenceEquals(areaTargets[j], enemy)) { duplicate = true; break; }
                if (!duplicate && targetCount < areaTargets.Length) areaTargets[targetCount++] = enemy;
            }
            return targetCount;
        }

        private void ApplySkillDamage(IDamageable target, int damage, string skillId)
        {
            if (target == null || !target.IsAlive || damage <= 0) return;
            using (manager.BeginSkillEffect(skillId)) target.TakeDamage(damage, inventory);
        }

        private void ApplyFreeze(EnemyAIBase enemy, float duration)
        {
            int index = GetStatusIndex(enemy);
            if (index >= 0)
            {
                EnemyStatus state = enemyStatuses[index];
                state.FrozenUntil = Mathf.Max(state.FrozenUntil, Time.time + duration);
                enemyStatuses[index] = state;
            }
            enemy.ApplySkillSlow(duration, .05f);
        }

        private int GetStatusIndex(EnemyAIBase enemy)
        {
            if (enemy == null) return -1;
            int free = -1;
            for (int i = 0; i < enemyStatuses.Length; i++)
            {
                EnemyStatus state = enemyStatuses[i];
                if (state.Enemy == enemy && state.SpawnGeneration == enemy.SpawnGeneration) return i;
                if (free < 0 && (state.Enemy == null || state.Enemy.SpawnGeneration != state.SpawnGeneration)) free = i;
            }
            if (free < 0) free = Mathf.Abs(enemy.GetInstanceID()) % enemyStatuses.Length;
            enemyStatuses[free] = new EnemyStatus { Enemy = enemy, SpawnGeneration = enemy.SpawnGeneration };
            return free;
        }

        private int FindStatusIndex(EnemyAIBase enemy)
        {
            if (enemy == null) return -1;
            for (int i = 0; i < enemyStatuses.Length; i++)
                if (enemyStatuses[i].Enemy == enemy && enemyStatuses[i].SpawnGeneration == enemy.SpawnGeneration) return i;
            return -1;
        }

        private void TickAilments()
        {
            if (Time.time < nextAilmentTick) return;
            nextAilmentTick = Time.time + .25f;
            if (!events.TryEnterChain()) return;
            try
            {
                using (events.BeginBatch())
                {
                    for (int i = 0; i < enemyStatuses.Length; i++)
                    {
                        EnemyStatus state = enemyStatuses[i];
                        EnemyAIBase enemy = state.Enemy;
                        if (enemy == null || !enemy.IsAlive || enemy.SpawnGeneration != state.SpawnGeneration) continue;
                        if (state.BleedUntil > Time.time && Time.time >= state.NextBleedAt)
                        {
                            state.NextBleedAt = Time.time + 1f;
                            ApplySkillDamage(enemy, 1, "force_bleeding_edge");
                        }
                        if (enemy.IsAlive && state.BurnUntil > Time.time && Time.time >= state.NextBurnAt)
                        {
                            state.NextBurnAt = Time.time + 1f;
                            ApplySkillDamage(enemy, 1, "magic_fire");
                        }
                        enemyStatuses[i] = state;
                    }
                }
            }
            finally { events.ExitChain(); }
        }

        private void ScheduleBurst(Vector2 position, float delay, float radius, int damage, string skillId,
            float knockback, bool freeze)
        {
            if (pendingBursts.Count >= 12) pendingBursts.RemoveAt(0);
            pendingBursts.Add(new PendingBurst
            {
                Position = position,
                FireAt = Time.time + Mathf.Max(.05f, delay),
                Radius = radius,
                Damage = damage,
                SkillId = skillId,
                Knockback = knockback,
                Freeze = freeze
            });
            manager.ProcSkill(skillId, position, amount: damage);
        }

        private void TickBursts()
        {
            for (int i = pendingBursts.Count - 1; i >= 0; i--)
            {
                PendingBurst burst = pendingBursts[i];
                if (Time.time < burst.FireAt) continue;
                pendingBursts.RemoveAt(i);
                AreaDamage(burst.Position, burst.Damage, burst.Radius, burst.SkillId, null,
                    burst.Knockback, burst.Freeze, false);
            }
        }

        private void TryOmnipresence(EnemyAIBase first, int sourceDamage)
        {
            if (first == null || !manager.TryStartSkillCooldown("mobility.omnipresence", NodeCooldown("mobility_omnipresence", .4f)) ||
                !events.TryEnterChain()) return;
            try
            {
                using (events.BeginBatch())
                {
                    int count = 1;
                    blinkTargets[0] = first;
                    Vector2 origin = first.transform.position;
                    EnemyAIBase current = FindNearestEnemy(origin, NodeRange("mobility_omnipresence", 3.25f), blinkTargets, count);
                    while (count < blinkTargets.Length && current != null && current.IsAlive)
                    {
                        blinkTargets[count++] = current;
                        origin = TeleportBeside(current, origin);
                        current = FindNearestEnemy(current.transform.position, NodeRange("mobility_omnipresence", 3.25f), blinkTargets, count);
                    }
                    if (count > 1)
                    {
                        manager.ProcSkill("mobility_omnipresence", transform.position, amount: count - 1);
                        int damage = Mathf.Max(1, Mathf.RoundToInt(sourceDamage * .35f));
                        for (int i = 1; i < count; i++)
                            if (blinkTargets[i] != null && blinkTargets[i].IsAlive)
                                ApplySkillDamage(blinkTargets[i], damage, "mobility_omnipresence");
                    }
                    for (int i = 0; i < count; i++) blinkTargets[i] = null;
                }
            }
            finally { events.ExitChain(); }
        }

        private Vector2 TeleportBeside(EnemyAIBase enemy, Vector2 from)
        {
            Vector2 towardPlayer = (Vector2)transform.position - (Vector2)enemy.transform.position;
            if (towardPlayer.sqrMagnitude < .001f) towardPlayer = from - (Vector2)enemy.transform.position;
            if (towardPlayer.sqrMagnitude < .001f) towardPlayer = Vector2.left;
            Vector2 radial = towardPlayer.normalized;
            Vector2 destination = (Vector2)enemy.transform.position + radial * .7f;
            bool clear = false;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                if (IsTeleportDestinationClear(destination, enemy)) { clear = true; break; }
                radial = new Vector2(-radial.y, radial.x);
                destination = (Vector2)enemy.transform.position + radial * .82f;
            }
            if (!clear) return (Vector2)transform.position;
            transform.position = new Vector3(destination.x, destination.y, transform.position.z);
            if (body != null) body.position = destination;
            Physics2D.SyncTransforms();
            return destination;
        }

        private bool IsTeleportDestinationClear(Vector2 destination, EnemyAIBase target)
        {
            int count = Physics2D.OverlapCircle(destination, .22f, TeleportBlockFilter, teleportHits);
            if (count == teleportHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Collider2D collider = teleportHits[i];
                if (collider == null || collider.transform.IsChildOf(transform) || collider.transform.IsChildOf(target.transform)) continue;
                if (collider.GetComponentInParent<EnemyAIBase>() != null) continue;
                return false;
            }
            return true;
        }

        private EnemyAIBase FindNearestEnemy(Vector2 position, float radius, GameObject ignored)
        {
            EnemyAIBase selected = null;
            float closest = radius * radius;
            IReadOnlyList<EnemyAIBase> enemies = EnemyAIBase.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyAIBase candidate = enemies[i];
                if (candidate == null || !candidate.IsAlive || ignored != null && candidate.gameObject == ignored) continue;
                float distance = ((Vector2)candidate.transform.position - position).sqrMagnitude;
                if (distance <= closest) { closest = distance; selected = candidate; }
            }
            return selected;
        }

        private EnemyAIBase FindNearestEnemy(Vector2 position, float radius, EnemyAIBase[] ignored, int ignoredCount)
        {
            EnemyAIBase selected = null;
            float closest = radius * radius;
            IReadOnlyList<EnemyAIBase> enemies = EnemyAIBase.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyAIBase candidate = enemies[i];
                if (candidate == null || !candidate.IsAlive) continue;
                bool skip = false;
                for (int j = 0; j < ignoredCount; j++) if (ignored[j] == candidate) { skip = true; break; }
                if (skip) continue;
                float distance = ((Vector2)candidate.transform.position - position).sqrMagnitude;
                if (distance <= closest) { closest = distance; selected = candidate; }
            }
            return selected;
        }

        private void BeginEnemyPhase(float duration)
        {
            phaseUntil = Mathf.Max(phaseUntil, Time.time + duration);
            IgnoreCurrentEnemyCollisions();
            if (phaseRoutine == null) phaseRoutine = StartCoroutine(RestorePhaseWhenReady());
        }

        private IEnumerator RestorePhaseWhenReady()
        {
            while (Time.time < phaseUntil)
            {
                yield return null;
            }
            RestoreEnemyCollision();
            phaseRoutine = null;
        }

        private void IgnoreCurrentEnemyCollisions()
        {
            if (playerColliders == null) return;
            IReadOnlyList<EnemyAIBase> enemies = EnemyAIBase.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyAIBase enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive) continue;
                enemyColliders.Clear();
                enemy.GetComponentsInChildren(true, enemyColliders);
                for (int p = 0; p < playerColliders.Length; p++)
                {
                    Collider2D playerCollider = playerColliders[p];
                    if (playerCollider == null || !playerCollider.enabled) continue;
                    for (int e = 0; e < enemyColliders.Count; e++)
                    {
                        Collider2D enemyCollider = enemyColliders[e];
                        if (enemyCollider == null || !enemyCollider.enabled || IsTracked(playerCollider, enemyCollider) ||
                            Physics2D.GetIgnoreCollision(playerCollider, enemyCollider)) continue;
                        Physics2D.IgnoreCollision(playerCollider, enemyCollider, true);
                        phasedCollisionPairs.Add(new CollisionPair { Player = playerCollider, Enemy = enemyCollider });
                    }
                }
            }
        }

        private bool IsTracked(Collider2D playerCollider, Collider2D enemyCollider)
        {
            for (int i = 0; i < phasedCollisionPairs.Count; i++)
                if (phasedCollisionPairs[i].Player == playerCollider && phasedCollisionPairs[i].Enemy == enemyCollider) return true;
            return false;
        }

        private void RestoreEnemyCollision()
        {
            for (int i = 0; i < phasedCollisionPairs.Count; i++)
            {
                CollisionPair pair = phasedCollisionPairs[i];
                if (pair.Player != null && pair.Enemy != null) Physics2D.IgnoreCollision(pair.Player, pair.Enemy, false);
            }
            phasedCollisionPairs.Clear();
        }

        private static SkillDefinition Node(string id) => SkillTreeCatalog.Find(id);

        private static float NodeValue(string id, float fallback)
        {
            SkillDefinition definition = Node(id);
            return definition != null && definition.Value != 0f ? definition.Value : fallback;
        }

        private static float NodeSecondary(string id, float fallback)
        {
            SkillDefinition definition = Node(id);
            return definition != null && definition.SecondaryValue != 0f ? definition.SecondaryValue : fallback;
        }

        private static float NodeCooldown(string id, float fallback)
        {
            SkillDefinition definition = Node(id);
            return definition != null && definition.Cooldown > 0f ? definition.Cooldown : fallback;
        }

        private static float NodeRange(string id, float fallback)
        {
            SkillDefinition definition = Node(id);
            return definition != null && definition.Range > 0f ? definition.Range : fallback;
        }

        private static float NodeRadius(string id, float fallback)
        {
            SkillDefinition definition = Node(id);
            return definition != null && definition.Radius > 0f ? definition.Radius : fallback;
        }

        private static float NodeDuration(string id, float fallback)
        {
            SkillDefinition definition = Node(id);
            return definition != null && definition.Duration > 0f ? definition.Duration : fallback;
        }
    }
}
