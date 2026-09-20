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
            Assert.That(FarmDefense.All.Any(d=>FortressPieces.IsWall(d.Kind)||d.Kind=="Trap"||d.Kind=="Turret"),Is.False);
            var house=VillageHouseHealth.All.First();house.TakeDamage(8,null);
            Assert.That(session.Security.Percent,Is.LessThan(100));
            player.transform.position=house.Transform.position;
            int wood=player.Wood,health=house.Health;
            Assert.That(house.Repair(player),Is.True);Assert.That(house.Health,Is.GreaterThan(health));Assert.That(player.Wood,Is.EqualTo(wood-2));
            Assert.That(session.Security.Percent,Is.EqualTo(100));
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
                session.PrepareNow();session.Advance(session.Settings.duskSeconds+1);
                Assert.That(session.Phase,Is.EqualTo(SlicePhase.Night));
                Assert.That(Object.FindFirstObjectByType<DayNightCycle>().TrySleepUntilMorning(),Is.False);
                for(int tick=0;tick<180&&session.Phase==SlicePhase.Night;tick++)
                {
                    session.Advance(2);
                    foreach(var enemy in session.Raids.Enemies)
                        if(enemy.IsAlive)
                        {
                            Assert.That(enemy.GetComponentInChildren<SpriteRenderer>().transform.localScale.x,
                                Is.EqualTo(enemy.Role==RaidRole.Brute&&!EnemyRoster.IsTinyRpg(enemy.CombatStyle)?1.35f:1f).Within(.001f),
                                "Reused enemies must retain native Tiny RPG scale and reset the legacy brute enlargement.");
                            enemy.TakeDamage(100,player);enemy.ReturnToPool();
                        }
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
        public IEnumerator VillageOccupationSuspendsRaidAndCanBeLiberatedWithoutRevivingTheFallen()
        {
            session.PrepareNow();session.Advance(session.Settings.duskSeconds+1);session.Advance(5);
            session.Core.TakeDamage(100,null);
            Assert.That(session.Phase,Is.EqualTo(SlicePhase.Night),"The well is no longer the objective.");
            foreach(var house in VillageHouseHealth.All.ToArray())house.TakeDamage(100,null);
            foreach(var person in VillageResidentHealth.All.ToArray())person.TakeDamage(100,null);
            Assert.That(session.Security.IsOccupied,Is.True);Assert.That(session.Security.Percent,Is.Zero);
            Assert.That(session.Raids.Alive,Is.InRange(1,4));
            float remaining=session.Remaining;session.Advance(100);
            Assert.That(session.Remaining,Is.EqualTo(remaining));
            Assert.That(session.CanSave,Is.False,"Occupation keeps the last safe preparation checkpoint.");
            foreach(var enemy in session.Raids.Enemies.Where(e=>e.IsAlive).ToArray())enemy.TakeDamage(100,session.Player);
            Assert.That(session.Security.CanLiberate,Is.False,"Defeating invaders alone cannot restore a ruined settlement.");
            var home=VillageHouseHealth.All.First();session.Player.transform.position=home.Transform.position;
            Assert.That(home.Repair(session.Player),Is.True);
            session.Player.transform.position=session.Security.RecoveryPoint;
            Assert.That(session.Security.Liberate(session.Player),Is.True);
            Assert.That(session.Security.IsOccupied,Is.False);Assert.That(session.Phase,Is.EqualTo(SlicePhase.Day));
            Assert.That(session.Security.LivingResidents,Is.Zero,"Rebuilding does not silently resurrect villagers.");
            Assert.That(session.Security.Percent,Is.GreaterThan(0));
            var save=Object.FindFirstObjectByType<GameSaveSystem>();Assert.That(save.TryLoadGame(),Is.True,save.LastSaveError);
            Assert.That(session.Security.IsOccupied,Is.False);Assert.That(session.Security.LivingResidents,Is.Zero);
            yield return null;
        }
    }
}
