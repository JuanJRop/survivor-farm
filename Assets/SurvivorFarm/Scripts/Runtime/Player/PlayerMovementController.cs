using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovementController : MonoBehaviour
    {
        public enum MobileMovementMode
        {
            KeyboardAndMouse
        }

        [SerializeField] private float walkSpeed = 3.1f;
        [SerializeField] private float runSpeed = 5.1f;
        [SerializeField] private MobileMovementMode movementMode = MobileMovementMode.KeyboardAndMouse;

        private Rigidbody2D body;
        private PlayerCharacterAnimator characterAnimator;
        private Vector2 moveInput;
        private PlayerInventory inventory;
        private PlayerSurvivalStats stats;
        private Vector2 dashDirection;
        private float dashUntil, nextDash;
        private float secondDashUntil;
        private bool secondDashReady;
        public bool IsDashing => Time.time < dashUntil;
        public float DashCooldown => secondDashReady && Time.time < secondDashUntil && !IsDashing ? 0 : Mathf.Max(0, nextDash - Time.time);
        public void RefundDashCooldown(float seconds) => nextDash = Mathf.Max(Time.time, nextDash - Mathf.Max(0, seconds));

        public MobileMovementMode MovementMode => movementMode;

        private void Awake()
        {
            var feet = GetComponent<CircleCollider2D>();
            if (feet != null) { feet.radius = .27f; feet.offset = new Vector2(0, -.12f); }
            if(feet!=null&&SurvivorFarm.Runtime.Core.PortfolioSession.Active){feet.radius=.24f;feet.offset=new Vector2(0,-.4f);}
            body = GetComponent<Rigidbody2D>();
            inventory = GetComponent<PlayerInventory>();
            stats = GetComponent<PlayerSurvivalStats>();
            characterAnimator = GetComponent<PlayerCharacterAnimator>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }

        private void Update()
        {
            if (SurvivorFarm.Runtime.UI.InventoryPanelSystem.IsOpen||UI.VillageUpgradeWindow.IsOpen||GetComponent<PlayerCombatController>()?.IsExecuting==true||!UI.FarmIntroduction.AllowsMovement) { StopMovement(); return; }
            moveInput = new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
            if (Core.PortfolioSession.Active && Input.GetKeyDown(KeyCode.Space)) TryDash(moveInput);
        }

        public bool TryDash(Vector2 direction)
        {
            SkillTreeManager skills = GetComponent<SkillTreeManager>();
            bool secondDash = skills != null && skills.HasSecondDash && secondDashReady && Time.time < secondDashUntil;
            if (direction.sqrMagnitude < .01f || Time.timeScale == 0 || IsDashing || (Time.time < nextDash && !secondDash) ||
                UI.InventoryPanelSystem.IsOpen || UI.VillageUpgradeWindow.IsOpen || UI.FarmIntroduction.BlocksGameplay || GetComponent<PlayerMountController>()?.IsMounted == true || stats != null && stats.CurrentHealth <= 0) return false;
            characterAnimator?.CancelAction();
            dashDirection = direction.normalized;
            dashUntil = Time.time + .18f * (skills?.DashDurationMultiplier ?? 1f);
            if (secondDash) secondDashReady = false;
            else
            {
                nextDash = Time.time + 1.4f * (skills?.DashCooldownMultiplier ?? 1f);
                secondDashReady = skills != null && skills.HasSecondDash;
                secondDashUntil = Time.time + .8f;
            }
            stats?.GrantInvulnerability(.22f);
            skills?.NotifyDash(dashDirection);
            return true;
        }

        private void FixedUpdate()
        {
            var combat = GetComponent<PlayerCombatController>();
            if (combat?.IsExecuting == true) { body.linearVelocity = Vector2.zero; return; }
            if (IsDashing) { body.linearVelocity = dashDirection * 11f * (GetComponent<SkillTreeManager>()?.DashSpeedMultiplier ?? 1f); return; }
            if (characterAnimator == null) characterAnimator = GetComponent<PlayerCharacterAnimator>();
            bool combatMovement = combat != null && combat.AllowsMovementDuringCombat;
            if (characterAnimator != null && characterAnimator.MovementLocked && !combatMovement)
            { body.linearVelocity = Vector2.zero; return; }
            float combatMultiplier = combatMovement ? combat.CombatMovementSpeedMultiplier : 1f;
            bool sprinting = IsSprintModifierHeld(Input.GetKey(KeyCode.LeftShift), Input.GetKey(KeyCode.RightShift));
            body.linearVelocity = moveInput * (sprinting ? runSpeed : walkSpeed) * combatMultiplier * (inventory?.MovementBonus ?? 1f) * (GetComponent<SkillTreeManager>()?.MovementMultiplier ?? 1f) * (GetComponent<PlayerMountController>()?.SpeedMultiplier ?? 1f);
        }

        public static bool IsSprintModifierHeld(bool leftShiftHeld, bool rightShiftHeld) => leftShiftHeld || rightShiftHeld;

        // Old save data can no longer enable destination or touch movement.
        public void SetMovementMode(MobileMovementMode mode)
        {
            movementMode = MobileMovementMode.KeyboardAndMouse;
            StopMovement();
        }

        public void StopMovement()
        {
            moveInput = Vector2.zero;
            dashUntil = 0;

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
