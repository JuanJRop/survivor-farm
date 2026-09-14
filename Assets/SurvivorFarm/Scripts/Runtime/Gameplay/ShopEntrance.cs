using UnityEngine;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class ShopEntrance : WorldInteractable
    {
        [SerializeField] private GameObject outdoorRoot;
        [SerializeField] private GameObject shopInteriorRoot;
        [SerializeField] private Transform player;
        [SerializeField] private Transform outsideSpawn;
        [SerializeField] private SimpleShopSystem shopSystem;
        [SerializeField] private float outsideCameraSize = 6f;

        public bool IsInsideShop => false;
        public Vector3 OutsidePosition => outsideSpawn != null ? outsideSpawn.position : transform.position + Vector3.down * 1.5f;

        private void Start()
        {
            HideInterior();
            shopSystem?.SetEntrance(this);
        }

        private void HideInterior()
        {
            if (shopInteriorRoot != null) shopInteriorRoot.SetActive(false);
        }

        public void Configure(
            GameObject outsideWorld,
            GameObject interiorWorld,
            Transform playerTransform,
            Transform outsidePoint,
            Transform insidePoint,
            SimpleShopSystem system)
        {
            outdoorRoot = outsideWorld;
            shopInteriorRoot = interiorWorld;
            player = playerTransform;
            outsideSpawn = outsidePoint;
            shopSystem = system;
            HideInterior();
            shopSystem?.SetEntrance(this);
        }

        public override string GetInteractionLabel(FarmTool selectedTool)
        {
            return "Mercado - comerciar";
        }

        public override void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            if (inventory == null || (player != null && player != inventory.transform)) return;
            HideInterior();
            (shopSystem != null ? shopSystem : FindFirstObjectByType<SimpleShopSystem>())?.SetOpen(true);
        }

        public void ExitShop()
        {
            HideInterior();
            shopSystem?.SetOpen(false);
        }

        public void RestoreInsideState(bool restoreInsideShop)
        {
            HideInterior();
            shopSystem?.SetOpen(false);
            if (restoreInsideShop) RecoverLegacyInterior();
        }

        public void RecoverLegacyInterior()
        {
            HideInterior();
            if (outdoorRoot != null) outdoorRoot.SetActive(true);
            if (player != null)
            {
                player.position = OutsidePosition;
                var body = player.GetComponent<Rigidbody2D>();
                if (body != null) { body.position = player.position; body.linearVelocity = Vector2.zero; }
                PlayerMovementController movement = player.GetComponent<PlayerMovementController>();
                movement?.StopMovement();
            }
            shopSystem?.SetOpen(false);
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.orthographicSize = outsideCameraSize;
                if (player != null)
                {
                    camera.transform.position = player.position + new Vector3(0f, 0f, -10f);
                }
            }

        }
    }
}
