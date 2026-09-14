using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivorFarm.Editor
{
    public static class OpenExplorationUpgrade
    {
        private const string Pack = "Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/";

        [MenuItem("Survivor Farm/Apply Open Exploration")]
        public static void ApplyMain()
        {
            EditorSceneManager.OpenScene("Assets/SurvivorFarm/Scenes/Main.unity");
            var outside = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(t => t.name == "Outdoor World");
            var existing = outside.Find("Open Exploration");
            if (existing != null)
            {
                AddBanks(existing);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();
                return;
            }
            var root = new GameObject("Open Exploration").transform;
            root.SetParent(outside, false);
            var river = new GameObject("Northern River").transform;
            river.SetParent(root, false);
            var water = AssetDatabase.LoadAllAssetsAtPath(Pack + "Farm and Tileset/Tileset/Water tile.png").OfType<Sprite>().First();
            for (int x = -37; x < 37; x++)
                for (int y = 6; y < 8; y++)
                    Art(river, "River Water", water, new Vector3(x + .5f, y + .5f), Vector2.one, -60);
            Solid(river, "West River Bank", new Vector2(-19.05f, 7), new Vector2(35.9f, 2));
            Solid(river, "East River Bank", new Vector2(19.05f, 7), new Vector2(35.9f, 2));

            var bridgeObject = new GameObject("Repairable Northern Bridge");
            bridgeObject.transform.SetParent(root, false);
            bridgeObject.transform.position = new Vector3(0, 7, 0);
            var bridge = bridgeObject.AddComponent<RepairableBridge>();
            var blocker = Solid(bridge.transform, "Broken Crossing", new Vector2(0, 7), new Vector2(2.2f, 2));
            var sprite = AssetDatabase.LoadAllAssetsAtPath(Pack + "Exterior/Fence and Bridge/Bridge.png").OfType<Sprite>().First(s => s.name == "Bridge_1");
            var deck = Art(bridge.transform, "Repaired Deck", sprite, new Vector3(0, 7), new Vector2(2.2f, 2.75f), -55);
            var broken = new GameObject("Broken Deck");
            broken.transform.SetParent(bridge.transform, false);
            var south = Slice(sprite.texture, "BrokenBridgeSouth", new Rect(0, 3, 49, 18));
            var north = Slice(sprite.texture, "BrokenBridgeNorth", new Rect(0, 46, 49, 18));
            Art(broken.transform, "South Remnant", south, new Vector3(0, 5.95f), new Vector2(2.2f, .8f), -55);
            Art(broken.transform, "North Remnant", north, new Vector3(0, 8.05f), new Vector2(2.2f, .8f), -55);
            var point = new GameObject("Repair Interaction Point").transform;
            point.SetParent(bridge.transform, false);
            point.position = new Vector3(0, 5.65f, 0);
            bridge.Configure(blocker, deck, broken, point);
            PrefabUtility.SaveAsPrefabAssetAndConnect(bridgeObject, "Assets/SurvivorFarm/Prefabs/World/RepairableBridge.prefab", InteractionMode.AutomatedAction);

            foreach (var zone in outside.GetComponentsInChildren<LandUnlockZone>(true))
            {
                zone.ConfigureExploration(zone.name.StartsWith("North") ? bridge : null);
                EditorUtility.SetDirty(zone);
                if (PrefabUtility.IsPartOfPrefabInstance(zone)) PrefabUtility.RecordPrefabInstancePropertyModifications(zone);
            }
            var start = outside.Find("Starting Unlocked Square");
            if (start != null) start.gameObject.SetActive(false);
            // The finite farm keeps its existing size; these edges prevent walking around the river.
            Solid(root, "West World Edge", new Vector2(-37, 0), new Vector2(1, 45));
            Solid(root, "East World Edge", new Vector2(37, 0), new Vector2(1, 45));
            Solid(root, "South World Edge", new Vector2(0, -22), new Vector2(75, 1));
            Solid(root, "North World Edge", new Vector2(0, 22), new Vector2(75, 1));
            AddBanks(root);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Open exploration saved: free southern region, river and repairable northern bridge.");
        }

        private static Sprite Slice(Texture2D texture, string name, Rect rect)
        {
            string path = "Assets/SurvivorFarm/Art/WorldSprites/" + name + ".asset";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                if (sprite.rect != rect)
                {
                    var replacement = Sprite.Create(texture, rect, Vector2.one * .5f, 16);
                    replacement.name = name;
                    EditorUtility.CopySerialized(replacement, sprite);
                    Object.DestroyImmediate(replacement);
                    EditorUtility.SetDirty(sprite);
                }
                return sprite;
            }
            sprite = Sprite.Create(texture, rect, Vector2.one * .5f, 16);
            sprite.name = name;
            AssetDatabase.CreateAsset(sprite, path);
            return sprite;
        }

        private static void AddBanks(Transform root)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Pack + "Farm and Tileset/Tileset/Tileset Grass Water Spring.png");
            var north = Slice(texture, "RiverNorthBank", new Rect(88, texture.height - 64, 16, 8));
            var south = Slice(texture, "RiverSouthBank", new Rect(88, texture.height - 8, 16, 8));
            var existing = root.Find("River Shores");
            if (existing != null)
            {
                foreach (Transform shore in existing)
                {
                    shore.position = new Vector3(shore.position.x, shore.name == "North Shore" ? 8.25f : 5.75f, 0);
                    shore.localScale = Vector3.one;
                }
                return;
            }
            var shores = new GameObject("River Shores").transform;
            shores.SetParent(root, false);
            for (int x = -37; x < 37; x++)
            {
                if (x >= -1 && x <= 0) continue;
                Art(shores, "North Shore", north, new Vector3(x + .5f, 8.25f), new Vector2(1, .5f), -59);
                Art(shores, "South Shore", south, new Vector3(x + .5f, 5.75f), new Vector2(1, .5f), -59);
            }
            foreach (var renderer in root.parent.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.name == "Grass Tuft" && renderer.transform.position.y > 5.8f && renderer.transform.position.y < 8.2f)
                    renderer.enabled = false;
        }

        public static void VerifyMain()
        {
            ApplyMain();
            EditorSceneManager.OpenScene("Assets/SurvivorFarm/Scenes/Main.unity");
            var bridge = Object.FindFirstObjectByType<RepairableBridge>();
            if (bridge == null || !PrefabUtility.IsPartOfPrefabInstance(bridge))
                throw new System.InvalidOperationException("Bridge prefab missing.");
            var zones = Object.FindObjectsByType<LandUnlockZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (zones.Length != 8 || zones.Any(z => z.IsAvailable))
                throw new System.InvalidOperationException("Legacy land sale still active.");
            bridge.Restore(false);
            Physics2D.SyncTransforms();
            bool Blocked(float x) => Physics2D.LinecastAll(new Vector2(x, 5.8f), new Vector2(x, 8.2f)).Any(h => !h.collider.isTrigger);
            if (!Blocked(0) || !Blocked(5) || zones.Where(z => z.name.StartsWith("North")).Any(z => z.IsUnlocked))
                throw new System.InvalidOperationException("Broken crossing is not protected.");
            bridge.Restore(true);
            Physics2D.SyncTransforms();
            if (Blocked(0) || !Blocked(5) || zones.Any(z => !z.IsUnlocked))
                throw new System.InvalidOperationException("Repaired crossing or northern access is invalid.");
            bridge.Restore(false);
            System.IO.Directory.CreateDirectory("Logs");
            System.IO.File.WriteAllText("Logs/open-exploration-scene.txt", "PASS: 8 land sales disabled; bridge prefab wired; broken crossing blocks; repair opens only the crossing; northern access follows bridge.");
            var camera = Camera.main;
            camera.transform.position = new Vector3(0, 7, -10);
            camera.orthographic = true;
            camera.orthographicSize = 5;
            var target = new RenderTexture(1024, 640, 24);
            var pixels = new Texture2D(1024, 640, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1024, 640), 0, 0);
                pixels.Apply();
                System.IO.File.WriteAllBytes("Logs/open-exploration-bridge.png", pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                Object.DestroyImmediate(pixels);
                Object.DestroyImmediate(target);
            }
        }

        private static GameObject Art(Transform parent, string name, Sprite sprite, Vector3 position, Vector2 size, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return go;
        }

        private static BoxCollider2D Solid(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = size;
            return collider;
        }
    }
}
