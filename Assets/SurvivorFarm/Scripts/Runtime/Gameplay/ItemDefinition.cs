using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    // Append new values: existing item assets and save files store these numeric IDs.
    public enum ItemKind { Wood, Stone, Fruit, Food, Coins, CommonSeed, MineralSeed, MagicSeed, Experience, Iron, GoldOre, Ruby, Emerald, Diamond, EmeraldShard, EarthEssence, Arrow, Leather, Bow }

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
                case ItemKind.Experience:
                    var mastery = inventory.GetComponent<ToolMastery>();
                    if (mastery == null) mastery = inventory.gameObject.AddComponent<ToolMastery>();
                    var weapon = inventory.GetComponent<PlayerToolbelt>()?.SelectedTool == FarmTool.Bow ? FarmTool.Bow : FarmTool.Sword;
                    for (int i = 0; i < amount; i++) mastery.Earn(weapon);
                    break;
                case ItemKind.Iron:
                    var adventure = inventory.GetComponent<AdventureProgress>();
                    if (adventure == null) adventure = inventory.gameObject.AddComponent<AdventureProgress>();
                    adventure.AddIron(amount);
                    break;
                case ItemKind.GoldOre: inventory.AddItem("GoldOre", amount); break;
                case ItemKind.Ruby: inventory.AddItem("Ruby", amount); break;
                case ItemKind.Emerald: inventory.AddItem("Emerald", amount); break;
                case ItemKind.Diamond: inventory.AddItem("Diamond", amount); break;
                case ItemKind.EmeraldShard: inventory.AddItem("EmeraldShard", amount); break;
                case ItemKind.EarthEssence: inventory.AddItem("EarthEssence", amount); break;
                case ItemKind.Arrow: inventory.AddItem("Arrow", amount); break;
                case ItemKind.Leather: inventory.AddItem("Leather", amount); break;
                case ItemKind.Bow:
                    inventory.AddEquipment("Bow");
                    UI.FarmNotificationCenter.Show("Arco desbloqueado · 2 para equipar · apunta con el cursor. Fabrica flechas con F.");
                    break;
            }
        }
    }
}
