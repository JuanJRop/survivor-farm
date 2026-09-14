using System.Linq;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SurvivorFarm.Editor
{
    public static class InteractionPromptUpgrade
    {
        [MenuItem("Survivor Farm/Apply Floating E Interaction")]
        public static void ApplyMain()
        {
            EditorSceneManager.OpenScene("Assets/SurvivorFarm/Scenes/Main.unity");
            var center = Object.FindFirstObjectByType<FarmNotificationCenter>();
            var data = new SerializedObject(center);
            var button = (Button)data.FindProperty("interactionButton").objectReferenceValue;
            if (button == null) throw new System.InvalidOperationException("Missing interaction button.");
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.sizeDelta = new Vector2(128f, 60f);
            rect.localScale = Vector3.one;
            var label = button.GetComponentInChildren<Text>(true);
            foreach (var graphic in button.GetComponentsInChildren<Graphic>(true))
                graphic.gameObject.SetActive(true);
            var image = button.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SurvivorFarm/UI/OriginalSprites/Panel.png");
            image.type = Image.Type.Sliced;
            image.color = new Color(0.08f, 0.06f, 0.05f, 0.96f);
            image.raycastTarget = true;
            label.text = "Interactuar";
            label.fontSize = 14;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 14;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(1f, 0.92f, 0.76f, 1f);
            label.raycastTarget = false;
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = label.rectTransform.pivot = new Vector2(0.5f, 1f);
            label.rectTransform.anchoredPosition = new Vector2(0f, -36f);
            label.rectTransform.sizeDelta = new Vector2(112f, 20f);
            var hand = button.GetComponentsInChildren<Image>(true).FirstOrDefault(i => i.name == "Hand Icon");
            if (hand != null)
            {
                hand.rectTransform.anchorMin = hand.rectTransform.anchorMax = hand.rectTransform.pivot = new Vector2(0.5f, 1f);
                hand.rectTransform.anchoredPosition = new Vector2(0f, -5f);
                hand.rectTransform.sizeDelta = new Vector2(28f, 28f);
            }
            if (button.GetComponent<InteractionPromptAnimation>() == null) button.gameObject.AddComponent<InteractionPromptAnimation>();
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, Object.FindFirstObjectByType<FarmPlayerInteractor>().PerformInteraction);
            center.ConfigureFloatingInteraction(button, label);
            button.interactable = true;
            button.gameObject.SetActive(false);
            EditorUtility.SetDirty(center);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("Floating interaction prompt installed.");
        }
    }
}
