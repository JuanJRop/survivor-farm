using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SurvivorFarm.Editor
{
    public static class PetSceneUpgrade
    {
        [MenuItem("Survivor Farm/Add Cat Companion To Main")]
        public static void UpgradeMain()
        {
            EditorSceneManager.OpenScene("Assets/SurvivorFarm/Scenes/Main.unity");
            ApplyToScene();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Cat companion, shared definition and equipment control installed.");
        }

        public static void ApplyToScene()
        {
            var inventory = Object.FindFirstObjectByType<PlayerInventory>();
            if (inventory.GetComponent<PlayerPetController>() != null) return;
            const string dataPath = "Assets/SurvivorFarm/Data/ScriptableObjects/GingerCat.asset";
            var definition = AssetDatabase.LoadAssetAtPath<PetDefinition>(dataPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<PetDefinition>();
                var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/Pets/Cats/1/Ginger.png")
                    .OfType<Sprite>().ToDictionary(s => s.name);
                var data = new SerializedObject(definition);
                foreach (string property in new[] { "walkFrames", "runFrames" })
                {
                    var frames = data.FindProperty(property);
                    frames.arraySize = 12;
                    for (int i = 0; i < 12; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = sprites[$"Ginger_{i + (property == "runFrames" ? 12 : 0)}"];
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(definition, dataPath);
            }
            var root = new GameObject("Cat Companion");
            var visual = new GameObject("Cat Sprite").AddComponent<SpriteRenderer>();
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one * 0.65f;
            visual.sortingOrder = 6;
            var pet = root.AddComponent<PetCompanion>();
            pet.ConfigureDefinition(definition, visual);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/SurvivorFarm/Prefabs/Characters/CatCompanion.prefab");
            Object.DestroyImmediate(root);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(inventory.transform.parent, false);
            instance.transform.position = inventory.transform.position + Vector3.right * 0.8f;
            var controller = inventory.gameObject.AddComponent<PlayerPetController>();
            var close = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(b => b.name == "Close Inventory");
            var button = Object.Instantiate(close, close.transform.parent);
            button.name = "Pet Equipment";
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, controller.ToggleEquipped);
            var rect = (RectTransform)button.transform;
            rect.anchoredPosition = new Vector2(0f, -184f);
            rect.sizeDelta = new Vector2(315f, 40f);
            var label = button.GetComponentInChildren<Text>();
            label.fontSize = 17;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = 17;
            var content = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(t => t.name == "Inventory Content");
            content.rectTransform.anchoredPosition = new Vector2(0f, 36f);
            content.rectTransform.sizeDelta = new Vector2(315f, 360f);
            controller.Configure(prefab.GetComponent<PetCompanion>(), instance.GetComponent<PetCompanion>(), label);
        }
    }
}
