using UnityEngine;
namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class BasicEnemyAI : EnemyAIBase
    {
        float retreatUntil;
        public override void ActivateFromPool(Vector3 position)
        {
            if(EnemyName=="Golem")ConfigureStats("Golem",16,2,1.1f,.85f,1.8f,12);
            base.ActivateFromPool(position);retreatUntil=0;
        }
        protected override void TickEnemy()
        {
            // Bats retreat after an attack, then approach again; slimes/golems pursue.
            if(EnemyName=="Murcielago" && Time.time<retreatUntil && Target!=null)
            {
                Vector3 direction=(transform.position-Target.position).normalized;
                Vector3 next=transform.position+direction*1.7f*Time.deltaTime;
                bool blocked=false;
                foreach(var hit in Physics2D.LinecastAll(transform.position,next))
                    if(!hit.collider.isTrigger&&!hit.transform.IsChildOf(transform))blocked=true;
                if(!blocked)transform.position=next;
                return;
            }
            base.TickEnemy();
        }
        protected override void AttackTarget(SurvivorFarm.Runtime.Player.PlayerSurvivalStats stats)
        {base.AttackTarget(stats);if(EnemyName=="Murcielago")retreatUntil=Time.time+1.1f;}
        protected override void OnDefeated(SurvivorFarm.Runtime.Player.PlayerInventory inventory)
        {base.OnDefeated(inventory);if(EnemyName=="Golem"&&inventory!=null){inventory.AddEquipment("Gem");inventory.AddEquipment("EarthElement");inventory.AddItem("EmeraldShard",1);inventory.AddItem("Emerald",1);inventory.AddItem("GoldOre",2);inventory.AddItem("EarthEssence",1);if(Random.value<.35f)inventory.AddItem("Diamond",1);inventory.GetComponent<SurvivorFarm.Runtime.Player.AdventureProgress>()?.DefeatGolem();SurvivorFarm.Runtime.UI.FarmNotificationCenter.Show("Golem derrotado: gema azul, elemento tierra, esmeralda y oro raro disponibles.");}}
    }
}
