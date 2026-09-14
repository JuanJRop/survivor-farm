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

        public MobileMovementMode MovementMode => movementMode;

        private void Awake()
        {
            var feet = GetComponent<CircleCollider2D>();
            if (feet != null) { feet.radius = .27f; feet.offset = new Vector2(0, -.12f); }
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }

        private void Update()
        {
            if (SurvivorFarm.Runtime.UI.InventoryPanelSystem.IsOpen) { StopMovement(); return; }
            moveInput = new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        }

        private void FixedUpdate()
        {
            if (characterAnimator == null) characterAnimator = GetComponent<PlayerCharacterAnimator>();
            if (characterAnimator != null && characterAnimator.MovementLocked)
            { body.linearVelocity = Vector2.zero; return; }
            body.linearVelocity = moveInput * (Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed) * (GetComponent<PlayerInventory>()?.MovementBonus ?? 1f);
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

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
