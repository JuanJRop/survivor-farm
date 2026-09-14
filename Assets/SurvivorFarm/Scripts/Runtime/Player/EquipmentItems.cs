using System;
using SurvivorFarm.Runtime.Gameplay;

namespace SurvivorFarm.Runtime.Player
{
    public static class EquipmentItems
    {
        public sealed class Definition
        {
            public string Id, Name, Icon;
            public int Slot;
            public FarmTool? Weapon;
            public float DefenseBonus;
            public float SpeedBonus;
            public float HungerSave;
            public int DamageBonus;
            public int FoodHealingBonus;
            public int FoodHungerBonus;
            public bool Fits(int slot) => Slot == slot || Slot == 6 && slot == 7;
        }
        public static readonly string[] SlotNames = { "Casco", "Pechera", "Botas", "Arma", "Escudo", "Gema", "Accesorio", "Accesorio especial" };
        public static readonly Definition[] All = {
            new Definition { Id="Sword", Name="Espada", Icon="Sword", Slot=3, Weapon=FarmTool.Sword },
            new Definition { Id="Bow", Name="Arco", Icon="Bow", Slot=3, Weapon=FarmTool.Bow },
            new Definition { Id="IronSword", Name="Espada de hierro", Icon="Sword", Slot=3, Weapon=FarmTool.Sword, DamageBonus=2 },
            new Definition { Id="HunterBow", Name="Arco de cazador", Icon="Bow", Slot=3, Weapon=FarmTool.Bow, DamageBonus=2, SpeedBonus=.03f },
            new Definition { Id="RubySword", Name="Espada de rubi", Icon="Sword", Slot=3, Weapon=FarmTool.Sword, DamageBonus=4 },
            new Definition { Id="DiamondBow", Name="Arco de diamante", Icon="Bow", Slot=3, Weapon=FarmTool.Bow, DamageBonus=4, SpeedBonus=.04f },
            new Definition { Id="Helmet", Name="Casco simple", Icon="Helmet", Slot=0, DefenseBonus=.15f },
            new Definition { Id="Chestplate", Name="Pechera simple", Icon="Chestplate", Slot=1, DefenseBonus=.25f },
            new Definition { Id="Boots", Name="Botas simples", Icon="Boots", Slot=2, SpeedBonus=.15f },
            new Definition { Id="CopperHelmet", Name="Casco de cobre", Icon="Helmet", Slot=0, DefenseBonus=.18f },
            new Definition { Id="CopperChestplate", Name="Pechera de cobre", Icon="Chestplate", Slot=1, DefenseBonus=.28f },
            new Definition { Id="CopperBoots", Name="Botas de cobre", Icon="Boots", Slot=2, DefenseBonus=.04f, SpeedBonus=.12f },
            new Definition { Id="IronHelmet", Name="Casco de hierro", Icon="Helmet", Slot=0, DefenseBonus=.22f },
            new Definition { Id="IronChestplate", Name="Pechera de hierro", Icon="Chestplate", Slot=1, DefenseBonus=.34f },
            new Definition { Id="IronBoots", Name="Botas de hierro", Icon="Boots", Slot=2, DefenseBonus=.08f, SpeedBonus=.08f },
            new Definition { Id="GoldHelmet", Name="Casco dorado", Icon="Helmet", Slot=0, DefenseBonus=.26f },
            new Definition { Id="GoldChestplate", Name="Pechera dorada", Icon="Chestplate", Slot=1, DefenseBonus=.40f },
            new Definition { Id="GoldBoots", Name="Botas doradas", Icon="Boots", Slot=2, DefenseBonus=.10f, SpeedBonus=.10f },
            new Definition { Id="RangerHood", Name="Capucha de explorador", Icon="Helmet", Slot=0, DefenseBonus=.12f, SpeedBonus=.04f },
            new Definition { Id="RangerVest", Name="Chaleco de explorador", Icon="Chestplate", Slot=1, DefenseBonus=.20f, SpeedBonus=.05f },
            new Definition { Id="RangerBoots", Name="Botas de explorador", Icon="Boots", Slot=2, SpeedBonus=.20f },
            new Definition { Id="MysticHood", Name="Capucha mistica", Icon="Helmet", Slot=0, DefenseBonus=.10f, FoodHealingBonus=1 },
            new Definition { Id="MysticRobe", Name="Tunica mistica", Icon="Chestplate", Slot=1, DefenseBonus=.18f, FoodHealingBonus=1 },
            new Definition { Id="MysticBoots", Name="Botas misticas", Icon="Boots", Slot=2, DefenseBonus=.04f, SpeedBonus=.16f, FoodHealingBonus=1 },
            new Definition { Id="Shield", Name="Escudo", Icon="Slot", Slot=4, DefenseBonus=.10f },
            new Definition { Id="WoodenShield", Name="Escudo de madera", Icon="Slot", Slot=4, DefenseBonus=.08f },
            new Definition { Id="IronShield", Name="Escudo de hierro", Icon="Slot", Slot=4, DefenseBonus=.16f },
            new Definition { Id="EmeraldShield", Name="Escudo esmeralda", Icon="Slot", Slot=4, DefenseBonus=.24f, FoodHealingBonus=1 },
            new Definition { Id="Gem", Name="Gema azul", Icon="Gem", Slot=5, DamageBonus=1 },
            new Definition { Id="RubyGem", Name="Gema rubi", Icon="Gem", Slot=5, DamageBonus=1 },
            new Definition { Id="SapphireGem", Name="Gema zafiro", Icon="Gem", Slot=5, FoodHealingBonus=1 },
            new Definition { Id="EmeraldGem", Name="Gema esmeralda", Icon="Gem", Slot=5, DefenseBonus=.05f },
            new Definition { Id="TopazGem", Name="Gema topacio", Icon="Gem", Slot=5, SpeedBonus=.05f },
            new Definition { Id="AmethystGem", Name="Gema amatista", Icon="Gem", Slot=5, FoodHealingBonus=1 },
            new Definition { Id="DiamondGem", Name="Gema diamante", Icon="Gem", Slot=5, DefenseBonus=.04f, DamageBonus=1, SpeedBonus=.03f },
            new Definition { Id="Ring", Name="Anillo", Icon="Ring", Slot=6, FoodHealingBonus=1 },
            new Definition { Id="Amulet", Name="Amuleto", Icon="Amulet", Slot=6, FoodHealingBonus=1 },
            new Definition { Id="DiamondAmulet", Name="Amuleto de diamante", Icon="Amulet", Slot=6, DefenseBonus=.06f, DamageBonus=1, FoodHealingBonus=1 },
            new Definition { Id="FireElement", Name="Elemento fuego", Icon="Gem", Slot=7, DamageBonus=1 },
            new Definition { Id="WaterElement", Name="Elemento agua", Icon="Gem", Slot=7, FoodHealingBonus=1 },
            new Definition { Id="EarthElement", Name="Elemento tierra", Icon="Gem", Slot=7, DefenseBonus=.06f },
            new Definition { Id="WindElement", Name="Elemento viento", Icon="Gem", Slot=7, SpeedBonus=.07f },
            new Definition { Id="NatureElement", Name="Elemento naturaleza", Icon="Gem", Slot=7, FoodHealingBonus=1 },
            new Definition { Id="LightElement", Name="Elemento luz", Icon="Gem", Slot=7, DefenseBonus=.04f, FoodHealingBonus=1 },
            new Definition { Id="ShadowElement", Name="Elemento sombra", Icon="Gem", Slot=7, DamageBonus=1, SpeedBonus=.02f }
        };
        public static int DamageBonusFor(PlayerInventory inventory, FarmTool tool)
        {
            if (inventory == null) return 0;
            int bonus = inventory.EquipmentDamage;
            // Keep the inventory's shared bonuses; only the other weapon is inactive.
            foreach (string id in inventory.EquippedEquipment ?? Array.Empty<string>())
            {
                Definition item = Find(id);
                if (item != null && item.Weapon.HasValue && item.Weapon.Value != tool)
                    bonus -= item.DamageBonus;
            }
            return bonus;
        }

        public static string Effect(string id) => Effect(Find(id));
        private static string Effect(Definition item)
        {
            if (item == null) return string.Empty;
            string value = string.Empty;
            if (item.DefenseBonus != 0f) value += $"Defensa {(int)Math.Round(item.DefenseBonus * 100f)}% ";
            if (item.SpeedBonus != 0f) value += $"Velocidad {(item.SpeedBonus > 0 ? "+" : "")}{(int)Math.Round(item.SpeedBonus * 100f)}% ";
            if (item.DamageBonus != 0) value += $"{(item.Weapon == FarmTool.Sword ? "Espada" : item.Weapon == FarmTool.Bow ? "Arco" : "Dano")} +{item.DamageBonus} ";
            if (item.FoodHealingBonus != 0) value += $"Comer cura +{item.FoodHealingBonus} ";
            return value.Trim();
        }
        public static Definition Find(string id) => Array.Find(All, d => d.Id == id);
    }
}
