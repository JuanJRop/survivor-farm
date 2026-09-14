using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SurvivorFarm.Tests
{
    public sealed class ResourceLifecycleTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private PlayerInventory inventory;

        [SetUp]
        public void SetUp()
        {
            inventory = CreateObject("Player").AddComponent<PlayerInventory>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject instance in objects)
                if (instance != null) Object.DestroyImmediate(instance);
            objects.Clear();
            foreach (ArrowProjectile arrow in Object.FindObjectsByType<ArrowProjectile>(FindObjectsSortMode.None))
                Object.DestroyImmediate(arrow.gameObject);
            foreach (FarmNotificationCenter center in Object.FindObjectsByType<FarmNotificationCenter>(FindObjectsSortMode.None))
                Object.DestroyImmediate(center.gameObject);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ToolsRewardOnceAndRemoveEveryVisualAndCollider(bool tree)
        {
            HarvestableResource resource = tree ? CreateResource<TreeResource>() : CreateResource<RockResource>();
            resource.Interact(FarmTool.Sword, inventory);
            Assert.That(resource.IsHarvested, Is.False);
            int events = 0;
            resource.Depleted += _ => events++;
            FarmTool tool = tree ? FarmTool.Axe : FarmTool.Pickaxe;
            resource.Interact(tool, inventory);
            resource.Interact(tool, inventory);
            Assert.That(tree ? inventory.Wood : inventory.Stone, Is.EqualTo(2));
            Assert.That(inventory.Coins, Is.EqualTo(4));
            Assert.That(events, Is.EqualTo(1));
            Assert.That(resource.gameObject.activeSelf, Is.False);
            Assert.That(resource.IsAvailable, Is.False);
            foreach (Renderer renderer in resource.GetComponentsInChildren<Renderer>(true))
                Assert.That(renderer.enabled, Is.False);
            foreach (Collider2D collider in resource.GetComponentsInChildren<Collider2D>(true))
                Assert.That(collider.enabled, Is.False);
            Physics2D.SyncTransforms();
            Assert.That(Physics2D.OverlapPoint(resource.transform.position), Is.Null);

            resource.Spawn(Vector3.right * 3f);
            Assert.That(resource.IsAvailable, Is.True);
            Assert.That(inventory.Coins, Is.EqualTo(4));
            resource.Interact(tool, inventory);
            Assert.That(tree ? inventory.Wood : inventory.Stone, Is.EqualTo(4));
        }

        [Test]
        public void AnimalRequiresLethalDamageAndPaysFoodOnlyOnce()
        {
            AnimalResource animal = CreateResource<AnimalResource>(3);
            IDamageable target = animal;
            animal.Interact(FarmTool.Axe, inventory);
            target.TakeDamage(0, inventory);
            target.TakeDamage(100, null);
            Assert.That(animal.CurrentHealth, Is.EqualTo(3));
            target.TakeDamage(1, inventory);
            Assert.That(inventory.Food, Is.Zero);
            Assert.That(animal.CurrentHealth, Is.EqualTo(2));
            target.TakeDamage(2, inventory);
            target.TakeDamage(2, inventory);
            Assert.That(inventory.Food, Is.EqualTo(2));
            Assert.That(inventory.Fruit, Is.Zero);
            Assert.That(target.IsAlive, Is.False);
        }

        [Test]
        public void SaveRestoresDepletionHealthAndFoodBySpawnId()
        {
            AnimalResource first = CreateResource<AnimalResource>(3);
            AnimalResource second = CreateResource<AnimalResource>(3);
            first.transform.position = Vector3.left * 2f;
            second.transform.position = Vector3.right * 2f;
            ResourceSpawnPoint firstSpawn = ResourceSpawnPoint.Attach(first);
            ResourceSpawnPoint secondSpawn = ResourceSpawnPoint.Attach(second);
            objects.Add(firstSpawn.gameObject);
            objects.Add(secondSpawn.gameObject);
            first.TakeDamage(3, inventory);
            second.TakeDamage(1, inventory);
            GameSaveSystem save = CreateObject("Save").AddComponent<GameSaveSystem>();
            save.Configure(inventory.transform, inventory, null, null, null, null, null, null, null, null);
            object state = Invoke(save, "BuildSaveData");
            string json = JsonUtility.ToJson(state);
            object restored = JsonUtility.FromJson(json, state.GetType());
            firstSpawn.Respawn();
            firstSpawn.name = "Renamed";
            firstSpawn.transform.SetAsLastSibling();
            second.TakeDamage(2, inventory);
            Invoke(save, "RestoreSaveData", restored);
            Assert.That(first.IsHarvested, Is.True);
            Assert.That(first.gameObject.activeSelf, Is.False);
            Assert.That(second.IsHarvested, Is.False);
            Assert.That(second.CurrentHealth, Is.EqualTo(2));
            Assert.That(inventory.Food, Is.EqualTo(2));
            firstSpawn.gameObject.SetActive(false);
            firstSpawn.gameObject.SetActive(true);
            firstSpawn.EnsureSpawned();
            Assert.That(first.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator AnimalInteractionUsesSwordCooldownAndBowProjectile()
        {
            PlayerToolbelt tools = inventory.gameObject.AddComponent<PlayerToolbelt>();
            PlayerMovementController movement = inventory.gameObject.AddComponent<PlayerMovementController>();
            PlayerCombatController combat = inventory.gameObject.AddComponent<PlayerCombatController>();
            AnimalResource animal = CreateResource<AnimalResource>(2);
            animal.transform.position = Vector3.right;
            Physics2D.SyncTransforms();
            tools.Select(FarmTool.Sword);
            movement.GetComponent<Rigidbody2D>().linearVelocity = Vector2.left * 3.1f;
            animal.Interact(FarmTool.Sword, inventory);
            animal.Interact(FarmTool.Sword, inventory);
            Assert.That(animal.CurrentHealth, Is.EqualTo(1));
            Assert.That(inventory.Food, Is.Zero);
            yield return new WaitForSeconds(0.5f);
            Assert.That(inventory.transform.position.sqrMagnitude, Is.LessThan(0.001f));
            tools.Select(FarmTool.Bow);
            animal.transform.position = Vector3.right * 3f;
            Physics2D.SyncTransforms();
            combat.Attack();
            Assert.That(Object.FindFirstObjectByType<ArrowProjectile>(), Is.Not.Null);
            yield return new WaitForSeconds(0.65f);
            Assert.That(animal.IsHarvested, Is.True);
            Assert.That(inventory.Food, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator OldArrowCannotDamageRespawnedAnimal()
        {
            AnimalResource animal = CreateResource<AnimalResource>(3);
            ArrowProjectile arrow = CreateObject("Arrow").AddComponent<ArrowProjectile>();
            arrow.Configure(animal, 3, inventory);
            animal.TakeDamage(3, inventory);
            animal.Spawn(Vector3.zero);
            yield return null;
            Assert.That(animal.CurrentHealth, Is.EqualTo(3));
            Assert.That(inventory.Food, Is.EqualTo(2));
        }

        [Test]
        public void SpawnPointInstantiatesOnlyOneResourceAndReusesIt()
        {
            TreeResource template = CreateResource<TreeResource>();
            template.gameObject.SetActive(false);
            ResourceSpawnPoint spawn = CreateObject("Spawn").AddComponent<ResourceSpawnPoint>();
            spawn.Configure(template, null, "test-spawn");
            spawn.EnsureSpawned();
            HarvestableResource instance = spawn.Instance;
            spawn.EnsureSpawned();
            Assert.That(spawn.Instance, Is.SameAs(instance));
            instance.Interact(FarmTool.Axe, inventory);
            spawn.EnsureSpawned();
            Assert.That(instance.IsAvailable, Is.False);
            spawn.Respawn();
            Assert.That(spawn.Instance, Is.SameAs(instance));
            Assert.That(instance.IsAvailable, Is.True);
        }

        [Test]
        public void EatingFoodRestoresLifeAndConsumesOneUnit()
        {
            PlayerSurvivalStats stats = inventory.gameObject.AddComponent<PlayerSurvivalStats>();
            stats.Restore(5, 2, 0.5f);
            inventory.AddFood(2);
            InventoryPanelSystem panel = CreateObject("Inventory").AddComponent<InventoryPanelSystem>();
            panel.Configure(null, null, inventory, null, null);
            panel.EatFood();
            Assert.That(inventory.Food, Is.EqualTo(1));
            Assert.That(stats.CurrentHealth, Is.EqualTo(4));
            Assert.That(stats.HungerPercent, Is.EqualTo(1f));
        }

        [Test]
        public void BowCannotHuntThroughLockedZoneCollider()
        {
            PlayerToolbelt tools = inventory.gameObject.AddComponent<PlayerToolbelt>();
            PlayerCombatController combat = inventory.gameObject.AddComponent<PlayerCombatController>();
            AnimalResource animal = CreateResource<AnimalResource>();
            animal.transform.position = Vector3.right * 3f;
            GameObject wall = CreateObject("Locked Zone");
            wall.transform.position = Vector3.right * 1.5f;
            wall.AddComponent<BoxCollider2D>();
            Physics2D.SyncTransforms();
            tools.Select(FarmTool.Bow);
            combat.AttackTarget(animal);
            Assert.That(Object.FindFirstObjectByType<ArrowProjectile>(), Is.Null);
            Assert.That(animal.IsHarvested, Is.False);
        }

        [Test]
        public void PooledEnemyCannotDropLootTwiceAfterDefeat()
        {
            GameObject root = CreateObject("Enemy");
            GameObject drops = CreateObject("Drops");
            root.transform.SetParent(drops.transform);
            GameObject template = CreateObject("Loot Template");
            template.SetActive(false);
            template.AddComponent<CircleCollider2D>().isTrigger = true;
            EnemyLootPickup loot = template.AddComponent<EnemyLootPickup>();
            loot.Configure(ResourceFlyweights.Item(ItemKind.Coins), 1, null);
            root.AddComponent<CircleCollider2D>();
            BasicEnemyAI enemy = root.AddComponent<BasicEnemyAI>();
            enemy.ConfigureLoot(loot);
            enemy.Configure(inventory.transform, null);
            enemy.ActivateFromPool(Vector3.right);
            enemy.TakeDamage(10, inventory);
            enemy.TakeDamage(10, inventory);
            Assert.That(inventory.Coins, Is.Zero);
            Assert.That(drops.GetComponentsInChildren<EnemyLootPickup>().Length, Is.EqualTo(1));
            Assert.That(enemy.IsAlive, Is.False);
        }

        [Test]
        public void ResourceInteractionTakesPriorityOverUnderlyingGrass()
        {
            var grid = CreateObject("Cultivation grid").AddComponent<Grid>();
            var paths = CreateObject("Farm Paths");
            paths.transform.SetParent(grid.transform);
            paths.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            var tools = inventory.gameObject.AddComponent<PlayerToolbelt>();
            TreeResource tree = CreateResource<TreeResource>();
            tree.transform.position = Vector3.right;
            FarmingPlot plot = CreateObject("Grass").AddComponent<FarmingPlot>();
            plot.transform.position = Vector3.right * 0.1f;
            FarmPlayerInteractor interactor = inventory.gameObject.AddComponent<FarmPlayerInteractor>();
            MethodInfo find = typeof(FarmPlayerInteractor).GetMethod("FindNearestInteractable", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(find.Invoke(interactor, new object[] { Vector3.zero, 1.35f }), Is.SameAs(tree));
            tree.Interact(FarmTool.Axe, inventory);
            Assert.That(find.Invoke(interactor, new object[] { Vector3.zero, 1.35f }), Is.Null, "Sword mode must not offer cultivation");
            tools.Select(FarmTool.Hoe);
            Assert.That(tools.SelectedTool, Is.EqualTo(FarmTool.Sword));
            Assert.That(find.Invoke(interactor, new object[] { Vector3.zero, 1.35f }), Is.Null);
        }

        [Test]
        public void VersionFiveSaveKeepsResourceOrderWhenAnimalsAreAdded()
        {
            TreeResource tree = CreateResource<TreeResource>();
            RockResource rock = CreateResource<RockResource>();
            AnimalResource animal = CreateResource<AnimalResource>(3);
            GameSaveSystem save = CreateObject("Save").AddComponent<GameSaveSystem>();
            save.Configure(inventory.transform, inventory, null, null, null, null, null, null, null, null);
            object data = Invoke(save, "BuildSaveData");
            data.GetType().GetField("version").SetValue(data, 5);
            FieldInfo field = data.GetType().GetField("resources");
            IList resources = (IList)field.GetValue(data);
            System.Type entryType = field.FieldType.GetGenericArguments()[0];
            foreach (bool harvested in new[] { true, false })
            {
                object entry = System.Activator.CreateInstance(entryType, true);
                entryType.GetField("harvested").SetValue(entry, harvested);
                resources.Add(entry);
            }
            Invoke(save, "RestoreSaveData", data);
            Assert.That(rock.IsHarvested, Is.True);
            Assert.That(tree.IsHarvested, Is.False);
            Assert.That(animal.IsHarvested, Is.False);
            Assert.That(inventory.Food, Is.Zero);
        }

        [Test]
        public void HudRestoresInteractionAndInventoryBindingsAfterSceneActivation()
        {
            PlayerToolbelt tools = inventory.gameObject.AddComponent<PlayerToolbelt>();
            GameObject hud = CreateObject("HUD");
            hud.SetActive(false);
            hud.AddComponent<Canvas>();
            FarmNotificationCenter center = hud.AddComponent<FarmNotificationCenter>();
            Button interact = new GameObject("Interact", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            interact.transform.SetParent(hud.transform, false);
            interact.gameObject.SetActive(false);
            Text toolName = new GameObject("Tool", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            toolName.transform.SetParent(hud.transform, false);
            Text[] counts = new Text[7];
            for (int i = 0; i < counts.Length; i++)
            {
                counts[i] = new GameObject("Count", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                counts[i].transform.SetParent(hud.transform, false);
            }
            typeof(FarmNotificationCenter).GetField("interactionButton", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(center, interact);
            typeof(FarmNotificationCenter).GetField("toolText", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(center, toolName);
            center.ConfigureGameplayBindings(inventory, tools, counts, new Image[0], new Sprite[0]);
            hud.SetActive(true);
            Camera camera = CreateObject("Camera").AddComponent<Camera>();
            camera.transform.position = Vector3.back * 10f;
            FarmNotificationCenter.SetInteractionButtonAtWorldPosition(true, "Interactuar", Vector3.zero, camera);
            Assert.That(interact.gameObject.activeSelf, Is.True);
            inventory.AddWood(2);
            Assert.That(counts[3].text, Is.EqualTo("2"));
            tools.SelectNext();
            Assert.That(toolName.text, Is.EqualTo("Arco"));
            hud.SetActive(false);
            inventory.AddWood(1);
            hud.SetActive(true);
            Assert.That(counts[3].text, Is.EqualTo("3"));
        }

        private T CreateResource<T>(int health = 1) where T : HarvestableResource
        {
            GameObject root = CreateObject(typeof(T).Name);
            root.AddComponent<CircleCollider2D>().radius = 0.25f;
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            GameObject child = new GameObject("Secondary Visual");
            child.transform.SetParent(root.transform, false);
            child.AddComponent<BoxCollider2D>().size = Vector2.one * 0.25f;
            SpriteRenderer secondary = child.AddComponent<SpriteRenderer>();
            T resource = root.AddComponent<T>();
            resource.Configure(renderer, secondary, 2, 4);
            resource.ConfigureHealth(health);
            return resource;
        }

        private GameObject CreateObject(string name)
        {
            GameObject result = new GameObject(name);
            objects.Add(result);
            return result;
        }

        private static object Invoke(object target, string method, params object[] arguments)
        {
            return target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, arguments);
        }
    }
}
