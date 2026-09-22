using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    public sealed class InventoryPanelSystem : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text contentText;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerToolUpgradeController toolUpgrades;
        [SerializeField] private PlayerCraftingController crafting;

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            backpackOpen = false;
            if (inventory != null) inventory.InventoryChanged -= Refresh;
            if (toolUpgrades != null) toolUpgrades.ToolUpgradesChanged -= Refresh;
            if (crafting != null) crafting.CraftingChanged -= Refresh;
        }

        private void Subscribe()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged -= Refresh;
                inventory.InventoryChanged += Refresh;
            }
            if (toolUpgrades != null)
            {
                toolUpgrades.ToolUpgradesChanged -= Refresh;
                toolUpgrades.ToolUpgradesChanged += Refresh;
            }
            if (crafting != null)
            {
                crafting.CraftingChanged -= Refresh;
                crafting.CraftingChanged += Refresh;
            }
        }

        public void Configure(
            GameObject inventoryPanel,
            Text content,
            PlayerInventory playerInventory,
            PlayerToolUpgradeController upgrades,
            PlayerCraftingController playerCrafting)
        {
            panel = inventoryPanel;
            contentText = content;
            inventory = playerInventory;
            toolUpgrades = upgrades;
            crafting = playerCrafting;

            Subscribe();
            Refresh();
            SetOpen(false);
        }

        public void Toggle()
        {
            SetOpen(panel == null || !panel.activeSelf);
        }

        public void Close()
        {
            SetOpen(false);
        }

        public void EatFood()
        {
            if (!BackpackActions.Use(inventory, "Food"))
                FarmNotificationCenter.Show("Necesitas comida y tener vida por recuperar.");
        }

        private void SetOpen(bool open)
        {
            if(open&&FarmIntroduction.IsOpen)return;
            EnsureVisual();
            if (open) { VillageDialogueWindow.CloseActive(); SimpleShopSystem.CloseActive(); GetComponent<AdventureWindow>()?.Close();inventory?.GetComponent<ConstructionSystem>()?.Cancel(); equipment?.Close(); GetComponent<CraftingWindow>()?.Close(); GetComponent<WorkshopProgressionWindow>()?.Close(); }
            backpackOpen = open;
            inventory?.GetComponent<PlayerMovementController>()?.StopMovement();
            if (panel != null)
            {
                panel.SetActive(open);
            }

            if (open)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (contentText == null || inventory == null)
            {
                return;
            }

            EnsureVisual();
            visual.Refresh();
        }

        private VisualBackpack visual;
        private static bool backpackOpen;
        public static bool IsOpen => backpackOpen || PetAdoptionWindow.IsOpen || MasteryWindow.IsOpen || VillageUpgradeWindow.IsOpen || WorkshopProgressionWindow.IsOpen || FarmIntroduction.BlocksGameplay || VillageDialogueWindow.IsOpen || SimpleShopSystem.IsOpen || AdventureWindow.IsOpen || ConstructionSystem.IsPlacing || CraftingWindow.IsOpen || PlayerEquipmentWindow.IsOpen || PlayerRespawnController.MenuOpen;
        private PlayerEquipmentWindow equipment;
        private void Start()
        {
            EnsureEquipment();
            if (inventory != null)
            {
                gameObject.AddComponent<CraftingWindow>().Configure(inventory, panel.transform.parent);
                gameObject.AddComponent<WorkshopProgressionWindow>().Configure(inventory, panel.transform.parent);
                gameObject.AddComponent<AdventureWindow>().Configure(inventory, panel.transform.parent);
                gameObject.AddComponent<VillageDialogueWindow>().Configure(panel.transform.parent);
            }
        }
        private void EnsureEquipment()
        {
            if (equipment != null || inventory == null) return;
            equipment = gameObject.AddComponent<PlayerEquipmentWindow>();
            equipment.Configure(inventory, this, panel.transform.parent);
        }
        public void OpenEquipment()
        {
            EnsureEquipment(); Close(); equipment.Open();
        }
        private void EnsureVisual()
        {
            if (visual != null || panel == null || inventory == null) return;
            visual = panel.GetComponent<VisualBackpack>() ?? panel.AddComponent<VisualBackpack>();
            visual.Configure(inventory, toolUpgrades, this);
        }
        private void Update()
        {
            if (PlayerRespawnController.MenuOpen) return;
            if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.B)) Toggle();
            if (Input.GetKeyDown(KeyCode.Escape) && backpackOpen) Close();
        }
    }
}
