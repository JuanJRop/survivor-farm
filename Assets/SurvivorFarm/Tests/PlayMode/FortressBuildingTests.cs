using System.Collections;
using System.Collections.Generic;
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
    public sealed class FortressGeometryTests
    {
        [Test] public void HouseholdUtilitiesKeepTheirOriginalArtAndShareAPlacementGrid()
        {
            foreach(var kind in new[]{"Campfire","Chest","Workbench","Bed","Furnace"})
            {
                Assert.That(BackpackActions.IsBuilding(kind),Is.True);
                Assert.That(ConstructionSystem.SpriteFor(kind),Is.Not.Null,kind);
                Assert.That(FortressPieces.Snap(kind,new Vector2(.7f,.7f),0),Is.EqualTo(new Vector2(.5f,.5f)));
            }
            Assert.That(FortressPieces.Overlaps("Chest",Vector2.zero,0,true,"Bed",Vector2.zero,0,true),Is.True);
            Assert.That(FortressPieces.Overlaps("Chest",Vector2.zero,0,true,"Bed",Vector2.right,0,true),Is.False);
        }
        [Test] public void CameraFramingNeverShowsBeyondTheOuterTerrain()
        {
            var root=new GameObject("Framing fixture");var camera=root.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=6.8f;camera.aspect=16f/9;
            var p=FarmExploration.FrameCamera(new Vector3(FarmExploration.HalfWidth,FarmExploration.HalfHeight,-10),camera);
            Assert.That(p.x+camera.orthographicSize*camera.aspect,Is.LessThanOrEqualTo(FarmExploration.HalfWidth+1.001f));
            Assert.That(p.y+camera.orthographicSize,Is.LessThanOrEqualTo(FarmExploration.HalfHeight+1.001f));Object.DestroyImmediate(root);
        }
    }
    public sealed class FortressBuildingTests
    {
        private PortfolioSession session;
        [UnitySetUp] public IEnumerator OpenMain()
        {
            SceneManager.LoadScene("Main");
            for(int i=0;i<100;i++){yield return null;session=PortfolioSession.Instance;if(session!=null&&session.IsReady)break;}
            Assert.That(session,Is.Not.Null);Assert.That(session.IsReady,Is.True);session.BeginNewGame();
        }
        [UnityTearDown] public IEnumerator CloseMain()
        {
            Time.timeScale=1;var main=SceneManager.GetSceneByName("Main");
            var empty=SceneManager.CreateScene("AfterFortressCheck");SceneManager.SetActiveScene(empty);
            if(main.IsValid())yield return SceneManager.UnloadSceneAsync(main);
        }
        [UnityTest] public IEnumerator CampaignHasNoDefensiveConstructionBeforeOrAfterLoading()
        {
            var player=session.Player;var build=player.GetComponent<ConstructionSystem>();
            int wood=player.Wood,stone=player.Stone;
            foreach(string kind in FortressPieces.Palette)
            {
                Assert.That(session.CanBuild(kind),Is.False);
                Assert.That(PortfolioSession.IsDemoRecipe(kind),Is.False);
                build.Begin(kind);Assert.That(ConstructionSystem.IsPlacing,Is.False);
                Assert.That(build.Pack(kind),Is.False);
                Assert.That(build.Place(kind,new Vector2(1,-12)),Is.False);
            }
            Assert.That(player.Wood,Is.EqualTo(wood));Assert.That(player.Stone,Is.EqualTo(stone));
            Assert.That(build.Buildings.Any(b=>BackpackActions.IsRetiredDefense(b.kind)),Is.False);
            Assert.That(FarmDefense.All.Any(d=>!d.IsCore),Is.False);
            var save=Object.FindFirstObjectByType<GameSaveSystem>();save.SaveGame(false);
            Assert.That(save.TryLoadGame(),Is.True,save.LastSaveError);
            Assert.That(build.Buildings.Any(b=>BackpackActions.IsRetiredDefense(b.kind)),Is.False);
            Assert.That(FarmDefense.All.Any(d=>!d.IsCore),Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator LegacyDefenseInventoryAndStructuresCannotReturnThroughSaveReload()
        {
            var player=session.Player;var build=player.GetComponent<ConstructionSystem>();
            // Inject legacy records into an otherwise real save, bypassing the new
            // creation guards to exercise the load migration rather than the UI.
            foreach(string kind in FortressPieces.Palette)
            {
                build.Buildings.Add(new BuildingData{kind=kind,x=1,y=-12,health=12});
                player.PackedBuildings.Add(new PackedBuilding{kind=kind,count=3});
            }
            var fire=build.Buildings.Single(b=>b.kind=="Campfire");
            var save=Object.FindFirstObjectByType<GameSaveSystem>();save.SaveGame(false);
            Assert.That(save.TryLoadGame(),Is.True,save.LastSaveError);
            Assert.That(build.Buildings.Any(b=>BackpackActions.IsRetiredDefense(b.kind)),Is.False);
            Assert.That(player.PackedBuildings.Any(p=>BackpackActions.IsRetiredDefense(p.kind)),Is.False);
            Assert.That(build.Buildings.Single(b=>b.kind=="Campfire").x,Is.EqualTo(fire.x));
            Assert.That(FarmDefense.All.Any(d=>!d.IsCore),Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator OuterVeinsAreReachableAndPreciousMinePaysOncePerDay()
        {
            var player=session.Player;var veins=Object.FindObjectsByType<IronVein>(FindObjectsSortMode.None);
            Assert.That(veins.Length,Is.EqualTo(4));
            Physics2D.SyncTransforms();var reachable=ReachableCells(player.transform.position);
            foreach(var vein in veins)
            {
                Assert.That(FarmExploration.Contains(vein.transform.position,1),Is.True);
                Assert.That(reachable.Any(p=>Vector2.Distance(p,vein.transform.position)<1.35f),Is.True,"No walking route to "+vein.name+" "+vein.transform.position);
            }
            var rare=veins.First(v=>v.Precious);player.GetComponent<ValleyCampaign>().Teleport(rare.transform.position+Vector3.down);
            player.GetComponent<PlayerToolUpgradeController>().Restore(1,3,1);
            player.GetComponent<PlayerToolbelt>().Select(FarmTool.Pickaxe);
            int iron=player.GetComponent<AdventureProgress>().Data.iron;
            rare.Interact(FarmTool.Pickaxe,player);Assert.That(rare.IsMining,Is.True);
            yield return new WaitForSeconds(3.1f);
            Assert.That(player.GetAvailableItemCount("GoldOre"),Is.EqualTo(2));Assert.That(player.GetComponent<AdventureProgress>().Data.iron,Is.EqualTo(iron+3));
            rare.Interact(FarmTool.Pickaxe,player);Assert.That(rare.IsMining,Is.False);Assert.That(player.GetAvailableItemCount("GoldOre"),Is.EqualTo(2));
        }
        // Foot-sized physics occupancy flood: validates actual scenery, not just the numeric map bounds.
        private static HashSet<Vector2> ReachableCells(Vector2 origin)
        {
            var seen=new HashSet<Vector2Int>();var reached=new HashSet<Vector2>();var queue=new Queue<Vector2Int>();
            var start=Vector2Int.RoundToInt(origin*2);queue.Enqueue(start);seen.Add(start);
            Vector2Int[] directions={Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
            while(queue.Count>0)
            {
                var cell=queue.Dequeue();Vector2 p=(Vector2)cell*.5f;reached.Add(p);
                foreach(var direction in directions)
                {
                    var next=cell+direction;Vector2 position=(Vector2)next*.5f;
                    if(!seen.Add(next)||!FarmExploration.Contains(position,.4f))continue;
                    if(Physics2D.OverlapCircleAll(position,.27f).Any(c=>!c.isTrigger&&c.GetComponentInParent<PlayerInventory>()==null))continue;
                    queue.Enqueue(next);
                }
            }
            return reached;
        }
    }
}
