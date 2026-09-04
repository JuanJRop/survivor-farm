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
            IWorldInteractable nearest = FindNearestInteractable(transform.position, interactionRadius);
            if (!ReferenceEquals(nearest, highlightedInteractable))
            {
                if (highlightedInteractable != null)
                {
                    highlightedInteractable.SetHighlighted(false);
                }

                highlightedInteractable = nearest;

                if (highlightedInteractable != null)
                {
                    highlightedInteractable.SetHighlighted(true);
                }
            }

            FarmTool selectedTool = toolbelt != null ? toolbelt.SelectedTool : FarmTool.Sword;
            FarmNotificationCenter.SetPrompt(highlightedInteractable != null
                ? highlightedInteractable.GetInteractionLabel(selectedTool)
                : "Tala arboles, pica rocas o cava pasto con la pala");
            if (highlightedInteractable != null)
            {
                if (mainCamera == null)
                {
                    mainCamera = Camera.main;
                }

                FarmNotificationCenter.SetInteractionButtonAtWorldPosition(
                    true,
                    "Interactuar",
                    highlightedInteractable.Transform.position + interactionButtonWorldOffset,
                    mainCamera);
            }
            else
            {
                FarmNotificationCenter.SetInteractionButton(false, "Interactuar");
            }

            if (highlightedInteractable != null && Input.GetKeyDown(KeyCode.E))
            {
                PerformInteraction();
            }

            HandleTouchInteraction();
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
            if (highlightedInteractable == null)
            {
                return;
            }

            movement?.StopMovement();
            FarmTool selectedTool = toolbelt != null ? toolbelt.SelectedTool : FarmTool.Sword;
            characterAnimator?.PlayToolAction(selectedTool);
            highlightedInteractable.Interact(selectedTool, inventory);
        }

        private static IWorldInteractable FindNearestInteractable(Vector3 position, float radius)
        {
            FarmingPlot[] plots = FindObjectsByType<FarmingPlot>(FindObjectsSortMode.None);
            HarvestableResource[] resources = FindObjectsByType<HarvestableResource>(FindObjectsSortMode.None);
            LandUnlockZone[] unlockZones = FindObjectsByType<LandUnlockZone>(FindObjectsSortMode.None);
            DungeonEntrance[] dungeonEntrances = FindObjectsByType<DungeonEntrance>(FindObjectsSortMode.None);
            DungeonExit[] dungeonExits = FindObjectsByType<DungeonExit>(FindObjectsSortMode.None);
            BaseHouse[] houses = FindObjectsByType<BaseHouse>(FindObjectsSortMode.None);
            DungeonChest[] chests = FindObjectsByType<DungeonChest>(FindObjectsSortMode.None);
            IWorldInteractable nearest = null;
            float nearestDistance = radius * radius;

            foreach (LandUnlockZone unlockZone in unlockZones)
            {
                if (!unlockZone.IsAvailable)
                {
                    continue;
                }

                float distance = (unlockZone.Transform.position - position).sqrMagnitude;
                if (distance <= nearestDistance)
                {
                    nearest = unlockZone;
                    nearestDistance = distance;
                }
            }

            if (nearest != null)
            {
                return nearest;
            }

            foreach (DungeonExit dungeonExit in dungeonExits)
            {
                if (!dungeonExit.IsAvailable)
                {
                    continue;
                }

                float distance = (dungeonExit.Transform.position - position).sqrMagnitude;
                if (distance <= nearestDistance)
                {
                    nearest = dungeonExit;
                    nearestDistance = distance;
                }
            }

            foreach (DungeonEntrance dungeonEntrance in dungeonEntrances)
            {
                if (!dungeonEntrance.IsAvailable)
                {
                    continue;
                }

                float distance = (dungeonEntrance.Transform.position - position).sqrMagnitude;
                if (distance <= nearestDistance)
                {
                    nearest = dungeonEntrance;
                    nearestDistance = distance;
                }
            }

            foreach (BaseHouse house in houses)
            {
                if (!house.IsAvailable)
                {
                    continue;
                }

                float distance = (house.Transform.position - position).sqrMagnitude;
                if (distance <= nearestDistance)
                {
                    nearest = house;
                    nearestDistance = distance;
                }
            }

            foreach (DungeonChest chest in chests)
            {
                if (!chest.IsAvailable)
                {
                    continue;
                }

                float distance = (chest.Transform.position - position).sqrMagnitude;
                if (distance <= nearestDistance)
                {
                    nearest = chest;
                    nearestDistance = distance;
                }
            }

            foreach (FarmingPlot plot in plots)
            {
                float distance = (plot.Transform.position - position).sqrMagnitude;
                if (distance <= nearestDistance)
                {
                    nearest = plot;
                    nearestDistance = distance;
                }
            }

            foreach (HarvestableResource resource in resources)
            {
                if (!resource.IsAvailable)
                {
                    continue;
                }

                float distance = (resource.Transform.position - position).sqrMagnitude;
                if (distance <= nearestDistance)
                {
                    nearest = resource;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private void HandleTouchInteraction()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                return;
            }

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began && !IsPointerOverUi(touch.fingerId))
                {
                    TryMoveAtScreenPosition(touch.position);
                }
            }
            else if (Input.GetMouseButtonDown(0) && !IsPointerOverUi(-1))
            {
                TryMoveAtScreenPosition(Input.mousePosition);
            }
        }

        private void TryMoveAtScreenPosition(Vector2 screenPosition)
        {
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
            if (movement != null)
            {
                movement.MoveToWorldPosition(worldPosition);
            }
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
