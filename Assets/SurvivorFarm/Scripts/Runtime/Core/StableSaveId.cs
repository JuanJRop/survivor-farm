using System;
using UnityEngine;

namespace SurvivorFarm.Runtime.Core
{
    [DisallowMultipleComponent]
    public sealed class StableSaveId : MonoBehaviour
    {
        [SerializeField, HideInInspector] private string id;
        [SerializeField, HideInInspector] private string[] legacyIds = Array.Empty<string>();
        [SerializeField, HideInInspector] private int legacyPlotIndex = -1;

        public string Id => id;
        public string[] LegacyIds => (string[])(legacyIds ?? Array.Empty<string>()).Clone();
        public int LegacyPlotIndex => legacyPlotIndex;

        public string Resolve(string legacyId) => string.IsNullOrEmpty(id) ? legacyId : id;
        public bool Matches(string savedId, string legacyId) => !string.IsNullOrEmpty(savedId) &&
            (savedId == Resolve(legacyId) || savedId == legacyId || Array.IndexOf(legacyIds ?? Array.Empty<string>(), savedId) >= 0);

        public static string Resolve(Component owner, string legacyId) =>
            owner != null && owner.TryGetComponent<StableSaveId>(out var stable) ? stable.Resolve(legacyId) : legacyId;

        public static bool Matches(Component owner, string savedId, string legacyId) =>
            owner != null && owner.TryGetComponent<StableSaveId>(out var stable) ? stable.Matches(savedId, legacyId) :
            !string.IsNullOrEmpty(savedId) && savedId == legacyId;

        public static bool Matches(Component owner, string savedId) => owner != null &&
            owner.TryGetComponent<StableSaveId>(out var stable) && stable.Matches(savedId, null);

#if UNITY_EDITOR
        // Explicit assignment only: Awake/OnValidate must never invent or change IDs.
        public void AssignPreservingLegacy(string legacyId, int plotIndex = -1)
        {
            if (string.IsNullOrWhiteSpace(legacyId)) throw new ArgumentException("A previous identity is required.", nameof(legacyId));
            if (!string.IsNullOrEmpty(id)) return;
            legacyIds = new[] { legacyId };
            legacyPlotIndex = plotIndex;
            id = "stable:" + Guid.NewGuid().ToString("N");
        }
#endif
    }
}
