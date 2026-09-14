using System;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace SurvivorFarm.Editor
{
    public static class EnemyRosterAssetBuilder
    {
        private const string Pack = "Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/";
        private const string Output = "Assets/SurvivorFarm/Resources/";

        [MenuItem("Tools/Survivor Farm/Build Enemy Animation Assets")]
        public static void Build()
        {
            BuildGoblin("Spear Goblin", "SpearGoblinAnimations", "Spear", 6);
            BuildGoblin("Archer Goblin", "ArcherGoblinAnimations", "Bow", 7);
            Save("EnemyProjectileArt", new[] {
                new PlayerAnimationLibrary.Clip { Name = "Arrow", Atlas = Texture("Character and Portrait/Character/Others/Arrow/Arrow.png"),
                    Width = 16, Height = 16, Frames = 1, Directions = 3, Pivot = new Vector2(.5f, .5f) }
            });
            Save("EnemyCampArt", new[] {
                new PlayerAnimationLibrary.Clip { Name = "Shelter", Atlas = Texture("Exterior/Beach/Tent.png"), Width = 48, Height = 48, Frames = 8, Directions = 4, Pivot = new Vector2(.5f, 0) },
                new PlayerAnimationLibrary.Clip { Name = "Fire", Atlas = Texture("Exterior/bonfire.png"), Width = 16, Height = 32, Frames = 6, Directions = 1, Pivot = new Vector2(.5f, 0), Loop = true, FramesPerSecond = 7 },
                new PlayerAnimationLibrary.Clip { Name = "Fence", Atlas = Texture("Exterior/Fence and Bridge/Fence Wood.png"), Width = 16, Height = 16, Frames = 6, Directions = 1, Pivot = new Vector2(.5f, 0) },
                new PlayerAnimationLibrary.Clip { Name = "Crate", Atlas = Texture("Exterior/Box.png"), Width = 16, Height = 16, Frames = 2, Directions = 1, Pivot = new Vector2(.5f, 0) },
                new PlayerAnimationLibrary.Clip { Name = "Chest", Atlas = Texture("Exterior/chest.png"), Width = 32, Height = 16, Frames = 8, Directions = 2, Pivot = new Vector2(.5f, 0) }
            });
            Save("SproutSlimeAnimations", new[] {
                SlimeClip("Idle", "Idle", 2, true), SlimeClip("Walk", "Walk", 4, true), SlimeClip("Attack", "Walk", 4, false),
                SlimeClip("Damage", "Damage", 4, false), SlimeClip("Dead", "Dead", 8, false)
            });
            AssetDatabase.SaveAssets();
            Debug.Log("PASS: original goblin animation libraries and arrow art generated.");
            if (Application.isBatchMode && GameSaveSystem.IsQa) EditorApplication.Exit(0);
        }

        private static void BuildGoblin(string folder, string name, string attack, int frames)
        {
            string path = "Enemy/Goblins/" + folder + "/";
            Save(name, new[] {
                Clip(path, "Idle", "Idle", 4, true, 6),
                Clip(path, "Walk", "Walk", 6, true, 9),
                Clip(path, "Attack", attack, frames, false, 9),
                Clip(path, "Damage", "Damage", 4, false, 16),
                Clip(path, "Dead", "Dead", 4, false, 8)
            });
        }

        private static PlayerAnimationLibrary.Clip Clip(string path, string name, string file, int frames, bool loop, float fps)
            => new PlayerAnimationLibrary.Clip { Name = name, Atlas = Texture(path + file + ".png"), Frames = frames,
                Width = 32, Height = 32, Directions = 3, Pivot = new Vector2(.5f, .5f), Loop = loop, FramesPerSecond = fps };

        private static Texture2D Texture(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Pack + path);
            if (texture == null) throw new InvalidOperationException("Missing original enemy atlas: " + path);
            return texture;
        }

        private static PlayerAnimationLibrary.Clip SlimeClip(string name, string file, int frames, bool loop) =>
            new PlayerAnimationLibrary.Clip { Name = name, Atlas = Texture("Enemy/Sprout Slime/Blue/" + file + ".png"),
                Width = 16, Height = 32, Frames = frames, Directions = 3, Pivot = new Vector2(.5f, .5f), Loop = loop, FramesPerSecond = 7 };

        private static void Save(string name, PlayerAnimationLibrary.Clip[] clips)
        {
            string path = Output + name + ".asset";
            var library = AssetDatabase.LoadAssetAtPath<PlayerAnimationLibrary>(path);
            bool create = library == null;
            if (create) library = ScriptableObject.CreateInstance<PlayerAnimationLibrary>();
            library.name = name;
            library.Clips = clips;
            if (create) AssetDatabase.CreateAsset(library, path);
            else EditorUtility.SetDirty(library);
        }
    }
}
