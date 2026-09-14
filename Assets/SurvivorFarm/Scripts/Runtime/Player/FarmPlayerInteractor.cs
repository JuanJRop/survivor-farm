using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.UI;

namespace SurvivorFarm.Runtime.Player
{
    public sealed class FarmPlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float interactionRadius = 1.35f;
        [SerializeField] private Vector3 interactionButtonWorldOffset = new Vector3(0f, 0.9f, 0f);

        private PlayerToolbelt toolbelt;
        private PlayerInventory inventory;
        private PlayerMovementController movement;
        private PlayerCharacterAnimator characterAnimator;
        private IWorldInteractable highlightedInteractable;
        private Camera mainCamera;
        private SpriteRenderer[] indicatorRenderers = new SpriteRenderer[0];

        private void Awake()
        {
            toolbelt = GetComponent<PlayerToolbelt>();
            inventory = GetComponent<PlayerInventory>();
            movement = GetComponent<PlayerMovementController>();
            characterAnimator = GetComponent<PlayerCharacterAnimator>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (InventoryPanelSystem.IsOpen || AdventureWindow.IsOpen || Time.timeScale == 0f)
            {
                FarmNotificationCenter.SetInteractionButton(false, "Interactuar");
                return;
            }
            IWorldInteractable nearest = FindNearestInteractable(transform.position, interactionRadius);
            if (!ReferenceEquals(nearest, highlightedInteractable))
            {
                if (highlightedInteractable != null)
                {
                    highlightedInteractable.SetHighlighted(false);
                }

                highlightedInteractable = nearest;
                indicatorRenderers = nearest != null ? nearest.Transform.GetComponentsInChildren<SpriteRenderer>() : new SpriteRenderer[0];

                if (highlightedInteractable != null)
                {
                    highlightedInteractable.SetHighlighted(true);
                }
            }

            FarmTool selectedTool = toolbelt != null ? toolbelt.SelectedTool : FarmTool.Sword;
            FarmTool interactionTool = highlightedInteractable != null
                ? ResolveInteractionTool(highlightedInteractable, selectedTool)
                : selectedTool;
            FarmNotificationCenter.SetPrompt(highlightedInteractable != null
                ? highlightedInteractable.GetInteractionLabel(interactionTool)
                : "Explora y reconstruye Raízclara.");
            if (mainCamera == null) mainCamera = Camera.main;
            FarmNotificationCenter.SetInteractionButtonAtWorldPosition(highlightedInteractable != null,
                highlightedInteractable != null ? highlightedInteractable.GetInteractionLabel(interactionTool).Replace("Interactuar: ", "") : "Interactuar",
                highlightedInteractable != null ? GetIndicatorPosition(highlightedInteractable) : Vector3.zero, mainCamera);
            if (highlightedInteractable != null && (Input.GetKeyDown(KeyCode.E) || (Input.GetMouseButtonDown(1) && !IsPointerOverUi(-1))))
                PerformInteraction();
        }

        private Vector3 GetIndicatorPosition(IWorldInteractable target)
        {
            Vector3 position = target.Transform.position + interactionButtonWorldOffset;
            foreach (var visual in indicatorRenderers)
                if (visual != null && visual.enabled && visual.gameObject.activeInHierarchy && visual.sprite != null) position.y = Mathf.Max(position.y, visual.bounds.max.y + 0.25f);
            return position;
        }

        private void OnDisable()
        {
            if (highlightedInteractable != null)
            {
                highlightedInteractable.SetHighlighted(false);
                highlightedInteractable = null;
            }

            FarmNotificationCenter.SetInteractionButton(false, "Interactuar");
        }

        public void PerformInteraction()
        {
            if (InventoryPanelSystem.IsOpen || Time.timeScale == 0f || highlightedInteractable == null ||
                highlightedInteractable.Transform == null || !highlightedInteractable.IsAvailable ||
                Vector2.Distance(transform.position, highlightedInteractable.Transform.position) > interactionRadius)
            {
                return;
            }

            FarmTool selectedTool = toolbelt != null ? toolbelt.SelectedTool : FarmTool.Sword;
            if (highlightedInteractable is FarmingPlot) return;
            movement?.StopMovement();
            FarmTool interactionTool = ResolveInteractionTool(highlightedInteractable, selectedTool);
            if (!Supports(highlightedInteractable, interactionTool) || AdventureWindow.IsOpen || IsPointerOverUi(-1)) return;
            if (characterAnimator != null && characterAnimator.MovementLocked) return;
            characterAnimator?.FaceWorldPosition(highlightedInteractable.Transform.position);
            highlightedInteractable.Interact(interactionTool, inventory);
            FarmNotificationCenter.PulseInteraction();
        }

        public static bool Supports(IWorldInteractable target, FarmTool tool)
        {
            if(target==null || !target.IsAvailable)return false;
            if(target is FarmingPlot)return false;
            if(target is AnimalResource)return tool==FarmTool.Sword || tool==FarmTool.Bow;
            if(target is HarvestableResource resource)return resource.SupportsTool(tool);
            if(target is ValleyInteraction story)return story.SupportsTool(tool);
            return true;
        }

        private static FarmTool ResolveInteractionTool(IWorldInteractable target, FarmTool selectedTool)
        {
            if (target is HarvestableResource resource && !(target is AnimalResource))
            {
                if (resource.SupportsTool(FarmTool.Axe)) return FarmTool.Axe;
                if (resource.SupportsTool(FarmTool.Pickaxe)) return FarmTool.Pickaxe;
            }

            if (target is ValleyInteraction story && !string.IsNullOrEmpty(story.Id))
            {
                if (story.Id.StartsWith("wood:")) return FarmTool.Axe;
                if (story.Id.StartsWith("stone:") || story.Id == "secret" || story.Id == "seal") return FarmTool.Pickaxe;
            }

            if (target is IronVein) return FarmTool.Pickaxe;

            return selectedTool;
        }
        public FarmingPlot SelectedPlot {get;private set;}
        IWorldInteractable FindNearestInteractable(Vector3 position, float radius)
        {
            FarmTool selectedTool=toolbelt!=null?toolbelt.SelectedTool:FarmTool.Sword;
            SelectedPlot=null;
            if(IsPointerOverUi(-1))return null;
            WorldInteractable[] interactables = FindObjectsByType<WorldInteractable>(FindObjectsSortMode.None);
            if(mainCamera!=null)
            {
                Vector3 pointer=mainCamera.ScreenToWorldPoint(Input.mousePosition);pointer.z=0;
                IWorldInteractable pointed = interactables
                    .Where(t=>!(t is FarmingPlot)&&Supports(t,ResolveInteractionTool(t,selectedTool))&&Vector2.Distance(position,t.Transform.position)<=radius&&Vector2.Distance(pointer,t.Transform.position)<.65f)
                    .OrderBy(t=>(t.Transform.position-pointer).sqrMagnitude).FirstOrDefault();
                if(pointed!=null)return pointed;
            }
            IWorldInteractable nearestObject = interactables
                .Where(t=>!(t is FarmingPlot)&&Supports(t,ResolveInteractionTool(t,selectedTool))&&Vector2.Distance(position,t.Transform.position)<=radius)
                .OrderBy(t=>(t.Transform.position-position).sqrMagnitude).FirstOrDefault();
            return nearestObject;
        }

        private static bool IsPointerOverUi(int pointerId)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            return pointerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(pointerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

    }
}
