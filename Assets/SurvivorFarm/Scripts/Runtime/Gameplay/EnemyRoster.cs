using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.Core;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public enum EnemyCombatStyle { Legacy, SpearGoblin, ArcherGoblin, Soldier, Orc, Demon, BloodMonster }

    public static class EnemyRoster
    {
        public static bool IsTinyRpg(EnemyCombatStyle style) => style >= EnemyCombatStyle.Soldier && style <= EnemyCombatStyle.BloodMonster;

        public static string DisplayName(EnemyCombatStyle style)
        {
            switch (style)
            {
                case EnemyCombatStyle.Soldier: return "Soldado";
                case EnemyCombatStyle.Orc: return "Orco";
                case EnemyCombatStyle.Demon: return "Demonio";
                case EnemyCombatStyle.BloodMonster: return "Monstruo de sangre";
                case EnemyCombatStyle.ArcherGoblin: return "Goblin arquero";
                default: return "Goblin lancero";
            }
        }

        public static string LibraryName(EnemyCombatStyle style)
        {
            switch (style)
            {
                case EnemyCombatStyle.Soldier: return "TinySoldierAnimations";
                case EnemyCombatStyle.Orc: return "TinyOrcAnimations";
                case EnemyCombatStyle.Demon: return "TinyDemonAnimations";
                case EnemyCombatStyle.BloodMonster: return "TinyBloodMonsterAnimations";
                case EnemyCombatStyle.ArcherGoblin: return "ArcherGoblinAnimations";
                default: return "SpearGoblinAnimations";
            }
        }

        public static EnemyCombatStyle RaidStyle(RaidRole role, int day, int index)
        {
            if (role == RaidRole.Archer) return EnemyCombatStyle.ArcherGoblin;
            if (role == RaidRole.Brute) return day >= 3 && index % 2 != 0 ? EnemyCombatStyle.BloodMonster : EnemyCombatStyle.Orc;
            if (index % 3 == 0) return EnemyCombatStyle.Soldier;
            return day >= 2 && index % 3 == 1 ? EnemyCombatStyle.Demon : EnemyCombatStyle.SpearGoblin;
        }

        public static bool Configure(EnemyAIBase enemy, EnemyCombatStyle style, EnemyProjectilePool projectiles)
        {
            if (style == EnemyCombatStyle.Legacy) return false;
            var library = Resources.Load<PlayerAnimationLibrary>(LibraryName(style));
            if (library == null)
            {
                Debug.LogError("Missing enemy animation library: " + LibraryName(style) + ". Rebuild the enemy animation assets.", enemy);
                return false;
            }
            enemy.ConfigureCombat(style, library, projectiles);
            return true;
        }
    }
}
