using System.Collections;
using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class DungeonPresentationTests
    {
        private bool loadedMain;

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            Time.timeScale = 1;
            if (!loadedMain) yield break;
            loadedMain = false;
            var main = SceneManager.GetSceneByName("Main");
            SceneManager.SetActiveScene(SceneManager.CreateScene("AfterDungeonPresentation"));
            if (main.IsValid() && main.isLoaded) yield return SceneManager.UnloadSceneAsync(main);
        }

        [UnityTest]
        public IEnumerator ShrineFootprintBlocksMovementAndCandlesAnimate()
        {
            var root = new GameObject("Shrine collision regression");
            root.transform.position = new Vector3(100, 100);
            var visual = root.AddComponent<SpriteRenderer>();
            var art = Resources.Load<DungeonArtCatalog>("DungeonArt");
            visual.sprite = art.Slice(art.Statue, 7, 0, 48, 48);
            root.transform.localScale = Vector3.one * (2.2f / visual.sprite.bounds.size.x);
            DungeonShrinePresentation.Configure(visual, art);
            var solid = root.GetComponent<BoxCollider2D>();
            var probe = new GameObject("Walking body");
            try
            {
                Physics2D.SyncTransforms();
                Assert.IsFalse(solid.isTrigger);
                Assert.Greater(solid.bounds.size.x, 1.8f);
                var animation = root.GetComponent<EnvironmentSpriteAnimation>();
                Assert.AreEqual(4, animation.Frames.Length);
                Assert.AreEqual(4, animation.Frames.Distinct().Count());
                Assert.AreEqual(3, root.GetComponentsInChildren<SpriteRenderer>().Length);
                probe.transform.position = new Vector3(100, solid.bounds.min.y - .35f);
                var body = probe.AddComponent<Rigidbody2D>(); body.gravityScale = 0; body.freezeRotation = true;
                probe.AddComponent<CircleCollider2D>().radius = .2f;
                Physics2D.SyncTransforms();
                for (int i = 0; i < 20; i++)
                {
                    body.MovePosition(body.position + Vector2.up * .08f);
                    yield return new WaitForFixedUpdate();
                }
                Assert.Less(body.position.y, solid.bounds.min.y, "Walking must stop in front of the pedestal.");
            }
            finally { Object.DestroyImmediate(probe); Object.DestroyImmediate(root); }
        }

        [UnityTest]
        public IEnumerator DungeonTimeReachesNightAndSuppliesPersist()
        {
            loadedMain = true;
            SceneManager.LoadScene("Main");
            PortfolioSession session = null;
            for (int i = 0; i < 120; i++)
            {
                yield return null; session = PortfolioSession.Instance;
                if (session != null && session.IsReady) break;
            }
            Assert.IsNotNull(session); Assert.IsTrue(session.IsReady); session.BeginNewGame();
            {
                var adventure = session.Adventure;
                session.Player.transform.position = VillageAdventure.PortalPositions[0] + Vector3.down;
                Assert.IsTrue(adventure.EnterDungeon(0));
                yield return new WaitForSeconds(.2f);
                var clockText = GameObject.Find("Reloj del valle").GetComponentInChildren<UnityEngine.UI.Text>();
                Assert.IsTrue(clockText.text.Contains(":"), "The running countdown must stay visible inside a dungeon.");
                var dungeon = adventure.Dungeons[0];
                var boxes = dungeon.GetComponentsInChildren<DungeonDestructible>();
                Assert.AreEqual(2, boxes.Length);
                Assert.AreEqual(2, dungeon.GetComponentsInChildren<DungeonShrinePresentation>().Length);
                Assert.IsTrue(dungeon.Chest.GetComponent<Collider2D>().enabled);
                Assert.IsFalse(dungeon.Chest.GetComponent<Collider2D>().isTrigger);
                var doorway = dungeon.GetComponentInChildren<VillageAdventurePortal>();
                Assert.AreEqual(Resources.Load<DungeonArtCatalog>("DungeonArt").Door,
                    doorway.GetComponent<SpriteRenderer>().sprite.texture);
                boxes[0].TakeDamage(100, session.Player);
                var saved = JsonUtility.FromJson<VillageAdventureSnapshot>(JsonUtility.ToJson(adventure.Capture()));
                adventure.ExitDungeon(); adventure.Restore(saved);
                Assert.IsFalse(boxes[0].IsAlive);
                Assert.AreEqual(0, boxes[0].Health);
                Assert.IsTrue(adventure.EnterDungeon(0));
                Assert.IsFalse(boxes[0].IsAlive);
                session.Advance(session.Remaining + .1f);
                Assert.AreEqual(SlicePhase.Preparation, session.Phase);
                Assert.IsTrue(adventure.IsInsideDungeon);
                session.Advance(session.Remaining + .1f);
                Assert.AreEqual(SlicePhase.Night, session.Phase);
                Assert.IsTrue(adventure.IsInsideDungeon);
                Assert.Greater(session.Raids.Total, 0);
            }
        }
    }
}
