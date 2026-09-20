using System;
using UnityEngine;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.UI;

namespace SurvivorFarm.Runtime.Player
{
    public sealed class PlayerToolbelt : MonoBehaviour
    {
        [SerializeField] private FarmTool selectedTool = FarmTool.Sword;

        public FarmTool SelectedTool => selectedTool;
        public bool BowUnlocked => GetComponent<PlayerInventory>() != null && GetComponent<PlayerInventory>().OwnsEquipment("Bow");

        public event Action<FarmTool> ToolChanged;

        private static readonly FarmTool[] Tools =
        {
            FarmTool.Sword,
            FarmTool.Bow
        };

        private void Start()
        {
            if (Array.IndexOf(Tools, selectedTool) < 0 || selectedTool == FarmTool.Bow && !BowUnlocked)
            {
                selectedTool = FarmTool.Sword;
            }

            NotifyToolChanged();
        }

        private void Update()
        {
            if (SurvivorFarm.Runtime.UI.InventoryPanelSystem.IsOpen || FarmIntroduction.IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Select(FarmTool.Sword);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Select(FarmTool.Bow);
            }
        }

        public void Select(FarmTool tool)
        {
            if (tool == FarmTool.Bow && !BowUnlocked) return;
            if(FarmIntroduction.IsOpen&&tool!=FarmTool.Sword)return;
            var inventory = GetComponent<PlayerInventory>();
            if (inventory != null && (tool == FarmTool.Sword || tool == FarmTool.Bow))
            {
                string equippedWeapon = inventory.EquippedEquipment != null && inventory.EquippedEquipment.Length > 3
                    ? inventory.EquippedEquipment[3]
                    : string.Empty;
                var equippedDefinition = EquipmentItems.Find(equippedWeapon);
                if (equippedDefinition == null || equippedDefinition.Weapon != tool)
                {
                    string fallbackId = tool == FarmTool.Sword ? "Sword" : "Bow";
                    var ownedWeapon = inventory.OwnsEquipment(fallbackId)
                        ? EquipmentItems.Find(fallbackId)
                        : Array.Find(EquipmentItems.All, item => item != null && item.Weapon == tool && inventory.OwnsEquipment(item.Id));
                    if (ownedWeapon == null) return;
                    if (inventory.EquippedEquipment[3] != ownedWeapon.Id) { inventory.Equip(ownedWeapon.Id,3); return; }
                }
            }
            if (Array.IndexOf(Tools, tool) < 0)
            {
                return;
            }

            if (selectedTool == tool)
            {
                return;
            }

            selectedTool = tool;
            NotifyToolChanged();
        }

        public void SelectNext()
        {
            SelectByOffset(1);
        }

        public void SelectPrevious()
        {
            SelectByOffset(-1);
        }

        public static string GetDisplayName(FarmTool tool)
        {
            switch (tool)
            {
                case FarmTool.Sword:
                    return "Espada";
                case FarmTool.Bow:
                    return "Arco";
                case FarmTool.Axe:
                    return "Hacha";
                case FarmTool.Pickaxe:
                    return "Pico";
                case FarmTool.Hoe:
                    return "Azada";
                case FarmTool.Shovel:
                    return "Azada";
                case FarmTool.WateringCan:
                    return "Regadera";
                default:
                    return tool.ToString();
            }
        }

        private void NotifyToolChanged()
        {
            ToolChanged?.Invoke(selectedTool);
            FarmNotificationCenter.SetTool(GetDisplayName(selectedTool));
        }

        private void SelectByOffset(int offset)
        {
            int index = Array.IndexOf(Tools, selectedTool);
            if (index < 0)
            {
                index = 0;
            }

            int nextIndex = (index + offset + Tools.Length) % Tools.Length;
            Select(Tools[nextIndex]);
        }
    }
}
