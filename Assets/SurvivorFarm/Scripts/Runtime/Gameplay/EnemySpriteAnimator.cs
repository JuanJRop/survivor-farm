using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class EnemySpriteAnimator : MonoBehaviour
    {
        private PlayerAnimationLibrary library;
        private SpriteRenderer visual;
        private Vector3 previousPosition;
        private float startedAt, actionUntil, actionDuration;
        private int direction;
        private bool dead;
        public string StateName { get; private set; } = "Idle";
        public int CurrentFrame { get; private set; }
        public SpriteRenderer Visual => visual;
        public float DeathDuration => .8f;

        public void Configure(PlayerAnimationLibrary clips, SpriteRenderer renderer)
        {
            library = clips;
            visual = renderer;
            // Prewarm all frames once in the shared library, not during combat.
            foreach (var clip in library.Clips)
                for (int row = 0; row < clip.Directions; row++)
                    for (int frame = 0; frame < clip.Frames; frame++) library.Frame(clip, row, frame);
            ResetState();
        }

        public void ResetState()
        {
            dead = false;
            direction = 0;
            if (visual != null) visual.flipX = false;
            previousPosition = transform.position;
            actionUntil = 0;
            SetState("Idle", true);
            RenderFrame();
        }

        public void Face(Vector2 facing)
        {
            if (dead || visual == null || facing.sqrMagnitude < .0001f) return;
            direction = Mathf.Abs(facing.x) > Mathf.Abs(facing.y) ? 2 : facing.y > 0 ? 1 : 0;
            visual.flipX = direction == 2 && facing.x < 0;
        }

        public void PlayAttack(Vector2 facing, float duration)
        {
            Face(facing);
            PlayAction("Attack", duration);
        }

        public void PlayHurt() { if (!dead) PlayAction("Damage", .24f); }
        public void PlayDeath() { dead = true; PlayAction("Dead", .5f); }
        public void CancelAttack()
        {
            if (StateName != "Attack") return;
            actionUntil = 0;
            SetState("Idle", true);
        }

        private void PlayAction(string state, float duration)
        {
            actionDuration = duration;
            actionUntil = Time.time + duration;
            SetState(state, true);
            RenderFrame();
        }

        private void SetState(string state, bool restart = false)
        {
            if (!restart && StateName == state) return;
            StateName = state;
            startedAt = Time.time;
        }

        private void LateUpdate()
        {
            Vector2 movement = transform.position - previousPosition;
            previousPosition = transform.position;
            if (!dead && Time.time >= actionUntil)
            {
                bool moving = movement.sqrMagnitude > .000001f;
                if (moving) Face(movement);
                SetState(moving ? "Walk" : "Idle");
            }
            RenderFrame();
        }

        private void RenderFrame()
        {
            if (library == null || visual == null) return;
            var clip = library.Find(StateName);
            if (clip == null) return;
            float elapsed = Time.time - startedAt;
            CurrentFrame = clip.Loop ? (int)(elapsed * clip.FramesPerSecond) % clip.Frames :
                Mathf.Min(clip.Frames - 1, (int)(elapsed / Mathf.Max(.01f, actionDuration) * clip.Frames));
            visual.sprite = library.Frame(clip, direction, CurrentFrame);
        }
    }
}
