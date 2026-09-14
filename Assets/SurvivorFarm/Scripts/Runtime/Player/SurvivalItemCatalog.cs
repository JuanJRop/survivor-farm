using System;
using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;

namespace SurvivorFarm.Runtime.Player
{
    public enum SurvivalItemCategory
    {
        Misc = 1,
        Food = 3,
        Material = 4,
        Seed = 5,
        Gem = 6,
        Element = 7
    }

    public static class SurvivalItemCatalog
    {
        public sealed class Definition
        {
            public string Id, Name, Icon, Description, CropItemId;
            public SurvivalItemCategory Category;
            public int Price, HungerRestore, HealRestore, HarvestYield = 1;
            public int SellPrice => Price;
            public int BuyPrice => (int)Math.Min(int.MaxValue, ((long)Math.Max(0, Price) * 3 + 1) / 2);
            public float GrowthSeconds;
            public string CropRole;
            public int UnlockMealsCooked;
            public SeedRarity SeedRarity;
            public bool IsFood, IsSeed, IsGem, IsElement;
        }

        private static readonly Definition[] definitions =
        {
            Food("Asparagus", "Esparrago", 4, 8),
            Food("Broccoli", "Brocoli", 5, 10),
            Food("Cabbage", "Repollo", 5, 10),
            Food("Carrot", "Zanahoria", 4, 8),
            Food("Cauliflower", "Coliflor", 6, 12),
            Food("Onion", "Cebolla", 4, 7),
            Food("Parsnip", "Chirivia", 4, 7),
            Food("Potato", "Patata", 5, 11),
            Food("Rice", "Arroz", 5, 9),
            Food("SpringOnion", "Cebolla tierna", 3, 6),
            Food("Strawberry", "Fresa", 7, 12),
            Food("AdzukiBean", "Judia adzuki", 5, 9),
            Food("Aloe", "Aloe", 4, 4, 1),
            Food("BellPepper", "Pimiento", 5, 9),
            Food("Blackberry", "Mora", 5, 8),
            Food("Corn", "Maiz", 6, 10),
            Food("Cucumber", "Pepino", 5, 9),
            Food("Eggplant", "Berenjena", 6, 10),
            Food("GreenBeans", "Judias verdes", 5, 9),
            Food("HotPepper", "Chile", 6, 8),
            Food("Melon", "Melon", 8, 14),
            Food("Pineapple", "Pina", 9, 15),
            Food("Sunflower", "Girasol", 6, 6),
            Food("Tomato", "Tomate", 5, 10),
            Food("Watermelon", "Sandia", 10, 18),
            Food("Wheat", "Trigo", 4, 7),
            Food("Beetroot", "Remolacha", 5, 9),
            Food("Grapes", "Uvas", 7, 12),
            Food("Pumpkin", "Calabaza", 9, 16),
            Food("Apple", "Manzana", 6, 10),
            Food("Apricot", "Albaricoque", 6, 10),
            Food("Banana", "Banana", 7, 12),
            Food("Cherry", "Cereza", 7, 11),
            Food("Coconut", "Coco", 8, 12),
            Food("Mango", "Mango", 8, 14),
            Food("Orange", "Naranja", 7, 12),
            Food("Peach", "Melocoton", 7, 12),

            Seed("AsparagusSeeds", "Semilla de esparrago", "Asparagus", "Asparagus", 3, SeedRarity.Common),
            Seed("BroccoliSeeds", "Semilla de brocoli", "Broccoli", "Broccoli", 3, SeedRarity.Common),
            Seed("CabbageSeeds", "Semilla de repollo", "Cabbage", "Cabbage", 3, SeedRarity.Common),
            Seed("CarrotSeeds", "Semilla de zanahoria", "Carrot", "Carrot", 2, SeedRarity.Common),
            Seed("CauliflowerSeeds", "Semilla de coliflor", "Cauliflower", "Cauliflower", 4, SeedRarity.Mineral),
            Seed("OnionSeeds", "Semilla de cebolla", "Onion", "Onion", 2, SeedRarity.Common),
            Seed("ParsnipSeeds", "Semilla de chirivia", "Parsnip", "Parsnip", 2, SeedRarity.Common),
            Seed("PotatoSeeds", "Semilla de patata", "Potato", "Potato", 3, SeedRarity.Common),
            Seed("RiceSeeds", "Semilla de arroz", "Rice", "Rice", 3, SeedRarity.Common),
            Seed("SpringOnionSeeds", "Semilla de cebolla tierna", "SpringOnion", "SpringOnion", 2, SeedRarity.Common),
            Seed("StrawberrySeeds", "Semilla de fresa", "Strawberry", "Strawberry", 5, SeedRarity.Mineral),
            Seed("AdzukiBeanSeeds", "Semilla de judia adzuki", "AdzukiBean", "AdzukiBean", 3, SeedRarity.Common),
            Seed("AloeSeeds", "Semilla de aloe", "Aloe", "Aloe", 4, SeedRarity.Mineral),
            Seed("BellPepperSeeds", "Semilla de pimiento", "BellPepper", "BellPepper", 3, SeedRarity.Common),
            Seed("BlackberrySeeds", "Semilla de mora", "Blackberry", "Blackberry", 4, SeedRarity.Mineral),
            Seed("CornSeeds", "Semilla de maiz", "Corn", "Corn", 4, SeedRarity.Mineral),
            Seed("CucumberSeeds", "Semilla de pepino", "Cucumber", "Cucumber", 3, SeedRarity.Common),
            Seed("EggplantSeeds", "Semilla de berenjena", "Eggplant", "Eggplant", 4, SeedRarity.Mineral),
            Seed("GreenBeansSeeds", "Semilla de judias verdes", "GreenBeans", "GreenBeans", 3, SeedRarity.Common),
            Seed("HotPepperSeeds", "Semilla de chile", "HotPepper", "HotPepper", 4, SeedRarity.Mineral),
            Seed("MelonSeeds", "Semilla de melon", "Melon", "Melon", 6, SeedRarity.Magic),
            Seed("PineappleSeeds", "Semilla de pina", "Pineapple", "Pineapple", 7, SeedRarity.Magic),
            Seed("SunflowerSeeds", "Semilla de girasol", "Sunflower", "Sunflower", 4, SeedRarity.Mineral),
            Seed("TomatoSeeds", "Semilla de tomate", "Tomato", "Tomato", 3, SeedRarity.Common),
            Seed("WatermelonSeeds", "Semilla de sandia", "Watermelon", "Watermelon", 8, SeedRarity.Magic),
            Seed("WheatSeeds", "Semilla de trigo", "Wheat", "Wheat", 2, SeedRarity.Common),
            Seed("BeetrootSeeds", "Semilla de remolacha", "Beetroot", "Beetroot", 3, SeedRarity.Common),
            Seed("GrapesSeeds", "Semilla de uvas", "Grapes", "Grapes", 5, SeedRarity.Mineral),
            Seed("PumpkinSeeds", "Semilla de calabaza", "Pumpkin", "Pumpkin", 7, SeedRarity.Magic),
            Seed("AppleSapling", "Brote de manzano", "Apple", "Apple", 7, SeedRarity.Magic, 2),
            Seed("ApricotSapling", "Brote de albaricoque", "Apricot", "Apricot", 7, SeedRarity.Magic, 2),
            Seed("BananaSapling", "Brote de banano", "Banana", "Banana", 8, SeedRarity.Magic, 2),
            Seed("CherrySapling", "Brote de cerezo", "Cherry", "Cherry", 8, SeedRarity.Magic, 2),
            Seed("CoconutSapling", "Brote de cocotero", "Coconut", "Coconut", 9, SeedRarity.Magic, 2),
            Seed("MangoSapling", "Brote de mango", "Mango", "Mango", 9, SeedRarity.Magic, 2),
            Seed("OrangeSapling", "Brote de naranjo", "Orange", "Orange", 8, SeedRarity.Magic, 2),
            Seed("PeachSapling", "Brote de melocoton", "Peach", "Peach", 8, SeedRarity.Magic, 2),

            MaterialItem("GoldOre", "Pepita de oro", "Coin", 24, "Metal precioso de vetas ricas. Sirve para canjear y fabricar equipo avanzado."),
            GemItem("Ruby", "Rubi", 70, "Gema roja completa. Material para armas agresivas y canjes caros."),
            GemItem("Emerald", "Esmeralda", 80, "Gema verde completa. Material para defensas y equipo de naturaleza."),
            GemItem("Diamond", "Diamante", 120, "Gema rara completa. Material para equipo de categoria alta."),
            GemItem("RubyShard", "Rubi menor", 18, "Material para equipo de fuego. Valor: +1 dano cuando se monta en una gema."),
            GemItem("SapphireShard", "Zafiro menor", 18, "Material para equipo de agua. Valor: +4 saciedad en comidas preparadas."),
            GemItem("EmeraldShard", "Esmeralda menor", 20, "Material para equipo de naturaleza. Valor: +5% resistencia."),
            GemItem("TopazShard", "Topacio menor", 16, "Material para equipo ligero. Valor: +5% velocidad."),
            GemItem("AmethystShard", "Amatista menor", 22, "Material para equipo arcano. Valor: +1 cura al comer."),
            GemItem("DiamondShard", "Diamante menor", 28, "Material para equipo raro. Valor: bonos equilibrados."),

            ElementItem("FireEssence", "Esencia de fuego", 14, "Elemento ofensivo menor: potencia el dano."),
            ElementItem("WaterEssence", "Esencia de agua", 14, "Elemento de aguante menor: mejora la comida."),
            ElementItem("EarthEssence", "Esencia de tierra", 14, "Elemento defensivo menor: endurece armaduras."),
            ElementItem("WindEssence", "Esencia de viento", 14, "Elemento ligero menor: mejora movimiento."),
            ElementItem("NatureEssence", "Esencia de naturaleza", 14, "Elemento vital menor: ayuda a recuperar vida."),
            ElementItem("LightEssence", "Esencia de luz", 18, "Elemento protector menor: equilibrio defensivo."),
            ElementItem("ShadowEssence", "Esencia de sombra", 18, "Elemento de riesgo menor: mas dano, menos margen.")
        };

        private static readonly Dictionary<string, Definition> byId =
            definitions.ToDictionary(definition => definition.Id, StringComparer.Ordinal);

        static SurvivalItemCatalog()
        {
            foreach (Definition seed in Seeds)
            {
                Definition crop = Find(seed.CropItemId);
                if (crop == null) continue;
                seed.GrowthSeconds = seed.Id.EndsWith("Sapling", StringComparison.Ordinal) ? 90f :
                    seed.SeedRarity == SeedRarity.Magic ? 60f : seed.SeedRarity == SeedRarity.Mineral ? 35f : 24f;
                seed.HarvestYield = Math.Max(seed.HarvestYield, 2);
                seed.UnlockMealsCooked = seed.SeedRarity == SeedRarity.Magic ? 6 : 1;
                seed.CropRole = "Alimento de temporada";
                switch (seed.CropItemId)
                {
                    case "Carrot":
                        seed.GrowthSeconds = 12f; seed.HarvestYield = 2; seed.UnlockMealsCooked = 0;
                        seed.CropRole = "Cosecha rapida para sopa"; break;
                    case "Potato":
                        seed.GrowthSeconds = 28f; seed.HarvestYield = 3; seed.UnlockMealsCooked = 0;
                        seed.CropRole = "Base de provisiones de viaje"; break;
                    case "Wheat":
                        seed.GrowthSeconds = 40f; seed.HarvestYield = 4; seed.UnlockMealsCooked = 0;
                        seed.CropRole = "Ingrediente para cocinar varias raciones"; break;
                    case "Strawberry":
                        seed.GrowthSeconds = 60f; seed.HarvestYield = 3; seed.UnlockMealsCooked = 0;
                        seed.CropRole = "Cosecha de valor para vender"; break;
                }

                crop.GrowthSeconds = seed.GrowthSeconds;
                crop.HarvestYield = seed.HarvestYield;
                crop.CropRole = seed.CropRole;
                crop.UnlockMealsCooked = 0;
                seed.Description = $"{seed.GrowthSeconds:0}s tras regar; cosecha {seed.HarvestYield} {crop.Name}. {seed.CropRole}.";
                if (seed.Id.EndsWith("Sapling", StringComparison.Ordinal)) seed.Description += " Cultivo de una cosecha.";
            }
        }

        public static IReadOnlyList<Definition> All => definitions;
        public static IEnumerable<Definition> Seeds => definitions.Where(definition => definition.IsSeed);
        public static IEnumerable<Definition> AvailableSeeds(int mealsCooked) => Seeds.Where(seed => seed.UnlockMealsCooked <= mealsCooked);
        public static Definition Find(string id) => string.IsNullOrEmpty(id) ? null : byId.TryGetValue(id, out Definition value) ? value : null;
        public static bool IsKnown(string id) => Find(id) != null;
        public static bool IsFood(string id) => Find(id)?.IsFood == true;
        public static bool IsSeed(string id) => Find(id)?.IsSeed == true;

        private static Definition Food(string id, string name, int price, int hunger, int heal = 0) =>
            new Definition
            {
                Id = id,
                Name = name,
                Icon = id,
                Category = SurvivalItemCategory.Food,
                Price = price,
                HungerRestore = hunger,
                HealRestore = Math.Max(1, heal),
                IsFood = true,
                Description = $"+{Math.Max(1, heal)} vida. Ingrediente de cocina."
            };

        private static Definition Seed(string id, string name, string icon, string crop, int price, SeedRarity rarity, int harvestYield = 1) =>
            new Definition
            {
                Id = id,
                Name = name,
                Icon = icon,
                CropItemId = crop,
                Category = SurvivalItemCategory.Seed,
                Price = price,
                SeedRarity = rarity,
                HarvestYield = harvestYield,
                IsSeed = true,
                Description = "Se planta con Interactuar en tierra labrada."
            };

        private static Definition MaterialItem(string id, string name, string icon, int price, string description) =>
            new Definition
            {
                Id = id,
                Name = name,
                Icon = icon,
                Category = SurvivalItemCategory.Material,
                Price = price,
                Description = description
            };

        private static Definition GemItem(string id, string name, int price, string description) =>
            new Definition
            {
                Id = id,
                Name = name,
                Icon = "Gem",
                Category = SurvivalItemCategory.Gem,
                Price = price,
                IsGem = true,
                Description = description
            };

        private static Definition ElementItem(string id, string name, int price, string description) =>
            new Definition
            {
                Id = id,
                Name = name,
                Icon = "Gem",
                Category = SurvivalItemCategory.Element,
                Price = price,
                IsElement = true,
                Description = description
            };
    }
}
