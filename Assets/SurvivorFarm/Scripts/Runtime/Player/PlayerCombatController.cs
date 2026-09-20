using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

namespace SurvivorFarm.Runtime.Player
{
    public sealed class PlayerCombatController : MonoBehaviour
    {
        [SerializeField] private float swordRange = 1.1f;
        [SerializeField] private float bowRange = 10f;
        [SerializeField] private float attackCooldown = 0.45f;
        [SerializeField] private int swordDamage = 1;
        [SerializeField] private int arrowDamage = 8;

        private PlayerToolbelt toolbelt;
        private PlayerMovementController movement;
        private PlayerCharacterAnimator characterAnimator;
        private PlayerInventory inventory;
        [SerializeField] private Text attackButtonText;
        [SerializeField] private Image cooldownFill;
        private float nextAttackTime;
        private float activeCooldown;
        private CombatFeelRangeCue swordCue;
        private Coroutine bowRelease;
        private Coroutine swordRelease;
        private ComboController combo;
        private HitFeedback hitFeedback;
        private AudioFeedback audioFeedback;
        private bool queued;
        private float queuedUntil;
        private IDamageable queuedTarget;
        private string selectedWeapon;
        private FarmTool selectedWeaponTool;
        private string selectedWeaponEquipment;
        private int selectedWeaponTier;
        private SwordChargeController charge;
        private bool chargePose, chargeReadySound;
        private Vector2 swordDirection=Vector2.down;
        private Coroutine execution;
        private EnemyAIBase executionTarget;
        private TextMesh executionPrompt;
        private float nextExecutionTime;
        private readonly RaycastHit2D[] executionHits = new RaycastHit2D[32];
        private readonly List<Collider2D> nearbyColliders = new List<Collider2D>(32);
        private readonly List<RaycastHit2D> pathHits = new List<RaycastHit2D>(16);
        private readonly HashSet<IDamageable> meleeTargets = new HashSet<IDamageable>();
        private static readonly ContactFilter2D TargetFilter = new ContactFilter2D { useTriggers = true };
        private PlayerProjectilePool arrows;
        private int displayedAttackState = int.MinValue;
        private bool displayedBow;
        public const float ExecutionRange = 3f;
        public bool IsExecuting => execution != null;
        public EnemyAIBase ExecutionTarget => FindExecutionTarget();
        public int SwordTier => Mathf.Clamp(GetComponent<PlayerCraftingController>()?.WeaponLevel ?? 1, 1, 3);
        public bool CanChargeSword => SwordTier >= 2;
        public bool BowUnlocked => inventory != null && inventory.OwnsEquipment("Bow");
        public int ArrowCount => inventory != null ? inventory.GetAvailableItemCount("Arrow") : 0;
        public float ExecutionCooldown => Mathf.Max(0, nextExecutionTime - Time.time);
        public ComboController Combo => combo;
        public SwordChargeController Charge=>charge;
        public bool LastAttackWasCharged {get;private set;}
        public bool HasBufferedAttack => queued;
        public float SwordRange=>swordRange;
        public void SetSwordRange(float value)=>swordRange=Mathf.Clamp(value,.85f,1.15f);
        public int ProgressionDamage => (GetComponent<AdventureProgress>()?.Data.temperedBlade == true ? 2 : 0) + EquipmentItems.DamageBonusFor(inventory, FarmTool.Sword) + ((GetComponent<PlayerCraftingController>()?.WeaponLevel ?? 1)-1);
        public int PetDamageBonus => GetComponent<PlayerPetController>()?.DamageBonus ?? 0;
        public int GetAttackDamage(FarmTool tool) => tool == FarmTool.Sword
            ? swordDamage + PetDamageBonus + ProgressionDamage
            : tool == FarmTool.Bow ? Mathf.Max(8, arrowDamage) + PetDamageBonus + EquipmentItems.DamageBonusFor(inventory, tool) + ((GetComponent<ToolMastery>()?.Level(FarmTool.Bow)??1)-1) * 4 : 0;

        private void Awake()
        {
            swordRange = Mathf.Clamp(swordRange, .85f, 1.15f);
            bowRange = Mathf.Max(10f, bowRange);
            toolbelt = GetComponent<PlayerToolbelt>();
            movement = GetComponent<PlayerMovementController>();
            characterAnimator = GetComponent<PlayerCharacterAnimator>();
            inventory = GetComponent<PlayerInventory>();
            combo = GetComponent<ComboController>(); if (combo == null) combo = gameObject.AddComponent<ComboController>();
            charge = GetComponent<SwordChargeController>(); if (charge == null) charge = gameObject.AddComponent<SwordChargeController>();
            hitFeedback = GetComponent<HitFeedback>(); if (hitFeedback == null) hitFeedback = gameObject.AddComponent<HitFeedback>();
            audioFeedback = GetComponent<AudioFeedback>();
        }

        private void Update()
        {
            RefreshWeapon();
            RefreshExecutionPrompt();
            if (IsExecuting) { RefreshAttackUi(); return; }
            if (CombatBlocked || movement != null && movement.IsDashing)
            { CancelMelee(); return; }
            if (Input.GetMouseButtonDown(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                if(toolbelt==null||toolbelt.SelectedTool==FarmTool.Sword)
                {
                    if(!BeginCharge())Attack();
                }
                else Attack();
            }

            if(charge.IsPressed)
            {
                if(chargePose&&characterAnimator!=null&&!characterAnimator.IsChargingSword){CancelMelee();return;}
                if(charge.IsCharging)
                {
                    if(!chargePose){chargePose=true;audioFeedback?.Play(CombatSound.Charge,transform.position,.65f);}
                    var aim=MouseAim();movement?.StopMovement();characterAnimator?.PoseSwordCharge(aim);
                    EnsureSwordCue().ShowCharge(transform.position,charge.Progress,aim-transform.position,SwordTier);
                    if(charge.Progress>=1&&!chargeReadySound)
                    {
                        chargeReadySound=true;audioFeedback?.Play(CombatSound.ChargeReady,transform.position,.8f);
                        CombatHitParticles.Spawn(transform.position+((Vector3)swordDirection)*.5f,transform.parent,ImpactSurface.Creature,true,false);
                    }
                }
                if(Input.GetMouseButtonUp(0))ReleaseCharge();
            }

            if (queued && Time.time >= nextAttackTime)
            {
                var target = queuedTarget;
                bool validBuffer = Time.time <= queuedUntil;
                queued = false; queuedTarget = null;
                if (validBuffer) AttackTarget(ValidTarget(target) ? target : null);
            }
            RefreshAttackUi();
        }

        public void ConfigureAttackUi(Text buttonText, Image fill)
        {
            attackButtonText = buttonText;
            cooldownFill = fill;
            displayedAttackState = int.MinValue;
            RefreshAttackUi();
        }

        public void Attack()
        {
            AttackTarget(null);
        }

        public void AttackTarget(IDamageable preferredTarget)
        {
            if (CombatBlocked || IsExecuting || (movement != null && movement.IsDashing)) return;
            var survival = GetComponent<PlayerSurvivalStats>();
            if (survival != null && survival.CurrentHealth <= 0) return;
            RefreshWeapon();
            if (Time.time < nextAttackTime)
            {
                if ((toolbelt == null || toolbelt.SelectedTool == FarmTool.Sword) &&
                    nextAttackTime - Time.time <= combo.Definition.inputBuffer)
                {
                    queued = true; queuedTarget = preferredTarget;
                    queuedUntil = nextAttackTime + .1f;
                }
                return;
            }

            FarmTool selectedTool = toolbelt != null ? toolbelt.SelectedTool : FarmTool.Sword;
            if (selectedTool != FarmTool.Sword && selectedTool != FarmTool.Bow)
            {
                FarmNotificationCenter.Show("Selecciona espada o arco para atacar.");
                return;
            }

            if (characterAnimator != null && characterAnimator.MovementLocked) return;
            movement?.StopMovement();
            if (selectedTool == FarmTool.Sword)
            {
                BeginSwordAttack(preferredTarget ?? FindNearestTarget(swordRange));
                return;
            }
            // Mouse position is the only player bow aim. Preferred melee targets never steer an arrow.
            TryShootBowAt(MouseAim());
        }

        private bool CombatBlocked => InventoryPanelSystem.IsOpen || MasteryWindow.IsOpen || VillageUpgradeWindow.IsOpen ||
            !FarmIntroduction.AllowsCombat || Time.timeScale <= 0 || GetComponent<PlayerSurvivalStats>()?.CurrentHealth <= 0 ||
            GetComponent<PlayerMountController>()?.IsMounted == true;

        public bool TryShootBowAt(Vector3 worldPosition)
        {
            RefreshWeapon();
            if (CombatBlocked || IsExecuting || !BowUnlocked || ArrowCount <= 0 || Time.time < nextAttackTime ||
                movement != null && movement.IsDashing || characterAnimator != null && characterAnimator.MovementLocked) return false;
            Vector2 aim = worldPosition - transform.position;
            if (aim.sqrMagnitude < .0001f) return false;
            Vector2 direction = aim.normalized;
            movement?.StopMovement();
            var clip = characterAnimator != null && characterAnimator.Library != null ? characterAnimator.Library.Find("Bow") : null;
            float clipDuration = clip != null && clip.FramesPerSecond > 0f ? clip.Frames / clip.FramesPerSecond : 0f;
            activeCooldown = Mathf.Max(Mathf.Max(.65f, attackCooldown), clipDuration);
            nextAttackTime = Time.time + activeCooldown;
            characterAnimator?.PlayAction("Bow", 0, transform.position + (Vector3)direction);
            int damage = GetAttackDamage(FarmTool.Bow);
            if (clipDuration > 0f)
                bowRelease = StartCoroutine(ReleaseArrow(direction, damage, clipDuration * .55f, characterAnimator.ActionVersion));
            else if (!ShootArrow(direction, damage))
            {
                nextAttackTime = Time.time; activeCooldown = 0;
                return false;
            }
            return true;
        }

        public bool BeginCharge()
        {
            RefreshWeapon();
            if(!CanChargeSword||CombatBlocked||IsExecuting||Time.time<nextAttackTime||charge.IsPressed||
                toolbelt!=null&&toolbelt.SelectedTool!=FarmTool.Sword||movement!=null&&movement.IsDashing||
                characterAnimator!=null&&characterAnimator.MovementLocked||GetComponent<PlayerSurvivalStats>()?.CurrentHealth<=0)return false;
            charge.Press(Time.time);chargePose=chargeReadySound=false;return true;
        }
        public void ReleaseCharge(IDamageable preferredTarget=null)
        {
            if(!charge.IsPressed)return;
            bool charged=charge.Release(Time.time)>=1;
            EnsureSwordCue().HideCharge();
            if(characterAnimator!=null&&characterAnimator.IsChargingSword)characterAnimator.CancelAction();
            chargePose=chargeReadySound=false;
            if(CombatBlocked||IsExecuting)return;
            if(!charged){AttackTarget(preferredTarget);return;}
            movement?.StopMovement();
            BeginSwordAttack(preferredTarget??FindNearestTarget(swordRange*combo.Definition.chargedRangeMultiplier),true);
        }
        private Vector3 MouseAim()
        {
            if(Camera.main==null)return transform.position+(Vector3)swordDirection;
            Vector3 aim=Camera.main.ScreenToWorldPoint(Input.mousePosition);aim.z=transform.position.z;
            if((aim-transform.position).sqrMagnitude>.01f)swordDirection=(aim-transform.position).normalized;
            return aim;
        }
        private CombatFeelRangeCue EnsureSwordCue()
        {
            if (swordCue != null) return swordCue;
            swordCue = GetComponent<CombatFeelRangeCue>();
            if (swordCue == null) swordCue = gameObject.AddComponent<CombatFeelRangeCue>();
            return swordCue;
        }

        private void RefreshWeapon()
        {
            string equipment = inventory?.EquippedEquipment != null && inventory.EquippedEquipment.Length > 3 ? inventory.EquippedEquipment[3] : "";
            FarmTool tool = toolbelt != null ? toolbelt.SelectedTool : FarmTool.Sword;
            int tier = SwordTier;
            if (selectedWeapon != null && selectedWeaponTool == tool && selectedWeaponEquipment == equipment && selectedWeaponTier == tier) return;
            selectedWeaponTool = tool; selectedWeaponEquipment = equipment; selectedWeaponTier = tier;
            string identity = tool + "/" + equipment + "/" + tier;
            if (bowRelease != null) StopCoroutine(bowRelease);
            bowRelease = null;
            if (selectedWeapon != null && characterAnimator != null && characterAnimator.CurrentClip == "Bow") characterAnimator.CancelAction();
            CancelMelee(); selectedWeapon = identity; combo.SelectWeapon(identity);
        }

        public void CancelMelee()
        {
            charge?.Cancel();chargePose=chargeReadySound=false;swordCue?.HideCharge();
            queued = false; queuedTarget = null;
            if (swordRelease != null) StopCoroutine(swordRelease);
            swordRelease = null;
            combo?.ResetChain();
            if (characterAnimator != null && characterAnimator.CurrentClip == "Sword" && characterAnimator.MovementLocked)
                characterAnimator.CancelAction();
        }

        private void BeginSwordAttack(IDamageable target,bool charged=false)
        {
            queued = false; queuedTarget = null;
            var attack = charged?combo.BeginCharged():combo.Begin(Time.time);
            if (attack == null) return;
            LastAttackWasCharged=charged;
            Vector3 aim=ValidTarget(target)?target.Transform.position:MouseAim();
            if((aim-transform.position).sqrMagnitude>.001f)swordDirection=(aim-transform.position).normalized;
            activeCooldown = Mathf.Max(.15f, attack.duration);
            nextAttackTime = Time.time + activeCooldown;
            int damage = Mathf.Max(1, Mathf.RoundToInt(GetAttackDamage(FarmTool.Sword) * attack.damageMultiplier) + attack.bonusDamage + (charged && SwordTier >= 3 ? 3 : 0));
            characterAnimator?.PlayMeleeAction(activeCooldown, attack.impactFraction, attack.heavy,
                aim);
            audioFeedback?.Play(charged?CombatSound.ChargedSwing:attack.heavy ? CombatSound.ThirdSwing : CombatSound.Swing, transform.position, .8f);
            if (characterAnimator == null || characterAnimator.Library == null)
                PerformSwordAreaAttack(damage, attack.heavy,charged);
            else swordRelease = StartCoroutine(ReleaseSword(damage, attack.heavy, charged,activeCooldown * attack.impactFraction, characterAnimator.ActionVersion));
        }

        private IEnumerator ReleaseSword(int damage, bool heavy, bool charged,float delay, int actionVersion)
        {
            float impactAt = Time.time + delay;
            while (true)
            {
                if (characterAnimator == null || characterAnimator.ActionVersion != actionVersion ||
                    characterAnimator.CurrentClip != "Sword" || CombatBlocked ||
                    movement != null && movement.IsDashing || GetComponent<PlayerSurvivalStats>()?.CurrentHealth <= 0) break;
                if (Time.time >= impactAt) { PerformSwordAreaAttack(damage, heavy,charged); break; }
                yield return null;
            }
            swordRelease = null;
        }

        private void PerformSwordAreaAttack(int damage, bool heavy,bool charged=false)
        {
            bool areaBlast = charged && SwordTier >= 3;
            float range=areaBlast?1.9f:charged?Mathf.Min(1.3f,swordRange*combo.Definition.chargedRangeMultiplier):swordRange;
            EnsureSwordCue().Show(transform.position, range, heavy, combo.StepNumber,charged,swordDirection,SwordTier);
            meleeTargets.Clear();
            Physics2D.OverlapCircle(transform.position, range, TargetFilter, nearbyColliders);
            foreach (var collider in nearbyColliders)
            {
                var candidate = collider.GetComponentInParent<IDamageable>();
                if (!ValidTarget(candidate) || candidate.Transform.IsChildOf(transform)) continue;
                if (Vector2.Distance(transform.position, candidate.Transform.position) > range || !HasClearPath(candidate, true)) continue;
                Vector2 offset = candidate.Transform.position - transform.position;
                if (!areaBlast && offset.sqrMagnitude > .08f && Vector2.Dot(swordDirection, offset.normalized) < (charged ? -.35f : -.05f)) continue;
                meleeTargets.Add(candidate);
            }
            hitFeedback.BeginStrike();
            foreach (var candidate in meleeTargets)
                if (ValidTarget(candidate)) hitFeedback.ApplyDamage(candidate, damage, inventory, heavy,FarmTool.Sword,charged);
            meleeTargets.Clear();
            nearbyColliders.Clear();
        }

        private bool HasClearPath(IDamageable target, bool swordArea = false)
        {
            Physics2D.Linecast(transform.position, target.Transform.position, TargetFilter, pathHits);
            foreach (RaycastHit2D hit in pathHits)
            {
                if (hit.collider.isTrigger || hit.transform.IsChildOf(transform) ||
                    hit.transform.IsChildOf(target.Transform))
                {
                    continue;
                }

                if (swordArea && hit.collider.GetComponentInParent<EnemyAIBase>() != null) continue;

                return false;
            }

            return true;
        }

        private static bool ValidTarget(IDamageable target) => target != null &&
            !(target is Object instance && instance == null) && target.Transform != null && DamageRules.CanPlayerHit(target);

        private IEnumerator ReleaseArrow(Vector2 direction, int damage, float delay, int actionVersion)
        {
            float releaseAt = Time.time + delay;
            while (true)
            {
                var stats = GetComponent<PlayerSurvivalStats>();
                if (CombatBlocked || stats != null && stats.CurrentHealth <= 0 || characterAnimator == null ||
                    characterAnimator.ActionVersion != actionVersion || characterAnimator.CurrentClip != "Bow" || movement != null && movement.IsDashing) break;
                if (Time.time >= releaseAt)
                {
                    ShootArrow(direction, damage);
                    break;
                }
                yield return null;
            }
            bowRelease = null;
        }

        private void OnDisable()
        {
            CancelMelee();
            CancelExecution();
            if (bowRelease != null) StopCoroutine(bowRelease);
            bowRelease = null;
            if (swordCue != null) swordCue.enabled = false;
        }

        private void OnEnable() { if (swordCue != null) swordCue.enabled = true; }
        private void OnApplicationFocus(bool focused) { if(!focused){CancelMelee();CancelExecution();} }

        private bool ShootArrow(Vector2 direction, int damage)
        {
            if (!BowUnlocked || ArrowCount <= 0) return false;
            if (arrows == null) arrows = PlayerProjectilePool.Create(transform);
            var projectile = arrows.Rent();
            if (projectile == null) return false;
            // Reserve capacity before paying: a full pool must never consume ammunition.
            if (!inventory.TryRemoveItem("Arrow", 1)) { projectile.ReturnToPool(); return false; }
            audioFeedback?.Play(CombatSound.Bow, transform.position, .75f);
            GameObject arrowObject = projectile.gameObject;
            arrowObject.transform.position = transform.position;
            arrowObject.transform.localScale = Vector3.one * 0.65f;

            SpriteRenderer renderer = arrowObject.GetComponent<SpriteRenderer>();
            renderer.sprite = CombatFeelVisuals.Arrow;
            renderer.color = Color.white;
            renderer.enabled = true; renderer.flipX = false; renderer.flipY = false;
            renderer.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 20);

            projectile.ConfigureDirection(direction, damage, inventory, bowRange);
            arrowObject.SetActive(true);
            return true;
        }

        public bool CanExecute(EnemyAIBase enemy)
        {
            return CanStartExecution && CanReachExecution(enemy);
        }

        private bool CanStartExecution => !(CombatBlocked || IsExecuting || Time.time < nextExecutionTime ||
                Time.time < nextAttackTime || movement != null && movement.IsDashing ||
                characterAnimator != null && characterAnimator.MovementLocked || inventory == null || !inventory.OwnsEquipment("Sword"));

        private bool CanReachExecution(EnemyAIBase enemy)
        {
            if (enemy == null || !enemy.CanBeExecuted) return false;
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance > ExecutionRange || !HasClearPath(enemy)) return false;
            Vector2 direction = (enemy.transform.position - transform.position).normalized;
            return ClearExecutionStep(direction, Mathf.Max(0, distance - .65f), enemy);
        }

        private EnemyAIBase FindExecutionTarget()
        {
            if (!CanStartExecution) return null;
            EnemyAIBase selected = null; float closest = ExecutionRange;
            Physics2D.OverlapCircle(transform.position, ExecutionRange, TargetFilter, nearbyColliders);
            foreach (var collider in nearbyColliders)
            {
                var enemy = collider.GetComponentInParent<EnemyAIBase>();
                if (!CanReachExecution(enemy)) continue;
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance <= closest) { closest = distance; selected = enemy; }
            }
            nearbyColliders.Clear();
            return selected;
        }

        public bool TryExecuteNearest() => TryExecute(FindExecutionTarget());
        public bool TryExecute(EnemyAIBase enemy)
        {
            if (!CanExecute(enemy)) return false;
            CancelMelee();
            if (bowRelease != null) StopCoroutine(bowRelease);
            bowRelease = null;
            toolbelt?.Select(FarmTool.Sword);
            nextExecutionTime = Time.time + 1.6f;
            activeCooldown = .55f; nextAttackTime = Time.time + activeCooldown;
            executionTarget = enemy;
            execution = StartCoroutine(Execute(enemy, enemy.SpawnGeneration));
            return true;
        }

        private bool ClearExecutionStep(Vector2 direction, float distance, EnemyAIBase target)
        {
            if (distance <= .001f) return true;
            // Sweep the actual movement footprint. The village feet sit below the sprite pivot.
            var feet = GetComponent<CircleCollider2D>();
            Vector2 center = feet != null ? (Vector2)feet.transform.TransformPoint(feet.offset) : (Vector2)transform.position;
            float radius = feet != null ? feet.radius * Mathf.Max(Mathf.Abs(feet.transform.lossyScale.x), Mathf.Abs(feet.transform.lossyScale.y)) : .24f;
            int count = Physics2D.CircleCast(center, radius, direction,
                new ContactFilter2D { useTriggers = false }, executionHits, distance);
            if (count == executionHits.Length) return false;
            for (int i = 0; i < count; i++)
                if (!executionHits[i].transform.IsChildOf(transform) && !executionHits[i].transform.IsChildOf(target.transform)) return false;
            return true;
        }

        private IEnumerator Execute(EnemyAIBase enemy, int generation)
        {
            movement?.StopMovement();
            Vector3 start = transform.position;
            Vector2 direction = (enemy.transform.position - start).normalized;
            float travel = Mathf.Clamp(Vector2.Distance(start, enemy.transform.position) - .65f, 0, 2.4f);
            Vector3 destination = start + (Vector3)(direction * travel);
            characterAnimator?.PlayAction("Run", .18f, enemy.transform.position);
            int version = characterAnimator != null ? characterAnimator.ActionVersion : 0;
            float until = Time.time + .16f;
            var body = GetComponent<Rigidbody2D>();
            bool aborted = false;
            while (Time.time < until)
            {
                if (CombatBlocked || enemy == null || !enemy.IsAlive || enemy.SpawnGeneration != generation ||
                    characterAnimator != null && characterAnimator.ActionVersion != version) { aborted = true; break; }
                Vector3 next = Vector3.Lerp(start, destination, Mathf.Clamp01(1 - (until - Time.time) / .16f));
                Vector2 delta = next - transform.position;
                if (!ClearExecutionStep(delta.normalized, delta.magnitude, enemy)) { aborted = true; break; }
                if (body != null) { body.position = next; body.linearVelocity = Vector2.zero; }
                transform.position = next; Physics2D.SyncTransforms();
                yield return null;
            }
            if (!aborted && enemy != null && enemy.CanBeExecuted && enemy.SpawnGeneration == generation &&
                Vector2.Distance(transform.position, enemy.transform.position) <= 1.25f && HasClearPath(enemy))
            {
                swordDirection = (enemy.transform.position - transform.position).normalized;
                characterAnimator?.PlayMeleeAction(.34f, .32f, true, enemy.transform.position);
                version = characterAnimator != null ? characterAnimator.ActionVersion : 0;
                float strikeAt = Time.time + .1f;
                while (Time.time < strikeAt)
                {
                    if (CombatBlocked || characterAnimator != null && characterAnimator.ActionVersion != version) { aborted = true; break; }
                    yield return null;
                }
                if (!aborted && enemy != null && enemy.CanBeExecuted && enemy.SpawnGeneration == generation &&
                    Vector2.Distance(transform.position, enemy.transform.position) <= 1.3f && HasClearPath(enemy))
                {
                    EnsureSwordCue().Show(transform.position, 1.2f, true, 3, false, swordDirection, SwordTier);
                    hitFeedback.BeginStrike(); hitFeedback.ApplyDamage(enemy, enemy.CurrentHealth, inventory, true, FarmTool.Sword);
                    audioFeedback?.Play(CombatSound.Finisher, enemy.transform.position, .9f);
                }
            }
            executionTarget = null; execution = null;
        }

        private void CancelExecution()
        {
            if (execution != null) StopCoroutine(execution);
            execution = null; executionTarget = null;
            movement?.StopMovement();
            if (executionPrompt != null) executionPrompt.gameObject.SetActive(false);
        }

        private void RefreshExecutionPrompt()
        {
            var target = FindExecutionTarget();
            if (target == null) { if (executionPrompt != null) executionPrompt.gameObject.SetActive(false); return; }
            if (executionPrompt == null)
            {
                var go = new GameObject("E · EJECUTAR"); go.transform.SetParent(transform.parent, false);
                executionPrompt = go.AddComponent<TextMesh>();
                executionPrompt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                executionPrompt.fontSize = 32; executionPrompt.characterSize = .08f;
                executionPrompt.anchor = TextAnchor.LowerCenter; executionPrompt.alignment = TextAlignment.Center;
                executionPrompt.text = "E · EJECUTAR"; executionPrompt.color = new Color(1, .85f, .55f);
                var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = executionPrompt.font.material; renderer.sortingOrder = 12500;
            }
            float height = 1.05f;
            if (target.SpriteAnimation != null && target.SpriteAnimation.Visual != null)
                height = target.SpriteAnimation.Visual.bounds.max.y - target.transform.position.y + .12f;
            executionPrompt.transform.position = target.transform.position + Vector3.up * height;
            executionPrompt.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            if (executionPrompt != null) Destroy(executionPrompt.gameObject);
            if (arrows != null) Destroy(arrows.gameObject);
        }

        private void RefreshAttackUi()
        {
            float remaining = Mathf.Max(0f, nextAttackTime - Time.time);
            bool ready = remaining <= 0f;

            if (attackButtonText != null)
            {
                bool bow = toolbelt != null && toolbelt.SelectedTool == FarmTool.Bow;
                int state = bow ? ArrowCount : ready ? -1 : Mathf.RoundToInt(remaining * 10);
                if (state != displayedAttackState || bow != displayedBow)
                {
                    displayedAttackState = state; displayedBow = bow;
                    attackButtonText.text = bow ? $"Flechas {state}" : ready ? "Atacar" : $"{state * .1f:0.0}s";
                }
            }

            if (cooldownFill != null)
            {
                cooldownFill.enabled = !ready;
                cooldownFill.fillAmount = activeCooldown <= 0f ? 0f : Mathf.Clamp01(remaining / activeCooldown);
            }
        }

        private IDamageable FindNearestTarget(float range)
        {
            Physics2D.OverlapCircle(transform.position, range, TargetFilter, nearbyColliders);
            IDamageable nearest = null;
            float nearestDistance = range * range;
            Camera camera = Camera.main;
            Vector2 aim = camera != null ? (Vector2)(camera.ScreenToWorldPoint(Input.mousePosition) - transform.position) : Vector2.zero;

            foreach (Collider2D candidate in nearbyColliders)
            {
                IDamageable target = candidate.GetComponentInParent<IDamageable>();
                if (!ValidTarget(target) || target.Transform.IsChildOf(transform))
                {
                    continue;
                }

                Vector2 offset=target.Transform.position-transform.position;
                if(aim.sqrMagnitude>.01f && Vector2.Angle(aim,offset)>50f)continue;
                float distance = offset.sqrMagnitude;
                if (distance <= nearestDistance && HasClearPath(target))
                {
                    nearest = target;
                    nearestDistance = distance;
                }
            }

            nearbyColliders.Clear();
            return nearest;
        }

    }
}
