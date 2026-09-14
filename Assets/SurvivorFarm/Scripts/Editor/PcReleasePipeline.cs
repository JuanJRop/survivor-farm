using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace SurvivorFarm.Editor
{
    [InitializeOnLoad] public static class PcReleasePipeline
    {
        static PcReleasePipeline(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(File.Exists("Library/RefreshScripts.request")){File.Delete("Library/RefreshScripts.request");AssetDatabase.Refresh();return;}
            if(File.Exists("Library/BuildWindows.request")){File.Delete("Library/BuildWindows.request");BuildWindows();}
        }
        [MenuItem("Survivor Farm/Build Windows Playtest")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/Windows");Directory.CreateDirectory("Design/Validation/PcRelease");
            try
            {
                PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
                PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=false;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/SurvivorFarm/Scenes/Main.unity"},locationPathName="Builds/Windows/SurvivorFarm.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.CompressWithLz4});
                File.WriteAllText("Design/Validation/PcRelease/build.txt",report.summary.result+" | errors="+report.summary.totalErrors+" | bytes="+report.summary.totalSize+" | time="+report.summary.totalTime);
                if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed");
            }
            catch(Exception e){File.WriteAllText("Design/Validation/PcRelease/build.txt","FAIL: "+e);Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
        }
    }
}
