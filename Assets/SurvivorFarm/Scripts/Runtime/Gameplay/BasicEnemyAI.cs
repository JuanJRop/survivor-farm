using UnityEngine;
namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class BasicEnemyAI : EnemyAIBase
    {
        float retreatUntil;
        public override void ActivateFromPool(Vector3 position)
        {
            if(EnemyName=="Golem")ConfigureStats("Golem",16,2,1.3f,1.25f,1.65f,12);
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
        {
            base.OnDefeated(inventory);
            if (EnemyName != "Golem" || inventory == null) return;
            inventory.AddEquipment("Gem"); inventory.AddEquipment("EarthElement");
            EnemyLootPickup.Scatter(transform.position, transform.parent, ItemKind.EmeraldShard, 1);
            EnemyLootPickup.Scatter(transform.position, transform.parent, ItemKind.Emerald, 1);
            EnemyLootPickup.Scatter(transform.position, transform.parent, ItemKind.GoldOre, 2);
            EnemyLootPickup.Scatter(transform.position, transform.parent, ItemKind.EarthEssence, 1);
            if (Random.value < .35f) EnemyLootPickup.Scatter(transform.position, transform.parent, ItemKind.Diamond, 1);
            inventory.GetComponent<SurvivorFarm.Runtime.Player.AdventureProgress>()?.DefeatGolem();
        }
    }
}
