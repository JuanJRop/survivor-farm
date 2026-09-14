using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    public static class VillageDialogueChecks
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        static void Click(VillageDialogueWindow window, string caption)
        {
            var button = window.Root.GetComponentsInChildren<Button>().First(b => b.interactable && b.GetComponentInChildren<Text>()?.text == caption);
            button.onClick.Invoke();
        }

        public static IEnumerator Run(PlayerInventory inventory, Action<string, int, int> capture)
        {
            if (!GameSaveSystem.IsQa) throw new InvalidOperationException("Dialogue verification requires --qa.");
            var campaign = inventory.GetComponent<ValleyCampaign>();
            campaign.Data.note = campaign.Data.camp = campaign.Data.harvest = true;
            campaign.Data.nicoWorkshopRepaired = false; campaign.Data.acceptedVillageQuests = 0;
            var dialogue = Object.FindFirstObjectByType<VillageDialogueWindow>();
            var residents = campaign.World.GetComponentsInChildren<ValleyInteraction>().Where(npc => VillageQuests.IsVillager(npc.Id)).ToArray();
            Check(residents.Length == 5, "Five villagers must expose conversations.");
            foreach (var resident in residents)
            {
                campaign.Teleport(resident.transform.position + Vector3.down * .8f);
                resident.Interact(FarmTool.Sword, inventory);
                Check(VillageDialogueWindow.IsOpen && dialogue.ResidentId == resident.Id, "Villager did not open its own conversation.");
                Check(dialogue.Root.Find("Aldeano").GetComponent<Image>().sprite != null, "Missing original villager portrait frame.");
                dialogue.Close();
            }
            var nico = residents.Single(npc => npc.Id == "village:blacksmith");
            int wood = inventory.Wood, stone = inventory.Stone, coins = inventory.Coins;
            for (int i = 0; i < 2; i++)
            {
                int width = i == 0 ? 1280 : 1920, height = i == 0 ? 720 : 1080;
                Screen.SetResolution(width, height, false); yield return new WaitForSeconds(.3f);
                campaign.Teleport(nico.transform.position + Vector3.down * .8f); nico.Interact(FarmTool.Sword, inventory);
                yield return null; capture("conversacion-" + width + ".png", width, height);
                Click(dialogue, "\u00bfNecesitas ayuda?"); yield return null;
                capture("oferta-mision-" + width + ".png", width, height);
                Click(dialogue, "Ahora no");
                Check(campaign.Data.acceptedVillageQuests == 0 && inventory.Wood == wood && inventory.Stone == stone && inventory.Coins == coins, "Declining changed mission or resources.");
                dialogue.Close();
            }
            nico.Interact(FarmTool.Sword, inventory); Click(dialogue, "\u00bfNecesitas ayuda?"); Click(dialogue, "Aceptar misi\u00f3n");
            Check(VillageQuests.IsAccepted(campaign.Data, nico.Id) && !campaign.WorkshopRestored && inventory.Wood == wood && inventory.Stone == stone && inventory.Coins == coins, "Accepting a quest must not deliver or reward it.");

            var save = Object.FindFirstObjectByType<GameSaveSystem>();
            var field = typeof(GameSaveSystem).GetField("player", Flags); field.SetValue(save, inventory.transform);
            try
            {
                var data = typeof(GameSaveSystem).GetMethod("BuildSaveData", Flags).Invoke(save, null);
                data = JsonUtility.FromJson(JsonUtility.ToJson(data), data.GetType());
                typeof(GameSaveSystem).GetMethod("RestoreSaveData", Flags).Invoke(save, new[] { data });
                Check(VillageQuests.IsAccepted(campaign.Data, nico.Id) && !VillageDialogueWindow.IsOpen && !campaign.WorkshopRestored, "Save/load lost acceptance or kept a stale dialogue.");
            }
            finally { field.SetValue(save, null); }

            campaign.Teleport(nico.transform.position + Vector3.down * .8f); nico.Interact(FarmTool.Sword, inventory); Click(dialogue, "Ver mi encargo");
            yield return null; capture("mision-en-curso-1920.png", 1920, 1080);
            Click(dialogue, "Entregar materiales");
            Check(campaign.WorkshopRestored && inventory.Wood == wood - 12 && inventory.Stone == stone - 8, "Confirmed quest delivery failed.");
            coins = inventory.Coins; int iron = inventory.GetComponent<AdventureProgress>().Data.iron;
            dialogue.Close(); nico.Interact(FarmTool.Sword, inventory); Click(dialogue, "Encargo completado");
            Check(!dialogue.DeliverQuest() && inventory.Coins == coins && inventory.GetComponent<AdventureProgress>().Data.iron == iron, "Repeated conversation duplicated quest rewards.");
            dialogue.Close();
            Directory.CreateDirectory("Design/Validation/VillageDialogue");
            File.WriteAllText("Design/Validation/VillageDialogue/result.txt", "PASS: five villager conversations with original sprites, decline without side effects, explicit acceptance and delivery buttons, accepted quest save/load, stale dialogue closure, unique rewards, screenshots at 1280x720 and 1920x1080.\n");
        }
    }
}
