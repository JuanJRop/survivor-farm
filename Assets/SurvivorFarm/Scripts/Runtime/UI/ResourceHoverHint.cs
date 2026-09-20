using System.Collections.Generic;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>Tracks pointed resources without placing text over the world.</summary>
    public sealed class ResourceHoverHint : MonoBehaviour
    {
        private readonly List<WorldInteractable> resources = new List<WorldInteractable>();
        private float nextRefresh;
        private int registryVersion = -1;
        private Camera worldCamera;
        public WorldInteractable Hovered { get; private set; }

        public WorldInteractable FindAt(Vector2 point)
        {
            if (registryVersion != WorldInteractable.RegistryVersion)
            {
                registryVersion = WorldInteractable.RegistryVersion;
                resources.Clear();
                var active = WorldInteractable.Active;
                for (int i = 0; i < active.Count; i++)
                {
                    var item = active[i];
                    if (item is HarvestableResource && !(item is AnimalResource) || item is IronVein)
                        resources.Add(item);
                }
            }
            return WorldPointerTargeting.FindAt(resources, point);
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .05f;
            if (worldCamera == null) worldCamera = Camera.main;
            bool blocked = InventoryPanelSystem.IsOpen || FarmIntroduction.IsOpen || Time.timeScale <= 0 || worldCamera == null ||
                EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            Hovered = blocked ? null : FindAt(worldCamera.ScreenToWorldPoint(Input.mousePosition));
        }

        private void OnDisable() => Hovered = null;
    }
}
