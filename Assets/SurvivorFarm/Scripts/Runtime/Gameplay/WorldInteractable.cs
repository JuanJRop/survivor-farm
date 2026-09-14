using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public abstract class WorldInteractable : MonoBehaviour, IWorldInteractable
    {
        private Vector3 defaultScale;
        private bool defaultScaleCaptured;

        public virtual Transform Transform => transform;
        public virtual bool IsAvailable => true;

        protected virtual float HighlightScale => 1.08f;

        public abstract string GetInteractionLabel(FarmTool selectedTool);
        public abstract void Interact(FarmTool selectedTool, PlayerInventory inventory);

        public virtual void SetHighlighted(bool highlighted)
        {
            if (!defaultScaleCaptured)
            {
                defaultScale = transform.localScale;
                defaultScaleCaptured = true;
            }

            transform.localScale = highlighted ? defaultScale * HighlightScale : defaultScale;
        }
    }
}
