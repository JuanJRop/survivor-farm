using System.Collections;
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
    public sealed class LivingValleyTests
    {
        PortfolioSession session;
        [UnitySetUp] public IEnumerator Setup()
        {
            SceneManager.LoadScene("Main");
            for(int i=0;i<100;i++){yield return null;session=PortfolioSession.Instance;if(session!=null&&session.IsReady)break;}
            Assert.That(session.IsReady,Is.True);session.BeginNewGame();
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            Time.timeScale=1;var main=SceneManager.GetSceneByName("Main");var empty=SceneManager.CreateScene("AfterLivingValley");SceneManager.SetActiveScene(empty);
            if(main.IsValid())yield return SceneManager.UnloadSceneAsync(main);
        }
        [UnityTest] public IEnumerator MasteryRequiresUseThenPurchaseAndPersists()
        {
            var p=session.Player;var m=p.GetComponent<ToolMastery>();p.AddWood(200);p.AddStone(200);p.AddCoins(200);
            Assert.That(m.Purchase(FarmTool.Axe),Is.False);
            for(int i=0;i<ToolMastery.Required(FarmTool.Axe,1);i++)m.Earn(FarmTool.Axe,1);
            Assert.That(m.Level(FarmTool.Axe),Is.EqualTo(1),"Unlock is not free equipment.");
            int coins=p.Coins;Assert.That(m.Purchase(FarmTool.Axe),Is.True);Assert.That(p.Coins,Is.EqualTo(coins-15));
            for(int i=0;i<20;i++)m.Earn(FarmTool.Axe,1);
            Assert.That(m.Uses(FarmTool.Axe,2),Is.Zero,"Tier one cannot farm the next tier's mastery.");
            p.AddEquipment("Bow");
            for(int i=0;i<ToolMastery.Required(FarmTool.Bow,1);i++)m.Earn(FarmTool.Bow);
            Assert.That(m.Purchase(FarmTool.Bow),Is.True);
            var save=Object.FindFirstObjectByType<GameSaveSystem>();save.SaveGame(false);m.Restore(null);
            Assert.That(save.TryLoadGame(),Is.True);Assert.That(m.Level(FarmTool.Bow),Is.EqualTo(2));Assert.That(m.Level(FarmTool.Axe),Is.EqualTo(2));
            yield return null;
        }
        [UnityTest] public IEnumerator ResourceTiersAndChickensUseExistingHealthLootAndSave()
        {
            var p=session.Player;var tiers=Object.FindObjectsByType<ResourceTier>(FindObjectsSortMode.None);
            Assert.That(tiers.Select(t=>t.Tier).Distinct().Count(),Is.EqualTo(3));
            var rich=tiers.First(t=>t.Tier==2&&t.Tool==FarmTool.Pickaxe);
            Assert.That(rich.CanHarvest(p),Is.False);p.GetComponent<PlayerToolUpgradeController>().Restore(1,2,1);Assert.That(rich.CanHarvest(p),Is.True);
            int iron=p.GetComponent<AdventureProgress>().Data.iron;rich.Grant(p);Assert.That(p.GetComponent<AdventureProgress>().Data.iron,Is.EqualTo(iron+2));
            var chicken=Object.FindObjectsByType<AnimalResource>(FindObjectsSortMode.None).First(a=>a.name.StartsWith("Chicken "));
            int food=p.Food;chicken.TakeDamage(100,p);chicken.TakeDamage(100,p);Assert.That(p.Food,Is.EqualTo(food+2));Assert.That(chicken.IsAlive,Is.False);
            var save=Object.FindFirstObjectByType<GameSaveSystem>();save.SaveGame(false);Assert.That(save.TryLoadGame(),Is.True);Assert.That(chicken.IsAlive,Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator CiviliansHaveBodiesMoveDieAndReduceSavedSecurity()
        {
            var people=VillageResidentHealth.All.ToArray();Assert.That(people.Length,Is.EqualTo(5));
            foreach(var person in people){Assert.That(person.GetComponent<Collider2D>().isTrigger,Is.False);Assert.That(DamageRules.CanPlayerHit(person),Is.False);}
            var positions=people.Select(p=>p.transform.position).ToArray();yield return new WaitForSeconds(6.5f);
            Assert.That(people.Where((p,i)=>Vector3.Distance(p.transform.position,positions[i])>.1f).Any(),Is.True);
            int strength=session.Core.Maximum;int safety=session.Security.Percent;people[0].TakeDamage(100,null);people[0].TakeDamage(100,null);
            Assert.That(session.Security.Percent,Is.LessThan(safety));Assert.That(session.Core.Maximum,Is.EqualTo(strength));
            int damagedSafety=session.Security.Percent;
            var save=Object.FindFirstObjectByType<GameSaveSystem>();save.SaveGame(false);Assert.That(save.TryLoadGame(),Is.True);
            Assert.That(people[0].IsAlive,Is.False);Assert.That(session.Security.Percent,Is.EqualTo(damagedSafety));
            Assert.That(save.TryLoadGame(),Is.True);Assert.That(session.Security.Percent,Is.EqualTo(damagedSafety),"Loading preserves casualties without applying damage twice.");
        }
        [UnityTest] public IEnumerator MovingFurniturePreservesContentsCostsNothingAndCancelKeepsOriginal()
        {
            var p=session.Player;var build=p.GetComponent<ConstructionSystem>();p.GetComponent<ValleyCampaign>().Teleport(new Vector3(0,-10));p.AddWood(20);Physics2D.SyncTransforms();
            var site=FurnitureTestSites.Find(build);build.Begin("Chest");Assert.That(build.PlaceSelected(site),Is.True);build.Cancel();
            var data=build.Buildings.First(b=>b.x==site.x&&b.y==site.y);data.wood=17;data.food=4;
            var chest=Object.FindObjectsByType<PlacedBuilding>(FindObjectsSortMode.None).Single(b=>b.Data==data);
            int wood=p.Wood;build.SelectMoveMode();Assert.That(build.SelectPieceAt(chest.GetComponentInChildren<SpriteRenderer>().bounds.center),Is.True);
            build.Cancel();Assert.That(new Vector2(data.x,data.y),Is.EqualTo(site));Assert.That(data.wood,Is.EqualTo(17));
            Assert.That(build.BeginMove(data),Is.True);var destination=FurnitureTestSites.Find(build,site);Assert.That(build.PlaceSelected(destination),Is.True);
            Assert.That(build.IsSelectingMove,Is.True);Assert.That(new Vector2(data.x,data.y),Is.EqualTo(destination));Assert.That(p.Wood,Is.EqualTo(wood));
            Assert.That(data.wood,Is.EqualTo(17));Assert.That(data.food,Is.EqualTo(4));build.Cancel();
            yield return null;
        }
        [UnityTest] public IEnumerator RaidersCanCommitAnAttackAgainstACivilian()
        {
            var p=session.Player;p.GetComponent<ValleyCampaign>().Teleport(new Vector3(12,-12));
            var person=VillageResidentHealth.All.First();person.GetComponent<VillageNpcRoutine>().enabled=false;
            person.transform.position=new Vector3(0,-10);int health=person.Health;
            session.Advance(session.Remaining+.1f);session.Advance(session.Settings.duskSeconds+1);
            var enemy=session.Raids.Enemies[0];enemy.ConfigureRaid(session,RaidRole.Chaser,null);enemy.ActivateFromPool(new Vector3(.75f,-10));Physics2D.SyncTransforms();
            yield return new WaitForSeconds(1.4f);
            Assert.That(person.Health,Is.LessThan(health),"The raider must damage a civilian, not only retarget its transform.");enemy.ReturnToPool();
        }
        [UnityTest] public IEnumerator IntroPausesTimeAndBanksKeepFeetOnGrass()
        {
            var intro=session.gameObject.AddComponent<FarmIntroduction>();intro.Open();float before=session.Remaining;
            session.Advance(100);yield return null;Assert.That(session.Remaining,Is.EqualTo(before));Assert.That(InventoryPanelSystem.IsOpen,Is.True);
            intro.Finish();Assert.That(session.IsPaused,Is.False);
            session.Advance(session.Remaining+.1f);Assert.That(session.Phase,Is.EqualTo(SlicePhase.Preparation));
            var bank=GameObject.Find("West River Bank").GetComponent<BoxCollider2D>();Assert.That(bank.bounds.max.y,Is.GreaterThanOrEqualTo(8.4f));
            var feet=session.Player.GetComponent<CircleCollider2D>();Assert.That(feet.offset.y-feet.radius,Is.LessThan(-.6f));
            Assert.That(Object.FindObjectsByType<EnvironmentSpriteAnimation>(FindObjectsSortMode.None).Any(a=>a.Frames!=null&&a.Frames.Length==4),Is.True);
        }
    }
}
