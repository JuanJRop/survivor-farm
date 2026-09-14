using SurvivorFarm.Runtime.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivorFarm.Editor
{
    public static class CombatLootUpgrade
    {
        [MenuItem("Survivor Farm/Apply Combat Knockback And Loot")]
        public static void ApplyMain()
        {
            EditorSceneManager.OpenScene("Assets/SurvivorFarm/Scenes/Main.unity");
            var root = new GameObject("Enemy Coin Loot");
            var collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.45f;
            var visual = new GameObject("Coin").AddComponent<SpriteRenderer>();
            visual.transform.SetParent(root.transform, false);
            visual.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SurvivorFarm/UI/OriginalSprites/Coin.png");
            visual.transform.localScale = Vector3.one * (0.35f / visual.sprite.bounds.size.x);
            visual.sortingOrder = 5000;
            var loot = root.AddComponent<EnemyLootPickup>();
            loot.Configure(AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/SurvivorFarm/Data/Flyweights/ItemDefinition_Coins.asset"), 1, visual.transform);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, "Assets/SurvivorFarm/Prefabs/Items/EnemyCoinLoot.prefab");
            Object.DestroyImmediate(root);
            var template = saved.GetComponent<EnemyLootPickup>();
            foreach (var sceneRoot in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var enemy in sceneRoot.GetComponentsInChildren<EnemyAIBase>(true))
                {
                    enemy.ConfigureLoot(template);
                    EditorUtility.SetDirty(enemy);
                    if (PrefabUtility.IsPartOfPrefabInstance(enemy)) PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);
                }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/SurvivorFarm/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset.GetComponentInChildren<EnemyAIBase>(true) == null) continue;
                var editable = PrefabUtility.LoadPrefabContents(path);
                foreach (var enemy in editable.GetComponentsInChildren<EnemyAIBase>(true)) enemy.ConfigureLoot(template);
                PrefabUtility.SaveAsPrefabAsset(editable, path);
                PrefabUtility.UnloadPrefabContents(editable);
            }
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Knockback and coin drops integrated.");
        }
    }
}
