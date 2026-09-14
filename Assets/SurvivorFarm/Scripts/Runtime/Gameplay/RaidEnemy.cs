using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Reuses base windups, retreating archer, collisions, damage, animation and recycling.</summary>
    public sealed class RaidEnemy : EnemyAIBase
    {
        private PortfolioSession session;
        private Transform player;
        private FarmDefense defense;
        private float nextTargetChoice;
        private RaidRole role;
        public RaidRole Role => role;
        public void ConfigureRaid(PortfolioSession owner, RaidRole archetype, EnemyProjectilePool projectiles)
        {
            session=owner;player=owner.Player.transform;role=archetype;
            Configure(player,null);
            EnemyRoster.Configure(this,role==RaidRole.Archer?EnemyCombatStyle.ArcherGoblin:EnemyCombatStyle.SpearGoblin,projectiles);
            ConfigureStats(role==RaidRole.Brute?"Demoledor":role==RaidRole.Archer?"Arquero":"Rastreador",
                role==RaidRole.Brute?12:role==RaidRole.Archer?5:4,role==RaidRole.Brute?2:1,
                role==RaidRole.Brute?1.1f:role==RaidRole.Archer?1.65f:2.2f,
                role==RaidRole.Archer?4.8f:.95f,role==RaidRole.Brute?2f:1.6f,0);
            var art=GetComponentInChildren<SpriteRenderer>();
            if(art!=null)
            {
                if(role==RaidRole.Brute) art.transform.localScale=Vector3.one*1.35f;
                ConfigureVisuals(art,null,role==RaidRole.Brute?new Color(1,.72f,.5f):Color.white,role.ToString());
            }
            defense=null;nextTargetChoice=0;
        }
        protected override void TickEnemy()
        {
            if(session==null||!session.InCombat){CancelAttack();return;}
            if(Time.time>=nextTargetChoice&&!IsPreparingAttack)
            {
                nextTargetChoice=Time.time+.5f;
                ChooseTarget();
            }
            base.TickEnemy();
        }
        private void ChooseTarget()
        {
            defense=null;
            Vector3 destination=role==RaidRole.Brute?session.Core.transform.position:player.position;
            if(role!=RaidRole.Archer)
            {
                float nearest=float.PositiveInfinity;
                foreach(var candidate in FarmDefense.All)
                {
                    if(!candidate.IsAlive||candidate.Kind=="Trap")continue;
                    float distance=Vector2.Distance(transform.position,candidate.transform.position);
                    bool onRoute=Vector2.Distance(candidate.transform.position,destination)<Vector2.Distance(transform.position,destination)+.5f;
                    if((role==RaidRole.Brute||distance<2f&&onRoute)&&distance<nearest){defense=candidate;nearest=distance;}
                }
            }
            Transform desired=defense!=null?defense.transform:player;
            if(Target!=desired){CancelAttack();Configure(desired,null);}
        }
        protected override void AttackTarget(PlayerSurvivalStats stats)
        {
            if(defense!=null&&defense.IsAlive)defense.TakeDamage(role==RaidRole.Brute?2:1,null);
            else base.AttackTarget(stats);
        }
        protected override void MoveTowardTarget()
        {
            Vector2 direction=session.Navigation.NextDirection(transform.position,Target.position);
            MoveInDirection(direction);
        }
        protected override bool CanMoveTo(Vector2 p) => Mathf.Abs(p.x)<18f&&p.y>-11f&&p.y<8f;
        protected override void OnDefeated(PlayerInventory inventory)
        {
            FarmGameEvents.RaiseEnemyDefeated();
            inventory?.GetComponent<GameFeelFeedback>()?.Pulse("Eliminado",transform.position);
        }
    }
}
