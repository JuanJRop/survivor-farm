using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class OutdoorEnemyAI : EnemyAIBase
    {
        private OutdoorEnemyPool owner;
        protected override HomeSafeZone ProjectileProtection => owner != null ? owner.SafeZone : null;
        public override bool CanLaunchProjectile => base.CanLaunchProjectile && owner != null && owner.CanChase && !owner.SafeZone.Contains(Target.position);
        protected override bool CanApplyKnockback(Vector2 position) => owner == null || !owner.SafeZone.Contains(position, 0.3f);
        public bool CanBeAttackedByPet(Vector2 playerPosition) => owner != null && owner.CanChase && !owner.SafeZone.Contains(playerPosition);
        protected override float MovementSpeedMultiplier => owner != null ? owner.SpeedMultiplier : 1f;
        protected override float AttackIntervalMultiplier => owner != null ? owner.AttackDelayMultiplier : 1f;

        public void ConfigureOutdoor(Transform player, OutdoorEnemyPool pool)
        {
            Configure(player, null);
            owner = pool;
        }

        protected override void TickEnemy()
        {
            if (owner == null || !owner.CanChase) { CancelAttack(); return; }
            if (owner.SafeZone.Contains(transform.position, 0.3f))
            {
                ReturnToPool();
                return;
            }
            if (owner.SafeZone.Contains(Target.position)) { CancelAttack(); return; }
            if (!IsPreparingAttack && Vector2.Distance(transform.position, Target.position) > owner.DetectionRange) { CancelAttack(); return; }
            base.TickEnemy();
        }

        protected override bool CanMoveTo(Vector2 position) => owner != null && owner.CanOccupy(position, this);

        protected override void AttackTarget(SurvivorFarm.Runtime.Player.PlayerSurvivalStats stats)
        { if(owner!=null && owner.CanChase && !owner.SafeZone.Contains(Target.position))base.AttackTarget(stats); }
        protected override void TryAttackTarget()
        {
            if (!owner.SafeZone.Contains(Target.position)) base.TryAttackTarget();
        }
    }
}
