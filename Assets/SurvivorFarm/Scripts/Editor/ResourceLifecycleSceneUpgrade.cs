using System;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    public static class ResourceLifecycleSceneUpgrade
    {
        private const string MainPath = "Assets/SurvivorFarm/Scenes/Main.unity";
        private const string PrefabFolder = "Assets/SurvivorFarm/Prefabs/World/";
        private const string AnimalPath = PrefabFolder + "Animal.prefab";
        private const string DeerPath = "Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/Forest Animals/Deer/Male/Idle.png";

        [MenuItem("Survivor Farm/Upgrade Resource Lifecycle In Main")]
        public static void UpgradeMain()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            VerifyScriptImports();
            EditorSceneManager.OpenScene(MainPath);
            ApplyToScene();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Resource lifecycle: Main upgraded successfully.");
        }

        private static void VerifyScriptImports()
        {
            foreach (Type type in new[] { typeof(AnimalResource), typeof(ResourceSpawnPoint) })
            {
                string path = $"Assets/SurvivorFarm/Scripts/Runtime/Gameplay/{type.Name}.cs";
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null || script.GetClass() != type)
                    throw new InvalidOperationException($"Unity has not imported {type.Name}. Open the project using its full path and recompile before saving prefabs.");
            }
        }

        public static void ApplyToScene()
        {
            Transform outdoor = Find<Transform>("Outdoor World");
            if (outdoor == null) throw new InvalidOperationException("Main has no Outdoor World.");
            RepairResourcePrefabSprites(outdoor);
            AnimalResource animalPrefab = GetAnimalPrefab();
            foreach (HarvestableResource resource in outdoor.GetComponentsInChildren<HarvestableResource>(true))
            {
                string prefabName = resource is TreeResource ? "Tree" : resource is RockResource ? "Rock" : "Animal";
                HarvestableResource prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + prefabName + ".prefab")
                    ?.GetComponent<HarvestableResource>();
                ResourceSpawnPoint.Attach(resource, prefab);
            }

            if (outdoor.GetComponentInChildren<AnimalResource>(true) == null)
            {
                GameObject animals = new GameObject("Animals");
                animals.transform.SetParent(outdoor, false);
                AddAnimal(animalPrefab, animals.transform, new Vector3(1.4f, -2.3f, 0f));
                AddAnimal(animalPrefab, animals.transform, new Vector3(-1.4f, 2.5f, 0f));
            }

            BindControls();
            ValidateSpawns(outdoor);
        }

        private static AnimalResource GetAnimalPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(AnimalPath);
            if (existing != null) return existing.GetComponent<AnimalResource>();

            Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(DeerPath).OfType<Sprite>()
                .Where(sprite => sprite.name == "Idle_0" || sprite.name == "Idle_1")
                .OrderBy(sprite => sprite.name).ToArray();
            if (frames.Length != 2) throw new InvalidOperationException("Missing sliced deer idle sprites.");

            GameObject animal = new GameObject("Animal");
            SpriteRenderer renderer = animal.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingOrder = 4;
            CircleCollider2D collider = animal.AddComponent<CircleCollider2D>();
            collider.radius = 0.35f;
            collider.offset = new Vector2(0f, -0.25f);
            AnimalResource resource = animal.AddComponent<AnimalResource>();
            resource.Configure(renderer, null, 2, 0);
            resource.ConfigureHealth(3);
            resource.ConfigureAnimation(renderer, frames);
            ResourceFlyweightMigration.PrepareForPrefab(animal);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(animal, AnimalPath);
            Object.DestroyImmediate(animal);
            return prefab.GetComponent<AnimalResource>();
        }

        private static void RepairResourcePrefabSprites(Transform outdoor)
        {
            foreach (string prefabName in new[] { "Tree", "Rock" })
            {
                string path = PrefabFolder + prefabName + ".prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    bool changed = false;
                    foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        if (renderer.sprite != null) continue;
                        SpriteRenderer source = outdoor.GetComponentsInChildren<SpriteRenderer>(true)
                            .FirstOrDefault(candidate => candidate.name == renderer.name && candidate.sprite != null);
                        if (source == null) throw new InvalidOperationException($"Missing sprite for {prefabName}/{renderer.name}.");
                        renderer.sprite = PersistShapeSprite(source.sprite, renderer.name == "Trunk" ? "ResourceSquare" : "ResourceCircle");
                        changed = true;
                    }
                    if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static Sprite PersistShapeSprite(Sprite source, string name)
        {
            string path = $"Assets/SurvivorFarm/Art/Sprites/{name}.asset";
            Sprite existing = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (existing != null) return existing;
            Texture2D texture = Object.Instantiate(source.texture);
            texture.name = name;
            AssetDatabase.CreateAsset(texture, path);
            Sprite sprite = Sprite.Create(texture, source.rect, source.pivot / source.rect.size, source.pixelsPerUnit);
            sprite.name = name;
            AssetDatabase.AddObjectToAsset(sprite, path);
            AssetDatabase.SaveAssets();
            return sprite;
        }

        private static void AddAnimal(AnimalResource prefab, Transform parent, Vector3 position)
        {
            GameObject animal = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, parent);
            animal.transform.position = position;
            ResourceSpawnPoint.Attach(animal.GetComponent<AnimalResource>(), prefab);
        }

        private static void BindControls()
        {
            PlayerCombatController combat = Object.FindFirstObjectByType<PlayerCombatController>();
            FarmPlayerInteractor interactor = Object.FindFirstObjectByType<FarmPlayerInteractor>();
            InventoryPanelSystem inventory = Object.FindFirstObjectByType<InventoryPanelSystem>();
            if (combat == null || interactor == null || inventory == null)
                throw new InvalidOperationException("Main is missing player or inventory scripts.");

            PlayerToolbelt tools = combat.GetComponent<PlayerToolbelt>();
            Bind(Find<Button>("Previous Tool"), tools.SelectPrevious);
            Bind(Find<Button>("Next Tool"), tools.SelectNext);
            Transform slots = Find<Transform>("Inventory Slots");
            Transform[] orderedSlots = slots.Cast<Transform>().OrderBy(slot => slot.GetSiblingIndex()).ToArray();
            Object.FindFirstObjectByType<FarmNotificationCenter>().ConfigureGameplayBindings(
                combat.GetComponent<PlayerInventory>(), tools,
                orderedSlots.Select(slot => slot.GetComponentInChildren<Text>(true)).ToArray(),
                orderedSlots.Select(slot => slot.Find("Item Icon").GetComponent<Image>()).ToArray(),
                Array.ConvertAll((FarmTool[])Enum.GetValues(typeof(FarmTool)), FarmPrototypeBuilder.GetToolSprite));

            Button attack = Find<Button>("Attack Button");
            Bind(attack, combat.Attack);
            combat.ConfigureAttackUi(attack.GetComponentInChildren<Text>(true), Find<Image>("Attack Cooldown Fill"));
            PrefabUtility.RecordPrefabInstancePropertyModifications(combat);
            Bind(Find<Button>("Interact Button"), interactor.PerformInteraction);
            Bind(Find<Button>("Inventory Button"), inventory.Toggle);
            Bind(Find<Button>("Close Inventory"), inventory.Close);

            Button eat = Find<Button>("Eat Food");
            Button close = Find<Button>("Close Inventory");
            if (eat == null)
            {
                eat = Object.Instantiate(close, close.transform.parent);
                eat.name = "Eat Food";
                eat.onClick = new Button.ButtonClickedEvent();
                eat.GetComponentInChildren<Text>().text = "Comer";
            }
            RectTransform eatRect = (RectTransform)eat.transform;
            RectTransform closeRect = (RectTransform)close.transform;
            eatRect.anchoredPosition = new Vector2(-90f, -232f);
            closeRect.anchoredPosition = new Vector2(90f, -232f);
            eatRect.sizeDelta = closeRect.sizeDelta = new Vector2(155f, 48f);
            Bind(eat, inventory.EatFood);
            Text content = Find<Text>("Inventory Content");
            content.fontSize = 19;
            content.resizeTextForBestFit = true;
            content.resizeTextMinSize = 16;
            content.resizeTextMaxSize = 19;
        }

        private static void Bind(Button button, UnityAction action)
        {
            if (button == null) throw new InvalidOperationException($"Missing button for {action.Method.Name}.");
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentTarget(i) == action.Target as Object &&
                    button.onClick.GetPersistentMethodName(i) == action.Method.Name) return;
            }
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static T Find<T>(string name) where T : Component
        {
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(component => component.name == name);
        }

        private static void ValidateSpawns(Transform outdoor)
        {
            ResourceSpawnPoint[] points = outdoor.GetComponentsInChildren<ResourceSpawnPoint>(true);
            if (points.Any(point => point.Instance == null || point.Prefab == null) ||
                points.Select(point => point.PersistentId).Distinct().Count() != points.Length)
                throw new InvalidOperationException("Resource spawn points need unique IDs, instances and prefabs.");
            if (points.Any(point => point.Prefab.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer => renderer.sprite == null)))
                throw new InvalidOperationException("Resource prefabs must retain their sprites after loading.");
            Debug.Log($"Validated {points.Length} resource spawn points with prefabs.");
        }
    }
}
