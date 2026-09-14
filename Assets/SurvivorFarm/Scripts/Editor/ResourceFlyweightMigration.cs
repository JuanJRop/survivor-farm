using System;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    public static class ResourceFlyweightMigration
    {
        private const string Root = "Assets/SurvivorFarm/Data/Flyweights";
        private const string CatalogFolder = "Assets/SurvivorFarm/Data/Resources";
        private const string ScenePath = "Assets/SurvivorFarm/Scenes/Main.unity";

        [MenuItem("Survivor Farm/Migrate Main To Resource Flyweights")]
        public static void MigrateMain()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            ApplyToSceneAndPrefabs();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Validate();
        }

        public static void ApplyToSceneAndPrefabs()
        {
            EnsureFolder(Root);
            EnsureFolder(CatalogFolder);
            foreach (ItemKind kind in Enum.GetValues(typeof(ItemKind))) Persist(ResourceFlyweights.Item(kind));

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/SurvivorFarm/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (MigrateHierarchy(prefab)) PrefabUtility.SaveAsPrefabAsset(prefab, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                MigrateHierarchy(root);

            FlyweightCatalog catalog = AssetDatabase.LoadAssetAtPath<FlyweightCatalog>(CatalogFolder + "/FlyweightCatalog.asset");
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<FlyweightCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogFolder + "/FlyweightCatalog.asset");
            }
            SerializedObject serialized = new SerializedObject(catalog);
            SetAssets<ItemDefinition>(serialized, "items");
            SetAssets<ResourceDefinition>(serialized, "resources");
            SetAssets<ResourceAnimation>(serialized, "animations");
            SetAssets<CultivationDefinition>(serialized, "cultivations");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void PrepareForPrefab(GameObject root)
        {
            EnsureFolder(Root);
            foreach (ItemKind kind in Enum.GetValues(typeof(ItemKind))) Persist(ResourceFlyweights.Item(kind));
            MigrateHierarchy(root, false);
        }

        private static bool MigrateHierarchy(GameObject root, bool shareSprites = true)
        {
            bool changed = false;
            foreach (HarvestableResource resource in root.GetComponentsInChildren<HarvestableResource>(true))
            {
                SerializedObject legacy = new SerializedObject(resource);
                ResourceDefinition existing = resource.Definition;
                ItemKind kind = resource is AnimalResource ? ItemKind.Food : resource is TreeResource ? ItemKind.Wood : ItemKind.Stone;
                ResourceAnimation clip = existing != null ? existing.Animation : null;
                SerializedProperty frames = legacy.FindProperty("idleFrames");
                if (frames != null && frames.arraySize > 0)
                {
                    Sprite[] sprites = Enumerable.Range(0, frames.arraySize).Select(i => (Sprite)frames.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                    clip = Persist(ResourceFlyweights.Animation(sprites, legacy.FindProperty("framesPerSecond").floatValue));
                }
                if (clip != null) clip = Persist(clip);
                ResourceDefinition definition = ResourceFlyweights.Resource(kind,
                    ReadInt(legacy, "harvestAmount", existing != null ? existing.HarvestAmount : 2),
                    ReadInt(legacy, "coinReward", existing != null ? existing.CoinReward : 5),
                    ReadInt(legacy, "maxHealth", existing != null ? existing.MaxHealth : 1), clip);
                resource.SetDefinition(Persist(definition));
                foreach (SpriteRenderer renderer in shareSprites ? resource.GetComponentsInChildren<SpriteRenderer>(true) : Array.Empty<SpriteRenderer>())
                {
                    if (resource is AnimalResource) continue;
                    renderer.sprite = LoadShape(renderer.name == "Trunk" ? "ResourceSquare" : "ResourceCircle");
                    EditorUtility.SetDirty(renderer);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
                EditorUtility.SetDirty(resource);
                PrefabUtility.RecordPrefabInstancePropertyModifications(resource);
                changed = true;
            }
            foreach (FarmingPlot plot in root.GetComponentsInChildren<FarmingPlot>(true))
            {
                SerializedObject legacy = new SerializedObject(plot);
                CultivationDefinition existing = plot.Definition;
                if (existing == null)
                {
                    existing = ResourceFlyweights.Cultivation(legacy.FindProperty("cropName")?.stringValue ?? "fruta",
                        ReadFloat(legacy, "growDuration", 8f), ReadFloat(legacy, "digDuration", 1.3f),
                        ReadFloat(legacy, "hoeDuration", 1.1f), ReadFloat(legacy, "plantDuration", 0.9f), ReadFloat(legacy, "waterDuration", 1f));
                }
                plot.SetDefinition(Persist(existing));
                foreach (SpriteRenderer renderer in shareSprites ? plot.GetComponentsInChildren<SpriteRenderer>(true) : Array.Empty<SpriteRenderer>())
                {
                    renderer.sprite = LoadShape(renderer.gameObject == plot.gameObject ? "ResourceSquare" : "ResourceCircle");
                    EditorUtility.SetDirty(renderer);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
                EditorUtility.SetDirty(plot);
                PrefabUtility.RecordPrefabInstancePropertyModifications(plot);
                changed = true;
            }
            foreach (CollectableItem item in root.GetComponentsInChildren<CollectableItem>(true))
            {
                item.SetDefinition(Persist(item.Definition != null ? item.Definition : ResourceFlyweights.Item(ItemKind.Fruit)));
                EditorUtility.SetDirty(item);
                PrefabUtility.RecordPrefabInstancePropertyModifications(item);
                changed = true;
            }
            return changed;
        }

        private static Sprite LoadShape(string name) => AssetDatabase.LoadAllAssetsAtPath($"Assets/SurvivorFarm/Art/Sprites/{name}.asset").OfType<Sprite>().Single();
        private static int ReadInt(SerializedObject source, string name, int fallback) => source.FindProperty(name)?.intValue ?? fallback;
        private static float ReadFloat(SerializedObject source, string name, float fallback) => source.FindProperty(name)?.floatValue ?? fallback;

        private static T Persist<T>(T value) where T : ScriptableObject
        {
            if (AssetDatabase.Contains(value)) return value;
            string path = $"{Root}/{typeof(T).Name}_{value.name}.asset";
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            value.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(value, path);
            return value;
        }

        private static void SetAssets<T>(SerializedObject target, string field) where T : ScriptableObject
        {
            T[] assets = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { Root })
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid))).OrderBy(asset => asset.name).ToArray();
            SerializedProperty array = target.FindProperty(field);
            array.arraySize = assets.Length;
            for (int i = 0; i < assets.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        public static void Validate()
        {
            HarvestableResource[] resources = Object.FindObjectsByType<HarvestableResource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            FarmingPlot[] plots = Object.FindObjectsByType<FarmingPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (resources.Any(resource => resource.Definition == null || !AssetDatabase.Contains(resource.Definition)) ||
                plots.Any(plot => plot.Definition == null || !AssetDatabase.Contains(plot.Definition)))
                throw new InvalidOperationException("All resource instances must reference persistent flyweights.");
            Debug.Log($"Flyweights validated: {resources.Length} resources / {resources.Select(r => r.Definition).Distinct().Count()} definitions; {plots.Length} plots / {plots.Select(p => p.Definition).Distinct().Count()} cultivation definitions.");
        }
    }
}
