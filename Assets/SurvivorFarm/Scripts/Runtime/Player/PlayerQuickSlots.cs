using System;
using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    /// <summary>
    /// Five small, player-owned quick slots. The component stores ids only and
    /// delegates the actual use/equip operation to the existing inventory
    /// facade, so adding new food or equipment does not require changing this
    /// system.
    /// </summary>
    public sealed class PlayerQuickSlots : MonoBehaviour
    {
        public const int SlotCount = 5;
        private static readonly string[] DefaultSlots = { "Sword", "Food", "Fruit", "", "" };

        // New players start with the three useful defaults; saved games replace
        // these values through Restore without changing the five-slot contract.
        [SerializeField] private string[] slots = { "Sword", "Food", "Fruit", "", "" };

        public event Action Changed;
        public int Count => SlotCount;
        public IReadOnlyList<string> Slots => slots;

        private PlayerInventory Inventory => GetComponent<PlayerInventory>();
        private PlayerToolbelt Toolbelt => GetComponent<PlayerToolbelt>();

        private void Awake()
        {
            Normalize();
        }

        public string Get(int index)
        {
            return index >= 0 && index < SlotCount && slots != null ? slots[index] ?? string.Empty : string.Empty;
        }

        public bool Set(int index, string id)
        {
            if (index < 0 || index >= SlotCount) return false;
            if (!CanAssign(id)) return false;
            Normalize();
            string normalized = id ?? string.Empty;
            if (slots[index] == normalized) return true;
            slots[index] = normalized;
            Changed?.Invoke();
            Inventory?.NotifyInventoryChanged();
            return true;
        }

        public void Clear(int index)
        {
            if (index < 0 || index >= SlotCount) return;
            Normalize();
            if (string.IsNullOrEmpty(slots[index])) return;
            slots[index] = string.Empty;
            Changed?.Invoke();
            Inventory?.NotifyInventoryChanged();
        }

        public bool Use(int index)
        {
            string id = Get(index);
            if (string.IsNullOrEmpty(id) || Inventory == null) return false;

            EquipmentItems.Definition equipment = EquipmentItems.Find(id);
            if (equipment?.Weapon is FarmTool weapon)
            {
                if (!Inventory.OwnsEquipment(id) || Toolbelt == null) return false;
                Toolbelt.Select(weapon);
                return Toolbelt.SelectedTool == weapon;
            }

            return BackpackActions.Use(Inventory, id);
        }

        public bool CanAssign(string id)
        {
            if (string.IsNullOrEmpty(id)) return true;
            if (Inventory == null) return false;
            if (EquipmentItems.Find(id) is EquipmentItems.Definition equipment)
                return equipment.Weapon.HasValue && Inventory.OwnsEquipment(id);
            return id == "Food" || id == "Fruit" || SurvivalItemCatalog.IsFood(id);
        }

        public int Amount(string id)
        {
            if (Inventory == null || string.IsNullOrEmpty(id)) return 0;
            EquipmentItems.Definition equipment = EquipmentItems.Find(id);
            if (equipment?.Weapon.HasValue == true) return Inventory.OwnsEquipment(id) ? 1 : 0;
            return BackpackActions.Count(Inventory, id);
        }

        public string DisplayName(string id)
        {
            if (string.IsNullOrEmpty(id)) return "Vacío";
            if (EquipmentItems.Find(id) is EquipmentItems.Definition equipment) return equipment.Name;
            if (id == "Food") return "Ración";
            if (id == "Fruit") return "Fruta";
            return SurvivalItemCatalog.Find(id)?.Name ?? id;
        }

        public string Icon(string id)
        {
            if (EquipmentItems.Find(id) is EquipmentItems.Definition equipment) return equipment.Icon;
            if (id == "Food" || id == "Fruit") return id;
            return SurvivalItemCatalog.Find(id)?.Icon ?? id;
        }

        /// <summary>Returns currently owned foods and weapons in stable order.</summary>
        public IReadOnlyList<string> GetCandidates()
        {
            var result = new List<string>(16);
            if (Inventory == null) return result;

            foreach (EquipmentItems.Definition item in EquipmentItems.All)
            {
                if (item == null || !item.Weapon.HasValue || !Inventory.OwnsEquipment(item.Id)) continue;
                if (!result.Contains(item.Id)) result.Add(item.Id);
            }

            if (Inventory.Food > 0) result.Add("Food");
            if (Inventory.Fruit > 0) result.Add("Fruit");
            foreach (SurvivalItemCatalog.Definition item in SurvivalItemCatalog.All)
            {
                if (!item.IsFood || Inventory.GetItemCount(item.Id) <= 0 || result.Contains(item.Id)) continue;
                result.Add(item.Id);
            }
            return result;
        }

        public void Restore(string[] saved)
        {
            slots = new string[SlotCount];
            for (int i = 0; i < SlotCount; i++)
            {
                string id = saved != null && i < saved.Length ? saved[i] : DefaultSlots[i];
                slots[i] = CanAssign(id) ? id ?? string.Empty : string.Empty;
            }
            Changed?.Invoke();
        }

        private void Normalize()
        {
            if (slots == null || slots.Length != SlotCount)
            {
                string[] old = slots;
                slots = new string[SlotCount];
                for (int i = 0; i < SlotCount; i++)
                    slots[i] = old != null && i < old.Length ? old[i] ?? string.Empty : DefaultSlots[i];
            }
            for (int i = 0; i < slots.Length; i++)
                if (!CanAssign(slots[i])) slots[i] = string.Empty;
        }
    }
}
