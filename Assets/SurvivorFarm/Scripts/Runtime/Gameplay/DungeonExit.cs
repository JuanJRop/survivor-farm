using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class DungeonExit : WorldInteractable
    {
        [SerializeField] private DungeonEntrance entrance;

        public override bool IsAvailable => entrance != null && entrance.IsInsideDungeon;

        protected override float HighlightScale => 1.12f;

        public void Configure(DungeonEntrance dungeonEntrance)
        {
            entrance = dungeonEntrance;
        }

        public override string GetInteractionLabel(FarmTool selectedTool)
        {
            return "Interactuar: salir de mazmorra";
        }

        public override void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            entrance?.ExitDungeon();
        }
    }
}
