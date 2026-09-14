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
        [SerializeField] private float swordRange = 1.25f;
        [SerializeField] private float bowRange = 6f;
        [SerializeField] private float attackCooldown = 0.45f;
        [SerializeField] private int swordDamage = 1;
        [SerializeField] private int arrowDamage = 1;

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
        public ComboController Combo => combo;
        public bool HasBufferedAttack => queued;
        public int ProgressionDamage => (GetComponent<AdventureProgress>()?.Data.temperedBlade == true ? 2 : 0) + EquipmentItems.DamageBonusFor(inventory, FarmTool.Sword) + ((GetComponent<PlayerCraftingController>()?.WeaponLevel ?? 1)-1);
        public int PetDamageBonus => GetComponent<PlayerPetController>()?.DamageBonus ?? 0;
        public int GetAttackDamage(FarmTool tool) => tool == FarmTool.Sword
            ? swordDamage + PetDamageBonus + ProgressionDamage
            : tool == FarmTool.Bow ? arrowDamage + PetDamageBonus + EquipmentItems.DamageBonusFor(inventory, tool) : 0;

        private void Awake()
        {
            toolbelt = GetComponent<PlayerToolbelt>();
            movement = GetComponent<PlayerMovementController>();
            characterAnimator = GetComponent<PlayerCharacterAnimator>();
            inventory = GetComponent<PlayerInventory>();
            combo = GetComponent<ComboController>() ?? gameObject.AddComponent<ComboController>();
            hitFeedback = GetComponent<HitFeedback>() ?? gameObject.AddComponent<HitFeedback>();
            audioFeedback = GetComponent<AudioFeedback>();
        }

        private void Update()
        {
            RefreshWeapon();
            if (InventoryPanelSystem.IsOpen || Time.timeScale == 0 || movement != null && movement.IsDashing)
            { CancelMelee(); return; }
            if (Input.GetMouseButtonDown(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                Attack();
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
            RefreshAttackUi();
        }

        public void Attack()
        {
            AttackTarget(null);
        }

        public void AttackTarget(IDamageable preferredTarget)
        {
            if (InventoryPanelSystem.IsOpen || Time.timeScale == 0f || (movement != null && movement.IsDashing)) return;
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
            float range = selectedTool == FarmTool.Sword ? swordRange : bowRange;
            IDamageable target = preferredTarget ?? FindNearestTarget(range);
            if (selectedTool == FarmTool.Sword)
            {
                BeginSwordAttack(target);
                return;
            }
            var clip = characterAnimator != null && characterAnimator.Library != null ? characterAnimator.Library.Find(PlayerCharacterAnimator.ToolClip(selectedTool)) : null;
            float clipDuration = clip != null && clip.FramesPerSecond > 0f ? clip.Frames / clip.FramesPerSecond : 0f;
            activeCooldown = Mathf.Max(attackCooldown, clipDuration);
            nextAttackTime = Time.time + activeCooldown;
            characterAnimator?.PlayAction(PlayerCharacterAnimator.ToolClip(selectedTool), 0, ValidTarget(target) ? (Vector3?)target.Transform.position : null);
            if (!ValidTarget(target) ||
                (target.Transform.position - transform.position).sqrMagnitude > range * range ||
                !HasClearPath(target))
            {
                FarmNotificationCenter.Show("No hay un objetivo al alcance.");
                return;
            }

            int damage = GetAttackDamage(FarmTool.Bow);
            if (clipDuration > 0f)
                bowRelease = StartCoroutine(ReleaseArrow(target, target.SpawnGeneration, damage, clipDuration * 0.55f));
            else ShootArrow(target, damage);
        }

        private void RefreshWeapon()
        {
            string equipment = inventory?.EquippedEquipment != null && inventory.EquippedEquipment.Length > 3 ? inventory.EquippedEquipment[3] : "";
            string identity = (toolbelt != null ? toolbelt.SelectedTool : FarmTool.Sword) + "/" + equipment;
            if (identity == selectedWeapon) return;
            if (bowRelease != null) StopCoroutine(bowRelease);
            bowRelease = null;
            if (selectedWeapon != null && characterAnimator != null && characterAnimator.CurrentClip == "Bow") characterAnimator.CancelAction();
            CancelMelee(); selectedWeapon = identity; combo.SelectWeapon(identity);
        }

        public void CancelMelee()
        {
            queued = false; queuedTarget = null;
            if (swordRelease != null) StopCoroutine(swordRelease);
            swordRelease = null;
            combo?.ResetChain();
            if (characterAnimator != null && characterAnimator.CurrentClip == "Sword" && characterAnimator.MovementLocked)
                characterAnimator.CancelAction();
        }

        private void BeginSwordAttack(IDamageable target)
        {
            queued = false; queuedTarget = null;
            var attack = combo.Begin(Time.time);
            if (attack == null) return;
            activeCooldown = Mathf.Max(.15f, attack.duration);
            nextAttackTime = Time.time + activeCooldown;
            int damage = Mathf.Max(1, Mathf.RoundToInt(GetAttackDamage(FarmTool.Sword) * attack.damageMultiplier) + attack.bonusDamage);
            characterAnimator?.PlayMeleeAction(activeCooldown, attack.impactFraction, attack.heavy,
                ValidTarget(target) ? (Vector3?)target.Transform.position : null);
            audioFeedback?.Play(attack.heavy ? CombatSound.ThirdSwing : CombatSound.Swing, transform.position, .8f);
            if (characterAnimator == null || characterAnimator.Library == null)
                PerformSwordAreaAttack(damage, attack.heavy);
            else swordRelease = StartCoroutine(ReleaseSword(damage, attack.heavy, activeCooldown * attack.impactFraction, characterAnimator.ActionVersion));
        }

        private IEnumerator ReleaseSword(int damage, bool heavy, float delay, int actionVersion)
        {
            float impactAt = Time.time + delay;
            while (true)
            {
                if (characterAnimator == null || characterAnimator.ActionVersion != actionVersion ||
                    characterAnimator.CurrentClip != "Sword" || InventoryPanelSystem.IsOpen ||
                    movement != null && movement.IsDashing || GetComponent<PlayerSurvivalStats>()?.CurrentHealth <= 0) break;
                if (Time.time >= impactAt) { PerformSwordAreaAttack(damage, heavy); break; }
                yield return null;
            }
            swordRelease = null;
        }

        private void PerformSwordAreaAttack(int damage, bool heavy)
        {
            if (swordCue == null) swordCue = GetComponent<CombatFeelRangeCue>() ?? gameObject.AddComponent<CombatFeelRangeCue>();
            swordCue.Show(transform.position, swordRange, heavy, combo.StepNumber);
            var targets = new HashSet<IDamageable>();
            foreach (var collider in Physics2D.OverlapCircleAll(transform.position, swordRange))
            {
                var candidate = collider.GetComponentInParent<IDamageable>();
                if (!ValidTarget(candidate) || candidate.Transform.IsChildOf(transform)) continue;
                if (Vector2.Distance(transform.position, candidate.Transform.position) > swordRange || !HasClearPath(candidate, true)) continue;
                targets.Add(candidate);
            }
            hitFeedback.BeginStrike();
            foreach (var candidate in targets)
                if (ValidTarget(candidate)) hitFeedback.ApplyDamage(candidate, damage, inventory, heavy);
        }

        private bool HasClearPath(IDamageable target, bool swordArea = false)
        {
            RaycastHit2D[] hits = Physics2D.LinecastAll(transform.position, target.Transform.position);
            foreach (RaycastHit2D hit in hits)
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

        private IEnumerator ReleaseArrow(IDamageable target, int generation, int damage, float delay)
        {
            float releaseAt = Time.time + delay;
            while (true)
            {
                var stats = GetComponent<PlayerSurvivalStats>();
                if (stats != null && stats.CurrentHealth <= 0 || characterAnimator == null ||
                    characterAnimator.CurrentClip != PlayerCharacterAnimator.ToolClip(FarmTool.Bow)) break;
                if (Time.time >= releaseAt)
                {
                    if (ValidTarget(target) && target.SpawnGeneration == generation &&
                        (target.Transform.position - transform.position).sqrMagnitude <= bowRange * bowRange && HasClearPath(target))
                        ShootArrow(target, damage);
                    break;
                }
                yield return null;
            }
            bowRelease = null;
        }

        private void OnDisable()
        {
            CancelMelee();
            if (bowRelease != null) StopCoroutine(bowRelease);
            bowRelease = null;
            if (swordCue != null) swordCue.enabled = false;
        }

        private void OnEnable() { if (swordCue != null) swordCue.enabled = true; }

        private void ShootArrow(IDamageable target, int damage)
        {
            audioFeedback?.Play(CombatSound.Bow, transform.position, .75f);
            GameObject arrowObject = new GameObject("Arrow Projectile");
            arrowObject.transform.position = transform.position;
            arrowObject.transform.localScale = Vector3.one * 0.65f;

            SpriteRenderer renderer = arrowObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CombatFeelVisuals.Arrow;
            renderer.color = Color.white;
            renderer.sortingOrder = 8;

            ArrowProjectile projectile = arrowObject.AddComponent<ArrowProjectile>();
            projectile.Configure(target, damage, inventory);
        }

        private void RefreshAttackUi()
        {
            float remaining = Mathf.Max(0f, nextAttackTime - Time.time);
            bool ready = remaining <= 0f;

            if (attackButtonText != null)
            {
                attackButtonText.text = ready ? "Atacar" : $"{remaining:0.0}s";
            }

            if (cooldownFill != null)
            {
                cooldownFill.enabled = !ready;
                cooldownFill.fillAmount = activeCooldown <= 0f ? 0f : Mathf.Clamp01(remaining / activeCooldown);
            }
        }

        private IDamageable FindNearestTarget(float range)
        {
            Collider2D[] candidates = Physics2D.OverlapCircleAll(transform.position, range);
            IDamageable nearest = null;
            float nearestDistance = range * range;
            Camera camera = Camera.main;
            Vector2 aim = camera != null ? (Vector2)(camera.ScreenToWorldPoint(Input.mousePosition) - transform.position) : Vector2.zero;

            foreach (Collider2D candidate in candidates)
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

            return nearest;
        }

    }
}
