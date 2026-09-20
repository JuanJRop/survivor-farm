using System.Collections.Generic;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public abstract class WorldInteractable : MonoBehaviour, IWorldInteractable
    {
        private static readonly List<WorldInteractable> active = new List<WorldInteractable>(512);
        public static IReadOnlyList<WorldInteractable> Active => active;
        public static int RegistryVersion { get; private set; }
        private int registryIndex = -1;
        private Vector3 defaultScale;
        private bool defaultScaleCaptured;

        public virtual Transform Transform => transform;
        public virtual bool IsAvailable => true;

        protected virtual float HighlightScale => 1.08f;

        public abstract string GetInteractionLabel(FarmTool selectedTool);
        public abstract void Interact(FarmTool selectedTool, PlayerInventory inventory);

        protected virtual void OnEnable()
        {
            if (registryIndex >= 0 && registryIndex < active.Count && active[registryIndex] == this) return;
            registryIndex = active.Count;
            active.Add(this); RegistryVersion++;
        }

        protected virtual void OnDisable()
        {
            if (registryIndex >= 0 && registryIndex < active.Count && active[registryIndex] == this)
            {
                int last = active.Count - 1;
                var moved = active[last];
                active[registryIndex] = moved;
                if (moved != null) moved.registryIndex = registryIndex;
                active.RemoveAt(last); RegistryVersion++;
            }
            registryIndex = -1;
            WorldPointerTargeting.Forget(this);
        }

        private void OnTransformChildrenChanged() => WorldPointerTargeting.Forget(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() { active.Clear(); RegistryVersion = 0; }

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
