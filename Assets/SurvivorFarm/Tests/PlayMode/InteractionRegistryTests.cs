using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Tests
{
    public sealed class InteractionRegistryTests
    {
        private GameObject root;
        private int initial;
        [SetUp] public void Setup() { root = new GameObject("Interaction registry tests"); initial = WorldInteractable.Active.Count; }
        [TearDown] public void Teardown() => Object.DestroyImmediate(root);
        private T Add<T>(string label) where T : Component
        { var go = new GameObject(label); go.transform.SetParent(root.transform); return go.AddComponent<T>(); }

        [Test] public void RegistrationsSurviveSwapRemovalAndReactivationWithoutSceneScans()
        {
            var first = Add<PointerTestResource>("First"); var middle = Add<PointerTestResource>("Middle"); var last = Add<PointerTestResource>("Last");
            Assert.AreEqual(initial + 3, WorldInteractable.Active.Count);
            middle.enabled = false; last.enabled = false;
            Assert.AreEqual(initial + 1, WorldInteractable.Active.Count);
            for (int i = 0; i < 100; i++) { middle.enabled = true; middle.enabled = false; }
            Assert.AreEqual(initial + 1, WorldInteractable.Active.Count);
            Object.DestroyImmediate(first.gameObject); Assert.AreEqual(initial, WorldInteractable.Active.Count);
        }

        [Test] public void SpecializedLifecycleOverridesAlsoRegisterAndUnregister()
        {
            var animal = Add<AnimalResource>("Animal"); var horse = Add<HorseMount>("Horse");
            var house = Add<VillageHouseHealth>("House"); var resident = Add<VillageResidentHealth>("Resident");
            Add<IronVein>("Ore"); Add<ValleyInteraction>("Story interaction");
            Assert.AreEqual(initial + 6, WorldInteractable.Active.Count);
            root.SetActive(false); Assert.AreEqual(initial, WorldInteractable.Active.Count);
            root.SetActive(true); Assert.AreEqual(initial + 6, WorldInteractable.Active.Count);
        }

        [Test] public void DisablingAnInteractableClearsItsPointerVisualCache()
        {
            var item = Add<PointerTestResource>("Pointer resource");
            WorldPointerTargeting.FindAt(new WorldInteractable[] { item }, Vector2.zero);
            int count = WorldPointerTargeting.CachedVisualCount;
            item.enabled = false;
            Assert.AreEqual(count - 1, WorldPointerTargeting.CachedVisualCount);
        }

        [Test] public void CharacterKeepsItsAuthoredAnimationWithoutAnAttachedSwordObject()
        {
            var player = Add<SpriteRenderer>("Player"); player.gameObject.AddComponent<PlayerCharacterAnimator>();
            Assert.IsNull(player.transform.Find("Espada equipada"));
            Assert.AreEqual(1, player.GetComponentsInChildren<SpriteRenderer>().Length);
            Assert.NotNull(PlayerWeaponPresentation.SwordSprite(2), "HUD upgrade icons remain available.");
        }
    }
}
