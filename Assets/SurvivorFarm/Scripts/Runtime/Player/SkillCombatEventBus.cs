using System;
using System.Collections.Generic;
using UnityEngine;
using SurvivorFarm.Runtime.Gameplay;

namespace SurvivorFarm.Runtime.Player
{
    public readonly struct SkillEventContext
    {
        public readonly GameObject Source;
        public readonly GameObject Target;
        public readonly Vector2 Position;
        public readonly Vector2 Direction;
        public readonly int Amount;
        public readonly FarmTool Tool;
        public readonly bool Charged;
        public readonly bool Heavy;
        public readonly bool Critical;
        public readonly int ChainDepth;
        /// <summary>Skill whose effect caused this event, or null for a normal player action.</summary>
        public readonly string SkillId;

        public SkillEventContext(GameObject source, GameObject target, Vector2 position, int amount,
            FarmTool tool = FarmTool.Sword, bool charged = false, bool heavy = false, bool critical = false,
            string skillId = null, Vector2 direction = default, int chainDepth = 0)
        {
            Source = source;
            Target = target;
            Position = position;
            Direction = direction;
            Amount = amount;
            Tool = tool;
            Charged = charged;
            Heavy = heavy;
            Critical = critical;
            SkillId = skillId;
            ChainDepth = chainDepth;
        }

        internal SkillEventContext AtDepth(int depth) => new SkillEventContext(Source, Target, Position,
            Amount, Tool, Charged, Heavy, Critical, SkillId, Direction, depth);
    }

    /// <summary>
    /// Per-player combat event hub. Events raised by skill damage are queued, so a kill
    /// reaction never re-enters the target collection that caused it. Both nesting depth
    /// and the total number of events in one chain have hard limits.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkillCombatEventBus : MonoBehaviour
    {
        public const int MaxChainDepth = 8;
        public const int MaxEventsPerChain = 96;

        private readonly struct PendingDispatch
        {
            public readonly Action<SkillEventContext> Listeners;
            public readonly SkillEventContext Context;
            public readonly int Depth;

            public PendingDispatch(Action<SkillEventContext> listeners, SkillEventContext context, int depth)
            {
                Listeners = listeners;
                Context = context;
                Depth = depth;
            }
        }

        private readonly Queue<PendingDispatch> pending = new Queue<PendingDispatch>(MaxEventsPerChain);
        private int batchDepth;
        private bool dispatching;
        private int processedEvents;
        private int manuallyEnteredDepth;

        public int ChainDepth { get; private set; }
        public event Action<SkillEventContext> OnAttack;
        public event Action<SkillEventContext> OnHit;
        public event Action<SkillEventContext> OnCriticalHit;
        public event Action<SkillEventContext> OnDash;
        public event Action<SkillEventContext> OnDamageTaken;
        public event Action<SkillEventContext> OnEnemyKilled;
        public event Action<SkillEventContext> OnEnemyExploded;
        public event Action<SkillEventContext> OnSkillUsed;
        public event Action<SkillEventContext> OnSkillUnlocked;
        public event Action<SkillEventContext> OnPlayerDeath;
        public event Action<SkillEventContext> OnPlayerRevived;

        public bool TryEnterChain()
        {
            if (ChainDepth >= MaxChainDepth) return false;
            ChainDepth++;
            manuallyEnteredDepth++;
            return true;
        }

        public void ExitChain()
        {
            if (manuallyEnteredDepth <= 0) return;
            manuallyEnteredDepth--;
            if (ChainDepth > 0) ChainDepth--;
        }

        public BatchScope BeginBatch()
        {
            batchDepth++;
            return new BatchScope(this);
        }

        public void RaiseAttack(SkillEventContext context) => Dispatch(OnAttack, context);
        public void RaiseHit(SkillEventContext context)
        {
            using (BeginBatch())
            {
                Dispatch(OnHit, context);
                if (context.Critical) Dispatch(OnCriticalHit, context);
            }
        }
        public void RaiseDash(SkillEventContext context) => Dispatch(OnDash, context);
        public void RaiseDamageTaken(SkillEventContext context) => Dispatch(OnDamageTaken, context);
        public void RaiseEnemyKilled(SkillEventContext context) => Dispatch(OnEnemyKilled, context);
        public void RaiseEnemyExploded(SkillEventContext context) => Dispatch(OnEnemyExploded, context);
        public void RaiseSkillUsed(SkillEventContext context) => Dispatch(OnSkillUsed, context);
        public void RaiseSkillUnlocked(SkillEventContext context) => Dispatch(OnSkillUnlocked, context);
        public void RaisePlayerDeath(SkillEventContext context) => Dispatch(OnPlayerDeath, context);
        public void RaisePlayerRevived(SkillEventContext context) => Dispatch(OnPlayerRevived, context);

        private void Dispatch(Action<SkillEventContext> listeners, SkillEventContext context)
        {
            if (listeners == null) return;
            int depth = ChainDepth + 1;
            if (depth > MaxChainDepth || pending.Count + processedEvents >= MaxEventsPerChain) return;
            pending.Enqueue(new PendingDispatch(listeners, context.AtDepth(depth), depth));
            if (!dispatching && batchDepth == 0) Drain();
        }

        private void Drain()
        {
            if (dispatching || batchDepth > 0 || pending.Count == 0) return;
            dispatching = true;
            processedEvents = 0;
            int previousDepth = ChainDepth;
            try
            {
                while (pending.Count > 0 && processedEvents < MaxEventsPerChain)
                {
                    PendingDispatch next = pending.Dequeue();
                    ChainDepth = next.Depth;
                    processedEvents++;
                    next.Listeners?.Invoke(next.Context);
                }
            }
            finally
            {
                // Discard the remainder if a chain exceeds its per-dispatch budget.
                pending.Clear();
                ChainDepth = previousDepth;
                dispatching = false;
                processedEvents = 0;
            }
        }

        private void EndBatch()
        {
            if (batchDepth > 0) batchDepth--;
            if (batchDepth == 0 && !dispatching) Drain();
        }

        public readonly struct BatchScope : IDisposable
        {
            private readonly SkillCombatEventBus owner;
            public BatchScope(SkillCombatEventBus owner) { this.owner = owner; }
            public void Dispose()
            {
                owner?.EndBatch();
            }
        }
    }
}
