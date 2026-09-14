using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class VillageBuildingService : WorldInteractable
    {
        public string LotId;
        public ValleyCampaign Campaign;
        protected override float HighlightScale => 1f;
        public override string GetInteractionLabel(FarmTool tool) => LotId switch
        {
            "nico" => "Armeria de Nico",
            "rolo" => "Carniceria y provisiones",
            "dalia" => "Provisiones de Dalia",
            _ => "Hablar con Mara"
        };

        public override void Interact(FarmTool tool, PlayerInventory inventory)
        {
            if (Campaign == null || inventory != Campaign.Inventory || Vector2.Distance(inventory.transform.position, transform.position) > 2f) return;
            if (LotId == "nico")
            {
                if (!Campaign.WorkshopRestored) { Campaign.TalkToVillager("village:blacksmith", transform); return; }
                var window = FindFirstObjectByType<CraftingWindow>();
                window?.Open(); window?.SelectCategory("Equipment");
            }
            else if (LotId == "rolo") FindFirstObjectByType<SimpleShopSystem>()?.OpenService("Food");
            else if (LotId == "dalia") FindFirstObjectByType<SimpleShopSystem>()?.OpenService("Food");
            else Campaign.TalkToVillager("village:elder", transform);
        }
    }
}
