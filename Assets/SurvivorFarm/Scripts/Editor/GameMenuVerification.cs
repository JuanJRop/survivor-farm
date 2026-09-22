using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    /// <summary>Runs the real Main-scene new-game flow through every unified menu page.</summary>
    [InitializeOnLoad]
    public static class GameMenuVerification
    {
        private const string Key = "VerifyUnifiedGameMenu";
        private const string Request = "Library/VerifyGameMenu.request";
        private const string Output = "Design/Validation/GameMenu/";

        static GameMenuVerification()
        {
            EditorApplication.update += PollRequest;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
                    new GameObject("Unified game menu QA").AddComponent<GameMenuTestRunner>();
                if (state == PlayModeStateChange.EnteredEditMode)
                    SessionState.SetBool(Key, false);
            };
        }

        public static void Run()
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene("Assets/SurvivorFarm/Scenes/Main.unity");
            SessionState.SetBool(Key, true);
            EditorApplication.isPlaying = true;
        }

        private static void PollRequest()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Request)) return;
            File.Delete(Request);
            Run();
        }
    }

    public sealed class GameMenuTestRunner : MonoBehaviour
    {
        private const string Output = "Design/Validation/GameMenu/";
        private const string PortfolioSlotKey = "SurvivorFarmPortfolioSlot";
        private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private bool hadPortfolioSlot;
        private string previousPortfolioSlot;
        private bool finished;

        private void Awake()
        {
            Application.runInBackground = true;
            foreach (var smoke in Object.FindObjectsByType<WindowsSmokeCheck>(FindObjectsSortMode.None))
                DestroyImmediate(smoke.gameObject);

            hadPortfolioSlot = PlayerPrefs.HasKey(PortfolioSlotKey);
            previousPortfolioSlot = PlayerPrefs.GetString(PortfolioSlotKey, string.Empty);
            DisableSaveWrites();
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(Output);
            IEnumerator run = Verify();
            while (true)
            {
                bool more = false;
                object current = null;
                Exception failure = null;
                try
                {
                    more = run.MoveNext();
                    if (more) current = run.Current;
                }
                catch (Exception error) { failure = error; }

                if (failure != null)
                {
                    Finish(false, failure);
                    yield break;
                }
                if (!more)
                {
                    Finish(true, null);
                    yield break;
                }
                yield return current;
            }
        }

        private IEnumerator Verify()
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while ((PortfolioSession.Instance == null || !PortfolioSession.Instance.IsReady) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            Require(PortfolioSession.Instance != null && PortfolioSession.Instance.IsReady,
                "Portfolio session did not finish initializing.");

            var session = PortfolioSession.Instance;
            DisableSaveWrites();
            var frontEnd = Object.FindFirstObjectByType<DemoFrontEnd>();
            Require(frontEnd != null, "Main scene did not create its title flow.");
            Require(frontEnd.IsVisible, "The new-game flow was not visible before starting.");
            frontEnd.NewGame();
            Require(session.HasBegun && session.Phase == SlicePhase.Day,
                "The production NewGame action did not begin the day phase.");

            var tutorial = Object.FindFirstObjectByType<FarmIntroduction>();
            Require(tutorial != null && FarmIntroduction.IsOpen,
                "New game did not open the tutorial.");
            var introductoryMenu = Object.FindFirstObjectByType<GameMenuWindow>();
            Require(introductoryMenu != null, "The unified menu was not available during the tutorial.");
            introductoryMenu.Open(GameMenuWindow.Page.Skills);
            Require(GameMenuWindow.IsOpen && SkillTreeWindow.IsOpen, "Escape-equivalent menu must also work during the tutorial.");
            introductoryMenu.Close();
            Require(FarmIntroduction.IsOpen && session.IsPaused && Time.timeScale == 0,
                "Closing the menu should preserve the tutorial hint's existing pause.");
            tutorial.Finish();
            yield return null;
            Require(!FarmIntroduction.IsOpen && Time.timeScale > 0 && !session.IsPaused,
                "Finishing the tutorial did not resume gameplay.");

            var menu = Object.FindFirstObjectByType<GameMenuWindow>();
            Require(menu != null, "PlayerInventory did not create the unified menu host.");
            menu.Open(GameMenuWindow.Page.Skills); // Production entry point used by the Escape route.
            yield return null;
            RequireMenuPaused(session, menu, "opening the host");
            Require(menu.Content != null && menu.Content.gameObject.activeInHierarchy,
                "The unified menu content root was not mounted.");

            var pages = new[]
            {
                new PageCheck(GameMenuWindow.Page.Skills, new[] { "PUNTOS DE DESTINO", "NIVEL" }, "habilidades.png"),
                new PageCheck(GameMenuWindow.Page.Workshop, new[] { "TIENDA", "FABRICAR" }, "taller.png"),
                new PageCheck(GameMenuWindow.Page.Upgrades, new[] { "HERRAMIENTAS Y ARMAS", "EQUIPO Y ARMADURAS" }, "mejoras.png"),
                new PageCheck(GameMenuWindow.Page.Equipment, new[] { "Preparación del viajero", "Cinco cajones rápidos" }, "equipo.png"),
                new PageCheck(GameMenuWindow.Page.Settings, new[] { "SONIDO Y PRESENTACIÓN", "CONTROLES" }, "ajustes.png")
            };

            foreach (var page in pages)
            {
                VerifyPageWidgets(session, menu, page.Page, page.RequiredText);
                yield return new WaitForSecondsRealtime(.15f);
                CaptureMenu(Output + page.Screenshot, menu);
            }

            menu.Close();
            yield return null;
            Require(!GameMenuWindow.IsOpen && !session.IsPaused && Time.timeScale > 0,
                "Closing the host did not resume gameplay.");
        }

        private void VerifyPageWidgets(PortfolioSession session, GameMenuWindow menu,
            GameMenuWindow.Page page, string[] requiredText)
        {
            menu.SelectPage(page);
            Canvas.ForceUpdateCanvases();
            menu.FitToViewport();
            Canvas.ForceUpdateCanvases();
            RequireMenuPaused(session, menu, page.ToString());
            Require(menu.CurrentPage == page, "Menu did not select page " + page + ".");
            if (page == GameMenuWindow.Page.Skills)
                VerifySkillNodePointer(menu, "force_bleeding_edge", "Filo sangrante");
            var activeTexts = menu.Content.GetComponentsInChildren<Text>(true)
                .Where(value => value != null && value.gameObject.activeInHierarchy)
                .Select(value => value.text ?? string.Empty).ToArray();
            var missing = requiredText.Where(required => !activeTexts.Any(text =>
                text.IndexOf(required, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
            Require(missing.Length == 0, page + " is missing expected menu widgets: " + string.Join(", ", missing));
            Require(menu.Content.GetComponentsInChildren<Button>(true)
                .Any(button => button != null && button.gameObject.activeInHierarchy),
                page + " has no active buttons.");
        }

        private static void VerifySkillNodePointer(GameMenuWindow menu, string skillId, string selectedName)
        {
            Canvas.ForceUpdateCanvases();
            Transform treeRoot = menu.Content.Find("Árbol de habilidades · tinta y oro");
            Require(treeRoot != null, "The mounted skill tree root is missing.");
            Transform nodeTransform = treeRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(value => value.name == "Técnica " + skillId);
            Require(nodeTransform != null, "The skill tree is missing node " + skillId + ".");

            GameObject node = nodeTransform.gameObject;
            Image circle = node.GetComponent<Image>();
            Button button = node.GetComponent<Button>();
            Require(circle != null && circle.raycastTarget,
                "The circular node is not a raycast target: " + skillId + ".");
            Require(button != null && button.interactable,
                "Locked skill nodes must remain selectable: " + skillId + ".");

            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            Require(eventSystem != null, "The Main scene has no EventSystem for UI input.");
            RectTransform rect = node.GetComponent<RectTransform>();
            Canvas canvas = node.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.rootCanvas.worldCamera : null;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(eventCamera,
                rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(eventSystem)
            {
                position = screen,
                button = PointerEventData.InputButton.Left
            };
            var results = new List<RaycastResult>();
            eventSystem.RaycastAll(pointer, results);
            bool circleHit = results.Any(result => result.gameObject == node);
            string hitNames = string.Join(", ", results.Select(result => result.gameObject.name).ToArray());
            Require(circleHit, "Production UI raycast missed skill node " + skillId +
                " at " + screen + ". Hits: " + hitNames);

            ExecuteEvents.Execute(node, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(node, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(node, pointer, ExecuteEvents.pointerClickHandler);
            var details = treeRoot.GetComponentsInChildren<Text>(true)
                .Where(value => value.gameObject.activeInHierarchy).Select(value => value.text ?? string.Empty).ToArray();
            Require(details.Contains(selectedName), "Clicking " + skillId + " did not select its detail panel.");
            Require(details.Any(value => value.StartsWith("SELLADA", StringComparison.Ordinal)),
                "The clicked locked node should explain its missing requirements.");
        }

        private sealed class PageCheck
        {
            public readonly GameMenuWindow.Page Page;
            public readonly string[] RequiredText;
            public readonly string Screenshot;

            public PageCheck(GameMenuWindow.Page page, string[] requiredText, string screenshot)
            {
                Page = page;
                RequiredText = requiredText;
                Screenshot = screenshot;
            }
        }

        private static void RequireMenuPaused(PortfolioSession session, GameMenuWindow menu, string step)
        {
            Require(GameMenuWindow.IsOpen && session.IsPaused && Mathf.Approximately(Time.timeScale, 0f),
                "The game did not pause while " + step + ".");
            Require(menu.Content != null && menu.Content.gameObject.activeInHierarchy,
                "Menu widgets were not active while " + step + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void CaptureMenu(string path, GameMenuWindow menu)
        {
            const int width = 1280;
            const int height = 720;
            Canvas menuCanvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(canvas => canvas != null && canvas.isRootCanvas && canvas.gameObject.name == "Menú de viaje");
            Require(menuCanvas != null, "Could not find the active menu canvas for rendering.");

            RenderMode previousMode = menuCanvas.renderMode;
            Camera previousCamera = menuCanvas.worldCamera;
            float previousDistance = menuCanvas.planeDistance;
            GameObject cameraObject = null;
            RenderTexture target = null;
            Texture2D image = null;
            RenderTexture previousActive = RenderTexture.active;
            var previousLayers = new List<KeyValuePair<GameObject, int>>();
            try
            {
                foreach (Transform child in menuCanvas.GetComponentsInChildren<Transform>(true))
                {
                    previousLayers.Add(new KeyValuePair<GameObject, int>(child.gameObject, child.gameObject.layer));
                    child.gameObject.layer = 31;
                }

                cameraObject = new GameObject("Game menu QA capture camera");
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.cullingMask = 1 << 31;
                camera.orthographic = true;
                camera.orthographicSize = height * .5f;
                camera.aspect = width / (float)height;
                camera.nearClipPlane = .1f;
                camera.farClipPlane = 1000f;
                camera.transform.position = new Vector3(0f, 0f, -10f);

                target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                target.Create();
                camera.targetTexture = target;
                menuCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                menuCanvas.worldCamera = camera;
                menuCanvas.planeDistance = 1f;
                Canvas.ForceUpdateCanvases();
                if (menu != null) menu.FitToViewport();
                Canvas.ForceUpdateCanvases();
                camera.Render();

                RenderTexture.active = target;
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, image.EncodeToPNG());
                Require(new FileInfo(path).Length > 0, "Rendered screenshot is empty: " + path);
            }
            finally
            {
                RenderTexture.active = previousActive;
                foreach (var layer in previousLayers)
                    if (layer.Key != null) layer.Key.layer = layer.Value;
                menuCanvas.worldCamera = previousCamera;
                menuCanvas.planeDistance = previousDistance;
                menuCanvas.renderMode = previousMode;
                Canvas.ForceUpdateCanvases();
                if (menu != null) menu.FitToViewport();
                if (cameraObject != null) DestroyImmediate(cameraObject);
                if (target != null)
                {
                    target.Release();
                    DestroyImmediate(target);
                }
                if (image != null) DestroyImmediate(image);
            }
        }

        private void DisableSaveWrites()
        {
            var save = Object.FindFirstObjectByType<GameSaveSystem>();
            if (save != null)
            {
                var playerField = typeof(GameSaveSystem).GetField("player", PrivateInstance);
                if (playerField != null) playerField.SetValue(save, null);
                save.enabled = false;
            }

            var session = PortfolioSession.Instance;
            if (session != null)
            {
                var savesField = typeof(PortfolioSession).GetField("saves", PrivateInstance);
                if (savesField != null) savesField.SetValue(session, null);
            }
        }

        private void RestorePortfolioSlot()
        {
            if (hadPortfolioSlot) PlayerPrefs.SetString(PortfolioSlotKey, previousPortfolioSlot);
            else PlayerPrefs.DeleteKey(PortfolioSlotKey);
            PlayerPrefs.Save();
        }

        private void Finish(bool success, Exception error)
        {
            Directory.CreateDirectory(Output);
            if (success)
                File.WriteAllText(Output + "result.txt", "PASS\nMain scene title flow starts a new game and safely closes the tutorial. " +
                    "The unified host opens through its public Escape-equivalent API, all five pages expose their expected widgets, " +
                    "and the session pauses while open and resumes when closed. Captures: habilidades.png, taller.png, mejoras.png, equipo.png, ajustes.png. " +
                    "Save writes were disabled and the prior portfolio slot preference was restored.\n");
            else
            {
                File.WriteAllText(Output + "result.txt", "FAIL\n" + error + "\n");
                Debug.LogException(error);
            }

            RestorePortfolioSlot();
            finished = true;
            SessionState.SetBool("VerifyUnifiedGameMenu", false);
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
        }

        private void OnApplicationQuit()
        {
            if (!finished) RestorePortfolioSlot();
        }
    }
}
