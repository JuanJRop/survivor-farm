using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    public static class CampCombatQuestChecks
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        static void Click(VillageDialogueWindow window, string caption)
        {
            window.Root.GetComponentsInChildren<Button>().First(b => b.interactable && b.GetComponentInChildren<Text>()?.text == caption).onClick.Invoke();
        }
        static void Roundtrip(PlayerInventory player)
        {
            var save = Object.FindFirstObjectByType<GameSaveSystem>();
            var field = typeof(GameSaveSystem).GetField("player", Flags);
            var previous = field.GetValue(save);
            field.SetValue(save, player.transform);
            try
            {
                var data = typeof(GameSaveSystem).GetMethod("BuildSaveData", Flags).Invoke(save, null);
                data = JsonUtility.FromJson(JsonUtility.ToJson(data), data.GetType());
                typeof(GameSaveSystem).GetMethod("RestoreSaveData", Flags).Invoke(save, new[] { data });
            }
            finally { field.SetValue(save, previous); }
        }

        public static IEnumerator Run(PlayerInventory player, Action<string, int, int> capture)
        {
            if (!GameSaveSystem.IsQa) throw new InvalidOperationException("Combat quest verification requires --qa.");
            var campaign = player.GetComponent<ValleyCampaign>();
            string original = JsonUtility.ToJson(campaign.Data);
            Vector3 position = player.transform.position, cameraPosition = Camera.main.transform.position;
            var dialogue = Object.FindFirstObjectByType<VillageDialogueWindow>();
            var journal = Object.FindFirstObjectByType<AdventureWindow>();
            var iria = campaign.World.GetComponentsInChildren<ValleyInteraction>().Single(n => n.Id == "village:guard");
            var west = player.GetComponent<EnemyCampWorld>().Camps.Single(c => c.Definition.Id == "west");
            var pets = Object.FindObjectsByType<PetCompanion>(FindObjectsSortMode.None).Where(p => p.enabled).ToArray();
            foreach (var pet in pets) pet.enabled = false;
            try
            {
                campaign.Data.rankRewardMask = 30;
                for (int i = 0; i < 2; i++)
                {
                    int width = i == 0 ? 1280 : 1920, height = i == 0 ? 720 : 1080;
                    Screen.SetResolution(width, height, false); yield return new WaitForSeconds(.2f);
                    campaign.Teleport(iria.transform.position + Vector3.down * .8f);
                    iria.Interact(FarmTool.Sword, player); Click(dialogue, "Campamentos");
                    yield return null; capture("camp-missions-iria-" + width + ".png", width, height);
                    dialogue.SelectCampQuest("north");
                    yield return null; capture("camp-mission-offer-" + width + ".png", width, height);
                    Click(dialogue, "Ahora no");
                    Check(campaign.Data.acceptedCampQuests == 0, "Declining a camp mission accepted it.");
                    dialogue.Close();
                }
                iria.Interact(FarmTool.Sword, player); Click(dialogue, "Campamentos"); dialogue.SelectCampQuest("west");
                Click(dialogue, "Aceptar mision"); dialogue.Close();
                Check(CampCombatQuests.IsAccepted(campaign.Data, "west"), "Iria did not accept the combat mission.");
                west.Members[0].TakeDamage(100, player);
                Roundtrip(player);
                Check(CampCombatQuests.DefeatedCount(campaign.Data, "west") == 1 && campaign.Data.trackedCampQuest == "west", "Save/load lost partial kills or tracking.");
                for (int i = 0; i < 2; i++)
                {
                    int width = i == 0 ? 1280 : 1920, height = i == 0 ? 720 : 1080;
                    Screen.SetResolution(width, height, false); yield return new WaitForSeconds(.2f);
                    journal.Open("Journal"); journal.SelectJournalTab("Combat");
                    yield return null; capture("camp-combat-journal-" + width + ".png", width, height);
                    var scroll = Object.FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Single(s => s.name == "Vista del diario");
                    scroll.verticalNormalizedPosition = 0;
                    yield return null; capture("camp-combat-journal-bottom-" + width + ".png", width, height); journal.Close();
                }
                campaign.Teleport(west.transform.position + Vector3.down * 4.5f);
                Camera.main.transform.position = west.transform.position + new Vector3(0, -.8f, -10);
                int coins = player.Coins, food = player.Food, iron = player.GetComponent<AdventureProgress>().Data.iron;
                foreach (var guard in west.Members) if (guard.IsAlive) guard.TakeDamage(100, player);
                Check(west.Claimed && player.Coins == coins + west.Definition.Coins && player.Food == food + west.Definition.Food &&
                    player.GetComponent<AdventureProgress>().Data.iron == iron + west.Definition.Iron, "Last guard failed to deliver automatic supplies.");
                Check(CampCombatQuests.CanClaim(campaign.Data, "west"), "Camp clear did not complete the mission objective.");
                yield return new WaitForSeconds(.9f); capture("camp-victory-rewards-1920.png", 1920, 1080);
                campaign.Teleport(iria.transform.position + Vector3.down * .8f);
                iria.Interact(FarmTool.Sword, player); Click(dialogue, "Campamentos"); dialogue.SelectCampQuest("west");
                yield return null; capture("camp-mission-return-1920.png", 1920, 1080);
                coins = player.Coins;
                Click(dialogue, "Informar victoria");
                Check(player.Coins == coins + CampCombatQuests.Find("west").Coins && CampCombatQuests.IsClaimed(campaign.Data, "west"), "Mission delivery did not grant Iria's separate reward.");
                Roundtrip(player);
                coins = player.Coins;
                Check(!campaign.CompleteCampQuest("west") && !west.TryClaim(player) && player.Coins == coins, "Rewards duplicated after full save roundtrip.");
                Check(CampCombatQuests.Tracked(campaign.Data) == null, "Completed mission remained in HUD.");
                Directory.CreateDirectory("Design/Validation/CampCombatQuests");
                File.WriteAllText("Design/Validation/CampCombatQuests/result.txt", "PASS: Iria combat offers and decline/accept, journal at two resolutions, partial kills/tracking through full GameSaveSystem roundtrip, automatic last-guard supplies, separate NPC mission reward and idempotent reload.\n");
            }
            finally
            {
                dialogue.Close(); journal.Close(); journal.SelectJournalTab("Objective");
                campaign.Restore(JsonUtility.FromJson<ValleyData>(original));
                campaign.Teleport(position); Camera.main.transform.position = cameraPosition;
                foreach (var pet in pets) if (pet != null) pet.enabled = true;
            }
        }
    }
}
