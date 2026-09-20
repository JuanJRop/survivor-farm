using System.Collections;
using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class VillageSecurityTests
    {
        private PortfolioSession session;
        [UnitySetUp] public IEnumerator OpenVillage()
        {
            SceneManager.LoadScene("Main");
            for(int i=0;i<100;i++)
            {
                yield return null;session=PortfolioSession.Instance;
                if(session!=null&&session.IsReady)break;
            }
            Assert.That(session.IsReady,Is.True);session.BeginNewGame();
            foreach(var person in VillageResidentHealth.All)person.GetComponent<VillageNpcRoutine>().enabled=false;
        }
        [UnityTearDown] public IEnumerator CloseVillage()
        {
            Time.timeScale=1;var main=SceneManager.GetSceneByName("Main");
            SceneManager.SetActiveScene(SceneManager.CreateScene("AfterVillageSecurity"));
            if(main.IsValid())yield return SceneManager.UnloadSceneAsync(main);
        }

        [UnityTest] public IEnumerator DamageAndAidAffectVillageSecurityWithoutChangingTheWell()
        {
            var safety=session.Security;var person=VillageResidentHealth.All.First();var player=session.Player;
            Assert.That(safety.Percent,Is.EqualTo(100));int core=session.Core.Health;
            person.TakeDamage(4,null);Assert.That(safety.Percent,Is.LessThan(100));
            int damaged=safety.Percent;person.TakeDamage(4,player);
            Assert.That(safety.Percent,Is.EqualTo(damaged),"Friendly attacks cannot injure villagers.");
            player.AddFood(1);player.transform.position=person.transform.position+Vector3.down*.6f;
            int food=player.Food;Assert.That(person.Aid(player),Is.True);
            Assert.That(player.Food,Is.EqualTo(food-1));Assert.That(safety.Percent,Is.EqualTo(100));
            session.Core.TakeDamage(1000,null);Assert.That(session.Core.Health,Is.EqualTo(core));
            Assert.That(session.Phase,Is.EqualTo(SlicePhase.Day));yield return null;
        }

        [UnityTest] public IEnumerator DestroyedHomeCanBeRebuiltAtItsDoorAndCasualtiesStayDead()
        {
            var player=session.Player;var home=VillageHouseHealth.All.First();var person=VillageResidentHealth.All.First();
            home.TakeDamage(1000,null);person.TakeDamage(1000,null);
            Assert.That(session.Security.DestroyedHouses,Is.EqualTo(1));Assert.That(session.Security.FallenResidents,Is.EqualTo(1));
            int safety=session.Security.Percent;player.AddWood(2);player.transform.position=home.Transform.position+Vector3.down*.3f;
            int wood=player.Wood;Assert.That(home.Repair(player),Is.True);
            Assert.That(home.Health,Is.EqualTo(8));Assert.That(player.Wood,Is.EqualTo(wood-2));
            Assert.That(home.IsAlive,Is.True);Assert.That(session.Security.Percent,Is.GreaterThan(safety));
            player.AddFood(1);player.transform.position=person.transform.position;
            Assert.That(person.Aid(player),Is.False);Assert.That(person.Health,Is.Zero);
            yield return null;
        }

        [UnityTest] public IEnumerator LegacySnapshotWithoutResidentsRestoresHealthyDefaultsAtHome()
        {
            var person=VillageResidentHealth.All.First();var home=person.Home;
            person.transform.position+=Vector3.right*3;person.TakeDamage(1000,null);
            Assert.That(person.IsAlive,Is.False);
            var legacy=session.Capture();legacy.residents=null;
            session.Restore(legacy);
            Assert.That(person.IsAlive,Is.True);Assert.That(person.Health,Is.EqualTo(person.Maximum));
            Assert.That((Vector2)person.transform.position,Is.EqualTo(home));
            Assert.That(session.Security.FallenResidents,Is.Zero);Assert.That(session.Security.Percent,Is.EqualTo(100));
            yield return null;
        }

        [UnityTest] public IEnumerator OccupationPersistsItsGarrisonAndRequiresCombatAndRebuildingToLiberate()
        {
            var player=session.Player;var homes=VillageHouseHealth.All.ToArray();var people=VillageResidentHealth.All.ToArray();
            foreach(var home in homes)home.TakeDamage(1000,null);
            foreach(var person in people)person.TakeDamage(1000,null);
            var safety=session.Security;
            Assert.That(safety.IsOccupied,Is.True);Assert.That(safety.Percent,Is.Zero);
            Assert.That(session.InCombat,Is.True);Assert.That(session.Phase,Is.Not.EqualTo(SlicePhase.Defeat));
            Assert.That(safety.GarrisonRemaining,Is.EqualTo(4));Assert.That(safety.CanLiberate,Is.False);
            float remaining=session.Remaining;session.Advance(100);Assert.That(session.Remaining,Is.EqualTo(remaining));
            var first=session.Raids.Enemies.First(e=>e.IsAlive);first.TakeDamage(1000,player);first.ReturnToPool();
            var snapshot=session.Capture();Assert.That(snapshot.security.garrisonRemaining,Is.EqualTo(3));
            session.Restore(snapshot);Assert.That(safety.IsOccupied,Is.True);Assert.That(safety.GarrisonRemaining,Is.EqualTo(3));
            foreach(var invader in session.Raids.Enemies.Where(e=>e.IsAlive).ToArray())
            {invader.TakeDamage(1000,player);invader.ReturnToPool();}
            Assert.That(safety.CanLiberate,Is.False,"Defeating invaders alone does not rebuild the village.");
            player.AddWood(2);player.transform.position=homes[0].Transform.position+Vector3.down*.3f;
            Assert.That(homes[0].Repair(player),Is.True);Assert.That(safety.CanLiberate,Is.True);
            player.transform.position=safety.RecoveryPoint;Assert.That(safety.Liberate(player),Is.True);
            Assert.That(safety.IsOccupied,Is.False);Assert.That(safety.Percent,Is.GreaterThan(0));
            Assert.That(session.Phase,Is.EqualTo(SlicePhase.Day));Assert.That(safety.FallenResidents,Is.EqualTo(people.Length));
            yield return null;
        }
    }
}
