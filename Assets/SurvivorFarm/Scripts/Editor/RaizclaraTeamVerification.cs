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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    [InitializeOnLoad]
    public static class RaizclaraTeamVerification
    {
        const string Key = "RaizclaraTeamVerification";
        static RaizclaraTeamVerification()
        {
            EditorApplication.playModeStateChanged += state => {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                SessionState.SetBool(Key, false);
                new GameObject("Team integration QA").AddComponent<RaizclaraTeamTestRunner>();
            };
        }

        public static void Run()
        {
            if (!GameSaveSystem.IsQa) throw new InvalidOperationException("Run integration checks only with --qa in an isolated project.");
            EditorSceneManager.OpenScene("Assets/SurvivorFarm/Scenes/Main.unity");
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }
    }

    public sealed class RaizclaraTeamTestRunner : MonoBehaviour
    {
        const string Dir = "Design/Validation/TeamUi/";
        void Awake()
        {
            foreach (var smoke in Object.FindObjectsByType<WindowsSmokeCheck>(FindObjectsSortMode.None)) DestroyImmediate(smoke.gameObject);
            var save = Object.FindFirstObjectByType<GameSaveSystem>();
            if (save != null)
            {
                typeof(GameSaveSystem).GetField("player", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(save, null);
                save.enabled = false;
            }
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir);
            var run = Verify();
            while (true)
            {
                bool next;
                try { next = run.MoveNext(); }
                catch (Exception e)
                {
                    File.WriteAllText(Dir + "result.txt", "FAIL: " + e);
                    Debug.LogException(e);
                    EditorApplication.Exit(1);
                    yield break;
                }
                if (!next) break;
                yield return run.Current;
            }
            File.WriteAllText(Dir + "result.txt", "PASS: UI captures at 1280x720 and 1920x1080, shared village lots, runtime footprint recorded. Campaign and layout regression follow.\n");
            new GameObject("Village regression QA").AddComponent<VillageLayoutTestRunner>();
            Destroy(gameObject);
        }

        IEnumerator Verify()
        {
            yield return null;
            foreach (var smoke in Object.FindObjectsByType<WindowsSmokeCheck>(FindObjectsSortMode.None)) DestroyImmediate(smoke.gameObject);
            var campaign = Object.FindFirstObjectByType<ValleyCampaign>();
            if (campaign == null) throw new Exception("Campaign missing");
            var inventory = campaign.Inventory;
            inventory.GetComponent<PlayerMovementController>().enabled = false;
            var clock = Object.FindFirstObjectByType<DayNightCycle>();
            clock.Restore(1, 10); clock.enabled = false;
            foreach (var pool in Object.FindObjectsByType<OutdoorEnemyPool>(FindObjectsSortMode.None)) pool.enabled = false;
            inventory.GetComponent<PlayerSurvivalStats>().Restore(5, 5, 1);
            inventory.AddWood(40); inventory.AddStone(40); inventory.AddCoins(150);
            foreach (var item in SurvivalItemCatalog.All) inventory.AddItem(item.Id, 3);
            inventory.AddEquipment("IronSword"); inventory.AddEquipment("HunterBow"); inventory.AddEquipment("IronHelmet");
            campaign.Teleport(new Vector3(.6f, -.5f));
            var camera = Camera.main;
            camera.GetComponent<CameraFollowTarget>().enabled = false;
            camera.orthographicSize = 6.3f;
            camera.transform.position = new Vector3(0, .5f, -10);
            yield return new WaitForSeconds(1);

            foreach (var pool in Object.FindObjectsByType<OutdoorEnemyPool>(FindObjectsSortMode.None))
            {
                // This runner disables population updates before some scene Start callbacks.
                pool.Initialize();
                var positions = (System.Collections.Generic.List<Vector3>)typeof(OutdoorEnemyPool)
                    .GetField("spawnPositions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(pool);
                if (positions.Count == 0) throw new Exception("Outdoor enemies lost their terrain spawn positions");
                bool camp = campaign.Data.camp;
                campaign.Data.camp = true; pool.enabled = true;
                bool spawned = pool.ActiveCount > 0 || pool.SpawnOne();
                pool.enabled = false; campaign.Data.camp = camp;
                if (!spawned) throw new Exception("Outdoor enemies cannot spawn without cultivation plots");
            }

            var enemyChecks = EnemyRosterChecks.Run(inventory, Capture);
            while (enemyChecks.MoveNext()) yield return enemyChecks.Current;

            var campChecks = EnemyCampChecks.Run(inventory, Capture);
            while (campChecks.MoveNext()) yield return campChecks.Current;

            var combatQuestChecks = CampCombatQuestChecks.Run(inventory, Capture);
            while (combatQuestChecks.MoveNext()) yield return combatQuestChecks.Current;

            var regrowthChecks = WildResourceRegrowthChecks.Run(inventory, Capture);
            while (regrowthChecks.MoveNext()) yield return regrowthChecks.Current;

            var dungeonChecks = DungeonExpeditionChecks.Run(inventory, Capture);
            while (dungeonChecks.MoveNext()) yield return dungeonChecks.Current;

            File.WriteAllText(Dir + "footprint.txt", "Controlled QA sample; not a player FPS benchmark.\n" +
                "GameObjects: " + Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length + "\n" +
                "SpriteRenderers: " + Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length + "\n" +
                "Managed bytes: " + UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() + "\n");
            foreach (var lot in VillageLayout.Lots)
            {
                var building = campaign.World.transform.Find("Pueblo inicial - Raizclara/" + lot.Name);
                if (building == null || Vector2.Distance(building.position, lot.Position) > .01f)
                    throw new Exception("Preview/runtime lot mismatch: " + lot.Id);
            }

            var journal = Object.FindFirstObjectByType<AdventureWindow>();
            var crafting = Object.FindFirstObjectByType<CraftingWindow>();
            var equipment = Object.FindFirstObjectByType<PlayerEquipmentWindow>();
            var backpack = Object.FindFirstObjectByType<InventoryPanelSystem>();
            for (int resolution = 0; resolution < 2; resolution++)
            {
                int width = resolution == 0 ? 1280 : 1920, height = resolution == 0 ? 720 : 1080;
                Screen.SetResolution(width, height, false);
                yield return new WaitForSeconds(.5f);
                journal.Open("Journal"); yield return null; yield return null;
                Capture("diario-" + width + ".png", width, height);
                journal.SelectJournalTab("Projects"); yield return null;
                Capture("proyectos-" + width + ".png", width, height);
                journal.SelectJournalTab("Villagers"); yield return null;
                Capture("aldeanos-" + width + ".png", width, height); journal.Close();
                crafting.Open(); yield return null; yield return null;
                Capture("recetas-" + width + ".png", width, height); crafting.Close();
                equipment.Open(); equipment.Preview("IronSword"); yield return null; yield return null;
                Capture("equipo-" + width + ".png", width, height); equipment.Close();
                backpack.Toggle(); yield return null; yield return null;
                Capture("mochila-" + width + ".png", width, height); backpack.Close();
                var shop = Object.FindFirstObjectByType<SimpleShopSystem>();
                shop.OpenService("Food"); yield return null; yield return null;
                Capture("provisiones-" + width + ".png", width, height); shop.ExitShop();
                shop.OpenService("Food"); yield return null; yield return null;
                Capture("mercado-" + width + ".png", width, height); shop.ExitShop();
            }
            var dialogueChecks = VillageDialogueChecks.Run(inventory, Capture);
            while (dialogueChecks.MoveNext()) yield return dialogueChecks.Current;
            ExteriorServicesChecks.Run(inventory);
        }

        static void Capture(string name, int width, int height)
        {
            var camera = Camera.main;
            var target = new RenderTexture(width, height, 24);
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            camera.targetTexture = target;
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; canvas.sortingOrder += 31000; }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
            File.WriteAllBytes(Dir + name, texture.EncodeToPNG());
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; canvas.sortingOrder -= 31000; }
            camera.targetTexture = null; RenderTexture.active = previous;
            Object.Destroy(target); Object.Destroy(texture);
        }
    }
}
