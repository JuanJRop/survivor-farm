using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SurvivorFarm.Runtime.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivorFarm.Editor
{
    [InitializeOnLoad]
    public static class PlayerAnimationUpgrade
    {
        public const string Root = "Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/Character and Portrait/Character/Pre-made/Josh/";
        public const string LibraryPath = "Assets/SurvivorFarm/Resources/JoshAnimationLibrary.asset";
        static PlayerAnimationUpgrade() => EditorApplication.update += Tick;
        private static void Tick()
        {
            const string request = "Library/ApplyPlayerAnimations.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { Apply(); File.WriteAllText("Library/PlayerAnimations-result.txt", "PASS: complete Josh library saved to scene and prefab."); }
            catch (Exception e) { File.WriteAllText("Library/PlayerAnimations-result.txt", e.ToString()); Debug.LogException(e); }
        }
        [MenuItem("Survivor Farm/Apply Complete Player Animations")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != "Assets/SurvivorFarm/Scenes/Main.unity") throw new InvalidOperationException("Open Main outside Play.");
            Directory.CreateDirectory("Assets/SurvivorFarm/Resources"); AssetDatabase.Refresh();
            var library = AssetDatabase.LoadAssetAtPath<PlayerAnimationLibrary>(LibraryPath);
            if (library == null) { library = ScriptableObject.CreateInstance<PlayerAnimationLibrary>(); AssetDatabase.CreateAsset(library, LibraryPath); }
            var clips = new List<PlayerAnimationLibrary.Clip>();
            foreach (string path in Directory.GetFiles(Root, "*.png", SearchOption.AllDirectories).OrderBy(p => p))
            {
                string relative = path.Replace('\\','/').Substring(Root.Length);
                // These are detachable overlays, color swatches or props, not separate character actions.
                if (relative.Contains("Colors/") || relative.Contains("no Horse") || relative.Contains("Shadow") || relative == "Fishing/Fish.png") continue;
                string key = relative.Substring(0, relative.Length - 4);
                if (key == "Bow and Arrow") key = "Bow";
                if (key == "Throwing items") key = "Plant";
                if (key == "Pick Up Itens/Pick Up Itens") key = "PickUp";
                var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(path.Replace('\\','/'));
                if (atlas == null) throw new InvalidOperationException("Missing atlas: " + path);
                bool large = relative.StartsWith("Fishing/") || relative.StartsWith("Pick Up Itens/");
                int width = large ? 64 : 32, height = relative.StartsWith("Horse/") ? 48 : width;
                var clip = new PlayerAnimationLibrary.Clip { Name = key, Atlas = atlas, Width = width, Height = height,
                    Frames = atlas.width / width, Directions = atlas.height / height,
                    FramesPerSecond = key == "Idle" || key.Contains("Idle") || key == "Sleep" ? 5 : key == "Sword" ? 22 : key == "Bow" ? 16 : key == "Walk" ? 9 : 12,
                    Loop = key == "Idle" || key == "Walk" || key == "Run" || key.Contains("Idle") || key.Contains("Swim") || key.Contains("Run") || key.Contains("Walk") || key == "Sleep" };
                if (relative.StartsWith("Horse/")) clip.Pivot = new Vector2(.5f,.25f);
                if (key == "Horse/Horse Idle" || key == "Sitting")
                { clip.HorizontalDirections = true; clip.Directions = 3; clip.Frames /= 3; }
                if (clip.Frames < 1 || clip.Directions < 1 || clip.Directions > 3) throw new InvalidOperationException("Unexpected sheet dimensions: " + relative);
                clips.Add(clip);
            }
            library.Clips = clips.ToArray(); EditorUtility.SetDirty(library); AssetDatabase.SaveAssets();
            const string prefabPath = "Assets/SurvivorFarm/Prefabs/Characters/Player.prefab";
            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try { Bind(prefab.GetComponent<PlayerCharacterAnimator>(), library); PrefabUtility.SaveAsPrefabAsset(prefab,prefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            foreach (var animator in UnityEngine.Object.FindObjectsByType<PlayerCharacterAnimator>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                Bind(animator,library);
                if (PrefabUtility.IsPartOfPrefabInstance(animator)) PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Directory.CreateDirectory("Design/Validation/PlayerAnimations");
            File.WriteAllLines("Design/Validation/PlayerAnimations/clips.tsv", new[] { "Clip\tFrames per direction\tDirections\tAtlas" }
                .Concat(clips.Select(c => c.Name + "\t" + c.Frames + "\t" + c.Directions + "\t" + AssetDatabase.GetAssetPath(c.Atlas))));
        }
        private static void Bind(PlayerCharacterAnimator animator, PlayerAnimationLibrary library)
        {
            if (animator == null) throw new InvalidOperationException("Missing player animator.");
            var renderer = animator.GetComponent<SpriteRenderer>();
            animator.SetLibrary(library,renderer);
            // Store an imported idle sprite in edit mode; animation sprites are cached at runtime.
            var idle = AssetDatabase.LoadAllAssetsAtPath(Root + "Idle.png").OfType<Sprite>().FirstOrDefault(s => s.name == "Idle_0");
            if (idle != null) renderer.sprite = idle;
            EditorUtility.SetDirty(animator); EditorUtility.SetDirty(renderer);
            if (PrefabUtility.IsPartOfPrefabInstance(renderer)) PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
    }

    [CustomEditor(typeof(PlayerCharacterAnimator))]
    public sealed class PlayerCharacterAnimatorInspector : UnityEditor.Editor
    {
        private int selected, direction;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector(); var animator = (PlayerCharacterAnimator)target;
            if (animator.Library == null) return;
            var clips = animator.Library.Clips; if (clips.Length == 0) return;
            selected = EditorGUILayout.Popup("Animation", Mathf.Clamp(selected,0,clips.Length-1), clips.Select(c=>c.Name).ToArray());
            direction = GUILayout.Toolbar(direction,new[] { "Front", "Back", "Side" });
            var clip = clips[selected]; int frame = (int)(EditorApplication.timeSinceStartup * clip.FramesPerSecond) % clip.Frames;
            Sprite sprite = animator.Library.Frame(clip,direction,frame);
            Rect area = GUILayoutUtility.GetRect(192,192); Rect textureRect = sprite.rect;
            GUI.DrawTextureWithTexCoords(area,clip.Atlas,new Rect(textureRect.x/clip.Atlas.width,textureRect.y/clip.Atlas.height,textureRect.width/clip.Atlas.width,textureRect.height/clip.Atlas.height),true);
            EditorGUILayout.LabelField(clip.Frames + " frames / direction · " + clip.Directions + " directions");
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
                if (GUILayout.Button("Play on player (Play mode)")) animator.PlayAction(clip.Name,2);
            Repaint();
        }
    }
}
