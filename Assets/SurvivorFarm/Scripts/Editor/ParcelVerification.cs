using System;
using System.IO;
using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    [InitializeOnLoad]
    public static class ParcelVerification
    {
        static double next;static int stage;const string Key="VerifyScreenParcels";
        static ParcelVerification()
        {
            EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){stage=0;next=EditorApplication.timeSinceStartup+2;}if(s==PlayModeStateChange.EnteredEditMode)SessionState.SetBool(Key,false);};
        }
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(File.Exists("Library/VerifyParcels.request")&&!File.Exists("Library/ExpandParcels.request")&&GameObject.Find("Screen Sized Parcels")!=null&&!EditorApplication.isPlayingOrWillChangePlaymode)
            {File.Delete("Library/VerifyParcels.request");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;return;}
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next||next==0)return;
            try
            {
                if(stage==0)
                {
                    Application.runInBackground=true;
                    var save=Object.FindFirstObjectByType<GameSaveSystem>();var data=new SerializedObject(save);data.FindProperty("player").objectReferenceValue=null;data.ApplyModifiedPropertiesWithoutUndo();save.enabled=false;
                    var zones=Object.FindObjectsByType<LandUnlockZone>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                    if(zones.Length!=8||zones.Any(z=>Mathf.Abs(z.transform.localScale.x-23.85f)>.01f||Mathf.Abs(z.transform.localScale.y-13.85f)>.01f))throw new Exception("Parcel size mismatch.");
                    var plots=Object.FindObjectsByType<FarmingPlot>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                    if(plots.Count(p=>p.name.StartsWith("ZZ Expanded Soil"))<200)throw new Exception("Missing expanded cultivable ground.");
                    foreach(var zone in zones)
                    {
                        var d=new SerializedObject(zone);var point=d.FindProperty("interactionPoint").objectReferenceValue as Transform;
                        var blocker=zone.GetComponent<Collider2D>();
                        if(blocker.enabled&&blocker.OverlapPoint(point.position))throw new Exception("Unlock gate trapped behind its blocker: "+zone.name);
                    }
                }
                else if(stage==1)ScreenCapture.CaptureScreenshot("Design/Validation/Parcels/parcela-pc.png");
                else{File.WriteAllText("Design/Validation/Parcels/result.txt","PASS: 8 surrounding parcels resized; additional cultivable ground present; all unlock interaction points outside blockers; Play preview captured. Test excluded from saves.");SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;}
                stage++;next=EditorApplication.timeSinceStartup+.6;
            }
            catch(Exception e){File.WriteAllText("Design/Validation/Parcels/result.txt","FAIL: "+e);SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;}
        }
    }
}
