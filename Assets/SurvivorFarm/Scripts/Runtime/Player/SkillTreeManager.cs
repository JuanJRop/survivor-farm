using System;
using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    [Serializable]
    public sealed class SkillTreeSaveData
    {
        public int skillPoints;
        public int playerLevel = 1;
        public int experience;
        public bool secondChanceUsed;
        public SkillLevelSaveData[] skills = Array.Empty<SkillLevelSaveData>();
    }

    [Serializable]
    public sealed class SkillLevelSaveData
    {
        public string id;
        public int level;
    }

    /// <summary>Owns points, requirements and persistence for the data-driven tree.</summary>
    [DisallowMultipleComponent]
    public sealed class SkillTreeManager : MonoBehaviour
    {
        [SerializeField, Min(0)] private int skillPoints = 3;
        [SerializeField, Min(1)] private int playerLevel = 1;
        [SerializeField, Min(0)] private int experience;
        private readonly Dictionary<string, int> levels = new Dictionary<string, int>(StringComparer.Ordinal);
        private SkillCombatEventBus events;
        private bool initialized;
        private bool secondChanceUsed;
        public event Action Changed;
        public SkillCombatEventBus Events { get { EnsureInitialized(); return events; } }
        public int SkillPoints => skillPoints;
        public int PlayerLevel => playerLevel;
        public int Experience => experience;
        public int ExperienceToNextLevel => 25 + playerLevel * 15;

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;
            events = GetComponent<SkillCombatEventBus>() ?? gameObject.AddComponent<SkillCombatEventBus>();
            foreach (SkillDefinition definition in SkillTreeCatalog.All) levels[definition.Id] = 0;
        }

        public static SkillTreeManager Ensure(PlayerInventory inventory)
        {
            if (inventory == null) return null;
            var manager = inventory.GetComponent<SkillTreeManager>() ?? inventory.gameObject.AddComponent<SkillTreeManager>();
            manager.EnsureInitialized();
            return manager;
        }

        public int GetLevel(string id)
        {
            EnsureInitialized();
            return id != null && levels.TryGetValue(id, out int value) ? value : 0;
        }

        public SkillStatus GetStatus(string id)
        {
            SkillDefinition definition = SkillTreeCatalog.Find(id);
            if (definition == null) return SkillStatus.Locked;
            int current = GetLevel(id);
            if (current >= 1) return SkillStatus.Maxed;
            return CanUnlock(definition) ? SkillStatus.Available : SkillStatus.Locked;
        }

        public bool CanUnlock(string id) => CanUnlock(SkillTreeCatalog.Find(id));

        private bool CanUnlock(SkillDefinition definition)
        {
            if (definition == null || GetLevel(definition.Id) >= 1 || skillPoints < definition.Cost || playerLevel < definition.RequiredPlayerLevel)
                return false;
            foreach (string prerequisite in definition.Prerequisites ?? Array.Empty<string>())
                if (GetLevel(prerequisite) <= 0) return false;
            return true;
        }

        public bool TryUnlock(string id)
        {
            EnsureInitialized();
            SkillDefinition definition = SkillTreeCatalog.Find(id);
            if (!CanUnlock(definition)) return false;
            skillPoints -= definition.Cost;
            levels[definition.Id] = 1;
            events.RaiseSkillUsed(new SkillEventContext(gameObject, null, transform.position, definition.Cost,
                skillId: definition.Id));
            Changed?.Invoke();
            return true;
        }

        public void AddSkillPoints(int amount)
        {
            if (amount <= 0) return;
            skillPoints = Mathf.Min(999, skillPoints + amount);
            Changed?.Invoke();
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0) return;
            experience = Mathf.Min(999999, experience + amount);
            bool leveled = false;
            while (experience >= ExperienceToNextLevel)
            {
                experience -= ExperienceToNextLevel;
                playerLevel = Mathf.Min(99, playerLevel + 1);
                skillPoints = Mathf.Min(999, skillPoints + 1);
                leveled = true;
            }
            if (leveled) FarmNotificationCenter.Show("Nivel " + playerLevel + ": +1 punto de habilidad.");
            Changed?.Invoke();
        }

        public float DamageMultiplier(FarmTool tool, bool charged)
        {
            float multiplier = 1f;
            if (GetLevel("force_heavy_hit") > 0 && tool == FarmTool.Sword && charged) multiplier += .20f;
            if (GetLevel("magic_spark") > 0) multiplier += .05f;
            if (GetLevel("magic_void") > 0) multiplier += .10f;
            if (GetLevel("force_titan_wrath") > 0 && charged) multiplier += .35f;
            if (GetLevel("chaos_death_mark") > 0) multiplier += .10f;
            return multiplier;
        }

        public float MovementMultiplier => 1f + (GetLevel("mobility_ghost_step") > 0 ? .12f : 0f) + (GetLevel("mobility_omnipresence") > 0 ? .10f : 0f);
        public float DashDurationMultiplier => 1f + (GetLevel("mobility_dash") > 0 ? .16f : 0f);
        public float DashSpeedMultiplier => 1f + (GetLevel("mobility_dash") > 0 ? .15f : 0f);
        public bool HasSecondDash => GetLevel("mobility_double_dash") > 0;
        public float DamageReduction => GetLevel("survival_immortal") > 0 ? .2f : GetLevel("survival_god_of_war") > 0 ? .3f : 0f;
        public float LifeSteal => GetLevel("survival_lifesteal") > 0 ? .08f : 0f;

        public bool TryConsumeSecondChance()
        {
            if (secondChanceUsed || GetLevel("survival_second_chance") <= 0) return false;
            secondChanceUsed = true;
            Changed?.Invoke();
            return true;
        }

        public void NotifyAttack(FarmTool tool, bool charged)
        {
            EnsureInitialized();
            events.RaiseAttack(new SkillEventContext(gameObject, null, transform.position, 0, tool, charged));
        }

        public void NotifyHit(GameObject target, int amount, FarmTool tool, bool heavy, bool charged, bool critical = false)
        {
            EnsureInitialized();
            events.RaiseHit(new SkillEventContext(gameObject, target, target != null ? target.transform.position : transform.position,
                amount, tool, charged, heavy, critical));
        }

        public void NotifyDash(Vector2 direction)
        {
            EnsureInitialized();
            events.RaiseDash(new SkillEventContext(gameObject, null, transform.position, 0, FarmTool.Sword));
        }

        public void NotifyDamageTaken(int amount)
        {
            EnsureInitialized();
            events.RaiseDamageTaken(new SkillEventContext(gameObject, gameObject, transform.position, amount));
        }

        public void NotifyEnemyKilled(GameObject target, int experienceReward = 5)
        {
            EnsureInitialized();
            AddExperience(experienceReward);
            events.RaiseEnemyKilled(new SkillEventContext(gameObject, target, target != null ? target.transform.position : transform.position, experienceReward));
        }

        public void NotifyPlayerDeath() => events?.RaisePlayerDeath(new SkillEventContext(gameObject, gameObject, transform.position, 0));
        public void NotifyPlayerRevived() => events?.RaisePlayerRevived(new SkillEventContext(gameObject, gameObject, transform.position, 0));

        public SkillTreeSaveData Capture()
        {
            EnsureInitialized();
            return new SkillTreeSaveData
            {
                skillPoints = skillPoints,
                playerLevel = playerLevel,
                experience = experience,
                secondChanceUsed = secondChanceUsed,
                skills = levels.Where(pair => pair.Value > 0).Select(pair => new SkillLevelSaveData { id = pair.Key, level = pair.Value }).ToArray()
            };
        }

        public void Restore(SkillTreeSaveData saved)
        {
            EnsureInitialized();
            skillPoints = Mathf.Clamp(saved?.skillPoints ?? 3, 0, 999);
            playerLevel = Mathf.Clamp(saved?.playerLevel ?? 1, 1, 99);
            experience = Mathf.Clamp(saved?.experience ?? 0, 0, 999999);
            secondChanceUsed = saved != null && saved.secondChanceUsed;
            foreach (SkillDefinition definition in SkillTreeCatalog.All) levels[definition.Id] = 0;
            if (saved?.skills != null)
                foreach (SkillLevelSaveData item in saved.skills)
                    if (item != null && levels.ContainsKey(item.id)) levels[item.id] = Mathf.Clamp(item.level, 0, 1);
            Changed?.Invoke();
        }
    }
}
