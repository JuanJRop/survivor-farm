using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    /// <summary>
    /// Plays skill feedback with a small reusable pool. It listens to the existing
    /// skill event bus, so unlocking a node immediately previews its effect and the
    /// same art is reused during attacks, dashes and kill reactions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkillFxRuntime : MonoBehaviour
    {
        private const int MaximumSlots = 12;

        private sealed class Slot
        {
            public GameObject Root;
            public SpriteRenderer Renderer;
            public SkillFxLibrary.Clip Clip;
            public float StartedAt;
            public float Duration;
            public bool Active;

            public void Stop()
            {
                Active = false;
                Clip = null;
                if (Renderer != null) Renderer.enabled = false;
                if (Root != null) Root.SetActive(false);
            }
        }

        private SkillTreeManager manager;
        private Slot[] slots;
        private int created;

        private void Awake()
        {
            manager = GetComponent<SkillTreeManager>() ?? gameObject.AddComponent<SkillTreeManager>();
            slots = new Slot[MaximumSlots];
            SkillCombatEventBus events = manager.Events;
            events.OnSkillUsed += OnSkillUsed;
            events.OnAttack += OnAttack;
            events.OnHit += OnHit;
            events.OnDash += OnDash;
            events.OnEnemyKilled += OnEnemyKilled;
            events.OnDamageTaken += OnDamageTaken;
        }

        private void OnDestroy()
        {
            if (manager == null || manager.Events == null) return;
            manager.Events.OnSkillUsed -= OnSkillUsed;
            manager.Events.OnAttack -= OnAttack;
            manager.Events.OnHit -= OnHit;
            manager.Events.OnDash -= OnDash;
            manager.Events.OnEnemyKilled -= OnEnemyKilled;
            manager.Events.OnDamageTaken -= OnDamageTaken;
        }

        private void Update()
        {
            if (slots == null) return;
            if (!GameFeelFeedback.Enabled)
            {
                for (int i = 0; i < created; i++) if (slots[i] != null && slots[i].Active) slots[i].Stop();
                return;
            }

            float now = Time.unscaledTime;
            for (int i = 0; i < created; i++)
            {
                Slot slot = slots[i];
                if (slot == null || !slot.Active || slot.Clip == null) continue;
                float age = now - slot.StartedAt;
                if (age >= slot.Duration)
                {
                    slot.Stop();
                    continue;
                }
                slot.Renderer.sprite = slot.Clip.At(age / Mathf.Max(.01f, slot.Duration));
            }
        }

        private void OnSkillUsed(SkillEventContext context)
        {
            SkillFxLibrary.Clip clip = SkillFxLibrary.ForSkill(context.SkillId);
            if (clip != null) Play(clip, transform.position + Vector3.up * .35f, 1.05f);
        }

        private void OnAttack(SkillEventContext context)
        {
            if (context.Tool != FarmTool.Bow && !context.Charged && !context.Heavy) return;
            SkillFxLibrary.Clip clip = SkillFxLibrary.ForAttack(context.Tool, context.Charged, context.Heavy);
            if (clip != null) Play(clip, transform.position + Vector3.up * .15f, .85f);
        }

        private void OnHit(SkillEventContext context)
        {
            if (context.Target == null) return;
            SkillFxLibrary.Clip clip = SkillFxLibrary.ForHit(manager, context);
            if (clip != null) Play(clip, context.Position, context.Charged ? 1.0f : .72f);
        }

        private void OnDash(SkillEventContext context)
        {
            SkillFxLibrary.Clip clip = SkillFxLibrary.ForDash(manager);
            if (clip != null) Play(clip, transform.position, .80f);
        }

        private void OnEnemyKilled(SkillEventContext context)
        {
            SkillFxLibrary.Clip clip = SkillFxLibrary.ForEnemyKilled(manager);
            if (clip != null) Play(clip, context.Position, .90f);
        }

        private void OnDamageTaken(SkillEventContext context)
        {
            SkillFxLibrary.Clip clip = SkillFxLibrary.ForDamageTaken();
            if (clip != null) Play(clip, transform.position, .52f);
        }

        private void Play(SkillFxLibrary.Clip clip, Vector3 position, float scale)
        {
            if (clip == null || clip.FrameCount == 0 || !GameFeelFeedback.Enabled) return;
            Slot slot = Rent();
            if (slot == null) return;
            slot.Clip = clip;
            slot.StartedAt = Time.unscaledTime;
            slot.Duration = Mathf.Max(.04f, clip.Duration);
            slot.Active = true;
            slot.Root.transform.position = position;
            slot.Root.transform.localScale = Vector3.one * (clip.Scale * scale);
            slot.Renderer.sortingOrder = clip.SortingOrder;
            slot.Renderer.color = Color.white;
            slot.Renderer.sprite = clip.At(0f);
            slot.Renderer.enabled = slot.Renderer.sprite != null;
            slot.Root.SetActive(true);
        }

        private Slot Rent()
        {
            for (int i = 0; i < created; i++)
            {
                if (slots[i] != null && !slots[i].Active) return slots[i];
            }
            if (created < MaximumSlots)
            {
                Slot createdSlot = CreateSlot(created++);
                slots[created - 1] = createdSlot;
                return createdSlot;
            }

            // Replace the oldest visual when the bounded pool is busy. This keeps
            // combat readable and guarantees a hard upper bound on GameObjects.
            int oldest = 0;
            float oldestTime = float.MaxValue;
            for (int i = 0; i < created; i++)
            {
                if (slots[i] == null) continue;
                if (slots[i].StartedAt < oldestTime) { oldest = i; oldestTime = slots[i].StartedAt; }
            }
            slots[oldest].Stop();
            return slots[oldest];
        }

        private Slot CreateSlot(int index)
        {
            var root = new GameObject("Skill FX · pooled " + index);
            // Keep the visual in the world hierarchy so a hit effect remains on
            // the enemy even while the player continues moving.
            root.transform.SetParent(transform.parent, true);
            root.SetActive(false);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.maskInteraction = SpriteMaskInteraction.None;
            renderer.sortingOrder = 12100;
            return new Slot { Root = root, Renderer = renderer, Active = false };
        }
    }
}
