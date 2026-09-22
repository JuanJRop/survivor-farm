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
        public float immortalityCooldownRemaining;
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
        private float immortalityReadyAt;
        private string activeSkillEffectId;
        private readonly Dictionary<string, float> skillCooldowns = new Dictionary<string, float>(StringComparer.Ordinal);
        public event Action Changed;
        public SkillCombatEventBus Events { get { EnsureInitialized(); return events; } }
        public int SkillPoints => skillPoints;
        public int PlayerLevel => playerLevel;
        public int Experience => experience;
        public int ExperienceToNextLevel => 25 + playerLevel * 15;
        public float CarnageDashCooldownRefund => GetLevel("force_carnage") > 0 ? .18f : 0f;
        public float CarnageAttackCooldownRefund => GetLevel("force_carnage") > 0 ? .12f : 0f;
        public float DashCooldownMultiplier => Mathf.Clamp01(1f - (GetLevel("mobility_ghost_step") > 0 ? .22f : 0f) - (GetLevel("mobility_omnipresence") > 0 ? .18f : 0f));
        public bool HasSpectralDash => GetLevel("mobility_spectral") > 0;
        public bool HasOffensiveTeleport => GetLevel("mobility_offensive_teleport") > 0;

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
            if (definition == null || GetLevel(definition.Id) >= definition.MaxLevel || skillPoints < definition.Cost || playerLevel < definition.RequiredPlayerLevel)
                return false;
            if (definition.RequiredBranchInvestment > GetBranchInvestment(definition.Branch)) return false;
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
            levels[definition.Id] = Mathf.Min(definition.MaxLevel, 1);
            events.RaiseSkillUnlocked(new SkillEventContext(gameObject, null, transform.position, definition.Cost,
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

        public int GetBranchInvestment(SkillBranch branch)
        {
            EnsureInitialized();
            int total = 0;
            foreach (SkillDefinition definition in SkillTreeCatalog.All)
                if (definition.Branch == branch) total += GetLevel(definition.Id);
            return total;
        }

        public float DamageMultiplier(FarmTool tool, bool charged)
        {
            float multiplier = 1f;
            if (GetLevel("force_heavy_hit") > 0 && tool == FarmTool.Sword && charged) multiplier += NodeValue("force_heavy_hit", .5f);
            if (GetLevel("magic_spark") > 0) multiplier += NodeValue("magic_spark", .05f);
            if (GetLevel("magic_void") > 0) multiplier += NodeValue("magic_void", .1f);
            if (GetLevel("magic_apotheosis") > 0) multiplier += NodeValue("magic_apotheosis", .15f);
            if (GetLevel("magic_cataclysm") > 0 && charged) multiplier += .20f;
            if (GetLevel("magic_thunder_god") > 0) multiplier += .12f;
            if (GetLevel("magic_glacier") > 0) multiplier += .12f;
            if (GetLevel("force_titan_wrath") > 0 && charged) multiplier += NodeValue("force_titan_wrath", .35f);
            if (GetLevel("force_monster_strength") > 0 && charged) multiplier += NodeSecondaryValue("force_monster_strength", .35f);
            if (GetLevel("chaos_divine_ascension") > 0) multiplier += NodeValue("chaos_divine_ascension", .2f);
            return multiplier;
        }

        public float MovementMultiplier => 1f + (GetLevel("mobility_ghost_step") > 0 ? .12f : 0f) + (GetLevel("mobility_omnipresence") > 0 ? .10f : 0f);
        public float DashDurationMultiplier => 1f + (GetLevel("mobility_dash") > 0 ? NodeValue("mobility_dash", .16f) : 0f);
        public float DashSpeedMultiplier => GetLevel("mobility_dash") > 0 ? NodeSecondaryValue("mobility_dash", 1.15f) : 1f;
        public bool HasSecondDash => GetLevel("mobility_double_dash") > 0;
        public float DamageReduction
        {
            get
            {
                float reduction = 0f;
                PlayerSurvivalStats survival = GetComponent<PlayerSurvivalStats>();
                if (GetLevel("survival_immortal") > 0 && survival != null && survival.CurrentHealth <= Mathf.CeilToInt(survival.MaxHealth * .35f)) reduction += NodeValue("survival_immortal", .2f);
                if (GetLevel("survival_god_of_war") > 0) reduction += .10f + Mathf.Min(.35f, NearbyEnemyCount(4f) * .035f);
                if (GetLevel("chaos_divine_ascension") > 0) reduction += .10f;
                return Mathf.Clamp(reduction, 0f, .75f);
            }
        }
        public float LifeSteal
        {
            get
            {
                float amount = GetLevel("survival_lifesteal") > 0 ? NodeValue("survival_lifesteal", .08f) : 0f;
                if (GetLevel("magic_void") > 0) amount += .03f;
                if (GetLevel("survival_god_of_war") > 0) amount += Mathf.Min(.15f, NearbyEnemyCount(4f) * .02f);
                if (GetLevel("chaos_divine_ascension") > 0) amount += .05f;
                return amount;
            }
        }

        public int NearbyEnemyCount(float radius)
        {
            int count = 0;
            IReadOnlyList<EnemyAIBase> enemies = EnemyAIBase.ActiveEnemies;
            float radiusSqr = radius * radius;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyAIBase enemy = enemies[i];
                if (enemy != null && enemy.IsAlive && ((Vector2)enemy.transform.position - (Vector2)transform.position).sqrMagnitude <= radiusSqr) count++;
            }
            return count;
        }

        public bool TryStartSkillCooldown(string key, float cooldown)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(key)) return false;
            if (skillCooldowns.TryGetValue(key, out float readyAt) && Time.time < readyAt) return false;
            skillCooldowns[key] = Time.time + Mathf.Max(.05f, cooldown);
            return true;
        }

        public SkillEffectScope BeginSkillEffect(string skillId)
        {
            string previous = activeSkillEffectId;
            activeSkillEffectId = skillId;
            return new SkillEffectScope(this, previous);
        }

        public readonly struct SkillEffectScope : IDisposable
        {
            private readonly SkillTreeManager owner;
            private readonly string previous;
            public SkillEffectScope(SkillTreeManager owner, string previous) { this.owner = owner; this.previous = previous; }
            public void Dispose()
            {
                if (owner != null) owner.activeSkillEffectId = previous;
            }
        }

        public bool TryConsumeSecondChance()
        {
            if (!secondChanceUsed && GetLevel("survival_second_chance") > 0)
            {
                secondChanceUsed = true;
                ProcSkill("survival_second_chance", transform.position);
                Changed?.Invoke();
                return true;
            }
            if (GetLevel("survival_immortal") > 0 && Time.time >= immortalityReadyAt)
            {
                immortalityReadyAt = Time.time + 90f;
                ProcSkill("survival_immortal", transform.position);
                Changed?.Invoke();
                return true;
            }
            return false;
        }

        public void ProcSkill(string skillId, Vector2 position, GameObject target = null, int amount = 0)
        {
            EnsureInitialized();
            events.RaiseSkillUsed(new SkillEventContext(gameObject, target, position, amount, skillId: skillId));
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
                amount, tool, charged, heavy, critical, activeSkillEffectId));
        }

        public void NotifyDash(Vector2 direction)
        {
            EnsureInitialized();
            events.RaiseDash(new SkillEventContext(gameObject, null, transform.position, 0,
                FarmTool.Sword, direction: direction));
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
            events.RaiseEnemyKilled(new SkillEventContext(gameObject, target,
                target != null ? target.transform.position : transform.position, experienceReward,
                skillId: activeSkillEffectId));
        }

        public void NotifyPlayerDeath() => events?.RaisePlayerDeath(new SkillEventContext(gameObject, gameObject, transform.position, 0));
        public void NotifyPlayerRevived() => events?.RaisePlayerRevived(new SkillEventContext(gameObject, gameObject, transform.position, 0));

        private static float NodeValue(string id, float fallback)
        {
            SkillDefinition definition = SkillTreeCatalog.Find(id);
            return definition != null && definition.Value != 0f ? definition.Value : fallback;
        }

        private static float NodeSecondaryValue(string id, float fallback)
        {
            SkillDefinition definition = SkillTreeCatalog.Find(id);
            return definition != null && definition.SecondaryValue != 0f ? definition.SecondaryValue : fallback;
        }

        public SkillTreeSaveData Capture()
        {
            EnsureInitialized();
            return new SkillTreeSaveData
            {
                skillPoints = skillPoints,
                playerLevel = playerLevel,
                experience = experience,
                secondChanceUsed = secondChanceUsed,
                immortalityCooldownRemaining = Mathf.Max(0f, immortalityReadyAt - Time.time),
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
            immortalityReadyAt = Time.time + Mathf.Clamp(saved?.immortalityCooldownRemaining ?? 0f, 0f, 90f);
            foreach (SkillDefinition definition in SkillTreeCatalog.All) levels[definition.Id] = 0;
            if (saved?.skills != null)
                foreach (SkillLevelSaveData item in saved.skills)
                    if (item != null && levels.ContainsKey(item.id)) levels[item.id] = Mathf.Clamp(item.level, 0, 1);
            Changed?.Invoke();
        }
    }
}
