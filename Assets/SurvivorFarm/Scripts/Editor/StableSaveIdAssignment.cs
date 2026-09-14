using System;
using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivorFarm.Editor
{
    public static class StableSaveIdAssignment
    {
        [MenuItem("Tools/Survivor Farm/Persistence/Assign Stable IDs With Legacy Aliases")]
        public static void Assign()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Assign identities only in the saved scene, outside Play and Prefab modes.");
            if (SceneManager.sceneCount != 1)
                throw new InvalidOperationException("Open only the original gameplay scene to preserve legacy positional indices.");
            Scene scene = SceneManager.GetSceneAt(0);
            if (scene.isDirty || string.IsNullOrEmpty(scene.path))
                throw new InvalidOperationException("The original scene must be saved before capturing its legacy identities.");

            var plots = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<FarmingPlot>(true))
                .OrderBy(plot => plot.LegacySortKey, StringComparer.Ordinal).ToList();
            var spawns = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ResourceSpawnPoint>(true)).ToList();
            if (plots.GroupBy(plot => plot.LegacySortKey).Any(group => group.Count() > 1))
                throw new InvalidOperationException("Duplicate legacy plot sort keys prevent a reliable index migration.");
            var assignments = new List<Assignment>();
            for (int i = 0; i < plots.Count; i++) assignments.Add(new Assignment(plots[i], plots[i].PersistentId, i));
            CheckIdentities(assignments);
            var resources = spawns.Select(spawn => new Assignment(spawn, spawn.PersistentId, -1)).ToList();
            CheckIdentities(resources);
            assignments.AddRange(resources);

            Undo.IncrementCurrentGroup();
            int groupIndex = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Assign stable save identities with migration aliases");
            foreach (Assignment assignment in assignments)
            {
                StableSaveId stable = assignment.Owner.GetComponent<StableSaveId>();
                if (stable != null && !string.IsNullOrEmpty(stable.Id)) continue;
                if (stable == null) stable = Undo.AddComponent<StableSaveId>(assignment.Owner.gameObject);
                Undo.RecordObject(stable, "Preserve previous save identity");
                stable.AssignPreservingLegacy(assignment.Legacy, assignment.Index);
                EditorUtility.SetDirty(stable);
                PrefabUtility.RecordPrefabInstancePropertyModifications(stable);
            }
            Undo.CollapseUndoOperations(groupIndex);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Stable identities assigned with previous keys and plot indices. Review the scene diff before saving. No save files were modified.");
        }

        private static void CheckIdentities(List<Assignment> assignments)
        {
            var owners = new Dictionary<string, Component>(StringComparer.Ordinal);
            foreach (Assignment assignment in assignments)
            {
                var keys = new List<string> { assignment.Legacy };
                StableSaveId stable = assignment.Owner.GetComponent<StableSaveId>();
                if (stable != null && !string.IsNullOrEmpty(stable.Id))
                {
                    keys.Add(stable.Id);
                    keys.AddRange(stable.LegacyIds);
                    if (stable.LegacyIds.Length == 0)
                        throw new InvalidOperationException("Existing identity has no migration alias: " + assignment.Owner.name);
                }
                foreach (string key in keys)
                {
                    if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Missing legacy identity: " + assignment.Owner.name);
                    if (owners.TryGetValue(key, out Component previous) && previous != assignment.Owner)
                        throw new InvalidOperationException("Ambiguous save identity: " + key);
                    owners[key] = assignment.Owner;
                }
            }
        }

        private sealed class Assignment
        {
            public readonly Component Owner;
            public readonly string Legacy;
            public readonly int Index;
            public Assignment(Component owner, string legacy, int index) { Owner = owner; Legacy = legacy; Index = index; }
        }
    }
}
