using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class WorldRenewalTests
    {
        private PortfolioSession session;
        [UnitySetUp] public IEnumerator Setup()
        {
            SceneManager.LoadScene("Main");for(int i=0;i<120;i++){yield return null;session=PortfolioSession.Instance;if(session!=null&&session.IsReady)break;}
            Assert.That(session!=null&&session.IsReady,Is.True);
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            Time.timeScale=1;AudioListener.volume=1;CombatTimeFeedback.ReducedMotion=false;
            var main=SceneManager.GetSceneByName("Main");SceneManager.SetActiveScene(SceneManager.CreateScene("AfterRenewal"));
            if(main.IsValid())yield return SceneManager.UnloadSceneAsync(main);
        }
        [UnityTest] public IEnumerator MenuHasSeparateHomeOptionsAndSaveBrowser()
        {
            var menu=session.GetComponent<DemoFrontEnd>();Assert.That(menu.IsVisible,Is.True);Assert.That(session.HasBegun,Is.False);
            menu.Options();Assert.That(menu.ScreenName,Is.EqualTo("Opciones"));DemoFrontEnd.SetVolume(.5f);Assert.That(AudioListener.volume,Is.EqualTo(.5f));
            menu.LoadScreen();Assert.That(menu.ScreenName,Is.EqualTo("Cargar"));menu.Home();
            session.BeginNewGame();yield return null;Assert.That(menu.IsVisible,Is.False);
            var save=Object.FindFirstObjectByType<GameSaveSystem>();save.SaveGame(false);
            Assert.That(PortfolioSaveCatalog.List(Path.GetDirectoryName(save.SaveFile)).Any(s=>s.Slot==session.QaSlot),Is.True);
            Assert.That(PortfolioSaveCatalog.Load(session,"../elsewhere"),Is.False);
        }
        [UnityTest] public IEnumerator TreesHaveSingleGroundedVisualSolidTrunksAndTiersIncludingVillageProps()
        {
            session.BeginNewGame();yield return null;
            var trees=Object.FindObjectsByType<TreeResource>(FindObjectsSortMode.None);
            foreach(var tree in trees.Where(t=>FarmExploration.Contains(t.transform.position,1)))
            {
                Assert.That(FarmExploration.IsRiver(tree.transform.position,.4f),Is.False,tree.name);
                Assert.That(tree.GetComponent<ResourceTier>(),Is.Not.Null);
                Assert.That(tree.GetComponent<CircleCollider2D>().isTrigger,Is.False);
                Assert.That(tree.GetComponentsInChildren<SpriteRenderer>().Count(s=>s.enabled&&s.sprite!=null),Is.EqualTo(1),tree.name);
                var art=tree.transform.Find("Árbol del pack").GetComponent<SpriteRenderer>();
                Assert.That(art.bounds.min.y,Is.InRange(tree.transform.position.y-.15f,tree.transform.position.y+.15f));
                Assert.That(tree.GetComponent<TreeOcclusionFader>().enabled,Is.True);
            }
            Assert.That(trees.Any(t=>t.transform.position.x==-8),Is.True,"Authored village tree converted to a resource.");
            var target=trees.First(t=>t.GetComponent<ResourceTier>().Tier==1);var player=session.Player;
            player.GetComponent<ValleyCampaign>().Teleport(target.transform.position+Vector3.down);
            target.Interact(FarmTool.Axe,player);yield return new WaitForSeconds(3);
            Assert.That(target.IsHarvested,Is.True);
        }
        [UnityTest] public IEnumerator NoFreeCatAndPurchaseRequiresDayAndAllMaterials()
        {
            session.BeginNewGame();var player=session.Player;var adoption=player.GetComponent<PetAdoption>();var pet=player.GetComponent<PlayerPetController>();
            Assert.That(adoption.Owned,Is.False);Assert.That(pet.Equipped,Is.False);Assert.That(pet.DamageBonus,Is.Zero);
            player.AddCoins(500);player.GetComponent<AdventureProgress>().AddIron(30);player.AddItem("GoldOre",10);
            Assert.That(adoption.Buy(),Is.False);var state=session.Capture();state.day=2;session.Restore(state);
            int coins=player.Coins;Assert.That(adoption.Buy(),Is.True);Assert.That(player.Coins,Is.EqualTo(coins-PetAdoption.CoinCost));
            Assert.That(adoption.Buy(),Is.False);Assert.That(pet.Equipped,Is.True);
            var saved=session.Capture();adoption.Restore(false);session.Restore(saved);Assert.That(adoption.Owned,Is.True);yield return null;
        }
        [UnityTest] public IEnumerator FloraPaysOnceAndPersistsWithTheHouseDamage()
        {
            session.BeginNewGame();var player=session.Player;
            var flower=Object.FindObjectsByType<ForagePlant>(FindObjectsSortMode.None).First(p=>!p.Fruit);
            player.GetComponent<ValleyCampaign>().Teleport(flower.transform.position+Vector3.down*.7f);int seeds=player.CommonSeeds;
            flower.Interact(FarmTool.Sword,player);flower.Interact(FarmTool.Sword,player);Assert.That(player.CommonSeeds,Is.EqualTo(seeds+1));
            var house=VillageHouseHealth.All.First();house.TakeDamage(7,null);Assert.That(house.Health,Is.EqualTo(25));
            Assert.That(DamageRules.CanPlayerHit(house),Is.False);
            var save=Object.FindFirstObjectByType<GameSaveSystem>();save.SaveGame(false);flower.Restore(false);house.TakeDamage(100,null);
            Assert.That(save.TryLoadGame(),Is.True);Assert.That(flower.Collected,Is.True);Assert.That(house.Health,Is.EqualTo(25));yield return null;
            var village=Object.FindFirstObjectByType<ValleyWorld>().transform.Find("Pueblo inicial - Raizclara");
            foreach(Transform prop in village)
                if(prop.name=="Fence"||prop.name=="Chest"||prop.name=="Sign"||prop.name=="Workbench")
                    Assert.That(prop.gameObject.activeSelf,Is.False,"Rejected decoration must stay hidden after loading: "+prop.name);
        }
        [UnityTest] public IEnumerator MoveAttachesPreviewAndCancelRestoresThenDemolitionRefundsOnlyPaidCosts()
        {
            session.BeginNewGame();var player=session.Player;player.AddWood(100);player.AddStone(100);
            player.GetComponent<ValleyCampaign>().Teleport(new Vector3(0,-10));Physics2D.SyncTransforms();
            var build=player.GetComponent<ConstructionSystem>();var site=FurnitureTestSites.Find(build);build.Begin("Chest");Assert.That(build.PlaceSelected(site),Is.True);
            var piece=build.Buildings.Last();Assert.That(build.BeginMove(piece),Is.True);Assert.That(build.MovingPiece,Is.SameAs(piece));
            Assert.That(build.Preview,Is.Not.Null);build.Cancel();Assert.That(build.Buildings.Contains(piece),Is.True);
            var refund=new DemolitionRefund(piece);Assert.That(refund.Wood,Is.EqualTo(5));Assert.That(refund.Stone,Is.EqualTo(1));
            int wood=player.Wood,stone=player.Stone;Assert.That(build.Demolish(piece),Is.True);
            Assert.That(player.Wood,Is.EqualTo(wood+5));Assert.That(player.Stone,Is.EqualTo(stone+1));Assert.That(build.Demolish(piece),Is.False);
            var authored=build.Buildings.First(b=>b.authoredFree);Assert.That(new DemolitionRefund(authored).Wood,Is.Zero);yield return null;
        }
        [UnityTest] public IEnumerator GrassBanksAndNativeCookingAnimationSurviveSaveLoad()
        {
            session.BeginNewGame();yield return null;Assert.That(session.Player.GetComponent<PlayerCombatController>().SwordRange,Is.EqualTo(1.35f));
            var shores=Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(a=>a.name=="Orilla norte de pasto"||a.name=="Orilla sur de pasto").ToArray();
            Assert.That(shores.Length,Is.GreaterThan(100));
            Assert.That(shores.All(s=>s.sprite.texture.name=="TerrainAtlas"&&Mathf.Abs(s.bounds.size.x-.5f)<.001f),Is.True,"Both banks use grass at the terrain pixel scale.");
            var fire=Object.FindObjectsByType<PlacedBuilding>(FindObjectsSortMode.None).First(b=>b.Data.kind=="Campfire");
            Assert.That(fire.GetComponentInChildren<EnvironmentSpriteAnimation>().Frames.Length,Is.EqualTo(4));
            var save=Object.FindFirstObjectByType<GameSaveSystem>();save.SaveGame(false);Assert.That(save.TryLoadGame(),Is.True);
            fire=Object.FindObjectsByType<PlacedBuilding>(FindObjectsSortMode.None).First(b=>b.Data.kind=="Campfire");
            Assert.That(fire.GetComponentInChildren<EnvironmentSpriteAnimation>().Frames.Length,Is.EqualTo(4),"Fire must remain animated after loading a save.");
            Assert.That(AudioFeedback.Clip(CombatSound.ChickenDeath),Is.Not.Null);Assert.That(AudioFeedback.Clip(CombatSound.RunStep),Is.Not.Null);
        }
        [UnityTest] public IEnumerator DestroyedMovingPieceCannotBeResurrected()
        {
            session.BeginNewGame();var player=session.Player;player.AddWood(50);
            player.GetComponent<ValleyCampaign>().Teleport(new Vector3(0,-10));Physics2D.SyncTransforms();
            var build=player.GetComponent<ConstructionSystem>();var site=FurnitureTestSites.Find(build);build.Begin("Chest");Assert.That(build.PlaceSelected(site),Is.True);
            var piece=build.Buildings.Last();Assert.That(build.BeginMove(piece),Is.True);build.RemoveDestroyed(piece);
            Assert.That(build.PlaceSelected(site+Vector2.right*2),Is.False);Assert.That(build.Buildings.Contains(piece),Is.False);Assert.That(build.IsMoving,Is.False);yield return null;
        }
        [UnityTest] public IEnumerator DashCannotCrossSolidTreeTrunk()
        {
            session.BeginNewGame();var player=session.Player;
            var tree=Object.FindObjectsByType<TreeResource>(FindObjectsSortMode.None).First(t=>!t.IsHarvested);
            tree.transform.position=new Vector3(0,-10);player.GetComponent<ValleyCampaign>().Teleport(new Vector3(-1.2f,-9.42f));
            Physics2D.SyncTransforms();Assert.That(player.GetComponent<PlayerMovementController>().TryDash(Vector2.right),Is.True);
            yield return new WaitForSeconds(.3f);Assert.That(player.transform.position.x,Is.LessThan(-.5f),"A real dash must stop at the trunk, not cross it.");
        }
        [UnityTest] public IEnumerator BruteReallyDamagesHouseAndRetargetsAfterDestruction()
        {
            session.BeginNewGame();foreach(var resident in VillageResidentHealth.All.ToArray())resident.gameObject.SetActive(false);
            session.Player.GetComponent<ValleyCampaign>().Teleport(new Vector3(20,-12));
            var home=VillageHouseHealth.All.First();var point=home.ContactPoint(home.transform.position+Vector3.down*4)+Vector2.down*.6f;
            session.Advance(session.Remaining+.1f);session.Advance(session.Settings.duskSeconds+1);
            var brute=session.Raids.Enemies[0];brute.ConfigureRaid(session,RaidRole.Brute,null);brute.ActivateFromPool(point);Physics2D.SyncTransforms();brute.ChooseTarget();
            Assert.That(brute.TargetKind,Is.EqualTo("House"));int hp=home.Health;
            yield return new WaitForSeconds(2);Assert.That(home.Health,Is.LessThan(hp));
            home.TakeDamage(100,null);brute.ChooseTarget();Assert.That(brute.SelectedTarget,Is.Not.EqualTo(home.transform));brute.ReturnToPool();
        }
    }
}
