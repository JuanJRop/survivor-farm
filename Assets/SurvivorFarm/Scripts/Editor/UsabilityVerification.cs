using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace SurvivorFarm.Editor
{
    [InitializeOnLoad] public static class UsabilityVerification
    {
        static UsabilityVerification()
        {
            EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Library/VerifyUsability.request"))return;File.Delete("Library/VerifyUsability.request");SessionState.SetBool("UsabilityQA",true);EditorApplication.isPlaying=true;};
            EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("UsabilityQA",false)){SessionState.SetBool("UsabilityQA",false);new GameObject("Usability QA").AddComponent<UsabilityRunner>();}};
        }
    }
    public sealed class UsabilityRunner : MonoBehaviour
    {
        const string Dir="Design/Validation/Usability/";const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        void Awake(){Application.runInBackground=true;}
        void Check(bool ok,string message){if(!ok)throw new Exception(message);File.AppendAllText(Dir+"test.txt","PASS: "+message+"\n");}
        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir);File.WriteAllText(Dir+"test.txt","Usability play mode verification\n");var run=Run();
            while(true){bool more;try{more=run.MoveNext();}catch(Exception e){File.AppendAllText(Dir+"test.txt","FAIL: "+e);EditorApplication.isPlaying=false;yield break;}if(!more)break;yield return run.Current;}
            File.AppendAllText(Dir+"test.txt","ALL PASSED\n");EditorApplication.isPlaying=false;
        }
        IEnumerator Run()
        {
            yield return null;
            var save=Object.FindFirstObjectByType<GameSaveSystem>();typeof(GameSaveSystem).GetField("player",Flags).SetValue(save,null);save.enabled=false;
            var inv=Object.FindFirstObjectByType<PlayerInventory>();var campaign=inv.GetComponent<ValleyCampaign>();var quest=Object.FindFirstObjectByType<TutorialQuestSystem>();
            var stats=inv.GetComponent<PlayerSurvivalStats>();stats.Restore(5,5,1);campaign.Restore(null);quest.Restore(0,0,0);
            inv.GetComponent<PlayerMovementController>().enabled=false;foreach(var pool in Object.FindObjectsByType<OutdoorEnemyPool>(FindObjectsSortMode.None))pool.enabled=false;
            var clock=Object.FindFirstObjectByType<DayNightCycle>();clock.Restore(1,8);clock.enabled=false;
            Check(campaign.Objective.Contains("nota")&&!campaign.Journal.Contains("Rey Limo"),"New game shows only first objective without future mission list");
            campaign.Teleport(campaign.Home);yield return new WaitForSeconds(.6f);ScreenCapture.CaptureScreenshot(Dir+"inicio-guiado.png");yield return new WaitForSeconds(.2f);
            campaign.Use("note",FarmTool.Sword);Check(campaign.Objective.Contains("hacha"),"Reading note advances to axe instruction");
            FarmGameEvents.RaiseTreeHarvested();Check(campaign.Objective.Contains("pico"),"One completed task reveals next tool");FarmGameEvents.RaiseRockHarvested();
            var tree=Object.FindObjectsByType<TreeResource>(FindObjectsSortMode.None).First();tree.Restore(false);
            var rock=Object.FindObjectsByType<RockResource>(FindObjectsSortMode.None).First();rock.Restore(false);
            Check(!FarmPlayerInteractor.Supports(tree,FarmTool.Sword)&&FarmPlayerInteractor.Supports(tree,FarmTool.Axe)&&!FarmPlayerInteractor.Supports(rock,FarmTool.Axe)&&FarmPlayerInteractor.Supports(rock,FarmTool.Pickaxe),"Context prompts require correct resource tool");
            Check(inv.GetComponent<CircleCollider2D>().radius<=.28f && tree.GetComponent<CircleCollider2D>().radius<=.3f,"Player and tree have narrow foot colliders");
            var plots=Object.FindObjectsByType<FarmingPlot>(FindObjectsSortMode.None).Where(p=>Mathf.Abs(p.transform.position.x)<36).ToArray();
            Check(plots.All(p=>(p.transform.position-CultivationGrid.Snap(p.transform.position)).sqrMagnitude<.0001f),"All farm cells share aligned grid");
            Check(plots.Select(p=>p.transform.position).Distinct().Count()==plots.Length,"No overlapping farm cells");Check(plots.All(p=>CultivationGrid.IsGreen(p.transform.position)),"All crops remain off roads");
            var plot=plots.First(p=>CultivationGrid.Clear(p.transform.position)&&Mathf.Abs(p.transform.position.x)<10&&Mathf.Abs(p.transform.position.y)<5);plot.Restore(0,0,0);
            Check(CultivationGrid.At(plot.transform.position+new Vector3(.5f,.5f))==plot && CultivationGrid.At(plot.transform.position+Vector3.right*1.4f)!=plot,"Pointer selects exact cell, not nearest neighbour");
            Check(!CultivationGrid.CanWork(plot,plot.transform.position+Vector3.right*3,1.35f,FarmTool.Hoe),"Out of reach cell cannot be opened with hoe");
            Check(!CultivationGrid.IsGreen(Vector3.zero),"Road rejects cultivation");
            Check(!Physics2D.CircleCastAll(new Vector2(-1.8f,1.1f),.27f,Vector2.up,1.9f).Any(h=>!h.collider.isTrigger&&h.collider.GetComponentInParent<PlayerInventory>()==null),"Passage between house and tree is clear");
            Check(!FarmPlayerInteractor.Supports(plot,FarmTool.Sword)&&FarmPlayerInteractor.Supports(plot,FarmTool.Hoe),"Sword cannot interact with ground");
            var animator=inv.GetComponent<PlayerCharacterAnimator>();campaign.Teleport(plot.transform.position+Vector3.down);inv.AddSeeds(5);animator.CancelAction();
            plot.Interact(FarmTool.Hoe,inv);yield return new WaitForSeconds(2);Check(plot.StateId==1,"Hoe opens selected grass cell");animator.CancelAction();
            plot.Interact(FarmTool.Hoe,inv);yield return new WaitForSeconds(2);Check(plot.StateId==2,"Hoe prepares same cell");
            int seeds=inv.CommonSeeds;plot.Interact(FarmTool.Sword,inv);Check(plot.StateId==2&&inv.CommonSeeds==seeds,"Sword cannot plant or consume seeds");
            plot.Interact(FarmTool.Hoe,inv);yield return new WaitForSeconds(2);Check(plot.StateId==3,"Seed planted with hoe");
            plot.Interact(FarmTool.WateringCan,inv);yield return new WaitForSeconds(2);Check(plot.StateId==4||plot.StateId==5,"Watering follows planted crop");
            var before=plots.ToDictionary(p=>p.PersistentId,p=>p.StateId);typeof(GameSaveSystem).GetField("player",Flags).SetValue(save,inv.transform);var data=typeof(GameSaveSystem).GetMethod("BuildSaveData",Flags).Invoke(save,null);plot.Restore(0,0,0);typeof(GameSaveSystem).GetMethod("RestoreSaveData",Flags).Invoke(save,new[]{data});typeof(GameSaveSystem).GetField("player",Flags).SetValue(save,null);Check(plots.All(p=>p.StateId==before[p.PersistentId]),"Save roundtrip restores every crop by stable identity");
            campaign.Data.camp=true;campaign.Travel(1);var enemy=Object.FindObjectsByType<ValleyEnemy>(FindObjectsSortMode.None).First(e=>e.Maximum==4&&Mathf.Abs(e.transform.position.x-100)<14);campaign.Teleport(enemy.transform.position+Vector3.down);enemy.TakeDamage(1,inv);
            Check(enemy.GetComponent<VisibleHitFeedback>()?.IsFlashing==true&&enemy.GetComponentInChildren<SpriteRenderer>().sharedMaterial==VisibleHitFeedback.WhiteMaterial,"Story enemy flashes white after damage");
            Check(Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Any(p=>p.name=="Hit Sparks"&&p.particleCount>0),"Hit emits visible particles");
            yield return null;
            ScreenCapture.CaptureScreenshot(Dir+"impacto.png");yield return new WaitForSeconds(.3f);Check(!enemy.GetComponent<VisibleHitFeedback>().IsFlashing,"Flash ends after hit");
            var prefab=AssetDatabase.LoadAssetAtPath<EnemyLootPickup>("Assets/SurvivorFarm/Prefabs/Items/EnemyCoinLoot.prefab");var coin=EnemyLootPickup.Spawn(prefab,enemy.transform.position+Vector3.right*2,null,ResourceFlyweights.Item(ItemKind.Coins),1);
            Check(coin!=null&&coin.GetComponentInChildren<SpriteRenderer>().bounds.size.x>=.64f,"Dropped gold has larger readable sprite");
            Object.FindFirstObjectByType<AdventureWindow>().Open("Journal");yield return new WaitForSeconds(.2f);ScreenCapture.CaptureScreenshot(Dir+"diario-guiado.png");yield return new WaitForSeconds(.3f);
        }
    }
}
