using System;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    [Serializable]
    public sealed class ComboAttack
    {
        public string label = "Corte";
        [Min(.15f)] public float duration = .42f;
        [Range(.15f, .8f)] public float impactFraction = .42f;
        [Min(1f)] public float damageMultiplier = 1f;
        [Min(0)] public int bonusDamage;
        public bool heavy;
    }

    /// <summary>Weapon-specific timing and damage; no input, physics or presentation logic.</summary>
    [CreateAssetMenu(menuName = "Survivor Farm/Combat/Melee combo")]
    public sealed class MeleeComboDefinition : ScriptableObject
    {
        [Min(.1f)] public float continuationWindow = .6f;
        [Range(0f, .25f)] public float inputBuffer = .16f;
        public ComboAttack chargedAttack = new ComboAttack { label="Descarga",duration=.66f,
            impactFraction=.34f,damageMultiplier=3f,bonusDamage=2,heavy=true };
        [Range(1f,2f)] public float chargedRangeMultiplier=1.3f;
        public ComboAttack[] attacks =
        {
            new ComboAttack(),
            new ComboAttack { label = "Retorno", duration = .38f, impactFraction = .4f },
            new ComboAttack { label = "Remate", duration = .58f, impactFraction = .52f,
                damageMultiplier = 2f, bonusDamage = 1, heavy = true }
        };
    }
}
