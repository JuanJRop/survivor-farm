using System;
using System.IO;
using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    [InitializeOnLoad]
    public static class OriginalWorldVerification
    {
        private const string Key = "OriginalWorldVerification";
        private const string Output = "Design/Validation/OriginalWorld/";
        private static double next;
        private static int stage;
        private static AnimalRoamingVisual[] animals;
        private static Vector3[] starts;
        private static Sprite[] frames;
        private static Transform player;
        private static float startedAt;
        static OriginalWorldVerification()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
                { stage = 0; next = EditorApplication.timeSinceStartup + 2; }
                if (s == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(Key, false);
            };
        }
        private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
        private static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            const string request = "Library/VerifyOriginalWorld.request";
            if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(request))
            {
                File.Delete(request);
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/SurvivorFarm/Scenes/Main.unity");
                SessionState.SetBool(Key, true); EditorApplication.isPlaying = true; return;
            }
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || next == 0 || EditorApplication.timeSinceStartup < next) return;
            try { Check(); }
            catch (Exception e) { File.WriteAllText(Output + "result.txt", "FAIL\n" + e); stage = 99; }
            next = EditorApplication.timeSinceStartup + (stage == 1 ? 5 : 2);
            if (stage == 99) { EditorApplication.isPlaying = false; SessionState.SetBool(Key, false); }
        }
        private static void Check()
        {
            if (stage == 0)
            {
                Application.runInBackground = true;
                startedAt = Time.time;
                player = Object.FindFirstObjectByType<PlayerMovementController>().transform;
                Require(Vector3.Distance(player.position, new Vector3(-.47f,.83f,0)) < .05f, "Start position changed.");
                // Keep preview teleports and crop checks out of the user's save file, including OnApplicationQuit.
                var save = Object.FindFirstObjectByType<GameSaveSystem>();
                var data = new SerializedObject(save); data.FindProperty("player").objectReferenceValue = null; data.ApplyModifiedPropertiesWithoutUndo(); save.enabled = false;
                var maps = Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
                Require(maps.Any(m => m.name == "Spring Grass" && m.GetUsedTilesCount() > 0), "No terrain tiles.");
                var ground = maps.First(m => m.name == "Spring Grass");
                Require(ground.GetSprite(Vector3Int.zero) == OriginalWorldArtUpgrade.S("Grass"), "Terrain lost its original sprite after scene reload.");
                Require(maps.Any(m => m.name == "Farm Paths" && m.GetUsedTilesCount() >= 5), "No connected path tiles.");
                animals = Object.FindObjectsByType<AnimalRoamingVisual>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Require(animals.Length >= 5, "Missing deer or chickens, including harvested resources.");
                Require(animals.All(a => a.Idle.All(s => s != null) && a.Walk.All(s => s != null)), "Missing directional frames.");
                animals = animals.Where(a => a.isActiveAndEnabled).ToArray();
                starts = animals.Select(a => a.transform.position).ToArray(); frames = animals.Select(a => a.Visual.sprite).ToArray();
                var plot = Object.FindObjectsByType<FarmingPlot>(FindObjectsSortMode.None).First();
                int oldState = plot.StateId, rarity = plot.PlantedSeedRarityId; float remaining = plot.RemainingGrowTime;
                plot.Restore((int)FarmingPlot.PlotState.Growing, 20);
                Require(plot.GetComponent<SpriteRenderer>().sprite.name == "WetSoil", "Watered crop does not use wet soil.");
                plot.Restore((int)FarmingPlot.PlotState.Ready, 0);
                Require(plot.GetComponentsInChildren<SpriteRenderer>().Any(r => r.enabled && r.sprite != null && r.sprite.name == "Crop4"), "Ripe crop art is missing.");
                plot.Restore(oldState, remaining, rarity);
                ScreenCapture.CaptureScreenshot(Output + "gameplay.png"); stage = 1;
            }
            else if (stage == 1)
            {
                Require(animals.Where((a,i) => Vector3.Distance(a.transform.position, starts[i]) > .03f).Any(),
                    "Animals did not move. Game seconds: " + (Time.time-startedAt) + "\n" + string.Join("\n", animals.Select(a => a.name + " speed=" + a.Speed + " position=" + a.transform.position + " walking=" + a.IsWalking + " overlaps=" + string.Join(",", Physics2D.OverlapCircleAll(a.transform.position,a.FootRadius).Select(c=>c.name)))));
                Require(animals.Where((a,i) => a.Visual.sprite != frames[i]).Any(), "Animal animation frames did not change.");
                ScreenCapture.CaptureScreenshot(Output + "animals-moving.png");
                stage = 2;
            }
            else if (stage == 2)
            {
                foreach (var zone in Object.FindObjectsByType<LandUnlockZone>(FindObjectsSortMode.None)) zone.Restore(true);
                player.position = new Vector3(7.6f,-2.6f,0); player.GetComponent<Rigidbody2D>().position = player.position;
                Camera.main.GetComponent<CameraFollowTarget>().SetTarget(player);
                stage = 3;
            }
            else if (stage == 3) { ScreenCapture.CaptureScreenshot(Output + "shop-exterior.png"); stage = 4; }
            else if (stage == 4) { Object.FindFirstObjectByType<ShopEntrance>().RestoreInsideState(true); stage = 5; }
            else if (stage == 5)
            {
                Require(Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None).Any(t => t.name == "Wood Floor"), "Shop interior floor did not activate.");
                ScreenCapture.CaptureScreenshot(Output + "shop-interior.png");
                File.WriteAllText(Output + "result.txt", "PASS\nTerrain and paths use original tiles.\nPlayer starts at the requested position.\nFive animals have complete animation frames, including harvested animals; active movement and frame changes verified over five seconds.\nWet soil and ripe crop visuals follow gameplay state.\nShop entrance activates the textured interior.\nPreview changes excluded from saves."); stage = 6;
            }
            else if (stage == 6)
            {
                var hud = Object.FindFirstObjectByType<SurvivorFarm.Runtime.UI.OriginalSpriteHud>();
                hud.gameObject.SetActive(false);
                var panel = Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None).FirstOrDefault(t => t.name == "Shop Panel");
                if (panel != null) panel.gameObject.SetActive(false);
                player.position = new Vector3(0,-.5f,0); player.GetComponent<Rigidbody2D>().position = player.position;
                Camera.main.GetComponent<CameraFollowTarget>().SetTarget(player);
                stage = 7;
            }
            else if (stage == 7) { ScreenCapture.CaptureScreenshot(Output + "shop-room.png"); stage = 8; }
            else { stage = 99; }
        }
    }
}
