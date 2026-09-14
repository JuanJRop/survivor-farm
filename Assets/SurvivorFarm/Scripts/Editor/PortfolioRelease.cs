using System;
using System.IO;
using System.Linq;
using SurvivorFarm.Runtime.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SurvivorFarm.Editor
{
    public static class PortfolioRelease
    {
        private const string Scene="Assets/SurvivorFarm/Scenes/Main.unity";
        [MenuItem("Survivor Farm/Portfolio/Enable on existing Main")]
        public static void Install()
        {
            var scene=EditorSceneManager.OpenScene(Scene);
            var bootstrap=UnityEngine.Object.FindFirstObjectByType<GameBootstrap>();
            if(bootstrap==null)throw new InvalidOperationException("The authored Main bootstrap is missing; refusing to rebuild it.");
            var session=bootstrap.GetComponent<PortfolioSession>()??bootstrap.gameObject.AddComponent<PortfolioSession>();
            const string path="Assets/SurvivorFarm/Data/ScriptableObjects/PortfolioSettings.asset";
            var settings=AssetDatabase.LoadAssetAtPath<SliceSettings>(path);
            if(settings==null){settings=ScriptableObject.CreateInstance<SliceSettings>();AssetDatabase.CreateAsset(settings,path);}
            var crops=AssetDatabase.LoadAllAssetsAtPath("Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/Farm Crops/Spring/Strawberry.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
            if(crops.Length>=6){settings.cropStages=new[]{crops[1],crops[3],crops[5]};EditorUtility.SetDirty(settings);}
            var serialized=new SerializedObject(session);serialized.FindProperty("settings").objectReferenceValue=settings;serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("PORTFOLIO_INSTALLED: existing Main retained; one director added.");
        }
        [MenuItem("Survivor Farm/Portfolio/Build Windows demo")]
        public static void Build()
        {
            Directory.CreateDirectory("Builds/Portfolio");Directory.CreateDirectory("Design/Validation/Portfolio");
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{Scene},
                locationPathName="Builds/Portfolio/SurvivalFarm.exe",target=BuildTarget.StandaloneWindows64,
                options=BuildOptions.CompressWithLz4|BuildOptions.CleanBuildCache});
            File.WriteAllText("Design/Validation/Portfolio/build.txt",$"{report.summary.result} | errors={report.summary.totalErrors} | bytes={report.summary.totalSize} | duration={report.summary.totalTime}");
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Portfolio Windows build failed.");
        }
    }
}
