using System.Collections.Generic;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    public abstract class EnemyAIBase : MonoBehaviour, IDamageable
    {
        private static readonly List<EnemyAIBase> activeEnemies = new List<EnemyAIBase>(96);
        private int registryIndex = -1;
        /// <summary>Enabled enemies, including the brief death animation. Check IsAlive before targeting.
        /// Iterate backwards if returning enemies to their pools while visiting this list.</summary>
        public static IReadOnlyList<EnemyAIBase> ActiveEnemies => activeEnemies;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => activeEnemies.Clear();

        private void OnEnable()
        {
            if (registryIndex >= 0 && registryIndex < activeEnemies.Count && activeEnemies[registryIndex] == this) return;
            registryIndex = activeEnemies.Count;
            activeEnemies.Add(this);
        }

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
        private float strikeAt = -1;
        private float recycleAt = -1;
        private Vector2 attackDirection;
        private EnemySpriteAnimator spriteAnimation;
        private EnemyProjectilePool projectiles;
        private Collider2D[] combatColliders;
        private bool[] colliderStates;
        private PlayerSurvivalStats targetStats;
        public EnemyCombatStyle CombatStyle { get; private set; }
        public bool IsDying => recycleAt >= 0;
        public EnemySpriteAnimator SpriteAnimation => spriteAnimation;
        public virtual bool CanLaunchProjectile => IsAlive && target != null && target.gameObject.activeInHierarchy && targetStats != null && targetStats.CurrentHealth > 0;
        protected virtual HomeSafeZone ProjectileProtection => null;
        [SerializeField] private EnemyLootPickup lootPrefab;
        [SerializeField, Min(0f)] private float knockbackDistance = 0.65f;
        [SerializeField, Min(0.01f)] private float knockbackDuration = 0.18f;
        private Vector2 knockbackVelocity;
        private float knockbackRemaining;
        private float activeKnockbackDuration;
        private float skillStunnedUntil;
        private float skillSlowedUntil;
        private float skillSlowMultiplier = 1f;
        private float skillFrozenUntil;
        public virtual bool IsElite => false;
        protected virtual float KnockbackResistance => 1f;
        private readonly RaycastHit2D[] knockbackHits = new RaycastHit2D[24];
        public void ConfigureLoot(EnemyLootPickup prefab) => lootPrefab = prefab;
        public EnemyLootPickup LootPrefab => lootPrefab;
        public bool IsPreparingAttack => strikeAt >= 0;
        public float AttackWindup => CombatStyle == EnemyCombatStyle.ArcherGoblin ? .7f : enemyName == "Golem" ? .85f : .55f;
        public int CurrentHealth => Mathf.Max(0, currentHealth);
        public int MaximumHealth => maxHealth;
        public bool IsSkillFrozen => Time.time < skillFrozenUntil;
        public virtual bool CanBeExecuted => IsAlive && !IsElite && CurrentHealth <= Mathf.Max(1, Mathf.CeilToInt(maxHealth * .25f));
        public bool IsProvoked { get; private set; }
        public void CalmDown() => IsProvoked = false;
        public void EnsureMinimumHealth(int health) => maxHealth = Mathf.Max(maxHealth, health);

        /// <summary>Applies a temporary movement slow or freeze from a player skill.</summary>
        public void ApplySkillSlow(float duration, float movementMultiplier)
        {
            if (!IsAlive || duration <= 0f) return;
            float until = Time.time + duration;
            skillSlowedUntil = Mathf.Max(skillSlowedUntil, until);
            skillSlowMultiplier = Mathf.Min(skillSlowMultiplier, Mathf.Clamp(movementMultiplier, 0f, 1f));
            if (movementMultiplier <= .15f)
            {
                skillFrozenUntil = Mathf.Max(skillFrozenUntil, until);
                ApplySkillStun(duration);
            }
        }

        /// <summary>Interrupts an enemy's movement and wind-up for the requested duration.</summary>
        public void ApplySkillStun(float duration)
        {
            if (!IsAlive || duration <= 0f) return;
            skillStunnedUntil = Mathf.Max(skillStunnedUntil, Time.time + duration);
            CancelAttack();
        }

        /// <summary>Applies a skill-directed impulse without requiring a player hit wrapper.</summary>
        public void ApplySkillKnockback(Vector2 direction, float distance, float duration)
        {
            if (!IsAlive || distance <= 0f || duration <= 0f) return;
            if (direction.sqrMagnitude < .0001f) direction = Vector2.up;
            activeKnockbackDuration = Mathf.Max(.04f, duration);
            knockbackVelocity = direction.normalized * (distance / activeKnockbackDuration);
            knockbackRemaining = activeKnockbackDuration;
            nextAttackTime = Mathf.Max(nextAttackTime, Time.time + activeKnockbackDuration);
            CancelAttack();
        }

        protected Transform Target => target;
        protected virtual float MovementSpeedMultiplier => 1f;
        protected virtual float AttackIntervalMultiplier => 1f;
        protected string EnemyName => enemyName;
        protected DungeonEnemyPool Pool => pool;
        public Transform Transform => transform;
        public bool IsAlive => isActiveAndEnabled && currentHealth > 0;
        public int SpawnGeneration { get; private set; }

        public virtual void Configure(Transform playerTarget, DungeonEnemyPool ownerPool)
        {
            target = playerTarget;
            targetStats = target != null ? target.GetComponent<PlayerSurvivalStats>() : null;
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

        public void ConfigureCombat(EnemyCombatStyle style, PlayerAnimationLibrary library, EnemyProjectilePool projectilePool)
        {
            if (style == EnemyCombatStyle.Legacy || library == null) return;
            projectiles = projectilePool;
            if (CombatStyle == style && spriteAnimation != null) return;
            CombatStyle = style;
            bool ranged = style == EnemyCombatStyle.ArcherGoblin;
            bool heavy = style == EnemyCombatStyle.Orc || style == EnemyCombatStyle.BloodMonster;
            ConfigureStats(EnemyRoster.DisplayName(style), heavy ? 16 : ranged ? 10 : 12, heavy ? 2 : 1,
                heavy ? 1.45f : ranged ? 1.7f : 2.1f, ranged ? 4.8f : style == EnemyCombatStyle.SpearGoblin ? 1.3f : 1.15f,
                heavy ? 1.7f : ranged ? 1.85f : 1.25f, heavy ? 6 : ranged ? 4 : 3);
            foreach (var animation in GetComponentsInChildren<MovementSpriteAnimation>(true)) animation.enabled = false;
            if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (bodyRenderer == null)
            {
                var art = new GameObject("Enemy Visual");
                art.transform.SetParent(transform, false);
                bodyRenderer = art.AddComponent<SpriteRenderer>();
            }
            if (bodyRenderer.transform != transform)
            {
                bodyRenderer.transform.localPosition = Vector3.zero;
                bodyRenderer.transform.localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1);
            }
            bodyColor = Color.white;
            bodyRenderer.enabled = true;
            var depth = bodyRenderer.GetComponent<WorldSpriteDepth>() ?? bodyRenderer.gameObject.AddComponent<WorldSpriteDepth>();
            depth.Visual = bodyRenderer;
            ConfigureAnimation(library);
            name = enemyName;
        }

        public void ConfigureAnimation(PlayerAnimationLibrary library)
        {
            if (library == null || bodyRenderer == null) return;
            // A pooled corpse may change archetype before ActivateFromPool restores it.
            // Preserve the authored collider state, not the temporary death disable.
            RestoreColliders();
            foreach (var animation in GetComponentsInChildren<MovementSpriteAnimation>(true)) animation.enabled = false;
            spriteAnimation = GetComponent<EnemySpriteAnimator>() ?? gameObject.AddComponent<EnemySpriteAnimator>();
            spriteAnimation.Configure(library, bodyRenderer);
            combatColliders = GetComponentsInChildren<Collider2D>(true);
            colliderStates = new bool[combatColliders.Length];
            for (int i = 0; i < combatColliders.Length; i++) colliderStates[i] = combatColliders[i].enabled;
        }

        private void RestoreColliders()
        {
            if (combatColliders == null) return;
            for (int i = 0; i < combatColliders.Length; i++)
                if (combatColliders[i] != null) combatColliders[i].enabled = colliderStates[i];
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

        public virtual void ActivateFromPool(Vector3 position)
        {
            projectiles?.Cancel(this);
            recycleAt = -1;
            RestoreColliders();
            transform.position = position;
            currentHealth = maxHealth;
            IsProvoked = false;
            SpawnGeneration++;
            nextAttackTime = 0f; strikeAt=-1;
            hurtFlashEndsAt = 0f;
            knockbackRemaining = 0f;
            knockbackVelocity = Vector2.zero;
            gameObject.SetActive(true);
            spriteAnimation?.ResetState();
            ApplyVisuals();
        }

        public virtual void ReturnToPool()
        {
            RestoreHitVisual();
            projectiles?.Cancel(this);
            recycleAt = -1;
            strikeAt = -1;
            knockbackRemaining = 0;
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (registryIndex >= 0 && registryIndex < activeEnemies.Count && activeEnemies[registryIndex] == this)
            {
                int last = activeEnemies.Count - 1;
                var moved = activeEnemies[last];
                activeEnemies[registryIndex] = moved;
                moved.registryIndex = registryIndex;
                activeEnemies.RemoveAt(last);
            }
            registryIndex = -1;
            projectiles?.Cancel(this);
            RestoreHitVisual();
            CancelAttack();
        }

        protected void CancelAttack()
        {
            if (strikeAt >= 0) nextAttackTime = Time.time + .3f;
            strikeAt = -1;
            spriteAnimation?.CancelAttack();
        }

        private void RestoreHitVisual()
        {
            GetComponent<VisibleHitFeedback>()?.ResetFlash();
        }

        public virtual void TakeDamage(int amount)
        {
            TakeDamage(amount, target != null ? target.GetComponent<PlayerInventory>() : null);
        }

        public virtual void TakeDamage(int amount, PlayerInventory source)
        {
            if (!IsAlive || amount <= 0)
            {
                return;
            }

            int finalDamage = Mathf.Max(1, amount);
            if (source != null) IsProvoked = true;
            bool heavy = HitFeedback.IsHeavy(gameObject, source);
            bool charged=HitFeedback.IsCharged(gameObject,source);
            strikeAt = -1;
            activeKnockbackDuration = charged?.5f:heavy ? Mathf.Max(.36f, knockbackDuration * 1.6f) : Mathf.Max(.22f,knockbackDuration);
            nextAttackTime = Time.time + activeKnockbackDuration;
            Vector2 away = (Vector2)transform.position - (source != null ? (Vector2)source.transform.position : target != null ? (Vector2)target.position : (Vector2)transform.position - Vector2.down);
            knockbackVelocity = (away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.up) *
                knockbackDistance * (charged?2.5f:heavy ? 1.6f : .8f) * KnockbackResistance / Mathf.Max(.01f, activeKnockbackDuration);
            knockbackRemaining = activeKnockbackDuration;
            currentHealth -= finalDamage;
            hurtFlashEndsAt = Time.time + hurtFlashDuration;
            HitFeedback.Report(gameObject, source, finalDamage, currentHealth <= 0, IsElite);
            FarmGameEvents.RaiseEnemyDamaged();
            if(!Core.PortfolioSession.Active)FarmNotificationCenter.Show($"{enemyName} recibio {finalDamage} de dano.");
            if (currentHealth <= 0)
            {
                projectiles?.Cancel(this);
                if (spriteAnimation == null) ReturnToPool();
                else
                {
                    recycleAt = Time.time + spriteAnimation.DeathDuration;
                    foreach (var collider in combatColliders) if (collider != null) collider.enabled = false;
                    spriteAnimation.PlayDeath();
                }
                OnDefeated(source);
                return;
            }

            spriteAnimation?.PlayHurt(charged?.48f:heavy ? .36f : .22f);
            ApplyVisuals();
        }

        private void Update()
        {
            ApplyVisuals();
            if (IsDying)
            {
                if(knockbackRemaining>0)TickKnockback();
                if (Time.time >= recycleAt) ReturnToPool();
                return;
            }
            if (!IsAlive) return;
            if (Time.time < skillStunnedUntil)
            {
                CancelAttack();
                return;
            }
            if (Time.time >= skillSlowedUntil) skillSlowMultiplier = 1f;
            RefreshTarget();
            if (target == null || !target.gameObject.activeInHierarchy || (targetStats != null && targetStats.CurrentHealth <= 0))
            {
                CancelAttack();
                projectiles?.Cancel(this);
                return;
            }
            if (knockbackRemaining > 0f) TickKnockback();
            else if (target != null) TickEnemy();
        }

        private void TickKnockback()
        {
            float step = Mathf.Min(Time.deltaTime, knockbackRemaining);
            knockbackRemaining -= step;
            // Quadratic ease-out keeps total travel constant while softening the end of the bounce.
            float duration = Mathf.Max(0.01f, activeKnockbackDuration);
            Vector2 displacement = knockbackVelocity * step * ((2f * knockbackRemaining + step) / duration);
            float distance = displacement.magnitude;
            if (distance <= 0f) return;
            int count = Physics2D.CircleCast(transform.position, 0.28f, displacement.normalized,
                new ContactFilter2D { useTriggers = false }, knockbackHits, distance);
            float allowed = count == knockbackHits.Length ? 0f : distance;
            for (int i = 0; i < count; i++)
                if (!knockbackHits[i].transform.IsChildOf(transform)) allowed = Mathf.Min(allowed, Mathf.Max(0f, knockbackHits[i].distance - 0.02f));
            Vector2 next = (Vector2)transform.position + displacement.normalized * allowed;
            if (CanApplyKnockback(next)) transform.position = new Vector3(next.x, next.y, transform.position.z);
        }

        protected virtual bool CanApplyKnockback(Vector2 position) => true;
        protected virtual void RefreshTarget() { }

        protected virtual void TickEnemy()
        {
            if (pool != null && pool.Expedition != null && !pool.Expedition.CanEngage(this)) { CancelAttack(); return; }
            Vector2 toTarget = AttackPoint - (Vector2)transform.position;
            float distance = toTarget.magnitude;
            if (IsPreparingAttack)
            {
                if (Time.time >= strikeAt)
                {
                    strikeAt = -1;
                    nextAttackTime = Time.time + attackInterval * AttackIntervalMultiplier;
                    bool facing = toTarget.sqrMagnitude < .01f || Vector2.Dot(attackDirection, toTarget.normalized) >= -.15f;
                    // A committed arrow still launches if the player dodges out of range or behind cover.
                    // The projectile's swept collision, not a second aim check, resolves that shot.
                    if (CombatStyle == EnemyCombatStyle.ArcherGoblin)
                    {
                        if (CanLaunchProjectile) AttackTarget(targetStats);
                    }
                    else if (distance <= attackRange + .1f && facing && ClearAttackPath()) AttackTarget(targetStats);
                }
                return;
            }
            spriteAnimation?.Face(toTarget);
            if (CombatStyle == EnemyCombatStyle.ArcherGoblin && distance < 2.2f)
            {
                if (!MoveInDirection(-toTarget)) TryAttackTarget();
                return;
            }
            if (distance > attackRange || (CombatStyle == EnemyCombatStyle.ArcherGoblin && !ClearAttackPath()))
            {
                MoveTowardTarget();
                return;
            }
            TryAttackTarget();
        }

        protected virtual bool CanMoveTo(Vector2 position) => pool == null || pool.Expedition == null || pool.Expedition.CanOccupy(this, position);

        // Large structures are hit at their surface, not at an unreachable centre behind the collider.
        protected virtual Vector2 AttackPoint => target!=null?(Vector2)target.position:(Vector2)transform.position;

        protected virtual void MoveTowardTarget() => MoveInDirection(target.position - transform.position);

        protected bool MoveInDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < .0001f) return false;
            float slowMultiplier = Time.time < skillSlowedUntil ? skillSlowMultiplier : 1f;
            Vector2 displacement = direction.normalized * moveSpeed * MovementSpeedMultiplier * slowMultiplier * Time.deltaTime;
            if (TryMove(displacement)) return true;
            // Sliding along a blocked axis lets chasers and retreating archers negotiate props.
            if (Mathf.Abs(displacement.x) > .001f && TryMove(new Vector2(displacement.x, 0))) return true;
            return Mathf.Abs(displacement.y) > .001f && TryMove(new Vector2(0, displacement.y));
        }

        private bool TryMove(Vector2 displacement)
        {
            Vector2 next = (Vector2)transform.position + displacement;
            if (!CanMoveTo(next)) return false;
            int count = Physics2D.CircleCast(transform.position, .23f, displacement.normalized,
                new ContactFilter2D { useTriggers = false }, knockbackHits, displacement.magnitude + .02f);
            if (count == knockbackHits.Length) return false;
            for (int i = 0; i < count; i++)
                if (!knockbackHits[i].transform.IsChildOf(transform)) return false;
            transform.position = new Vector3(next.x, next.y, transform.position.z);
            return true;
        }

        protected virtual void TryAttackTarget()
        {
            if (Time.time < nextAttackTime || !ClearAttackPath()) return;
            attackDirection = (AttackPoint - (Vector2)transform.position).normalized;
            strikeAt = Time.time + AttackWindup;
            spriteAnimation?.PlayAttack(attackDirection, AttackWindup + .2f);
        }

        private bool ClearAttackPath()
        {
            if (target == null) return false;
            int count = Physics2D.Linecast(transform.position, AttackPoint,
                new ContactFilter2D { useTriggers = false }, knockbackHits);
            if (count == knockbackHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                if (CombatStyle == EnemyCombatStyle.ArcherGoblin && knockbackHits[i].collider.GetComponentInParent<EnemyAIBase>() != null) continue;
                if (!knockbackHits[i].transform.IsChildOf(transform) && !knockbackHits[i].transform.IsChildOf(target)) return false;
            }
            return true;
        }

        protected virtual void AttackTarget(PlayerSurvivalStats stats)
        {
            if (CombatStyle == EnemyCombatStyle.ArcherGoblin)
            {
                projectiles?.Fire(this, target, attackDirection, damage, ProjectileProtection);
                return;
            }
            stats?.TakeDamage(damage);
            FarmNotificationCenter.Show($"{enemyName} te golpeo: -{damage} vida.");
        }

        protected virtual void OnDefeated(PlayerInventory inventory)
        {
            pool?.NotifyDefeated(this);
            DropLoot();
            inventory?.GetComponent<SkillTreeManager>()?.NotifyEnemyKilled(gameObject, IsElite ? 12 : 5);
            FarmGameEvents.RaiseEnemyDefeated();
        }

        protected void DropLoot() => EnemyLootTable.Drop(transform.position, transform.parent, CombatStyle, IsElite, coinReward, lootPrefab);

        private void ApplyVisuals()
        {
            if (bodyRenderer != null)
            {
                Color warning = CombatStyle == EnemyCombatStyle.Legacy ? new Color(1f, .4f, .1f) : new Color(1f, .85f, .7f);
                bodyRenderer.color = Time.time < hurtFlashEndsAt ? Color.Lerp(Color.white, hurtColor, 0.25f) : IsPreparingAttack ? warning : bodyColor;
            }

            if (healthFillRenderer != null)
            {
                float healthPercent = maxHealth <= 0 ? 0f : Mathf.Clamp01((float)currentHealth / maxHealth);
                healthFillRenderer.transform.localScale = new Vector3(healthPercent, 1f, 1f);
                healthFillRenderer.enabled = IsAlive;
            }
        }
    }
}
