using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivorFarm.Editor
{
    public static class OutdoorEnemySceneUpgrade
    {
        [MenuItem("Survivor Farm/Add Outdoor Enemies To Main")]
        public static void UpgradeMain()
        {
            EditorSceneManager.OpenScene("Assets/SurvivorFarm/Scenes/Main.unity");
            ApplyToScene();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("Outdoor enemy pool and home safe zone installed.");
        }

        public static void ApplyToScene()
        {
            var existing = Object.FindFirstObjectByType<OutdoorEnemyPool>();
            if (existing != null)
            {
                existing.ConfigureClock(Object.FindFirstObjectByType<DayNightCycle>());
                EditorUtility.SetDirty(existing);
                return;
            }
            var house = Object.FindFirstObjectByType<BaseHouse>();
            var player = Object.FindFirstObjectByType<PlayerInventory>();
            if (house == null || player == null) throw new System.InvalidOperationException("Missing home or player.");
            var root = new GameObject("Outdoor Enemy");
            root.SetActive(false);
            var visual = root.AddComponent<SpriteRenderer>();
            visual.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SurvivorFarm/Art/Sprites/ResourceCircle.asset");
            if (visual.sprite == null) throw new System.InvalidOperationException("Missing circle sprite.");
            visual.sortingOrder = 5;
            root.transform.localScale = Vector3.one * (0.55f / visual.sprite.bounds.size.x);
            root.AddComponent<CircleCollider2D>().radius = visual.sprite.bounds.size.x * 0.5f;
            var enemy = root.AddComponent<OutdoorEnemyAI>();
            enemy.ConfigureStats("Limo salvaje", 3, 1, 1.6f, 0.7f, 1.5f, 2);
            enemy.ConfigureVisuals(visual, null, new Color(0.85f, 0.2f, 0.25f), "Limo salvaje");
            var material = visual.sharedMaterial;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/SurvivorFarm/Prefabs/World/OutdoorEnemy.prefab");
            Object.DestroyImmediate(root);

            var zoneRoot = new GameObject("Home Safe Zone");
            zoneRoot.transform.SetParent(house.transform.parent, false);
            var line = zoneRoot.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.startWidth = line.endWidth = 0.035f;
            line.startColor = line.endColor = new Color(0.3f, 0.95f, 0.7f, 0.6f);
            line.sortingOrder = 0;
            var zone = zoneRoot.AddComponent<HomeSafeZone>();
            zone.Configure(house.transform, player, line);
            var poolRoot = new GameObject("Outdoor Enemy Pool");
            poolRoot.transform.SetParent(house.transform.parent, false);
            var pool = poolRoot.AddComponent<OutdoorEnemyPool>();
            pool.Configure(prefab.GetComponent<OutdoorEnemyAI>(), player.transform, zone, house.transform.parent);
            pool.Initialize();
            pool.ConfigureClock(Object.FindFirstObjectByType<DayNightCycle>());
        }
    }
}
