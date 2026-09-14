using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SurvivorFarm.Editor
{
    public static class DayNightSceneUpgrade
    {
        [MenuItem("Survivor Farm/Add Day Night Cycle To Main")]
        public static void UpgradeMain()
        {
            EditorSceneManager.OpenScene("Assets/SurvivorFarm/Scenes/Main.unity");
            ApplyToScene();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("Day/night cycle installed in Main.");
        }

        public static void ApplyToScene()
        {
            if (Object.FindFirstObjectByType<DayNightCycle>() != null) return;
            var controller = new GameObject("Day Night Cycle").AddComponent<DayNightCycle>();
            controller.transform.SetParent(GameObject.Find("00 Controladores")?.transform, false);
            var canvas = new GameObject("Day Night World Tint", typeof(RectTransform), typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100;
            canvas.transform.SetParent(controller.transform, false);
            var overlay = new GameObject("Tint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
            overlay.transform.SetParent(canvas.transform, false);
            overlay.rectTransform.anchorMin = Vector2.zero;
            overlay.rectTransform.anchorMax = Vector2.one;
            overlay.rectTransform.offsetMin = Vector2.zero;
            overlay.rectTransform.offsetMax = Vector2.zero;
            controller.Configure(overlay,
                Object.FindFirstObjectByType<ShopEntrance>(FindObjectsInactive.Include),
                Object.FindFirstObjectByType<DungeonEntrance>(FindObjectsInactive.Include));
            var save = Object.FindFirstObjectByType<GameSaveSystem>();
            if (save != null)
            {
                var serialized = new SerializedObject(save);
                serialized.FindProperty("dayNightCycle").objectReferenceValue = controller;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
