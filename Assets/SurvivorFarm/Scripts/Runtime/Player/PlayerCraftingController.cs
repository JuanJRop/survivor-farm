using System;
using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    public sealed class PlayerCraftingController : MonoBehaviour
    {
        public struct EquipmentRecipe
        {
            public string Id;
            public int Wood, Stone, Iron, Coins;
            public string Item;
            public int ItemAmount;
        }

        private static readonly EquipmentRecipe[] equipmentRecipes =
        {
            new EquipmentRecipe { Id="Helmet", Wood=4, Stone=8 },
            new EquipmentRecipe { Id="Chestplate", Wood=8, Stone=12 },
            new EquipmentRecipe { Id="Boots", Wood=4, Stone=4 },
            new EquipmentRecipe { Id="CopperHelmet", Wood=5, Stone=10, Coins=8 },
            new EquipmentRecipe { Id="CopperChestplate", Wood=10, Stone=14, Coins=12 },
            new EquipmentRecipe { Id="CopperBoots", Wood=5, Stone=6, Coins=8 },
            new EquipmentRecipe { Id="IronHelmet", Wood=4, Stone=8, Iron=3, Coins=14 },
            new EquipmentRecipe { Id="IronChestplate", Wood=8, Stone=12, Iron=5, Coins=20 },
            new EquipmentRecipe { Id="IronBoots", Wood=4, Stone=6, Iron=3, Coins=14 },
            new EquipmentRecipe { Id="GoldHelmet", Wood=4, Stone=4, Iron=4, Item="GoldOre", ItemAmount=2 },
            new EquipmentRecipe { Id="GoldChestplate", Wood=6, Stone=8, Iron=6, Item="GoldOre", ItemAmount=4 },
            new EquipmentRecipe { Id="GoldBoots", Wood=4, Stone=4, Iron=4, Item="GoldOre", ItemAmount=2 },
            new EquipmentRecipe { Id="RangerHood", Wood=10, Stone=4, Coins=12 },
            new EquipmentRecipe { Id="RangerVest", Wood=14, Stone=6, Coins=18 },
            new EquipmentRecipe { Id="RangerBoots", Wood=10, Stone=4, Coins=12 },
            new EquipmentRecipe { Id="MysticHood", Wood=4, Stone=10, Iron=2, Coins=18 },
            new EquipmentRecipe { Id="MysticRobe", Wood=6, Stone=14, Iron=3, Coins=24 },
            new EquipmentRecipe { Id="MysticBoots", Wood=5, Stone=8, Iron=2, Coins=18 },
            new EquipmentRecipe { Id="WoodenShield", Wood=10, Stone=2 },
            new EquipmentRecipe { Id="IronShield", Wood=6, Stone=8, Iron=4, Coins=16 },
            new EquipmentRecipe { Id="IronSword", Wood=6, Stone=4, Iron=6, Coins=25 },
            new EquipmentRecipe { Id="HunterBow", Wood=14, Stone=4, Iron=3, Coins=22 },
            new EquipmentRecipe { Id="RubySword", Wood=6, Stone=4, Iron=8, Item="Ruby", ItemAmount=1 },
            new EquipmentRecipe { Id="DiamondBow", Wood=10, Stone=4, Iron=8, Item="Diamond", ItemAmount=1 },
            new EquipmentRecipe { Id="RubyGem", Stone=6, Iron=2, Item="Ruby", ItemAmount=1 },
            new EquipmentRecipe { Id="EmeraldGem", Stone=6, Iron=2, Item="Emerald", ItemAmount=1 },
            new EquipmentRecipe { Id="DiamondGem", Stone=8, Iron=3, Item="Diamond", ItemAmount=1 },
            new EquipmentRecipe { Id="EmeraldShield", Wood=6, Stone=6, Iron=6, Item="Emerald", ItemAmount=1 },
            new EquipmentRecipe { Id="DiamondAmulet", Stone=10, Iron=4, Item="Diamond", ItemAmount=1 }
        };

        public static bool TryGetEquipmentRecipe(string id, out EquipmentRecipe recipe)
        {
            recipe = System.Array.Find(equipmentRecipes, value => value.Id == id);
            return !string.IsNullOrEmpty(recipe.Id);
        }

        public static EquipmentRecipe[] EquipmentRecipes => equipmentRecipes;

        [SerializeField] private int storageLevel;
        [SerializeField] private int campLevel;
        [SerializeField] private bool bedBuilt;

        [SerializeField] private bool campfireBuilt;
        [SerializeField] private int mealsCooked, weaponLevel = 1;
        public bool CampfireBuilt => campfireBuilt;
        public int MealsCooked => mealsCooked;
        public int WeaponLevel => weaponLevel;
        public bool BedBuilt => bedBuilt;

        public IReadOnlyList<EconomyRecipeDescriptor> GetRecipeDescriptors()
        {
            var descriptors = new List<EconomyRecipeDescriptor>();
            foreach (EconomyCookingRecipe recipe in EconomyCookingRecipes.All)
                if (TryGetRecipeDescriptor(recipe.Id, out EconomyRecipeDescriptor descriptor)) descriptors.Add(descriptor);
            if (TryGetRecipeDescriptor("Sword", out EconomyRecipeDescriptor sword)) descriptors.Add(sword);
            foreach (string id in new[] { "Campfire", "Fence", "Chest", "Workbench", "Beacon", "Bed", "Cabinet", "Furnace" })
                if (TryGetRecipeDescriptor(id, out EconomyRecipeDescriptor descriptor)) descriptors.Add(descriptor);
            foreach (EquipmentRecipe recipe in equipmentRecipes)
                if (TryGetRecipeDescriptor(recipe.Id, out EconomyRecipeDescriptor descriptor)) descriptors.Add(descriptor);
            return descriptors.AsReadOnly();
        }

        private EconomyCookingRecipe ResolveCookingRecipe(string id)
        {
            EconomyCookingRecipe recipe = EconomyCookingRecipes.Find(id);
            if (id != "Food" || inventory == null || inventory.Fruit >= 2) return recipe;
            // The legacy command also accepts the first affordable basic crop recipe.
            return EconomyCookingRecipes.All.FirstOrDefault(value => value.Id != "Food" && !value.RequiresWorkshop &&
                value.Ingredients.All(cost => BackpackActions.Count(inventory, cost.ItemId) >= cost.Amount)) ?? recipe;
        }

        public bool TryGetRecipeDescriptor(string id, out EconomyRecipeDescriptor recipe)
        {
            recipe = null;
            inventory = inventory != null ? inventory : GetComponent<PlayerInventory>();
            survivalStats = survivalStats != null ? survivalStats : GetComponent<PlayerSurvivalStats>();
            var costs = new List<EconomyIngredientDescriptor>();
            var requirements = new List<EconomyRequirementDescriptor>
            {
                new EconomyRequirementDescriptor("Alive", "Necesitas estar con vida.", survivalStats != null && survivalStats.CurrentHealth > 0),
                new EconomyRequirementDescriptor("Inventory", "Necesitas un inventario.", inventory != null)
            };
            string name, outputId = id;
            int outputAmount = 1;
            EconomyCookingRecipe cooking = ResolveCookingRecipe(id);
            if (cooking != null)
            {
                name = cooking.Name;
                outputId = "Food";
                outputAmount = cooking.OutputAmount;
                foreach (EconomyRecipeCost cost in cooking.Ingredients) AddIngredient(costs, cost.ItemId, cost.Amount);
                requirements.Add(new EconomyRequirementDescriptor("Campfire", "Coloca una fogata para cocinar.", campfireBuilt));
                if (cooking.RequiresWorkshop)
                    requirements.Add(new EconomyRequirementDescriptor("WorkshopRestored", "Repara el taller de Nico.", GetComponent<ValleyCampaign>()?.WorkshopRestored == true));
                requirements.Add(new EconomyRequirementDescriptor("OutputCapacity", "No caben mas raciones en el inventario.", inventory != null && (long)inventory.Food + outputAmount <= int.MaxValue));
            }
            else if (id == "Sword")
            {
                name = "Mejorar espada";
                AddIngredient(costs, "Wood", 6 * weaponLevel);
                AddIngredient(costs, "Stone", 8 * weaponLevel);
                requirements.Add(new EconomyRequirementDescriptor("WeaponLevel", "La espada ya tiene la mejora maxima.", weaponLevel < 3));
            }
            else if (BackpackActions.IsBuilding(id))
            {
                name = ConstructionSystem.Label(id);
                ConstructionSystem.Cost(id, out int wood, out int stone, out int iron);
                AddIngredient(costs, "Wood", wood);
                AddIngredient(costs, "Stone", stone);
                AddIngredient(costs, "Iron", iron);
                if (id == "Campfire" || id == "Bed")
                {
                    bool built = id == "Campfire" ? campfireBuilt : bedBuilt;
                    requirements.Add(new EconomyRequirementDescriptor("NotBuilt", "Ya esta construido o en la mochila.", !built && inventory != null && inventory.PackedCount(id) == 0));
                }
                if (id == "Cabinet" || id == "Furnace")
                {
                    string reason = GetComponent<HouseSystem>()?.Requirement(id);
                    requirements.Add(new EconomyRequirementDescriptor("House", string.IsNullOrEmpty(reason) ? "Necesitas una casa adecuada." : reason, reason == string.Empty));
                }
            }
            else if (TryGetEquipmentRecipe(id, out EquipmentRecipe equipment))
            {
                name = EquipmentItems.Find(id)?.Name ?? id;
                AddIngredient(costs, "Wood", equipment.Wood);
                AddIngredient(costs, "Stone", equipment.Stone);
                AddIngredient(costs, "Iron", equipment.Iron);
                AddIngredient(costs, "Coins", equipment.Coins);
                AddIngredient(costs, equipment.Item, equipment.ItemAmount);
                requirements.Add(new EconomyRequirementDescriptor("NotOwned", "Ya tienes este equipo.", inventory != null && !inventory.OwnsEquipment(id)));
            }
            else return false;
            recipe = new EconomyRecipeDescriptor(id, name, outputId, outputAmount, costs, requirements);
            return true;
        }

        private void AddIngredient(List<EconomyIngredientDescriptor> costs, string id, int amount)
        {
            if (amount <= 0 || string.IsNullOrEmpty(id)) return;
            string name = SurvivalItemCatalog.Find(id)?.Name ?? id switch
            {
                "Wood" => "madera", "Stone" => "piedra", "Iron" => "hierro", "Coins" => "oro", "Fruit" => "fruta", _ => id
            };
            int owned = id == "Coins" ? inventory?.Coins ?? 0 : BackpackActions.Count(inventory, id);
            costs.Add(new EconomyIngredientDescriptor(id, name, amount, owned));
        }

        public bool Craft(string id)
        {
            survivalStats = survivalStats != null ? survivalStats : GetComponent<PlayerSurvivalStats>();
            inventory = inventory != null ? inventory : GetComponent<PlayerInventory>();
            if (survivalStats == null || survivalStats.CurrentHealth <= 0 || inventory == null) return false;
            if (id == "Bed") return CraftBed();
            if (BackpackActions.IsBuilding(id)) return GetComponent<ConstructionSystem>().Pack(id);
            if (!TryGetRecipeDescriptor(id, out EconomyRecipeDescriptor descriptor)) return false;
            if (!descriptor.CanCraft)
            {
                FarmNotificationCenter.Show(descriptor.UnavailableReason);
                return false;
            }
            EconomyCookingRecipe cooking = ResolveCookingRecipe(id);
            if (cooking != null)
            {
                int previousMeals = mealsCooked;
                mealsCooked = (int)Math.Min(int.MaxValue, (long)mealsCooked + 1);
                if (!inventory.TryPrepareMeals(cooking.Ingredients, cooking.OutputAmount))
                {
                    mealsCooked = previousMeals;
                    return false;
                }
            }
            else if (id == "Sword")
            {
                if (weaponLevel>=3 || !TryPay(6*weaponLevel,8*weaponLevel)) return false;
                weaponLevel++;
            }
            else if (TryGetEquipmentRecipe(id, out EquipmentRecipe recipe))
            {
                if (inventory.OwnsEquipment(id) || !TryPay(recipe.Wood, recipe.Stone, recipe.Iron, recipe.Coins, recipe.Item, recipe.ItemAmount)) return false;
                inventory.AddEquipment(id);
            }
            else return false;
            CraftingChanged?.Invoke();
            GetComponent<SurvivorFarm.Runtime.Gameplay.GameFeelFeedback>()?.Pulse("Fabricado",transform.position,false,true);
            FarmNotificationCenter.Show("Fabricación terminada. Abre B para comida o C para equipar.");
            return true;
        }
        public void RegisterPlacedFire(){campfireBuilt=true;CraftingChanged?.Invoke();}
        public void RegisterPlacedBed(){bedBuilt=true;CraftingChanged?.Invoke();}
        public void RefreshPlacedUtilities(){var b=GetComponent<ConstructionSystem>().Buildings;campfireBuilt=b.Exists(x=>x.kind=="Campfire");bedBuilt=b.Exists(x=>x.kind=="Bed");CraftingChanged?.Invoke();}
        public const int BedWoodCost = 10;
        public const int BedStoneCost = 4;

        public bool CraftBed()
        {
            return GetComponent<ConstructionSystem>().Pack("Bed");
        }

        private PlayerInventory inventory;
        private PlayerSurvivalStats survivalStats;
        private Sprite squareSprite;
        private Sprite circleSprite;
        private GameObject storageVisual;
        private GameObject fireVisual;
        private GameObject campVisual;

        public int StorageLevel => storageLevel;
        public int CampLevel => campLevel;

        public event Action CraftingChanged;

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
            survivalStats = GetComponent<PlayerSurvivalStats>();
        }

        public void Configure(Sprite square, Sprite circle)
        {
            squareSprite = square;
            circleSprite = circle;
            EnsureVisuals();
            RefreshVisuals();
        }

        public void CraftStorage()
        {
            if (!TryPay(8 + storageLevel * 6, 4 + storageLevel * 3))
            {
                return;
            }

            storageLevel++;
            inventory?.IncreaseSeedCapacity(10);
            RefreshVisuals();
            FarmNotificationCenter.Show($"Almacen construido Nv.{storageLevel}. Almacen de la granja mejorado.");
            CraftingChanged?.Invoke();
        }

        public void CraftCamp()
        {
            if (!TryPay(6 + campLevel * 5, 6 + campLevel * 5))
            {
                return;
            }

            campLevel++;
            survivalStats?.IncreaseMaxHealth(1);
            RefreshVisuals();
            FarmNotificationCenter.Show($"Campamento mejorado Nv.{campLevel}. +1 vida maxima.");
            CraftingChanged?.Invoke();
        }

        public string GetCraftingSummary()
        {
            return $"Almacen Nv.{storageLevel} | Campamento Nv.{campLevel}";
        }

        public void Restore(int savedStorageLevel, int savedCampLevel, bool savedBedBuilt = false, bool savedCampfire = false, int savedMeals = 0, int savedWeapon = 1)
        {
            storageLevel = Mathf.Max(0, savedStorageLevel);
            campLevel = Mathf.Max(0, savedCampLevel);
            bedBuilt = savedBedBuilt; campfireBuilt=savedCampfire; mealsCooked=Mathf.Max(0,savedMeals); weaponLevel=Mathf.Clamp(savedWeapon,1,3);
            RefreshVisuals();
            CraftingChanged?.Invoke();
        }

        private void OnDestroy(){if(fireVisual!=null)Destroy(fireVisual);}

        private bool TryPay(int woodCost, int stoneCost)
        {
            return TryPay(woodCost, stoneCost, 0, 0);
        }

        private bool TryPay(int woodCost, int stoneCost, int ironCost, int coinCost)
        {
            return TryPay(woodCost, stoneCost, ironCost, coinCost, null, 0);
        }

        private bool TryPay(int woodCost, int stoneCost, int ironCost, int coinCost, string itemId, int itemAmount)
        {
            if (inventory == null)
            {
                return false;
            }

            AdventureProgress progress = inventory.GetComponent<AdventureProgress>();
            int iron = progress != null ? progress.Data.iron : 0;
            bool needsItem = !string.IsNullOrEmpty(itemId) && itemAmount > 0;
            int ownedItem = needsItem ? inventory.GetItemCount(itemId) : 0;
            if (inventory.Wood < woodCost || inventory.Stone < stoneCost || iron < ironCost || inventory.Coins < coinCost || (needsItem && ownedItem < itemAmount))
            {
                string itemText = needsItem ? $", {itemAmount} {SurvivalItemCatalog.Find(itemId)?.Name ?? itemId}" : "";
                FarmNotificationCenter.Show($"Falta material: {woodCost} madera, {stoneCost} piedra, {ironCost} hierro, {coinCost} oro{itemText}.");
                return false;
            }

            inventory.TryRemoveWood(woodCost);
            inventory.TryRemoveStone(stoneCost);
            if (ironCost > 0) progress.SpendIron(ironCost);
            if (coinCost > 0) inventory.TrySpendCoins(coinCost);
            if (needsItem) inventory.TryRemoveItem(itemId, itemAmount);
            return true;
        }

        private void EnsureVisuals()
        {
            if (squareSprite == null || circleSprite == null)
            {
                return;
            }

            if (storageVisual == null)
            {
                storageVisual = CreateStructureVisual("Storage Structure", new Vector3(-1.55f, -6.85f, 0f), new Color(0.48f, 0.30f, 0.16f));
            }

            if (campVisual == null)
            {
                campVisual = CreateStructureVisual("Camp Structure", new Vector3(1.55f, -6.85f, 0f), new Color(0.74f, 0.36f, 0.16f));
            }
        }

        private GameObject CreateStructureVisual(string name, Vector3 position, Color color)
        {
            GameObject root = new GameObject(name);
            root.transform.position = position;

            SpriteRenderer baseRenderer = root.AddComponent<SpriteRenderer>();
            baseRenderer.sprite = squareSprite;
            baseRenderer.color = color;
            baseRenderer.sortingOrder = 1;
            root.transform.localScale = new Vector3(0.9f, 0.75f, 1f);

            GameObject marker = new GameObject("Marker");
            marker.transform.SetParent(root.transform);
            marker.transform.localPosition = new Vector3(0f, 0.44f, -0.01f);
            marker.transform.localScale = Vector3.one * 0.28f;

            SpriteRenderer markerRenderer = marker.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = circleSprite;
            markerRenderer.color = Color.white;
            markerRenderer.sortingOrder = 2;
            return root;
        }

        private void RefreshVisuals()
        {
            if(fireVisual!=null){Destroy(fireVisual);fireVisual=null;}
            EnsureVisuals();

            if (storageVisual != null)
            {
                storageVisual.SetActive(storageLevel > 0);
            }

            if (campVisual != null)
            {
                campVisual.SetActive(campLevel > 0);
            }
        }
    }
}
