using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class CampEnemyAI : EnemyAIBase
    {
        private EnemyCamp camp;
        private Vector3 guardOffset;
        private int slot, patrolStep;
        private float nextPatrol;
        private bool wasEngaged;
        public Vector3 GuardPosition => camp.transform.position + guardOffset;
        public bool IsReturning { get; private set; }
        public bool CanBeAttackedByPet => camp != null && camp.CanEngage && !IsReturning;
        public override bool CanLaunchProjectile => base.CanLaunchProjectile && camp != null && camp.CanEngage && !IsReturning;
        protected override HomeSafeZone ProjectileProtection => camp != null ? camp.Protection : null;

        public void ConfigureCamp(EnemyCamp owner, Transform target, int index, Vector3 offset)
        {
            camp = owner; slot = index; guardOffset = offset;
            Configure(target, null);
        }

        public override void ActivateFromPool(Vector3 position)
        {
            base.ActivateFromPool(position);
            IsReturning = false;
            wasEngaged = false;
            patrolStep = slot % 2;
            nextPatrol = Time.time + 1 + slot * .4f;
        }

        protected override bool CanMoveTo(Vector2 position) => camp != null && camp.CanOccupy(position);
        protected override bool CanApplyKnockback(Vector2 position) => CanMoveTo(position);

        protected override void TickEnemy()
        {
            if (camp == null || !camp.isActiveAndEnabled) { CancelAttack(); return; }
            if (!camp.CanEngage)
            {
                CancelAttack();
                if (wasEngaged) IsReturning = true;
                wasEngaged = false;
                if (IsReturning || Vector2.Distance(transform.position, GuardPosition) > .85f) ReturnHome();
                else Patrol();
                return;
            }
            wasEngaged = true;
            if (IsReturning || Vector2.Distance(transform.position, camp.transform.position) >= EnemyCamp.LeashRadius - .1f)
            {
                CancelAttack();
                ReturnHome();
                return;
            }
            base.TickEnemy();
        }

        private void ReturnHome()
        {
            IsReturning = true;
            if (Vector2.Distance(transform.position, GuardPosition) > .15f) MoveInDirection(GuardPosition - transform.position);
            else IsReturning = false;
        }

        private void Patrol()
        {
            if (Target == null || Vector2.Distance(Target.position, camp.transform.position) > 20) return;
            if (Time.time >= nextPatrol) { patrolStep = 1 - patrolStep; nextPatrol = Time.time + 3; }
            Vector3 point = GuardPosition + Vector3.right * (patrolStep == 0 ? -.45f : .45f);
            if (Vector2.Distance(transform.position, point) > .12f) MoveInDirection(point - transform.position);
        }

        protected override void AttackTarget(PlayerSurvivalStats stats)
        {
            if (camp != null && camp.CanEngage && !IsReturning) base.AttackTarget(stats);
        }

        protected override void OnDefeated(PlayerInventory inventory)
        {
            base.OnDefeated(inventory);
            camp?.Defeated(slot);
        }
    }
}
