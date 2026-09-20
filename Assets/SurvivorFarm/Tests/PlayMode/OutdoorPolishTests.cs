using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    public sealed class OutdoorPolishTests
    {
        [UnityTearDown] public IEnumerator Teardown()
        {
            Time.timeScale=1;
            var main=SceneManager.GetSceneByName("Main");
            if(main.IsValid()&&main.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("AfterOutdoorPolish"));
                yield return SceneManager.UnloadSceneAsync(main);
            }
        }

        [Test] public void EveryPathCapHasGrassCornersAndAnUnbrokenDirtCentre()
        {
            var root=new GameObject("Path corner regression");
            var readable=new Texture2D(2,2);
            try
            {
                readable.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath,"SurvivorFarm/Resources/StoryArt/TerrainAtlas.png")));
                root.AddComponent<ValleyTerrain>().Paint(new HashSet<Vector2Int>{Vector2Int.zero},Vector3.zero,Vector3.right*.5f,Vector3.up*.5f,0);
                var quarters=root.GetComponentsInChildren<SpriteRenderer>();
                Assert.That(quarters.Length,Is.EqualTo(4));
                foreach(var quarter in quarters)
                {
                    bool right=quarter.transform.position.x>.25f,top=quarter.transform.position.y>.25f;
                    var rect=quarter.sprite.rect;
                    var corner=readable.GetPixel((int)(right?rect.xMax-1:rect.xMin),(int)(top?rect.yMax-1:rect.yMin));
                    var centre=readable.GetPixel((int)(right?rect.xMin:rect.xMax-1),(int)(top?rect.yMin:rect.yMax-1));
                    Assert.That(corner.g,Is.GreaterThan(corner.r),"A convex road cap must meet the surrounding grass, without a protruding dirt square.");
                    Assert.That(centre.r,Is.GreaterThan(centre.g),"The four dirt quadrants must meet at the centre without a grass seam.");
                    Assert.That(quarter.bounds.size.x,Is.EqualTo(.25f).Within(.001f));
                }
            }
            finally { Object.DestroyImmediate(root);Object.DestroyImmediate(readable); }
        }

        [UnityTest] public IEnumerator BridgeBlocksBothRailsAndLeavesTheDeckOpen()
        {
            SceneManager.LoadScene("Main");
            PortfolioSession session=null;
            for(int i=0;i<120;i++){yield return null;session=PortfolioSession.Instance;if(session!=null&&session.IsReady)break;}
            Assert.That(session!=null&&session.IsReady,Is.True);
            session.BeginNewGame();yield return null;
            var bridge=Object.FindFirstObjectByType<RepairableBridge>(FindObjectsInactive.Include);
            var art=Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Single(s=>s.name=="Puente de tablones y barandas del pack");
            var rails=art.GetComponentsInChildren<BoxCollider2D>();
            Assert.That(rails.Length,Is.EqualTo(2));Physics2D.SyncTransforms();
            Vector2 centre=bridge.transform.position;
            Assert.That(rails.All(r=>!r.isTrigger&&!r.OverlapPoint(centre)),Is.True);
            var along=Physics2D.CircleCastAll(centre+Vector2.down*1.7f,.16f,Vector2.up,3.4f);
            Assert.That(along.Any(hit=>rails.Contains(hit.collider)),Is.False,"The player's feet can cross along the complete central deck.");
            foreach(int side in new[]{-1,1})
            {
                var across=Physics2D.CircleCastAll(centre,.16f,Vector2.right*side,2f);
                Assert.That(across.Any(hit=>rails.Contains(hit.collider)),Is.True,"The player cannot walk off either side onto a rail.");
            }
        }
    }
}
