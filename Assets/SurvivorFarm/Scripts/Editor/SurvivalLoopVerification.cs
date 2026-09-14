using System;
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
    [InitializeOnLoad] public static class SurvivalLoopVerification
    {
        const string Key="VerifySurvivalLoop", Output="Design/Validation/SurvivalLoop/";
        static int stage;static double next;
        static PlayerInventory inventory;static GameSaveSystem save;static BasicEnemyAI enemy;static PlayerSurvivalStats stats;
        static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static SurvivalLoopVerification()
        {
            EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){stage=0;next=EditorApplication.timeSinceStartup+2;}if(s==PlayModeStateChange.EnteredEditMode)SessionState.SetBool(Key,false);};
        }
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static void Set(object target,string field,object value)=>target.GetType().GetField(field,Flags).SetValue(target,value);
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(File.Exists("Library/VerifySurvivalLoop.request")&&!EditorApplication.isPlayingOrWillChangePlaymode)
            {File.Delete("Library/VerifySurvivalLoop.request");Directory.CreateDirectory(Output);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;return;}
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||next==0||EditorApplication.timeSinceStartup<next)return;
            try
            {
                if(stage==0)
                {
                    Application.runInBackground=true;save=Object.FindFirstObjectByType<GameSaveSystem>();Set(save,"player",null);save.enabled=false;
                    inventory=Object.FindFirstObjectByType<PlayerInventory>();stats=inventory.GetComponent<PlayerSurvivalStats>();stats.Restore(5,5,1);Set(stats,"invulnerableUntil",0f);
                    foreach(var e in Object.FindObjectsByType<EnemyAIBase>(FindObjectsSortMode.None))e.ReturnToPool();
                    foreach(var pool in Object.FindObjectsByType<OutdoorEnemyPool>(FindObjectsSortMode.None))pool.enabled=false;
                    var craft=inventory.GetComponent<PlayerCraftingController>();craft.Restore(0,0);inventory.Restore(10,0,0,0,0,0,0,99,0);
                    Require(!craft.Craft("Campfire")&&inventory.Wood==0,"Crafting without materials succeeded");
                    inventory.AddWood(100);inventory.AddStone(100);inventory.AddFruit(20);
                    Require(!craft.Craft("Food")&&inventory.Fruit==20,"Cooking bypassed fogata");
                    Require(craft.Craft("Campfire")&&inventory.Wood==94&&inventory.Stone==96,"Wrong campfire payment");
                    Require(!craft.Craft("Campfire")&&inventory.Wood==94,"Duplicate campfire charged materials");
                    craft.RegisterPlacedFire();
                    Require(craft.Craft("Food")&&inventory.Food==1&&inventory.Fruit==18,"Food recipe failed");
                    Require(craft.Craft("Bed"),"Bed failed");craft.RegisterPlacedBed();Require(craft.Craft("Sword")&&craft.WeaponLevel==2,"Weapon progression failed");
                    inventory.RestoreEquipment(null,null);
                    Require(craft.Craft("Helmet")&&craft.Craft("Chestplate")&&craft.Craft("Boots"),"Equipment recipes failed");
                    inventory.Equip("Helmet",0);inventory.Equip("Chestplate",1);inventory.Equip("Boots",2);
                    Require(Mathf.Abs(inventory.ArmorReduction-.4f)<.001f&&inventory.MovementBonus>1,"Equipment effects missing");
                    stats.TakeDamage(3);Require(stats.CurrentHealth==3,"Armour did not mitigate cumulative damage");stats.Restore(5,5,1);
                    var clock=Object.FindFirstObjectByType<DayNightCycle>();clock.enabled=false;clock.Restore(1,8);
                    var quest=Object.FindFirstObjectByType<TutorialQuestSystem>();quest.Restore(0,0,0);
                    FarmGameEvents.RaiseTreeHarvested();FarmGameEvents.RaiseRockHarvested();FarmGameEvents.RaiseGrassDug();FarmGameEvents.RaiseSoilHoed();FarmGameEvents.RaiseSeedPlanted();FarmGameEvents.RaiseCropWatered();FarmGameEvents.RaiseCropHarvested();quest.Evaluate();
                    Require(quest.QuestIndex==10,"First day objectives did not follow completed actions");
                    clock.Restore(1,22);FarmGameEvents.RaiseEnemyDefeated();clock.Restore(2,6);quest.Evaluate();Require(quest.Complete,"First night did not complete");
                    // 25 minutes of deterministic clock/hunger simulation, eating every 250 seconds.
                    clock.Restore(1,8);stats.Restore(5,5,1);inventory.AddFood(6);
                    for(int i=0;i<6;i++){clock.Advance(250);stats.AdvanceNeeds(250);Object.FindFirstObjectByType<InventoryPanelSystem>().EatFood();}
                    Require(stats.CurrentHealth==5&&stats.HungerPercent>.95f,"25-minute food balance failed");Require(clock.Day==3&&Mathf.Abs(clock.Hour)<.01f,"25-minute clock simulation failed");
                    var zones=Object.FindObjectsByType<LandUnlockZone>(FindObjectsInactive.Include,FindObjectsSortMode.None);inventory.AddCoins(10000);
                    foreach(var zone in zones){zone.Restore(false);Set(zone,"prerequisiteZone",null);zone.Interact(FarmTool.Sword,inventory);Require(zone.IsUnlocked,"Region unlock failed");}
                    Require(inventory.OwnsEquipment("Ring")&&inventory.OwnsEquipment("Amulet"),"Exploration rewards missing");
                    int wood=inventory.Wood;foreach(var zone in zones)zone.Interact(FarmTool.Sword,inventory);Require(inventory.Wood==wood,"Repeated discovery reward");
                    Set(save,"player",inventory.transform);
                    try
                    {
                        var state=typeof(GameSaveSystem).GetMethod("BuildSaveData",Flags).Invoke(save,null);string json=JsonUtility.ToJson(state);
                        File.WriteAllText(Output+"save-roundtrip.json",json);craft.Restore(0,0);quest.Restore(0,0,0);inventory.RestoreEquipment(null,null);
                        typeof(GameSaveSystem).GetMethod("RestoreSaveData",Flags).Invoke(save,new[]{JsonUtility.FromJson(json,state.GetType())});
                        Require(craft.CampfireBuilt&&craft.BedBuilt&&craft.MealsCooked==1&&craft.WeaponLevel==2,"Craft state lost on load");Require(quest.Complete&&inventory.OwnsEquipment("Ring"),"Quest/equipment lost on load");
                    }finally{Set(save,"player",null);}
                    // Isolated combat arena away from scene colliders.
                    inventory.transform.position=new Vector3(100,100);stats.Restore(5,5,1);inventory.RestoreEquipment(null,null);
                    enemy=new GameObject("Combat verification",typeof(CircleCollider2D)).AddComponent<BasicEnemyAI>();enemy.Configure(inventory.transform,null);enemy.ConfigureStats("Limo",3,1,1,.8f,1,2);enemy.ActivateFromPool(new Vector3(100.5f,100));Physics2D.SyncTransforms();enemy.enabled=false;typeof(EnemyAIBase).GetMethod("TickEnemy",Flags).Invoke(enemy,null);
                }
                else if(stage==1)
                {
                    Require(enemy.IsPreparingAttack,"Enemy did not telegraph");Require(stats.CurrentHealth==5,"Enemy hit before windup");inventory.transform.position=new Vector3(104,100);
                }
                else if(stage==2)
                {
                    typeof(EnemyAIBase).GetMethod("TickEnemy",Flags).Invoke(enemy,null);Require(stats.CurrentHealth==5,"Dodging did not avoid attack");enemy.enabled=true;enemy.ReturnToPool();enemy.ConfigureStats("Golem",8,2,1,.8f,1,12);enemy.ActivateFromPool(new Vector3(100,100));enemy.TakeDamage(100,inventory);Require(inventory.OwnsEquipment("Gem"),"Golem reward failed");
                    foreach(var id in new[]{"Helmet","Chestplate","Boots","Ring","Amulet"})inventory.AddEquipment(id);inventory.Equip("Helmet",0);inventory.Equip("Chestplate",1);inventory.Equip("Boots",2);inventory.Equip("Gem",5);
                    inventory.transform.position=new Vector3(-.47f,.83f,0);if(Camera.main!=null)Camera.main.transform.position=new Vector3(-.47f,.83f,-10);
                    Object.FindFirstObjectByType<DayNightCycle>().Restore(1,10);Object.FindFirstObjectByType<CraftingWindow>().Open();
                }
                else if(stage==3)ScreenCapture.CaptureScreenshot(Output+"taller.png");
                else if(stage==4){Object.FindFirstObjectByType<CraftingWindow>().Close();Object.FindFirstObjectByType<PlayerEquipmentWindow>().Open();}
                else if(stage==5)ScreenCapture.CaptureScreenshot(Output+"equipo.png");
                else
                {
                    File.WriteAllText(Output+"result.txt","PASS: recipe costs, requirements and duplicate prevention; armor and boots; first-day objectives and first-night completion; accelerated 25-minute clock/hunger/food simulation; eight unlock rewards once; JSON save/load of crafting/quests/equipment; attack windup and dodge; golem loot; workshop/equipment screenshots. Player save excluded. This is deterministic accelerated testing, not a human 25-minute playthrough.");SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;
                }
                stage++;next=EditorApplication.timeSinceStartup+(stage==1 ? .2 : .8);
            }
            catch(Exception e){File.WriteAllText(Output+"result.txt","FAIL: "+e);SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;}
        }
    }
}
