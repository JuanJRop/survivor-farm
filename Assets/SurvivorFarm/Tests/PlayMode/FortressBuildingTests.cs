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
        [TestCase(0,1,0)] [TestCase(1,0,1)]
        public void ModulesSnapToSharedEdges(int rotation,float x,float y)
        {Assert.That(FortressPieces.Snap("StoneWall",new Vector2(.7f,.7f),rotation),Is.EqualTo(new Vector2(x,y)));}
        [Test] public void AdjacentStraightAndCornerModulesJoinWithoutOccupyingEachOther()
        {
            Assert.That(FortressPieces.Overlaps("Fence",new Vector2(1,0),0,true,"StoneWall",new Vector2(3,0),0,true),Is.False);
            Assert.That(FortressPieces.Overlaps("Fence",new Vector2(1,0),0,true,"StoneWall",new Vector2(2,1),1,true),Is.False);
            Assert.That(FortressPieces.Overlaps("Fence",new Vector2(1,0),0,true,"StoneWall",new Vector2(1,0),0,true),Is.True);
        }
        [Test] public void RotationExchangesTheActualCollisionDimensions()
        {
            var a=FortressPieces.Footprint("Fence",0);var b=FortressPieces.Footprint("Fence",1);
            Assert.That(a.x,Is.EqualTo(b.y));Assert.That(a.y,Is.EqualTo(b.x));Assert.That(a.x,Is.GreaterThan(1.9f));
        }
        [Test] public void EveryPalettePieceHasAnIconAndTheStrongerMaterialsHaveMoreHealth()
        {
            foreach(var kind in FortressPieces.Palette){Assert.That(BackpackActions.IsBuilding(kind),Is.True);Assert.That(ConstructionSystem.SpriteFor(kind),Is.Not.Null,kind);}
            Assert.That(FortressPieces.Health("StoneWall"),Is.GreaterThan(FortressPieces.Health("Fence")));
            Assert.That(FortressPieces.Health("ReinforcedWall"),Is.GreaterThan(FortressPieces.Health("StoneWall")));
        }
        [Test] public void CameraFramingNeverShowsBeyondTheOuterTerrain()
        {
            var root=new GameObject("Framing fixture");var camera=root.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=6.8f;camera.aspect=16f/9;
            var p=FarmExploration.FrameCamera(new Vector3(32,18,-10),camera);
            Assert.That(p.x+camera.orthographicSize*camera.aspect,Is.LessThanOrEqualTo(33.001f));
            Assert.That(p.y+camera.orthographicSize,Is.LessThanOrEqualTo(19.001f));Object.DestroyImmediate(root);
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
        [UnityTest] public IEnumerator ContinuousPlacementConsumesPackedThenMaterialsAndPersists()
        {
            var player=session.Player;var build=player.GetComponent<ConstructionSystem>();
            player.GetComponent<ValleyCampaign>().Teleport(new Vector3(0,-10));Physics2D.SyncTransforms();
            player.AddWood(100);player.AddPacked("Fence",2);int wood=player.Wood;
            Assert.That(build.BeginPacked("Fence"),Is.True);
            Assert.That(build.PlaceSelected(new Vector2(1,-12)),Is.True);
            Assert.That(build.PlaceSelected(new Vector2(3,-12)),Is.True);
            Assert.That(ConstructionSystem.IsPlacing,Is.True);Assert.That(player.PackedCount("Fence"),Is.Zero);Assert.That(player.Wood,Is.EqualTo(wood));
            build.Rotate();Assert.That(build.PlaceSelected(new Vector2(4,-11)),Is.True);
            Assert.That(player.Wood,Is.EqualTo(wood-2));
            int count=build.Buildings.Count;
            Assert.That(build.PlaceSelected(new Vector2(4,-11)),Is.False);Assert.That(build.Buildings.Count,Is.EqualTo(count));Assert.That(player.Wood,Is.EqualTo(wood-2));
            build.Cancel();var save=Object.FindFirstObjectByType<GameSaveSystem>();save.SaveGame(false);
            Assert.That(save.TryLoadGame(),Is.True,save.LastSaveError);Assert.That(build.Buildings.Count,Is.EqualTo(count));
            var restored=build.Buildings.Single(b=>b.x==4&&b.y==-11);
            Assert.That(restored.rotation,Is.EqualTo(1));Assert.That(restored.wallVersion,Is.EqualTo(1));
            yield return null;
        }
        [UnityTest] public IEnumerator ReinforcedWallRequiresGoldAndDoesNotSpendOnFailure()
        {
            var player=session.Player;var build=player.GetComponent<ConstructionSystem>();
            player.GetComponent<ValleyCampaign>().Teleport(new Vector3(0,-10));Physics2D.SyncTransforms();
            player.AddWood(100);player.AddStone(100);player.GetComponent<AdventureProgress>().AddIron(20);
            build.Begin("ReinforcedWall");int wood=player.Wood;
            Assert.That(build.AvailablePlacements("ReinforcedWall"),Is.Zero);
            Assert.That(build.PlaceSelected(new Vector2(1,-12)),Is.False);Assert.That(player.Wood,Is.EqualTo(wood));
            player.AddItem("GoldOre",2);Assert.That(build.AvailablePlacements("ReinforcedWall"),Is.EqualTo(2));
            Assert.That(build.PlaceSelected(new Vector2(1,-12)),Is.True);Assert.That(player.GetAvailableItemCount("GoldOre"),Is.EqualTo(1));
            var wall=FarmDefense.All.Single(d=>d.Kind=="ReinforcedWall");Assert.That(wall.Maximum,Is.EqualTo(64));
            build.Cancel();Assert.That(player.GetComponent<ConstructionPalette>().Visible,Is.False);
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
            player.GetComponent<PlayerToolbelt>().Select(FarmTool.Pickaxe);
            int iron=player.GetComponent<AdventureProgress>().Data.iron;
            rare.Interact(FarmTool.Pickaxe,player);Assert.That(rare.IsMining,Is.True);
            yield return new WaitForSeconds(3.1f);
            Assert.That(player.GetAvailableItemCount("GoldOre"),Is.EqualTo(2));Assert.That(player.GetComponent<AdventureProgress>().Data.iron,Is.EqualTo(iron+3));
            rare.Interact(FarmTool.Pickaxe,player);Assert.That(rare.IsMining,Is.False);Assert.That(player.GetAvailableItemCount("GoldOre"),Is.EqualTo(2));
        }
        [UnityTest] public IEnumerator UpgradeKeepsPositionDamageRatioAndSavesNewMaterial()
        {
            var player=session.Player;var build=player.GetComponent<ConstructionSystem>();
            player.GetComponent<ValleyCampaign>().Teleport(new Vector3(0,-10));Physics2D.SyncTransforms();player.AddWood(40);player.AddStone(40);
            build.Begin("Fence");Assert.That(build.PlaceSelected(new Vector2(1,-12)),Is.True);
            var wall=FarmDefense.All.First(d=>d.transform.position==new Vector3(1,-12));wall.TakeDamage(10,null);
            int count=build.Buildings.Count;build.Begin("StoneWall");Assert.That(build.PlaceSelected(new Vector2(1,-12)),Is.True);
            Assert.That(build.Buildings.Count,Is.EqualTo(count));
            wall=FarmDefense.All.First(d=>d.Kind=="StoneWall");Assert.That(wall.Health,Is.EqualTo(18));
            Assert.That(ConstructionSystem.IsPlacing,Is.True);build.Cancel();
            var save=Object.FindFirstObjectByType<GameSaveSystem>();save.SaveGame(false);Assert.That(save.TryLoadGame(),Is.True);
            wall=FarmDefense.All.First(d=>d.Kind=="StoneWall");Assert.That(wall.Health,Is.EqualTo(18));
            Assert.That(wall.transform.position,Is.EqualTo(new Vector3(1,-12)));
            yield return null;
        }
        [UnityTest] public IEnumerator BruteCanDamageTheEndOfALargeWall()
        {
            var player=session.Player;var build=player.GetComponent<ConstructionSystem>();player.AddWood(10);
            player.GetComponent<ValleyCampaign>().Teleport(new Vector3(0,-10));Physics2D.SyncTransforms();
            build.Begin("Fence");Assert.That(build.PlaceSelected(new Vector2(1,-12)),Is.True);build.Cancel();
            var wall=FarmDefense.All.First(d=>d.transform.position==new Vector3(1,-12));int hp=wall.Health;
            session.PrepareNow();session.Advance(26);
            var enemy=session.Raids.Enemies[0];enemy.ConfigureRaid(session,RaidRole.Brute,null);enemy.ActivateFromPool(new Vector3(-.35f,-12));Physics2D.SyncTransforms();
            // This lies outside the old centre-based .95 range but directly at the wall's exposed end.
            Assert.That(Vector2.Distance(enemy.transform.position,wall.transform.position),Is.GreaterThan(.95f));
            yield return new WaitForSeconds(1.6f);
            Assert.That(wall.Health,Is.LessThan(hp),"The large wall must not make an invulnerable end-cap.");
            enemy.ReturnToPool();
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
