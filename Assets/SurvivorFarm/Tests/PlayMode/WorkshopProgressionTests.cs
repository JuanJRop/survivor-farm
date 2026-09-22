using System.Linq;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Tests
{
    public sealed class WorkshopProgressionTests
    {
        private GameObject root;
        private bool hadFarmVolume;
        private float originalFarmVolume;
        private bool hadFarmReducedMotion;
        private int originalFarmReducedMotion;
        private float originalAudioVolume;
        private bool originalReducedMotion;

        [SetUp]
        public void CaptureSettings()
        {
            hadFarmVolume = PlayerPrefs.HasKey("FarmVolume");
            originalFarmVolume = PlayerPrefs.GetFloat("FarmVolume");
            hadFarmReducedMotion = PlayerPrefs.HasKey("FarmReducedMotion");
            originalFarmReducedMotion = PlayerPrefs.GetInt("FarmReducedMotion");
            originalAudioVolume = AudioListener.volume;
            originalReducedMotion = CombatTimeFeedback.ReducedMotion;
        }

        [TearDown]
        public void TearDown()
        {
            GameMenuWindow.Instance?.Close();
            if (root != null) Object.DestroyImmediate(root);
            DestroyMenuCanvases();
            Time.timeScale = 1f;
            if (hadFarmVolume) PlayerPrefs.SetFloat("FarmVolume", originalFarmVolume);
            else PlayerPrefs.DeleteKey("FarmVolume");
            if (hadFarmReducedMotion) PlayerPrefs.SetInt("FarmReducedMotion", originalFarmReducedMotion);
            else PlayerPrefs.DeleteKey("FarmReducedMotion");
            PlayerPrefs.Save();
            AudioListener.volume = originalAudioVolume;
            CombatTimeFeedback.ReducedMotion = originalReducedMotion;
        }

        private static void DestroyMenuCanvases()
        {
            foreach (GameObject canvas in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                         .Where(value => value.name == "Menú de viaje"))
                Object.DestroyImmediate(canvas);
        }

        [Test]
        public void QuickSlotsKeepFiveAssignmentsAndOnlyAcceptUsableItems()
        {
            root = new GameObject("Quick slots fixture");
            PlayerInventory inventory = root.AddComponent<PlayerInventory>();
            PlayerQuickSlots quick = root.GetComponent<PlayerQuickSlots>();

            Assert.IsNotNull(quick);
            Assert.AreEqual(PlayerQuickSlots.SlotCount, quick.Count);
            Assert.AreEqual("Sword", quick.Get(0));
            Assert.IsFalse(quick.Set(4, "Leather"));

            inventory.AddEquipment("Bow");
            Assert.IsTrue(quick.Set(3, "Bow"));
            Assert.AreEqual("Bow", quick.Get(3));
            quick.Restore(new[] { "Bow", "Food" });
            Assert.AreEqual("Bow", quick.Get(0));
            Assert.AreEqual("Food", quick.Get(1));
            Assert.AreEqual(string.Empty, quick.Get(4));
        }

        [Test]
        public void UnifiedMenuRoutesWorkshopSkillsEquipmentAndResumesPausedRun()
        {
            Time.timeScale = 1f;
            root = new GameObject("Workshop fixture");
            PlayerInventory inventory = root.AddComponent<PlayerInventory>();
            root.AddComponent<PlayerToolUpgradeController>();
            root.AddComponent<PlayerCraftingController>();
            root.AddComponent<ToolMastery>();
            GameMenuWindow.OpenWorkshopActive();

            GameMenuWindow menu = GameMenuWindow.Instance;
            Assert.IsNotNull(menu);
            Assert.IsTrue(GameMenuWindow.IsOpen);
            Assert.AreEqual(GameMenuWindow.Page.Workshop, menu.CurrentPage);
            Assert.AreEqual(WorkshopProgressionWindow.Section.Shop, root.GetComponent<WorkshopProgressionWindow>().CurrentSection);
            Assert.AreEqual(0f, Time.timeScale);

            GameMenuWindow.OpenSkillsActive();
            Assert.AreEqual(GameMenuWindow.Page.Skills, menu.CurrentPage);
            Assert.IsTrue(SkillTreeWindow.IsOpen);
            Assert.IsNotNull(menu.Content.GetComponentInChildren<SkillDemonstrationPreview>(true));

            GameMenuWindow.OpenEquipmentActive();
            Assert.AreEqual(GameMenuWindow.Page.Equipment, menu.CurrentPage);
            string[] labels = Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Select(text => text.text).ToArray();
            CollectionAssert.Contains(labels, "Cinco cajones rápidos");
            for (int slot = 1; slot <= PlayerQuickSlots.SlotCount; slot++)
                Assert.IsTrue(labels.Any(text => text.StartsWith(slot + "   ")));

            menu.Close();
            Assert.IsFalse(GameMenuWindow.IsOpen);
            Assert.IsFalse(SkillTreeWindow.IsOpen);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void UnifiedSettingsPersistVolumeAndReducedMotionAcrossMenuRecreation()
        {
            Time.timeScale = 1f;
            PlayerPrefs.SetFloat("FarmVolume", .75f);
            PlayerPrefs.SetInt("FarmReducedMotion", 0);
            PlayerPrefs.Save();

            root = new GameObject("Settings persistence fixture");
            root.AddComponent<PlayerInventory>();
            GameMenuWindow menu = root.GetComponent<GameMenuWindow>();
            Assert.IsNotNull(menu);
            Assert.AreEqual(.75f, AudioListener.volume);
            Assert.IsFalse(CombatTimeFeedback.ReducedMotion);

            menu.Open(GameMenuWindow.Page.Settings);
            Transform settings = menu.Content.Find("Ajustes de partida");
            Assert.IsNotNull(settings);
            settings.GetComponentsInChildren<Button>(true).Where(button => button.gameObject.activeInHierarchy)
                .First(button => button.gameObject.name.StartsWith("Volumen ")).onClick.Invoke();
            Assert.AreEqual(.5f, AudioListener.volume);
            Assert.AreEqual(.5f, PlayerPrefs.GetFloat("FarmVolume"));

            settings.GetComponentsInChildren<Button>(true).Where(button => button.gameObject.activeInHierarchy)
                .First(button => button.gameObject.name.StartsWith("Movimiento de cámara ")).onClick.Invoke();
            Assert.IsTrue(CombatTimeFeedback.ReducedMotion);
            Assert.AreEqual(1, PlayerPrefs.GetInt("FarmReducedMotion"));

            menu.Close();
            Object.DestroyImmediate(root);
            root = null;
            DestroyMenuCanvases();

            root = new GameObject("Recreated settings fixture");
            root.AddComponent<PlayerInventory>();
            Assert.AreEqual(.5f, AudioListener.volume);
            Assert.IsTrue(CombatTimeFeedback.ReducedMotion);
        }
    }
}
