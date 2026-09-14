using System;
using System.IO;
using System.Reflection;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    [InitializeOnLoad]
    public static class DeathMenuVerification
    {
        const string Key="VerifyDeathMenu", Output="Design/Validation/DeathMenu/";
        static int stage; static double next;
        static PlayerSurvivalStats stats; static PlayerRespawnController respawn;
        static GameSaveSystem save; static PlayerInventory inventory; static int coins;
        static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static DeathMenuVerification()
        {
            EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged+=state=>{
                if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){stage=0;next=EditorApplication.timeSinceStartup+2;}
                if(state==PlayModeStateChange.EnteredEditMode)SessionState.SetBool(Key,false);
            };
        }
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static void Kill()
        {
            typeof(PlayerSurvivalStats).GetField("invulnerableUntil",Flags).SetValue(stats,0f);
            stats.TakeDamage(stats.MaxHealth+20);
        }
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            const string request="Library/VerifyDeathMenu.request";
            if(File.Exists(request)&&!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete(request);Directory.CreateDirectory(Output);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;return;
            }
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||next==0||EditorApplication.timeSinceStartup<next)return;
            try
            {
                if(stage==0)
                {
                    Application.runInBackground=true;
                    save=Object.FindFirstObjectByType<GameSaveSystem>();
                    typeof(GameSaveSystem).GetField("player",Flags).SetValue(save,null);save.enabled=false;
                    stats=Object.FindFirstObjectByType<PlayerSurvivalStats>();respawn=stats.GetComponent<PlayerRespawnController>();inventory=stats.GetComponent<PlayerInventory>();coins=inventory.Coins;
                    stats.Restore(5,5,1);Kill();
                }
                else if(stage==1)
                {
                    Require(PlayerRespawnController.MenuOpen&&Time.timeScale==0,"Death menu did not appear and pause gameplay.");
                    ScreenCapture.CaptureScreenshot(Output+"menu-muerte.png");
                }
                else if(stage==2)
                {
                    Vector3 before=stats.transform.position;
                    respawn.ContinueHere();
                    Require(stats.CurrentHealth==stats.MaxHealth&&stats.HungerPercent==1&&Time.timeScale>0,"Continue did not revive and resume.");
                    Require(stats.transform.position==before&&!stats.GetComponent<PlayerCharacterAnimator>().MovementLocked,"Continue moved or left player locked.");
                    stats.TakeDamage(99);Require(stats.CurrentHealth==stats.MaxHealth,"Respawn protection did not prevent immediate death.");
                    respawn.SetCheckpoint(new Vector3(-.47f,.83f,0));stats.transform.position=new Vector3(2,-1,0);Kill();
                }
                else if(stage==3)
                {
                    respawn.ReturnToCheckpoint();
                    Require(Vector3.Distance(stats.transform.position,respawn.Checkpoint)<.01f,"Checkpoint respawn failed.");
                    Require(inventory.Coins==coins,"Respawn changed inventory.");
                    // Exercise the real save loader with a dead character and persisted checkpoint.
                    stats.Restore(5,0,0);
                    typeof(GameSaveSystem).GetField("player",Flags).SetValue(save,stats.transform);
                    try
                    {
                        object state=typeof(GameSaveSystem).GetMethod("BuildSaveData",Flags).Invoke(save,null);
                        string json=JsonUtility.ToJson(state);
                        typeof(GameSaveSystem).GetMethod("RestoreSaveData",Flags).Invoke(save,new[]{JsonUtility.FromJson(json,state.GetType())});
                    }
                    finally{typeof(GameSaveSystem).GetField("player",Flags).SetValue(save,null);}
                    Require(stats.CurrentHealth==stats.MaxHealth&&stats.HungerPercent==1,"Loading a dead save did not recover player.");
                    Require(!stats.GetComponent<PlayerCharacterAnimator>().MovementLocked,"Dead save left animation locked.");
                }
                else
                {
                    Require(Time.timeScale>0&&!PlayerRespawnController.MenuOpen,"Death overlay or pause persisted after recovery.");
                    File.WriteAllText(Output+"result.txt","PASS\nDeath overlay pauses gameplay. Continue revives in place. Checkpoint restores position. Inventory retained. Temporary damage protection. Dead save JSON restores full health/hunger and unlocks animation. No test data written to user save. Exit action not executed during verification.");
                    SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;
                }
                stage++;next=EditorApplication.timeSinceStartup+.7;
            }
            catch(Exception e){File.WriteAllText(Output+"result.txt","FAIL\n"+e);SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;}
        }
    }
}
