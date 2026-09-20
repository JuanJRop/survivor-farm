using System;
using System.Collections;
using System.IO;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Core
{
    /// <summary>Explicit opt-in executable QA, including scene transitions and native render captures.</summary>
    public sealed class PracticeSmokeCheck : MonoBehaviour
    {
        private string output;private bool failed;private float started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(GameSaveSystem.IsQa&&Array.IndexOf(Environment.GetCommandLineArgs(),"--practice-smoke")>=0)
                new GameObject("Practice executable QA").AddComponent<PracticeSmokeCheck>();
        }
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;started=Time.realtimeSinceStartup;
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../QA/Practice"));Directory.CreateDirectory(output);
            Application.logMessageReceived+=Log;
        }
        private void Log(string message,string stack,LogType type)
        {
            if(type!=LogType.Error&&type!=LogType.Exception)return;
            failed=true;File.AppendAllText(Path.Combine(output,"errors.txt"),message+"\n"+stack+"\n");
        }
        private void Update(){if(Time.realtimeSinceStartup-started>150)Finish(false,"Timed out.");}
        private IEnumerator Start()
        {
            var run=Run();while(true)
            {
                bool next;try{next=run.MoveNext();}catch(Exception e){Finish(false,e.ToString());yield break;}
                if(!next)break;yield return run.Current;
            }
            Finish(!failed,"Scene navigation, native arena roster, boss, crops, household furniture, retired defensive construction blocked, farm stations and return to title. Automated verification, not a manual playtest.");
        }
        private IEnumerator Run()
        {
            while(PortfolioSession.Instance==null||!PortfolioSession.Instance.IsReady)yield return null;
            var menu=PortfolioSession.Instance.GetComponent<DemoFrontEnd>();menu.PracticeScreen();yield return new WaitForSecondsRealtime(.3f);Capture("01-menu-practicas");
            PracticeSession.Open(PracticeMode.Combat);yield return null;
            while(PortfolioSession.Instance?.Practice?.Ready!=true)yield return null;
            var arena=PortfolioSession.Instance.Practice;yield return new WaitForSecondsRealtime(.4f);Capture("02-arena");
            arena.Session.Pause(true);yield return null;Capture("03-controles-arena");arena.Session.Pause(false);
            arena.Waves.StartWave(3);
            for(int i=0;i<12;i++){arena.Waves.Tick(3);yield return null;}
            Require(arena.Waves.Alive==6,"Combined wave is missing enemies.");Capture("04-bestiario");
            arena.Waves.StartSingle(6);yield return new WaitForSecondsRealtime(3.7f);Capture("05-custodio");
            Require(arena.Session.Raids.Boss!=null&&arena.Session.Raids.Boss.IsAlive,"Boss failed to spawn.");
            arena.Session.Raids.Boss.TakeDamage(999,arena.Session.Player);yield return new WaitForSecondsRealtime(.4f);
            Require(!arena.Waves.Running,"Boss practice did not end.");
            PracticeSession.Open(PracticeMode.Farm);yield return null;
            while(PortfolioSession.Instance?.Practice?.Ready!=true)yield return null;
            var farm=PortfolioSession.Instance.Practice;yield return new WaitForSecondsRealtime(.4f);Capture("06-taller-huerto");
            farm.Session.Pause(true);yield return null;Capture("07-controles-taller");farm.Session.Pause(false);
            foreach(var plot in FindObjectsByType<FarmingPlot>(FindObjectsSortMode.None).Where(p=>p.StateId==2).Take(6))
            {plot.Interact(FarmTool.Hoe,farm.Session.Player);plot.Interact(FarmTool.WateringCan,farm.Session.Player);}
            farm.GrowCrops();yield return null;Capture("08-cultivos");
            farm.GoToStation(1);Physics2D.SyncTransforms();var build=farm.Session.Player.GetComponent<ConstructionSystem>();
            build.Begin("Fence");Require(!ConstructionSystem.IsPlacing,"Retired defensive construction is still available.");
            var site=FindHouseholdSite(build);build.Begin("Chest");Require(build.PlaceSelected(site),"Household chest placement failed.");
            Require(build.PlaceSelected(FindHouseholdSite(build,site)),"Second household chest placement failed.");build.Cancel();yield return new WaitForSecondsRealtime(.3f);Capture("09-muebles");
            farm.GoToStation(4);farm.SetToolTier(3);yield return new WaitForSecondsRealtime(.3f);Capture("10-mineria");
            farm.ReturnToTitle();yield return null;
            while(PortfolioSession.Instance==null||!PortfolioSession.Instance.IsReady)yield return null;
            Require(!PortfolioSession.Instance.IsPractice&&!PortfolioSession.Instance.HasBegun,"Return to title leaked practice state.");Capture("11-regreso-menu");
        }
        private static Vector2 FindHouseholdSite(ConstructionSystem build,Vector2? avoid=null)
        {
            var origin=(Vector2)build.transform.position;
            for(int ring=2;ring<=10;ring++)for(int x=-ring;x<=ring;x++)for(int y=-ring;y<=ring;y++)
            {
                if(Mathf.Abs(x)!=ring&&Mathf.Abs(y)!=ring)continue;
                var point=build.SnapPlacement("Chest",origin+new Vector2(x,y)*.5f);
                if(avoid.HasValue&&Vector2.Distance(point,avoid.Value)<1.5f)continue;
                if(build.CanPlace("Chest",point,out _))return point;
            }
            throw new Exception("No free grass near the furniture practice station.");
        }
        private void Capture(string name)
        {
            var camera=Camera.main;
            var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var orders=canvases.Select(c=>c.sortingOrder).ToArray();var distances=canvases.Select(c=>c.planeDistance).ToArray();
            var target=new RenderTexture(1280,720,24);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
            var previous=camera.targetTexture;var active=RenderTexture.active;
            try
            {
                camera.targetTexture=target;
                for(int i=0;i<canvases.Length;i++){var c=canvases[i];c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;c.sortingOrder=30000+orders[i];}
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());
            }
            finally
            {
                for(int i=0;i<canvases.Length;i++){var c=canvases[i];c.renderMode=RenderMode.ScreenSpaceOverlay;c.worldCamera=null;c.sortingOrder=orders[i];c.planeDistance=distances[i];}
                camera.targetTexture=previous;RenderTexture.active=active;target.Release();Destroy(target);Destroy(texture);
            }
        }
        private static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        private void Finish(bool pass,string message){File.WriteAllText(Path.Combine(output,"result.txt"),(pass?"PASS\n":"FAIL\n")+message);Application.Quit(pass?0:1);}
        private void OnDestroy()=>Application.logMessageReceived-=Log;
    }
}
