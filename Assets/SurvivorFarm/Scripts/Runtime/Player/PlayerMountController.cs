using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    [DefaultExecutionOrder(800)]
    public sealed class PlayerMountController : MonoBehaviour
    {
        public bool IsMounted => MountedHorse != null;
        public HorseMount MountedHorse { get; private set; }
        public float SpeedMultiplier => IsMounted ? 1.65f : 1f;
        private PlayerInventory inventory;
        private Rigidbody2D body;
        private CircleCollider2D feet;
        private SpriteRenderer playerVisual;
        private bool playerWasVisible;
        private CollisionDetectionMode2D previousCollisionMode;
        private Vector3 previousRiderPosition, lastHorsePosition, lastSafePosition;
        private Vector2 facing = Vector2.down;
        private readonly Collider2D[] overlaps = new Collider2D[32];
        private readonly RaycastHit2D[] passageHits = new RaycastHit2D[24];

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>(); body = GetComponent<Rigidbody2D>();
            feet = GetComponent<CircleCollider2D>(); playerVisual = GetComponent<SpriteRenderer>();
            if (playerVisual == null) playerVisual = GetComponentInChildren<SpriteRenderer>();
        }

        public bool TryMount(HorseMount horse)
        {
            if (IsMounted || horse == null || !horse.IsAvailable || inventory == null || body == null ||
                Vector2.Distance(transform.position, horse.transform.position) > 1.65f || Time.timeScale <= 0 ||
                InventoryPanelSystem.IsOpen || GetComponent<PlayerSurvivalStats>()?.CurrentHealth <= 0 ||
                VillageAdventure.Instance?.IsInsideDungeon == true || !FarmExploration.Contains(transform.position, .5f) ||
                !HasClearPassage(GroundPosition, horse.transform.position, horse)) return false;
            if (inventory.GetItemCount("Saddle") <= 0)
            { FarmNotificationCenter.Show("Fabrica una silla de montar con cuero para montar a caballo."); return false; }
            GetComponent<PlayerCombatController>()?.CancelMelee();
            GetComponent<PlayerCharacterAnimator>()?.CancelAction();
            GetComponent<PlayerMovementController>()?.StopMovement();
            MountedHorse = horse; horse.AttachRider(this);
            previousCollisionMode = body.collisionDetectionMode; body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            previousRiderPosition = lastSafePosition = transform.position;
            lastHorsePosition = GroundPosition;
            if (playerVisual != null) { playerWasVisible = playerVisual.enabled; playerVisual.enabled = false; }
            horse.transform.position = lastHorsePosition;
            horse.ShowRider(facing, false);
            FarmNotificationCenter.Show("A caballo · E para bajar");
            return true;
        }

        private Vector3 GroundPosition => feet != null ? transform.TransformPoint(feet.offset) : transform.position + Vector3.down * .4f;

        public bool TryDismount()
        {
            if (!IsMounted) return false;
            if (!TryFindDismount(lastHorsePosition, out var destination))
            { FarmNotificationCenter.Show("Busca un poco de espacio para bajar."); return false; }
            FinishDismount(lastHorsePosition);
            MovePlayer(destination);
            return true;
        }

        public void ForceDismount()
        {
            if (!IsMounted) return;
            bool travelled = Vector2.Distance(transform.position, previousRiderPosition) > 3f || !FarmExploration.Contains(transform.position, .5f);
            Vector3 horsePosition = lastHorsePosition;
            Vector3 destination = transform.position;
            bool clear = !travelled && TryFindDismount(horsePosition, out destination);
            if (!clear && !travelled) horsePosition = MountedHorse.Home;
            FinishDismount(horsePosition);
            if (clear) MovePlayer(destination);
        }

        private void FinishDismount(Vector3 horsePosition)
        {
            var horse = MountedHorse; MountedHorse = null;
            if (playerVisual != null) playerVisual.enabled = playerWasVisible;
            if (body != null) body.collisionDetectionMode = previousCollisionMode;
            GetComponent<PlayerMovementController>()?.StopMovement();
            horse.ReleaseRider(horsePosition);
        }

        private void MovePlayer(Vector3 position)
        {
            transform.position = position;
            if (body != null) { body.position = position; body.linearVelocity = Vector2.zero; }
            Physics2D.SyncTransforms();
        }

        private bool TryFindDismount(Vector3 horsePosition, out Vector3 destination)
        {
            Physics2D.SyncTransforms();
            Vector3 offset = GroundPosition - transform.position;
            for (int ring = 0; ring < 2; ring++) for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * .25f;
                Vector2 ground = (Vector2)horsePosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (.95f + ring * .35f);
                if (!GroundIsWalkable(ground) || !HasClearPassage(GroundPosition, ground, MountedHorse)) continue;
                int count = Physics2D.OverlapCircle(ground, .3f, new ContactFilter2D { useTriggers = false }, overlaps);
                bool blocked = count == overlaps.Length;
                for (int c = 0; c < count; c++)
                    if (!overlaps[c].transform.IsChildOf(transform) && !overlaps[c].transform.IsChildOf(MountedHorse.transform)) blocked = true;
                if (blocked) continue;
                destination = new Vector3(ground.x, ground.y, transform.position.z) - offset;
                return true;
            }
            destination = transform.position; return false;
        }

        private static bool GroundIsWalkable(Vector2 point) => FarmExploration.Contains(point, .3f) && !FarmExploration.IsRiver(point, .3f);

        private bool HasClearPassage(Vector2 from, Vector2 to, HorseMount horse)
        {
            Vector2 delta = to - from;
            if (delta.sqrMagnitude < .001f) return true;
            Physics2D.SyncTransforms();
            int count = Physics2D.CircleCast(from, .25f, delta.normalized, new ContactFilter2D { useTriggers = false }, passageHits, delta.magnitude);
            if (count == passageHits.Length) return false;
            for (int i=0; i<count; i++)
                if (!passageHits[i].transform.IsChildOf(transform) && !passageHits[i].transform.IsChildOf(horse.transform)) return false;
            return true;
        }

        private void LateUpdate()
        {
            if (!IsMounted) return;
            bool travelled = Vector2.Distance(transform.position, previousRiderPosition) > 3f;
            if (travelled || PortfolioSession.Instance?.IsPaused == true || GetComponent<PlayerSurvivalStats>()?.CurrentHealth <= 0 ||
                VillageAdventure.Instance?.IsInsideDungeon == true || GetComponent<HouseSystem>()?.IsInside == true ||
                !FarmExploration.Contains(transform.position, .5f))
            { ForceDismount(); return; }
            if (!GroundIsWalkable(GroundPosition)) MovePlayer(lastSafePosition);
            else lastSafePosition = transform.position;
            Vector2 motion = body != null ? body.linearVelocity : (Vector2)(transform.position - previousRiderPosition);
            if (motion.sqrMagnitude > .01f) facing = motion.normalized;
            lastHorsePosition = GroundPosition;
            MountedHorse.transform.position = lastHorsePosition;
            MountedHorse.ShowRider(facing, motion.sqrMagnitude > .01f);
            if (playerVisual != null) playerVisual.enabled = false;
            previousRiderPosition = transform.position;
        }
        private void OnDisable() => ForceDismount();
    }
}
