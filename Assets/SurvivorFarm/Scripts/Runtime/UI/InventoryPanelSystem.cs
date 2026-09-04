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

            if (inventory != null)
            {
                inventory.InventoryChanged += Refresh;
            }

            if (toolUpgrades != null)
            {
                toolUpgrades.ToolUpgradesChanged += Refresh;
            }

            if (crafting != null)
            {
                crafting.CraftingChanged += Refresh;
            }

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

        private void SetOpen(bool open)
        {
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

            string tools = toolUpgrades != null ? toolUpgrades.GetUpgradeSummary() : "Herramientas Nv.1";
            string structures = crafting != null ? crafting.GetCraftingSummary() : "Sin estructuras";
            contentText.text =
                $"Semilla comun: {inventory.CommonSeeds}/{inventory.MaxSeedsPerSlot}\n" +
                $"Semilla mineral: {inventory.MineralSeeds}/{inventory.MaxSeedsPerSlot}\n" +
                $"Semilla magica: {inventory.MagicSeeds}/{inventory.MaxSeedsPerSlot}\n\n" +
                $"Madera: {inventory.Wood}\n" +
                $"Piedra: {inventory.Stone}\n" +
                $"Fruta: {inventory.Fruit}\n" +
                $"Oro: {inventory.Coins}\n\n" +
                $"{tools}\n" +
                $"{structures}";
        }
    }
}
