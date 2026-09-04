using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovementController : MonoBehaviour
    {
        public enum MobileMovementMode
        {
            TapToMove
        }

        [SerializeField] private float walkSpeed = 3.1f;
        [SerializeField] private float runSpeed = 5.1f;
        [SerializeField] private float runDistance = 3.2f;
        [SerializeField] private float destinationStopDistance = 0.08f;
        [SerializeField] private MobileMovementMode movementMode = MobileMovementMode.TapToMove;

        private Rigidbody2D body;
        private Vector2 moveInput;
        private Vector2 destination;
        private float destinationMoveSpeed = 3.1f;
        private bool hasDestination;

        public MobileMovementMode MovementMode => movementMode;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }

        private void Update()
        {
            Vector2 keyboardInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            moveInput = Vector2.zero;

            if (keyboardInput.sqrMagnitude > 0.01f)
            {
                moveInput = keyboardInput;
                hasDestination = false;
            }

            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }
        }

        private void FixedUpdate()
        {
            if (hasDestination)
            {
                Vector2 toDestination = destination - body.position;
                if (toDestination.magnitude <= destinationStopDistance)
                {
                    hasDestination = false;
                    body.linearVelocity = Vector2.zero;
                    return;
                }

                body.linearVelocity = toDestination.normalized * destinationMoveSpeed;
                return;
            }

            body.linearVelocity = moveInput * (Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed);
        }

        public void SetMovementMode(MobileMovementMode mode)
        {
            movementMode = MobileMovementMode.TapToMove;
            hasDestination = false;
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        public void MoveToWorldPosition(Vector2 worldPosition)
        {
            destination = worldPosition;
            Vector2 currentPosition = body != null ? body.position : (Vector2)transform.position;
            destinationMoveSpeed = Vector2.Distance(currentPosition, destination) >= runDistance ? runSpeed : walkSpeed;
            hasDestination = true;
        }

        public void StopMovement()
        {
            moveInput = Vector2.zero;
            hasDestination = false;

            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
