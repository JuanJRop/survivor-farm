using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Tests
{
    internal static class FurnitureTestSites
    {
        // The old wall fixture sat on the south road. Household furniture must
        // obey the real grass and occupancy rules while exercising its APIs.
        public static Vector2 Find(ConstructionSystem construction,Vector2? avoid=null)
        {
            var origin=(Vector2)construction.transform.position;
            for(int ring=2;ring<=10;ring++)
                for(int x=-ring;x<=ring;x++)for(int y=-ring;y<=ring;y++)
                {
                    if(Mathf.Abs(x)!=ring&&Mathf.Abs(y)!=ring)continue;
                    var candidate=construction.SnapPlacement("Chest",origin+new Vector2(x,y)*.5f);
                    if(avoid.HasValue&&Vector2.Distance(candidate,avoid.Value)<1.5f)continue;
                    if(construction.CanPlace("Chest",candidate,out _))return candidate;
                }
            Assert.Fail("No unobstructed grass for household furniture near "+origin);
            return origin;
        }
    }
    public sealed class DefenseRetirementTests
    {
        private GameObject root;
        private PlayerInventory inventory;
        private ConstructionSystem construction;

        [SetUp]
        public void SetUp()
        {
            root=new GameObject("Defense retirement test");
            var world=new GameObject("Outdoor World");world.transform.SetParent(root.transform);
            var actor=new GameObject("Player");actor.transform.SetParent(root.transform);
            inventory=actor.AddComponent<PlayerInventory>();
            actor.AddComponent<PlayerSurvivalStats>();
            actor.AddComponent<PlayerCraftingController>();
            construction=actor.AddComponent<ConstructionSystem>();
            inventory.AddWood(100);inventory.AddStone(100);
        }

        [TearDown]
        public void TearDown()
        {
            construction.Cancel();
            Object.DestroyImmediate(root);
        }

        [TestCase("Fence")]
        [TestCase("StoneWall")]
        [TestCase("ReinforcedWall")]
        [TestCase("Trap")]
        [TestCase("Turret")]
        public void RetiredDefensesCannotBeCraftedPackedOrPlaced(string kind)
        {
            int wood=inventory.Wood,stone=inventory.Stone;
            Assert.IsFalse(BackpackActions.IsBuilding(kind));
            var crafting=inventory.GetComponent<PlayerCraftingController>();
            Assert.IsFalse(crafting.TryGetRecipeDescriptor(kind,out _));
            Assert.IsFalse(crafting.Craft(kind));
            Assert.IsFalse(construction.Pack(kind));
            inventory.AddPacked(kind,4);
            Assert.AreEqual(0,inventory.PackedCount(kind));
            Assert.IsFalse(construction.BeginPacked(kind));
            construction.Begin(kind);
            Assert.IsFalse(ConstructionSystem.IsPlacing);
            Assert.IsFalse(construction.Place(kind,Vector2.zero));
            construction.AddAuthoredDefense(kind,Vector2.zero);
            Assert.IsEmpty(construction.Buildings);
            Assert.AreEqual(wood,inventory.Wood);
            Assert.AreEqual(stone,inventory.Stone);
        }

        [Test]
        public void LegacySaveDropsDefensesAndPreservesFurnitureAndStoredResources()
        {
            var chest=new BuildingData {kind="Chest",x=4,y=-4,wood=12,food=7,stone=3,iron=2};
            var saved=new List<BuildingData> {chest};
            var packed=new List<PackedBuilding> {new PackedBuilding {kind="Campfire",count=1}};
            foreach(string kind in FortressPieces.Palette)
            {
                saved.Add(new BuildingData {kind=kind,x=7,y=-4,health=20});
                packed.Add(new PackedBuilding {kind=kind,count=5});
            }
            inventory.RestorePacked(packed);
            construction.Restore(saved);
            Assert.AreEqual(1,construction.Buildings.Count);
            Assert.AreSame(chest,construction.Buildings[0]);
            Assert.AreEqual(12,chest.wood);Assert.AreEqual(7,chest.food);
            Assert.AreEqual(3,chest.stone);Assert.AreEqual(2,chest.iron);
            Assert.AreEqual(1,inventory.PackedBuildings.Count);
            Assert.AreEqual("Campfire",inventory.PackedBuildings[0].kind);
            Assert.AreEqual(1,root.GetComponentsInChildren<PlacedBuilding>().Length);
            Assert.IsEmpty(root.GetComponentsInChildren<FarmDefense>());
            Assert.AreEqual(6,saved.Count,"Loading must not mutate the source save record.");
        }

        [Test]
        public void RecipeCatalogKeepsCookingEquipmentAndHouseholdUtilities()
        {
            var ids=inventory.GetComponent<PlayerCraftingController>().GetRecipeDescriptors().Select(recipe=>recipe.Id).ToArray();
            CollectionAssert.Contains(ids,"Food");
            CollectionAssert.Contains(ids,"Sword");
            CollectionAssert.Contains(ids,"Campfire");
            CollectionAssert.Contains(ids,"Chest");
            Assert.IsFalse(ids.Any(BackpackActions.IsRetiredDefense));
        }
    }
}
