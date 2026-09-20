using System;
using System.Collections;
using System.IO;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEngine;

namespace SurvivorFarm.Runtime.Core
{
    /// <summary>Explicit executable QA for the imported enemies, combat FX and river presentation.</summary>
    public sealed class AssetPackSmokeCheck : MonoBehaviour
    {
        private string output;
        private bool failed,finished;
        private float started;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(GameSaveSystem.IsQa&&Array.IndexOf(Environment.GetCommandLineArgs(),"--asset-pack-smoke")>=0&&
                FindFirstObjectByType<AssetPackSmokeCheck>()==null)
                new GameObject("Imported asset pack executable QA").AddComponent<AssetPackSmokeCheck>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;started=Time.realtimeSinceStartup;
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../QA/AssetPacks"));Directory.CreateDirectory(output);
            Application.logMessageReceived+=Log;
        }
        private void Log(string message,string stack,LogType type)
        {
            if(type!=LogType.Error&&type!=LogType.Exception)return;
            failed=true;File.AppendAllText(Path.Combine(output,"errors.txt"),message+"\n"+stack+"\n");
        }
        private void Update(){if(!finished&&Time.realtimeSinceStartup-started>150)Finish(false,"Timed out.");}
        private IEnumerator Start()
        {
            var run=Run();
            while(true)
            {
                bool next;
                try{next=run.MoveNext();}
                catch(Exception e){Finish(false,e.ToString());yield break;}
                if(!next)break;
                yield return run.Current;
            }
            Finish(!failed,"Upper water advances in sync, lower grass remains static, bridge stays open. " +
                "All four imported enemies spawn from the arena roster and play their authored attacks. " +
                "Actual normal and charged sword hits use the imported Combat FX atlas and damage a living enemy. " +
                "Automated arranged gameplay capture; campaign saves are not used.");
        }

        private IEnumerator Run()
        {
            while(PortfolioSession.Instance==null||!PortfolioSession.Instance.IsReady)yield return null;
            PracticeSession.Open(PracticeMode.Farm);yield return null;
            while(PortfolioSession.Instance?.Practice?.Ready!=true)yield return null;
            var farm=PortfolioSession.Instance.Practice;
            farm.Session.Player.GetComponent<ValleyCampaign>().Teleport(new Vector3(-5,4.7f));
            Frame(new Vector2(-3,6.5f),4);
            yield return new WaitForSecondsRealtime(.2f);
            var shores=FindObjectsByType<EnvironmentSpriteAnimation>(FindObjectsSortMode.None)
                .Where(a=>a.Visual!=null&&a.Visual.sprite!=null&&a.Visual.sprite.texture.name=="LivingWater").ToArray();
            var south=FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(s=>s.enabled&&s.name=="Orilla sur de pasto").ToArray();
            Require(shores.Length>0&&shores.All(a=>a.transform.position.y>7.5f),"Water animation escaped the upper bank.");
            Require(shores.All(a=>a.Visual.sprite==shores[0].Visual.sprite),"Upper bank frames are not synchronized.");
            Require(south.Length>0&&south.All(s=>s.sprite!=null&&s.sprite.texture.name=="TerrainAtlas"),"Lower bank is not native grass.");
            var first=shores[0].Visual.sprite;var lower=south.Select(s=>s.sprite).ToArray();
            Capture("01-river-frame-a");
            yield return new WaitForSeconds(.3f);
            Require(shores[0].Visual.sprite!=first&&shores.All(a=>a.Visual.sprite==shores[0].Visual.sprite),"Upper river did not advance in sync.");
            Require(south.Select(s=>s.sprite).SequenceEqual(lower),"Lower grass changed with the water animation.");
            Capture("02-river-frame-b");
            var bridge=FindFirstObjectByType<RepairableBridge>();Physics2D.SyncTransforms();
            Require(bridge!=null&&bridge.IsRepaired&&!Physics2D.OverlapCircleAll(bridge.transform.position,.35f).Any(c=>!c.isTrigger),"Repaired bridge crossing is blocked.");

            PracticeSession.Open(PracticeMode.Combat);yield return null;
            while(PortfolioSession.Instance?.Practice?.Ready!=true)yield return null;
            var arena=PortfolioSession.Instance.Practice;
            var player=arena.Session.Player;
            EnemyAIBase target=null;
            string[] labels={"soldier","orc","demon","blood-monster"};
            for(int i=0;i<labels.Length;i++)
            {
                arena.Waves.StartSingle(7+i);arena.Waves.Tick(2);arena.Waves.Tick(2);
                yield return null;
                target=arena.Waves.Enemies.Single();
                Require(target.IsAlive&&target.SpriteAnimation!=null,"Imported arena enemy has no live sprite animator: "+labels[i]);
                // Keep the actual selected creature still while its authored attack plays for inspection.
                target.enabled=false;target.transform.position=new Vector3(.5f,-2);
                player.GetComponent<ValleyCampaign>().Teleport(new Vector3(-1.3f,-2));
                Frame(new Vector2(0,-1.1f),2.8f);
                target.SpriteAnimation.PlayAttack(Vector2.left,.8f);
                yield return new WaitForSeconds(.24f);
                Require(target.SpriteAnimation.CurrentFrame>0&&target.SpriteAnimation.Visual.sprite!=null,"Imported attack did not advance: "+labels[i]);
                Capture((3+i).ToString("00")+"-"+labels[i]+"-attack");
            }

            arena.Heal();
            arena.SetToolTier(3);
            player.GetComponent<PlayerToolbelt>().Select(FarmTool.Sword);
            var combat=player.GetComponent<PlayerCombatController>();
            arena.Session.SetPracticePhase(false);
            target.enabled=true;
            target.ConfigureStats("Monstruo de sangre · prueba de impacto",60,1,.4f,.25f,2,0);
            target.ActivateFromPool(new Vector3(.5f,-2));
            player.GetComponent<ValleyCampaign>().Teleport(new Vector3(-.6f,-2));
            Frame(new Vector2(0,-1.5f),2.5f);Physics2D.SyncTransforms();
            yield return new WaitForSeconds(.4f);
            int before=target.CurrentHealth;combat.AttackTarget(target);
            float until=Time.realtimeSinceStartup+3;
            while(player.GetComponent<CombatFeelRangeCue>()?.IsShowing!=true&&Time.realtimeSinceStartup<until)yield return null;
            var cue=player.GetComponent<CombatFeelRangeCue>();
            Require(cue!=null&&cue.IsShowing&&target.CurrentHealth<before,"Normal sword did not produce a visible damaging hit.");
            Require(cue.SweepVisual!=null&&cue.SweepVisual.sprite!=null&&cue.SweepVisual.sprite.texture==CombatFxLibrary.Atlas,"Normal attack uses an unexpected effect atlas.");
            yield return new WaitForSecondsRealtime(.075f);Capture("07-normal-combat-fx");

            yield return new WaitForSecondsRealtime(.75f);
            player.GetComponent<CombatTimeFeedback>()?.Cancel();
            combat.CancelMelee();
            Require(combat.BeginCharge(),"Charged attack could not begin.");
            yield return new WaitForSeconds(SwordChargeController.FullChargeTime+.08f);
            Require(combat.Charge.Progress>=1,"Sword never reached full charge.");
            Require(cue.ChargeVisual != null && cue.ChargeVisual.enabled && cue.ChargeVisual.sprite != null &&
                cue.ChargeVisual.sprite.texture.filterMode == FilterMode.Point, "Charged sword effect is missing or uses blurry filtering.");
            Capture("08-charge-combat-fx");
            before=target.CurrentHealth;combat.ReleaseCharge(target);
            until=Time.realtimeSinceStartup+3;
            while(!cue.IsShowing&&Time.realtimeSinceStartup<until)yield return null;
            Require(combat.LastAttackWasCharged&&cue.IsShowing&&target.CurrentHealth<before,"Charged sword did not produce a visible damaging hit.");
            Require(cue.SweepVisual.sprite!=null&&cue.SweepVisual.sprite.texture==CombatFxLibrary.Atlas,"Charged attack uses an unexpected effect atlas.");
            yield return new WaitForSecondsRealtime(.13f);Capture("09-charged-combat-fx");
        }

        private static void Frame(Vector2 center,float size)
        {
            var camera=Camera.main;Require(camera!=null,"No camera for native screenshots.");
            var follow=camera.GetComponent<CameraFollowTarget>();if(follow!=null)follow.enabled=false;
            var feedback=camera.GetComponent<CameraFeedback>();if(feedback!=null){feedback.RestoreBasePose();feedback.enabled=false;}
            camera.orthographicSize=size;camera.transform.position=new Vector3(center.x,center.y,-10);
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
        private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        private void Finish(bool pass,string message)
        {
            if(finished)return;finished=true;
            File.WriteAllText(Path.Combine(output,"result.txt"),(pass?"PASS\n":"FAIL\n")+message);Application.Quit(pass?0:1);
        }
        private void OnDestroy()=>Application.logMessageReceived-=Log;
    }
}
