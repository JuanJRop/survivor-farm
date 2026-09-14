using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class EnemyCampChest : WorldInteractable
    {
        public EnemyCamp Camp { get; set; }
        public override bool IsAvailable => Camp != null && !Camp.Claimed;
        public override string GetInteractionLabel(FarmTool selectedTool) => Camp == null ? "" :
            Camp.IsCleared ? "Recuperar suministros" : "Campamento: " + Camp.Remaining + " enemigos";
        public override void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            if (!IsAvailable || inventory == null || Vector2.Distance(inventory.transform.position, transform.position) > 1.75f) return;
            if (!Camp.IsCleared) FarmNotificationCenter.Show("Derrota a los guardias para recuperar los suministros.");
            else Camp.TryClaim(inventory);
        }
        public override void SetHighlighted(bool highlighted) { }
    }
}
