using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Village raids prioritize residents and homes while the nearby player can draw aggro.</summary>
    public sealed class RaidEnemy : EnemyAIBase
    {
        private PortfolioSession session;
        private Transform player;
        private VillageResidentHealth resident;
        private VillageHouseHealth house;
        private float nextTargetChoice;
        private RaidRole role;
        private int assignment;
        public RaidRole Role => role;
        public Transform SelectedTarget=>Target;
        public bool AssignedToPlayer=>RaidTargetPolicy.TargetsPlayer(assignment);
        public string TargetKind=>house!=null?"House":resident!=null?"Civilian":"Player";
        public override bool IsElite => role == RaidRole.Brute;
        protected override float KnockbackResistance => role == RaidRole.Brute ? .65f : 1f;
        public override bool CanLaunchProjectile=>IsAlive&&Target!=null&&Target.gameObject.activeInHierarchy&&
            (house!=null?house.IsAlive:resident!=null?resident.IsAlive:Target.GetComponent<PlayerSurvivalStats>()?.CurrentHealth>0);

        public void ConfigureRaid(PortfolioSession owner,RaidRole archetype,EnemyProjectilePool projectiles,int assignmentIndex=0,EnemyCombatStyle style=EnemyCombatStyle.Legacy)
        {
            session=owner;player=owner.Player.transform;role=archetype;assignment=assignmentIndex;
            Configure(player,null);
            if(style==EnemyCombatStyle.Legacy)style=role==RaidRole.Archer?EnemyCombatStyle.ArcherGoblin:EnemyCombatStyle.SpearGoblin;
            EnemyRoster.Configure(this,style,projectiles);
            string displayName=EnemyRoster.IsTinyRpg(style)?EnemyRoster.DisplayName(style):role==RaidRole.Brute?"Demoledor":role==RaidRole.Archer?"Arquero":"Rastreador";
            ConfigureStats(displayName,
                role==RaidRole.Brute?12:role==RaidRole.Archer?5:4,role==RaidRole.Brute?2:1,
                role==RaidRole.Brute?1.4f:role==RaidRole.Archer?1.8f:2.3f,
                role==RaidRole.Archer?4.8f:style==EnemyCombatStyle.SpearGoblin?1.3f:1.15f,role==RaidRole.Brute?1.8f:1.35f,role==RaidRole.Brute?6:3);
            var art=GetComponentInChildren<SpriteRenderer>();
            if(art!=null)
            {
                art.transform.localScale=Vector3.one*(role==RaidRole.Brute&&!EnemyRoster.IsTinyRpg(style)?1.35f:1f);
                ConfigureVisuals(art,null,role==RaidRole.Brute&&!EnemyRoster.IsTinyRpg(style)?new Color(1,.72f,.5f):Color.white,displayName);
            }
            name=displayName;
            resident=null;house=null;nextTargetChoice=0;
            if(GetComponent<WorldHealthReadout>()==null)gameObject.AddComponent<WorldHealthReadout>();
        }
        protected override void RefreshTarget()
        {
            if(session==null||!session.InCombat)return;
            bool invalid=Target==null||!Target.gameObject.activeInHierarchy||
                resident!=null&&!resident.IsAlive||house!=null&&!house.IsAlive;
            if(invalid||Time.time>=nextTargetChoice&&!IsPreparingAttack)
            {nextTargetChoice=Time.time+.5f;ChooseTarget();}
        }
        protected override void TickEnemy()
        {
            if(session==null||!session.InCombat){CancelAttack();return;}
            base.TickEnemy();
        }
        public override void TakeDamage(int amount, PlayerInventory source)
        {
            base.TakeDamage(amount, source);
            if (!IsAlive || amount <= 0 || source == null || source.transform != player) return;
            resident = null; house = null; CancelAttack(); Configure(player, null);
            nextTargetChoice = 0;
        }
        public void ChooseTarget()
        {
            if(session==null||player==null)return;
            if(session.Practice!=null&&session.Practice.Mode==PracticeMode.Combat||session.Security!=null&&session.Security.IsOccupied)
            {
                resident=null;house=null;
                if(Target!=player){CancelAttack();Configure(player,null);}
                return;
            }
            float playerDistance=Vector2.Distance(transform.position,player.position);
            if (IsProvoked && playerDistance > 18f) CalmDown();
            bool playerAlive = player.GetComponent<PlayerSurvivalStats>()?.CurrentHealth > 0;
            bool intercept = playerAlive && (playerDistance < 3f || IsProvoked || AssignedToPlayer && playerDistance < 7f);
            if (intercept)
            {
                resident = null; house = null;
                if (Target != player) { CancelAttack(); Configure(player, null); }
                return;
            }
            // Commit to a fleeing resident instead of retargeting the nearest building every half second.
            if (resident != null && resident.IsAlive && Vector2.Distance(transform.position, resident.transform.position) < 14f) return;
            resident=null;house=null;
            float closestPerson=float.PositiveInfinity,closestHouse=float.PositiveInfinity;
            foreach(var person in VillageResidentHealth.All)
            {
                if(!person.IsAlive)continue;
                float distance=Vector2.Distance(transform.position,person.transform.position);
                if(distance<closestPerson){closestPerson=distance;resident=person;}
            }
            foreach(var home in VillageHouseHealth.All)
            {
                if(!home.IsAlive)continue;
                float distance=Vector2.Distance(transform.position,home.ContactPoint(transform.position));
                if(distance<closestHouse){closestHouse=distance;house=home;}
            }
            // Raiders threaten the settlement; a nearby defender can pull part of the wave away.
            Transform desired=player;
            if(house!=null&&(resident==null||closestHouse<(role==RaidRole.Brute?closestPerson*1.6f:closestPerson*.8f)))
            {desired=house.transform;resident=null;}
            else if(resident!=null){desired=resident.transform;house=null;}
            else if(house!=null)desired=house.transform;
            if(Target!=desired){CancelAttack();Configure(desired,null);}
        }
        public void ProvokeByDefender(VillageResidentHealth defender)
        {
            if (!IsAlive || defender == null || !defender.IsAlive || IsProvoked ||
                player != null && Vector2.Distance(transform.position, player.position) < 3f) return;
            resident = defender; house = null; CancelAttack(); Configure(defender.transform, null);
            nextTargetChoice = Time.time + .5f;
        }
        protected override void AttackTarget(PlayerSurvivalStats stats)
        {
            // Archers use swept projectiles for villagers AND structures; cover can intercept them.
            if(role==RaidRole.Archer){base.AttackTarget(stats);return;}
            if(house!=null&&house.IsAlive)house.TakeDamage(role==RaidRole.Brute?2:1,null);
            else if(resident!=null&&resident.IsAlive)resident.TakeDamage(role==RaidRole.Brute?2:1,null);
            else base.AttackTarget(stats);
        }
        protected override Vector2 AttackPoint=>house!=null&&house.IsAlive?house.ContactPoint(transform.position):base.AttackPoint;
        protected override void MoveTowardTarget()
        {
            Vector2 goal=Target.position;
            if(house!=null)
            {
                goal=AttackPoint;
                goal+=((Vector2)transform.position-goal).normalized*.38f;
            }
            MoveInDirection(session.Navigation.NextDirection(transform.position,goal));
        }
        protected override bool CanMoveTo(Vector2 p) => World.FarmExploration.Contains(p,.5f);
        protected override bool CanApplyKnockback(Vector2 p)=>CanMoveTo(p);
        protected override void OnDefeated(PlayerInventory inventory)
        {
            DropLoot();
            FarmGameEvents.RaiseEnemyDefeated();
        }
    }
}
