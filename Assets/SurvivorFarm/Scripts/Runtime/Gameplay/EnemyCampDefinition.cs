using System;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    [Serializable]
    public sealed class EnemyCampState
    {
        public string id;
        public Vector3 center;
        public int defeatedMask;
        public bool claimed;
    }

    public sealed class EnemyCampDefinition
    {
        public readonly string Id, Title;
        public readonly Vector3 PreferredCenter;
        public readonly EnemyCombatStyle[] Roster;
        public readonly int ShelterVariant, Coins, Iron, Food;
        public EnemyCampDefinition(string id, string title, Vector3 center, int variant, int coins, int iron, int food, params EnemyCombatStyle[] roster)
        {
            Id = id; Title = title; PreferredCenter = center; ShelterVariant = variant;
            Coins = coins; Iron = iron; Food = food; Roster = roster;
        }

        public static readonly EnemyCampDefinition[] All = {
            new EnemyCampDefinition("south", "Escondite del sur", new Vector3(-8, -16), 4, 15, 2, 2,
                EnemyCombatStyle.Legacy, EnemyCombatStyle.SpearGoblin, EnemyCombatStyle.Legacy),
            new EnemyCampDefinition("west", "Saqueadores del oeste", new Vector3(-24, -10), 5, 25, 4, 2,
                EnemyCombatStyle.SpearGoblin, EnemyCombatStyle.ArcherGoblin, EnemyCombatStyle.SpearGoblin, EnemyCombatStyle.Legacy),
            new EnemyCampDefinition("east", "Vigias del este", new Vector3(25, -10), 6, 30, 4, 3,
                EnemyCombatStyle.ArcherGoblin, EnemyCombatStyle.SpearGoblin, EnemyCombatStyle.ArcherGoblin, EnemyCombatStyle.Legacy),
            new EnemyCampDefinition("north", "Bastion del norte", new Vector3(18, 15), 7, 45, 6, 3,
                EnemyCombatStyle.SpearGoblin, EnemyCombatStyle.ArcherGoblin, EnemyCombatStyle.SpearGoblin, EnemyCombatStyle.ArcherGoblin, EnemyCombatStyle.Legacy)
        };
    }
}
