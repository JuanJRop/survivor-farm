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
    public sealed class JuicePracticeTarget : EnemyAIBase { protected override void TickEnemy() { } }
    public sealed class JuiceInteractionZone : WorldInteractable
    {
        public override string GetInteractionLabel(FarmTool tool)=>"Interaction range, not a solid object";
        public override void Interact(FarmTool tool,PlayerInventory inventory) { }
    }
    public sealed class CombatJuiceTests
    {
        private GameObject root;private PlayerInventory player;private PlayerCombatController combat;
        [SetUp] public void Setup()
        {
            Time.timeScale=1;GameFeelFeedback.Enabled=true;CombatTimeFeedback.ReducedMotion=false;
            root=new GameObject("Juice fixture");var actor=new GameObject("Player");actor.transform.SetParent(root.transform);
            actor.AddComponent<SpriteRenderer>();player=actor.AddComponent<PlayerInventory>();actor.AddComponent<PlayerToolbelt>();
            if(actor.GetComponent<PlayerCraftingController>()==null)actor.AddComponent<PlayerCraftingController>();
            actor.AddComponent<PlayerCharacterAnimator>();combat=actor.AddComponent<PlayerCombatController>();
        }
        [TearDown] public void Teardown()
        {
            Object.DestroyImmediate(root);
            foreach(var burst in Object.FindObjectsByType<ImpactBurstLifetime>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.DestroyImmediate(burst.gameObject);
            Time.timeScale=1;CombatTimeFeedback.ReducedMotion=false;
        }
        private JuicePracticeTarget Enemy(float distance)
        {
            var actor=new GameObject("Receiver");actor.transform.SetParent(root.transform);var art=actor.AddComponent<SpriteRenderer>();actor.AddComponent<CircleCollider2D>().radius=.12f;
            var enemy=actor.AddComponent<JuicePracticeTarget>();enemy.ConfigureVisuals(art,null,Color.white,"Receiver");
            enemy.Configure(player.transform,null);enemy.ConfigureStats("Receiver",30,1,1,.5f,1,0);enemy.ActivateFromPool(Vector3.right*distance);Physics2D.SyncTransforms();return enemy;
        }
        [Test] public void HoldStateSeparatesTapPartialAndFullRelease()
        {
            var hold=combat.Charge;hold.Press(10);Assert.That(hold.Release(10.1f),Is.Zero);Assert.That(hold.IsPressed,Is.False);
            hold.Press(10);Assert.That(hold.ProgressAt(10.6f),Is.InRange(.1f,.9f));Assert.That(hold.Release(11.1f),Is.EqualTo(1));
            hold.Press(20);hold.Cancel();Assert.That(hold.ProgressAt(30),Is.Zero);
            Assert.That(combat.Combo.Definition.chargedAttack.damageMultiplier,Is.GreaterThan(combat.Combo.Definition.attacks[2].damageMultiplier));
        }
        [UnityTest] public IEnumerator ChargedReleaseHitsOnceAtItsAnimationFrameAndPushesFurther()
        {
            player.GetComponent<PlayerCraftingController>().Restore(0,0,savedWeapon:2);
            var enemy=Enemy(1.2f);
            var distant=Enemy(1.6f);distant.transform.position=new Vector3(1.6f,.7f);
            var behind=Enemy(-.8f);Physics2D.SyncTransforms();
            int hits=0;bool charged=false;
            player.GetComponent<HitFeedback>().HitResolved+=h=>{hits++;charged=h.Charged;};
            Assert.That(combat.BeginCharge(),Is.True);yield return new WaitForSeconds(1.15f);
            Assert.That(enemy.CurrentHealth,Is.EqualTo(30));Assert.That(combat.Charge.Progress,Is.EqualTo(1));
            combat.ReleaseCharge(enemy);Assert.That(enemy.CurrentHealth,Is.EqualTo(30));
            yield return new WaitForSeconds(.75f);
            Assert.That(enemy.CurrentHealth,Is.EqualTo(22));Assert.That(hits,Is.EqualTo(1));Assert.That(charged,Is.True);
            Assert.That(distant.CurrentHealth,Is.EqualTo(30),"Charging adds impact without granting a distant sword hit.");
            Assert.That(behind.CurrentHealth,Is.EqualTo(30),"A charged forward swing must not hit directly behind the player.");
            Assert.That(enemy.transform.position.x,Is.GreaterThan(2.5f));Assert.That(combat.Charge.IsPressed,Is.False);
        }
        [UnityTest] public IEnumerator AShortClickIsOneNormalComboAttack()
        {
            player.GetComponent<PlayerCraftingController>().Restore(0,0,savedWeapon:2);
            var enemy=Enemy(.8f);Assert.That(combat.BeginCharge(),Is.True);
            yield return new WaitForSeconds(.05f);combat.ReleaseCharge(enemy);yield return new WaitForSeconds(.65f);
            Assert.That(enemy.CurrentHealth,Is.EqualTo(28));Assert.That(combat.LastAttackWasCharged,Is.False);
        }
        [UnityTest] public IEnumerator DamageAnimationAndPauseCancelAChargeWithoutReleasingIt()
        {
            player.GetComponent<PlayerCraftingController>().Restore(0,0,savedWeapon:2);
            var enemy=Enemy(.8f);combat.BeginCharge();yield return new WaitForSeconds(.3f);
            player.GetComponent<PlayerCharacterAnimator>().PlayNamedAction("Damage");yield return null;
            Assert.That(combat.Charge.IsPressed,Is.False);Assert.That(enemy.CurrentHealth,Is.EqualTo(30));
            player.GetComponent<PlayerCharacterAnimator>().CancelAction();combat.BeginCharge();Time.timeScale=0;yield return null;
            Assert.That(combat.Charge.IsPressed,Is.False);Assert.That(player.GetComponent<CombatFeelRangeCue>().IsCharging,Is.False);
        }
        [UnityTest] public IEnumerator ChargedKnockbackCannotCrossSolidCover()
        {
            var enemy=Enemy(.8f);var wall=new GameObject("Solid wall");wall.transform.SetParent(root.transform);wall.transform.position=new Vector3(1.5f,0);
            wall.AddComponent<BoxCollider2D>().size=new Vector2(.15f,3);Physics2D.SyncTransforms();
            player.GetComponent<HitFeedback>().ApplyDamage(enemy,1,player,true,FarmTool.Sword,true);
            yield return new WaitForSeconds(.65f);Assert.That(enemy.transform.position.x,Is.LessThan(1.16f));
        }
        [Test] public void OpenGapPreventsBreachingButAClosedEnclosureAllowsIt()
        {
            FarmDefense gap=null;
            for(int y=-2;y<=2;y+=4)for(int x=-1;x<=1;x+=2)gap=Wall(new Vector2(x,y),0);
            Wall(new Vector2(-2,0),1);Wall(new Vector2(2,0),1);
            var nav=new FarmRaidNavigation();var origin=new Vector2(0,5);
            Assert.That(nav.HasRoute(origin,Vector2.zero),Is.False);
            Assert.That(nav.BlockingWall(origin,Vector2.zero),Is.Not.Null);
            gap.gameObject.SetActive(false);nav.Invalidate();
            Assert.That(nav.HasRoute(origin,Vector2.zero),Is.True);Assert.That(nav.BlockingWall(origin,Vector2.zero),Is.Null);
        }
        private FarmDefense Wall(Vector2 p,int rotation)
        {
            var actor=new GameObject("Wall");actor.transform.SetParent(root.transform);actor.transform.position=p;
            actor.AddComponent<BoxCollider2D>().size=FortressPieces.Footprint("Fence",rotation);
            var defense=actor.AddComponent<FarmDefense>();defense.Configure(new BuildingData{kind="Fence",rotation=rotation,wallVersion=1},null,player);return defense;
        }
        [Test] public void FallbackAssignmentsAreStableAndEvenlySplit()
        {Assert.That(Enumerable.Range(0,20).Count(RaidTargetPolicy.TargetsPlayer),Is.EqualTo(10));Assert.That(RaidTargetPolicy.TargetsPlayer(4),Is.False);}
    }
    public sealed class CombatJuiceIntegrationTests
    {
        private PortfolioSession session;
        [UnitySetUp] public IEnumerator Setup()
        {
            SceneManager.LoadScene("Main");for(int i=0;i<100;i++){yield return null;session=PortfolioSession.Instance;if(session!=null&&session.IsReady)break;}
            Assert.That(session.IsReady,Is.True);session.BeginNewGame();
        }
        [UnityTearDown] public IEnumerator Teardown()
        {
            Time.timeScale=1;var main=SceneManager.GetSceneByName("Main");var empty=SceneManager.CreateScene("AfterJuice");SceneManager.SetActiveScene(empty);
            if(main.IsValid())yield return SceneManager.UnloadSceneAsync(main);
        }
        [UnityTest] public IEnumerator InteractionVolumesDoNotRejectFurnitureButSolidBodiesDo()
        {
            var p=session.Player;var build=p.GetComponent<ConstructionSystem>();p.GetComponent<ValleyCampaign>().Teleport(new Vector3(0,-10));p.AddWood(50);
            Physics2D.SyncTransforms();var site=FurnitureTestSites.Find(build);
            var zone=new GameObject("Non-solid service range");zone.transform.position=site;
            zone.AddComponent<BoxCollider2D>().size=new Vector2(5,5);zone.GetComponent<Collider2D>().isTrigger=true;zone.AddComponent<JuiceInteractionZone>();Physics2D.SyncTransforms();
            Assert.That(build.CanPlace("Chest",site,out var reason),Is.True,reason);
            zone.GetComponent<Collider2D>().isTrigger=false;Physics2D.SyncTransforms();
            Assert.That(build.CanPlace("Chest",site,out _),Is.False);
            zone.GetComponent<Collider2D>().isTrigger=true;Physics2D.SyncTransforms();
            build.Begin("Chest");Assert.That(build.PlaceSelected(site),Is.True);
            build.Cancel();Object.Destroy(zone);yield return null;
        }
        [UnityTest] public IEnumerator HoverUsesCanopyAndHarvestHasAWorldClockInsteadOfTextBars()
        {
            var p=session.Player;var tree=Object.FindObjectsByType<TreeResource>(FindObjectsSortMode.None).First(t=>t.GetComponent<ResourceTier>()?.Tier==1);
            var hover=p.GetComponent<ResourceHoverHint>();var art=tree.GetComponentInChildren<SpriteRenderer>();
            Assert.That(hover.FindAt(art.bounds.center),Is.Not.Null);
            p.GetComponent<ValleyCampaign>().Teleport(tree.transform.position+Vector3.down*.95f);Physics2D.SyncTransforms();
            tree.Interact(FarmTool.Axe,p);yield return new WaitForSeconds(.25f);
            Assert.That(tree.IsGathering,Is.True);Assert.That(p.GetComponent<WorldActionClock>().IsVisible,Is.True);
            Assert.That(tree.GetInteractionLabel(FarmTool.Axe),Does.Not.Contain("#").And.Not.Contain("%"));
            tree.CancelGathering();Assert.That(p.GetComponent<WorldActionClock>().IsVisible,Is.False);
        }
        [UnityTest] public IEnumerator GuidedPracticeRequiresMovementAndComboThenDefersChargeUntilTierTwo()
        {
            var p=session.Player;var intro=session.gameObject.AddComponent<FarmIntroduction>();intro.Open();float clock=session.Remaining;
            Assert.That(FarmIntroduction.AllowsMovement,Is.False);yield return new WaitForSecondsRealtime(.8f);
            Assert.That(intro.TryBeginMovement(Vector2.right),Is.True);Assert.That(FarmIntroduction.AllowsCombat,Is.False);
            p.GetComponent<ValleyCampaign>().Teleport(p.transform.position+Vector3.right*1.1f);yield return null;
            Assert.That(intro.CurrentLesson,Is.EqualTo(FarmIntroduction.Lesson.DashRunHint));
            yield return new WaitForSecondsRealtime(.8f);intro.Next();intro.Next();
            typeof(FarmIntroduction).GetField("ran",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .SetValue(intro,true);
            Assert.IsTrue(p.GetComponent<PlayerMovementController>().TryDash(Vector2.right));
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(intro.CurrentLesson,Is.EqualTo(FarmIntroduction.Lesson.ComboHint));
            yield return new WaitForSecondsRealtime(.8f);intro.Next();intro.Next();
            Assert.That(FarmIntroduction.AllowsCombat,Is.True);Assert.That(intro.PracticeEnemy,Is.Not.Null);
            var combat=p.GetComponent<PlayerCombatController>();
            for(int i=0;i<3;i++)
            {
                intro.PracticeEnemy.transform.position=p.transform.position+Vector3.right*.8f;Physics2D.SyncTransforms();
                combat.AttackTarget(intro.PracticeEnemy);yield return new WaitForSeconds(i==2?.4f:.46f);
            }
            yield return new WaitForSecondsRealtime(.7f);Assert.That(intro.CurrentLesson,Is.EqualTo(FarmIntroduction.Lesson.Mission));
            Assert.That(combat.SwordTier,Is.EqualTo(1));Assert.That(combat.CanChargeSword,Is.False);
            Assert.That(session.Remaining,Is.EqualTo(clock));Assert.That(p.GetComponent<ToolMastery>().Uses(FarmTool.Sword,1),Is.Zero);
            intro.Finish();Assert.That(FarmIntroduction.IsOpen,Is.False);Assert.That(session.IsPaused,Is.False);
        }
        [UnityTest] public IEnumerator RaiderRetargetsAfterCivilianDeathToAnIntactHouse()
        {
            var p=session.Player;p.GetComponent<ValleyCampaign>().Teleport(new Vector3(10,-12));
            var people=VillageResidentHealth.All.ToArray();foreach(var person in people)person.GetComponent<VillageNpcRoutine>().enabled=false;
            var civilian=people[0];civilian.transform.position=new Vector3(0,-10);foreach(var other in people.Skip(1))other.transform.position=new Vector3(-20,12);
            session.PrepareNow();session.Advance(session.Settings.duskSeconds+1);
            var enemy=session.Raids.Enemies[0];enemy.ConfigureRaid(session,RaidRole.Chaser,null,1);enemy.ActivateFromPool(new Vector3(.8f,-10));Physics2D.SyncTransforms();enemy.ChooseTarget();
            Assert.That(enemy.SelectedTarget,Is.SameAs(civilian.transform));civilian.TakeDamage(100,null);yield return new WaitForSeconds(.7f);
            Assert.That(enemy.TargetKind,Is.EqualTo("House"));
            Assert.That(enemy.SelectedTarget.GetComponent<VillageHouseHealth>()?.IsAlive,Is.True);enemy.ReturnToPool();
        }
        [UnityTest] public IEnumerator ArchersDeliverRealProjectilesToCiviliansAndHouses()
        {
            var p=session.Player;p.GetComponent<ValleyCampaign>().Teleport(new Vector3(12,-12));
            var people=VillageResidentHealth.All.ToArray();var person=people[0];
            foreach(var other in people.Skip(1))other.gameObject.SetActive(false);
            person.GetComponent<VillageNpcRoutine>().enabled=false;person.transform.position=new Vector3(0,-10);
            session.PrepareNow();session.Advance(session.Settings.duskSeconds+1);
            var arrows=EnemyProjectilePool.Ensure(session.gameObject,24);var enemy=session.Raids.Enemies[0];
            enemy.ConfigureRaid(session,RaidRole.Archer,arrows,0);enemy.ActivateFromPool(new Vector3(-3,-10));Physics2D.SyncTransforms();
            int health=person.Health;yield return new WaitForSeconds(1.65f);
            Assert.That(person.Health,Is.LessThan(health),"An archer must launch and connect, not just choose the civilian.");
            person.gameObject.SetActive(false);enemy.ReturnToPool();
            var home=VillageHouseHealth.All.First();home.transform.position=new Vector3(0,-10);session.Navigation.Invalidate();
            enemy.ConfigureRaid(session,RaidRole.Archer,arrows,0);enemy.ActivateFromPool(new Vector3(-3,-10));Physics2D.SyncTransforms();
            health=home.Health;yield return new WaitForSeconds(1.65f);
            Assert.That(home.Health,Is.LessThan(health),"House-targeting archers must have a valid projectile receiver.");enemy.ReturnToPool();
        }
    }
}
