using System.Collections;
using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class LootProgressionTests
    {
        private GameObject root;
        private PlayerInventory player;
        [SetUp] public void SetUp()
        {
            root = new GameObject("Loot progression fixture");
            var actor = new GameObject("Player"); actor.transform.SetParent(root.transform);
            player = actor.AddComponent<PlayerInventory>(); actor.AddComponent<PlayerSurvivalStats>();
            actor.AddComponent<PlayerCraftingController>();
        }
        [TearDown] public void TearDown() => Object.DestroyImmediate(root);

        [Test] public void ArrowBundlesAndLeatherSaddleSpendExactMaterialsAndCannotDuplicateSaddle()
        {
            player.Restore(0,0,0,20,10,0,0,99,2); player.AddItem("Leather",8);
            var craft = player.GetComponent<PlayerCraftingController>();
            Assert.IsTrue(craft.Craft("Arrow")); Assert.AreEqual(8, player.GetItemCount("Arrow"));
            Assert.AreEqual(18, player.Wood); Assert.AreEqual(9, player.Stone);
            Assert.IsTrue(craft.Craft("Saddle")); Assert.AreEqual(14, player.Wood);
            Assert.AreEqual(0, player.GetItemCount("Leather")); Assert.AreEqual(1, player.GetItemCount("Saddle"));
            player.AddItem("Leather",8); Assert.IsFalse(craft.Craft("Saddle")); Assert.AreEqual(8,player.GetItemCount("Leather"));
        }
        [Test] public void ArcherAlwaysDropsAmmunitionAndEveryLootItemHasAShadow()
        {
            EnemyLootTable.Drop(Vector3.right*5,root.transform,EnemyCombatStyle.ArcherGoblin,false,3);
            var drops=root.GetComponentsInChildren<EnemyLootPickup>();
            Assert.IsTrue(drops.Any(d=>d.Item.Kind==ItemKind.Arrow&&d.Amount>=3));
            Assert.IsTrue(drops.All(d=>d.transform.Find("Loot ground shadow")!=null));
        }
        [UnityTest] public IEnumerator NearbyLootFliesQuicklyToPlayerWithoutWalkingOverIt()
        {
            var drop=EnemyLootPickup.Spawn(null,Vector3.right*1.8f,root.transform,ResourceFlyweights.Item(ItemKind.Arrow),4);
            Vector3 start=player.transform.position;
            yield return new WaitForSeconds(1.05f);
            Assert.AreEqual(4,player.GetItemCount("Arrow")); Assert.AreEqual(start,player.transform.position);
            Assert.IsTrue(drop==null||!drop.IsUncollected);
        }
        [UnityTest] public IEnumerator LootCannotMagnetThroughSolidWalls()
        {
            var wall=new GameObject("Wall",typeof(BoxCollider2D));wall.transform.SetParent(root.transform);
            wall.transform.position=Vector3.right*.8f;wall.GetComponent<BoxCollider2D>().size=new Vector2(.15f,3);
            var drop=EnemyLootPickup.Spawn(null,Vector3.right*1.7f,root.transform,ResourceFlyweights.Item(ItemKind.Arrow),4);
            Physics2D.SyncTransforms(); yield return new WaitForSeconds(1.05f);
            Assert.AreEqual(0,player.GetItemCount("Arrow")); Assert.IsFalse(drop.IsAttracting);
        }
        [UnityTest] public IEnumerator OverlappingPickupTriggersCannotCollectAcrossAThinWall()
        {
            var body=player.gameObject.AddComponent<Rigidbody2D>();body.gravityScale=0;body.constraints=RigidbodyConstraints2D.FreezeAll;
            player.gameObject.AddComponent<CircleCollider2D>().radius=.2f;
            var wall=new GameObject("Thin wall",typeof(BoxCollider2D));wall.transform.SetParent(root.transform);
            wall.transform.position=Vector3.right*.225f;wall.GetComponent<BoxCollider2D>().size=new Vector2(.04f,3);
            var drop=EnemyLootPickup.Spawn(null,Vector3.right*.45f,root.transform,ResourceFlyweights.Item(ItemKind.Arrow),4);
            Physics2D.SyncTransforms();yield return new WaitForSeconds(.9f);
            Assert.AreEqual(0,player.GetItemCount("Arrow"),"Touching trigger circles must not bypass the wall checked by magnetic attraction.");
            Assert.IsTrue(drop.IsUncollected);
            wall.transform.position+=Vector3.up*5;Physics2D.SyncTransforms();yield return new WaitForSeconds(.3f);
            Assert.AreEqual(4,player.GetItemCount("Arrow"));
        }
        [Test] public void ScatteringCannotThrowLootOntoTheFarSideOfAWall()
        {
            var wall=new GameObject("Scatter wall",typeof(BoxCollider2D));wall.transform.SetParent(root.transform);
            wall.transform.position=Vector3.right*.6f;wall.GetComponent<BoxCollider2D>().size=new Vector2(.2f,8);
            Physics2D.SyncTransforms();
            var drop=EnemyLootPickup.Scatter(Vector3.zero,root.transform,ItemKind.Arrow,4,angle:0);
            Assert.Less(drop.LandingPosition.x,wall.GetComponent<BoxCollider2D>().bounds.min.x);
            Assert.IsFalse(Physics2D.LinecastAll(Vector2.zero,drop.LandingPosition).Any(h=>h.collider==wall.GetComponent<BoxCollider2D>()),"A free destination beyond a wall is not a reachable drop location.");
        }
    }
}
