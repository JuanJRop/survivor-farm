using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class BaseHouse : WorldInteractable
    {
        [SerializeField] private SpriteRenderer[] renderers = new SpriteRenderer[0];

        protected override float HighlightScale => 1.08f;

        public void Configure(params SpriteRenderer[] houseRenderers)
        {
            renderers = houseRenderers ?? new SpriteRenderer[0];
        }

        public override string GetInteractionLabel(FarmTool selectedTool)
        {
            return "Gestionar refugio";
        }

        public override void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            inventory?.GetComponent<HouseSystem>()?.OpenServices();
        }
    }
}
