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
    public sealed class PracticeSceneTests
    {
        private PracticeSession practice;
        private IEnumerator Load(PracticeMode mode)
        {
            PracticeSession.Open(mode);
            for(int i=0;i<180;i++)
            {
                yield return null;practice=PortfolioSession.Instance?.Practice;
                if(practice!=null&&practice.Ready)break;
            }
            Assert.That(practice!=null&&practice.Ready,Is.True,"Practice initialization failed.");
        }
        [UnitySetUp] public IEnumerator Setup(){yield return Load(PracticeMode.Combat);}
        [UnityTearDown] public IEnumerator Teardown()
        {
            Time.timeScale=1;var scene=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("AfterPractice"));
            yield return SceneManager.UnloadSceneAsync(scene);
        }
        [UnityTest] public IEnumerator ArenaHasItsOwnTerrainBoundsAndNoVillageColliders()
        {
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo(PracticeSession.ArenaScene));
            Assert.That(GameObject.Find("Arena · claro del entrenamiento"),Is.Not.Null);
            Assert.That(Object.FindObjectsByType<TreeResource>(FindObjectsSortMode.None),Is.Empty);
            Assert.That(Object.FindObjectsByType<VillageResidentHealth>(FindObjectsSortMode.None),Is.Empty);
            Assert.That(Physics2D.OverlapCircleAll(Vector2.zero,.6f).Any(c=>!c.isTrigger),Is.False);
            Assert.That(Physics2D.OverlapCircleAll(new Vector2(16,-2),.2f).Any(c=>!c.isTrigger),Is.True);
            Assert.That(practice.Session.Player.GetComponent<PlayerSurvivalStats>().CurrentHealth,Is.EqualTo(5));yield return null;
        }
        [UnityTest] public IEnumerator PracticeExplicitlyUnlocksBowAndRefillsAmmunitionWithoutAccumulatingIt()
        {
            var player = practice.Session.Player;
            Assert.IsTrue(player.OwnsEquipment("Bow"));
            Assert.AreEqual(PracticeSession.PracticeArrowSupply, player.GetItemCount("Arrow"));
            var belt = player.GetComponent<PlayerToolbelt>(); belt.Select(FarmTool.Bow);
            Assert.AreEqual(FarmTool.Bow, belt.SelectedTool);
            var combat = player.GetComponent<PlayerCombatController>();
            Assert.IsTrue(combat.TryShootBowAt(player.transform.position + Vector3.right * 5));
            yield return new WaitForSeconds(.5f);
            Assert.AreEqual(PracticeSession.PracticeArrowSupply - 1, player.GetItemCount("Arrow"));
            practice.Refill(); practice.Refill();
            Assert.AreEqual(PracticeSession.PracticeArrowSupply, player.GetItemCount("Arrow"));
            Assert.IsTrue(player.OwnsEquipment("Bow"));
        }
        [UnityTest] public IEnumerator EveryCreatureAndBossCanSpawnAndTakeDamage()
        {
            for(int index=0;index<PracticeWaveDirector.Roster.Length;index++)
            {
                practice.Waves.StartSingle(index);practice.Waves.Tick(2);practice.Waves.Tick(2);yield return null;
                var enemy=index==6?(EnemyAIBase)practice.Session.Raids.Boss:practice.Waves.Enemies.Single();
                Assert.That(enemy.IsAlive,Is.True,PracticeWaveDirector.Roster[index]);
                Assert.That(enemy.GetComponentsInChildren<SpriteRenderer>().Any(s=>s.enabled&&s.sprite!=null),Is.True,enemy.name);
                Assert.That(enemy.GetComponentsInChildren<Collider2D>().Any(c=>c.enabled),Is.True,enemy.name);
                enemy.TakeDamage(999,practice.Session.Player);Assert.That(enemy.CurrentHealth,Is.Zero,enemy.name);
                practice.Waves.Tick(0);
            }
            Assert.That(practice.Session.Phase,Is.EqualTo(SlicePhase.Day));
        }
        [UnityTest] public IEnumerator CircuitCompletesAllFiveWavesAndCanRestart()
        {
            practice.Waves.StartCircuit();bool bossSeen=false;
            for(int step=0;step<150&&practice.Waves.Running;step++)
            {
                practice.Waves.Tick(7);
                foreach(var enemy in practice.Waves.Enemies.ToArray())if(enemy.IsAlive)enemy.TakeDamage(999,practice.Session.Player);
                var boss=practice.Session.Raids.Boss;
                if(boss!=null&&boss.IsAlive){bossSeen=true;boss.TakeDamage(999,practice.Session.Player);}
                yield return null;
            }
            Assert.That(bossSeen,Is.True);Assert.That(practice.Waves.Running,Is.False);
            Assert.That(practice.Status,Does.Contain("Circuito completado"));
            practice.Waves.StartCircuit();Assert.That(practice.Waves.Running,Is.True);Assert.That(practice.Waves.WaveIndex,Is.Zero);
        }
        [UnityTest] public IEnumerator ReplacingEncounterCancelsOldEnemiesAndBossMarkers()
        {
            practice.Waves.StartSingle(6);practice.Waves.Tick(2);practice.Waves.Tick(2);
            var old=practice.Session.Raids.Boss;Assert.That(old.IsAlive,Is.True);
            practice.Waves.StartSingle(4);Assert.That(old.IsAlive,Is.False);
            practice.Waves.Tick(2);practice.Waves.Tick(2);yield return null;
            var archer=(RaidEnemy)practice.Waves.Enemies.Single();archer.ChooseTarget();
            Assert.That(archer.TargetKind,Is.EqualTo("Player"));Assert.That(archer.SelectedTarget,Is.EqualTo(practice.Session.Player.transform));
            Assert.That(practice.Session.Raids.Boss,Is.Null);
        }
        [UnityTest] public IEnumerator PracticeCannotLoadOrWriteCampaignAndTimeDoesNotAdvance()
        {
            yield return Load(PracticeMode.Farm);
            var session=practice.Session;var save=Object.FindFirstObjectByType<GameSaveSystem>();
            string slot=PlayerPrefs.GetString("SurvivorFarmPortfolioSlot","");string path=save.SaveFile;
            bool existed=File.Exists(path);byte[] before=existed?File.ReadAllBytes(path):null;
            save.SaveGame(false);Assert.That(save.TryLoadGame(),Is.False);Assert.That(session.CanSave,Is.False);
            session.Advance(99999);session.PrepareNow();Assert.That(session.Phase,Is.EqualTo(SlicePhase.Day));
            Assert.That(session.Remaining,Is.Zero);Assert.That(PlayerPrefs.GetString("SurvivorFarmPortfolioSlot",""),Is.EqualTo(slot));
            Assert.That(File.Exists(path),Is.EqualTo(existed));if(existed)CollectionAssert.AreEqual(before,File.ReadAllBytes(path));
        }
        [UnityTest] public IEnumerator FarmKeepsWorkingCropsAndHouseholdUtilitiesWithoutDefenses()
        {
            yield return Load(PracticeMode.Farm);var p=practice.Session.Player;
            var plots=Object.FindObjectsByType<FarmingPlot>(FindObjectsSortMode.None).Where(f=>f.StateId==2).ToArray();
            Assert.That(plots.Length,Is.GreaterThanOrEqualTo(6));
            var plot=plots[0];practice.Teleport(plot.transform.position+Vector3.down*.6f);
            plot.Interact(FarmTool.Hoe,p);Assert.That(plot.StateId,Is.EqualTo(3));
            plot.Interact(FarmTool.WateringCan,p);Assert.That(plot.StateId,Is.EqualTo(4));
            practice.GrowCrops();Assert.That(plot.StateId,Is.EqualTo(5));
            plot.Interact(FarmTool.Hoe,p);Assert.That(practice.Session.Harvested,Is.EqualTo(1));
            practice.GoToStation(1);Physics2D.SyncTransforms();var build=p.GetComponent<ConstructionSystem>();
            build.Begin("Fence");Assert.That(ConstructionSystem.IsPlacing,Is.False);
            var site=FurnitureTestSites.Find(build);build.Begin("Chest");Assert.That(build.PlaceSelected(site),Is.True);
            Assert.That(build.PlaceSelected(FurnitureTestSites.Find(build,site)),Is.True);
            var piece=build.Buildings.Last();Assert.That(build.BeginMove(piece),Is.True);build.Cancel();Assert.That(build.Demolish(piece),Is.True);
        }
        [UnityTest] public IEnumerator FarmContainsThreeResourceTiersAndRepeatableSupplies()
        {
            yield return Load(PracticeMode.Farm);var p=practice.Session.Player;
            foreach(int tier in new[]{1,2,3})Assert.That(Object.FindObjectsByType<ResourceTier>(FindObjectsSortMode.None).Any(r=>r.Tier==tier),Is.True);
            foreach(int tier in new[]{1,2,3})
            {
                practice.SetToolTier(tier);
                foreach(var tool in ToolMastery.Branches)Assert.That(p.GetComponent<ToolMastery>().Level(tool),Is.EqualTo(tier),tool.ToString());
            }
            p.TryRemoveWood(100);practice.Refill();int wood=p.Wood,iron=p.GetComponent<AdventureProgress>().Data.iron,gold=p.GetItemCount("GoldOre");
            practice.Refill();Assert.That(p.Wood,Is.EqualTo(wood));Assert.That(p.Wood,Is.EqualTo(180));
            Assert.That(p.GetComponent<AdventureProgress>().Data.iron,Is.EqualTo(iron));Assert.That(p.GetItemCount("GoldOre"),Is.EqualTo(gold));
        }
        [UnityTest] public IEnumerator AllFarmStationsLandOnClearDryGround()
        {
            yield return Load(PracticeMode.Farm);var player=practice.Session.Player.transform;
            for(int station=0;station<6;station++)
            {
                practice.GoToStation(station);Physics2D.SyncTransforms();
                Assert.That(FarmExploration.IsRiver(player.position,.4f),Is.False);
                Assert.That(Physics2D.OverlapCircleAll(player.position,.4f).Any(c=>!c.isTrigger&&!c.transform.IsChildOf(player)),Is.False,"Station "+station);
            }
        }
        [UnityTest] public IEnumerator DestroyedHighlightedTargetIsSafeDuringSceneUnload()
        {
            var interactor=practice.Session.Player.GetComponent<FarmPlayerInteractor>();
            var target=new GameObject("Destroyed interaction target").AddComponent<FarmingPlot>();
            typeof(FarmPlayerInteractor).GetField("highlightedInteractable",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(interactor,(IWorldInteractable)target);
            IWorldInteractable reference=target;Object.DestroyImmediate(target.gameObject);
            Assert.That(FarmPlayerInteractor.Supports(reference,FarmTool.Hoe),Is.False);
            interactor.PerformInteraction();interactor.enabled=false;yield return null;
            LogAssert.NoUnexpectedReceived();interactor.enabled=true;
        }
        [UnityTest] public IEnumerator DefeatAndPauseRemainRecoverableAndMenuStillStartsDemo()
        {
            practice.Session.Pause(true);Assert.That(Time.timeScale,Is.Zero);practice.Session.Pause(false);
            practice.Waves.StartSingle(3);practice.Waves.Tick(2);practice.Waves.Tick(2);
            practice.Session.Player.GetComponent<PlayerSurvivalStats>().TakeDamage(999);yield return null;yield return null;
            Assert.That(practice.Waves.Running,Is.False);Assert.That(practice.Session.Player.GetComponent<PlayerSurvivalStats>().CurrentHealth,Is.EqualTo(5));
            practice.ReturnToTitle();for(int i=0;i<120;i++){yield return null;if(PortfolioSession.Instance!=null&&PortfolioSession.Instance.IsReady)break;}
            var demo=PortfolioSession.Instance;Assert.That(demo.IsPractice,Is.False);Assert.That(demo.HasBegun,Is.False);
            var menu=demo.GetComponent<DemoFrontEnd>();menu.PracticeScreen();Assert.That(menu.ScreenName,Is.EqualTo("Prácticas"));
            menu.Home();demo.BeginNewGame();Assert.That(demo.Phase,Is.EqualTo(SlicePhase.Day));Assert.That(demo.Day,Is.EqualTo(1));
            Assert.IsFalse(demo.Player.OwnsEquipment("Bow"), "Practice supplies must not unlock the campaign bow.");
        }
    }
}
