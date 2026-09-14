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
    [InitializeOnLoad] public static class PcAdventureVerification
    {
        const string Key="PcAdventureTest";
        static PcAdventureVerification()
        {
            EditorApplication.update+=()=>{if(!EditorApplication.isCompiling&&!EditorApplication.isUpdating&&!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Library/VerifyPcAdventure.request")){File.Delete("Library/VerifyPcAdventure.request");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}};
            EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);new GameObject("Prueba de aventura PC").AddComponent<PcAdventureTestRunner>();}};
        }
    }
    public sealed class PcAdventureTestRunner : MonoBehaviour
    {
        const string Output="Design/Validation/PcRelease/";static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        PlayerInventory inventory;GameSaveSystem save;PlayerCharacterAnimator animator;PlayerSurvivalStats stats;FarmingPlot plot;
        static void Require(bool value,string reason){if(!value)throw new Exception(reason);}
        void Note(string s){File.AppendAllText(Output+"playtest.txt",s+"\n");}
        IEnumerator Start()
        {
            Directory.CreateDirectory(Output);File.WriteAllText(Output+"playtest.txt","Automated Play Mode journey; production sprites and gameplay methods, accelerated world time. User save excluded.\n");
            var run=Run();
            while(true){bool more;try{more=run.MoveNext();}catch(Exception e){Note("FAIL: "+e);End();yield break;}if(!more)break;yield return run.Current;}
            Note("PASS: first-day actions, directional animation, construction/cancellation/collisions/chest, iron tool gate, expedition, upgraded weapon, beacon, save roundtrip, 25 minutes of accelerated engine time.");End();
        }
        void End(){Time.timeScale=1;EditorApplication.isPlaying=false;}
        void Move(Vector2 p){inventory.transform.position=p;var body=inventory.GetComponent<Rigidbody2D>();body.position=p;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();}
        IEnumerator MineVein(IronVein vein)
        {
            var adventure=inventory.GetComponent<AdventureProgress>();int before=adventure.Data.iron;
            vein.Interact(FarmTool.Pickaxe,inventory);
            float timeout=Time.time+4f;
            while(adventure.Data.iron==before&&Time.time<timeout)yield return null;
        }
        IEnumerator Run()
        {
            yield return null;
            Application.runInBackground=true;save=Object.FindFirstObjectByType<GameSaveSystem>();typeof(GameSaveSystem).GetField("player",Flags).SetValue(save,null);save.enabled=false;
            inventory=Object.FindFirstObjectByType<PlayerInventory>();stats=inventory.GetComponent<PlayerSurvivalStats>();stats.Restore(5,5,1);
            inventory.Restore(10,0,0,0,0,0,0,99,0);inventory.RestoreEquipment(null,null);inventory.GetComponent<AdventureProgress>().Restore(null);inventory.GetComponent<ConstructionSystem>().Restore(null);
            inventory.GetComponent<PlayerToolUpgradeController>().Restore(1,1,1);inventory.GetComponent<PlayerCraftingController>().Restore(0,0);
            animator=inventory.GetComponent<PlayerCharacterAnimator>();animator.CancelAction();
            foreach(var enemy in Object.FindObjectsByType<EnemyAIBase>(FindObjectsSortMode.None))enemy.ReturnToPool();foreach(var pool in Object.FindObjectsByType<OutdoorEnemyPool>(FindObjectsSortMode.None))pool.enabled=false;
            var clock=Object.FindFirstObjectByType<DayNightCycle>();clock.Restore(1,8);clock.enabled=false;
            inventory.GetComponent<PlayerMovementController>().enabled=false;var rb=inventory.GetComponent<Rigidbody2D>();rb.simulated=false;
            rb.linearVelocity=Vector2.right*2;yield return null;Require(!inventory.GetComponent<SpriteRenderer>().flipX&&animator.CurrentClip=="Walk","Right walk mirrored");rb.linearVelocity=Vector2.left*2;yield return null;Require(inventory.GetComponent<SpriteRenderer>().flipX,"Left walk not mirrored");rb.linearVelocity=Vector2.zero;
            var quest=Object.FindFirstObjectByType<TutorialQuestSystem>();quest.Restore(0,0,0);
            foreach(var tree in Object.FindObjectsByType<TreeResource>(FindObjectsSortMode.None).Take(12)){tree.Restore(false);Move(tree.transform.position+Vector3.down);while(!tree.IsHarvested){tree.Interact(FarmTool.Axe,inventory);yield return new WaitForSeconds(.65f);}}
            foreach(var rock in Object.FindObjectsByType<RockResource>(FindObjectsSortMode.None).Take(8)){rock.Restore(false);Move(rock.transform.position+Vector3.down);while(!rock.IsHarvested){rock.Interact(FarmTool.Pickaxe,inventory);yield return new WaitForSeconds(.65f);}}
            Require(inventory.Wood>0&&inventory.Stone>0,"Real gathering failed");Note("Real tree/rock interactions granted resources.");
            plot=Object.FindObjectsByType<FarmingPlot>(FindObjectsSortMode.None).First();plot.Restore((int)FarmingPlot.PlotState.Grass,0);Move(plot.transform.position+Vector3.down);
            animator.CancelAction();plot.Interact(FarmTool.Hoe,inventory);yield return new WaitForSeconds(2);Require(plot.StateId==(int)FarmingPlot.PlotState.Dug,"Opening soil with hoe did not complete");
            animator.CancelAction();plot.Interact(FarmTool.Hoe,inventory);yield return new WaitForSeconds(2);Require(plot.StateId==(int)FarmingPlot.PlotState.Tilled,"Hoe did not complete");
            plot.Interact(FarmTool.Hoe,inventory);yield return new WaitForSeconds(2);Require(plot.StateId==(int)FarmingPlot.PlotState.SeededDry,"Plant did not complete");
            plot.Interact(FarmTool.WateringCan,inventory);yield return new WaitForSeconds(12);Require(plot.StateId==(int)FarmingPlot.PlotState.Ready,"Crop never ripened");plot.Interact(FarmTool.Hoe,inventory);yield return new WaitForSeconds(1);Require(inventory.Fruit>0,"Harvest missing");
            // Repeat crop cycle to provide enough fruit for cooking through actual growth.
            yield return new WaitForSeconds(1);plot.Interact(FarmTool.Hoe,inventory);yield return new WaitForSeconds(2);plot.Interact(FarmTool.WateringCan,inventory);yield return new WaitForSeconds(12);plot.Interact(FarmTool.Hoe,inventory);yield return new WaitForSeconds(1);
            var crafting=inventory.GetComponent<PlayerCraftingController>();Require(crafting.Craft("Campfire"),"Campfire crafting unavailable after gathering");crafting.RegisterPlacedFire();Require(crafting.Craft("Food")&&crafting.Craft("Bed"),"First day crafting unavailable after gathering");crafting.RegisterPlacedBed();quest.Evaluate();Require(quest.QuestIndex==10,"First day objectives did not advance");Note("Hoe open → hoe prepare → plant → water → natural growth → harvest → cook → bed passed.");
            inventory.AddWood(200);inventory.AddStone(200);inventory.AddCoins(200); // Controlled fixtures for construction/progression tests below.
            var build=inventory.GetComponent<ConstructionSystem>();Move(new Vector2(4,4));int wood=inventory.Wood;build.Begin("Chest");build.Cancel();Require(inventory.Wood==wood,"Cancelling spent resources");Require(!build.Place("Chest",inventory.transform.position)&&inventory.Wood==wood,"Built on player or spent on invalid location");
            Vector2 Position(string kind)
            {for(float x=2;x<8;x+=1.1f)for(float y=2;y<6;y+=1.1f)if(build.CanPlace(kind,new Vector2(x,y),out _))return new Vector2(x,y);throw new Exception("No valid construction position");}
            var chestPoint=Position("Chest");Require(build.Place("Chest",chestPoint),"Chest placement failed");Move(chestPoint+Vector2.down);var chest=build.Buildings.First(b=>b.kind=="Chest");int before=inventory.Wood;Require(build.Transfer(chest,"Wood",true)&&chest.wood==10&&inventory.Wood==before-10,"Chest deposit failed");Require(build.Transfer(chest,"Wood",false)&&chest.wood==0&&inventory.Wood==before,"Chest withdraw failed");build.Transfer(chest,"Food",true);
            Move(new Vector2(4,4));Require(build.Place("Fence",Position("Fence")),"Fence placement failed");var benchPoint=Position("Workbench");Require(build.Place("Workbench",benchPoint),"Workbench placement failed");
            var adventure=inventory.GetComponent<AdventureProgress>();var vein=Object.FindObjectsByType<IronVein>(FindObjectsSortMode.None).First();vein.Interact(FarmTool.Pickaxe,inventory);Require(adventure.Data.iron==0,"Iron ignored tool gate");Require(inventory.GetComponent<PlayerToolUpgradeController>().TryUpgrade(FarmTool.Pickaxe),"Pico upgrade failed");
            foreach(var v in Object.FindObjectsByType<IronVein>(FindObjectsSortMode.None))yield return MineVein(v);Require(adventure.Data.iron==12,"Iron gathering failed");vein.Interact(FarmTool.Pickaxe,inventory);Require(adventure.Data.iron==12,"Iron duplicate same day");
            adventure.EnterRuins();var foe=new GameObject("QA Golem",typeof(CircleCollider2D)).AddComponent<BasicEnemyAI>();foe.Configure(inventory.transform,null);foe.ConfigureStats("Golem",8,2,1,1,1,12);foe.ActivateFromPool(new Vector3(8,8));clock.Restore(1,22);foe.TakeDamage(100,inventory);Require(adventure.Data.defeatedGolem&&inventory.OwnsEquipment("Gem"),"Golem objective failed");
            var home=Object.FindFirstObjectByType<HomeSafeZone>();Move(home.Center);yield return null;Require(adventure.Data.returnedHome,"Return home objective failed");clock.Restore(2,6);quest.Evaluate();Require(quest.Complete,"Night objective failed");
            Move(benchPoint+Vector2.down);Require(build.TemperSword()&&adventure.Data.temperedBlade,"Workbench progression failed");clock.Restore(3,8);foreach(var v in Object.FindObjectsByType<IronVein>(FindObjectsSortMode.None))yield return MineVein(v);
            Move(new Vector2(4,4));Require(build.Place("Beacon",Position("Beacon"))&&adventure.Data.beacon,"Beacon goal failed");
            int buildings=build.Buildings.Count,iron=adventure.Data.iron;typeof(GameSaveSystem).GetField("player",Flags).SetValue(save,inventory.transform);
            try{var data=typeof(GameSaveSystem).GetMethod("BuildSaveData",Flags).Invoke(save,null);var json=JsonUtility.ToJson(data);adventure.Restore(null);build.Restore(null);typeof(GameSaveSystem).GetMethod("RestoreSaveData",Flags).Invoke(save,new[]{JsonUtility.FromJson(json,data.GetType())});Require(adventure.Data.beacon&&adventure.Data.temperedBlade&&adventure.Data.iron==iron&&build.Buildings.Count==buildings,"New progression/buildings lost in save roundtrip");}finally{typeof(GameSaveSystem).GetField("player",Flags).SetValue(save,null);}
            Note("Construction, storage, tool-gated iron, expedition, blade, beacon and save roundtrip passed.");
            Move(home.Center);stats.Restore(5,5,1);inventory.AddFood(12);clock.Restore(1,8);clock.enabled=true;Time.timeScale=20;
            float end=Time.time+1500,nextNote=Time.time+300;
            while(Time.time<end){if(stats.HungerPercent<.8f)Object.FindFirstObjectByType<InventoryPanelSystem>().EatFood();Require(stats.CurrentHealth>0,"Died during fed survival cycle");if(Time.time>=nextNote){Note("Engine simulation: day "+clock.Day+" hour "+clock.Hour.ToString("0.0")+", hunger "+stats.HungerPercent.ToString("0.00"));nextNote+=300;}yield return null;}
            Time.timeScale=1;clock.enabled=false;Require(clock.Day>=2,"World clock did not progress");
            var window=Object.FindFirstObjectByType<AdventureWindow>();window.Open("Build");yield return new WaitForSeconds(.3f);ScreenCapture.CaptureScreenshot(Output+"construccion.png");yield return new WaitForSeconds(.3f);window.Open("Journal");yield return new WaitForSeconds(.3f);ScreenCapture.CaptureScreenshot(Output+"diario.png");yield return new WaitForSeconds(.3f);window.Close();
        }
    }
}
