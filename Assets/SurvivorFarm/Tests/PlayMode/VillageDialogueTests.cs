using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Tests
{
    public sealed class VillageDialogueTests
    {
        GameObject root, player, speaker;
        ValleyCampaign campaign;
        PlayerInventory inventory;
        VillageDialogueWindow dialogue;

        [SetUp]
        public void SetUp()
        {
            Assert.IsNull(Object.FindFirstObjectByType<GameSaveSystem>());
            root = new GameObject("Village dialogue test");
            player = new GameObject("Player"); player.transform.SetParent(root.transform); player.SetActive(false);
            inventory = player.AddComponent<PlayerInventory>(); player.AddComponent<PlayerSurvivalStats>(); player.AddComponent<AdventureProgress>();
            campaign = player.AddComponent<ValleyCampaign>();
            typeof(ValleyCampaign).GetProperty("Inventory").GetSetMethod(true).Invoke(campaign, new object[] { inventory });
            campaign.Data.note = campaign.Data.camp = campaign.Data.harvest = campaign.Data.sealStone = true;
            inventory.AddCoins(200); inventory.AddWood(100); inventory.AddStone(100); inventory.AddFood(20); inventory.AddFruit(10); inventory.AddSeeds(20);
            player.GetComponent<AdventureProgress>().AddIron(20);
            speaker = new GameObject("Speaker"); speaker.transform.SetParent(root.transform); speaker.transform.position = Vector3.right;
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)); canvas.transform.SetParent(root.transform);
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(1280, 720);
            dialogue = root.AddComponent<VillageDialogueWindow>(); dialogue.Configure(canvas.transform);
        }

        [TearDown]
        public void TearDown() { VillageDialogueWindow.CloseActive(); Object.DestroyImmediate(root); }
        string Stock() => string.Join(",", inventory.Coins, inventory.Wood, inventory.Stone, inventory.Food, inventory.Fruit, inventory.CommonSeeds, player.GetComponent<AdventureProgress>().Data.iron, campaign.Data.influence);
        void Unlock(string id) { if (id == "village:merchant") campaign.Data.nicoWorkshopRepaired = campaign.Data.daliaGardenRestored = true; }

        [TestCase("village:elder")]
        [TestCase("village:blacksmith")]
        [TestCase("village:farmer")]
        [TestCase("village:merchant")]
        [TestCase("village:guard")]
        public void TalkDeclineAcceptAndConfirmAreSeparateAndRewardsAreUnique(string id)
        {
            Unlock(id); string before = Stock();
            Assert.IsTrue(dialogue.Open(campaign, id, speaker.transform));
            Assert.IsTrue(VillageDialogueWindow.IsOpen); Assert.IsTrue(InventoryPanelSystem.IsOpen);
            Assert.AreEqual(before, Stock()); Assert.IsFalse(VillageQuests.IsAccepted(campaign.Data, id));
            dialogue.AskForQuest(); dialogue.DeclineQuest();
            Assert.AreEqual(before, Stock()); Assert.IsFalse(VillageQuests.IsAccepted(campaign.Data, id));
            Assert.IsFalse(campaign.CompleteVillageQuest(id));
            dialogue.AskForQuest(); Assert.IsTrue(dialogue.AcceptQuest());
            Assert.AreEqual(before, Stock()); Assert.IsTrue(VillageQuests.IsAccepted(campaign.Data, id));
            Assert.IsFalse(VillageQuests.IsComplete(campaign.Data, id));
            Assert.IsFalse(dialogue.DeliverQuest(), "Accept returns to conversation; a double click cannot deliver materials.");
            dialogue.AskForQuest(); Assert.IsTrue(dialogue.DeliverQuest());
            Assert.IsTrue(VillageQuests.IsComplete(campaign.Data, id));
            string after = Stock();
            Assert.IsFalse(campaign.CompleteVillageQuest(id)); Assert.IsFalse(campaign.AcceptVillageQuest(id));
            Assert.IsTrue(dialogue.Open(campaign, id, speaker.transform)); dialogue.AskForQuest();
            Assert.IsFalse(dialogue.DeliverQuest()); Assert.AreEqual(after, Stock());
        }

        [Test]
        public void AcceptanceSurvivesSaveRoundtripAndOldCompletedProjectsStayCompleted()
        {
            Assert.IsTrue(campaign.AcceptVillageQuest("village:blacksmith"));
            campaign.Restore(JsonUtility.FromJson<ValleyData>(JsonUtility.ToJson(campaign.Data)));
            Assert.IsTrue(VillageQuests.IsAccepted(campaign.Data, "village:blacksmith"));
            StringAssert.Contains("En curso", campaign.VillageBoard);
            string before = Stock();
            campaign.Restore(JsonUtility.FromJson<ValleyData>("{\"nicoWorkshopRepaired\":true}"));
            Assert.IsFalse(campaign.AcceptVillageQuest("village:blacksmith"));
            Assert.IsFalse(campaign.CompleteVillageQuest("village:blacksmith"));
            Assert.IsTrue(VillageQuests.IsComplete(campaign.Data, "village:blacksmith")); Assert.AreEqual(before, Stock());
        }

        [Test]
        public void LockedAndIncompleteQuestsDoNotConsumeAnything()
        {
            campaign.Data.camp = false; string before = Stock();
            Assert.IsFalse(campaign.AcceptVillageQuest("village:blacksmith")); Assert.AreEqual(before, Stock());
            campaign.Data.camp = true; Assert.IsTrue(campaign.AcceptVillageQuest("village:blacksmith"));
            inventory.TryRemoveStone(inventory.Stone); before = Stock();
            Assert.IsFalse(campaign.CompleteVillageQuest("village:blacksmith"));
            Assert.AreEqual(before, Stock()); Assert.IsFalse(campaign.WorkshopRestored);
            Assert.IsFalse(campaign.AcceptVillageQuest("unknown"));
        }

        [Test]
        public void TalkingToRoloAfterCompletionNeverTradesAutomatically()
        {
            campaign.Data.roloMarketOpened = true; string before = Stock();
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(dialogue.Open(campaign, "village:merchant", speaker.transform));
                dialogue.AskAboutVillage(); dialogue.Close();
            }
            Assert.AreEqual(before, Stock());
        }

        [Test]
        public void CloseButtonReleasesInputWithoutAcceptingTheMission()
        {
            dialogue.Open(campaign, "village:elder", speaker.transform); dialogue.AskForQuest();
            dialogue.Root.GetComponentsInChildren<Button>().Single(button => button.name == "Cerrar conversacion").onClick.Invoke();
            Assert.IsFalse(VillageDialogueWindow.IsOpen); Assert.AreEqual(0, campaign.Data.acceptedVillageQuests);
        }

        [Test]
        public void DistanceAndReloadInvalidatePendingDialogueChoices()
        {
            dialogue.Open(campaign, "village:elder", speaker.transform); dialogue.AskForQuest();
            player.transform.position = new Vector3(50, 50);
            Assert.IsFalse(dialogue.AcceptQuest());
            player.transform.position = Vector3.zero; dialogue.Open(campaign, "village:elder", speaker.transform); dialogue.AskForQuest();
            campaign.Restore(JsonUtility.FromJson<ValleyData>(JsonUtility.ToJson(campaign.Data)));
            Assert.IsFalse(dialogue.AcceptQuest()); Assert.AreEqual(0, campaign.Data.acceptedVillageQuests);
        }

        [Test]
        public void ReentrantDeliveryCannotGrantTheRewardTwice()
        {
            campaign.AcceptVillageQuest("village:blacksmith");
            int coins = inventory.Coins;
            int iron = player.GetComponent<AdventureProgress>().Data.iron;
            System.Action repeat = () => campaign.CompleteVillageQuest("village:blacksmith");
            inventory.InventoryChanged += repeat;
            try { Assert.IsTrue(campaign.CompleteVillageQuest("village:blacksmith")); }
            finally { inventory.InventoryChanged -= repeat; }
            Assert.AreEqual(iron + 2, player.GetComponent<AdventureProgress>().Data.iron);
            Assert.AreEqual(coins + 10, inventory.Coins, "Only the first rank reward may add coins.");
            Assert.AreEqual(88, inventory.Wood); Assert.AreEqual(92, inventory.Stone);
        }

        [TestCase("south")]
        [TestCase("west")]
        [TestCase("east")]
        [TestCase("north")]
        public void CampMissionsRequireAcceptanceAllKillsAndExplicitDelivery(string id)
        {
            var quest = CampCombatQuests.Find(id);
            campaign.Data.rankRewardMask = 30;
            string before = Stock();
            Assert.IsTrue(dialogue.Open(campaign, "village:guard", speaker.transform));
            dialogue.AskForCampQuests(); Assert.IsTrue(dialogue.SelectCampQuest(id));
            dialogue.DeclineCampQuest();
            Assert.AreEqual(before, Stock()); Assert.IsFalse(CampCombatQuests.IsAccepted(campaign.Data, id));
            Assert.IsFalse(dialogue.DeliverCampQuest());
            dialogue.SelectCampQuest(id); Assert.IsTrue(dialogue.AcceptCampQuest());
            Assert.AreEqual(before, Stock()); Assert.AreEqual(id, campaign.Data.trackedCampQuest);
            Assert.IsFalse(dialogue.AcceptCampQuest(), "Accepting closes the offer; double clicks must not act again.");
            var state = new EnemyCampState { id = id, defeatedMask = (1 << (quest.Camp.Roster.Length - 1)) - 1 };
            campaign.Data.enemyCamps.Add(state);
            dialogue.SelectCampQuest(id);
            Assert.IsFalse(dialogue.DeliverCampQuest()); Assert.AreEqual(before, Stock());
            state.defeatedMask = (1 << quest.Camp.Roster.Length) - 1;
            int coins = inventory.Coins, influence = campaign.Data.influence, rubies = inventory.GetItemCount("Ruby");
            Assert.IsTrue(dialogue.DeliverCampQuest());
            Assert.AreEqual(coins + quest.Coins, inventory.Coins);
            Assert.AreEqual(influence + quest.Influence, campaign.Data.influence);
            Assert.AreEqual(rubies + quest.Rubies, inventory.GetItemCount("Ruby"));
            Assert.IsNull(campaign.Data.trackedCampQuest);
            Assert.IsTrue(CampCombatQuests.IsClaimed(campaign.Data, id));
            campaign.Restore(JsonUtility.FromJson<ValleyData>(JsonUtility.ToJson(campaign.Data)));
            Assert.IsFalse(campaign.CompleteCampQuest(id)); Assert.IsFalse(campaign.AcceptCampQuest(id));
            Assert.AreEqual(coins + quest.Coins, inventory.Coins);
        }

        [Test]
        public void EarlierCampVictoriesCountAndOldSaveDefaultsAreUnaccepted()
        {
            campaign.Restore(JsonUtility.FromJson<ValleyData>("{\"enemyCamps\":[{\"id\":\"south\",\"claimed\":true}]}"));
            Assert.IsTrue(CampCombatQuests.IsCleared(campaign.Data, "south"));
            Assert.IsFalse(CampCombatQuests.IsAccepted(campaign.Data, "south"));
            Assert.IsFalse(campaign.CompleteCampQuest("south"));
            Assert.IsTrue(campaign.AcceptCampQuest("south"));
            Assert.IsTrue(CampCombatQuests.CanClaim(campaign.Data, "south"));
            StringAssert.Contains("Iria", campaign.HudObjective);
            Assert.IsTrue(campaign.CompleteCampQuest("south"));
        }

        [Test]
        public void PartialKillsAndTrackingSurviveReloadWithoutCountingOtherCamps()
        {
            campaign.AcceptCampQuest("west");
            campaign.Data.enemyCamps.Add(new EnemyCampState { id = "west", defeatedMask = 5 });
            campaign.Data.enemyCamps.Add(new EnemyCampState { id = "south", claimed = true });
            campaign.Restore(JsonUtility.FromJson<ValleyData>(JsonUtility.ToJson(campaign.Data)));
            Assert.AreEqual(2, CampCombatQuests.DefeatedCount(campaign.Data, "west"));
            StringAssert.Contains("2/4", campaign.HudObjective);
            Assert.IsFalse(CampCombatQuests.CanClaim(campaign.Data, "west"));
            Assert.IsFalse(campaign.TrackCampQuest("north"));
            Assert.IsTrue(campaign.TrackCampQuest(null));
            Assert.IsNull(CampCombatQuests.Tracked(campaign.Data));
            Assert.IsFalse(campaign.AcceptCampQuest("unknown"));
        }

        [Test]
        public void CampQuestChoicesRejectOtherResidentsDistanceAndStaleSaves()
        {
            dialogue.Open(campaign, "village:elder", speaker.transform);
            Assert.IsFalse(dialogue.SelectCampQuest("south"));
            dialogue.Open(campaign, "village:guard", speaker.transform); dialogue.SelectCampQuest("south");
            player.transform.position = Vector3.one * 20;
            Assert.IsFalse(dialogue.AcceptCampQuest());
            player.transform.position = Vector3.zero;
            dialogue.Open(campaign, "village:guard", speaker.transform); dialogue.SelectCampQuest("south");
            campaign.Restore(JsonUtility.FromJson<ValleyData>(JsonUtility.ToJson(campaign.Data)));
            Assert.IsFalse(dialogue.AcceptCampQuest());
            Assert.AreEqual(0, campaign.Data.acceptedCampQuests);
        }

        [Test]
        public void ReentrantCampMissionDeliveryPaysOnceAndInvalidTrackingIsNormalized()
        {
            campaign.Data.rankRewardMask = 30;
            campaign.AcceptCampQuest("north");
            campaign.Data.enemyCamps.Add(new EnemyCampState { id = "north", claimed = true });
            int coins = inventory.Coins, rubies = inventory.GetItemCount("Ruby");
            System.Action repeat = () => campaign.CompleteCampQuest("north");
            inventory.InventoryChanged += repeat;
            try { Assert.IsTrue(campaign.CompleteCampQuest("north")); }
            finally { inventory.InventoryChanged -= repeat; }
            Assert.AreEqual(coins + 50, inventory.Coins); Assert.AreEqual(rubies + 1, inventory.GetItemCount("Ruby"));
            campaign.Restore(new ValleyData { acceptedCampQuests = 128, claimedCampQuests = 8, trackedCampQuest = "unknown" });
            Assert.AreEqual(8, campaign.Data.acceptedCampQuests); Assert.IsNull(campaign.Data.trackedCampQuest);
        }
    }
}
