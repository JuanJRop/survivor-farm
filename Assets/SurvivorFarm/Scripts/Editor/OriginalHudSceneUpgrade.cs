using System;
using System.IO;
using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SurvivorFarm.Editor
{
    [InitializeOnLoad]
    public static class OriginalHudSceneUpgrade
    {
        private const string SpriteRoot = "Assets/SurvivorFarm/UI/OriginalSprites/";
        private const string Request = "Library/ApplyOriginalHud.request";
        private static Font font;
        private static Sprite panel;
        private static readonly Color Ink = new Color32(71, 42, 41, 255);
        private static readonly Color HudInk = new Color(1f, .93f, .76f, 1f);
        private static readonly Color HudPanel = new Color(.045f, .055f, .038f, .95f);
        private static readonly Color HudSubPanel = new Color(.12f, .075f, .045f, .96f);
        private static readonly Color HudAccent = new Color(1f, .73f, .28f, 1f);

        static OriginalHudSceneUpgrade() => EditorApplication.update += ProcessRequest;

        private static void ProcessRequest()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            try { Apply(); File.WriteAllText("Library/OriginalHud-result.txt", "SUCCESS: original sprite HUD and player/camera placement saved."); }
            catch (Exception error) { File.WriteAllText("Library/OriginalHud-result.txt", error.ToString()); Debug.LogException(error); }
        }

        [MenuItem("Survivor Farm/Apply Original Sprite HUD")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play before editing the HUD.");
            if (SceneManager.GetActiveScene().path != "Assets/SurvivorFarm/Scenes/Main.unity")
                throw new InvalidOperationException("Open the Survivor Farm Main scene before applying the HUD.");
            var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First(c => c.name == "Farm HUD");
            if (canvas.GetComponentInChildren<OriginalSpriteHud>(true) != null)
            {
                var existingHud = canvas.GetComponentInChildren<OriginalSpriteHud>(true);
                existingHud.ModalPanels = new[] { "Full Inventory Panel", "Options Panel", "Shop Panel" }
                    .Select(n => Descendant(canvas.transform, n) as RectTransform).Where(t => t != null).ToArray();
                EditorUtility.SetDirty(existingHud);
                ApplyPlacement();
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                return;
            }
            foreach (string path in Directory.GetFiles(SpriteRoot, "*.png"))
            {
                string assetPath = path.Replace('\\', '/');
                var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                if (importer == null) { AssetDatabase.ImportAsset(assetPath); importer = (TextureImporter)AssetImporter.GetAtPath(assetPath); }
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                string name = Path.GetFileNameWithoutExtension(path);
                importer.spriteBorder = name == "Panel" ? Vector4.one * 6 : name == "Slot" ? Vector4.one * 5 : Vector4.zero;
                importer.SaveAndReimport();
            }
            panel = S("Panel");
            font = AssetDatabase.LoadAssetAtPath<Font>("SurvivorFarm/Assets/Project/UnityDefault/TextMesh Pro/Fonts/LiberationSans.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(480, 854);
            scaler.matchWidthOrHeight = 0;
            var toolbelt = Object.FindFirstObjectByType<PlayerToolbelt>();
            var inventory = toolbelt.GetComponent<PlayerInventory>();
            var stats = toolbelt.GetComponent<PlayerSurvivalStats>();
            var center = Object.FindFirstObjectByType<FarmNotificationCenter>();
            var inventoryMenu = Object.FindFirstObjectByType<InventoryPanelSystem>();
            var options = Object.FindFirstObjectByType<MobileOptionsMenu>();
            Transform quest = Descendant(canvas.transform, "Quest Text");
            foreach (string name in new[] { "Survival HUD", "Mobile Weapon Selector", "Inventory Slots", "Quest Panel", "Inventory Button", "Attack Button", "Interact Button", "Options Button", "Prompt", "Notification" })
                Descendant(canvas.transform, name)?.gameObject.SetActive(false);

            RectTransform root = Rect(canvas.transform, "Original Sprite HUD");
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            root.SetAsFirstSibling();
            var hud = root.gameObject.AddComponent<OriginalSpriteHud>();
            hud.Toolbelt = toolbelt; hud.Inventory = inventory; hud.Stats = stats;
            hud.Clock = Object.FindFirstObjectByType<DayNightCycle>();
            hud.Panel = panel; hud.Slot = S("Slot"); hud.FullHeart = S("Heart"); hud.EmptyHeart = S("EmptyHeart");

            RectTransform health = Box(root, "Vitals", 12, 12, 308, 92);
            hud.Hearts = new Image[8];
            for (int i = 0; i < 8; i++) hud.Hearts[i] = Icon(health, "Health " + i, S("Heart"), 12 + i * 30, 10, 28, 28);
            Icon(health, "Hunger Icon", R("Food"), 12, 48, 24, 24);
            hud.HungerLabel = Label(health, "Hunger", "Hambre 100%", 44, 49, 92, 24, 14);
            hud.HungerLabel.color = new Color(.78f, .92f, .60f, 1f);
            var hungerFrame = Icon(health, "Hunger Frame", S("HungerFrame"), 140, 50, 154, 22);
            hungerFrame.color = new Color(.95f, .79f, .55f, .98f);
            hud.HungerFill = Icon(hungerFrame.transform, "Hunger Fill", S("HungerFill"), 4, 5, 145, 12);
            hud.HungerFill.type = Image.Type.Filled; hud.HungerFill.fillMethod = Image.FillMethod.Horizontal; hud.HungerFill.fillOrigin = 0;

            RectTransform wallet = Box(root, "Day and Wallet", 12, 12, 154, 84, true);
            hud.Day = Label(wallet, "Day", "DÍA 1", 16, 12, 126, 27, 17);
            Icon(wallet, "Coin", S("Coin"), 20, 42, 30, 30);
            hud.Coins = Label(wallet, "Coins", "0", 58, 44, 82, 28, 20);
            RectTransform mission = Box(root, "Mission", 12, 104, 384, 126, true);
            Icon(mission, "Quest Icon", S("Quest"), 12, 12, 32, 32);
            if (quest != null)
            {
                quest.SetParent(mission, false); quest.gameObject.SetActive(true);
                Place((RectTransform)quest, 52, 10, 318, 108);
                StyleText(quest.GetComponent<Text>(), 15);
                quest.GetComponent<Text>().color = HudInk;
                quest.GetComponent<Text>().resizeTextForBestFit = true;
                quest.GetComponent<Text>().resizeTextMinSize = 11;
                quest.GetComponent<Text>().resizeTextMaxSize = 15;
                AddHudOutline(quest.GetComponent<Text>());
            }
            var settings = MakeButton(root, "HUD Settings", "Ajustes", 12, 240, 116, 38, true);
            Place(settings.GetComponentInChildren<Text>().rectTransform, 42, 4, 66, 30);
            Icon(settings.transform, "HUD Icon", R("Workbench"), 10, 7, 24, 24);
            UnityEventTools.AddPersistentListener(settings.onClick, options.TogglePanel);

            RectTransform toolbar = Box(root, "Tools", 0, 12, 260, 80, false, true);
            toolbar.anchorMin = toolbar.anchorMax = toolbar.pivot = new Vector2(.5f, 0);
            hud.ToolButtons = new Button[(int)FarmTool.Hoe + 1]; hud.ToolFrames = new Image[(int)FarmTool.Hoe + 1];
            string[] names = { "Sword", "Bow", "Hoe" };
            Sprite[] tools = names.Select(S).ToArray();
            FarmTool[] mappedTools = { FarmTool.Sword, FarmTool.Bow, FarmTool.Hoe };
            for (int i = 0; i < names.Length; i++)
            {
                var button = MakeButton(toolbar, "Select " + names[i], "", 18 + i * 78, 11, 62, 58);
                button.image.sprite = hud.Slot;
                Icon(button.transform, "Tool Icon", tools[i], 12, 6, 38, 38);
                Label(button.transform, "Shortcut", (i + 1).ToString(), 5, 39, 20, 13, 11).color = HudAccent;
                int toolIndex = (int)mappedTools[i];
                hud.ToolButtons[toolIndex] = button; hud.ToolFrames[toolIndex] = button.image;
            }
            RectTransform selected = Box(root, "Selected Tool", 0, 98, 192, 30, false, true);
            selected.anchorMin = selected.anchorMax = new Vector2(.5f, 0); selected.pivot = new Vector2(.5f, 0);
            hud.SelectedTool = Label(selected, "Tool Name", "", 8, 3, 176, 24, 15, TextAnchor.MiddleCenter);
            var backpack = MakeButton(root, "HUD Backpack", "Mochila", 16, 76, 104, 48, false, true);
            Place(backpack.GetComponentInChildren<Text>().rectTransform, 42, 3, 54, 42);
            Icon(backpack.transform, "Backpack Icon", S("Backpack"), 10, 10, 25, 25);
            UnityEventTools.AddPersistentListener(backpack.onClick, inventoryMenu.Toggle);
            var attack = MakeButton(root, "HUD Attack", "Atacar", 18, 194, 76, 68, true, true);
            Place(attack.GetComponentInChildren<Text>().rectTransform, 4, 45, 68, 18);
            Icon(attack.transform, "Sword Icon", S("Sword"), 22, 7, 32, 32);
            UnityEventTools.AddPersistentListener(attack.onClick, toolbelt.GetComponent<PlayerCombatController>().Attack);
            var cooldown = Icon(attack.transform, "Cooldown", panel, 0, 0, 76, 68);
            cooldown.color = new Color(0, 0, 0, .2f); cooldown.type = Image.Type.Filled; cooldown.fillMethod = Image.FillMethod.Horizontal;
            toolbelt.GetComponent<PlayerCombatController>().ConfigureAttackUi(attack.GetComponentInChildren<Text>(), cooldown);
            var interact = MakeButton(root, "HUD Interact", "Interactuar", 18, 110, 128, 60, true, true);
            Place(interact.GetComponentInChildren<Text>().rectTransform, 8, 36, 112, 20);
            interact.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            interact.GetComponentInChildren<Text>().resizeTextMinSize = 10; interact.GetComponentInChildren<Text>().resizeTextMaxSize = 14;
            Icon(interact.transform, "Hand Icon", S("Hand"), 50, 5, 28, 28);
            UnityEventTools.AddPersistentListener(interact.onClick, toolbelt.GetComponent<FarmPlayerInteractor>().PerformInteraction);
            RectTransform promptPanel = Box(root, "Context", 16, 132, 320, 54, false, true);
            promptPanel.GetComponent<Image>().color = HudSubPanel;
            Text prompt = Label(promptPanel, "Context Text", "Acércate a un objeto para interactuar.", 12, 6, 296, 42, 14);
            Text notification = Label(root, "Toast", "", 12, 174, 456, 42, 16, TextAnchor.MiddleCenter);
            center.ConfigureFloatingInteraction(interact, interact.GetComponentInChildren<Text>());
            FarmNotificationCenter.Bind(prompt, hud.SelectedTool, interact.GetComponentInChildren<Text>(), interact, notification,
                null, new Text[0], new Image[0], hud.Hearts, hud.HungerFill, hud.HungerLabel);
            center.ConfigureGameplayBindings(inventory, toolbelt, new Text[0], new Image[0],
                System.Array.ConvertAll((FarmTool[])System.Enum.GetValues(typeof(FarmTool)), tool => S(tool.ToString())));
            center.ConfigureWeaponFrames(hud.ToolFrames[(int)FarmTool.Sword], hud.ToolFrames[(int)FarmTool.Bow], hud.ToolFrames[(int)FarmTool.Hoe]);
            hud.Refresh();

            foreach (string name in new[] { "Full Inventory Panel", "Options Panel", "Shop Panel" })
            {
                Transform modal = Descendant(canvas.transform, name);
                if (modal == null) continue;
                RectTransform mr = (RectTransform)modal;
                mr.anchorMin = mr.anchorMax = new Vector2(.5f, .5f); mr.pivot = new Vector2(.5f, .5f); mr.anchoredPosition = Vector2.zero;
                foreach (Image img in modal.GetComponentsInChildren<Image>(true)) { img.sprite = panel; img.type = Image.Type.Sliced; img.color = Color.white; }
                foreach (Text label in modal.GetComponentsInChildren<Text>(true)) StyleText(label, label.fontSize);
            }
            Wire(Descendant(canvas.transform, "Close Inventory"), inventoryMenu.Close);
            Wire(Descendant(canvas.transform, "Eat Food"), inventoryMenu.EatFood);
            Wire(Descendant(canvas.transform, "Close Options"), options.TogglePanel);
            hud.ModalPanels = new[] { "Full Inventory Panel", "Options Panel", "Shop Panel" }
                .Select(n => Descendant(canvas.transform, n) as RectTransform).Where(t => t != null).ToArray();
            ApplyPlacement();
            EditorUtility.SetDirty(center); EditorUtility.SetDirty(hud);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Original sprite HUD installed; player (-0.47, 0.83, 0), camera (-0.47, 0.83, -10).");
        }

        private static void ApplyPlacement()
        {
            var player = Object.FindFirstObjectByType<PlayerToolbelt>().transform;
            player.position = new Vector3(-.47f, .83f, 0); player.rotation = Quaternion.identity; player.localScale = Vector3.one;
            var body = player.GetComponent<Rigidbody2D>(); if (body != null) body.position = player.position;
            var camera = Camera.main; camera.transform.rotation = Quaternion.identity; camera.transform.localScale = Vector3.one;
            var follow = camera.GetComponent<CameraFollowTarget>() ?? camera.gameObject.AddComponent<CameraFollowTarget>();
            follow.SetTarget(player);
            camera.transform.position = new Vector3(-.47f, .83f, -10);
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);
            var save = Object.FindFirstObjectByType<GameSaveSystem>();
            var data = new SerializedObject(save); data.FindProperty("restorePlayerLocation").boolValue = false; data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(player); EditorUtility.SetDirty(camera); EditorUtility.SetDirty(follow);
        }

        private static Sprite S(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + name + ".png");
        private static Sprite R(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SurvivorFarm/Resources/BackpackIcons/" + name + ".png") ?? S(name);
        private static Transform Descendant(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
        private static void Wire(Transform target, UnityAction action)
        {
            if (target == null) return;
            var button = target.GetComponent<Button>();
            if (button != null && button.onClick.GetPersistentEventCount() == 0) UnityEventTools.AddPersistentListener(button.onClick, action);
        }
        private static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return (RectTransform)go.transform;
        }
        private static void Place(RectTransform rect, float x, float y, float w, float h, bool right = false, bool bottom = false)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(right ? 1 : 0, bottom ? 0 : 1);
            rect.anchoredPosition = new Vector2(right ? -x : x, bottom ? y : -y); rect.sizeDelta = new Vector2(w, h);
        }
        private static RectTransform Box(Transform parent, string name, float x, float y, float w, float h, bool right = false, bool bottom = false)
        {
            var rect = Rect(parent, name); Place(rect, x, y, w, h, right, bottom);
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = panel; image.type = Image.Type.Sliced; image.raycastTarget = false;
            image.color = HudPanel;
            return rect;
        }
        private static Image Icon(Transform parent, string name, Sprite sprite, float x, float y, float w, float h)
        {
            var rect = Rect(parent, name); Place(rect, x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = sprite; image.raycastTarget = false; return image;
        }
        private static void StyleText(Text text, int size) { text.font = font; text.fontSize = size; text.color = Ink; text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; }
        private static Text Label(Transform parent, string name, string value, float x, float y, float w, float h, int size, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var rect = Rect(parent, name); Place(rect, x, y, w, h);
            var text = rect.gameObject.AddComponent<Text>(); StyleText(text, size); text.color = HudInk; text.fontStyle = FontStyle.Bold; text.alignment = alignment; text.text = value; AddHudOutline(text); return text;
        }
        private static Button MakeButton(Transform parent, string name, string caption, float x, float y, float w, float h, bool right = false, bool bottom = false)
        {
            var rect = Box(parent, name, x, y, w, h, right, bottom); var image = rect.GetComponent<Image>(); image.raycastTarget = true;
            image.color = HudSubPanel;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            Label(rect, "Label", caption, 4, 4, w - 8, h - 8, 14, TextAnchor.MiddleCenter);
            return button;
        }

        private static void AddHudOutline(Text text)
        {
            if (text == null || text.GetComponent<Outline>() != null) return;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.04f, .025f, .015f, .92f);
            outline.effectDistance = new Vector2(1.25f, -1.25f);
            outline.useGraphicAlpha = true;
        }
    }
}
