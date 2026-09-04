using System;
using UnityEngine;
using SurvivorFarm.Runtime.UI;

namespace SurvivorFarm.Runtime.Player
{
    public sealed class PlayerSurvivalStats : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 5;
        [SerializeField] private int currentHealth = 5;
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float currentHunger = 100f;
        [SerializeField] private float hungerDrainPerSecond = 0.35f;
        [SerializeField] private int starvationDamage = 1;
        [SerializeField] private float starvationDamageInterval = 4f;

        private float nextStarvationDamageTime;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public float HungerPercent => maxHunger <= 0f ? 0f : Mathf.Clamp01(currentHunger / maxHunger);

        public event Action StatsChanged;

        private void Start()
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);
            NotifyChanged();
        }

        private void Update()
        {
            if (currentHealth <= 0)
            {
                return;
            }

            float previousHunger = currentHunger;
            currentHunger = Mathf.Max(0f, currentHunger - hungerDrainPerSecond * Time.deltaTime);

            if (currentHunger <= 0f && Time.time >= nextStarvationDamageTime)
            {
                TakeDamage(starvationDamage);
                nextStarvationDamageTime = Time.time + starvationDamageInterval;
            }
            else if (!Mathf.Approximately(previousHunger, currentHunger))
            {
                NotifyChanged();
            }
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || currentHealth <= 0)
            {
                return;
            }

            currentHealth = Mathf.Max(0, currentHealth - amount);
            NotifyChanged();

            if (currentHealth <= 0)
            {
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

        public void RestoreHunger(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            currentHunger = Mathf.Min(maxHunger, currentHunger + amount);
            nextStarvationDamageTime = Time.time + starvationDamageInterval;
            NotifyChanged();
        }

        public void Restore(int savedHealth, float hungerPercent)
        {
            Restore(maxHealth, savedHealth, hungerPercent);
        }

        public void Restore(int savedMaxHealth, int savedHealth, float hungerPercent)
        {
            maxHealth = Mathf.Max(1, savedMaxHealth);
            currentHealth = Mathf.Clamp(savedHealth, 0, maxHealth);
            currentHunger = Mathf.Clamp01(hungerPercent) * maxHunger;
            nextStarvationDamageTime = Time.time + starvationDamageInterval;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            StatsChanged?.Invoke();
            FarmNotificationCenter.SetSurvival(currentHealth, maxHealth, HungerPercent);
        }
    }
}
