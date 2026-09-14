using System;
using System.IO;
using System.Linq;
using System.Reflection;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    [InitializeOnLoad]
    public static class BackpackVerification
    {
        private const string Key = "VerifyVisualBackpack";
        private const string Output = "Design/Validation/Backpack/";
        private static double next;
        private static int stage;
        static BackpackVerification()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false)) { stage = 0; next = EditorApplication.timeSinceStartup + 2; }
                if (state == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(Key,false);
            };
        }
        private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
        private static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            const string request = "Library/VerifyBackpack.request";
            if (File.Exists(request) && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete(request); Directory.CreateDirectory(Output);
                foreach (string path in Directory.GetFiles("Assets/SurvivorFarm/Resources/BackpackIcons", "*.png"))
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                    importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.mipmapEnabled = false; importer.alphaIsTransparency = true;
                    if (path.EndsWith("Panel.png") || path.EndsWith("Slot.png")) importer.spriteBorder = Vector4.one * 5;
                    importer.SaveAndReimport();
                }
                SessionState.SetBool(Key,true); EditorApplication.isPlaying = true; return;
            }
            if (!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || next == 0 || EditorApplication.timeSinceStartup < next) return;
            try
            {
                if (stage == 0)
                {
                    Application.runInBackground = true;
                    var save = Object.FindFirstObjectByType<GameSaveSystem>();
                    var data = new SerializedObject(save); data.FindProperty("player").objectReferenceValue = null; data.ApplyModifiedPropertiesWithoutUndo(); save.enabled = false;
                    var inventory = Object.FindFirstObjectByType<PlayerInventory>();
                    inventory.Restore(250, 40, 15, 4000, 125, 30, 82, 20, 16);
                    inventory.AddItem("CarrotSeeds", 2);
                    inventory.AddItem("Carrot", 3);
                    inventory.AddItem("RubyShard", 1);
                    inventory.AddItem("FireEssence", 1);
                    Require(inventory.CommonSeeds == 250, "Old seed capacity truncated restored inventory.");
                    inventory.AddSeeds(100); Require(inventory.CommonSeeds == 350, "Seed gathering is still capped.");
                    var menu = Object.FindFirstObjectByType<InventoryPanelSystem>(); menu.Close(); menu.Toggle();
                    var view = Object.FindFirstObjectByType<VisualBackpack>();
                    Require(view.StackCount > 50, "Inventory did not grow beyond the original slots.");
                    foreach (var image in view.GetComponentsInChildren<UnityEngine.UI.Image>())
                        if (image.name == "Icono") Require(image.sprite != null, "Missing original item sprite.");
                    var cells = view.GetComponentsInChildren<BackpackCell>();
                    string moved = cells[0].Item.Key;
                    view.BeginDrag(cells[0].Item); view.Drop(cells[3].Item); view.EndDrag();
                    Require(inventory.BackpackOrder[3] == moved, "Drag ordering was not stored.");
                    var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    data.FindProperty("player").objectReferenceValue = inventory.transform; data.ApplyModifiedPropertiesWithoutUndo();
                    try
                    {
                        object state = typeof(GameSaveSystem).GetMethod("BuildSaveData", flags).Invoke(save,null);
                        string json = JsonUtility.ToJson(state);
                        inventory.BackpackOrder = new string[0]; inventory.Restore(0,0,0,0,0); inventory.RestoreItemStacks(null);
                        object roundTrip = JsonUtility.FromJson(json,state.GetType());
                        typeof(GameSaveSystem).GetMethod("RestoreSaveData",flags).Invoke(save,new[]{roundTrip});
                    }
                    finally { data.FindProperty("player").objectReferenceValue = null; data.ApplyModifiedPropertiesWithoutUndo(); }
                    Require(inventory.CommonSeeds == 350 && inventory.GetItemCount("CarrotSeeds") == 2 && inventory.GetItemCount("Carrot") == 3 && inventory.BackpackOrder[3] == moved, "Save roundtrip lost counts, catalog items or slot order.");
                    view.Refresh(); view.SetCategory(1); Require(view.VisibleCount == 0,"Misc category should start empty.");
                    view.SetCategory(2); Require(view.VisibleCount == 0,"Weapons leaked into the normal backpack.");
                    view.SetCategory(3); Require(view.VisibleCount == 3,"Food category mismatch.");
                    view.SetCategory(5); Require(view.VisibleCount == 7,"Seed category mismatch.");
                    view.SetCategory(6); Require(view.VisibleCount == 1,"Gem category mismatch.");
                    view.SetCategory(7); Require(view.VisibleCount == 1,"Element category mismatch.");
                    var equipmentWindow = Object.FindFirstObjectByType<PlayerEquipmentWindow>();
                    Require(equipmentWindow != null, "Missing character equipment window.");
                    Require(!inventory.Equip("Helmet",0), "An unowned helmet could be equipped.");
                    inventory.AddEquipment("Helmet"); inventory.AddEquipment("Boots"); inventory.AddEquipment("Ring");
                    Require(!inventory.Equip("Helmet",2), "Helmet fitted into boots slot.");
                    Require(inventory.Equip("Helmet",0) && inventory.Equip("Boots",2), "Armor slots rejected compatible owned items.");
                    Require(inventory.Equip("Ring",6) && inventory.Equip("Ring",7) && string.IsNullOrEmpty(inventory.EquippedEquipment[6]), "Accessory was duplicated across slots.");
                    Require(inventory.Equip("Bow",3) && inventory.GetComponent<PlayerToolbelt>().SelectedTool == SurvivorFarm.Runtime.Gameplay.FarmTool.Bow, "Weapon equip did not select combat weapon.");
                    data.FindProperty("player").objectReferenceValue = inventory.transform; data.ApplyModifiedPropertiesWithoutUndo();
                    try
                    {
                        object state = typeof(GameSaveSystem).GetMethod("BuildSaveData", flags).Invoke(save,null);
                        string json = JsonUtility.ToJson(state);
                        inventory.RestoreEquipment(null,null);
                        typeof(GameSaveSystem).GetMethod("RestoreSaveData",flags).Invoke(save,new[]{JsonUtility.FromJson(json,state.GetType())});
                        Require(inventory.EquippedEquipment[0] == "Helmet" && inventory.EquippedEquipment[7] == "Ring", "Equipment did not survive save roundtrip.");
                    }
                    finally { data.FindProperty("player").objectReferenceValue=null;data.ApplyModifiedPropertiesWithoutUndo(); }
                    inventory.RestoreEquipment(null,null);
                    inventory.GetComponent<PlayerToolbelt>().Select(SurvivorFarm.Runtime.Gameplay.FarmTool.Sword);
                    equipmentWindow.Open(); Require(PlayerEquipmentWindow.IsOpen && InventoryPanelSystem.IsOpen, "Equipment does not block gameplay input.");
                    menu.Toggle(); Require(!PlayerEquipmentWindow.IsOpen, "Backpack and equipment windows overlap.");
                    view.SetCategory(0);
                    Require(!view.GetComponentsInChildren<BackpackCell>().Any(c => c.Item.Tool == SurvivorFarm.Runtime.Gameplay.FarmTool.Sword || c.Item.Tool == SurvivorFarm.Runtime.Gameplay.FarmTool.Bow), "Weapon remained visible in backpack.");
                    var scroll = view.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
                    scroll.verticalNormalizedPosition = 0;
                    Canvas.ForceUpdateCanvases();
                    scroll.onValueChanged.Invoke(Vector2.zero);
                    Require(view.GetComponentsInChildren<BackpackCell>().Any(c => c.Item.Icon == "Food"), "Scrolling did not reveal the last items.");
                    // A compact sample for the screenshot after checking a backpack with 58 stacks.
                    inventory.Restore(250, 40, 15, 180, 125, 30, 82, 20, 16);
                    inventory.GetComponent<PlayerSurvivalStats>().Restore(5,5,1);
                    view.SetCategory(0);
                    view.Select(view.GetComponentsInChildren<BackpackCell>()[0].Item);
                }
                else if (stage == 1) ScreenCapture.CaptureScreenshot(Output + "mochila.png");
                else if (stage == 2) Object.FindFirstObjectByType<PlayerEquipmentWindow>().Open();
                else if (stage == 3) ScreenCapture.CaptureScreenshot(Output + "equipamiento.png");
                else
                {
                    File.WriteAllText(Output + "result.txt", "PASS\nOriginal sprite icons loaded. Dynamic stacks beyond 50 slots. Drag reorder. Categories. Save JSON roundtrip preserves 350 seeds and slot order. Equipment ownership, slot compatibility, accessory uniqueness, weapon activation and equipment JSON roundtrip verified. Separate windows and no weapons in backpack. Test data excluded from disk saves.");
                    EditorApplication.isPlaying = false; SessionState.SetBool(Key,false);
                }
                stage++; next = EditorApplication.timeSinceStartup + .8;
            }
            catch (Exception e)
            {
                File.WriteAllText(Output + "result.txt", "FAIL\n" + e); EditorApplication.isPlaying = false; SessionState.SetBool(Key,false);
            }
        }
    }
}
