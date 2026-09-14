using System;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    public sealed class PlayerToolUpgradeController : MonoBehaviour
    {
        private const int MaxToolLevel = 3;

        [SerializeField] private int axeLevel = 1;
        [SerializeField] private int pickaxeLevel = 1;
        [SerializeField] private int shovelLevel = 1;

        private PlayerInventory inventory;

        public int AxeLevel => axeLevel;
        public int PickaxeLevel => pickaxeLevel;
        public int ShovelLevel => shovelLevel;

        public event Action ToolUpgradesChanged;

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
        }

        public int GetToolLevel(FarmTool tool)
        {
            switch (tool)
            {
                case FarmTool.Axe:
                    return axeLevel;
                case FarmTool.Pickaxe:
                    return pickaxeLevel;
                case FarmTool.Hoe:
                case FarmTool.Shovel:
                    return shovelLevel;
                default:
                    return 1;
            }
        }

        public int GetResourceBonus(FarmTool tool)
        {
            return Mathf.Max(0, GetToolLevel(tool) - 1);
        }

        public float GetDigDurationMultiplier()
        {
            return Mathf.Lerp(1f, 0.62f, (shovelLevel - 1) / 2f);
        }

        public float GetSeedFindChanceBonus()
        {
            return 0.12f * Mathf.Max(0, shovelLevel - 1);
        }

        public void UpgradeAxe()
        {
            TryUpgrade(FarmTool.Axe);
        }

        public void UpgradePickaxe()
        {
            TryUpgrade(FarmTool.Pickaxe);
        }

        public void UpgradeShovel()
        {
            UpgradeHoe();
        }

        public void UpgradeHoe()
        {
            TryUpgrade(FarmTool.Hoe);
        }

        public bool TryUpgrade(FarmTool tool)
        {
            if (tool != FarmTool.Axe && tool != FarmTool.Pickaxe) return false;
            int currentLevel = GetToolLevel(tool);
            if (currentLevel >= MaxToolLevel)
            {
                FarmNotificationCenter.Show($"{PlayerToolbelt.GetDisplayName(tool)} ya esta al maximo.");
                return false;
            }

            if (inventory == null)
            {
                return false;
            }

            UpgradeCost cost = GetUpgradeCost(currentLevel + 1);
            if (inventory.Coins < cost.coins || inventory.Wood < cost.wood || inventory.Stone < cost.stone)
            {
                FarmNotificationCenter.Show($"Falta material: {cost.coins} oro, {cost.wood} madera, {cost.stone} piedra.");
                return false;
            }

            inventory.TrySpendCoins(cost.coins);
            inventory.TryRemoveWood(cost.wood);
            inventory.TryRemoveStone(cost.stone);
            SetToolLevel(tool, currentLevel + 1);
            FarmNotificationCenter.Show($"{PlayerToolbelt.GetDisplayName(tool)} subio a nivel {currentLevel + 1}.");
            return true;
        }

        public string GetUpgradeSummary()
        {
            return $"Hacha Nv.{axeLevel} | Pico Nv.{pickaxeLevel}";
        }

        public bool GetNextCost(FarmTool tool, out int wood, out int stone, out int coins)
        {
            if (tool != FarmTool.Axe && tool != FarmTool.Pickaxe) { wood=stone=coins=0; return false; }
            int level=GetToolLevel(tool);var cost=GetUpgradeCost(level+1);
            wood=cost.wood;stone=cost.stone;coins=cost.coins;return level<MaxToolLevel;
        }

        public string GetNextCostText(FarmTool tool)
        {
            int currentLevel = GetToolLevel(tool);
            if (currentLevel >= MaxToolLevel)
            {
                return "Maximo";
            }

            UpgradeCost cost = GetUpgradeCost(currentLevel + 1);
            return $"{cost.coins} oro {cost.wood} mad {cost.stone} pie";
        }

        public void Restore(int savedAxeLevel, int savedPickaxeLevel, int savedShovelLevel)
        {
            axeLevel = Mathf.Clamp(savedAxeLevel, 1, MaxToolLevel);
            pickaxeLevel = Mathf.Clamp(savedPickaxeLevel, 1, MaxToolLevel);
            shovelLevel = Mathf.Clamp(savedShovelLevel, 1, MaxToolLevel);
            ToolUpgradesChanged?.Invoke();
        }

        private void SetToolLevel(FarmTool tool, int level)
        {
            int clampedLevel = Mathf.Clamp(level, 1, MaxToolLevel);
            switch (tool)
            {
                case FarmTool.Axe:
                    axeLevel = clampedLevel;
                    break;
                case FarmTool.Pickaxe:
                    pickaxeLevel = clampedLevel;
                    break;
                case FarmTool.Hoe:
                case FarmTool.Shovel:
                    shovelLevel = clampedLevel;
                    break;
            }

            ToolUpgradesChanged?.Invoke();
        }

        private static UpgradeCost GetUpgradeCost(int targetLevel)
        {
            return targetLevel == 2
                ? new UpgradeCost(15, 4, 4)
                : new UpgradeCost(35, 8, 8);
        }

        private struct UpgradeCost
        {
            public readonly int coins;
            public readonly int wood;
            public readonly int stone;

            public UpgradeCost(int coins, int wood, int stone)
            {
                this.coins = coins;
                this.wood = wood;
                this.stone = stone;
            }
        }
    }
}
