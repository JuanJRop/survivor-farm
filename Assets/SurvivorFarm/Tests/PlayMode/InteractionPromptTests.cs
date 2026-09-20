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
        public void PromptShowsOnlyEAndHidesWithoutAnAvailableTarget()
        {
            FarmNotificationCenter.SetInteractionButtonAtWorldPosition(true, "Interactuar", Vector3.zero, camera);
            Assert.IsTrue(button.gameObject.activeSelf);
            Assert.AreEqual("E", button.GetComponentInChildren<Text>().text);
            Assert.AreEqual(48f, ((RectTransform)button.transform).sizeDelta.x);
            Assert.AreEqual(0, button.image.color.a, "Only the keycap has a background; the oversized dark banner is removed.");
            FarmNotificationCenter.SetInteractionButton(false, "Interactuar");
            Assert.IsFalse(button.gameObject.activeSelf);
        }

        [Test]
        public void OnlyHouseRepairAddsAWordToThePrompt()
        {
            FarmNotificationCenter.SetInteractionButton(true, "Reparar");
            Assert.AreEqual("E · Reparar", button.GetComponentInChildren<Text>().text);
            FarmNotificationCenter.SetInteractionButton(true, "Retirar flores · una semilla");
            Assert.AreEqual("E", button.GetComponentInChildren<Text>().text);
            Assert.AreEqual(48f, ((RectTransform)button.transform).sizeDelta.x);
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

        [TestCase(5.8f)]
        [TestCase(9f)]
        public void NearbyBadgeMovesBesidePlayerAtDifferentCameraZooms(float zoom)
        {
            camera.orthographicSize = zoom;
            var player = new GameObject("Player").AddComponent<FarmPlayerInteractor>();
            player.transform.SetParent(root.transform);
            typeof(FarmPlayerInteractor).GetField("mainCamera", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(player, camera);
            var target = new GameObject("Exit").AddComponent<ShopEntrance>();
            target.transform.SetParent(root.transform); target.transform.position = Vector3.down * .9f;
            var getPosition = typeof(FarmPlayerInteractor).GetMethod("GetIndicatorPosition", BindingFlags.NonPublic | BindingFlags.Instance);
            Vector3 point = (Vector3)getPosition.Invoke(player, new object[] { target });
            Vector3 screen = camera.WorldToScreenPoint(point);
            float halfBadge = FarmNotificationCenter.InteractionBadgeScreenSize(false).x * .5f;
            float playerRight = camera.WorldToScreenPoint(Vector3.right * .35f).x;
            float playerLeft = camera.WorldToScreenPoint(Vector3.left * .35f).x;
            Assert.IsTrue(screen.x - halfBadge >= playerRight + 7.9f || screen.x + halfBadge <= playerLeft - 7.9f,
                "The badge can choose either side, but must clear the player at this zoom.");
            Assert.That(point.y, Is.EqualTo(0).Within(.001f), "Keep the badge close to the exit rather than lifting it over the player.");
            target.transform.position = new Vector3(3, -.9f);
            Vector3 unobstructed = (Vector3)getPosition.Invoke(player, new object[] { target });
            Assert.That(unobstructed.x, Is.EqualTo(3).Within(.001f), "Only move prompts that overlap the player.");
        }
    }
}
