using System;
using SurvivorFarm.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace SurvivorFarm.Editor
{
    /// <summary>The supplied packs use 100-pixel cells with transparent padding, not 100-pixel characters.</summary>
    public static class TinyRPGEnemyAssetBuilder
    {
        internal const string ArtRoot = "Assets/SurvivorFarm/Art/Sprites/TinyRPGCharacters/";

        [MenuItem("Tools/Survivor Farm/Build Tiny RPG Enemy Assets")]
        public static void Build()
        {
            BuildCharacter("Soldier", "TinySoldierAnimations", 6);
            BuildCharacter("Orc", "TinyOrcAnimations", 6);
            BuildCharacter("Demon_A", "TinyDemonAnimations", 7);
            BuildCharacter("Blood Monster_A", "TinyBloodMonsterAnimations", 8);
            AssetDatabase.SaveAssets();
            Debug.Log("PASS: four unique Tiny RPG enemies, with idle, walk, attack, hurt and death animations.");
        }

        private static void BuildCharacter(string character, string resource, int attackFrames)
        {
            string path = "Assets/SurvivorFarm/Resources/" + resource + ".asset";
            var library = AssetDatabase.LoadAssetAtPath<PlayerAnimationLibrary>(path);
            bool create = library == null;
            if (create) library = ScriptableObject.CreateInstance<PlayerAnimationLibrary>();
            library.name = resource;
            library.Clips = new[] {
                Clip(character, "Idle", "Idle", 6, true, 7),
                Clip(character, "Walk", "Walk", 8, true, 10),
                Clip(character, "Attack", "Attack01", attackFrames, false, 10),
                Clip(character, "Damage", "Hurt", 4, false, 16),
                Clip(character, "Dead", "Death", 4, false, 8)
            };
            if (create) AssetDatabase.CreateAsset(library, path);
            else EditorUtility.SetDirty(library);
        }

        private static PlayerAnimationLibrary.Clip Clip(string character, string state, string file, int frames, bool loop, float fps)
        {
            string path = ArtRoot + character + "/" + character + "_" + file + ".png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null || texture.width != frames * 100 || texture.height != 100)
                throw new InvalidOperationException("Missing or incorrectly sized Tiny RPG sheet: " + path);
            return new PlayerAnimationLibrary.Clip {
                Name = state, Atlas = texture, Width = 100, Height = 100, Frames = frames,
                Directions = 1, Pivot = new Vector2(.5f, .5f), FramesPerSecond = fps, Loop = loop
            };
        }
    }

    public sealed class TinyRPGEnemyTextureImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(TinyRPGEnemyAssetBuilder.ArtRoot, StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
        }
    }
}
