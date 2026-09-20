using System.Collections;
using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class VillageProgressionTests
    {
        private PortfolioSession session;
        private VillageProgression village;
        [UnitySetUp] public IEnumerator Setup()
        {
            SceneManager.LoadScene("Main");
            for(int i=0;i<180;i++){yield return null;session=PortfolioSession.Instance;if(session!=null&&session.IsReady)break;}
            Assert.That(session!=null&&session.IsReady,Is.True);
            session.BeginNewGame();village=VillageProgression.Instance;Assert.That(village,Is.Not.Null);
            session.Player.GetComponent<ValleyCampaign>().Teleport(village.WellPosition+Vector3.down);
            foreach(var npc in Object.FindObjectsByType<VillageNpcRoutine>(FindObjectsSortMode.None))npc.enabled=false;
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            Time.timeScale=1;
            session?.Player.GetComponent<VillageUpgradeWindow>()?.Close();
            var main=SceneManager.GetSceneByName("Main");SceneManager.SetActiveScene(SceneManager.CreateScene("AfterVillageProgression"));
            if(main.IsValid())yield return SceneManager.UnloadSceneAsync(main);
        }
        private void Fund(){session.Player.AddWood(600);session.Player.AddStone(600);session.Player.AddCoins(1000);session.Player.AddFood(30);}

        [UnityTest] public IEnumerator UpgradesSpendRealResourcesRespectCapsAndImproveHouseArt()
        {
            var house=village.Houses.First();var stock=session.Player;
            int wood=stock.Wood,stone=stock.Stone,coins=stock.Coins;
            Assert.That(village.UpgradeHouse(house),Is.False);
            Assert.That((stock.Wood,stock.Stone,stock.Coins),Is.EqualTo((wood,stone,coins)),"Rejected purchases cannot partially charge inventory.");
            Fund();wood=stock.Wood;stone=stock.Stone;coins=stock.Coins;
            var art=house.GetComponentsInChildren<SpriteRenderer>().First(s=>s.enabled&&s.sprite!=null);var original=art.sprite;
            Assert.That(village.UpgradeHouse(house),Is.True);
            Assert.That(house.Level,Is.EqualTo(2));Assert.That(house.Maximum,Is.EqualTo(56));Assert.That(house.Health,Is.EqualTo(56));
            Assert.That(art.sprite,Is.Not.SameAs(original));
            Assert.That((stock.Wood,stock.Stone,stock.Coins),Is.EqualTo((wood-20,stone-14,coins-20)));
            Assert.That(village.UpgradeHouse(house),Is.True);coins=stock.Coins;
            Assert.That(village.UpgradeHouse(house),Is.False);Assert.That(stock.Coins,Is.EqualTo(coins));
            Assert.That(village.UpgradeWell(),Is.True);Assert.That(village.UpgradeWell(),Is.True);coins=stock.Coins;
            Assert.That(village.UpgradeWell(),Is.False);Assert.That(stock.Coins,Is.EqualTo(coins));Assert.That(village.GuardCapacity,Is.EqualTo(4));
            house.TakeDamage(10,null);var saved=house.Capture();int level=house.Level;house.Restore(saved);
            Assert.That(house.Level,Is.EqualTo(level));Assert.That(house.Maximum,Is.EqualTo(80));Assert.That(house.Health,Is.EqualTo(70));
            village.OpenHouse(house);Assert.That(VillageUpgradeWindow.IsOpen,Is.True);yield return null;
        }

        [UnityTest] public IEnumerator RecruitTrainAndCasualtiesRoundTripWithoutCloningOrRevivingGuards()
        {
            Fund();Assert.That(village.Recruit(),Is.True);Assert.That(village.Recruit(),Is.True);
            int coins=session.Player.Coins;Assert.That(village.Recruit(),Is.False);Assert.That(session.Player.Coins,Is.EqualTo(coins));
            var guard=village.Guards[0];Assert.That(village.TrainStrength(guard),Is.True);Assert.That(village.TrainToughness(guard),Is.True);
            Assert.That(guard.Damage,Is.EqualTo(3));Assert.That(guard.Body.Maximum,Is.EqualTo(23));
            int integrity=session.Security.Percent;guard.Body.TakeDamage(999,null);Assert.That(session.Security.Percent,Is.EqualTo(integrity));
            var saved=JsonUtility.FromJson<VillageProgressionState>(JsonUtility.ToJson(village.Capture()));
            village.Restore(null);Assert.That(village.Guards,Is.Empty);village.Restore(saved);yield return null;
            Assert.That(village.Guards.Count,Is.EqualTo(2));guard=village.Guards.First(g=>g.Slot==0);
            Assert.That(guard.Body.Health,Is.Zero);Assert.That(guard.Body.IsAlive,Is.False);Assert.That(guard.Strength,Is.EqualTo(2));Assert.That(guard.Toughness,Is.EqualTo(2));
            Assert.That(village.Recover(guard),Is.True);Assert.That(guard.Body.Health,Is.EqualTo(23));Assert.That(guard.Body.IsAlive,Is.True);
            coins=session.Player.Coins;Assert.That(village.Recover(guard),Is.False);Assert.That(session.Player.Coins,Is.EqualTo(coins));
            session.Player.GetComponent<ValleyCampaign>().Teleport(new Vector3(25,-14));
            coins=session.Player.Coins;Assert.That(village.UpgradeWell(),Is.False);Assert.That(session.Player.Coins,Is.EqualTo(coins),"Management is local, not available while exploring.");
        }

        [UnityTest] public IEnumerator GuardsTelegraphCanMissAndKeepFightingWithoutThePlayerNearby()
        {
            Fund();Assert.That(village.Recruit(),Is.True);var guard=village.Guards[0];
            session.Player.GetComponent<ValleyCampaign>().Teleport(new Vector3(25,-14));
            var enemyRoot=new GameObject("Guard combat test enemy");enemyRoot.AddComponent<CircleCollider2D>().isTrigger=true;
            var enemy=enemyRoot.AddComponent<GuardTestEnemy>();enemy.Configure(session.Player.transform,null);enemy.ConfigureStats("Prueba",30,1,0,1,30,0);
            enemy.ActivateFromPool(guard.transform.position+Vector3.right*.8f);Physics2D.SyncTransforms();
            float until=Time.time+2;
            while(!guard.IsPreparingAttack&&Time.time<until)yield return null;
            Assert.That(guard.IsPreparingAttack,Is.True);Assert.That(enemy.CurrentHealth,Is.EqualTo(30),"Windup cannot deal immediate damage.");
            enemy.transform.position+=Vector3.right*1.8f;Physics2D.SyncTransforms();yield return new WaitForSeconds(.45f);
            Assert.That(enemy.CurrentHealth,Is.EqualTo(30),"Moving out of the committed strike dodges it.");
            enemy.transform.position=guard.transform.position+Vector3.right*.8f;Physics2D.SyncTransforms();
            until=Time.time+3;while(guard.LandedStrikes==0&&Time.time<until)yield return null;
            Assert.That(guard.LandedStrikes,Is.EqualTo(1));Assert.That(enemy.CurrentHealth,Is.EqualTo(28));
            yield return new WaitForSeconds(.15f);Assert.That(guard.LandedStrikes,Is.EqualTo(1),"Guard strikes have a real cooldown.");
            enemy.transform.position=new Vector3(80,0);Physics2D.SyncTransforms();yield return null;
            Assert.That(guard.CurrentTarget,Is.Null,"Guard pursuit stays close to the village.");Object.Destroy(enemyRoot);
        }

        [UnityTest] public IEnumerator ARealRaiderFightsTheHiredGuardWhileThePlayerExploresADungeon()
        {
            Fund();Assert.That(village.Recruit(),Is.True);var guard=village.Guards[0];
            session.Player.GetComponent<ValleyCampaign>().Teleport(VillageAdventure.PortalPositions[0]);
            Assert.That(session.Adventure.EnterDungeon(0),Is.True);
            foreach(var dungeonEnemy in session.Adventure.Dungeons[0].GetComponentsInChildren<EnemyAIBase>())dungeonEnemy.ReturnToPool();
            session.Advance(session.Remaining+.1f);session.Advance(session.Remaining+.1f);
            Assert.That(session.InCombat,Is.True);Assert.That(session.Adventure.IsInsideDungeon,Is.True);
            var raider=session.Raids.Enemies[0];raider.ConfigureRaid(session,RaidRole.Chaser,null);
            raider.ConfigureStats("Asaltante de prueba",30,1,.4f,1.15f,2f,0);
            raider.ActivateFromPool(guard.transform.position+Vector3.right*.85f);Physics2D.SyncTransforms();
            float until=Time.time+4;
            while(guard.LandedStrikes==0&&Time.time<until)yield return null;
            Assert.That(guard.LandedStrikes,Is.GreaterThan(0));Assert.That(raider.CurrentHealth,Is.LessThan(30));
            Assert.That(raider.SelectedTarget,Is.EqualTo(guard.transform),"A defender draws local aggro instead of redirecting the raid into the distant dungeon.");
            Assert.That(Vector2.Distance(guard.transform.position,session.Player.transform.position),Is.GreaterThan(100));
            int health=guard.Body.Health;until=Time.time+4;
            while(guard.Body.Health==health&&Time.time<until)yield return null;
            Assert.That(guard.Body.Health,Is.LessThan(health),"The enemy must actually retaliate against the recruited guard.");
            raider.ReturnToPool();session.Adventure.ExitDungeon();
        }
    }
    public sealed class GuardTestEnemy : EnemyAIBase
    {
        protected override void TickEnemy() { }
    }
}
