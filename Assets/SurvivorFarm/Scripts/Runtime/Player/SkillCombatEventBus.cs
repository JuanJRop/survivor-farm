using System;
using UnityEngine;
using SurvivorFarm.Runtime.Gameplay;

namespace SurvivorFarm.Runtime.Player
{
    public readonly struct SkillEventContext
    {
        public readonly GameObject Source;
        public readonly GameObject Target;
        public readonly Vector2 Position;
        public readonly int Amount;
        public readonly FarmTool Tool;
        public readonly bool Charged;
        public readonly bool Heavy;
        public readonly bool Critical;
        /// <summary>Identifier of the skill that caused the event, when the event is an unlock/use event.</summary>
        public readonly string SkillId;

        public SkillEventContext(GameObject source, GameObject target, Vector2 position, int amount,
            FarmTool tool = FarmTool.Sword, bool charged = false, bool heavy = false, bool critical = false,
            string skillId = null)
        {
            Source = source;
            Target = target;
            Position = position;
            Amount = amount;
            Tool = tool;
            Charged = charged;
            Heavy = heavy;
            Critical = critical;
            SkillId = skillId;
        }
    }

    /// <summary>Per-player combat event hub. ChainDepth makes skill synergies finite.</summary>
    [DisallowMultipleComponent]
    public sealed class SkillCombatEventBus : MonoBehaviour
    {
        public const int MaxChainDepth = 8;
        public int ChainDepth { get; private set; }
        public event Action<SkillEventContext> OnAttack;
        public event Action<SkillEventContext> OnHit;
        public event Action<SkillEventContext> OnCriticalHit;
        public event Action<SkillEventContext> OnDash;
        public event Action<SkillEventContext> OnDamageTaken;
        public event Action<SkillEventContext> OnEnemyKilled;
        public event Action<SkillEventContext> OnEnemyExploded;
        public event Action<SkillEventContext> OnSkillUsed;
        public event Action<SkillEventContext> OnPlayerDeath;
        public event Action<SkillEventContext> OnPlayerRevived;

        public bool TryEnterChain()
        {
            if (ChainDepth >= MaxChainDepth) return false;
            ChainDepth++;
            return true;
        }

        public void ExitChain() { if (ChainDepth > 0) ChainDepth--; }

        public void RaiseAttack(SkillEventContext context) => Dispatch(OnAttack, context);
        public void RaiseHit(SkillEventContext context)
        {
            Dispatch(OnHit, context);
            if (context.Critical) Dispatch(OnCriticalHit, context);
        }
        public void RaiseDash(SkillEventContext context) => Dispatch(OnDash, context);
        public void RaiseDamageTaken(SkillEventContext context) => Dispatch(OnDamageTaken, context);
        public void RaiseEnemyKilled(SkillEventContext context) => Dispatch(OnEnemyKilled, context);
        public void RaiseEnemyExploded(SkillEventContext context) => Dispatch(OnEnemyExploded, context);
        public void RaiseSkillUsed(SkillEventContext context) => Dispatch(OnSkillUsed, context);
        public void RaisePlayerDeath(SkillEventContext context) => Dispatch(OnPlayerDeath, context);
        public void RaisePlayerRevived(SkillEventContext context) => Dispatch(OnPlayerRevived, context);

        private void Dispatch(Action<SkillEventContext> listeners, SkillEventContext context)
        {
            if (listeners == null || !TryEnterChain()) return;
            try { listeners.Invoke(context); }
            finally { ExitChain(); }
        }
    }
}
