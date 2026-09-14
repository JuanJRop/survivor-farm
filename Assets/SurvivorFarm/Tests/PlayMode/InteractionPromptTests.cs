using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SurvivorFarm.Tests
{
    public sealed class InteractionPromptTests
    {
        private GameObject root;
        private Button button;
        private Camera camera;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Prompt Test");
            var canvas = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            canvas.transform.SetParent(root.transform);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            button = new GameObject("E", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            button.transform.SetParent(canvas.transform, false);
            button.gameObject.AddComponent<InteractionPromptAnimation>();
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(button.transform);
            var center = root.AddComponent<FarmNotificationCenter>();
            center.ConfigureFloatingInteraction(button, label);
            var cameraObject = new GameObject("Camera");
            cameraObject.transform.SetParent(root.transform);
            camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = Vector3.back * 10f;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(root);

        [Test]
        public void PromptShowsInteractAndHidesWithoutAnAvailableTarget()
        {
            FarmNotificationCenter.SetInteractionButtonAtWorldPosition(true, "Interactuar", Vector3.zero, camera);
            Assert.IsTrue(button.gameObject.activeSelf);
            Assert.AreEqual("Interactuar", button.GetComponentInChildren<Text>().text);
            FarmNotificationCenter.SetInteractionButton(false, "Interactuar");
            Assert.IsFalse(button.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator PromptAnimatesWithoutChangingItsLayoutSize()
        {
            var rect = (RectTransform)button.transform;
            Vector2 size = rect.sizeDelta;
            FarmNotificationCenter.SetInteractionButton(true, "Interactuar");
            FarmNotificationCenter.PulseInteraction();
            yield return null;
            yield return null;
            Assert.AreEqual(size, rect.sizeDelta);
            Assert.LessOrEqual(rect.localScale.x, 1f);
            Assert.Greater(rect.localScale.x, 0f);
        }

        [Test]
        public void ShopIsDiscoverableByTheSameInteractionSelector()
        {
            var shop = new GameObject("Shop").AddComponent<ShopEntrance>();
            shop.transform.SetParent(root.transform);
            shop.transform.position = Vector3.right;
            var player = new GameObject("Player").AddComponent<FarmPlayerInteractor>();
            player.transform.SetParent(root.transform);
            var find = typeof(FarmPlayerInteractor).GetMethod("FindNearestInteractable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.AreSame(shop, find.Invoke(player, new object[] { Vector3.zero, 1.35f }));
        }
    }
}
