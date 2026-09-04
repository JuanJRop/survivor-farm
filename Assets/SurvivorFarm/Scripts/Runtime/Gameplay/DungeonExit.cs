using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class DungeonExit : MonoBehaviour, IWorldInteractable
    {
        [SerializeField] private DungeonEntrance entrance;

        public Transform Transform => transform;
        public bool IsAvailable => entrance != null && entrance.IsInsideDungeon;

        public void Configure(DungeonEntrance dungeonEntrance)
        {
            entrance = dungeonEntrance;
        }

        public string GetInteractionLabel(FarmTool selectedTool)
        {
            return "Interactuar: salir de mazmorra";
        }

        public void SetHighlighted(bool highlighted)
        {
            transform.localScale = highlighted ? Vector3.one * 1.12f : Vector3.one;
        }

        public void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            entrance?.ExitDungeon();
        }
    }
}
