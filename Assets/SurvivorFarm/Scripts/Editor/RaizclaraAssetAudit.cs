using System;
using System.IO;
using System.Text;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEditor;
using UnityEngine;

namespace SurvivorFarm.Editor
{
    public static class RaizclaraAssetAudit
    {
        public static void Run()
        {
            if (!GameSaveSystem.IsQa) throw new InvalidOperationException("Asset audit is restricted to isolated --qa runs.");
            Directory.CreateDirectory("Design/Validation/TeamAssets");
            var report = new StringBuilder();
            string[] scripts = {
                "Assets/SurvivorFarm/Scripts/Runtime/Player/PlayerAnimationLibrary.cs",
                "Assets/SurvivorFarm/Scripts/Runtime/Gameplay/FlyweightCatalog.cs",
                "Assets/SurvivorFarm/Scripts/Runtime/Gameplay/CombatFeelVisuals.cs",
                "Assets/SurvivorFarm/Scripts/Runtime/Gameplay/VillageNpcArtCatalog.cs"
            };
            foreach (string path in scripts)
            {
                var before = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                report.AppendLine(path + " before=" + (before != null ? before.GetClass()?.AssemblyQualifiedName ?? "NO CLASS" : "NO SCRIPT"));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                var after = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                report.AppendLine("after=" + (after != null ? after.GetClass()?.AssemblyQualifiedName ?? "NO CLASS" : "NO SCRIPT"));
            }
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/SurvivorFarm" }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            foreach (string path in Directory.GetFiles("Assets/SurvivorFarm/Resources", "*.asset"))
                AssetDatabase.ImportAsset(path.Replace('\\', '/'), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            bool animation = Resources.Load<PlayerAnimationLibrary>("JoshAnimationLibrary") != null;
            bool arrow = CombatFeelVisuals.Arrow != null;
            bool villagers = Resources.Load<VillageNpcArtCatalog>("VillageNpcArt") != null;
            report.AppendLine("Animation=" + animation + " Arrow=" + arrow + " Villagers=" + villagers);
            File.WriteAllText("Design/Validation/TeamAssets/result.txt", report.ToString());
            Debug.Log(report.ToString());
            EditorApplication.Exit(animation && arrow && villagers ? 0 : 1);
        }
    }
}
