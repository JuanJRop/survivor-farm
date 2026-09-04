using UnityEngine;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class ShopEntrance : MonoBehaviour, IWorldInteractable
    {
        [SerializeField] private GameObject outdoorRoot;
        [SerializeField] private GameObject shopInteriorRoot;
        [SerializeField] private Transform player;
        [SerializeField] private Transform outsideSpawn;
        [SerializeField] private Transform insideSpawn;
        [SerializeField] private SimpleShopSystem shopSystem;
        [SerializeField] private float outsideCameraSize = 6f;
        [SerializeField] private float insideCameraSize = 4.2f;

        private bool insideShop;

        public Transform Transform => transform;
        public bool IsAvailable => true;
        public bool IsInsideShop => insideShop;

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
            insideSpawn = insidePoint;
            shopSystem = system;
            SetInside(false, false);
        }

        public string GetInteractionLabel(FarmTool selectedTool)
        {
            return insideShop ? "Interactuar: salir de tienda" : "Interactuar: entrar a tienda";
        }

        public void SetHighlighted(bool highlighted)
        {
            transform.localScale = highlighted ? Vector3.one * 1.08f : Vector3.one;
        }

        public void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            SetInside(!insideShop, true);
        }

        public void ExitShop()
        {
            SetInside(false, true);
        }

        public void RestoreInsideState(bool restoreInsideShop)
        {
            SetInside(restoreInsideShop, false);
        }

        private void SetInside(bool value, bool notify)
        {
            insideShop = value;

            if (outdoorRoot != null)
            {
                outdoorRoot.SetActive(!insideShop);
            }

            if (shopInteriorRoot != null)
            {
                shopInteriorRoot.SetActive(insideShop);
            }

            Transform spawn = insideShop ? insideSpawn : outsideSpawn;
            if (player != null && spawn != null)
            {
                player.position = spawn.position;
                PlayerMovementController movement = player.GetComponent<PlayerMovementController>();
                movement?.StopMovement();
            }

            if (shopSystem != null)
            {
                shopSystem.SetOpen(insideShop);
            }

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.orthographicSize = insideShop ? insideCameraSize : outsideCameraSize;
                if (player != null)
                {
                    camera.transform.position = player.position + new Vector3(0f, 0f, -10f);
                }
            }

            if (notify)
            {
                FarmNotificationCenter.Show(insideShop ? "Entraste a la tienda." : "Saliste de la tienda.");
            }
        }
    }
}
