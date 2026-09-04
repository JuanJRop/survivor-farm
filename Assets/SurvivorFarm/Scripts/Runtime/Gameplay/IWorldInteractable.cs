using UnityEngine;
using SurvivorFarm.Runtime.Player;

namespace SurvivorFarm.Runtime.Gameplay
{
    public interface IWorldInteractable
    {
        Transform Transform { get; }
        bool IsAvailable { get; }

        string GetInteractionLabel(FarmTool selectedTool);
        void SetHighlighted(bool highlighted);
        void Interact(FarmTool selectedTool, PlayerInventory inventory);
    }
}
