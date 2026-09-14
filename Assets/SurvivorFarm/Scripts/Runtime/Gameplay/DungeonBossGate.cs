using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class DungeonBossGate : WorldInteractable
    {
        private DungeonExpedition expedition;
        public void Configure(DungeonExpedition owner) => expedition = owner;
        protected override float HighlightScale => 1;
        public override bool IsAvailable => expedition != null && !expedition.BossFightActive && !expedition.State.bossDefeated;
        public override string GetInteractionLabel(FarmTool tool) => expedition.BossDoorReady ? "Entrar: camara del Custodio" : "Derrota a los guardias de la antesala";
        public override void Interact(FarmTool tool, PlayerInventory inventory)
        {
            if (inventory != null && !expedition.BeginBoss()) FarmNotificationCenter.Show("La antesala sigue custodiada.");
        }
    }
}
