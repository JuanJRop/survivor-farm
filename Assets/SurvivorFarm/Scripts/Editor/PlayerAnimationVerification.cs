using System;
using System.IO;
using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    [InitializeOnLoad]
    public static class PlayerAnimationVerification
    {
        private const string Key = "PlayerAnimationVerification";
        private const string Output = "Design/Validation/PlayerAnimations/";
        private static double next;
        private static int stage;
        private static PlayerCharacterAnimator animator;
        private static PlayerMovementController movement;
        private static Rigidbody2D body;
        private static FarmingPlot plot;
        private static int testedFrames;
        static PlayerAnimationVerification()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false)) { stage=0; next=EditorApplication.timeSinceStartup+2; }
                if (s == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(Key,false);
            };
        }
        private static void Require(bool condition,string message) { if (!condition) throw new Exception(message); }
        private static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            const string request="Library/VerifyPlayerAnimations.request";
            if (File.Exists(request) && !EditorApplication.isPlayingOrWillChangePlaymode)
            { File.Delete(request); EditorSceneManager.OpenScene("Assets/SurvivorFarm/Scenes/Main.unity"); SessionState.SetBool(Key,true); EditorApplication.isPlaying=true; return; }
            if (!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || next==0 || EditorApplication.timeSinceStartup<next) return;
            try { Check(); }
            catch(Exception e) { File.WriteAllText(Output+"result.txt","FAIL\n"+e); stage=99; }
            next=EditorApplication.timeSinceStartup+.22;
            if(stage==99) { EditorApplication.isPlaying=false; SessionState.SetBool(Key,false); }
        }
        private static void Check()
        {
            if(stage==0)
            {
                Application.runInBackground=true;
                animator=Object.FindFirstObjectByType<PlayerCharacterAnimator>(); movement=animator.GetComponent<PlayerMovementController>(); body=animator.GetComponent<Rigidbody2D>();
                var save=Object.FindFirstObjectByType<GameSaveSystem>(); var data=new SerializedObject(save); data.FindProperty("player").objectReferenceValue=null; data.ApplyModifiedPropertiesWithoutUndo(); save.enabled=false;
                animator.GetComponent<PlayerSurvivalStats>().Restore(5,5,1);
                foreach(var enemy in Object.FindObjectsByType<EnemyAIBase>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
                Require(animator.Library!=null && animator.Library.Clips.Length>=45,"Complete library was not loaded at runtime.");
                testedFrames=0;
                foreach(var clip in animator.Library.Clips)
                    for(int d=0;d<3;d++) for(int f=0;f<clip.Frames;f++)
                    {
                        var sprite=animator.Library.Frame(clip,d,f);
                        Require(sprite!=null && sprite.rect.xMin>=0 && sprite.rect.yMin>=0 && sprite.rect.xMax<=clip.Atlas.width && sprite.rect.yMax<=clip.Atlas.height,"Invalid frame: "+clip.Name);
                        testedFrames++;
                    }
                movement.enabled=false; body.simulated=false; body.linearVelocity=Vector2.right*2;
            }
            else if(stage==1)
            {
                Require(animator.CurrentClip=="Walk" && animator.CurrentDirection==2 && animator.CurrentFrame>0,"Right-facing walk did not animate.");
                body.linearVelocity=Vector2.up*5;
            }
            else if(stage==2)
            {
                Require(animator.CurrentClip=="Run" && animator.CurrentDirection==1,"Back-facing run did not animate.");
                body.linearVelocity=Vector2.zero; body.simulated=true; movement.enabled=true;
                animator.GetComponent<PlayerToolbelt>().Select(FarmTool.Sword);
                animator.GetComponent<PlayerCombatController>().Attack();
                Require(animator.CurrentClip=="Sword","Attack button did not start sword animation.");
            }
            else if(stage==3)
            {
                Require(animator.CurrentClip=="Sword" && animator.CurrentFrame>0 && animator.MovementLocked,"Sword frames or action lock failed.");
                ScreenCapture.CaptureScreenshot(Output+"sword.png");
            }
            else if(stage==4)
            {
                animator.CancelAction();
                plot=Object.FindObjectsByType<FarmingPlot>(FindObjectsSortMode.None).First();
                plot.Restore((int)FarmingPlot.PlotState.Tilled,0);
                ResourceFlyweights.Seed(SeedRarity.Common).Grant(animator.GetComponent<PlayerInventory>(),1);
                plot.Interact(FarmTool.Hoe,animator.GetComponent<PlayerInventory>());
                Require(animator.CurrentClip=="Plant","Planting did not trigger planting animation.");
            }
            else if(stage==5)
            {
                Require(animator.CurrentFrame>0,"Planting strip did not advance."); ScreenCapture.CaptureScreenshot(Output+"plant.png");
            }
            else if(stage==6)
            {
                animator.CancelAction(); plot.Restore((int)FarmingPlot.PlotState.SeededDry,0);
                plot.Interact(FarmTool.WateringCan,animator.GetComponent<PlayerInventory>());
                Require(animator.CurrentClip=="Watering","Watering did not trigger watering animation.");
            }
            else if(stage==7)
            {
                Require(animator.CurrentFrame>0,"Watering strip did not advance."); ScreenCapture.CaptureScreenshot(Output+"watering.png");
            }
            else if(stage==8)
            {
                animator.CancelAction(); plot.Restore((int)FarmingPlot.PlotState.Ready,0);
                plot.Interact(FarmTool.Hoe,animator.GetComponent<PlayerInventory>());
                Require(animator.CurrentClip=="PickUp","Harvest did not trigger pickup.");
            }
            else if(stage==9)
            {
                animator.GetComponent<PlayerSurvivalStats>().TakeDamage(1);
                Require(animator.CurrentClip=="Damage","Damage animation did not start.");
            }
            else if(stage==10) { animator.GetComponent<PlayerSurvivalStats>().TakeDamage(100); }
            else if(stage==11)
            {
                Require(animator.CurrentClip=="Dead" && animator.MovementLocked,"Death pose or movement lock failed.");
                File.WriteAllText(Output+"result.txt","PASS\n"+animator.Library.Clips.Length+" complete character clips loaded after reloading Main.\n"+testedFrames+" directional frames verified within atlas bounds.\nWalk, run, facing, sword attack, planting, watering, harvesting, damage and death verified through live gameplay hooks.\nTemporary test state excluded from saves.");
            }
            else { stage=99; return; }
            stage++;
        }
    }
}
