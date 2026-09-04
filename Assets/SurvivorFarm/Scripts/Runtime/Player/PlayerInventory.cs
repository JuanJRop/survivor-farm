using System;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private int slotCount = 7;
        [SerializeField] private int commonSeeds;
        [SerializeField] private int mineralSeeds;
        [SerializeField] private int magicSeeds;
        [SerializeField] private int wood;
        [SerializeField] private int stone;
        [SerializeField] private int fruit;
        [SerializeField] private int coins;
        [SerializeField] private int maxSeedsPerSlot = 20;

        public int SlotCount => slotCount;
        public int Seeds => commonSeeds;
        public int CommonSeeds => commonSeeds;
        public int MineralSeeds => mineralSeeds;
        public int MagicSeeds => magicSeeds;
        public int TotalSeeds => commonSeeds + mineralSeeds + magicSeeds;
        public int Wood => wood;
        public int Stone => stone;
        public int Fruit => fruit;
        public int Coins => coins;
        public int MaxSeedsPerSlot => maxSeedsPerSlot;

        public event Action InventoryChanged;

        private void Start()
        {
            InventoryChanged?.Invoke();
        }

        public bool TryConsumeSeed()
        {
            SeedRarity ignored;
            return TryConsumeSeed(out ignored);
        }

        public bool TryConsumeSeed(out SeedRarity rarity)
        {
            if (magicSeeds > 0)
            {
                magicSeeds--;
                rarity = SeedRarity.Magic;
                InventoryChanged?.Invoke();
                return true;
            }

            if (mineralSeeds > 0)
            {
                mineralSeeds--;
                rarity = SeedRarity.Mineral;
                InventoryChanged?.Invoke();
                return true;
            }

            if (commonSeeds > 0)
            {
                commonSeeds--;
                rarity = SeedRarity.Common;
                InventoryChanged?.Invoke();
                return true;
            }

            rarity = SeedRarity.Common;
            return false;
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
                    mineralSeeds = Mathf.Min(mineralSeeds + amount, maxSeedsPerSlot);
                    break;
                case SeedRarity.Magic:
                    magicSeeds = Mathf.Min(magicSeeds + amount, maxSeedsPerSlot);
                    break;
                default:
                    commonSeeds = Mathf.Min(commonSeeds + amount, maxSeedsPerSlot);
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
            if (amount <= 0)
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

            wood += amount;
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

        public void AddStone(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            stone += amount;
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

            fruit += amount;
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

        public void AddCoins(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            coins += amount;
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
            int savedMaxSeedsPerSlot)
        {
            maxSeedsPerSlot = Mathf.Max(20, savedMaxSeedsPerSlot);
            commonSeeds = Mathf.Clamp(savedCommonSeeds, 0, maxSeedsPerSlot);
            mineralSeeds = Mathf.Clamp(savedMineralSeeds, 0, maxSeedsPerSlot);
            magicSeeds = Mathf.Clamp(savedMagicSeeds, 0, maxSeedsPerSlot);
            wood = Mathf.Max(0, savedWood);
            stone = Mathf.Max(0, savedStone);
            fruit = Mathf.Max(0, savedFruit);
            coins = Mathf.Max(0, savedCoins);
            InventoryChanged?.Invoke();
        }

        public void IncreaseSeedCapacity(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            maxSeedsPerSlot += amount;
            InventoryChanged?.Invoke();
        }
    }
}
