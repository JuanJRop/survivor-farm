using System;
using System.Collections.Generic;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    /// <summary>
    /// Converts carried, non-permanent resources into physical loot when the
    /// player dies. Equipment and town progress remain persistent so a death
    /// never leaves the player without a usable weapon or destroys the village.
    /// </summary>
    public sealed class PlayerDeathDrops : MonoBehaviour
    {
        private struct Drop
        {
            public ItemKind Kind;
            public int Amount;
            public Drop(ItemKind kind, int amount) { Kind = kind; Amount = amount; }
        }

        private readonly List<Drop> drops = new List<Drop>(24);
        private bool droppedForLife;
        private PlayerInventory inventory;

        public bool DroppedForCurrentLife => droppedForLife;
        public int LastDropCount { get; private set; }

        private void Awake() => inventory = GetComponent<PlayerInventory>();

        /// <summary>Called once from the death notification, before the death menu pauses time.</summary>
        public int DropAtDeathPosition()
        {
            if (droppedForLife) return LastDropCount;
            droppedForLife = true;
            LastDropCount = 0;
            inventory = inventory != null ? inventory : GetComponent<PlayerInventory>();
            if (inventory == null) return 0;

            drops.Clear();
            AddDrop(ItemKind.CommonSeed, inventory.CommonSeeds);
            AddDrop(ItemKind.MineralSeed, inventory.MineralSeeds);
            AddDrop(ItemKind.MagicSeed, inventory.MagicSeeds);
            AddDrop(ItemKind.Wood, inventory.Wood);
            AddDrop(ItemKind.Stone, inventory.Stone);
            AddDrop(ItemKind.Fruit, inventory.Fruit);
            AddDrop(ItemKind.Food, inventory.Food);
            AddDrop(ItemKind.Coins, inventory.Coins);

            // Iron is part of the adventure progression record rather than an
            // inventory stack, but it is still a carried resource on death.
            AdventureProgress adventure = inventory.GetComponent<AdventureProgress>();
            if (adventure != null) AddDrop(ItemKind.Iron, adventure.Data != null ? adventure.Data.iron : 0);

            // Catalog items are stored as public stacks. Snapshot the list before
            // removing from it because TryRemoveItem compacts depleted stacks.
            var stacks = inventory.ItemStacks != null
                ? new List<InventoryStack>(inventory.ItemStacks)
                : new List<InventoryStack>();
            foreach (InventoryStack stack in stacks)
            {
                if (stack == null || stack.count <= 0 || string.IsNullOrEmpty(stack.id)) continue;
                int amount = inventory.GetItemCount(stack.id);
                if (amount <= 0 || !inventory.TryRemoveItem(stack.id, amount)) continue;
                if (!TryMapCatalogItem(stack.id, out ItemKind kind)) continue;
                AddDrop(kind, amount);
            }

            // Any future catalog item that has no visual ground-loot mapping is
            // deliberately removed instead of silently surviving the reset.
            inventory.ItemStacks?.Clear();
            inventory.Restore(0, 0, 0, 0, 0, 0, 0, inventory.MaxSeedsPerSlot, 0);
            inventory.PreferredSeed = -1;
            inventory.PreferredCatalogSeed = null;
            inventory.BackpackOrder = Array.Empty<string>();
            PlayerQuickSlots quickSlots = inventory.GetComponent<PlayerQuickSlots>();
            if (quickSlots != null)
                for (int i = 0; i < quickSlots.Count; i++) quickSlots.Clear(i);
            if (adventure != null && adventure.Data != null)
            {
                adventure.Data.iron = 0;
                inventory.NotifyInventoryChanged();
            }

            Transform parent = ResolveLootParent();
            for (int i = 0; i < drops.Count; i++)
            {
                Drop drop = drops[i];
                EnemyLootPickup pickup = EnemyLootPickup.Scatter(
                    transform.position, parent, drop.Kind, drop.Amount, null,
                    i * 2.399963f);
                if (pickup != null) LastDropCount++;
            }

            if (LastDropCount > 0)
                FarmNotificationCenter.Show("Has perdido tus recursos. Regresa al lugar de tu caída para recuperarlos.");
            return LastDropCount;
        }

        public void ResetForNextLife()
        {
            droppedForLife = false;
            LastDropCount = 0;
            drops.Clear();
        }

        private void AddDrop(ItemKind kind, int amount)
        {
            if (amount <= 0) return;
            for (int i = 0; i < drops.Count; i++)
            {
                if (drops[i].Kind != kind) continue;
                Drop current = drops[i];
                current.Amount = (int)Math.Min(int.MaxValue, (long)current.Amount + amount);
                drops[i] = current;
                return;
            }
            drops.Add(new Drop(kind, amount));
        }

        private static bool TryMapCatalogItem(string id, out ItemKind kind)
        {
            kind = ItemKind.Food;
            if (SurvivalItemCatalog.IsSeed(id))
            {
                SurvivalItemCatalog.Definition seed = SurvivalItemCatalog.Find(id);
                kind = (ItemKind)((int)ItemKind.CommonSeed + (int)seed.SeedRarity);
                return true;
            }
            if (SurvivalItemCatalog.IsFood(id)) return true;
            switch (id)
            {
                case "GoldOre": kind = ItemKind.GoldOre; return true;
                case "Ruby": kind = ItemKind.Ruby; return true;
                case "Emerald": kind = ItemKind.Emerald; return true;
                case "Diamond": kind = ItemKind.Diamond; return true;
                case "RubyShard":
                case "SapphireShard":
                case "TopazShard":
                case "AmethystShard":
                case "DiamondShard": kind = ItemKind.EmeraldShard; return true;
                case "EmeraldShard": kind = ItemKind.EmeraldShard; return true;
                case "FireEssence":
                case "WaterEssence":
                case "WindEssence":
                case "NatureEssence":
                case "LightEssence":
                case "ShadowEssence": kind = ItemKind.EarthEssence; return true;
                case "EarthEssence": kind = ItemKind.EarthEssence; return true;
                case "Arrow": kind = ItemKind.Arrow; return true;
                case "Leather": kind = ItemKind.Leather; return true;
                // Saddle is a reusable crafted object and has no separate
                // ground-loot enum yet; leather is its recoverable material.
                case "Saddle": kind = ItemKind.Leather; return true;
                default: return false;
            }
        }

        private Transform ResolveLootParent()
        {
            DungeonEntrance dungeonEntrance = FindFirstObjectByType<DungeonEntrance>(FindObjectsInactive.Include);
            if (dungeonEntrance != null && dungeonEntrance.IsInsideDungeon)
            {
                DungeonEnemyPool dungeon = FindFirstObjectByType<DungeonEnemyPool>(FindObjectsInactive.Include);
                if (dungeon != null) return dungeon.transform;
            }
            OutdoorEnemyPool outside = FindFirstObjectByType<OutdoorEnemyPool>(FindObjectsInactive.Include);
            return outside != null ? outside.transform : transform.parent;
        }
    }
}
