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

        public event Action<FarmTool> ToolChanged;

        private static readonly FarmTool[] Tools =
        {
            FarmTool.Sword,
            FarmTool.Bow,
            FarmTool.Axe,
            FarmTool.Pickaxe,
            FarmTool.Hoe,
            FarmTool.Shovel,
            FarmTool.WateringCan
        };

        private void Start()
        {
            if (Array.IndexOf(Tools, selectedTool) < 0)
            {
                selectedTool = FarmTool.Sword;
            }

            NotifyToolChanged();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                Select(FarmTool.Sword);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                Select(FarmTool.Bow);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                Select(FarmTool.Axe);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                Select(FarmTool.Pickaxe);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                Select(FarmTool.Hoe);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                Select(FarmTool.Shovel);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                Select(FarmTool.WateringCan);
            }
        }

        public void Select(FarmTool tool)
        {
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
                    return "Pala";
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
