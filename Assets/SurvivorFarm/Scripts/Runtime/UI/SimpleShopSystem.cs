using UnityEngine;
using System.Collections.Generic;
using System.Linq;
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
        private ExteriorShopWindow window;
        private static SimpleShopSystem activeShop;
        public static bool IsOpen => activeShop != null && activeShop.panel != null && activeShop.panel.activeInHierarchy;
        public static void CloseActive() { if (activeShop != null) activeShop.SetOpen(false); }

        public void Configure(
            GameObject shopPanel,
            Text wallet,
            Text stock,
            PlayerInventory playerInventory,
            PlayerToolUpgradeController upgrades,
            PlayerCraftingController playerCrafting)
        {
            Unsubscribe();
            panel = shopPanel;
            walletText = wallet;
            stockText = stock;
            inventory = playerInventory;
            toolUpgrades = upgrades;
            crafting = playerCrafting;

            Subscribe();

            Refresh();
            SetOpen(false);
        }

        public void SetEntrance(ShopEntrance shopEntrance)
        {
            entrance = shopEntrance;
        }

        public void SetOpen(bool open)
        {
            if (open)
            {
                VillageDialogueWindow.CloseActive();
                if (activeShop != null && activeShop != this) CloseActive();
                FindFirstObjectByType<AdventureWindow>()?.Close();
                FindFirstObjectByType<InventoryPanelSystem>()?.Close();
                FindFirstObjectByType<PlayerEquipmentWindow>()?.Close();
                FindFirstObjectByType<CraftingWindow>()?.Close();
                inventory?.GetComponent<ConstructionSystem>()?.Cancel();
                inventory?.GetComponent<PlayerMovementController>()?.StopMovement();
                EnsureWindow();
                activeShop = this;
                panel?.transform.SetAsLastSibling();
            }
            else if (activeShop == this) activeShop = null;
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
            // Kept for serialized buttons from old scenes; seeds are no longer sold.
        }

        public void BuyMineralSeeds()
        {
        }

        public void BuyMagicSeeds()
        {
        }

        public void BuyWood()
        {
            BuyItem(4, () => inventory.AddWood(1), "Compraste 1 madera.", inventory?.Wood ?? 0);
        }

        public void BuyStone()
        {
            BuyItem(5, () => inventory.AddStone(1), "Compraste 1 piedra.", inventory?.Stone ?? 0);
        }

        public void BuyFruit()
        {
            BuyItem(8, () => inventory.AddFruit(1), "Compraste 1 fruta.", inventory?.Fruit ?? 0);
        }

        private void EnsureWindow()
        {
            if (window != null || panel == null || inventory == null) return;
            window = panel.GetComponent<ExteriorShopWindow>() ?? panel.AddComponent<ExteriorShopWindow>();
            window.Configure(this, inventory, toolUpgrades);
        }

        public void OpenService(string category)
        {
            SetOpen(true);
            window?.SelectCategory(category);
        }

        private void Update()
        {
            if (activeShop != this) return;
            if (Input.GetKeyDown(KeyCode.Escape) || PlayerRespawnController.MenuOpen ||
                (inventory != null && inventory.GetComponent<PlayerSurvivalStats>()?.CurrentHealth <= 0)) ExitShop();
            else if (panel != null) FarmUiStyle.FitWindow((RectTransform)panel.transform);
        }

        private void Subscribe()
        {
            Unsubscribe();
            if (inventory != null) inventory.InventoryChanged += Refresh;
            if (toolUpgrades != null) toolUpgrades.ToolUpgradesChanged += Refresh;
            if (crafting != null) crafting.CraftingChanged += Refresh;
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() { SetOpen(false); Unsubscribe(); }

        private void Unsubscribe()
        {
            if (inventory != null) inventory.InventoryChanged -= Refresh;
            if (toolUpgrades != null) toolUpgrades.ToolUpgradesChanged -= Refresh;
            if (crafting != null) crafting.CraftingChanged -= Refresh;
        }

        private void OnDestroy()
        {
            if (activeShop == this) activeShop = null;
            Unsubscribe();
        }

        public void BuyFood() => BuyItem(18, () => inventory.AddFood(1), "Compraste una racion.", inventory?.Food ?? 0);

        public bool CanBuyCatalogItem(string id, out string requirement)
        {
            var item = SurvivalItemCatalog.Find(id);
            requirement = item == null ? "Objeto desconocido." : item.IsSeed ? "Objeto retirado del mercado." : inventory == null ? "Necesitas un inventario." :
                (inventory.GetComponent<PlayerCraftingController>()?.MealsCooked ?? 0) < item.UnlockMealsCooked ?
                $"Cocina {item.UnlockMealsCooked} veces para ampliar el surtido." : string.Empty;
            return requirement.Length == 0;
        }

        public IEnumerable<SurvivalItemCatalog.Definition> GetAvailableCatalogItems() =>
            SurvivalItemCatalog.All.Where(item => CanBuyCatalogItem(item.Id, out _));

        public void BuyCatalogItem(string id, int amount = 1)
        {
            var item = SurvivalItemCatalog.Find(id);
            if (item == null || amount <= 0) return;
            if (!CanBuyCatalogItem(id, out string requirement))
            {
                FarmNotificationCenter.Show(requirement);
                return;
            }
            if (!inventory.TryBuyCatalogItem(id, amount))
            {
                FarmNotificationCenter.Show("Compra no disponible: revisa el oro y la cantidad.");
                return;
            }
            FarmNotificationCenter.Show($"Compraste {amount} x {item.Name}.");
        }

        public void SellSeeds()
        {
            SellResource("CommonSeeds");
        }

        public void SellMineralSeeds()
        {
            SellResource("MineralSeeds");
        }

        public void SellMagicSeeds()
        {
            SellResource("MagicSeeds");
        }

        public void SellWood()
        {
            SellResource("Wood");
        }

        public void SellStone()
        {
            SellResource("Stone");
        }

        public void SellFruit()
        {
            SellResource("Fruit");
        }

        public void SellCatalogItem(string id, int amount = 1)
        {
            var item = SurvivalItemCatalog.Find(id);
            if (item == null || amount <= 0) return;
            SellResource(id, amount);
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
            UpgradeHoe();
        }

        public void UpgradeHoe()
        {
            toolUpgrades?.UpgradeHoe();
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
            SetOpen(false);
        }

        private void SellResource(string id, int amount = 1)
        {
            if (!BackpackActions.Sell(inventory, id, amount))
            {
                FarmNotificationCenter.Show("Venta no disponible: revisa la cantidad y el espacio para oro.");
            }
        }

        private void BuyItem(int coinCost, System.Action addItem, string message, int currentCount = 0)
        {
            if (inventory == null)
            {
                return;
            }

            if (currentCount == int.MaxValue)
            {
                FarmNotificationCenter.Show("No caben mas unidades de este recurso.");
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
                int foods = 0, rareMaterials = 0, gems = 0, elements = 0;
                foreach (var stack in inventory.ItemStacks ?? new System.Collections.Generic.List<InventoryStack>())
                {
                    var item = stack != null ? SurvivalItemCatalog.Find(stack.id) : null;
                    if (item == null) continue;
                    else if (item.IsFood) foods += stack.count;
                    else if (item.Category == SurvivalItemCategory.Material) rareMaterials += stack.count;
                    else if (item.IsGem) gems += stack.count;
                    else if (item.IsElement) elements += stack.count;
                }
                int iron = inventory.GetComponent<AdventureProgress>()?.Data.iron ?? 0;
                int goldOre = inventory.GetItemCount("GoldOre");
                stockText.text = $"Madera {inventory.Wood} | Piedra {inventory.Stone} | Hierro {iron} | Oro bruto {goldOre} | Mat. raros {rareMaterials} | Fruta {inventory.Fruit} | Comidas {foods} | Gemas {gems} | Elem. {elements}{toolSummary}{craftingSummary}";
            }
            if (window != null) { window.Refresh(); return; }
        }
    }
}
