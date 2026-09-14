using System;
using System.Collections;
using System.Collections.Generic;
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
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    [InitializeOnLoad]
    public static class VillageLayoutVerification
    {
        const string Key = "VillageLayoutVerification";
        static VillageLayoutVerification()
        {
            EditorApplication.playModeStateChanged += state => {
                if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
                SessionState.SetBool(Key, false);
                new GameObject("Village layout QA").AddComponent<VillageLayoutTestRunner>();
            };
        }

        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/SurvivorFarm/Scenes/Main.unity");
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }
    }

    public sealed class VillageLayoutTestRunner : MonoBehaviour
    {
        const string Dir = "Design/Validation/VillageLayout/";
        void Awake()
        {
            Application.runInBackground = true;
            foreach (var smoke in Object.FindObjectsByType<WindowsSmokeCheck>(FindObjectsSortMode.None)) DestroyImmediate(smoke.gameObject);
            var save = Object.FindFirstObjectByType<GameSaveSystem>();
            if (save != null)
            {
                typeof(GameSaveSystem).GetField("player", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(save, null);
                save.enabled = false;
            }
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir);
            var run = Verify();
            while (true)
            {
                bool more;
                try { more = run.MoveNext(); }
                catch (Exception e)
                {
                    File.WriteAllText(Dir + "test.txt", "FAIL: " + e);
                    Debug.LogException(e);
                    if (Application.isBatchMode) EditorApplication.Exit(1);
                    else EditorApplication.isPlaying = false;
                    yield break;
                }
                if (!more) break;
                yield return run.Current;
            }
            File.WriteAllText(Dir + "test.txt", "PASS: connected streets, reachable village services and house doors, consistent house scale, no village world labels, compact HUD, garden layout, board interaction, screenshots at 1920x1080 and 1280x720.\n");
            new GameObject("Campaign regression QA").AddComponent<ValleyTestRunner>();
            Destroy(gameObject);
        }

        IEnumerator Verify()
        {
            yield return null;
            foreach (var smoke in Object.FindObjectsByType<WindowsSmokeCheck>(FindObjectsSortMode.None)) DestroyImmediate(smoke.gameObject);
            var campaign = Object.FindFirstObjectByType<ValleyCampaign>();
            var player = campaign.Inventory;
            campaign.Restore(null);
            player.GetComponent<HouseSystem>().LoadLayout(null);
            player.GetComponent<PlayerMovementController>().enabled = false;
            player.GetComponent<PlayerSurvivalStats>().Restore(5, 5, 1);
            var clock = Object.FindFirstObjectByType<DayNightCycle>();
            clock.Restore(1, 10); clock.enabled = false;
            foreach (var pool in Object.FindObjectsByType<OutdoorEnemyPool>(FindObjectsSortMode.None)) pool.enabled = false;
            var camera = Camera.main;
            camera.GetComponent<CameraFollowTarget>().enabled = false;
            camera.orthographicSize = 6.3f;
            campaign.Teleport(new Vector3(.6f, -.5f));
            camera.transform.position = new Vector3(0, .5f, -10);
            yield return new WaitForSeconds(2);
            var savedCrops = Object.FindObjectsByType<FarmingPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(p => VillageLayout.IsVillage(p.transform.position) && !VillageLayout.IsGarden(p.transform.position))
                .OrderBy(p => p.PersistentId).Take(6).ToArray();
            var cropIds = savedCrops.Select(p => p.PersistentId).ToArray();
            foreach (var crop in savedCrops) crop.Restore(2, 0, 0);
            Check(savedCrops.Length == 6 && savedCrops.All(p => p.StateId == 2 && !p.gameObject.activeInHierarchy && !p.IsAvailable), "Legacy crops must remain stored but inactive");
            Check(savedCrops.Select(p => p.PersistentId).SequenceEqual(cropIds), "Moving crops changed their save identifiers");
            Physics2D.SyncTransforms();
            Check(Vector2.Distance(camera.transform.position, new Vector2(0, .5f)) < .1f, "Another runner moved the village camera");
            Capture("pueblo-inicial.png", 1920, 1080);

            var village = campaign.World.transform.Find("Pueblo inicial - Raizclara");
            Check(village != null, "Village root missing");
            Check(village.GetComponentsInChildren<TextMesh>().Length == 0, "Persistent world labels clutter the village");
            string[] houses = { "Casa de Mara", "Casa del molinero", "Taller de Nico", "Mercado de Rolo" };
            foreach (string name in houses)
            {
                var sprite = village.Find(name).GetComponentInChildren<SpriteRenderer>();
                Check(Mathf.Abs(sprite.bounds.size.x - VillageLayout.HouseWidth) < .03f, "House scale mismatch: " + name);
            }
            var paths = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None).First(t => t.name == "Farm Paths");
            var reachable = ReachableStreets(paths, player.transform);
            foreach (var item in campaign.World.GetComponentsInChildren<ValleyInteraction>().Where(i => i.Id.StartsWith("village:") || i.Id == "note" || i.Id == "go:1" && VillageLayout.IsVillage(i.transform.position)))
                Check(reachable.Any(p => Vector2.Distance(p, item.transform.position) <= 1.3f), "Service has no clear street approach: " + item.Id);
            foreach (string name in houses)
            {
                Vector3 door = village.Find(name).position + new Vector3(.6f, -.2f);
                Check(reachable.Any(p => Vector2.Distance(p, door) < .85f), "Blocked house entrance: " + name);
            }
            Check(!CultivationGrid.IsGreen(Vector3.zero), "Plaza can be tilled or blocked by construction");
            var crops = Object.FindObjectsByType<FarmingPlot>(FindObjectsSortMode.None).Where(p => p.StateId > 0 && VillageLayout.IsVillage(p.transform.position)).ToArray();
            Check(crops.Length == 0, "Active cultivation remains in the village");
            var hud = Object.FindFirstObjectByType<OriginalSpriteHud>();
            Check(hud.HungerLabel == null || !hud.HungerLabel.gameObject.activeInHierarchy, "Hunger UI remains visible");
            Check(hud.HungerFill == null || !hud.HungerFill.gameObject.activeInHierarchy, "Hunger bar remains visible");
            Check(hud.ToolButtons.Where(b => b != null && b.gameObject.activeInHierarchy).Count() == 2, "Toolbar must show two weapons");
            Check(!hud.GetComponentsInChildren<RectTransform>(true).First(t => t.name == "Context").gameObject.activeSelf, "Idle help panel still visible");
            Check(hud.GetComponentsInChildren<Button>().Where(b => b.GetComponent<HudActionTooltip>() != null).All(b => ((RectTransform)b.transform).rect.width <= 60), "Oversized HUD shortcuts");
            yield return new WaitForSeconds(2);
            Capture("pueblo-1920.png", 1920, 1080);
            Capture("pueblo-1280.png", 1280, 720);
            camera.orthographicSize = 8;
            Capture("plano-pueblo.png", 1920, 1080);
            camera.orthographicSize = 6.3f;
            campaign.Data.maraPantryStocked = campaign.Data.nicoWorkshopRepaired = campaign.Data.daliaGardenRestored = campaign.Data.roloMarketOpened = campaign.Data.guardPostBuilt = true;
            campaign.World.Refresh();
            Capture("pueblo-reconstruido.png", 1920, 1080);
            var board = campaign.World.GetComponentsInChildren<ValleyInteraction>().First(i => i.Id == "village:board");
            campaign.Teleport(board.transform.position + Vector3.down * .7f);
            board.Interact(FarmTool.Sword, player);
            Check(AdventureWindow.IsOpen, "Board does not open the journal");
            Object.FindFirstObjectByType<AdventureWindow>().Close();
            campaign.Restore(null);
        }

        static List<Vector2> ReachableStreets(Tilemap paths, Transform player)
        {
            var walkable = new Dictionary<Vector3Int, Vector2>();
            foreach (var cell in paths.cellBounds.allPositionsWithin)
            {
                Vector2 p = paths.GetCellCenterWorld(cell);
                if (!VillageLayout.IsVillage(p) || !paths.HasTile(cell)) continue;
                if (Physics2D.OverlapCircleAll(p, .22f).Any(c => !c.isTrigger && !c.transform.IsChildOf(player))) continue;
                walkable[cell] = p;
            }
            var start = walkable.OrderBy(p => p.Value.sqrMagnitude).First().Key;
            var visited = new HashSet<Vector3Int> { start };
            var queue = new Queue<Vector3Int>(); queue.Enqueue(start);
            Vector3Int[] steps = { Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down };
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var step in steps)
                    if (walkable.ContainsKey(cell + step) && visited.Add(cell + step)) queue.Enqueue(cell + step);
            }
            Check(visited.Count > 150, "Street network is disconnected");
            return visited.Select(c => walkable[c]).ToList();
        }
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
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
            Destroy(target); Destroy(texture);
        }
    }
}
