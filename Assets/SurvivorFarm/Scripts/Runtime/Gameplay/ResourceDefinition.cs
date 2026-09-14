using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    [CreateAssetMenu(menuName = "Survivor Farm/Flyweights/Resource")]
    public sealed class ResourceDefinition : ScriptableObject
    {
        [SerializeField] private ItemDefinition reward;
        [SerializeField, Min(1)] private int harvestAmount = 2;
        [SerializeField, Min(0)] private int coinReward = 5;
        [SerializeField, Min(1)] private int maxHealth = 1;
        [SerializeField] private ResourceAnimation animation;

        public ItemDefinition Reward => reward;
        public int HarvestAmount => harvestAmount;
        public int CoinReward => coinReward;
        public int MaxHealth => maxHealth;
        public ResourceAnimation Animation => animation;

        internal void Initialize(ItemDefinition item, int amount, int coins, int health, ResourceAnimation clip)
        {
            reward = item;
            harvestAmount = Mathf.Max(1, amount);
            coinReward = Mathf.Max(0, coins);
            maxHealth = Mathf.Max(1, health);
            animation = clip;
        }
    }
}
