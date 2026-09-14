using System.Reflection;
using NUnit.Framework;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SurvivorFarm.Tests
{
    public sealed class WildResourceRegrowthTests
    {
        GameObject root;
        PlayerInventory player;
        Tilemap ground, paths;
        Tile tile;
        Random.State random;
        GameObject Child(string name) { var go = new GameObject(name); go.transform.SetParent(root.transform); return go; }
        [SetUp]
        public void SetUp()
        {
            random = Random.state; Random.InitState(9143);
            root = new GameObject("Regrowth tests");
            player = Child("Player").AddComponent<PlayerInventory>();
            var grid = Child("Grid"); grid.AddComponent<Grid>();
            ground = Child("Spring Grass").AddComponent<Tilemap>(); ground.transform.SetParent(grid.transform);
            paths = Child("Farm Paths").AddComponent<Tilemap>(); paths.transform.SetParent(grid.transform);
            tile = ScriptableObject.CreateInstance<Tile>();
            for (int x = -35; x < 35; x++) for (int y = -22; y < 22; y++) ground.SetTile(new Vector3Int(x, y), tile);
        }
        [TearDown]
        public void TearDown() { Object.DestroyImmediate(root); Object.DestroyImmediate(tile); Random.state = random; }
        ResourceSpawnPoint Resource(bool tree, Vector3 position)
        {
            var go = Child(tree ? "Tree" : "Rock"); go.transform.position = position;
            var sprite = go.AddComponent<SpriteRenderer>(); go.AddComponent<CircleCollider2D>().radius = .3f;
            HarvestableResource resource = tree ? go.AddComponent<TreeResource>() : go.AddComponent<RockResource>();
            resource.Configure(sprite, null, 2, 0);
            return ResourceSpawnPoint.Attach(resource);
        }
        WildResourceRegrowth World()
        {
            var world = player.gameObject.AddComponent<WildResourceRegrowth>(); world.Initialize(); Physics2D.SyncTransforms(); return world;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DepletionWaitsThenRelocatesTheSameSharedResource(bool tree)
        {
            var point = Resource(tree, new Vector3(-18, -12)); var world = World();
            point.EnableRegrowth(10, 10);
            var instance = point.Instance; var definition = instance.Definition; string id = point.PersistentId;
            Vector3 oldPosition = instance.transform.position;
            instance.Interact(tree ? FarmTool.Axe : FarmTool.Pickaxe, player);
            world.Tick(9); Assert.IsTrue(instance.IsHarvested);
            world.Tick(1); Assert.IsFalse(instance.IsHarvested);
            Assert.AreSame(instance, point.Instance); Assert.AreSame(definition, instance.Definition);
            Assert.GreaterOrEqual(Vector2.Distance(oldPosition, instance.transform.position), 1);
            Assert.LessOrEqual(Vector2.Distance(point.transform.position, instance.transform.position), 6.1f);
            Assert.AreEqual(id, point.PersistentId); Assert.AreEqual(1, world.PoolCount);
            Assert.AreEqual(2, tree ? player.Wood : player.Stone, "Regrowth itself must not grant resources.");
        }

        [Test]
        public void RoadsVillageCenterWaterPlayerAndSolidObjectsAreExcluded()
        {
            var point = Resource(true, new Vector3(-10, -5)); var world = World();
            Vector3 candidate = new Vector3(-15, -5);
            Assert.IsTrue(world.CanGrowAt(point, candidate));
            Assert.IsFalse(world.CanGrowAt(point, new Vector3(-6, -5)));
            paths.SetTile(paths.WorldToCell(candidate), tile);
            Assert.IsFalse(world.CanGrowAt(point, candidate)); paths.ClearAllTiles();
            ground.SetTile(ground.WorldToCell(candidate), null);
            Assert.IsFalse(world.CanGrowAt(point, candidate)); ground.SetTile(ground.WorldToCell(candidate), tile);
            player.transform.position = candidate;
            Assert.IsFalse(world.CanGrowAt(point, candidate)); player.transform.position = Vector3.zero;
            var obstacle = Child("Building"); obstacle.transform.position = candidate; obstacle.AddComponent<BoxCollider2D>(); Physics2D.SyncTransforms();
            Assert.IsFalse(world.CanGrowAt(point, candidate));
        }

        [Test]
        public void BlockedSitesRetryWithoutCreatingObjectsOrOverlapping()
        {
            var point = Resource(false, new Vector3(-18, -12)); var world = World();
            point.Instance.Interact(FarmTool.Pickaxe, player);
            var obstacle = Child("Blocked area"); obstacle.transform.position = point.transform.position; obstacle.AddComponent<BoxCollider2D>().size = Vector2.one * 16;
            var instance = point.Instance;
            world.Tick(200);
            Assert.IsTrue(instance.IsHarvested); Assert.AreEqual(5, point.RegrowthRemaining);
            Object.DestroyImmediate(obstacle);
            world.Tick(5);
            Assert.IsFalse(instance.IsHarvested); Assert.AreSame(instance, point.Instance);
            Assert.AreEqual(1, root.GetComponentsInChildren<HarvestableResource>(true).Length);
        }

        [Test]
        public void SaveRoundtripPreservesRandomPositionPendingTimerAndStableIdentity()
        {
            var tree = Resource(true, new Vector3(-18, -12)); var rock = Resource(false, new Vector3(18, -12)); var world = World();
            tree.RespawnAt(new Vector3(-16, -12));
            rock.EnableRegrowth(60, 60); rock.Instance.Interact(FarmTool.Pickaxe, player); world.Tick(17);
            var save = Child("Save").AddComponent<GameSaveSystem>(); save.Configure(player.transform, player, null, null, null, null, null, null, null, null);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var state = typeof(GameSaveSystem).GetMethod("BuildSaveData", flags).Invoke(save, null);
            state = JsonUtility.FromJson(JsonUtility.ToJson(state), state.GetType());
            tree.Respawn(); rock.Respawn();
            typeof(GameSaveSystem).GetMethod("RestoreSaveData", flags).Invoke(save, new[] { state });
            Assert.AreEqual(new Vector3(-16, -12), tree.Instance.transform.position);
            Assert.IsTrue(rock.Instance.IsHarvested); Assert.That(rock.RegrowthRemaining, Is.EqualTo(43).Within(.001f));
            world.Tick(42); Assert.IsTrue(rock.Instance.IsHarvested);
            world.Tick(1.01f); Assert.IsFalse(rock.Instance.IsHarvested);
        }

        [Test]
        public void OldAndInvalidRegrowthDataKeepTheAuthoredAnchor()
        {
            var point = Resource(true, new Vector3(-18, -12)); World();
            point.RespawnAt(new Vector3(-16, -12)); point.RestoreRegrowth(null);
            Assert.AreEqual(point.transform.position, point.Instance.transform.position);
            point.RestoreRegrowth(new ResourceRegrowthState { position = new Vector3(float.NaN, 4), remaining = 20 });
            Assert.AreEqual(point.transform.position, point.Instance.transform.position);
            point.RestoreRegrowth(new ResourceRegrowthState { position = Vector3.zero, remaining = 20 });
            Assert.AreEqual(point.transform.position, point.Instance.transform.position);
        }

        [Test]
        public void ExistingNearbyResourcesReserveTheirSpaceAndThePoolIsBounded()
        {
            var first = Resource(true, new Vector3(-18, -12)); var second = Resource(false, new Vector3(-15, -12)); var world = World();
            Assert.IsFalse(world.CanGrowAt(first, second.Instance.transform.position));
            world.Initialize(); world.Initialize(); Assert.AreEqual(2, world.PoolCount);
            first.Instance.Interact(FarmTool.Axe, player);
            ground.gameObject.SetActive(false); world.Tick(300);
            Assert.IsTrue(first.Instance.IsHarvested);
            Assert.AreEqual(0, first.RegrowthRemaining, "Time should still pass while the outdoor map is hidden.");
            ground.gameObject.SetActive(true); world.Tick(0);
            Assert.IsFalse(first.Instance.IsHarvested);
            Assert.Greater(Vector2.Distance(first.Instance.transform.position, second.Instance.transform.position), 1.7f);
        }
    }
}
