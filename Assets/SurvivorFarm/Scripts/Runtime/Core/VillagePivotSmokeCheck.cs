using System;
using System.Collections;
using System.IO;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEngine;

namespace SurvivorFarm.Runtime.Core
{
    /// <summary>Opt-in native integration checks; uses isolated QA saves and arranged gameplay captures.</summary>
    public sealed class VillagePivotSmokeCheck : MonoBehaviour
    {
        private string output;
        private bool failed,finished;
        private float started;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(GameSaveSystem.IsQa&&Array.IndexOf(Environment.GetCommandLineArgs(),"--village-pivot-smoke")>=0&&FindFirstObjectByType<VillagePivotSmokeCheck>()==null)
                new GameObject("Village pivot executable QA").AddComponent<VillagePivotSmokeCheck>();
        }
        private void Awake()
        {
            Application.runInBackground=true;started=Time.realtimeSinceStartup;
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../QA/VillagePivot"));Directory.CreateDirectory(output);
            Application.logMessageReceived+=Log;
        }
        private void Log(string message,string stack,LogType type)
        {
            if(type!=LogType.Error&&type!=LogType.Exception)return;
            failed=true;File.AppendAllText(Path.Combine(output,"errors.txt"),message+"\n"+stack+"\n");
        }
        private void Update(){if(!finished&&Time.realtimeSinceStartup-started>180)Finish(false,"Timed out.");}
        private IEnumerator Start()
        {
            var run=Run();
            while(true)
            {
                bool next;try{next=run.MoveNext();}catch(Exception e){Finish(false,e.ToString());yield break;}
                if(!next)break;yield return run.Current;
            }
            Finish(!failed,"Native Windows checks passed: compact interaction and canopy fading, original livestock and reusable crafted saddle, mastery window, persistent village upgrades and trained guard, split dungeon wave and live miniboss, physical bow/arrows/chest loot, security and repair, animated shrines, destructible supplies, running dungeon clock, occupied town liberated without resurrecting civilians. Screenshots are arranged gameplay captures with QA supplies.");
        }
        private IEnumerator Run()
        {
            while(PortfolioSession.Instance==null||!PortfolioSession.Instance.IsReady)yield return null;
            var session=PortfolioSession.Instance;session.BeginNewGame();yield return null;
            var player=session.Player;var campaign=player.GetComponent<ValleyCampaign>();var security=session.Security;var adventure=session.Adventure;
            Require(security.Percent==100&&adventure.Camps.Count==3&&adventure.Dungeons.Count==2,"Village configuration incomplete.");
            Require(!player.OwnsEquipment("Bow"),"A new game already owns the dungeon bow.");
            Require(player.transform.Find("Espada equipada")==null,"The removed attached sword is still on the player.");
            Require(!FarmDefense.All.Any(d=>FortressPieces.IsWall(d.Kind)||d.Kind=="Trap"||d.Kind=="Turret"),"Retired defenses remain in the scene.");
            Frame(Vector2.zero,6.8f);yield return new WaitForSecondsRealtime(.2f);Capture("01-village-security");
            var bridge=FindFirstObjectByType<RepairableBridge>(FindObjectsInactive.Include);
            campaign.Teleport(bridge.transform.position+Vector3.down*2.2f);Frame(bridge.transform.position,4.5f);
            yield return new WaitForSecondsRealtime(.2f);Capture("01b-river-and-bridge");

            var shrub=FindObjectsByType<ForagePlant>(FindObjectsSortMode.None)
                .Where(p=>p.Fruit&&p.IsAvailable&&FarmExploration.Contains(p.transform.position,2))
                .OrderBy(p=>p.transform.position.sqrMagnitude).First();
            campaign.Teleport(shrub.transform.position+Vector3.down*.8f);Frame(shrub.transform.position,3.8f);
            yield return new WaitForSecondsRealtime(.3f);
            var prompt=FindObjectsByType<InteractionPromptAnimation>(FindObjectsSortMode.None).FirstOrDefault();
            Require(prompt!=null&&prompt.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text=="E")&&
                !prompt.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text.Length>1),"Nearby shrub does not use a compact E indicator.");
            Capture("01c-compact-shrub-prompt");
            var tree=FindObjectsByType<TreeResource>(FindObjectsSortMode.None)
                .Where(t=>t.IsAvailable&&t.GetComponent<TreeOcclusionFader>()!=null&&FarmExploration.Contains(t.transform.position,3)&&
                    !FarmExploration.IsRiver(t.transform.position+Vector3.up*1.3f,.5f))
                .OrderBy(t=>t.transform.position.sqrMagnitude).First();
            campaign.Teleport(tree.transform.position+Vector3.up*1.3f);Frame(tree.transform.position+Vector3.up*.8f,3.8f);
            yield return new WaitForSecondsRealtime(.3f);
            Require(tree.GetComponentsInChildren<SpriteRenderer>().Any(s=>s.enabled&&s.sprite!=null&&s.color.a<.65f),"Tree canopy does not fade over the player.");
            Capture("01d-transparent-tree-canopy");

            var cow=FindObjectsByType<AnimalResource>(FindObjectsSortMode.None).FirstOrDefault(a=>a.IsCow&&a.IsAlive);
            Require(cow!=null&&HorseMount.All.Count>=1,"Authored cows or horses are missing.");
            campaign.Teleport(cow.transform.position+Vector3.down*1.2f);Frame(cow.transform.position+Vector3.up*.5f,3.8f);
            cow.TakeDamage(1,player);yield return new WaitForSecondsRealtime(.1f);
            Require(cow.IsReactingToHit&&cow.GetComponentInChildren<SpriteRenderer>().color.g<.95f,"Cow damage feedback is not visible.");
            Capture("01e-animal-hit-reaction");
            cow.TakeDamage(100,player);
            Require(cow.transform.parent.GetComponentsInChildren<EnemyLootPickup>().Any(d=>d.Item.Kind==ItemKind.Leather),"Cow death did not scatter leather.");
            yield return new WaitForSecondsRealtime(.1f);Capture("01f-cow-leather-and-death");

            // Only the isolated QA inventory receives supplies; the actual crafting transaction is exercised.
            player.AddItem("Leather",8);player.AddWood(4);
            int leather=player.GetItemCount("Leather"),saddleWood=player.Wood;
            Require(player.GetComponent<PlayerCraftingController>().Craft("Saddle")&&player.GetItemCount("Saddle")==1&&
                player.GetItemCount("Leather")==leather-8&&player.Wood==saddleWood-4,"Saddle did not consume exactly 8 leather and 4 wood.");
            var horse=HorseMount.All.OrderBy(h=>h.Id).First();horse.GetComponent<AnimalRoamingVisual>().enabled=false;
            var riding=player.GetComponent<PlayerMountController>();
            for(int direction=0;direction<8&&!riding.IsMounted;direction++)
            {
                float angle=direction*Mathf.PI*.25f;
                campaign.Teleport(horse.transform.position+new Vector3(Mathf.Cos(angle),Mathf.Sin(angle))*.95f+Vector3.up*.4f);
                riding.TryMount(horse);
            }
            Require(riding.IsMounted&&riding.SpeedMultiplier>1&&!horse.GetComponent<Collider2D>().enabled,"Crafted saddle cannot mount an authored horse.");
            Frame(horse.transform.position+Vector3.up*.7f,3.8f);yield return new WaitForSecondsRealtime(.25f);
            Require(riding.IsMounted&&horse.Visual.sprite.texture.name.StartsWith("PackRider"),"Mounted horse is missing its original rider art.");
            Capture("01g-mounted-original-horse");
            Require(riding.TryDismount()&&player.GetItemCount("Saddle")==1&&horse.GetComponent<Collider2D>().enabled,"Dismount is blocked or consumes the reusable saddle.");

            var mastery=player.GetComponent<MasteryWindow>();Require(mastery!=null,"K mastery window is missing.");
            mastery.Open();yield return new WaitForSecondsRealtime(.15f);Require(MasteryWindow.IsOpen,"K mastery window cannot open.");
            Capture("01h-mastery-tree");mastery.Close();Require(!MasteryWindow.IsOpen,"K mastery window cannot close.");

            var home=VillageHouseHealth.All.First();home.TakeDamage(16,null);
            Require(security.Percent<100,"House damage did not reduce security.");
            campaign.Teleport(home.Transform.position);Frame(home.transform.position,5.2f);
            yield return new WaitForSecondsRealtime(.2f);Capture("02-damaged-house");
            Require(home.Repair(player),"House repair failed.");

            var save=FindFirstObjectByType<GameSaveSystem>();
            var progression=VillageProgression.Instance;Require(progression!=null,"Village progression is missing.");
            var previousProgression=progression.Capture();
            var upgradedHouse=progression.Houses.First(h=>h.Health==h.Maximum);
            int previousCivilians=security.LivingResidents;
            player.AddWood(200);player.AddStone(200);player.AddCoins(200);player.AddFood(12);
            campaign.Teleport(progression.WellPosition+Vector3.down*1.3f);
            Require(progression.UpgradeHouse(upgradedHouse)&&progression.UpgradeWell()&&progression.Recruit(),"House, well or guard investment failed: "+progression.LastMessage);
            var guard=progression.Guards.Single();
            Require(progression.TrainStrength(guard)&&progression.TrainToughness(guard),"Recruited guard cannot train.");
            Require(security.LivingResidents==previousCivilians,"A hired guard incorrectly counts as a civilian.");
            Frame(upgradedHouse.transform.position,4.5f);yield return new WaitForSecondsRealtime(.2f);Capture("02b-upgraded-house");
            Frame(progression.WellPosition,5.2f);yield return new WaitForSecondsRealtime(.2f);Capture("02c-upgraded-well-and-guard");
            progression.OpenVillage();yield return new WaitForSecondsRealtime(.15f);
            Require(VillageUpgradeWindow.IsOpen,"Village improvements window cannot open near the well.");
            Capture("02d-village-improvements-menu");player.GetComponent<VillageUpgradeWindow>().Close();
            save.SaveGame(false);Require(save.TryLoadGame(),"Village improvement save failed: "+save.LastSaveError);
            Require(upgradedHouse.Level==2&&progression.WellLevel==2&&progression.Guards.Count==1&&
                progression.Guards[0].Strength==2&&progression.Guards[0].Toughness==2&&player.GetItemCount("Saddle")==1,
                "House, well, trained guard or saddle did not survive loading.");
            progression.Restore(previousProgression);yield return null;
            Require(progression.Guards.Count==previousProgression.guards.Count&&security.LivingResidents==previousCivilians,"QA progression was not restored before the occupation scenario.");
            save.SaveGame(false);

            var camp=adventure.Camps[0];campaign.Teleport(camp.transform.position+new Vector3(0,-4));Frame(camp.transform.position,5.8f);
            yield return new WaitForSecondsRealtime(.3f);Capture("03-monster-camp");
            Require(camp.Members.Any(e=>e.IsAlive),"Overworld camp has no live enemies.");
            foreach(var enemy in camp.Members.ToArray())if(enemy.IsAlive)enemy.TakeDamage(1000,player);
            Require(camp.IsCleared&&!camp.Claimed,"Camp reward was paid before the chest was opened.");
            campaign.Teleport(camp.Chest.transform.position+Vector3.down);int coins=player.Coins;
            Require(camp.TryClaim(player)&&!camp.TryClaim(player),"Chest reward is missing or repeatable.");
            yield return new WaitForSecondsRealtime(.2f);Capture("03b-scattered-chest-loot");
            yield return new WaitForSeconds(.65f);
            foreach(var drop in camp.GetComponentsInChildren<EnemyLootPickup>())drop.TryCollect(player);
            Require(player.Coins>coins,"Physical chest coins cannot be collected.");
            save.SaveGame(false);Require(save.TryLoadGame(),"Adventure save failed: "+save.LastSaveError);
            Require(camp.Claimed&&!camp.TryClaim(player),"Camp reward repeated after loading.");

            campaign.Teleport(VillageAdventure.PortalPositions[0]+Vector3.down);Frame(VillageAdventure.PortalPositions[0],5.5f);
            yield return new WaitForSecondsRealtime(.2f);Capture("04-dungeon-entrance");
            Require(adventure.EnterDungeon(0),"Portal entry failed.");Frame(VillageAdventure.DungeonDefinitions[0].PreferredCenter,6.2f);
            float clock=session.Remaining;session.Advance(10);Require(session.Remaining<clock&&!session.CanSave,"Dungeon stopped the village clock.");
            Require(adventure.Dungeons[0].GetComponentsInChildren<DungeonShrinePresentation>().Length==2,"Shrines lack candle animation and collision.");
            var supply=adventure.Dungeons[0].GetComponentsInChildren<DungeonDestructible>().First();
            supply.TakeDamage(100,player);Require(!supply.IsAlive,"Dungeon supplies cannot be broken.");
            yield return new WaitForSecondsRealtime(.3f);Capture("05-dungeon-guardians");
            var dungeon=adventure.Dungeons[0];Require(dungeon.HasDungeonChallenge&&dungeon.MiniBoss!=null,"Dungeon lacks a split wave and miniboss.");
            // Defeating the parent activates children later in this roster; a second pass also catches future ordering changes.
            for(int pass=0;pass<2;pass++)
                foreach(var enemy in dungeon.Members.ToArray())if(enemy!=dungeon.MiniBoss&&enemy.IsAlive)enemy.TakeDamage(1000,player);
            Require(dungeon.MiniBoss.IsAlive&&!dungeon.IsCleared,"Miniboss did not awaken after the split wave.");
            player.GetComponent<PlayerSurvivalStats>().GrantInvulnerability(3);
            campaign.Teleport(dungeon.MiniBoss.transform.position+Vector3.down*3);Frame(dungeon.MiniBoss.transform.position+Vector3.down*.8f,4.5f);
            yield return new WaitForSecondsRealtime(.25f);Capture("05a-dungeon-miniboss");
            dungeon.MiniBoss.TakeDamage(1000,player);Require(dungeon.IsCleared,"Defeating the miniboss did not unlock the chest.");
            campaign.Teleport(dungeon.Chest.transform.position+Vector3.down);Require(dungeon.TryClaim(player),"Dungeon chest could not be opened.");
            var rewards=dungeon.GetComponentsInChildren<EnemyLootPickup>();
            Require(rewards.Any(d=>d.Item.Kind==ItemKind.Bow)&&rewards.Any(d=>d.Item.Kind==ItemKind.Arrow),"Dungeon chest does not physically scatter bow and arrows.");
            int arrows=player.GetItemCount("Arrow");
            yield return new WaitForSecondsRealtime(.2f);Capture("05b-dungeon-chest-loot");
            yield return new WaitForSeconds(.65f);
            foreach(var drop in dungeon.GetComponentsInChildren<EnemyLootPickup>())drop.TryCollect(player);
            Require(player.OwnsEquipment("Bow")&&player.GetItemCount("Arrow")>=arrows+16,"Landed bow or arrows cannot be collected.");
            adventure.ExitDungeon();Require(!adventure.IsInsideDungeon&&FarmExploration.Contains(player.transform.position),"Dungeon return failed.");
            save.SaveGame(false);Require(save.TryLoadGame()&&adventure.CompletedCount==2&&player.OwnsEquipment("Bow")&&
                player.GetItemCount("Arrow")>=arrows+16,"Dungeon bow, ammunition or completion did not persist.");

            campaign.Teleport(new Vector3(0,-5));
            foreach(var house in VillageHouseHealth.All.ToArray())house.TakeDamage(1000,null);
            foreach(var person in VillageResidentHealth.All.Where(p=>p.GetComponent<VillageGuard>()==null).ToArray())person.TakeDamage(1000,null);
            Require(security.IsOccupied&&security.GarrisonRemaining>0,"Monsters did not occupy the ruined town.");
            Frame(Vector2.zero,6.8f);yield return new WaitForSecondsRealtime(.3f);Capture("06-occupied-village");
            foreach(var enemy in session.Raids.Enemies.ToArray())if(enemy.IsAlive)enemy.TakeDamage(1000,player);
            Require(!security.CanLiberate,"Ruins can be liberated without a home.");
            player.AddWood(2);campaign.Teleport(home.Transform.position);Require(home.Repair(player),"Destroyed home cannot be rebuilt.");
            campaign.Teleport(security.RecoveryPoint);Require(security.Liberate(player),"Village cannot be liberated.");
            Require(!security.IsOccupied&&security.Percent>0&&security.LivingResidents==0&&session.Phase==SlicePhase.Day,"Liberation state is inconsistent.");
            yield return new WaitForSecondsRealtime(.3f);Capture("07-reclaimed-village");
        }
        private static void Frame(Vector2 center,float size)
        {
            var camera=Camera.main;Require(camera!=null,"Missing camera.");
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
        {if(finished)return;finished=true;File.WriteAllText(Path.Combine(output,"result.txt"),(pass?"PASS\n":"FAIL\n")+message);Application.Quit(pass?0:1);}
        private void OnDestroy()=>Application.logMessageReceived-=Log;
    }
}
