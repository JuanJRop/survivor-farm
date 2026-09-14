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
    public sealed class PortfolioIntegrationTests
    {
        private PortfolioSession session;
        [UnitySetUp]
        public IEnumerator OpenAuthoredMain()
        {
            SceneManager.LoadScene("Main");
            for(int i=0;i<100;i++)
            {
                yield return null;session=Object.FindFirstObjectByType<PortfolioSession>();
                if(session!=null&&session.IsReady)break;
            }
            Assert.That(session,Is.Not.Null);Assert.That(session.IsReady,Is.True);
            Assert.That(Time.timeScale,Is.EqualTo(0));
            session.BeginNewGame();
            Assert.That(session.Phase,Is.EqualTo(SlicePhase.Day));
        }
        [UnityTearDown]
        public IEnumerator CloseMain()
        {
            Time.timeScale=1;
            var main=SceneManager.GetSceneByName("Main");
            var empty=SceneManager.CreateScene("AfterPortfolioCheck");SceneManager.SetActiveScene(empty);
            if(main.IsValid())yield return SceneManager.UnloadSceneAsync(main);
        }
        [UnityTest]
        public IEnumerator ExistingFarmSupportsCropsRepairDamageAndSave()
        {
            var player=session.Player;var stats=player.GetComponent<PlayerSurvivalStats>();
            Assert.That(Object.FindObjectsByType<FarmingPlot>(FindObjectsSortMode.None).Length,Is.EqualTo(6));
            Assert.That(Object.FindFirstObjectByType<OutdoorEnemyPool>().enabled,Is.False);
            Assert.That(DamageRules.CanPlayerHit(session.Core),Is.False);
            Assert.That(DamageRules.CanPlayerHit(stats),Is.False);
            int hp=stats.CurrentHealth;stats.TakeDamage(1);stats.TakeDamage(1);
            Assert.That(stats.CurrentHealth,Is.EqualTo(hp-1),"Simultaneous hits need a grace period.");
            var plot=Object.FindObjectsByType<FarmingPlot>(FindObjectsSortMode.None).First();
            player.transform.position=plot.transform.position+Vector3.down*.7f;
            int seeds=player.CommonSeeds;
            plot.Interact(FarmTool.Sword,player);Assert.That(plot.StateId,Is.EqualTo(3));
            plot.Interact(FarmTool.Sword,player);Assert.That(plot.StateId,Is.EqualTo(4));
            plot.AdvanceGrowth(36);Assert.That(plot.StateId,Is.EqualTo(5));
            plot.Interact(FarmTool.Sword,player);
            Assert.That(player.Fruit,Is.EqualTo(2));Assert.That(player.CommonSeeds,Is.EqualTo(seeds));
            Assert.That(player.GetComponent<PlayerCraftingController>().Craft("Food"),Is.True);
            var fence=FarmDefense.All.First(d=>d.Kind=="Fence"&&d.Health<d.Maximum);
            player.transform.position=fence.transform.position+Vector3.down;
            int wood=player.Wood,health=fence.Health;
            Assert.That(fence.Repair(player),Is.True);Assert.That(fence.Health,Is.GreaterThan(health));Assert.That(player.Wood,Is.EqualTo(wood-2));
            var save=Object.FindFirstObjectByType<GameSaveSystem>();save.SaveGame(false);
            Assert.That(save.IsSavingBlocked,Is.False,save.LastSaveError);
            int fruit=player.Fruit;player.AddFruit(20);
            Assert.That(save.TryLoadGame(),Is.True,save.LastSaveError);
            Assert.That(player.Fruit,Is.EqualTo(fruit));Assert.That(session.Day,Is.EqualTo(1));
            yield return null;
        }
        [UnityTest]
        public IEnumerator ThreeBoundedNightsLeadToTwoPhaseBossAndVictory()
        {
            var player=session.Player;
            for(int day=1;day<=3;day++)
            {
                Assert.That(session.Day,Is.EqualTo(day));
                session.PrepareNow();session.Advance(30);
                Assert.That(session.Phase,Is.EqualTo(SlicePhase.Night));
                Assert.That(Object.FindFirstObjectByType<DayNightCycle>().TrySleepUntilMorning(),Is.False);
                for(int tick=0;tick<180&&session.Phase==SlicePhase.Night;tick++)
                {
                    session.Advance(2);
                    foreach(var enemy in session.Raids.Enemies)
                        if(enemy.IsAlive){enemy.TakeDamage(100,player);enemy.ReturnToPool();}
                    Assert.That(session.Raids.Alive,Is.LessThanOrEqualTo(session.Settings.maximumConcurrentEnemies));
                }
                if(day<3){Assert.That(session.Phase,Is.EqualTo(SlicePhase.Dawn));session.Advance(9);}
            }
            Assert.That(session.Phase,Is.EqualTo(SlicePhase.BossIntro));session.Advance(6);
            Assert.That(session.Phase,Is.EqualTo(SlicePhase.Boss));
            var boss=session.Raids.Boss;
            boss.TakeDamage(boss.MaximumHealth/2,player);
            Assert.That(session.Raids.BossPattern.PhaseTwo,Is.True);
            boss.TakeDamage(1000,player);
            Assert.That(session.Phase,Is.EqualTo(SlicePhase.Victory));
            Assert.That(session.Raids.Alive,Is.EqualTo(0));Assert.That(Time.timeScale,Is.EqualTo(0));
            session.Advance(10000);Assert.That(session.Phase,Is.EqualTo(SlicePhase.Victory));
            yield return null;
        }
        [UnityTest]
        public IEnumerator CoreDestructionEndsEncounterAndCancelsSpawning()
        {
            session.PrepareNow();session.Advance(30);session.Advance(5);
            session.Core.TakeDamage(100,null);
            Assert.That(session.Phase,Is.EqualTo(SlicePhase.Defeat));
            int count=session.Raids.Spawned;session.Advance(100);
            Assert.That(session.Raids.Spawned,Is.EqualTo(count));Assert.That(session.Raids.Alive,Is.EqualTo(0));
            Assert.That(session.CanSave,Is.False,"A lost encounter must not overwrite its preparation checkpoint.");
            yield return null;
        }
    }
}
