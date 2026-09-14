using System;
using UnityEngine;
using SurvivorFarm.Runtime.UI;

namespace SurvivorFarm.Runtime.Player
{
    public sealed class PlayerSurvivalStats : MonoBehaviour, SurvivorFarm.Runtime.Gameplay.IDamageable
    {
        [SerializeField] private int maxHealth = 5;
        [SerializeField] private int currentHealth = 5;
        private float invulnerableUntil;
        private float blockedDamage;
        private void Awake()
        {
            if (GetComponent<PlayerRespawnController>() == null) gameObject.AddComponent<PlayerRespawnController>();
        }
        public void Revive()
        {
            Restore(maxHealth,maxHealth,1f);
            invulnerableUntil = Time.time + 3f;
        }

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public Transform Transform => transform;
        public bool IsAlive => isActiveAndEnabled && currentHealth > 0;
        public int SpawnGeneration { get; private set; }
        public void TakeDamage(int amount, PlayerInventory source) => TakeDamage(amount);
        public void GrantInvulnerability(float seconds) => invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + Mathf.Max(0, seconds));
        // Legacy saves and callers still carry satiety; it no longer affects gameplay.
        public float HungerPercent => 1f;

        public event Action StatsChanged;

        private void Start()
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            if (currentHealth <= 0) Revive();
            NotifyChanged();
        }

        public void AdvanceNeeds(float seconds) { }

        public void TakeDamage(int amount, bool starvation = false)
        {
            if (starvation || amount <= 0 || currentHealth <= 0 || Time.time < invulnerableUntil)
            {
                return;
            }

            if(!starvation)
            {
                blockedDamage += amount*(GetComponent<PlayerInventory>()?.ArmorReduction ?? 0);
                int blocked=Mathf.FloorToInt(blockedDamage);blockedDamage-=blocked;amount-=blocked;
                if(amount<=0){FarmNotificationCenter.Show("Tu armadura absorbió el golpe.");return;}
            }
            currentHealth = Mathf.Max(0, currentHealth - amount);
            if (Core.PortfolioSession.Active) GrantInvulnerability(.7f);
            GetComponent<PlayerCombatController>()?.CancelMelee();
            GetComponent<SurvivorFarm.Runtime.Gameplay.HitFeedback>()?.PlayerHurt(amount, currentHealth <= 0);
            GetComponent<PlayerCharacterAnimator>()?.PlayNamedAction(currentHealth <= 0 ? "Dead" : "Damage");
            NotifyChanged();

            if (currentHealth <= 0)
            {
                FindFirstObjectByType<TutorialQuestSystem>()?.NotifyDeath();
                FarmNotificationCenter.Show("Te quedaste sin vida.");
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || currentHealth <= 0)
            {
                return;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            NotifyChanged();
        }

        public void IncreaseMaxHealth(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            maxHealth += amount;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            NotifyChanged();
        }

        public void RestoreHunger(float amount) { }

        public void Restore(int savedHealth, float hungerPercent)
        {
            Restore(maxHealth, savedHealth, hungerPercent);
        }

        public void Restore(int savedMaxHealth, int savedHealth, float hungerPercent)
        {
            SpawnGeneration++;
            maxHealth = Mathf.Max(1, savedMaxHealth);
            currentHealth = Mathf.Clamp(savedHealth, 0, maxHealth);
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            StatsChanged?.Invoke();
            FarmNotificationCenter.SetSurvival(currentHealth, maxHealth, HungerPercent);
        }
    }
}
