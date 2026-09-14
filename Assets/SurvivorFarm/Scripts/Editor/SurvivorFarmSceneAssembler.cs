using System.Collections.Generic;
using System.IO;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivorFarm.Editor
{
    public static class SurvivorFarmSceneAssembler
    {
        private const string MainScenePath = "Assets/SurvivorFarm/Scenes/Main.unity";
        private const string CharactersFolder = "Assets/SurvivorFarm/Prefabs/Characters";
        private const string WorldFolder = "Assets/SurvivorFarm/Prefabs/World";
        private const string ItemsFolder = "Assets/SurvivorFarm/Prefabs/Items";
        private const string CropSpritePath = "Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/Farm Crops/Spring/Strawberry.png";
        private const string FallbackCropSpritePath = "Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/Farm Crops/Summer/Tomato.png";

        [MenuItem("Survivor Farm/Rebuild Main Scene And Prefabs")]
        public static void RebuildMainSceneAndPrefabs()
        {
            EnsureProjectFolders();

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            FarmPrototypeBuilder.Build(false);
            AddMaterializedSceneBootstrap();

            Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>
            {
                { "Player", SaveSceneObjectAsPrefab("Player Base", $"{CharactersFolder}/Player.prefab", true) },
                { "Farming Plot", SaveSceneObjectAsPrefabByPrefix("Grass Cell", $"{WorldFolder}/FarmingPlot.prefab", true) },
                { "Tree", SaveSceneObjectAsPrefab("Tree", $"{WorldFolder}/Tree.prefab", true) },
                { "Rock", SaveSceneObjectAsPrefab("Rock", $"{WorldFolder}/Rock.prefab", true) },
                { "Player Base House", SaveSceneObjectAsPrefab("Player Base House", $"{WorldFolder}/PlayerBaseHouse.prefab", true) },
                { "Dungeon Chest", SaveSceneObjectAsPrefab("Entry Chest", $"{WorldFolder}/DungeonChest.prefab", false) },
                { "Fruit", CreateFruitPrefab($"{ItemsFolder}/Fruit.prefab") }
            };

            BuildPrefabReferenceShelf(prefabs);
            ResourceLifecycleSceneUpgrade.ApplyToScene();
            ResourceFlyweightMigration.ApplyToSceneAndPrefabs();
            DayNightSceneUpgrade.ApplyToScene();
            BedSceneUpgrade.ApplyToScene();
            OutdoorEnemySceneUpgrade.ApplyToScene();
            PetSceneUpgrade.ApplyToScene();

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MainScenePath);
            OriginalHudSceneUpgrade.Apply();
            OriginalWorldArtUpgrade.Apply();
            PlayerAnimationUpgrade.Apply();
            PcControlsUpgrade.Apply();
            ScreenSizedParcelsUpgrade.Apply();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Survivor Farm Main scene rebuilt at {MainScenePath}.");
        }

        private static void EnsureProjectFolders()
        {
            EnsureFolder("Assets/SurvivorFarm");
            EnsureFolder("Assets/SurvivorFarm/Scenes");
            EnsureFolder("Assets/SurvivorFarm/Prefabs");
            EnsureFolder(CharactersFolder);
            EnsureFolder(WorldFolder);
            EnsureFolder(ItemsFolder);
        }

        private static void EnsureFolder(string folderPath)
        {
            folderPath = folderPath.Replace("\\", "/");
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
            string folderName = Path.GetFileName(folderPath);

            if (!string.IsNullOrEmpty(parent) && parent != "Assets")
            {
                EnsureFolder(parent);
            }

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static void AddMaterializedSceneBootstrap()
        {
            Transform controllers = FindSceneObject("00 Controladores")?.transform;
            GameObject bootstrapObject = new GameObject("Game Bootstrap");
            if (controllers != null)
            {
                bootstrapObject.transform.SetParent(controllers, false);
            }

            GameBootstrap bootstrap = bootstrapObject.AddComponent<GameBootstrap>();
            SerializedObject serializedBootstrap = new SerializedObject(bootstrap);
            SerializedProperty buildOnStart = serializedBootstrap.FindProperty("buildFarmPrototypeOnStart");
            if (buildOnStart != null)
            {
                buildOnStart.boolValue = false;
            }

            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject SaveSceneObjectAsPrefab(string objectName, string prefabPath, bool connectSceneInstance)
        {
            return SaveSceneObjectAsPrefab(FindSceneObject(objectName), prefabPath, connectSceneInstance);
        }

        private static GameObject SaveSceneObjectAsPrefabByPrefix(string objectNamePrefix, string prefabPath, bool connectSceneInstance)
        {
            return SaveSceneObjectAsPrefab(FindSceneObjectByPrefix(objectNamePrefix), prefabPath, connectSceneInstance);
        }

        private static GameObject SaveSceneObjectAsPrefab(GameObject source, string prefabPath, bool connectSceneInstance)
        {
            if (source == null)
            {
                Debug.LogWarning($"Could not create prefab at {prefabPath}; source object was not found.");
                return null;
            }

            ResourceFlyweightMigration.PrepareForPrefab(source);
            return connectSceneInstance
                ? PrefabUtility.SaveAsPrefabAssetAndConnect(source, prefabPath, InteractionMode.AutomatedAction)
                : PrefabUtility.SaveAsPrefabAsset(source, prefabPath);
        }

        private static GameObject CreateFruitPrefab(string prefabPath)
        {
            GameObject fruit = new GameObject("Fruit");
            fruit.transform.localScale = Vector3.one * 0.62f;

            SpriteRenderer renderer = fruit.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadFirstSprite(CropSpritePath, FallbackCropSpritePath);
            renderer.color = renderer.sprite != null ? Color.white : new Color(1f, 0.22f, 0.16f);
            renderer.sortingOrder = 5;

            CircleCollider2D collider = fruit.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.35f;

            FruitPickup pickup = fruit.AddComponent<FruitPickup>();
            pickup.Configure(1);

            ResourceFlyweightMigration.PrepareForPrefab(fruit);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(fruit, prefabPath);
            Object.DestroyImmediate(fruit);
            return prefab;
        }

        private static Sprite LoadFirstSprite(params string[] assetPaths)
        {
            foreach (string assetPath in assetPaths)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null)
                {
                    return sprite;
                }

                Object[] subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
                foreach (Object subAsset in subAssets)
                {
                    if (subAsset is Sprite subSprite)
                    {
                        return subSprite;
                    }
                }
            }

            string[] guids = AssetDatabase.FindAssets("Strawberry t:Sprite", new[] { "Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/Farm Crops" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static void BuildPrefabReferenceShelf(IReadOnlyDictionary<string, GameObject> prefabs)
        {
            Transform prefabSection = FindSceneObject("04 Prefabs del Mundo")?.transform;
            if (prefabSection == null)
            {
                return;
            }

            GameObject shelf = new GameObject("Biblioteca de Prefabs");
            shelf.transform.SetParent(prefabSection, false);

            int index = 0;
            foreach (KeyValuePair<string, GameObject> prefab in prefabs)
            {
                if (prefab.Value == null)
                {
                    continue;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab.Value);
                instance.name = $"{prefab.Key} Reference";
                instance.transform.SetParent(shelf.transform, false);
                instance.transform.localPosition = new Vector3(index * 1.25f, 0f, 0f);
                index++;
            }

            shelf.SetActive(false);
        }

        private static GameObject FindSceneObject(string objectName)
        {
            foreach (GameObject candidate in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.scene == SceneManager.GetActiveScene() && candidate.name == objectName)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static GameObject FindSceneObjectByPrefix(string objectNamePrefix)
        {
            foreach (GameObject candidate in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.scene == SceneManager.GetActiveScene() && candidate.name.StartsWith(objectNamePrefix, System.StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
