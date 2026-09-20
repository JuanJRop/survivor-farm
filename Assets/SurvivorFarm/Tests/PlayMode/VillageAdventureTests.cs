using System.Collections;
using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class VillageAdventureTests
    {
        private PortfolioSession session;
        private VillageAdventure adventure;
        [UnitySetUp]
        public IEnumerator OpenVillage()
        {
            SceneManager.LoadScene("Main");
            for (int i = 0; i < 120; i++)
            {
                yield return null;
                session = Object.FindFirstObjectByType<PortfolioSession>();
                if (session != null && session.IsReady) break;
            }
            Assert.IsNotNull(session); Assert.IsTrue(session.IsReady);
            session.BeginNewGame(); adventure = session.Adventure;
            Assert.IsNotNull(adventure);
            session.Player.GetComponent<PlayerSurvivalStats>().GrantInvulnerability(100);
        }
        [UnityTearDown]
        public IEnumerator CloseVillage()
        {
            Time.timeScale = 1;
            var main = SceneManager.GetSceneByName("Main");
            var empty = SceneManager.CreateScene("AfterVillageAdventure"); SceneManager.SetActiveScene(empty);
            if (main.IsValid()) yield return SceneManager.UnloadSceneAsync(main);
        }

        [UnityTest]
        public IEnumerator CampsFightByDayAndChestsRequireInteractionAndPersistExactlyOnce()
        {
            Assert.AreEqual(3, adventure.Camps.Count); Assert.AreEqual(2, adventure.Dungeons.Count);
            foreach(var edge in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None)
                .Where(c=>c.name=="West World Edge"||c.name=="East World Edge"||c.name=="North World Edge"||c.name=="South World Edge"))
                Assert.IsFalse(edge.enabled, "The authored inner world edge must not block the expanded map.");
            foreach(var destination in adventure.Camps.Select(c=>(Vector2)c.transform.position+Vector2.down*4)
                .Concat(VillageAdventure.PortalPositions.Select(p=>(Vector2)p+Vector2.down*1.7f)))
                Assert.IsTrue(session.Navigation.HasRoute(new Vector2(1,-1),destination), "Excursion is unreachable: "+destination);
            var camp = adventure.Camps[0];
            Assert.IsTrue(FarmExploration.Contains(camp.transform.position, 6));
            Assert.Greater(Mathf.Abs(camp.transform.position.x), 32, "Excursions must occupy newly opened terrain.");
            session.Player.transform.position = camp.transform.position + Vector3.down * 3;
            Assert.AreEqual(SlicePhase.Day, session.Phase); Assert.IsTrue(camp.CanEngage);
            int coins = session.Player.Coins, wood = session.Player.Wood, stone = session.Player.Stone;
            Assert.IsFalse(camp.TryClaim(session.Player));
            foreach (var guard in camp.Members) guard.TakeDamage(1000, session.Player);
            Assert.IsTrue(camp.IsCleared); Assert.IsFalse(camp.Claimed);
            Assert.AreEqual(coins, session.Player.Coins, "The coffer reward is collected at the chest.");
            foreach (var drop in camp.GetComponentsInChildren<EnemyLootPickup>()) drop.Discard();
            session.Player.transform.position = camp.Chest.transform.position;
            Assert.IsTrue(camp.TryClaim(session.Player)); Assert.AreEqual(coins, session.Player.Coins);
            Assert.IsFalse(camp.Chest.GetComponent<Collider2D>().isTrigger);
            yield return new WaitForSeconds(.7f);
            foreach (var drop in camp.GetComponentsInChildren<EnemyLootPickup>()) drop.TryCollect(session.Player);
            Assert.AreEqual(coins + camp.Definition.Coins, session.Player.Coins);
            Assert.AreEqual(wood + camp.Definition.Iron * 2, session.Player.Wood);
            Assert.AreEqual(stone + camp.Definition.Iron, session.Player.Stone);
            var saved = JsonUtility.FromJson<VillageAdventureSnapshot>(JsonUtility.ToJson(adventure.Capture()));
            saved.encounters[0].center = new Vector3(float.PositiveInfinity, 9999);
            adventure.Restore(saved);
            Assert.AreEqual(camp.Definition.PreferredCenter, camp.transform.position);
            Assert.IsTrue(camp.Claimed); Assert.IsFalse(camp.TryClaim(session.Player));
            Assert.AreEqual(coins + camp.Definition.Coins, session.Player.Coins);
            Assert.IsTrue(camp.Members.All(e => !e.IsAlive));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DungeonKeepsVillageClockRunningFramesCameraAndReturnsWithPersistentProgress()
        {
            session.Player.transform.position = VillageAdventure.PortalPositions[0] + Vector3.down;
            float remaining = session.Remaining;
            Assert.IsTrue(adventure.EnterDungeon(0)); Assert.IsTrue(adventure.IsInsideDungeon);
            Assert.IsFalse(session.CanSave);
            session.Advance(60);
            Assert.AreEqual(remaining - 60, session.Remaining, .001f, "The village clock must keep running during an expedition.");
            var camp = adventure.Dungeons[0];
            Assert.IsTrue(camp.gameObject.activeInHierarchy); Assert.IsTrue(camp.CanEngage);
            Assert.Less(Vector2.Distance(Camera.main.transform.position, camp.transform.position), 10);
            camp.Members[0].TakeDamage(1000, session.Player);
            Assert.AreEqual(camp.Members.Count - 1, camp.Remaining);
            adventure.ExitDungeon();
            Assert.IsFalse(adventure.IsInsideDungeon); Assert.IsTrue(session.CanSave);
            Assert.Less(Vector2.Distance(session.Player.transform.position, VillageAdventure.PortalPositions[0]), 2);
            Assert.IsFalse(camp.gameObject.activeInHierarchy);
            var saved = JsonUtility.FromJson<VillageAdventureSnapshot>(JsonUtility.ToJson(adventure.Capture()));
            adventure.Restore(saved);
            Assert.IsTrue(adventure.EnterDungeon(0)); Assert.IsFalse(camp.Members[0].IsAlive);
            foreach (var enemy in camp.Members.Where(e => e.IsAlive)) enemy.TakeDamage(1000, session.Player);
            Assert.IsFalse(camp.Claimed);
            session.Player.transform.position = camp.Chest.transform.position;
            Assert.IsTrue(camp.TryClaim(session.Player));
            adventure.ExitDungeon();
            Assert.AreEqual(1, adventure.CompletedCount);
            Assert.IsTrue(adventure.EnterDungeon(0));
            var checkpoint = new Vector3(10, -12);
            session.Player.transform.position = checkpoint;
            adventure.Restore(adventure.Capture());
            Assert.AreEqual(checkpoint, session.Player.transform.position, "Loading an outdoor checkpoint from inside a dungeon must not overwrite it with the portal position.");
            Assert.IsFalse(adventure.IsInsideDungeon);
            Assert.IsTrue(camp.Claimed);
            session.Advance(session.Remaining + .1f);
            Assert.AreEqual(SlicePhase.Preparation, session.Phase); Assert.IsFalse(adventure.EnterDungeon(0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator DungeonSplitsEnemyThenSpawnsBossAndUnlocksBowAsPhysicalReward()
        {
            Assert.IsFalse(session.Player.OwnsEquipment("Bow"));
            session.Player.transform.position = VillageAdventure.PortalPositions[0] + Vector3.down;
            Assert.IsTrue(adventure.EnterDungeon(0));
            var camp = adventure.Dungeons[0];
            Assert.IsFalse(camp.MiniBoss.IsAlive);
            var parent = camp.Members[2];
            int fullSize = parent.MaximumHealth;
            parent.TakeDamage(1000, session.Player);
            var children = camp.Members.Skip(camp.Definition.Roster.Length).Take(2).ToArray();
            Assert.IsTrue(children.All(c => c.IsAlive && c.MaximumHealth < fullSize && c.transform.localScale.x < 1));
            children[0].TakeDamage(1000, session.Player);
            var state = camp.Capture(); camp.Restore(JsonUtility.FromJson<EnemyCampState>(JsonUtility.ToJson(state)));
            Assert.IsFalse(parent.IsAlive); Assert.IsFalse(children[0].IsAlive); Assert.IsTrue(children[1].IsAlive);
            foreach (var enemy in camp.Members.Where(e => e != camp.MiniBoss).ToArray()) if (enemy.IsAlive) enemy.TakeDamage(1000, session.Player);
            Assert.IsTrue(camp.MiniBoss.IsAlive); Assert.IsTrue(camp.MiniBoss.IsElite);
            session.Player.transform.position = camp.Chest.transform.position + Vector3.down;
            Assert.IsFalse(camp.TryClaim(session.Player));
            camp.MiniBoss.TakeDamage(1000, session.Player);
            Assert.IsTrue(camp.TryClaim(session.Player));
            var bow = camp.GetComponentsInChildren<EnemyLootPickup>().Single(d => d.Item.Kind == ItemKind.Bow);
            Assert.IsFalse(session.Player.OwnsEquipment("Bow"));
            session.Player.transform.position = camp.transform.position + Vector3.down * 4;
            yield return new WaitForSeconds(.65f);
            Assert.IsTrue(bow.TryCollect(session.Player)); Assert.IsTrue(session.Player.OwnsEquipment("Bow"));
            camp.Restore(camp.Capture()); Assert.IsFalse(camp.TryClaim(session.Player));
            Assert.IsTrue(camp.GetComponentsInChildren<EnemyLootPickup>().Any(d => d.Item.Kind == ItemKind.Diamond));
        }

        [UnityTest] public IEnumerator SplitFragmentsStayReachableWhenTheParentDiesBesideAWall()
        {
            session.Player.transform.position=VillageAdventure.PortalPositions[0]+Vector3.down;
            Assert.IsTrue(adventure.EnterDungeon(0));var camp=adventure.Dungeons[0];
            var wall=new GameObject("Fragment spawn wall",typeof(BoxCollider2D));wall.transform.SetParent(camp.transform,false);
            wall.transform.localPosition=new Vector3(1.25f,0);wall.GetComponent<BoxCollider2D>().size=new Vector2(.4f,4);
            var parent=camp.Members[2];parent.transform.position=camp.transform.position+Vector3.right*.75f;
            Physics2D.SyncTransforms();parent.TakeDamage(1000,session.Player);Physics2D.SyncTransforms();
            var fragments=camp.Members.Skip(camp.Definition.Roster.Length).Take(2).ToArray();
            Assert.IsTrue(fragments.All(f=>f.IsAlive));
            foreach(var fragment in fragments)
            {
                Assert.IsFalse(Physics2D.OverlapCircleAll(fragment.transform.position,.26f).Contains(wall.GetComponent<BoxCollider2D>()),"A fragment embedded in a wall would block dungeon completion.");
                Assert.Less(fragment.transform.position.x,wall.GetComponent<BoxCollider2D>().bounds.min.x,"A fragment must not teleport through the wall while splitting.");
            }
            Assert.IsFalse(camp.MiniBoss.IsAlive);Assert.IsFalse(camp.IsCleared);
            var snapshot=JsonUtility.FromJson<EnemyCampState>(JsonUtility.ToJson(camp.Capture()));camp.Restore(snapshot);
            Assert.IsFalse(parent.IsAlive);Assert.IsTrue(fragments.All(f=>f.IsAlive));Assert.IsFalse(camp.MiniBoss.IsAlive);
            Object.Destroy(wall);yield return null;
        }
    }
}
