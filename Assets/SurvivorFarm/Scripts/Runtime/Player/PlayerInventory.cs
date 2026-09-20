using System;
using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    [Serializable] public sealed class PackedBuilding { public string kind; public int count; }
    [Serializable] public sealed class InventoryStack { public string id; public int count; }
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private int slotCount = 7;
        [SerializeField] private int commonSeeds;
        [SerializeField] private int mineralSeeds;
        [SerializeField] private int magicSeeds;
        [SerializeField] private int wood;
        [SerializeField] private int stone;
        [SerializeField] private int fruit;
        [SerializeField] private int food;
        [SerializeField] private int coins;
        [SerializeField] private int maxSeedsPerSlot = 20;
        [SerializeField] private int totalGathered;
        public int TotalGathered => totalGathered;
        public event Action GatheringChanged;

        public void RecordGathered(int amount)
        {
            if (amount <= 0) return;
            totalGathered = (int)Math.Min(int.MaxValue, (long)totalGathered + amount);
            GatheringChanged?.Invoke();
        }

        public void RestoreGathered(int amount)
        {
            totalGathered = Mathf.Max(0, amount);
            GatheringChanged?.Invoke();
        }

        public int SlotCount => slotCount;
        public int Seeds => commonSeeds;
        public int CommonSeeds => commonSeeds;
        public int MineralSeeds => mineralSeeds;
        public int MagicSeeds => magicSeeds;
        public int TotalSeeds => (int)Math.Min(int.MaxValue, (long)commonSeeds + mineralSeeds + magicSeeds + CatalogSeedCount);
        public int Wood => wood;
        public int Stone => stone;
        public int Fruit => fruit;
        public int Food => food;
        public int Coins => coins;
        public int MaxSeedsPerSlot => int.MaxValue;
        public List<PackedBuilding> PackedBuildings = new List<PackedBuilding>();
        public List<InventoryStack> ItemStacks = new List<InventoryStack>();
        public int PreferredSeed = -1;
        public string PreferredCatalogSeed;
        private readonly System.Collections.Generic.HashSet<EconomySeedReservation> seedReservations = new System.Collections.Generic.HashSet<EconomySeedReservation>();
        private int CatalogSeedCount => (int)Math.Min(int.MaxValue, (ItemStacks ?? new List<InventoryStack>())
            .Where(stack => stack != null && SurvivalItemCatalog.IsSeed(stack.id))
            .Sum(stack => (long)Mathf.Max(0, stack.count)));
        public int PackedCount(string kind) => PackedBuildings?.FirstOrDefault(p=>p.kind==kind)?.count??0;
        public void AddPacked(string kind,int amount=1){if(amount<=0||!BackpackActions.IsBuilding(kind))return;PackedBuildings??=new List<PackedBuilding>();var p=PackedBuildings.FirstOrDefault(p=>p.kind==kind);if(p==null){p=new PackedBuilding{kind=kind};PackedBuildings.Add(p);}p.count=AddSafe(p.count,amount);InventoryChanged?.Invoke();}
        public bool RemovePacked(string kind,int amount=1){var p=PackedBuildings?.FirstOrDefault(p=>p.kind==kind);if(amount<=0||p==null||p.count<amount)return false;p.count-=amount;InventoryChanged?.Invoke();return true;}
        public void RestorePacked(List<PackedBuilding> items){PackedBuildings=(items??new List<PackedBuilding>()).Where(p=>p!=null&&p.count>0&&BackpackActions.IsBuilding(p.kind)).Select(p=>new PackedBuilding{kind=p.kind,count=p.count}).ToList();InventoryChanged?.Invoke();}
        public string[] BackpackOrder = new string[0];
        public string[] OwnedEquipment = { "Sword" };
        public string[] EquippedEquipment = { "", "", "", "Sword", "", "", "", "" };
        public bool HasEquipped(string id) => Array.IndexOf(EquippedEquipment,id)>=0;
        public float ArmorReduction => Mathf.Clamp(EquippedDefinitions.Sum(item => item.DefenseBonus), 0f, 0.75f);
        public float MovementBonus => 1f + EquippedDefinitions.Sum(item => item.SpeedBonus);
        public int EquipmentDamage => EquippedDefinitions.Sum(item => item.DamageBonus);
        public int FoodHealingBonus => EquippedDefinitions.Sum(item => item.FoodHealingBonus);
        public int FoodHungerBonus => EquippedDefinitions.Sum(item => item.FoodHungerBonus);
        public float HungerMultiplier => Mathf.Clamp(1f - EquippedDefinitions.Sum(item => item.HungerSave), 0.55f, 1.2f);
        public IEnumerable<EquipmentItems.Definition> EquippedDefinitions => (EquippedEquipment ?? new string[0])
            .Select(EquipmentItems.Find)
            .Where(item => item != null);
        public bool OwnsEquipment(string id) => Array.IndexOf(OwnedEquipment, id) >= 0;
        public void AddEquipment(string id)
        {
            if (EquipmentItems.Find(id) == null || OwnsEquipment(id)) return;
            Array.Resize(ref OwnedEquipment, OwnedEquipment.Length + 1);
            OwnedEquipment[OwnedEquipment.Length - 1] = id;
            InventoryChanged?.Invoke();
        }

        public int GetItemCount(string id)
        {
            if (string.IsNullOrEmpty(id) || ItemStacks == null) return 0;
            return (int)Math.Min(int.MaxValue, ItemStacks.Where(stack => stack != null && stack.id == id).Sum(stack => (long)Mathf.Max(0, stack.count)));
        }

        public int GetAvailableItemCount(string id) => Mathf.Max(0, GetItemCount(id) - seedReservations.Count(value => value.SeedItemId == id));
        public int GetAvailableSeedCount(SeedRarity rarity) => Mathf.Max(0, GetSeedCount(rarity) - seedReservations.Count(value => value.SeedItemId == null && value.Rarity == rarity));

        public void AddItem(string id, int amount = 1)
        {
            if (amount <= 0 || !SurvivalItemCatalog.IsKnown(id)) return;
            ItemStacks ??= new List<InventoryStack>();
            InventoryStack stack = ItemStacks.FirstOrDefault(value => value != null && value.id == id);
            if (stack == null)
            {
                stack = new InventoryStack { id = id };
                ItemStacks.Add(stack);
            }

            stack.count = AddSafe(stack.count, amount);
            InventoryChanged?.Invoke();
        }

        public bool TryBuyCatalogItem(string id, int amount = 1)
        {
            SurvivalItemCatalog.Definition item = SurvivalItemCatalog.Find(id);
            if (item == null || item.IsSeed || !EconomyTradeRules.TryGetTotalPrice(item.BuyPrice, amount, out int total) ||
                coins < total || (long)GetItemCount(id) + amount > int.MaxValue) return false;
            coins -= total;
            AddItem(id, amount);
            return true;
        }

        public bool TryRemoveItem(string id, int amount = 1)
        {
            if (amount <= 0 || string.IsNullOrEmpty(id) || ItemStacks == null) return false;
            if (GetAvailableItemCount(id) < amount) return false;
            RemoveItemUnits(id, amount);
            InventoryChanged?.Invoke();
            return true;
        }

        private void RemoveItemUnits(string id, int amount)
        {
            int remaining = amount;
            foreach (InventoryStack stack in ItemStacks.Where(value => value != null && value.id == id))
            {
                int take = Math.Min(Mathf.Max(0, stack.count), remaining);
                stack.count -= take;
                remaining -= take;
                if (remaining == 0) break;
            }
            ItemStacks.RemoveAll(stack => stack != null && stack.id == id && stack.count <= 0);
            if (PreferredCatalogSeed == id && GetItemCount(id) <= 0) PreferredCatalogSeed = null;
        }

        public bool TryPrepareMeals(IReadOnlyList<EconomyRecipeCost> ingredients, int amount)
        {
            if (ingredients == null || ingredients.Count == 0 || amount <= 0 || (long)food + amount > int.MaxValue) return false;
            var costs = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (EconomyRecipeCost ingredient in ingredients)
            {
                if (ingredient == null || string.IsNullOrEmpty(ingredient.ItemId) || ingredient.Amount <= 0) return false;
                string id = ingredient.ItemId;
                if (id != "Fruit" && id != "Wood" && !SurvivalItemCatalog.IsFood(id)) return false;
                costs.TryGetValue(id, out int previous);
                long total = (long)previous + ingredient.Amount;
                if (total > int.MaxValue) return false;
                costs[id] = (int)total;
            }
            foreach (var cost in costs)
            {
                int available = cost.Key == "Fruit" ? fruit : cost.Key == "Wood" ? wood : GetAvailableItemCount(cost.Key);
                if (available < cost.Value) return false;
            }
            // Publish one inventory change after all ingredients and outputs have been applied.
            foreach (var cost in costs)
            {
                if (cost.Key == "Fruit") fruit -= cost.Value;
                else if (cost.Key == "Wood") wood -= cost.Value;
                else RemoveItemUnits(cost.Key, cost.Value);
            }
            food += amount;
            InventoryChanged?.Invoke();
            return true;
        }

        public void RestoreItemStacks(List<InventoryStack> saved)
        {
            ClearSeedReservations();
            ItemStacks = new List<InventoryStack>();
            foreach (InventoryStack stack in saved ?? new List<InventoryStack>())
            {
                if (stack == null || stack.count <= 0 || !SurvivalItemCatalog.IsKnown(stack.id)) continue;
                InventoryStack existing = ItemStacks.FirstOrDefault(value => value.id == stack.id);
                if (existing == null) ItemStacks.Add(new InventoryStack { id = stack.id, count = stack.count });
                else existing.count = AddSafe(existing.count, stack.count);
            }

            InventoryChanged?.Invoke();
        }
        public bool Equip(string id, int slot)
        {
            var item = EquipmentItems.Find(id);
            if (slot < 0 || slot >= 8 || item == null || !item.Fits(slot) || !OwnsEquipment(id)) return false;
            for (int i = 0; i < 8; i++) if (EquippedEquipment[i] == id) EquippedEquipment[i] = "";
            EquippedEquipment[slot] = id;
            if (item.Weapon.HasValue) GetComponent<PlayerToolbelt>()?.Select(item.Weapon.Value);
            InventoryChanged?.Invoke(); return true;
        }
        public void Unequip(int slot)
        {
            if (slot < 0 || slot >= 8) return;
            EquippedEquipment[slot] = "";
            var belt = GetComponent<PlayerToolbelt>();
            if (slot == 3 && belt != null && (belt.SelectedTool == FarmTool.Sword || belt.SelectedTool == FarmTool.Bow))
            {
                if (OwnsEquipment("Sword")) belt.Select(FarmTool.Sword);
                else if (OwnsEquipment("Bow")) belt.Select(FarmTool.Bow);
            }
            InventoryChanged?.Invoke();
        }
        public void RestoreEquipment(string[] owned, string[] equipped)
        {
            OwnedEquipment = owned ?? new[] { "Sword" };
            EquippedEquipment = new string[8];
            if (equipped == null) EquippedEquipment[3] = OwnsEquipment("Sword") ? "Sword" : "";
            else for (int i = 0; i < Math.Min(8,equipped.Length); i++)
            {
                var item = EquipmentItems.Find(equipped[i]);
                if (item != null && item.Fits(i) && OwnsEquipment(item.Id) && Array.IndexOf(EquippedEquipment,item.Id) < 0) EquippedEquipment[i] = item.Id;
            }
            InventoryChanged?.Invoke();
        }
        private static int AddSafe(int value, int amount) => (int)Math.Min(int.MaxValue, (long)value + amount);

        public event Action InventoryChanged;

        public void NotifyInventoryChanged()=>InventoryChanged?.Invoke();
        private void Awake()
        {
            if(GetComponent<AdventureProgress>()==null)gameObject.AddComponent<AdventureProgress>();
            if(GetComponent<ConstructionSystem>()==null)gameObject.AddComponent<ConstructionSystem>();
            if(GetComponent<GameFeelFeedback>()==null)gameObject.AddComponent<GameFeelFeedback>();
        }
        private void Start()
        {
            InventoryChanged?.Invoke();
        }

        public bool TryConsumeSeed()
        {
            SeedRarity ignored;
            string ignoredCrop;
            return TryConsumeSeed(out ignored, out ignoredCrop);
        }

        public bool TryConsumeSeed(out SeedRarity rarity)
        {
            string ignoredCrop;
            return TryConsumeSeed(out rarity, out ignoredCrop);
        }

        public bool TryConsumeSeed(out SeedRarity rarity, out string cropItemId)
        {
            rarity = SeedRarity.Common;
            cropItemId = null;
            if (!TryReserveSeed(out EconomySeedReservation reservation)) return false;
            rarity = reservation.Rarity;
            cropItemId = reservation.CropItemId;
            return CommitSeedReservation(reservation);
        }

        public bool TryReserveSeed(out EconomySeedReservation reservation)
        {
            reservation = null;
            if (!string.IsNullOrEmpty(PreferredCatalogSeed))
            {
                SurvivalItemCatalog.Definition definition = SurvivalItemCatalog.Find(PreferredCatalogSeed);
                if (definition != null && definition.IsSeed && GetAvailableItemCount(definition.Id) > 0)
                {
                    reservation = new EconomySeedReservation(definition.SeedRarity, definition.Id, definition.CropItemId);
                }
                else if (GetItemCount(PreferredCatalogSeed) > 0) return false;
                else PreferredCatalogSeed = null;
            }
            if (reservation == null && PreferredSeed >= 0 && PreferredSeed <= 2)
            {
                SeedRarity preferred = (SeedRarity)PreferredSeed;
                if (GetAvailableSeedCount(preferred) <= 0) return false;
                reservation = new EconomySeedReservation(preferred);
            }
            if (reservation == null)
            {
                foreach (SeedRarity rarity in new[] { SeedRarity.Magic, SeedRarity.Mineral, SeedRarity.Common })
                {
                    if (GetAvailableSeedCount(rarity) <= 0) continue;
                    reservation = new EconomySeedReservation(rarity);
                    break;
                }
            }
            if (reservation == null)
            {
                var seed = SurvivalItemCatalog.Seeds.FirstOrDefault(value => GetAvailableItemCount(value.Id) > 0);
                if (seed != null) reservation = new EconomySeedReservation(seed.SeedRarity, seed.Id, seed.CropItemId);
            }
            if (reservation == null) return false;
            seedReservations.Add(reservation);
            InventoryChanged?.Invoke();
            return true;
        }

        public bool CommitSeedReservation(EconomySeedReservation reservation)
        {
            if (reservation == null || !reservation.IsActive || !seedReservations.Remove(reservation)) return false;
            reservation.IsActive = false;
            return reservation.SeedItemId != null ? TryRemoveItem(reservation.SeedItemId, 1) : TryRemoveSeeds(reservation.Rarity, 1);
        }

        public void ReleaseSeedReservation(EconomySeedReservation reservation)
        {
            if (reservation == null || !seedReservations.Remove(reservation)) return;
            reservation.IsActive = false;
            InventoryChanged?.Invoke();
        }

        private void ClearSeedReservations()
        {
            foreach (EconomySeedReservation reservation in seedReservations) reservation.IsActive = false;
            seedReservations.Clear();
        }

        public int GetSeedCount(SeedRarity rarity)
        {
            switch (rarity)
            {
                case SeedRarity.Mineral:
                    return mineralSeeds;
                case SeedRarity.Magic:
                    return magicSeeds;
                default:
                    return commonSeeds;
            }
        }

        public void AddSeeds(int amount)
        {
            AddSeeds(SeedRarity.Common, amount);
        }

        public void AddSeeds(SeedRarity rarity, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            switch (rarity)
            {
                case SeedRarity.Mineral:
                    mineralSeeds = AddSafe(mineralSeeds, amount);
                    break;
                case SeedRarity.Magic:
                    magicSeeds = AddSafe(magicSeeds, amount);
                    break;
                default:
                    commonSeeds = AddSafe(commonSeeds, amount);
                    break;
            }

            InventoryChanged?.Invoke();
        }

        public bool TryRemoveSeeds(int amount)
        {
            return TryRemoveSeeds(SeedRarity.Common, amount);
        }

        public bool TryRemoveSeeds(SeedRarity rarity, int amount)
        {
            if (amount <= 0 || GetAvailableSeedCount(rarity) < amount)
            {
                return false;
            }

            switch (rarity)
            {
                case SeedRarity.Mineral:
                    if (mineralSeeds < amount) return false;
                    mineralSeeds -= amount;
                    break;
                case SeedRarity.Magic:
                    if (magicSeeds < amount) return false;
                    magicSeeds -= amount;
                    break;
                default:
                    if (commonSeeds < amount) return false;
                    commonSeeds -= amount;
                    break;
            }

            InventoryChanged?.Invoke();
            return true;
        }

        public void AddWood(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            wood = AddSafe(wood, amount);
            InventoryChanged?.Invoke();
        }

        public bool TryRemoveWood(int amount)
        {
            if (amount <= 0 || wood < amount)
            {
                return false;
            }

            wood -= amount;
            InventoryChanged?.Invoke();
            return true;
        }

        public bool TrySpendMaterials(int woodAmount, int stoneAmount)
        {
            if (woodAmount < 0 || stoneAmount < 0 || (woodAmount == 0 && stoneAmount == 0) ||
                wood < woodAmount || stone < stoneAmount) return false;
            wood -= woodAmount;
            stone -= stoneAmount;
            InventoryChanged?.Invoke();
            return true;
        }

        public void AddStone(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            stone = AddSafe(stone, amount);
            InventoryChanged?.Invoke();
        }

        public bool TryRemoveStone(int amount)
        {
            if (amount <= 0 || stone < amount)
            {
                return false;
            }

            stone -= amount;
            InventoryChanged?.Invoke();
            return true;
        }

        public void AddFruit(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            fruit = AddSafe(fruit, amount);
            InventoryChanged?.Invoke();
        }

        public bool TryRemoveFruit(int amount)
        {
            if (amount <= 0 || fruit < amount)
            {
                return false;
            }

            fruit -= amount;
            InventoryChanged?.Invoke();
            return true;
        }

        public void AddFood(int amount)
        {
            if (amount <= 0) return;
            food = AddSafe(food, amount);
            InventoryChanged?.Invoke();
        }

        public bool TryRemoveFood(int amount)
        {
            if (amount <= 0 || food < amount) return false;
            food -= amount;
            InventoryChanged?.Invoke();
            return true;
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            coins = AddSafe(coins, amount);
            InventoryChanged?.Invoke();
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount <= 0 || coins < amount)
            {
                return false;
            }

            coins -= amount;
            InventoryChanged?.Invoke();
            return true;
        }

        public void Restore(int savedSeeds, int savedWood, int savedStone, int savedFruit, int savedCoins)
        {
            Restore(savedSeeds, 0, 0, savedWood, savedStone, savedFruit, savedCoins, maxSeedsPerSlot);
        }

        public void Restore(
            int savedCommonSeeds,
            int savedMineralSeeds,
            int savedMagicSeeds,
            int savedWood,
            int savedStone,
            int savedFruit,
            int savedCoins,
            int savedMaxSeedsPerSlot,
            int savedFood = 0)
        {
            ClearSeedReservations();
            maxSeedsPerSlot = 99;
            commonSeeds = Mathf.Max(0, savedCommonSeeds);
            mineralSeeds = Mathf.Max(0, savedMineralSeeds);
            magicSeeds = Mathf.Max(0, savedMagicSeeds);
            wood = Mathf.Max(0, savedWood);
            stone = Mathf.Max(0, savedStone);
            fruit = Mathf.Max(0, savedFruit);
            food = Mathf.Max(0, savedFood);
            coins = Mathf.Max(0, savedCoins);
            InventoryChanged?.Invoke();
        }

        public void IncreaseSeedCapacity(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            maxSeedsPerSlot = 99;
            InventoryChanged?.Invoke();
        }
    }
}
