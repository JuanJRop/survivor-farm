using System;
using System.Collections.Generic;
using System.Linq;

namespace SurvivorFarm.Runtime.Player
{
    public sealed class EconomyRecipeCost
    {
        public string ItemId { get; }
        public int Amount { get; }
        public EconomyRecipeCost(string itemId, int amount) { ItemId = itemId; Amount = amount; }
    }

    public sealed class EconomyCookingRecipe
    {
        public string Id { get; }
        public string Name { get; }
        public int OutputAmount { get; }
        public bool RequiresWorkshop { get; }
        public IReadOnlyList<EconomyRecipeCost> Ingredients { get; }

        internal EconomyCookingRecipe(string id, string name, int output, bool workshop, params EconomyRecipeCost[] costs)
        {
            Id = id; Name = name; OutputAmount = output; RequiresWorkshop = workshop;
            Ingredients = Array.AsReadOnly(costs);
        }
    }

    public static class EconomyCookingRecipes
    {
        public static IReadOnlyList<EconomyCookingRecipe> All { get; } = Array.AsReadOnly(new[]
        {
            new EconomyCookingRecipe("Food", "Racion de fruta", 1, false, new EconomyRecipeCost("Fruit", 2)),
            new EconomyCookingRecipe("CarrotSoup", "Sopa de zanahoria", 1, false, new EconomyRecipeCost("Carrot", 2)),
            new EconomyCookingRecipe("TravelRations", "Provisiones de viaje", 2, false, new EconomyRecipeCost("Potato", 2), new EconomyRecipeCost("Wheat", 1)),
            new EconomyCookingRecipe("WorkshopRations", "Lote de provisiones de Nico", 3, true, new EconomyRecipeCost("Potato", 2), new EconomyRecipeCost("Wheat", 2), new EconomyRecipeCost("Wood", 1))
        });

        public static EconomyCookingRecipe Find(string id) => All.FirstOrDefault(recipe => recipe.Id == id);
    }

    public sealed class EconomyIngredientDescriptor
    {
        public string ItemId { get; }
        public string Name { get; }
        public int Required { get; }
        public int Owned { get; }
        public int Missing => Math.Max(0, Required - Owned);

        internal EconomyIngredientDescriptor(string itemId, string name, int required, int owned)
        {
            ItemId = itemId; Name = name; Required = required; Owned = owned;
        }
    }

    public sealed class EconomyRequirementDescriptor
    {
        public string Id { get; }
        public string Description { get; }
        public bool IsMet { get; }

        internal EconomyRequirementDescriptor(string id, string description, bool isMet)
        {
            Id = id; Description = description; IsMet = isMet;
        }
    }

    public sealed class EconomyRecipeDescriptor
    {
        public string Id { get; }
        public string Name { get; }
        public string OutputId { get; }
        public int OutputAmount { get; }
        public IReadOnlyList<EconomyIngredientDescriptor> Ingredients { get; }
        public IReadOnlyList<EconomyRequirementDescriptor> Requirements { get; }
        public bool IsAvailable => Requirements.All(requirement => requirement.IsMet);
        public bool CanCraft => IsAvailable && Ingredients.All(ingredient => ingredient.Missing == 0);
        public string UnavailableReason => Requirements.FirstOrDefault(requirement => !requirement.IsMet)?.Description ??
            string.Join(", ", Ingredients.Where(ingredient => ingredient.Missing > 0).Select(ingredient => $"Faltan {ingredient.Missing} {ingredient.Name}"));

        internal EconomyRecipeDescriptor(string id, string name, string outputId, int outputAmount,
            IEnumerable<EconomyIngredientDescriptor> ingredients, IEnumerable<EconomyRequirementDescriptor> requirements)
        {
            Id = id; Name = name; OutputId = outputId; OutputAmount = outputAmount;
            Ingredients = Array.AsReadOnly(ingredients.ToArray());
            Requirements = Array.AsReadOnly(requirements.ToArray());
        }
    }
}
