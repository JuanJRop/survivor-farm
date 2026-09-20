using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>The tutorial reuses real hurt, knockback and death animation, without
    /// attacks, loot, raid counters or mastery farming.</summary>
    public sealed class TrainingEnemy : EnemyAIBase
    {
        public override bool CanLaunchProjectile=>false;
        public void ConfigurePractice(PlayerInventory player)
        {
            Configure(player.transform,null);
            EnemyRoster.Configure(this,EnemyCombatStyle.SpearGoblin,null);
            ConfigureStats("Práctica",40,0,2f,.6f,10,0);
            ConfigureVisuals(GetComponentInChildren<SpriteRenderer>(),null,new Color(.75f,.9f,1),"Práctica");
        }
        protected override void TickEnemy()
        {
            if(Target!=null&&Vector2.Distance(transform.position,Target.position)>1.05f)
                MoveInDirection(Target.position-transform.position);
        }
        protected override bool CanApplyKnockback(Vector2 p)=>World.FarmExploration.Contains(p,.5f);
        protected override void OnDefeated(PlayerInventory inventory) { }
    }
}
