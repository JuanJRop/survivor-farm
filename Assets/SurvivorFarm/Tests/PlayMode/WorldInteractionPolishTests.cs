using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEngine;
using UnityEngine.TestTools;

namespace SurvivorFarm.Tests
{
    public sealed class PointerTestResource : WorldInteractable
    {
        public override string GetInteractionLabel(FarmTool tool) => "Resource";
        public override void Interact(FarmTool tool, PlayerInventory inventory) { }
    }

    public sealed class CivilianThreatTestEnemy : EnemyAIBase { protected override void TickEnemy() { } }

    public sealed class WorldInteractionPolishTests
    {
        private GameObject root;
        private Texture2D opaque, cutout;
        private Sprite opaqueSprite, cutoutSprite;

        [SetUp] public void Setup()
        {
            root = new GameObject("Interaction regressions");
            opaque = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            cutout = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++)
            {
                opaque.SetPixel(x, y, Color.white);
                cutout.SetPixel(x, y, x < 2 ? Color.clear : Color.white);
            }
            opaque.Apply(); cutout.Apply();
            opaqueSprite = Sprite.Create(opaque, new Rect(0, 0, 4, 4), Vector2.one * .5f, 1);
            cutoutSprite = Sprite.Create(cutout, new Rect(0, 0, 4, 4), Vector2.one * .5f, 1);
        }

        [TearDown] public void Teardown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(opaqueSprite); Object.DestroyImmediate(cutoutSprite);
            Object.DestroyImmediate(opaque); Object.DestroyImmediate(cutout);
        }

        [Test] public void PointerSelectsVisibleFrontObjectButIgnoresTransparentPixels()
        {
            var bush = Resource("Bush", opaqueSprite, 1);
            var tree = Resource("Tree canopy", cutoutSprite, 2);
            var candidates = new WorldInteractable[] { bush, tree };
            Assert.AreSame(tree, WorldPointerTargeting.FindAt(candidates, new Vector2(.75f, .5f)));
            Assert.AreSame(bush, WorldPointerTargeting.FindAt(candidates, new Vector2(-.75f, .5f)), "Empty canopy corners must not steal the target.");
            tree.GetComponent<SpriteRenderer>().flipX = true;
            Assert.AreSame(tree, WorldPointerTargeting.FindAt(candidates, new Vector2(-.75f, .5f)));
            Assert.AreSame(bush, WorldPointerTargeting.FindAt(candidates, new Vector2(.75f, .5f)));
        }

        [UnityTest] public IEnumerator TreeBecomesTransparentBehindPlayerAndRecoversItsOpacity()
        {
            var tree = Resource("Tree", opaqueSprite, 1);
            var art = tree.GetComponent<SpriteRenderer>();
            art.transform.position = Vector3.up;
            var player = new GameObject("Player position"); player.transform.SetParent(root.transform);
            player.transform.position = Vector3.up * 1.5f;
            var fader = TreeOcclusionFader.Ensure(tree.gameObject);
            Field(fader, "player", player.transform);
            yield return new WaitForSeconds(.2f);
            Assert.That(art.color.a, Is.LessThan(.5f));
            TreeOcclusionFader.Ensure(tree.gameObject);
            player.transform.position = Vector3.down * 3;
            yield return new WaitForSeconds(.2f);
            Assert.That(art.color.a, Is.EqualTo(1f).Within(.01f), "Refreshing a faded tree must preserve its original opacity.");
        }

        [Test] public void CivilianReactsToALocalEnemyAndFindsAnEscapeLaneAroundAWall()
        {
            var civilian = new GameObject("Civilian"); civilian.transform.SetParent(root.transform);
            var visual = civilian.AddComponent<SpriteRenderer>();
            var health = civilian.AddComponent<VillageResidentHealth>(); health.Configure("village:farmer");
            var routine = civilian.AddComponent<VillageNpcRoutine>();
            Field(routine, "resident", health); Field(routine, "visual", visual);
            var enemyObject = new GameObject("Nearby threat", typeof(CircleCollider2D)); enemyObject.transform.SetParent(root.transform);
            var enemy = enemyObject.AddComponent<CivilianThreatTestEnemy>(); enemy.Configure(null, null);
            enemy.ActivateFromPool(Vector3.left * 2);
            var wall = new GameObject("House wall", typeof(BoxCollider2D)); wall.transform.SetParent(root.transform);
            wall.transform.position = new Vector3(.9f, .25f); wall.GetComponent<BoxCollider2D>().size = new Vector2(.4f, 1f);
            Physics2D.SyncTransforms();
            var tick = typeof(VillageNpcRoutine).GetMethod("TickResident", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < 5; i++) { tick.Invoke(routine, new object[] { .1f }); Physics2D.SyncTransforms(); }
            Assert.IsTrue(routine.IsFleeing, "Local danger must work without waiting for a global combat phase.");
            Assert.That(Vector2.Distance(civilian.transform.position, enemy.transform.position), Is.GreaterThan(2.15f));
            Assert.That(Mathf.Abs(civilian.transform.position.y), Is.GreaterThan(.1f), "The villager should run around the wall, not freeze against it.");
        }

        private PointerTestResource Resource(string name, Sprite sprite, int sorting)
        {
            var obj = new GameObject(name); obj.transform.SetParent(root.transform);
            var visual = obj.AddComponent<SpriteRenderer>(); visual.sprite = sprite; visual.sortingOrder = sorting;
            return obj.AddComponent<PointerTestResource>();
        }

        private static void Field(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
