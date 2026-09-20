using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class RegistryTestEnemy : EnemyAIBase
    {
        protected override void TickEnemy() { }
    }

    public sealed class AiScalabilityTests
    {
        private GameObject root;
        private int initialEnemies;
        [SetUp] public void Setup()
        {
            root=new GameObject("AI scalability fixture");initialEnemies=EnemyAIBase.ActiveEnemies.Count;
        }
        [TearDown] public void TearDown()=>Object.DestroyImmediate(root);

        private GameObject Child(string label,Vector2 position)
        {
            var child=new GameObject(label);child.transform.SetParent(root.transform);child.transform.position=position;return child;
        }
        private RegistryTestEnemy Enemy(string label)
        {
            var actor=Child(label,Vector2.zero);actor.AddComponent<CircleCollider2D>().isTrigger=true;
            var enemy=actor.AddComponent<RegistryTestEnemy>();enemy.ActivateFromPool(Vector3.zero);return enemy;
        }

        [Test] public void RegistryTracksPooledReuseDisableAndSwapRemovalWithoutDuplicates()
        {
            var first=Enemy("First");var middle=Enemy("Middle");var last=Enemy("Last");
            Assert.That(EnemyAIBase.ActiveEnemies.Count,Is.EqualTo(initialEnemies+3));
            middle.ReturnToPool();
            Assert.That(EnemyAIBase.ActiveEnemies,Has.No.Member(middle));
            last.ReturnToPool(); // It moved into the middle slot; its new index must also be removed.
            Assert.That(EnemyAIBase.ActiveEnemies.Count,Is.EqualTo(initialEnemies+1));
            for(int i=0;i<100;i++)
            {
                middle.ActivateFromPool(Vector3.right);middle.ActivateFromPool(Vector3.right);
                Assert.That(EnemyAIBase.ActiveEnemies.Count,Is.EqualTo(initialEnemies+2));
                middle.enabled=false;Assert.That(EnemyAIBase.ActiveEnemies,Has.No.Member(middle));
                middle.enabled=true;middle.ReturnToPool();
            }
            Object.DestroyImmediate(first.gameObject);
            Assert.That(EnemyAIBase.ActiveEnemies.Count,Is.EqualTo(initialEnemies));
        }

        [Test] public void UnloadingAnEnemyHierarchyLeavesNoStaleRegistryEntries()
        {
            Enemy("First");Enemy("Second");var pooled=Enemy("Inactive");pooled.ReturnToPool();
            root.SetActive(false);Assert.That(EnemyAIBase.ActiveEnemies.Count,Is.EqualTo(initialEnemies));
            root.SetActive(true);Assert.That(EnemyAIBase.ActiveEnemies.Count,Is.EqualTo(initialEnemies+2));
            Object.DestroyImmediate(root);root=null;
            Assert.That(EnemyAIBase.ActiveEnemies.Count,Is.EqualTo(initialEnemies));
            foreach(var enemy in EnemyAIBase.ActiveEnemies)Assert.That(enemy!=null,Is.True);
        }

        [Test] public void NavigationUsesOneBroadPhaseQueryAndReusesBoundedRouteBuffers()
        {
            var navigation=new FarmRaidNavigation();
            for(int pass=0;pass<3;pass++)
            {
                navigation.Invalidate();
                for(int i=0;i<48;i++)navigation.HasRoute(new Vector2(35,-20),new Vector2(-35+i*.5f,-20));
                Assert.That(navigation.MapRefreshCount,Is.EqualTo(pass+1));
                Assert.That(navigation.MapOverlapQueryCount,Is.EqualTo(pass+1));
                Assert.That(navigation.FieldBufferCount,Is.EqualTo(24),"Moving targets recycle the same fixed-size route cache.");
            }
        }

        [Test] public void RasterizedShapesMatchCircularClearanceAndRefreshAfterMovement()
        {
            Vector2 origin=new Vector2(25,19);
            var rotated=Child("Rotated solid",origin).AddComponent<BoxCollider2D>();
            rotated.size=new Vector2(2.5f,.22f);rotated.transform.rotation=Quaternion.Euler(0,0,33);
            var circle=Child("Round solid",origin+new Vector2(-2,1)).AddComponent<CircleCollider2D>();circle.radius=.55f;
            var polygon=Child("Triangle",origin+new Vector2(2,1)).AddComponent<PolygonCollider2D>();
            polygon.points=new[]{new Vector2(-.7f,-.5f),new Vector2(.8f,-.5f),new Vector2(0,.9f)};
            var railA=Child("Bridge rail A",origin+Vector2.down*2).AddComponent<BoxCollider2D>();railA.size=new Vector2(.2f,1.5f);
            var railB=Child("Bridge rail B",origin+new Vector2(1.5f,-2)).AddComponent<BoxCollider2D>();railB.size=railA.size;
            var ignored=Child("Trigger",origin+Vector2.up*2).AddComponent<BoxCollider2D>();ignored.isTrigger=true;
            var moving=Enemy("Moving enemy");moving.GetComponent<CircleCollider2D>().isTrigger=false;moving.transform.position=origin+Vector2.right*3;
            var player=Child("Moving player",origin+Vector2.left*3);player.AddComponent<CircleCollider2D>();player.AddComponent<PlayerInventory>();
            var navigation=new FarmRaidNavigation();
            CompareClearance(navigation,origin);
            rotated.transform.position+=Vector3.down*1.5f;circle.enabled=false;railB.transform.position+=Vector3.right;
            navigation.Invalidate();CompareClearance(navigation,origin);
            Assert.That(navigation.MapOverlapQueryCount,Is.EqualTo(2));
        }

        [UnityTest] public IEnumerator RemovingATurretDisposesItsProjectileStorageAndLeases()
        {
            var tower=Child("Turret",new Vector2(35,-20));tower.AddComponent<BoxCollider2D>();
            var defense=tower.AddComponent<FarmDefense>();
            defense.Configure(new BuildingData{kind="Turret",health=-1},null,null);
            var pool=(PlayerProjectilePool)typeof(FarmDefense).GetField("projectiles",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(defense);
            Assert.That(pool,Is.Not.Null);Assert.That(pool.CreatedCount,Is.EqualTo(1));
            var arrow=pool.Rent();Assert.That(arrow,Is.Not.Null);
            Object.Destroy(tower);
            yield return null;yield return null;
            Assert.That(pool==null,Is.True,"The pool is a sibling of the tower and needs explicit ownership cleanup.");
            Assert.That(arrow==null,Is.True,"Outstanding leases must also be disposed when their owner is removed.");
        }

        private static void CompareClearance(FarmRaidNavigation navigation,Vector2 origin)
        {
            Physics2D.SyncTransforms();navigation.HasRoute(new Vector2(35,-20),new Vector2(36,-20));
            var cells=(bool[])typeof(FarmRaidNavigation).GetField("blocked",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(navigation);
            const int width=177;
            for(int y=-7;y<=7;y++)for(int x=-7;x<=7;x++)
            {
                Vector2 point=origin+new Vector2(x*.5f,y*.5f);bool expected=false;
                foreach(var hit in Physics2D.OverlapCircleAll(point,.24f))
                {
                    if(hit.isTrigger||hit.GetComponentInParent<EnemyAIBase>()!=null||hit.GetComponentInParent<PlayerInventory>()!=null||
                        hit.GetComponentInParent<VillageResidentHealth>()!=null)continue;
                    expected=true;break;
                }
                int index=Mathf.RoundToInt((point.y+26)*2)*width+Mathf.RoundToInt((point.x+44)*2);
                Assert.That(cells[index],Is.EqualTo(expected),"Circular clearance at "+point);
            }
        }
    }
}
