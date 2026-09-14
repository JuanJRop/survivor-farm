using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Tests
{
    public sealed class VillageServicesTests
    {
        GameObject root, clockRoot, gatherRoot;
        ValleyCampaign campaign;
        PlayerInventory inventory;
        AdventureProgress progress;
        DayNightCycle clock;

        [SetUp]
        public void SetUp()
        {
            Assert.That(Object.FindFirstObjectByType<GameSaveSystem>(), Is.Null, "Run these tests in an isolated test scene, without a personal save system.");
            root = new GameObject("Village services test");
            root.SetActive(false);
            inventory = root.AddComponent<PlayerInventory>();
            root.AddComponent<PlayerSurvivalStats>();
            progress = root.AddComponent<AdventureProgress>();
            campaign = root.AddComponent<ValleyCampaign>();
            // Inactive fixture avoids world generation and the campaign's Awake side effects.
            typeof(ValleyCampaign).GetProperty("Inventory").GetSetMethod(true).Invoke(campaign, new object[] { inventory });
            clockRoot = new GameObject("Village services clock");
            clock = clockRoot.AddComponent<DayNightCycle>();
            clock.enabled = false;
            clock.Restore(1, 12);
        }

        [TearDown]
        public void TearDown()
        {
            if (gatherRoot != null) Object.DestroyImmediate(gatherRoot);
            if (root != null) Object.DestroyImmediate(root);
            if (clockRoot != null) Object.DestroyImmediate(clockRoot);
        }

        [TestCase(16)]
        [TestCase(28)]
        [TestCase(99)]
        public void StoryPointsAloneDoNotGrantHighVillageRank(int influence)
        {
            campaign.Data = new ValleyData { influence = influence, restored = true, boss = true, guardian = true, portal = true };
            Assert.That(campaign.InfluenceRank, Is.EqualTo(1));
        }

        [Test]
        public void ProtectorRequiresPantryWorkshopAndAThirdProject()
        {
            var data = campaign.Data;
            data.influence = 28;
            data.nicoWorkshopRepaired = data.daliaGardenRestored = data.roloMarketOpened = true;
            Assert.That(campaign.InfluenceRank, Is.EqualTo(2));
            data.maraPantryStocked = true;
            Assert.That(campaign.InfluenceRank, Is.EqualTo(3));
            data.nicoWorkshopRepaired = false;
            Assert.That(campaign.InfluenceRank, Is.EqualTo(2));
        }

        [Test]
        public void LeaderRequiresAllFiveProjectsAndEnoughPoints()
        {
            CompleteAllProjects();
            campaign.Data.influence = 27;
            Assert.That(campaign.InfluenceRank, Is.EqualTo(3));
            campaign.Data.influence = 28;
            Assert.That(campaign.InfluenceRank, Is.EqualTo(4));
            campaign.Data.guardPostBuilt = false;
            Assert.That(campaign.InfluenceRank, Is.EqualTo(3));
        }

        [Test]
        public void LegacyRankRewardsStayOwnedAndCannotBeClaimedAgain()
        {
            inventory.AddEquipment("HunterBow");
            campaign.Restore(JsonUtility.FromJson<ValleyData>("{\"influence\":28,\"rankRewardMask\":30}"));
            Assert.That(campaign.InfluenceRank, Is.EqualTo(1));
            Assert.That(campaign.WorkshopService.UsedToday, Is.False);
            CompleteAllProjects();
            campaign.AddInfluence(1);
            Assert.That(inventory.OwnsEquipment("HunterBow"), Is.True);
            Assert.That(inventory.Coins, Is.Zero);
            Assert.That(inventory.Food, Is.Zero);
            Assert.That(progress.Data.iron, Is.Zero);
            Assert.That(campaign.Data.rankRewardMask, Is.EqualTo(30));
        }

        [Test]
        public void CompletingMilestonesPaysEachNewRankOnlyOnce()
        {
            campaign.Data.influence = 28;
            campaign.Data.rankRewardMask = 1 << 1;
            CompleteAllProjects();
            campaign.Changed("");
            int coins = inventory.Coins, food = inventory.Food, iron = progress.Data.iron;
            Assert.That(campaign.Data.rankRewardMask, Is.EqualTo(30));
            campaign.Restore(JsonUtility.FromJson<ValleyData>(JsonUtility.ToJson(campaign.Data)));
            campaign.Changed("");
            Assert.That(inventory.Coins, Is.EqualTo(coins));
            Assert.That(inventory.Food, Is.EqualTo(food));
            Assert.That(progress.Data.iron, Is.EqualTo(iron));
        }

        [Test]
        public void WorkshopRepairIsPaidOnceAndDoesNotConsumeDailyService()
        {
            campaign.Data.camp = true;
            inventory.AddWood(12); inventory.AddStone(8);
            Assert.That(campaign.AcceptVillageQuest("village:blacksmith"), Is.True);
            Assert.That(campaign.CompleteVillageQuest("village:blacksmith"), Is.True);
            Assert.That(campaign.WorkshopRestored, Is.True);
            Assert.That(progress.Data.iron, Is.EqualTo(2));
            Assert.That(inventory.OwnsEquipment("WoodenShield"), Is.True);
            Assert.That(inventory.Wood, Is.Zero);
            Assert.That(inventory.Stone, Is.Zero);
            Assert.That(campaign.Data.nicoSupplyDay, Is.Zero);
            int influence = campaign.Data.influence;
            campaign.CompleteVillageQuest("village:blacksmith");
            Assert.That(progress.Data.iron, Is.EqualTo(2));
            Assert.That(campaign.Data.influence, Is.EqualTo(influence));
        }

        [Test]
        public void WorkshopRequiresReconstructionAndMaterialsWithoutPartialPayment()
        {
            inventory.AddWood(4); inventory.AddStone(5);
            Assert.That(campaign.TryUseWorkshopService(), Is.False);
            campaign.Data.nicoWorkshopRepaired = true;
            Assert.That(campaign.WorkshopService.CanUse, Is.False);
            Assert.That(campaign.TryUseWorkshopService(), Is.False);
            Assert.That(inventory.Wood, Is.EqualTo(4));
            Assert.That(inventory.Stone, Is.EqualTo(5));
            Assert.That(campaign.Data.nicoSupplyDay, Is.Zero);
            Assert.That(progress.Data.iron, Is.Zero);
        }

        [Test]
        public void WorkshopDailyLimitSurvivesSaveLoadAndResetsNextDay()
        {
            campaign.Data.nicoWorkshopRepaired = true;
            inventory.AddWood(12); inventory.AddStone(18);
            Assert.That(campaign.TryUseWorkshopService(), Is.True);
            Assert.That(inventory.Wood, Is.EqualTo(8));
            Assert.That(inventory.Stone, Is.EqualTo(12));
            Assert.That(progress.Data.iron, Is.EqualTo(2));
            campaign.Restore(JsonUtility.FromJson<ValleyData>(JsonUtility.ToJson(campaign.Data)));
            Assert.That(campaign.TryUseWorkshopService(), Is.False);
            Assert.That(progress.Data.iron, Is.EqualTo(2));
            clock.Restore(2, 8);
            Assert.That(campaign.TryUseWorkshopService(), Is.True);
            Assert.That(progress.Data.iron, Is.EqualTo(4));
            Assert.That(campaign.Data.nicoSupplyDay, Is.EqualTo(2));
        }

        [Test]
        public void ReentrantInventoryCallbackCannotDuplicateWorkshopOrder()
        {
            campaign.Data.nicoWorkshopRepaired = true;
            inventory.AddWood(12); inventory.AddStone(18);
            Action repeat = () => campaign.TryUseWorkshopService();
            inventory.InventoryChanged += repeat;
            try { Assert.That(campaign.TryUseWorkshopService(), Is.True); }
            finally { inventory.InventoryChanged -= repeat; }
            Assert.That(progress.Data.iron, Is.EqualTo(2));
            Assert.That(inventory.Wood, Is.EqualTo(8));
            Assert.That(inventory.Stone, Is.EqualTo(12));
        }

        [Test]
        public void ProtectorReceivesLargerOrderForTheSameMaterials()
        {
            CompleteAllProjects();
            campaign.Data.influence = 16;
            campaign.Data.rankRewardMask = 30;
            inventory.AddWood(4); inventory.AddStone(6);
            Assert.That(campaign.WorkshopService.IronReward, Is.EqualTo(3));
            Assert.That(campaign.TryUseWorkshopService(), Is.True);
            Assert.That(progress.Data.iron, Is.EqualTo(3));
        }

        [Test]
        public void RepeatedWellInteractionsNeverRestoreHunger()
        {
            var stats = root.GetComponent<PlayerSurvivalStats>();
            stats.Restore(5, 5, .25f);
            for (int i = 0; i < 20; i++) campaign.Use("village:well", FarmTool.Sword);
            Assert.That(stats.HungerPercent, Is.EqualTo(1f));
        }

        [Test]
        public void NoteCannotBeUsedToFarmInfluence()
        {
            campaign.Use("note", FarmTool.Sword);
            campaign.Use("note", FarmTool.Sword);
            Assert.That(campaign.Data.influence, Is.EqualTo(1));
        }

        [Test]
        public void FiveResidentsHaveDistinctOriginalArtAndValidAnimationFrames()
        {
            var art = Resources.Load<VillageNpcArtCatalog>("VillageNpcArt");
            Assert.That(art, Is.Not.Null);
            var idleAtlases = new HashSet<Texture2D>();
            foreach (string id in new[] { "village:elder", "village:blacksmith", "village:farmer", "village:merchant", "village:guard" })
            {
                var entry = Array.Find(art.Entries, value => value.Id == id);
                Assert.That(entry, Is.Not.Null, id);
                Assert.That(entry.Idle, Is.Not.Null, id);
                Assert.That(entry.Walk, Is.Not.Null, id);
                Assert.That(entry.Work, Is.Not.Null, id);
                Assert.That(idleAtlases.Add(entry.Idle), Is.True, "Repeated character for " + id);
                for (int direction = 0; direction < 3; direction++)
                {
                    Assert.That(art.Frame(id, false, false, direction, 0).rect.size, Is.EqualTo(new Vector2(32, 32)));
                    Assert.That(art.Frame(id, true, false, direction, 99), Is.Not.Null);
                    Assert.That(art.Frame(id, false, true, direction, 99), Is.Not.Null);
                }
            }
        }

        [Test]
        public void WorkshopFacadeChangesAndPreservesTheSharedLotAndCollisionSize()
        {
            var world = root.AddComponent<ValleyWorld>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(ValleyWorld).GetField("campaign", flags).SetValue(world, campaign);
            var house = (GameObject)typeof(ValleyWorld).GetMethod("VillageBuilding", flags).Invoke(world, new object[] { root.transform, "nico" });
            var sprite = house.GetComponentInChildren<SpriteRenderer>(true);
            var collider = house.GetComponent<BoxCollider2D>();
            var initialSprite = sprite.sprite;
            var collisionSize = collider.size;
            Assert.That(initialSprite, Is.Not.Null);
            Assert.That(house.transform.position, Is.EqualTo(VillageLayout.GetLot("nico").Position));
            campaign.Data.nicoWorkshopRepaired = true;
            typeof(ValleyWorld).GetMethod("RefreshBuilding", flags).Invoke(world, new object[] { house, "nico" });
            Assert.That(sprite.sprite, Is.Not.SameAs(initialSprite));
            Assert.That(sprite.sprite, Is.SameAs(HouseSprites.Facade(VillageLayout.GetLot("nico").RestoredFacade)));
            Assert.That(sprite.sprite.bounds.size.x * sprite.transform.lossyScale.x, Is.EqualTo(VillageLayout.HouseWidth).Within(.001f));
            Assert.That(collider.size, Is.EqualTo(collisionSize));
            Assert.That(house.transform.position, Is.EqualTo(VillageLayout.GetLot("nico").Position));
        }

        [TestCase("village:elder")]
        [TestCase("village:blacksmith")]
        [TestCase("village:farmer")]
        [TestCase("village:merchant")]
        [TestCase("village:guard")]
        public void EveryResidentChangesWithProgressAndStaysWithinReach(string id)
        {
            string before = campaign.GetVillagerDialogue(id);
            CompleteAllProjects();
            Assert.That(campaign.GetVillagerDialogue(id), Is.Not.EqualTo(before));
            for (int hour = 0; hour < 24; hour++)
                Assert.That(VillageResidents.Offset(id, hour, true).magnitude, Is.LessThan(.45f));
            Assert.That(VillageResidents.Activity(campaign.Data, id, 8), Is.Not.EqualTo(VillageResidents.Activity(campaign.Data, id, 12)));
            Assert.That(VillageResidents.Activity(campaign.Data, id, 12), Is.Not.EqualTo(VillageResidents.Activity(campaign.Data, id, 22)));
        }

        [TestCase("distance")]
        [TestCase("resource disabled")]
        [TestCase("inventory disabled")]
        [TestCase("animator disabled")]
        [TestCase("animation interrupted")]
        [TestCase("campaign restored")]
        public void CampaignGatheringInterruptionsDoNotGrantResourcesOrDiscovery(string cause)
        {
            var resource = CreateGatheringFixture("wood:0", out var animator);
            resource.Interact(FarmTool.Axe, inventory);
            Assert.That(resource.IsGathering, Is.True);
            switch (cause)
            {
                case "distance": inventory.transform.position = resource.transform.position + Vector3.right * 1.36f; break;
                case "resource disabled": resource.gameObject.SetActive(false); break;
                case "inventory disabled": inventory.enabled = false; break;
                case "animator disabled": animator.enabled = false; break;
                case "animation interrupted": animator.PlayNamedAction("Damage"); break;
                case "campaign restored": campaign.Restore(new ValleyData()); break;
            }
            GatherUpdate(resource);
            Assert.That(resource.IsGathering, Is.False);
            Assert.That(inventory.Wood, Is.Zero);
            Assert.That(campaign.Data.discoveries, Is.Empty);
            if (cause == "animation interrupted") Assert.That(animator.CurrentClip, Is.EqualTo("Damage"));
            else Assert.That(animator.MovementLocked, Is.False);
        }

        [Test]
        public void CampaignGatheringDeathCannotBeHiddenBySameFrameRevive()
        {
            var resource = CreateGatheringFixture("wood:0", out _);
            resource.Interact(FarmTool.Axe, inventory);
            var stats = inventory.GetComponent<PlayerSurvivalStats>();
            stats.Restore(0, 1f);
            stats.Revive();
            Assert.That(resource.IsGathering, Is.False);
            GatherUpdate(resource);
            Assert.That(inventory.Wood, Is.Zero);
            Assert.That(campaign.Data.discoveries, Is.Empty);
        }

        [TestCase("wood:0", FarmTool.Axe, 4, 0)]
        [TestCase("stone:0", FarmTool.Pickaxe, 0, 4)]
        public void CampaignGatheringPaysOriginalAmountOnlyAtCompletion(string id, FarmTool tool, int wood, int stone)
        {
            var resource = CreateGatheringFixture(id, out var animator);
            resource.Interact(tool, inventory);
            SetGatherField(resource, "nextHitAt", Time.time);
            GatherUpdate(resource);
            Assert.That(inventory.Wood + inventory.Stone, Is.Zero);
            Assert.That(campaign.Data.discoveries, Is.Empty);
            SetGatherField(resource, "gatherEndsAt", Time.time - .01f);
            GatherUpdate(resource);
            resource.Interact(tool, inventory);
            Assert.That(inventory.Wood, Is.EqualTo(wood));
            Assert.That(inventory.Stone, Is.EqualTo(stone));
            Assert.That(campaign.Data.discoveries, Is.EquivalentTo(new[] { id + ":1" }));
            Assert.That(resource.IsGathering, Is.False);
            Assert.That(animator.MovementLocked, Is.False);
        }

        [Test]
        public void CampaignGatheringAcceptsNaturalIdleAtTheDeadline()
        {
            var resource = CreateGatheringFixture("wood:0", out var animator);
            resource.Interact(FarmTool.Axe, inventory);
            SetGatherField(resource, "gatherEndsAt", Time.time - .01f);
            typeof(PlayerCharacterAnimator).GetField("actionEndsAt", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(animator, Time.time - .01f);
            typeof(PlayerCharacterAnimator).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(animator, null);
            Assert.That(animator.CurrentClip, Is.EqualTo("Idle"));
            GatherUpdate(resource);
            Assert.That(inventory.Wood, Is.EqualTo(4));
        }

        ValleyInteraction CreateGatheringFixture(string id, out PlayerCharacterAnimator animator)
        {
            gatherRoot = new GameObject("Village gathering tests");
            var player = new GameObject("Gatherer"); player.transform.SetParent(gatherRoot.transform);
            inventory = player.AddComponent<PlayerInventory>();
            player.AddComponent<PlayerSurvivalStats>();
            player.AddComponent<SpriteRenderer>();
            animator = player.AddComponent<PlayerCharacterAnimator>();
            Assert.That(animator.Library, Is.Not.Null);
            typeof(ValleyCampaign).GetProperty("Inventory").GetSetMethod(true).Invoke(campaign, new object[] { inventory });
            var node = new GameObject("Campaign resource"); node.transform.SetParent(gatherRoot.transform); node.transform.position = Vector3.right;
            node.AddComponent<SpriteRenderer>();
            var resource = node.AddComponent<ValleyInteraction>();
            resource.Campaign = campaign; resource.Id = id; resource.Label = id;
            return resource;
        }

        static void GatherUpdate(ValleyInteraction resource) => typeof(ValleyInteraction).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(resource, null);
        static void SetGatherField(ValleyInteraction resource, string field, object value) => typeof(ValleyInteraction).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(resource, value);

        void CompleteAllProjects()
        {
            var data = campaign.Data;
            data.maraPantryStocked = data.nicoWorkshopRepaired = data.daliaGardenRestored = data.roloMarketOpened = data.guardPostBuilt = true;
        }
    }
}
