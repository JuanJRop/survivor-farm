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
    [InitializeOnLoad] public static class HouseVerification
    {
        static HouseVerification()
        {
            EditorApplication.update+=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Library/VerifyHouse.request"))return;File.Delete("Library/VerifyHouse.request");SessionState.SetBool("HouseQA",true);EditorApplication.isPlaying=true;};
            EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("HouseQA",false)){SessionState.SetBool("HouseQA",false);new GameObject("House QA").AddComponent<HouseRunner>();}};
        }
    }
    public sealed class HouseRunner : MonoBehaviour
    {
        const string Dir="Design/Validation/House/";const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        void Awake(){Application.runInBackground=true;}
        void Check(bool ok,string message){if(!ok)throw new Exception(message);File.AppendAllText(Dir+"test.txt","PASS: "+message+"\n");}
        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir);File.WriteAllText(Dir+"test.txt","House play-mode checks\n");var run=Run();
            while(true){bool more;try{more=run.MoveNext();}catch(Exception e){File.AppendAllText(Dir+"test.txt","FAIL: "+e);EditorApplication.isPlaying=false;yield break;}if(!more)break;yield return run.Current;}
            File.AppendAllText(Dir+"test.txt","ALL PASSED\n");EditorApplication.isPlaying=false;
        }
        IEnumerator Run()
        {
            yield return null;
            var save=Object.FindFirstObjectByType<GameSaveSystem>();var field=typeof(GameSaveSystem).GetField("player",Flags);field.SetValue(save,null);save.enabled=false;
            var inv=Object.FindFirstObjectByType<PlayerInventory>();var house=inv.GetComponent<HouseSystem>();var build=inv.GetComponent<ConstructionSystem>();var campaign=inv.GetComponent<ValleyCampaign>();var craft=inv.GetComponent<PlayerCraftingController>();
            campaign.Data.camp=false;campaign.Data.boss=false;inv.GetComponent<PlayerMovementController>().enabled=false;inv.GetComponent<PlayerSurvivalStats>().Restore(5,5,1);inv.AddWood(1000);inv.AddStone(1000);inv.AddCoins(2000);inv.GetComponent<AdventureProgress>().AddIron(100);
            if(house.IsInside)house.Exit(false);house.LoadLayout(null);build.Restore(null);inv.RestorePacked(null);craft.Restore(0,0,false,false,0,1);
            Object.FindFirstObjectByType<BaseHouse>().Interact(FarmTool.Sword,inv);yield return null;
            Check(house.IsInside&&house.RoomRoot.gameObject.activeInHierarchy,"House entrance activates interior");
            Check(!house.Buy("Bed"),"Furniture progression starts with a campfire");
            Check(house.Buy("Campfire")&&build.PlacePacked("Campfire",new Vector2(-2,99)),"Buy and place first campfire");
            int coins=inv.Coins;Check(house.Buy("Bed")&&inv.Coins==coins-25&&inv.PackedCount("Bed")==1,"Bed purchase charges once and creates a carried kit");
            Check(!build.PlacePacked("Bed",new Vector2(0,97.5f))&&inv.PackedCount("Bed")==1,"Door stays free without consuming furniture");
            Check(build.PlacePacked("Bed",new Vector2(2,100)),"Bed placed indoors");
            var bed=build.Buildings.First(b=>b.kind=="Bed");campaign.Teleport(new Vector3(2,99));var clock=Object.FindFirstObjectByType<DayNightCycle>();clock.Restore(3,22);clock.enabled=false;house.Sleep(bed);Check(clock.Day==4,"Placed bed advances to morning");
            Check(house.Buy("Chest")&&build.PlacePacked("Chest",new Vector2(-2,101)),"Buy and place storage");var chest=build.Buildings.First(b=>b.kind=="Chest");campaign.Teleport(new Vector3(-2,100));Check(build.Transfer(chest,"Wood",true),"Deposit resources");int stored=chest.wood;
            Check(!build.Store(chest),"Full chest cannot be packed");Check(build.BeginMove(chest),"Start moving full chest");build.Cancel();Check(chest.x==-2&&chest.y==101&&chest.wood==stored,"Cancel preserves chest position and contents");
            Check(build.BeginMove(chest)&&build.Place("Chest",new Vector2(-1,101)),"Move chest to chosen position");build.Cancel();Check(chest.wood==stored,"Move preserves stored resources");
            Check(house.Upgrade()&&house.Data.level==1&&house.Size.x==10,"Expand refuge to wooden house");Check(!house.Upgrade(),"Stone house requires exploration");campaign.Data.camp=true;Check(house.Upgrade()&&house.Data.level==2,"Unlock stone house");
            campaign.Teleport(new Vector3(2,99));Check(house.Buy("Furnace")&&build.PlacePacked("Furnace",new Vector2(3,101)),"Buy and place furnace");var furnace=build.Buildings.First(b=>b.kind=="Furnace");campaign.Teleport(new Vector3(3,100));int wood=inv.Wood,stone=inv.Stone,iron=inv.GetComponent<AdventureProgress>().Data.iron;Check(house.Smelt(furnace)&&inv.Wood==wood-2&&inv.Stone==stone-5&&inv.GetComponent<AdventureProgress>().Data.iron==iron+1,"Furnace converts exact resources into iron");
            Check(!house.Upgrade(),"Mansion requires defeating boss");campaign.Data.boss=true;Check(house.Upgrade()&&house.Size.x==18,"Mansion expands available space");house.ChangeStyle();
            field.SetValue(save,inv.transform);try{var data=typeof(GameSaveSystem).GetMethod("BuildSaveData",Flags).Invoke(save,null);data=JsonUtility.FromJson(JsonUtility.ToJson(data),data.GetType());typeof(GameSaveSystem).GetMethod("RestoreSaveData",Flags).Invoke(save,new[]{data});}finally{field.SetValue(save,null);}
            Check(house.IsInside&&house.Data.level==3&&house.Data.style==1&&build.Buildings.Any(b=>b.kind=="Chest"&&b.x==-1&&b.wood==stored&&b.indoors),"Save roundtrip retains interior, upgrades, finish, furniture and storage");
            campaign.Teleport(new Vector3(0,99));yield return new WaitForSeconds(.4f);ScreenCapture.CaptureScreenshot(Dir+"interior.png");yield return new WaitForSeconds(.3f);Object.FindFirstObjectByType<AdventureWindow>().Open("Home");yield return new WaitForSeconds(.2f);ScreenCapture.CaptureScreenshot(Dir+"catalogo.png");yield return new WaitForSeconds(.3f);
            house.Exit(false);Check(!house.IsInside&&!house.RoomRoot.gameObject.activeInHierarchy,"Exit restores outdoor play");Check(!build.CanPlace("Bed",Vector2.zero,out _),"Beds cannot be placed outdoors");
            house.Enter(false);inv.GetComponent<PlayerRespawnController>().ReturnToCheckpoint();Check(!house.IsInside,"Checkpoint respawn exits house before saving");
        }
    }
}
