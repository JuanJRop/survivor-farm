using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    /// <summary>
    /// Plays skill feedback with a small reusable pool. It listens to the existing
    /// skill event bus, so only learned effects that actually trigger are displayed. The
    /// same art is reused by menu previews, attacks, dashes and kill reactions.
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
            events.OnDamageTaken += OnDamageTaken;
        }

        private void OnDestroy()
        {
            if (slots != null)
                for (int i = 0; i < created; i++)
                    if (slots[i]?.Root != null) Destroy(slots[i].Root);
            if (manager == null || manager.Events == null) return;
            manager.Events.OnSkillUsed -= OnSkillUsed;
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

            float now = Time.time;
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
            if (manager == null || manager.GetLevel(context.SkillId) <= 0) return;
            SkillFxLibrary.Clip clip = SkillFxLibrary.ForSkill(context.SkillId);
            if (clip == null) return;
            SkillDefinition definition = SkillTreeCatalog.Find(context.SkillId);
            float scale = 1.05f;
            if (definition != null && definition.Radius > 0 && clip.At(0) != null)
            {
                Vector3 extents = clip.At(0).bounds.extents;
                scale = definition.Radius / Mathf.Max(.01f, Mathf.Max(extents.x, extents.y) * clip.Scale);
            }
            Play(clip, context.Position, scale);
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
            slot.StartedAt = Time.time;
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
