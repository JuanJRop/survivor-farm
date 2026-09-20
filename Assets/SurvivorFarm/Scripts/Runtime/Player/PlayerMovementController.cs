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
        public bool IsDashing => Time.time < dashUntil;
        public float DashCooldown => Mathf.Max(0, nextDash - Time.time);

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
            if (direction.sqrMagnitude < .01f || Time.timeScale == 0 || Time.time < nextDash ||
                UI.InventoryPanelSystem.IsOpen || UI.VillageUpgradeWindow.IsOpen || UI.FarmIntroduction.IsOpen || GetComponent<PlayerMountController>()?.IsMounted == true || stats != null && stats.CurrentHealth <= 0) return false;
            characterAnimator?.CancelAction();
            dashDirection = direction.normalized;
            dashUntil = Time.time + .18f;
            nextDash = Time.time + 1.4f;
            stats?.GrantInvulnerability(.22f);
            return true;
        }

        private void FixedUpdate()
        {
            if (GetComponent<PlayerCombatController>()?.IsExecuting == true) { body.linearVelocity = Vector2.zero; return; }
            if (IsDashing) { body.linearVelocity = dashDirection * 11f; return; }
            if (characterAnimator == null) characterAnimator = GetComponent<PlayerCharacterAnimator>();
            if (characterAnimator != null && characterAnimator.MovementLocked)
            { body.linearVelocity = Vector2.zero; return; }
            body.linearVelocity = moveInput * (Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed) * (inventory?.MovementBonus ?? 1f) * (GetComponent<PlayerMountController>()?.SpeedMultiplier ?? 1f);
        }

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
