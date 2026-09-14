using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    public sealed class PlayerCharacterAnimator : MonoBehaviour
    {
        [SerializeField] private PlayerAnimationLibrary library;
        [SerializeField] private SpriteRenderer spriteRenderer;
        private Rigidbody2D body;
        private PlayerSurvivalStats stats;
        private PlayerAnimationLibrary.Clip active;
        private string action;
        private float actionEndsAt, elapsed;
        private bool repeatAction;
        private Vector2 facing = Vector2.down;
        private int direction;
        public PlayerAnimationLibrary Library => library;
        public string CurrentClip => active != null ? active.Name : string.Empty;
        public int CurrentFrame { get; private set; }
        public int CurrentDirection => direction;
        public bool MovementLocked => stats != null && stats.CurrentHealth <= 0 || action != null && Time.time < actionEndsAt;
        public void Configure(SpriteRenderer renderer, string characterName, string fallbackName)
        { spriteRenderer = renderer; Initialize(); }
        public void SetLibrary(PlayerAnimationLibrary value, SpriteRenderer renderer)
        { library = value; spriteRenderer = renderer; Initialize(); }
        private void Awake() => Initialize();
        private void Initialize()
        {
            if (library == null) library = Resources.Load<PlayerAnimationLibrary>("JoshAnimationLibrary");
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            body = GetComponent<Rigidbody2D>(); stats = GetComponent<PlayerSurvivalStats>();
            Switch("Idle", true); Render();
        }
        private void Update()
        {
            if (library == null) return;
            Vector2 velocity = body != null ? body.linearVelocity : Vector2.zero;
            if (stats != null && stats.CurrentHealth <= 0)
            { Switch("Dead", false); elapsed += Time.deltaTime; Render(); return; }
            if (action != null && Time.time < actionEndsAt) Switch(action, false);
            else
            {
                action = null;
                if (velocity.sqrMagnitude > .0064f) facing = velocity.normalized;
                Switch(velocity.magnitude > 3.9f ? "Run" : velocity.magnitude > .08f ? "Walk" : "Idle", false);
            }
            elapsed += Time.deltaTime; Render();
        }
        public void FaceWorldPosition(Vector3 target)
        {
            Vector2 delta = target - transform.position;
            if (delta.sqrMagnitude > .001f) facing = delta.normalized;
            Render();
        }
        public void PlayToolAction(FarmTool tool) => PlayNamedAction(ToolClip(tool));
        public void PlayNamedAction(string name) => PlayAction(name, 0);
        public void PlayAction(string name, float duration, Vector3? target = null)
        {
            var clip = library != null ? library.Find(name) : null;
            if (clip == null || stats != null && stats.CurrentHealth <= 0 && name != "Dead") return;
            if (target.HasValue) FaceWorldPosition(target.Value);
            GetComponent<PlayerMovementController>()?.StopMovement();
            action = name; repeatAction = duration > clip.Frames / clip.FramesPerSecond;
            actionEndsAt = Time.time + Mathf.Max(duration, clip.Frames / clip.FramesPerSecond);
            Switch(name, true); Render();
        }
        public void CancelAction() { action = null; actionEndsAt = 0; Switch("Idle",true); Render(); }
        private void Switch(string name, bool restart)
        {
            if (library == null || !restart && CurrentClip == name) return;
            var clip = library.Find(name); if (clip == null) return;
            active = clip; elapsed = 0;
        }
        private void Render()
        {
            if (active == null || spriteRenderer == null) return;
            direction = Mathf.Abs(facing.x) > Mathf.Abs(facing.y) ? 2 : facing.y > 0 ? 1 : 0;
            int frame = Mathf.FloorToInt(elapsed * active.FramesPerSecond);
            bool loop = active.Loop || action != null && repeatAction;
            CurrentFrame = loop ? frame % active.Frames : Mathf.Min(frame, active.Frames - 1);
            spriteRenderer.sprite = library.Frame(active,direction,CurrentFrame);
            // Josh's side-view frames face right; mirror only when facing left.
            spriteRenderer.flipX = direction == 2 && facing.x < 0;
        }
        public static string ToolClip(FarmTool tool)
        {
            switch (tool)
            {
                case FarmTool.Axe: return "Axe";
                case FarmTool.Pickaxe: return "Pickaxe";
                case FarmTool.Hoe: return "Hoe";
                case FarmTool.Shovel: return "Shovel";
                case FarmTool.WateringCan: return "Watering";
                case FarmTool.Bow: return "Bow";
                default: return "Sword";
            }
        }
    }
}
