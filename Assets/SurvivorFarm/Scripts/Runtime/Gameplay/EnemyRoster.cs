using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public enum EnemyCombatStyle { Legacy, SpearGoblin, ArcherGoblin }

    public static class EnemyRoster
    {
        public static bool Configure(EnemyAIBase enemy, EnemyCombatStyle style, EnemyProjectilePool projectiles)
        {
            if (style == EnemyCombatStyle.Legacy) return false;
            var library = Resources.Load<PlayerAnimationLibrary>(style == EnemyCombatStyle.ArcherGoblin ? "ArcherGoblinAnimations" : "SpearGoblinAnimations");
            if (library == null)
            {
                Debug.LogError("Missing goblin animation library. Run Tools/Survivor Farm/Build Enemy Animation Assets.", enemy);
                return false;
            }
            enemy.ConfigureCombat(style, library, projectiles);
            return true;
        }
    }
}
