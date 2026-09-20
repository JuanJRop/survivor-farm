using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public static class EnemyLootTable
    {
        public static ItemKind Roll(EnemyCombatStyle style, bool elite, float roll)
        {
            float gemChance = elite ? .3f : style == EnemyCombatStyle.Demon || style == EnemyCombatStyle.BloodMonster ? .16f : .06f;
            if (roll < gemChance)
                return elite && roll < gemChance * .65f ? ItemKind.Diamond : style == EnemyCombatStyle.Legacy || style == EnemyCombatStyle.ArcherGoblin ? ItemKind.Emerald : ItemKind.Ruby;
            float experienceChance = style == EnemyCombatStyle.Legacy || style == EnemyCombatStyle.Demon ? .58f : .4f;
            return roll < gemChance + experienceChance ? ItemKind.Experience : ItemKind.Coins;
        }

        public static void Drop(Vector3 position, Transform parent, EnemyCombatStyle style, bool elite, int reward, EnemyLootPickup prefab = null)
        {
            if (style == EnemyCombatStyle.ArcherGoblin)
                EnemyLootPickup.Scatter(position, parent, ItemKind.Arrow, Random.Range(3, 7), prefab);
            int rolls = elite ? 3 : 1;
            for (int i = 0; i < rolls; i++)
            {
                ItemKind kind = Roll(style, elite, Random.value);
                int amount = kind == ItemKind.Coins ? Random.Range(Mathf.Max(1, reward / 2), Mathf.Max(3, reward + 2)) : kind == ItemKind.Experience ? Random.Range(1, elite ? 5 : 3) : 1;
                EnemyLootPickup.Scatter(position, parent, kind, amount, prefab);
            }
        }
    }
}
