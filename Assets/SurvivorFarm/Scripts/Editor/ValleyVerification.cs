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
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    public sealed class ValleyArtImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/Resources/StoryArt/"))return;
            var t=(TextureImporter)assetImporter;t.textureType=TextureImporterType.Default;t.npotScale=TextureImporterNPOTScale.None;t.filterMode=FilterMode.Point;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.textureCompression=TextureImporterCompression.Uncompressed;
        }
    }
    [InitializeOnLoad] public static class ValleyVerification
    {
        const string Key="ValleyVerification";
        static ValleyVerification()
        {
            EditorApplication.update+=()=>{
                if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists("Library/VerifyValley.request"))return;
                File.Delete("Library/VerifyValley.request");
                foreach(var p in Directory.GetFiles("Assets/SurvivorFarm/Resources/StoryArt","*.png"))AssetDatabase.ImportAsset(p.Replace('\\','/'),ImportAssetOptions.ForceUpdate);
                EditorSceneManager.OpenScene("Assets/SurvivorFarm/Scenes/Main.unity");
                SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
            };
            EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Key,false)){SessionState.SetBool(Key,false);new GameObject("Valley QA").AddComponent<ValleyTestRunner>();}};
        }
    }
    public sealed class ValleyTestRunner : MonoBehaviour
    {
        const string Dir="Design/Validation/Valley/";const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Check(bool test,string error){if(!test)throw new Exception(error);}
        void Note(string s){File.AppendAllText(Dir+"test.txt",s+"\n");}
        void Awake(){Application.runInBackground=true;}
        IEnumerator Start()
        {
            Directory.CreateDirectory(Dir);File.WriteAllText(Dir+"test.txt","Valley Play Mode verification\n");
            var run=Run();while(true){bool more;try{more=run.MoveNext();}catch(Exception e){Note("FAIL: "+e);EditorApplication.isPlaying=false;if(Application.isBatchMode)EditorApplication.Exit(1);yield break;}if(!more)break;yield return run.Current;}
            Note("PASS: gates, six zones, village quests, rank rewards, actual interactions, guardian, boss phases and retry, reward idempotence, save roundtrip.");EditorApplication.isPlaying=false;if(Application.isBatchMode)EditorApplication.Exit(0);
        }
        IEnumerator Run()
        {
            yield return null;Application.runInBackground=true;
            var save=Object.FindFirstObjectByType<GameSaveSystem>();Check(save!=null,"GameSaveSystem missing from Main.unity");typeof(GameSaveSystem).GetField("player",Flags).SetValue(save,null);save.enabled=false;
            var inv=Object.FindFirstObjectByType<PlayerInventory>();var c=inv.GetComponent<ValleyCampaign>();Check(c!=null,"Campaign did not attach");
            c.Restore(null);inv.Restore(0,0,0,0,0,0,0,99,0);inv.RestorePacked(null);inv.RestoreItemStacks(null);inv.RestoreEquipment(null,null);inv.GetComponent<AdventureProgress>().Restore(null);inv.GetComponent<PlayerCraftingController>().Restore(0,0,false,false,0,1);inv.GetComponent<ConstructionSystem>().Restore(null);
            c.Teleport(c.Home);var stats=inv.GetComponent<PlayerSurvivalStats>();stats.Restore(5,5,1);inv.AddWood(150);inv.AddStone(150);inv.AddCoins(150);inv.AddFruit(10);
            inv.GetComponent<PlayerMovementController>().enabled=false;var clock=Object.FindFirstObjectByType<DayNightCycle>();clock.enabled=false;clock.Restore(1,8);
            foreach(var pool in Object.FindObjectsByType<OutdoorEnemyPool>(FindObjectsSortMode.None))pool.enabled=false;
            Check(!c.Travel(5)&&!c.Travel(1),"Locked route bypass");
            Check(c.World.Art("Portal").rect.width==48&&c.World.Art("Boss").rect.width==16,"Original sprite crop failed");
            c.Use("note",FarmTool.Sword);var craft=inv.GetComponent<PlayerCraftingController>();Check(craft.Craft("Campfire"),"Campfire craft failed W"+inv.Wood+" S"+inv.Stone);c.Teleport(new Vector3(3,-3));var builder=inv.GetComponent<ConstructionSystem>();bool placed=false;for(float x=2;x<9&&!placed;x+=.5f)for(float y=-6;y<-2&&!placed;y+=.5f)if(builder.CanPlace("Campfire",new Vector2(x,y),out _))placed=builder.PlacePacked("Campfire",new Vector2(x,y));Check(placed,"Crafted fire could not be placed");Check(craft.Craft("Food"),"Food craft failed Fruit"+inv.Fruit+" Campfire"+craft.CampfireBuilt);bool bedCrafted=craft.Craft("Bed");Check(bedCrafted||craft.BedBuilt||inv.PackedCount("Bed")>0,"Bed craft failed W"+inv.Wood+" S"+inv.Stone+" HP"+stats.CurrentHealth+" Campfire"+craft.CampfireBuilt+" Bed"+craft.BedBuilt+" Packed"+inv.PackedCount("Bed"));if(!craft.BedBuilt)craft.RegisterPlacedBed();clock.Restore(2,8);FarmGameEvents.RaiseSleptUntilMorning();c.Evaluate();Check(c.Data.camp,"First night objective failed");
            inv.AddFood(10);Check(!c.Data.harvest,"First day must not depend on harvesting");
            Use(c,"village:elder");Use(c,"village:blacksmith");Use(c,"village:farmer");Use(c,"village:merchant");
            Check(c.Data.maraPantryStocked&&c.Data.nicoWorkshopRepaired&&c.Data.daliaGardenRestored&&c.Data.roloMarketOpened&&c.Data.influence>=16&&inv.OwnsEquipment("IronHelmet"),"Village reconstruction missions or rank rewards failed");
            Check(c.Travel(1),"Camp locked");Use(c,"cargo");Use(c,"bench");Check(c.Data.bench,"Bench failed");Use(c,"chicken");Check(c.Data.chicken,"Rescue failed");
            var terrain=c.World.GetComponent<SurvivorFarm.Runtime.World.ValleyTerrain>();
            Check(terrain!=null&&terrain.EdgeQuarters>100&&terrain.PlantCount>50,"Missing grass edges or vegetation");
            Check(c.World.GetComponentsInChildren<Transform>().Where(t=>t.name=="Planta decorativa").All(t=>t.GetComponent<Collider2D>()==null),"Decorative plants block movement");
            c.Teleport(ValleyWorld.Center(1));yield return null;Capture("campamento-plantas.png");
            var art=c.World.GetComponentsInChildren<SpriteRenderer>();Check(art.All(a=>a.sprite!=null),"Missing world sprites");
            c.Travel(2);inv.GetComponent<PlayerToolUpgradeController>().Restore(1,1,1);Use(c,"seal",FarmTool.Pickaxe);Check(!c.Data.sealStone,"Pick gate bypass");inv.GetComponent<PlayerToolUpgradeController>().TryUpgrade(FarmTool.Pickaxe);Use(c,"seal",FarmTool.Pickaxe);Check(c.Data.sealStone,"Seal failed");
            c.Use("secret",FarmTool.Pickaxe,false);int iron=inv.GetComponent<AdventureProgress>().Data.iron;c.Use("secret",FarmTool.Pickaxe,false);Check(inv.GetComponent<AdventureProgress>().Data.iron==iron,"Duplicate gathering");
            inv.GetComponent<AdventureProgress>().AddIron(6);inv.AddFood(2);Use(c,"village:guard");Check(c.Data.guardPostBuilt&&inv.OwnsEquipment("IronShield"),"Guard post mission failed");
            c.Travel(3);var guardian=c.World.GetComponentsInChildren<ValleyEnemy>().First(e=>e.Maximum==12);guardian.TakeDamage(100,inv);Check(c.Data.guardian,"Guardian didn't unlock shrine");
            c.Travel(4);Use(c,"portal");Check(c.Data.portal,"Portal ritual failed");yield return null;Capture("santuario.png");
            Use(c,"portal");Check(c.Data.zone==5,"Portal entry failed");
            var boss=c.World.GetComponentsInChildren<ValleyEnemy>().First(e=>e.Maximum==36);
            c.Teleport(ValleyWorld.Center(5)+new Vector3(-2,-3));yield return new WaitForSeconds(2);Check(boss.IsAlive,"Boss missing");
            float end=Time.time+8;while(!boss.Vulnerable&&Time.time<end){stats.Heal(5);yield return null;}Check(boss.Vulnerable,"Boss never exhausted after leap");
            int hp=boss.Health;boss.TakeDamage(1,inv);Check(boss.Health==hp-2,"Exhaustion damage wrong");
            stats.TakeDamage(100);yield return null;yield return null;
            Check(PlayerRespawnController.MenuOpen,"Death menu missing in arena");
            inv.GetComponent<PlayerRespawnController>().ReturnToCheckpoint();c.SyncLocation();
            Check(c.Data.zone==4&&stats.CurrentHealth>0,"Checkpoint did not return to sanctuary");
            c.Travel(5);Check(boss.Health==boss.Maximum,"Retry didn't reset health");
            c.Teleport(ValleyWorld.Center(5)+new Vector3(-2,-3));yield return null;Capture("rey-limo.png");ScreenCapture.CaptureScreenshot(Dir+"rey-limo-ui.png");yield return new WaitForSeconds(.2f);
            foreach(var bud in c.World.GetComponentsInChildren<ValleyBud>())bud.TakeDamage(10,inv);
            boss.TakeDamage(100,inv);Check(c.Data.boss,"Boss victory failed");Use(c,"seed");Check(c.Data.seed,"Seed missing");c.Travel(4);c.Travel(0);Use(c,"plant",FarmTool.Sword);Check(c.Data.restored,"Final relic restoration failed");int coins=inv.Coins,max=stats.MaxHealth;Use(c,"plant",FarmTool.Sword);Check(inv.Coins==coins&&stats.MaxHealth==max,"Final reward duplicated");
            typeof(GameSaveSystem).GetField("player",Flags).SetValue(save,inv.transform);
            try{var data=typeof(GameSaveSystem).GetMethod("BuildSaveData",Flags).Invoke(save,null);string json=JsonUtility.ToJson(data);c.Restore(null);typeof(GameSaveSystem).GetMethod("RestoreSaveData",Flags).Invoke(save,new[]{JsonUtility.FromJson(json,data.GetType())});Check(c.Data.restored&&c.Data.portal&&c.Data.boss&&c.Data.discoveries.Count>0,"Save roundtrip lost story");}
            finally{typeof(GameSaveSystem).GetField("player",Flags).SetValue(save,null);}
            Note("All progression and encounter assertions passed.");
            Object.FindFirstObjectByType<AdventureWindow>().Open("Journal");yield return new WaitForSeconds(.2f);ScreenCapture.CaptureScreenshot(Dir+"diario.png");yield return new WaitForSeconds(.3f);
        }
        static void Use(ValleyCampaign c,string id,FarmTool tool=FarmTool.Sword)
        {
            var item=c.World.GetComponentsInChildren<ValleyInteraction>().First(i=>i.Id==id);
            c.Teleport(item.transform.position+Vector3.down*.8f);item.Interact(tool,c.Inventory);
            if(!VillageQuests.IsVillager(id))return;
            var dialogue=Object.FindFirstObjectByType<VillageDialogueWindow>();
            Check(dialogue!=null&&VillageDialogueWindow.IsOpen,"NPC conversation did not open: "+id);
            dialogue.AskForQuest();
            if(!VillageQuests.IsAccepted(c.Data,id))Check(dialogue.AcceptQuest(),"Quest acceptance failed: "+id);
            dialogue.AskForQuest();Check(dialogue.DeliverQuest(),"Quest delivery failed: "+id);dialogue.Close();
        }
        static void Capture(string name)
        {
            var camera=Camera.main;var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(Dir+name,tex.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=old;Destroy(rt);Destroy(tex);
        }
    }
}
