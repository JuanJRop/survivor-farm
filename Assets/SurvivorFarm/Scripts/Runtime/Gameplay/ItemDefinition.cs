using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public enum ItemKind { Wood, Stone, Fruit, Food, Coins, CommonSeed, MineralSeed, MagicSeed }

    [CreateAssetMenu(menuName = "Survivor Farm/Flyweights/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private ItemKind kind;
        [SerializeField] private string displayName;
        public ItemKind Kind => kind;
        public string DisplayName => displayName;

        internal void Initialize(ItemKind itemKind, string label)
        {
            kind = itemKind;
            displayName = label;
        }

        public void Grant(PlayerInventory inventory, int amount)
        {
            if (inventory == null || amount <= 0) return;
            switch (kind)
            {
                case ItemKind.Wood: inventory.AddWood(amount); break;
                case ItemKind.Stone: inventory.AddStone(amount); break;
                case ItemKind.Fruit: inventory.AddFruit(amount); break;
                case ItemKind.Food: inventory.AddFood(amount); break;
                case ItemKind.Coins: inventory.AddCoins(amount); break;
                case ItemKind.CommonSeed: inventory.AddSeeds(SeedRarity.Common, amount); break;
                case ItemKind.MineralSeed: inventory.AddSeeds(SeedRarity.Mineral, amount); break;
                case ItemKind.MagicSeed: inventory.AddSeeds(SeedRarity.Magic, amount); break;
            }
        }
    }
}
