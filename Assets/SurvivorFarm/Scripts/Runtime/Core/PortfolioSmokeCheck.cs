using System;
using System.Collections;
using System.IO;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Core
{
    /// <summary>Opt-in executable verification; never runs during a normal player's session.</summary>
    public sealed class PortfolioSmokeCheck : MonoBehaviour
    {
        private string output;
        private bool failed;
        private float started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--portfolio-smoke")>=0)
                new GameObject("Portfolio executable QA").AddComponent<PortfolioSmokeCheck>();
        }
        private void Awake()
        {
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../QA/Portfolio"));Directory.CreateDirectory(output);
            Application.runInBackground=true;started=Time.realtimeSinceStartup;Application.logMessageReceived+=Log;
        }
        private void Log(string message,string stack,LogType type)
        {
            if(type!=LogType.Error&&type!=LogType.Exception)return;
            failed=true;File.AppendAllText(Path.Combine(output,"errors.txt"),message+"\n"+stack+"\n");
        }
        private void Update(){if(Time.realtimeSinceStartup-started>120)Finish(false,"Verification timed out.");}
        private IEnumerator Start()
        {
            var run=Run();
            while(true)
            {
                bool next;
                try{next=run.MoveNext();}catch(Exception error){Finish(false,error.ToString());yield break;}
                if(!next)break;yield return run.Current;
            }
            Finish(!failed,"Windows executable: title, crops, recipes, save/load, bounded raids, boss patterns, phase II, ending; staged screenshots captured. This is automated verification, not a human balance playtest.");
        }
        private IEnumerator Run()
        {
            PortfolioSession session=null;
            for(int i=0;i<200;i++){session=PortfolioSession.Instance;if(session!=null&&session.IsReady)break;yield return null;}
            Require(session!=null&&session.IsReady,"Main did not initialize.");
            yield return new WaitForSecondsRealtime(.5f);Capture("01-title");yield return new WaitForSecondsRealtime(.3f);
            session.BeginNewGame();
            yield return new WaitForSecondsRealtime(.7f);Capture("02-day");yield return new WaitForSecondsRealtime(.3f);
            var player=session.Player;var campaign=player.GetComponent<ValleyCampaign>();
            var plots=FindObjectsByType<FarmingPlot>(FindObjectsSortMode.None);
            foreach(var plot in plots)
            {
                campaign.Teleport(plot.transform.position+Vector3.down*.8f);
                plot.Interact(FarmTool.Hoe,player);plot.Interact(FarmTool.WateringCan,player);plot.AdvanceGrowth(36);
            }
            player.GetComponent<PlayerCharacterAnimator>().CancelAction();
            campaign.Teleport(new Vector3(8.5f,-5.8f));
            yield return new WaitForSecondsRealtime(.6f);Capture("03-farming");yield return new WaitForSecondsRealtime(.3f);
            foreach(var plot in plots){campaign.Teleport(plot.transform.position+Vector3.down*.8f);plot.Interact(FarmTool.Hoe,player);}
            Require(player.Fruit>=12,"Crop rewards missing.");
            Require(player.GetComponent<PlayerCraftingController>().Craft("Food"),"Cooking failed.");
            campaign.Teleport(new Vector3(1,-2));
            var save=FindFirstObjectByType<GameSaveSystem>();save.SaveGame(false);
            int wood=player.Wood;player.AddWood(30);Require(save.TryLoadGame()&&player.Wood==wood,"Checkpoint failed.");
            for(int day=1;day<=3;day++)
            {
                session.PrepareNow();session.Advance(26);
                // Allow real AI movement and committed attacks before accelerating the wave schedule.
                for(int i=0;i<6;i++){session.Advance(5);yield return new WaitForSecondsRealtime(.15f);}
                if(day==1){yield return new WaitForSecondsRealtime(.6f);Capture("04-night");yield return new WaitForSecondsRealtime(.3f);}
                for(int i=0;i<180&&session.Phase==SlicePhase.Night;i++)
                {
                    session.Advance(3);
                    foreach(var enemy in session.Raids.Enemies)if(enemy.IsAlive){enemy.TakeDamage(100,player);enemy.ReturnToPool();}
                }
                Require(session.Phase==(day==3?SlicePhase.BossIntro:SlicePhase.Dawn),"Night did not finish.");
                if(day<3)session.Advance(9);
            }
            session.Advance(6);
            var boss=session.Raids.Boss;
            Require(boss!=null,"Boss missing.");
            player.GetComponent<PlayerSurvivalStats>().GrantInvulnerability(120);
            campaign.Teleport(new Vector3(2,-5));
            yield return new WaitForSecondsRealtime(1.25f);Capture("05-boss");yield return new WaitForSecondsRealtime(.3f);
            // Execute the actual attack state machine and verify that all four patterns occur.
            var seen=new System.Collections.Generic.HashSet<int>();
            Time.timeScale=4;
            float end=Time.realtimeSinceStartup+10;
            while(Time.realtimeSinceStartup<end&&session.Phase==SlicePhase.Boss)
            {seen.Add(session.Raids.BossPattern.Pattern);yield return null;}
            Require(seen.Count==4,"Not all four boss attacks executed.");
            Time.timeScale=1;boss.TakeDamage(boss.MaximumHealth/2/(session.Raids.BossPattern.IsExposed?2:1),player);
            Require(boss.IsAlive&&session.Raids.BossPattern.PhaseTwo,"Second phase missing.");
            yield return new WaitForSecondsRealtime(1.1f);Capture("06-phase-two");yield return new WaitForSecondsRealtime(.3f);
            boss.TakeDamage(1000,player);Require(session.Phase==SlicePhase.Victory,"Victory missing.");
            yield return new WaitForSecondsRealtime(1.2f);Capture("07-ending");yield return new WaitForSecondsRealtime(.5f);
        }
        private void Capture(string name)
        {
            // Explicit offscreen rendering works even when the Windows QA helper is hidden.
            // Temporarily route overlay canvases through the same camera, then restore them.
            var camera=Camera.main;
            var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var orders=canvases.Select(c=>c.sortingOrder).ToArray();
            var target=new RenderTexture(1280,720,24);
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
            var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
            try
            {
                camera.targetTexture=target;
                for(int i=0;i<canvases.Length;i++)
                {var canvas=canvases[i];canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder=30000+orders[i];}
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
                var pixels=texture.GetPixels32();int visible=0;
                for(int i=0;i<pixels.Length;i+=101)if(pixels[i].r>12||pixels[i].g>12||pixels[i].b>12)visible++;
                Require(visible>100,"Screenshot contains no rendered scene.");
                File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());
            }
            finally
            {
                for(int i=0;i<canvases.Length;i++)
                {var canvas=canvases[i];canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;canvas.sortingOrder=orders[i];}
                camera.targetTexture=previousTarget;RenderTexture.active=previousActive;target.Release();Destroy(target);Destroy(texture);
            }
        }
        private static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        private void Finish(bool pass,string message){File.WriteAllText(Path.Combine(output,"result.txt"),(pass?"PASS\n":"FAIL\n")+message);Application.Quit(pass?0:1);}
        private void OnDestroy()=>Application.logMessageReceived-=Log;
    }
}
