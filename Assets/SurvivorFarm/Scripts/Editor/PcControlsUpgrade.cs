using System;
using System.IO;
using SurvivorFarm.Runtime.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivorFarm.Editor
{
    [InitializeOnLoad]
    public static class PcControlsUpgrade
    {
        static PcControlsUpgrade() => EditorApplication.update += Tick;
        private static void Tick()
        {
            const string request = "Library/ApplyPcControls.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { Apply(); File.WriteAllText("Library/PcControls-result.txt", "PASS: PC HUD saved, WASD and mouse controls compiled."); }
            catch (Exception e) { File.WriteAllText("Library/PcControls-result.txt", e.ToString()); Debug.LogException(e); }
        }
        [MenuItem("Survivor Farm/Apply PC Controls")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != "Assets/SurvivorFarm/Scenes/Main.unity")
                throw new InvalidOperationException("Open Main outside Play.");
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (canvas.name == "Farm HUD") PcControlsLayout.Apply(canvas);
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }
    }
}
