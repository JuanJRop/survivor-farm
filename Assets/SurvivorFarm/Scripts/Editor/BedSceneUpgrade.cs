using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivorFarm.Editor
{
    public static class BedSceneUpgrade
    {
        [MenuItem("Survivor Farm/Add Craftable Bed To Main")]
        public static void UpgradeMain()
        {
            EditorSceneManager.OpenScene("Assets/SurvivorFarm/Scenes/Main.unity");
            ApplyToScene();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("Craftable bed installed in Main.");
        }

        public static void ApplyToScene()
        {
            if (Object.FindFirstObjectByType<PlayerBed>() != null) return;
            var house = Object.FindFirstObjectByType<BaseHouse>();
            var owner = Object.FindFirstObjectByType<PlayerCraftingController>();
            var clock = Object.FindFirstObjectByType<DayNightCycle>();
            if (house == null || owner == null || clock == null) throw new System.InvalidOperationException("Bed requires house, player and day/night cycle.");
            var sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/Interior/Beds.png")
                .OfType<Sprite>().First(s => s.name == "Beds_0");
            var root = new GameObject("Player Bed");
            var visual = root.AddComponent<SpriteRenderer>();
            visual.sprite = sprite;
            visual.sortingOrder = 3;
            root.transform.localScale = Vector3.one * (1.25f / sprite.bounds.size.y);
            var bed = root.AddComponent<PlayerBed>();
            bed.Configure(visual, null, null);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, "Assets/SurvivorFarm/Prefabs/World/PlayerBed.prefab");
            Object.DestroyImmediate(root);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(house.transform.parent, false);
            instance.transform.position = house.transform.position + new Vector3(1.9f, 0f, 0f);
            instance.GetComponent<PlayerBed>().Configure(instance.GetComponent<SpriteRenderer>(), owner, clock);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<PlayerBed>());
        }
    }
}
