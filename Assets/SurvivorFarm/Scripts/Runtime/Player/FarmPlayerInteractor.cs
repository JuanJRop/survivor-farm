using System;
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
        private SpriteRenderer playerVisual;
        private FarmHandCursor handCursor;
        private Predicate<WorldInteractable> pointerFilter;
        private FarmTool pointerTool;

        // Interface references bypass Unity's destroyed-object null comparison during scene unload.
        private static bool Exists(IWorldInteractable value) => value != null &&
            (!(value is UnityEngine.Object native) || native != null);

        private void Awake()
        {
            toolbelt = GetComponent<PlayerToolbelt>();
            inventory = GetComponent<PlayerInventory>();
            movement = GetComponent<PlayerMovementController>();
            characterAnimator = GetComponent<PlayerCharacterAnimator>();
            mainCamera = Camera.main;
            playerVisual = GetComponent<SpriteRenderer>();
            pointerFilter = AcceptPointer;
            handCursor = GetComponent<FarmHandCursor>();
            if (handCursor == null) handCursor = gameObject.AddComponent<FarmHandCursor>();
        }

        private void Update()
        {
            if(!Exists(highlightedInteractable))highlightedInteractable=null;
            if (mainCamera == null) mainCamera = Camera.main;
            handCursor?.SetEnemyTarget(!InventoryPanelSystem.IsOpen && !VillageUpgradeWindow.IsOpen &&
                !AdventureWindow.IsOpen && !IsPointerOverUi(-1) && IsEnemyUnderPointer());
            if (InventoryPanelSystem.IsOpen || VillageUpgradeWindow.IsOpen || !FarmIntroduction.AllowsInteraction || AdventureWindow.IsOpen || Time.timeScale == 0f)
            {
                FarmNotificationCenter.SetInteractionButton(false, "Interactuar");
                return;
            }
            var mount = GetComponent<PlayerMountController>();
            if (mount != null && mount.IsMounted)
            {
                FarmNotificationCenter.SetInteractionButton(false, "E");
                if (Input.GetKeyDown(KeyCode.E)) mount.TryDismount();
                return;
            }
            var combat = GetComponent<PlayerCombatController>();
            if (combat != null && (combat.IsExecuting || Input.GetKeyDown(KeyCode.E) && combat.TryExecuteNearest()))
            { FarmNotificationCenter.SetInteractionButton(false, "E"); return; }
            IWorldInteractable nearest = FindNearestInteractable(transform.position, interactionRadius);
            if (!ReferenceEquals(nearest, highlightedInteractable))
            {
                if (highlightedInteractable != null)
                {
                    highlightedInteractable.SetHighlighted(false);
                }

                highlightedInteractable = nearest;
                indicatorRenderers = nearest != null ? nearest.Transform.GetComponentsInChildren<SpriteRenderer>() : Array.Empty<SpriteRenderer>();

                if (highlightedInteractable != null)
                {
                    highlightedInteractable.SetHighlighted(true);
                }
            }

            FarmNotificationCenter.SetPrompt(string.Empty);
            FarmNotificationCenter.SetInteractionButtonAtWorldPosition(highlightedInteractable != null,
                highlightedInteractable is VillageHouseHealth house ? house.Health < house.Maximum ? "Reparar" : "Mejorar" : "E",
                highlightedInteractable != null ? GetIndicatorPosition(highlightedInteractable) : Vector3.zero, mainCamera);
            if (highlightedInteractable != null && (Input.GetKeyDown(KeyCode.E) || (Input.GetMouseButtonDown(1) && !IsPointerOverUi(-1))))
                PerformInteraction();
        }

        private Vector3 GetIndicatorPosition(IWorldInteractable target)
        {
            bool repair = target is VillageHouseHealth;
            Vector3 position = target.Transform.position + (repair ? Vector3.up * .65f : interactionButtonWorldOffset);
            if (!repair)
                foreach (var visual in indicatorRenderers)
                    if (visual != null && visual.enabled && visual.gameObject.activeInHierarchy && visual.sprite != null) position.y = Mathf.Max(position.y, visual.bounds.max.y + 0.25f);
            if (mainCamera == null) return position;

            if (playerVisual == null) playerVisual = GetComponentInChildren<SpriteRenderer>();
            Bounds playerBounds = playerVisual != null && playerVisual.sprite != null ? playerVisual.bounds :
                new Bounds(transform.position + Vector3.up * .25f, new Vector3(.7f, 1.3f, 0f));
            Vector3 min = mainCamera.WorldToScreenPoint(playerBounds.min);
            Vector3 max = mainCamera.WorldToScreenPoint(playerBounds.max);
            Vector3 screen = mainCamera.WorldToScreenPoint(position);
            Vector2 halfBadge = FarmNotificationCenter.InteractionBadgeScreenSize(repair) * .5f;
            const float gap = 8f;
            if (screen.y + halfBadge.y >= min.y - gap && screen.y - halfBadge.y <= max.y + gap &&
                screen.x + halfBadge.x >= min.x - gap && screen.x - halfBadge.x <= max.x + gap)
            {
                // Keep the badge at the object's height, beside the player rather than over their face.
                float left = min.x - halfBadge.x - gap, right = max.x + halfBadge.x + gap;
                bool preferLeft = screen.x < (min.x + max.x) * .5f;
                if (right + halfBadge.x > mainCamera.pixelRect.xMax) preferLeft = true;
                else if (left - halfBadge.x < mainCamera.pixelRect.xMin) preferLeft = false;
                screen.x = preferLeft ? left : right;
                position = mainCamera.ScreenToWorldPoint(screen);
            }
            return position;
        }

        private void OnDisable()
        {
            if (Exists(highlightedInteractable))
            {
                highlightedInteractable.SetHighlighted(false);
            }
            highlightedInteractable = null;

            FarmNotificationCenter.SetInteractionButton(false, "Interactuar");
        }

        public void PerformInteraction()
        {
            if (VillageUpgradeWindow.IsOpen || GetComponent<PlayerMountController>()?.IsMounted == true || GetComponent<PlayerCombatController>()?.IsExecuting == true) return;
            if (InventoryPanelSystem.IsOpen || !FarmIntroduction.AllowsInteraction || Time.timeScale == 0f || !Exists(highlightedInteractable) ||
                highlightedInteractable.Transform == null || !highlightedInteractable.IsAvailable ||
                Vector2.Distance(transform.position, highlightedInteractable.Transform.position) > interactionRadius)
            {
                return;
            }

            FarmTool selectedTool = toolbelt != null ? toolbelt.SelectedTool : FarmTool.Sword;
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
            if(!Exists(target) || !target.IsAvailable)return false;
            if(target is Behaviour component && !component.isActiveAndEnabled)return false;
            if(target is FarmingPlot plot)return plot.SupportsTool(tool);
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
            var interactables=WorldInteractable.Active;
            if(mainCamera==null)mainCamera=Camera.main;
            if(mainCamera!=null)
            {
                Vector3 pointer=mainCamera.ScreenToWorldPoint(Input.mousePosition);pointer.z=0;
                pointerTool = selectedTool;
                var pointed = WorldPointerTargeting.FindAt(interactables, pointer, pointerFilter);
                // A deliberate aim must never harvest a different nearby object.
                if(pointed!=null)return Vector2.Distance(position,pointed.Transform.position)<=radius?pointed:null;
            }
            IWorldInteractable nearestObject = null;
            float bestDistance = radius * radius;
            for (int i = 0; i < interactables.Count; i++)
            {
                var candidate = interactables[i];
                if (candidate == null || !Supports(candidate, ResolveInteractionTool(candidate, selectedTool))) continue;
                float distance = ((Vector2)(candidate.Transform.position - position)).sqrMagnitude;
                if (distance > bestDistance) continue;
                bestDistance = distance; nearestObject = candidate;
            }
            return nearestObject;
        }

        private bool AcceptPointer(WorldInteractable target) => Supports(target, ResolveInteractionTool(target, pointerTool));

        private bool IsEnemyUnderPointer()
        {
            if (mainCamera == null || IsPointerOverUi(-1)) return false;
            Vector3 pointer = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            pointer.z = 0;
            var enemies = EnemyAIBase.ActiveEnemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.IsAlive) continue;
                var collider = enemy.GetComponent<Collider2D>();
                if (collider != null && collider.OverlapPoint(pointer)) return true;
            }
            return false;
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
