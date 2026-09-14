using System.Collections.Generic;
using System.Globalization;
using SurvivorFarm.Runtime.Player;

namespace SurvivorFarm.Runtime.UI
{
    public static class UiEquipmentComparison
    {
        public static int TargetSlot(string[] equipped, EquipmentItems.Definition item, int filter)
        {
            if (filter >= 0) return filter;
            if (item.Slot == 6 && equipped != null && equipped.Length > 7 &&
                !string.IsNullOrEmpty(equipped[6]) && equipped[6] != item.Id && string.IsNullOrEmpty(equipped[7])) return 7;
            return item.Slot;
        }

        // Piece bonuses, not final damage or capped player statistics; those belong to gameplay.
        public static string Differences(EquipmentItems.Definition current, EquipmentItems.Definition candidate)
        {
            var lines = new List<string>();
            Add(lines, "Defensa", (candidate?.DefenseBonus ?? 0) - (current?.DefenseBonus ?? 0), 100, " pp");
            Add(lines, "Velocidad", (candidate?.SpeedBonus ?? 0) - (current?.SpeedBonus ?? 0), 100, "%");
            Add(lines, "Da\u00f1o", (candidate?.DamageBonus ?? 0) - (current?.DamageBonus ?? 0));
            Add(lines, "Vida al comer", (candidate?.FoodHealingBonus ?? 0) - (current?.FoodHealingBonus ?? 0));
            return lines.Count == 0 ? "Sin cambios en bonificaciones" : string.Join("\n", lines);
        }

        private static void Add(List<string> lines, string label, float difference, int multiplier = 1, string unit = "")
        {
            int value = (int)System.Math.Round(difference * multiplier);
            if (value == 0) return;
            string color = value > 0 ? "#97E4AB" : "#FF9B92";
            lines.Add(label + " <color=" + color + ">" + (value > 0 ? "+" : "") + value.ToString(CultureInfo.InvariantCulture) + unit + "</color>");
        }
    }
}
