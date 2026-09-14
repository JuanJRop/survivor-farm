using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    public abstract class EnemyAIBase : MonoBehaviour, IDamageable
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
        private Material normalMaterial;
        private static Material flashMaterial;
        private static Material FlashMaterial
        {
            get
            {
                if (flashMaterial == null)
                {
                    var shader = Resources.Load<Shader>("CombatHitFlash");
                    if (shader != null) flashMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
                }
                return flashMaterial;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetFlashMaterial()
        {
            if (flashMaterial != null) Destroy(flashMaterial);
            flashMaterial = null;
        }
        private readonly RaycastHit2D[] knockbackHits = new RaycastHit2D[24];
        public void ConfigureLoot(EnemyLootPickup prefab) => lootPrefab = prefab;
        public EnemyLootPickup LootPrefab => lootPrefab;
        public bool IsPreparingAttack => strikeAt >= 0;
        public float AttackWindup => CombatStyle == EnemyCombatStyle.ArcherGoblin ? .7f : enemyName == "Golem" ? .85f : .55f;
        public int CurrentHealth => Mathf.Max(0, currentHealth);
        public int MaximumHealth => maxHealth;
        public void EnsureMinimumHealth(int health) => maxHealth = Mathf.Max(maxHealth, health);

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
            ConfigureStats(ranged ? "Goblin arquero" : "Goblin lancero", ranged ? 10 : 12, 1,
                ranged ? 1.5f : 1.8f, ranged ? 4.8f : .95f, ranged ? 2.1f : 1.5f, ranged ? 4 : 3);
            foreach (var animation in GetComponentsInChildren<MovementSpriteAnimation>(true)) animation.enabled = false;
            if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>(true);
            if (bodyRenderer == null)
            {
                var art = new GameObject("Goblin Visual");
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
            if (bodyRenderer != null && normalMaterial != null) bodyRenderer.sharedMaterial = normalMaterial;
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
            strikeAt = -1;
            nextAttackTime = Time.time + knockbackDuration;
            Vector2 away = (Vector2)transform.position - (source != null ? (Vector2)source.transform.position : target != null ? (Vector2)target.position : (Vector2)transform.position - Vector2.down);
            knockbackVelocity = (away.sqrMagnitude > 0.0001f ? away.normalized : Vector2.up) * knockbackDistance / Mathf.Max(0.01f, knockbackDuration);
            knockbackRemaining = knockbackDuration;
            currentHealth -= finalDamage;
            source?.GetComponent<GameFeelFeedback>()?.Pulse("−"+finalDamage,transform.position,true);
            hurtFlashEndsAt = Time.time + hurtFlashDuration;
            VisibleHitFeedback.Play(gameObject);
            FarmGameEvents.RaiseEnemyDamaged();
            FarmNotificationCenter.Show($"{enemyName} recibio {finalDamage} de dano.");
            if (currentHealth <= 0)
            {
                projectiles?.Cancel(this);
                knockbackRemaining = 0;
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

            spriteAnimation?.PlayHurt();
            ApplyVisuals();
        }

        private void Update()
        {
            ApplyVisuals();
            if (IsDying)
            {
                if (Time.time >= recycleAt) ReturnToPool();
                return;
            }
            if (!IsAlive) return;
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
            float duration = Mathf.Max(0.01f, knockbackDuration);
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

        protected virtual void TickEnemy()
        {
            if (pool != null && pool.Expedition != null && !pool.Expedition.CanEngage(this)) { CancelAttack(); return; }
            Vector2 toTarget = target.position - transform.position;
            float distance = toTarget.magnitude;
            if (IsPreparingAttack)
            {
                if (Time.time >= strikeAt)
                {
                    strikeAt = -1;
                    nextAttackTime = Time.time + attackInterval * AttackIntervalMultiplier;
                    bool facing = CombatStyle != EnemyCombatStyle.SpearGoblin || Vector2.Dot(attackDirection, toTarget.normalized) >= .25f;
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

        protected virtual void MoveTowardTarget() => MoveInDirection(target.position - transform.position);

        protected bool MoveInDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < .0001f) return false;
            Vector2 displacement = direction.normalized * moveSpeed * MovementSpeedMultiplier * Time.deltaTime;
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
            attackDirection = ((Vector2)(target.position - transform.position)).normalized;
            strikeAt = Time.time + AttackWindup;
            spriteAnimation?.PlayAttack(attackDirection, AttackWindup + .2f);
        }

        private bool ClearAttackPath()
        {
            if (target == null) return false;
            int count = Physics2D.Linecast(transform.position, target.position,
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
            EnemyLootPickup.Spawn(lootPrefab, transform.position, transform.parent, ResourceFlyweights.Item(ItemKind.Coins), coinReward);
            FarmGameEvents.RaiseEnemyDefeated();
            FarmNotificationCenter.Show($"Derrotaste a {enemyName}.");
        }

        private void ApplyVisuals()
        {
            if (bodyRenderer != null)
            {
                if (normalMaterial == null) normalMaterial = bodyRenderer.sharedMaterial;
                bodyRenderer.sharedMaterial = Time.time < hurtFlashEndsAt && FlashMaterial != null ? FlashMaterial : normalMaterial;
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
