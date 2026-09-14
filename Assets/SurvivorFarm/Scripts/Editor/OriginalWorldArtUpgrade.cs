using System;
using System.IO;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    [InitializeOnLoad]
    public static class OriginalWorldArtUpgrade
    {
        public const string Root = "Assets/SurvivorFarm/Art/WorldSprites/";
        private const string Request = "Library/ApplyOriginalWorld.request";
        [Serializable] private class Entry { public string name; public float ppu, pivot; }
        [Serializable] private class Manifest { public Entry[] entries; }
        static OriginalWorldArtUpgrade() => EditorApplication.update += Tick;
        private static void Tick()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            try
            {
                File.Delete(Request); Apply();
                File.WriteAllText("Library/OriginalWorld-result.txt", "PASS: Original terrain, buildings, resources and animated animals saved in Main.");
            }
            catch (Exception e) { File.WriteAllText("Library/OriginalWorld-result.txt", e.ToString()); Debug.LogException(e); }
        }

        [MenuItem("Survivor Farm/Apply Original World Graphics")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != "Assets/SurvivorFarm/Scenes/Main.unity")
                throw new InvalidOperationException("Open Main outside Play mode.");
            foreach (var item in JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "manifest.json")).entries)
            {
                string path = Root + item.name + ".png";
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = item.ppu; importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = new Vector2(.5f, item.pivot);
                settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
            }
            // Update prefab sources as well as placed instances, so respawns and pools retain the art.
            foreach (string path in new[] { "Tree", "Rock", "Animal", "Deer", "FarmingPlot", "OutdoorEnemy", "PlayerBaseHouse" }
                .Select(n => "Assets/SurvivorFarm/Prefabs/World/" + n + ".prefab").Where(File.Exists))
            {
                var prefab = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    StyleResources(prefab); StyleEnemies(prefab);
                    var baseHouse = prefab.GetComponent<BaseHouse>();
                    if (baseHouse != null)
                    {
                        StyleBuilding(prefab, "House", 1);
                        baseHouse.Configure(prefab.transform.Find("Original Building Visual").GetComponent<SpriteRenderer>());
                    }
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) { StyleResources(root); StyleEnemies(root); }
            var house = Find<BaseHouse>();
            var outside = house.transform.parent;
            house.transform.position = new Vector3(0, 2.2f, 0);
            if (outside.Find("Original World Graphics") == null) CreateTerrain(outside);
            var graphics = outside.Find("Original World Graphics");
            PaintTerrain(graphics);
            var middleChicken = graphics.Find("Chicken 2");
            if (middleChicken != null) middleChicken.position = new Vector3(-1.8f, 1f, 0);
            foreach (string name in new[] { "Well", "Shipping Chest" })
            {
                var prop = graphics.Find(name); if (prop == null) continue;
                var footprint = Ensure<BoxCollider2D>(prop.gameObject);
                footprint.size = new Vector2(.65f, .4f); footprint.offset = new Vector2(0, .1f);
            }
            StyleBuilding(house.gameObject, "House", 1f);
            house.Configure(house.transform.Find("Original Building Visual").GetComponent<SpriteRenderer>());
            var shop = Find<ShopEntrance>(); StyleBuilding(shop.gameObject, "Shop", .85f);
            foreach (var rock in FindAll<RockResource>())
            {
                Vector3 pos = rock.transform.position;
                if (pos.x > 6.3f && pos.x < 10f && pos.y > -1.8f && pos.y < -.7f)
                {
                    Vector3 destination = new Vector3(pos.x < 8 ? 5.7f : 10.5f, -2.2f, pos.z);
                    var spawn = rock.GetComponentInParent<ResourceSpawnPoint>();
                    if (spawn != null) spawn.transform.position += destination - pos;
                    else rock.transform.position = destination;
                }
            }
            foreach (var bed in FindAll<PlayerBed>()) bed.transform.position = new Vector3(1.7f, 1.7f, 0);
            foreach (var actor in FindAll<PlayerMovementController>())
                foreach (var renderer in actor.GetComponentsInChildren<SpriteRenderer>()) Depth(renderer, -.4f);
            StyleShopInterior();
            // Keep the established gameplay locations and colliders; the art is aligned to them.
            foreach (var line in FindAll<HomeSafeZone>().SelectMany(z => z.GetComponents<LineRenderer>()))
            { line.startColor = line.endColor = new Color(.85f, 1f, .62f, .18f); line.startWidth = line.endWidth = .018f; }
            foreach (var zone in FindAll<LandUnlockZone>())
            {
                var point = new SerializedObject(zone).FindProperty("interactionPoint").objectReferenceValue as Transform;
                var marker = point != null ? point.GetComponentInChildren<SpriteRenderer>(true) : null;
                if (marker != null)
                { marker.sprite = S("Sign"); marker.color = Color.white; marker.transform.localScale = Vector3.one * .75f; }
            }
            foreach (var component in FindAll<Component>())
            {
                if (component == null) continue;
                EditorUtility.SetDirty(component);
                if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        }

        private static void StyleResources(GameObject root)
        {
            foreach (var tree in root.GetComponentsInChildren<TreeResource>(true))
            {
                var renderer = tree.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.name == "Canopy") ?? tree.GetComponentInChildren<SpriteRenderer>(true);
                foreach (var old in tree.GetComponentsInChildren<SpriteRenderer>(true)) if (old != renderer) old.sprite = null;
                renderer.sprite = S("Tree"); renderer.color = Color.white; renderer.transform.localPosition = new Vector3(0, -.55f, 0); renderer.transform.localScale = Vector3.one;
                Depth(renderer);
            }
            foreach (var rock in root.GetComponentsInChildren<RockResource>(true))
            {
                var renderer = rock.GetComponent<SpriteRenderer>(); renderer.sprite = S("Rock"); renderer.color = Color.white; Depth(renderer);
            }
            foreach (var plot in root.GetComponentsInChildren<FarmingPlot>(true))
                plot.ConfigureArt(S("Soil"), S("WetSoil"), Enumerable.Range(0, 5).Select(i => S("Crop" + i)).ToArray());
            foreach (var animal in root.GetComponentsInChildren<AnimalResource>(true))
            {
                var renderer = animal.GetComponentsInChildren<SpriteRenderer>(true).First();
                renderer.color = Color.white;
                // Preserve the existing collider scale; normalize only the art to a natural body size.
                var visual = renderer.transform == animal.transform ? Child(animal.transform, "Original Animal Visual") : renderer.transform;
                if (renderer.transform == animal.transform)
                {
                    renderer.sprite = null;
                    renderer = Ensure<SpriteRenderer>(visual.gameObject);
                }
                renderer.sprite = S("DeerIdle0_0"); renderer.color = Color.white;
                visual.localScale = new Vector3(1f / animal.transform.lossyScale.x, 1f / animal.transform.lossyScale.y, 1);
                visual.localPosition = Vector3.zero;
                var roaming = Ensure<AnimalRoamingVisual>(animal.gameObject);
                roaming.Visual = renderer; roaming.Idle = Frames("DeerIdle", 2); roaming.Walk = Frames("DeerWalk", 4);
                roaming.IdleFrames = 2; roaming.WalkFrames = 4;
                var data = new SerializedObject(animal);
                data.FindProperty("bodyRenderer").objectReferenceValue = renderer;
                data.FindProperty("mainRenderer").objectReferenceValue = renderer;
                data.ApplyModifiedPropertiesWithoutUndo(); Depth(renderer);
            }
        }

        private static void StyleEnemies(GameObject root)
        {
            foreach (var enemy in root.GetComponentsInChildren<EnemyAIBase>(true))
            {
                var data = new SerializedObject(enemy);
                var body = data.FindProperty("bodyRenderer").objectReferenceValue as SpriteRenderer;
                if (body == null) body = enemy.GetComponent<SpriteRenderer>();
                if (body == null) continue;
                var visual = Child(enemy.transform, "Original Enemy Visual");
                var renderer = Ensure<SpriteRenderer>(visual.gameObject);
                body.sprite = null; renderer.sprite = S("SlimeIdle0"); renderer.color = Color.white;
                visual.localScale = new Vector3(.8f / enemy.transform.lossyScale.x, .8f / enemy.transform.lossyScale.y, 1);
                data.FindProperty("bodyRenderer").objectReferenceValue = renderer; data.FindProperty("bodyColor").colorValue = Color.white; data.ApplyModifiedPropertiesWithoutUndo();
                var animation = Ensure<MovementSpriteAnimation>(enemy.gameObject);
                animation.Visual = renderer; animation.Idle = new[] { S("SlimeIdle0") }; animation.Walk = new[] { S("SlimeWalk0"), S("SlimeWalk1") }; Depth(renderer);
            }
        }

        private static void StyleBuilding(GameObject root, string sprite, float scale)
        {
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true)) renderer.sprite = null;
            var visual = Child(root.transform, "Original Building Visual");
            visual.localPosition = new Vector3(0, -.65f, 0); visual.localScale = Vector3.one * scale;
            var art = Ensure<SpriteRenderer>(visual.gameObject);
            art.sprite = S(sprite); art.color = Color.white; Depth(art);
            // Collision is the footprint, not the high roof. Interaction remains on the original root.
            var collider = root.GetComponent<BoxCollider2D>();
            if (collider != null) { collider.size = new Vector2(sprite == "House" ? 1.8f : 2.6f, 1f); collider.offset = new Vector2(0, -.2f); }
        }

        private static void CreateTerrain(Transform outside)
        {
            var root = Child(outside, "Original World Graphics");
            root.gameObject.AddComponent<Grid>().cellSize = new Vector3(.5f, .5f, 1);
            PaintTerrain(root);
            foreach (var r in outside.GetComponentsInChildren<SpriteRenderer>(true))
                if (r.name == "Farm Ground" || r.name == "Starting Unlocked Square") r.enabled = false;
            var random = new System.Random(22);
            for (int i = 0; i < 170; i++)
            {
                float x = (float)random.NextDouble() * 24 - 12, y = (float)random.NextDouble() * 24 - 12;
                if (Mathf.Abs(x) < 1 || y > -.2f && y < 1.7f) continue;
                var tuft = Prop(root, "Grass Tuft", "Tuft", new Vector2(x, y), 1); tuft.sortingOrder = -80;
            }
            Prop(root, "Well", "Well", new Vector2(2.4f, -.6f), 1);
            Prop(root, "Shipping Chest", "Chest", new Vector2(-1.65f, -1.9f), 1);
            for (int i = 0; i < 3; i++)
            {
                var chicken = Prop(root, "Chicken " + (i + 1), "Chicken0_0", new Vector2(-1.8f + i * 1.6f, i == 1 ? 2.5f : -.3f), 1);
                var roaming = chicken.gameObject.AddComponent<AnimalRoamingVisual>();
                roaming.Visual = chicken; roaming.Idle = Frames("Chicken", 4); roaming.Walk = Frames("Chicken", 4);
                roaming.IdleFrames = roaming.WalkFrames = 4; roaming.Speed = .32f; roaming.Radius = .7f; roaming.FootRadius = .1f;
            }
        }

        private static void PaintTerrain(Transform root)
        {
            var terrain = NewTilemap(root, "Spring Grass", -100);
            var paths = NewTilemap(root, "Farm Paths", -90);
            terrain.ClearAllTiles(); paths.ClearAllTiles();
            var grass = Tile("Grass");
            for (int y = -25; y < 25; y++) for (int x = -25; x < 25; x++) terrain.SetTile(new Vector3Int(x, y, 0), grass);
            for (int y = -24; y < 24; y++)
                for (int x = -1; x <= 1; x++) paths.SetTile(new Vector3Int(x, y, 0), Tile(x == -1 ? "PathLeft" : x == 1 ? "PathRight" : "Path"));
            for (int x = -24; x < 24; x++)
                for (int y = 0; y <= 2; y++) paths.SetTile(new Vector3Int(x, y, 0), Tile(x >= -1 && x <= 1 ? "Path" : y == 0 ? "PathBottom" : y == 2 ? "PathTop" : "Path"));
            terrain.RefreshAllTiles(); paths.RefreshAllTiles();
        }

        private static void StyleShopInterior()
        {
            var transforms = FindAll<Transform>();
            var floor = transforms.FirstOrDefault(t => t.name == "Shop Floor");
            if (floor == null) return;
            floor.GetComponent<SpriteRenderer>().enabled = false;
            var tilesRoot = Child(floor.parent, "Original Shop Floor");
            if (tilesRoot.GetComponent<Grid>() == null) tilesRoot.gameObject.AddComponent<Grid>().cellSize = new Vector3(.5f, .5f, 1);
            var tiles = tilesRoot.GetComponentInChildren<Tilemap>();
            if (tiles == null) tiles = NewTilemap(tilesRoot, "Wood Floor", -80);
            for (int x = -8; x < 8; x++) for (int y = -8; y < 8; y++) tiles.SetTile(new Vector3Int(x, y, 0), Tile("Floor"));
            foreach (var pair in new[] { ("Shop Counter", "Counter", 1.8f), ("Shop Clerk", "Clerk", 1f), ("Shop Exit", "Sign", 1f) })
            {
                var target = transforms.FirstOrDefault(t => t.name == pair.Item1); if (target == null) continue;
                var renderer = target.GetComponent<SpriteRenderer>(); renderer.sprite = S(pair.Item2); renderer.color = Color.white;
                target.localScale = Vector3.one * pair.Item3; Depth(renderer);
                if (pair.Item1 == "Shop Counter") target.localScale = new Vector3(1.8f, .8f, 1);
            }
        }

        public static Sprite S(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Root + name + ".png");
        private static Sprite[] Frames(string prefix, int columns) => Enumerable.Range(0, 3).SelectMany(r => Enumerable.Range(0, columns).Select(c => S(prefix + r + "_" + c))).ToArray();
        private static T[] FindAll<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        private static T Find<T>() where T : Component => FindAll<T>().First();
        private static Transform Child(Transform parent, string name)
        {
            var child = parent.Find(name); if (child != null) return child;
            child = new GameObject(name).transform; child.SetParent(parent, false); return child;
        }
        private static void Depth(SpriteRenderer renderer, float offset = 0)
        {
            var order = Ensure<WorldSpriteDepth>(renderer.gameObject);
            order.Visual = renderer; order.GroundOffset = offset;
        }
        private static SpriteRenderer Prop(Transform parent, string name, string sprite, Vector2 position, float scale)
        {
            var child = new GameObject(name).transform; child.SetParent(parent, false); child.position = new Vector3(position.x, position.y, 0); child.localScale = Vector3.one * scale;
            var renderer = child.gameObject.AddComponent<SpriteRenderer>(); renderer.sprite = S(sprite); Depth(renderer); return renderer;
        }
        private static Tilemap NewTilemap(Transform parent, string name, int order)
        {
            var child = Child(parent, name); var map = Ensure<Tilemap>(child.gameObject);
            Ensure<TilemapRenderer>(child.gameObject).sortingOrder = order; return map;
        }
        private static T Ensure<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            return component != null ? component : root.AddComponent<T>();
        }
        private static Tile Tile(string name)
        {
            string path = Root + name + "Tile.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path); if (tile != null) return tile;
            tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = S(name); tile.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, path); return tile;
        }
    }
}
