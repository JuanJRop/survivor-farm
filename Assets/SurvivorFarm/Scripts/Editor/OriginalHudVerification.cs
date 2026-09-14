using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    /// <summary>Explicit, one-shot smoke check of the actual Main scene in Play mode.</summary>
    [InitializeOnLoad]
    public static class OriginalHudVerification
    {
        private const string Key = "OriginalHudVerificationRunning";
        private const string Request = "Library/VerifyOriginalHud.request";
        private const string Output = "Design/Validation/OriginalHud";
        private static double checkAt;
        private static double stopAt;
        private static bool checkedScene;

        static OriginalHudVerification()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
                { checkAt = EditorApplication.timeSinceStartup + 1; checkedScene = false; }
                if (state == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(Key, false);
            };
        }

        private static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (!SessionState.GetBool(Key, false) && File.Exists(Request) && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete(Request); Directory.CreateDirectory(Output);
                SessionState.SetBool(Key, true); EditorApplication.isPlaying = true; return;
            }
            if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || checkAt == 0) return;
            if (!checkedScene && EditorApplication.timeSinceStartup >= checkAt)
            {
                checkedScene = true;
                try { Verify(); }
                catch (Exception error) { File.WriteAllText(Output + "/result.txt", "FAIL\n" + error); Debug.LogException(error); }
                stopAt = EditorApplication.timeSinceStartup + 2;
            }
            if (checkedScene && EditorApplication.timeSinceStartup >= stopAt)
            { EditorApplication.isPlaying = false; SessionState.SetBool(Key, false); }
        }

        private static void Require(bool value, string message)
        { if (!value) throw new InvalidOperationException(message); }

        private static void Verify()
        {
            var hud = Object.FindFirstObjectByType<OriginalSpriteHud>();
            Require(hud != null, "Original HUD is missing in Play.");
            var checks = new List<string>();
            Vector3 expected = new Vector3(-.47f, .83f, 0);
            Require(Vector3.Distance(hud.Toolbelt.transform.position, expected) < .02f, "Saved location replaced the requested player start: " + hud.Toolbelt.transform.position);
            Require(Vector3.Distance(Camera.main.transform.position, expected + Vector3.back * 10) < .02f, "Camera does not follow the requested start.");
            checks.Add("Player and camera match requested start, including after loading saved progress.");
            var originalTool = hud.Toolbelt.SelectedTool;
            int manualButtons = 0;
            for (int i = 0; i < hud.ToolButtons.Length; i++)
            {
                bool manualTool = i == (int)FarmTool.Sword || i == (int)FarmTool.Bow || i == (int)FarmTool.Hoe;
                Require(hud.ToolButtons[i] == null || hud.ToolButtons[i].gameObject.activeSelf == manualTool, "Only sword, bow and hoe should be visible in the toolbar.");
                if (!manualTool || hud.ToolButtons[i] == null) continue;
                hud.ToolButtons[i].onClick.Invoke();
                Require(hud.Toolbelt.SelectedTool == (FarmTool)i, "Tool button did not select index " + i);
                Require(hud.ToolFrames != null && hud.ToolFrames.Length > i && hud.ToolFrames[i] != null, "Toolbar frame missing for weapon index " + i);
                Require(hud.ToolFrames[i].sprite == hud.Panel, "Selected tool frame is stale.");
                manualButtons++;
            }
            Require(manualButtons == 3, "Toolbar should expose exactly sword, bow and hoe.");
            hud.Toolbelt.Select(originalTool);
            checks.Add("Sword, bow and hoe toolbar buttons select the correct tool and refresh the selected frame.");
            Require(hud.Coins.text == hud.Inventory.Coins.ToString(), "Wallet differs from inventory.");
            Require(hud.Day.text.Contains(hud.Clock.Day.ToString()), "Day label differs from clock.");
            checks.Add("Wallet and day reflect the current game state.");
            int max = hud.Stats.MaxHealth, health = hud.Stats.CurrentHealth;
            float hunger = hud.Stats.HungerPercent;
            try
            {
                hud.Stats.Restore(5, 3, .5f);
                Require(Mathf.Abs(hud.HungerFill.fillAmount - .5f) < .001f, "Hunger event did not update the bar.");
                Require(hud.Hearts[2].sprite == hud.FullHeart && hud.Hearts[3].sprite == hud.EmptyHeart, "Health event did not update heart sprites.");
            }
            finally { hud.Stats.Restore(max, health, hunger); }
            checks.Add("Health and hunger update through their live events; test state restored.");
            var canvas = hud.GetComponentInParent<Canvas>();
            var inventoryMenu = Object.FindFirstObjectByType<InventoryPanelSystem>();
            var backpack = hud.GetComponentsInChildren<Button>().First(b => b.name == "HUD Backpack");
            backpack.onClick.Invoke();
            var inventoryPanel = canvas.GetComponentsInChildren<RectTransform>(true).First(r => r.name == "Full Inventory Panel");
            Require(inventoryPanel.gameObject.activeSelf, "Backpack does not open inventory.");
            inventoryPanel.GetComponentsInChildren<Button>().First(b => b.name == "Close Inventory").onClick.Invoke();
            Require(!inventoryPanel.gameObject.activeSelf, "Close inventory button is not connected.");
            checks.Add("Backpack opens inventory and the Close button closes it.");
            Require(hud.GetComponentsInChildren<Button>(true).First(b => b.name == "HUD Attack").onClick.GetPersistentEventCount() == 1, "Attack is not wired exactly once.");
            Require(hud.GetComponentsInChildren<Button>(true).First(b => b.name == "HUD Interact").onClick.GetPersistentEventCount() == 1, "Interact is not wired exactly once.");
            checks.Add("Attack and interaction actions have persistent bindings.");
            Canvas.ForceUpdateCanvases();
            foreach (var button in hud.ToolButtons)
            {
                if (button == null) continue;
                Vector3[] corners = new Vector3[4]; ((RectTransform)button.transform).GetWorldCorners(corners);
                Require(corners[0].x >= -.1f && corners[2].x <= Screen.width + .1f, "Toolbar extends outside the screen.");
            }
            checks.Add("Toolbar fits the active game viewport.");
            File.WriteAllText(Output + "/result.txt", "PASS\n" + string.Join("\n", checks) + "\nViewport: " + Screen.width + " x " + Screen.height);
            ScreenCapture.CaptureScreenshot(Output + "/play-mode.png");
        }
    }
}
