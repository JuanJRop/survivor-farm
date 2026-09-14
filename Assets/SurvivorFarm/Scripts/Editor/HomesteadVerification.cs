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
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace SurvivorFarm.Editor
{
    [InitializeOnLoad] public static class HomesteadVerification
    {
        static HomesteadVerification()
        {
            EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Library/VerifyHomestead.request"))return;File.Delete("Library/VerifyHomestead.request");SessionState.SetBool("HomesteadQA",true);EditorApplication.isPlaying=true;};
            EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("HomesteadQA",false)){SessionState.SetBool("HomesteadQA",false);new GameObject("Homestead QA").AddComponent<HomesteadRunner>();}};
        }
    }
    public sealed class HomesteadRunner : MonoBehaviour
    {
        const string Dir="Design/Validation/Homestead/";const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        void Awake(){Application.runInBackground=true;}
        void Check(bool ok,string text){if(!ok)throw new Exception(text);File.AppendAllText(Dir+"test.txt","PASS: "+text+"\n");}
        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir);File.WriteAllText(Dir+"test.txt","Homestead play-mode checks\n");var run=Run();
            while(true){bool more;try{more=run.MoveNext();}catch(Exception e){File.AppendAllText(Dir+"test.txt","FAIL: "+e);EditorApplication.isPlaying=false;yield break;}if(!more)break;yield return run.Current;}
            File.AppendAllText(Dir+"test.txt","ALL PASSED\n");EditorApplication.isPlaying=false;
        }
        IEnumerator Run()
        {
            yield return null;
            var save=Object.FindFirstObjectByType<GameSaveSystem>();var playerField=typeof(GameSaveSystem).GetField("player",Flags);playerField.SetValue(save,null);save.enabled=false;
            var inv=Object.FindFirstObjectByType<PlayerInventory>();var campaign=inv.GetComponent<ValleyCampaign>();var craft=inv.GetComponent<PlayerCraftingController>();var construction=inv.GetComponent<ConstructionSystem>();
            var stats=inv.GetComponent<PlayerSurvivalStats>();stats.Restore(5,5,.5f);campaign.Restore(null);Object.FindFirstObjectByType<TutorialQuestSystem>().Restore(0,0,0);inv.RestorePacked(null);craft.Restore(0,0,false,false,0,1);
            inv.GetComponent<PlayerMovementController>().enabled=false;foreach(var pool in Object.FindObjectsByType<OutdoorEnemyPool>(FindObjectsSortMode.None))pool.enabled=false;
            var clock=Object.FindFirstObjectByType<DayNightCycle>();clock.Restore(1,8);clock.enabled=false;
            var spawns=Object.FindObjectsByType<ResourceSpawnPoint>(FindObjectsSortMode.None);
            Check(spawns.Count(s=>s.PersistentId.StartsWith("tutorial:arbol"))==14 && spawns.Count(s=>s.PersistentId.StartsWith("tutorial:roca"))==6,"Tutorial adds 14 harvestable trees and 6 rocks");
            Check(Object.FindObjectsByType<TutorialSupply>(FindObjectsSortMode.None).Length==6,"Four teaching stations and two supply caches");
            campaign.Teleport(new Vector3(-5,1.2f));yield return new WaitForSeconds(.5f);ScreenCapture.CaptureScreenshot(Dir+"tutorial.png");yield return new WaitForSeconds(.2f);
            var cache=Object.FindObjectsByType<TutorialSupply>(FindObjectsSortMode.None).First(s=>s.Index==1);campaign.Teleport(cache.transform.position+Vector3.down*.7f);cache.Interact(FarmTool.Sword,inv);int fences=inv.PackedCount("Fence");cache.Interact(FarmTool.Sword,inv);Check(fences==3&&inv.PackedCount("Fence")==3,"Supply reward only once");
            var target=Object.FindFirstObjectByType<TutorialPracticeTarget>();int gold=inv.Coins;for(int i=0;i<3;i++)target.TakeDamage(1,inv);Check(inv.Coins==gold+15,"Practice enemy rewards three hits");yield return new WaitForSeconds(2.1f);for(int i=0;i<3;i++)target.TakeDamage(1,inv);Check(inv.Coins==gold+15,"Practice can repeat without duplicating reward");
            inv.AddWood(100);inv.AddStone(100);inv.AddFood(2);inv.AddFruit(5);
            int wood=inv.Wood,stone=inv.Stone;Check(craft.Craft("Campfire"),"Campfire crafting succeeds");Check(inv.PackedCount("Campfire")==1&&inv.Wood==wood-6&&inv.Stone==stone-4&&!craft.CampfireBuilt,"Crafted fire is a carried item, not an automatic building");
            Check(construction.BeginPacked("Campfire"),"Backpack fire starts placement preview");construction.Cancel();Check(inv.PackedCount("Campfire")==1,"Cancel keeps carried fire");
            campaign.Teleport(new Vector3(3,-3));Check(!construction.PlacePacked("Campfire",Vector2.zero)&&inv.PackedCount("Campfire")==1,"Invalid placement keeps carried fire");
            Vector2 place=Vector2.zero;for(float x=2;x<=10;x+=.5f)for(float y=-6;y<=-2;y+=.5f)if(construction.CanPlace("Campfire",new Vector2(x,y),out _))place=new Vector2(x,y);
            Check(place!=Vector2.zero,"Tutorial retains buildable space");wood=inv.Wood;stone=inv.Stone;Check(construction.PlacePacked("Campfire",place),"Place fire at chosen position");
            Check(inv.PackedCount("Campfire")==0&&inv.Wood==wood&&inv.Stone==stone&&craft.CampfireBuilt,"Placement consumes one kit without paying twice");Check(!construction.PlacePacked("Campfire",place+Vector2.right*1.5f),"Cannot place nonexistent second kit");
            Check(craft.Craft("Food"),"Cooking unlocks after placement");
            gold=inv.Coins;wood=inv.Wood;Check(BackpackActions.Sell(inv,"Wood",3)&&inv.Wood==wood-3&&inv.Coins==gold+9,"Sell uses shop prices and exact selected quantity");
            stone=inv.Stone;gold=inv.Coins;Check(BackpackActions.Remove(inv,"Stone",2)&&inv.Stone==stone-2&&inv.Coins==gold,"Discard removes exact quantity without gold");
            float hunger=stats.HungerPercent;Check(BackpackActions.Use(inv,"Fruit")&&stats.HungerPercent>hunger,"Fruit usable from backpack");
            inv.AddSeeds(SeedRarity.Common,3);inv.AddSeeds(SeedRarity.Magic,2);Check(BackpackActions.Use(inv,"CommonSeeds"),"Select seed from backpack");int magic=inv.MagicSeeds;Check(inv.TryConsumeSeed(out var rarity)&&rarity==SeedRarity.Common&&inv.MagicSeeds==magic,"Plant selected seed rather than spending rare seed");
            inv.AddPacked("Campfire");var state=spawns.Where(s=>s.PersistentId.StartsWith("tutorial:")).First();state.Restore(true);
            playerField.SetValue(save,inv.transform);object data;
            try{data=typeof(GameSaveSystem).GetMethod("BuildSaveData",Flags).Invoke(save,null);data=JsonUtility.FromJson(JsonUtility.ToJson(data),data.GetType());inv.RestorePacked(null);typeof(GameSaveSystem).GetMethod("RestoreSaveData",Flags).Invoke(save,new[]{data});}finally{playerField.SetValue(save,null);}
            Check(inv.PackedCount("Campfire")==1&&inv.PackedCount("Fence")==3&&inv.PreferredSeed==0&&state.Instance.IsHarvested,"Save/load preserves kits, chosen seed and tutorial resource state");
            var legacy=JsonUtility.FromJson(JsonUtility.ToJson(data),data.GetType());legacy.GetType().GetField("version").SetValue(legacy,16);
            var legacyInv=legacy.GetType().GetField("inventory").GetValue(legacy);legacyInv.GetType().GetField("packedBuildings").SetValue(legacyInv,null);
            ((System.Collections.Generic.List<BuildingData>)legacy.GetType().GetField("buildings").GetValue(legacy)).RemoveAll(b=>b.kind=="Campfire");
            playerField.SetValue(save,inv.transform);try{typeof(GameSaveSystem).GetMethod("RestoreSaveData",Flags).Invoke(save,new[]{legacy});Check(inv.PackedCount("Campfire")==1&&!craft.CampfireBuilt,"Old fixed campfire migrates to a placeable item without loss");typeof(GameSaveSystem).GetMethod("RestoreSaveData",Flags).Invoke(save,new[]{data});}finally{playerField.SetValue(save,null);}
            var panel=Object.FindFirstObjectByType<InventoryPanelSystem>();panel.Toggle();yield return null;
            var bag=Object.FindFirstObjectByType<VisualBackpack>();bag.SetCategory(4);var cell=bag.GetComponentsInChildren<BackpackCell>().First(c=>c.Item?.Icon=="Campfire");cell.OnPointerEnter(null);
            Check(bag.GetComponentsInChildren<Button>().Any(b=>b.GetComponentInChildren<Text>()?.text=="Colocar"),"Hover reveals place action");yield return new WaitForSeconds(.2f);ScreenCapture.CaptureScreenshot(Dir+"mochila-acciones.png");yield return new WaitForSeconds(.2f);
            bag.UseSelected();Check(ConstructionSystem.IsPlacing,"Inventory action opens placement");construction.Cancel();panel.Close();
            var plots=Object.FindObjectsByType<FarmingPlot>(FindObjectsSortMode.None).Take(3).ToArray();
            for(int i=0;i<3;i++){plots[i].transform.position=new Vector3(-7+i*1.4f,-3.7f);plots[i].Restore(i==0?1:i==1?2:4,30,0);plots[i].GetComponent<CultivationSoilVisual>().SetImmediate();campaign.World.Label(i==0?"CAVADA":i==1?"LABRADA":"REGADA",plots[i].transform.position+Vector3.up*.85f,.065f);}
            yield return new WaitForSeconds(.3f);
            var colors=plots.Select(p=>p.GetComponentInChildren<CultivationSoilVisual>().GetComponent<SpriteRenderer>().color).ToArray();
            Check(colors[2].grayscale<colors[1].grayscale&&colors[1].grayscale<colors[0].grayscale,"Soil darkens after tilling and watering");
            Check(plots[0].GetComponentsInChildren<SpriteRenderer>().All(s=>!s.name.StartsWith("Surco")||!s.enabled)&&plots[1].GetComponentsInChildren<SpriteRenderer>().Count(s=>s.name.StartsWith("Surco")&&s.enabled)==3,"Hoe adds three visible furrows");
            campaign.Teleport(new Vector3(-5.6f,-4.6f));yield return new WaitForSeconds(.2f);FarmNotificationCenter.Show("");ScreenCapture.CaptureScreenshot(Dir+"etapas-tierra.png");yield return new WaitForSeconds(.3f);
        }
    }
}
