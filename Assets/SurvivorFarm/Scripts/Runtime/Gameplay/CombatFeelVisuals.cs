using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class CombatFeelVisuals : ScriptableObject
    {
        [SerializeField] private Sprite arrow;
        private static CombatFeelVisuals shared;
        public static Sprite Arrow
        {
            get
            {
                if (shared == null) shared = Resources.Load<CombatFeelVisuals>("CombatFeelVisuals");
                return shared != null ? shared.arrow : null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() => shared = null;
    }
}
