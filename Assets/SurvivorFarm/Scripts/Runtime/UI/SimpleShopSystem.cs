using UnityEngine;
using UnityEngine.UI;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;

namespace SurvivorFarm.Runtime.UI
{
    public sealed class SimpleShopSystem : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text walletText;
        [SerializeField] private Text stockText;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private ShopEntrance entrance;
        [SerializeField] private PlayerToolUpgradeController toolUpgrades;
        [SerializeField] private PlayerCraftingController crafting;

        public void Configure(
            GameObject shopPanel,
            Text wallet,
            Text stock,
            PlayerInventory playerInventory,
            PlayerToolUpgradeController upgrades,
            PlayerCraftingController playerCrafting)
        {
            panel = shopPanel;
            walletText = wallet;
            stockText = stock;
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

        public void SetEntrance(ShopEntrance shopEntrance)
        {
            entrance = shopEntrance;
        }

        public void SetOpen(bool open)
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

        public void BuySeeds()
        {
            if (inventory == null)
            {
                return;
            }

            int availableSpace = inventory.MaxSeedsPerSlot - inventory.Seeds;
            if (availableSpace <= 0)
            {
                FarmNotificationCenter.Show("El slot de semillas esta lleno.");
                return;
            }

            if (!inventory.TrySpendCoins(5))
            {
                FarmNotificationCenter.Show("No tienes monedas suficientes.");
                return;
            }

            int purchasedSeeds = Mathf.Min(3, availableSpace);
            inventory.AddSeeds(purchasedSeeds);
            FarmNotificationCenter.Show($"Compraste {purchasedSeeds} semillas.");
        }

        public void BuyMineralSeeds()
        {
            BuySeedBundle(SeedRarity.Mineral, 12, 1, "Compraste 1 semilla mineral.");
        }

        public void BuyMagicSeeds()
        {
            BuySeedBundle(SeedRarity.Magic, 25, 1, "Compraste 1 semilla magica.");
        }

        public void BuyWood()
        {
            BuyItem(4, () => inventory.AddWood(1), "Compraste 1 madera.");
        }

        public void BuyStone()
        {
            BuyItem(5, () => inventory.AddStone(1), "Compraste 1 piedra.");
        }

        public void BuyFruit()
        {
            BuyItem(8, () => inventory.AddFruit(1), "Compraste 1 fruta.");
        }

        public void SellSeeds()
        {
            SellItem(inventory != null && inventory.TryRemoveSeeds(1), 1, "Vendiste 1 semilla.");
        }

        public void SellMineralSeeds()
        {
            SellItem(inventory != null && inventory.TryRemoveSeeds(SeedRarity.Mineral, 1), 5, "Vendiste 1 semilla mineral.");
        }

        public void SellMagicSeeds()
        {
            SellItem(inventory != null && inventory.TryRemoveSeeds(SeedRarity.Magic, 1), 12, "Vendiste 1 semilla magica.");
        }

        public void SellWood()
        {
            SellItem(inventory != null && inventory.TryRemoveWood(1), 3, "Vendiste 1 madera.");
        }

        public void SellStone()
        {
            SellItem(inventory != null && inventory.TryRemoveStone(1), 4, "Vendiste 1 piedra.");
        }

        public void SellFruit()
        {
            SellItem(inventory != null && inventory.TryRemoveFruit(1), 6, "Vendiste 1 fruta.");
        }

        public void UpgradeAxe()
        {
            toolUpgrades?.UpgradeAxe();
            Refresh();
        }

        public void UpgradePickaxe()
        {
            toolUpgrades?.UpgradePickaxe();
            Refresh();
        }

        public void UpgradeShovel()
        {
            toolUpgrades?.UpgradeShovel();
            Refresh();
        }

        public void CraftStorage()
        {
            crafting?.CraftStorage();
            Refresh();
        }

        public void CraftCamp()
        {
            crafting?.CraftCamp();
            Refresh();
        }

        public void ExitShop()
        {
            entrance?.ExitShop();
        }

        private void SellItem(bool removed, int coinsEarned, string message)
        {
            if (!removed)
            {
                FarmNotificationCenter.Show("No tienes ese objeto para vender.");
                return;
            }

            inventory.AddCoins(coinsEarned);
            FarmNotificationCenter.Show(message);
        }

        private void BuyItem(int coinCost, System.Action addItem, string message)
        {
            if (inventory == null)
            {
                return;
            }

            if (inventory.GetSeedCount(rarity) + amount > inventory.MaxSeedsPerSlot)
            {
                FarmNotificationCenter.Show("Ese slot de semillas esta lleno.");
                return;
            }

            if (!inventory.TrySpendCoins(coinCost))
            {
                FarmNotificationCenter.Show("No tienes monedas suficientes.");
                return;
            }

            addItem?.Invoke();
            FarmNotificationCenter.Show(message);
        }

        private void BuySeedBundle(SeedRarity rarity, int coinCost, int amount, string message)
        {
            if (inventory == null)
            {
                return;
            }

            if (!inventory.TrySpendCoins(coinCost))
            {
                FarmNotificationCenter.Show("No tienes monedas suficientes.");
                return;
            }

            inventory.AddSeeds(rarity, amount);
            FarmNotificationCenter.Show(message);
        }

        private void Refresh()
        {
            if (inventory == null)
            {
                return;
            }

            if (walletText != null)
            {
                walletText.text = $"Monedas: {inventory.Coins}";
            }

            if (stockText != null)
            {
                string toolSummary = toolUpgrades != null ? $"\n{toolUpgrades.GetUpgradeSummary()}" : string.Empty;
                string craftingSummary = crafting != null ? $"\n{crafting.GetCraftingSummary()}" : string.Empty;
                stockText.text = $"Semillas C{inventory.CommonSeeds} M{inventory.MineralSeeds} G{inventory.MagicSeeds} | Madera {inventory.Wood} | Piedra {inventory.Stone} | Fruta {inventory.Fruit}{toolSummary}{craftingSummary}";
            }
        }
    }
}
