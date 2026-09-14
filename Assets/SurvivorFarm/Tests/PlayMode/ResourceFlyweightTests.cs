using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Tests
{
    public sealed class ResourceFlyweightTests
    {
        [Test]
        public void EqualResourceConfigurationsUseOneDefinition()
        {
            ResourceDefinition first = ResourceFlyweights.Resource(ItemKind.Wood, 2, 4, 1);
            for (int i = 0; i < 10000; i++)
                Assert.That(ResourceFlyweights.Resource(ItemKind.Wood, 2, 4, 1), Is.SameAs(first));
            Assert.That(first.Reward, Is.SameAs(ResourceFlyweights.Item(ItemKind.Wood)));
            Assert.That(ResourceFlyweights.Resource(ItemKind.Wood, 5, 8, 1), Is.Not.SameAs(first));
            Assert.That(ResourceFlyweights.Resource(ItemKind.Stone, 2, 4, 1), Is.Not.SameAs(first));
        }

        [Test]
        public void SpawnedCopiesShareDataButNeverHealthOrDepletion()
        {
            GameObject source = new GameObject("Tree");
            GameObject clone = null;
            GameObject player = new GameObject("Player");
            try
            {
                TreeResource first = source.AddComponent<TreeResource>();
                first.Configure(null, null, 2, 4);
                first.ConfigureHealth(2);
                clone = Object.Instantiate(source);
                TreeResource second = clone.GetComponent<TreeResource>();
                Assert.That(second.Definition, Is.SameAs(first.Definition));
                PlayerInventory inventory = player.AddComponent<PlayerInventory>();
                first.Interact(FarmTool.Axe, inventory);
                Assert.That(first.CurrentHealth, Is.EqualTo(1));
                Assert.That(second.CurrentHealth, Is.EqualTo(2));
                first.Interact(FarmTool.Axe, inventory);
                Assert.That(first.IsHarvested, Is.True);
                Assert.That(second.IsAvailable, Is.True);
                Assert.That(second.Definition.MaxHealth, Is.EqualTo(2));
                second.ConfigureHealth(4);
                Assert.That(second.Definition, Is.Not.SameAs(first.Definition));
                Assert.That(first.Definition.MaxHealth, Is.EqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(source);
                if (clone != null) Object.DestroyImmediate(clone);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void AnimationIsSharedAndDefensivelyCopiesInputFrames()
        {
            Texture2D texture = new Texture2D(2, 2);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
            try
            {
                Sprite[] input = { sprite };
                ResourceAnimation first = ResourceFlyweights.Animation(input, 7f);
                ResourceAnimation second = ResourceFlyweights.Animation(new[] { sprite }, 7f);
                input[0] = null;
                Assert.That(second, Is.SameAs(first));
                Assert.That(first.GetFrame(0), Is.SameAs(sprite));
                Assert.That(ResourceFlyweights.Animation(new[] { sprite }, 8f), Is.Not.SameAs(first));
            }
            finally
            {
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void EveryCurrentItemHasASharedDefinitionAndCorrectReward()
        {
            GameObject player = new GameObject("Player");
            try
            {
                PlayerInventory inventory = player.AddComponent<PlayerInventory>();
                foreach (ItemKind kind in System.Enum.GetValues(typeof(ItemKind)))
                {
                    ItemDefinition item = ResourceFlyweights.Item(kind);
                    Assert.That(ResourceFlyweights.Item(kind), Is.SameAs(item));
                    item.Grant(inventory, 2);
                }
                Assert.That(inventory.Wood, Is.EqualTo(2));
                Assert.That(inventory.Stone, Is.EqualTo(2));
                Assert.That(inventory.Fruit, Is.EqualTo(2));
                Assert.That(inventory.Food, Is.EqualTo(2));
                Assert.That(inventory.Coins, Is.EqualTo(2));
                Assert.That(inventory.CommonSeeds, Is.EqualTo(2));
                Assert.That(inventory.MineralSeeds, Is.EqualTo(2));
                Assert.That(inventory.MagicSeeds, Is.EqualTo(2));
            }
            finally { Object.DestroyImmediate(player); }
        }

        [Test]
        public void PlotsShareCultivationButKeepTheirOwnGrowthState()
        {
            GameObject firstObject = new GameObject("Plot A");
            GameObject secondObject = new GameObject("Plot B");
            try
            {
                FarmingPlot first = firstObject.AddComponent<FarmingPlot>();
                FarmingPlot second = secondObject.AddComponent<FarmingPlot>();
                Assert.That(first.Definition, Is.SameAs(second.Definition));
                first.Restore((int)FarmingPlot.PlotState.Growing, 4f, (int)SeedRarity.Magic);
                second.Restore((int)FarmingPlot.PlotState.Grass, 0f, (int)SeedRarity.Common);
                Assert.That(first.RemainingGrowTime, Is.EqualTo(4f).Within(0.01f));
                Assert.That(second.RemainingGrowTime, Is.Zero);
                Assert.That(first.PlantedSeedRarityId, Is.EqualTo((int)SeedRarity.Magic));
                Assert.That(second.StateId, Is.EqualTo((int)FarmingPlot.PlotState.Grass));
            }
            finally
            {
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
            }
        }

        [Test]
        public void BuiltCatalogReferencesAreUsedWithoutCloning()
        {
            Assert.That(Resources.Load<FlyweightCatalog>("FlyweightCatalog"), Is.Not.Null);
            ItemDefinition fruit = ResourceFlyweights.Item(ItemKind.Fruit);
            Assert.That(fruit.hideFlags, Is.EqualTo(HideFlags.None));
            Assert.That(ResourceFlyweights.Cultivation().hideFlags, Is.EqualTo(HideFlags.None));
        }
    }
}
