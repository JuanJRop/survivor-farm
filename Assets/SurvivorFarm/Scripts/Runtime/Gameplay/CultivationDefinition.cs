using UnityEngine;
using SurvivorFarm.Runtime.Player;

namespace SurvivorFarm.Runtime.Gameplay
{
    [CreateAssetMenu(menuName = "Survivor Farm/Flyweights/Cultivation")]
    public sealed class CultivationDefinition : ScriptableObject
    {
        [SerializeField] private string cropName = "fruta";
        [SerializeField, Min(0.01f)] private float growDuration = 8f;
        [SerializeField, Min(0.01f)] private float digDuration = 1.3f;
        [SerializeField, Min(0.01f)] private float hoeDuration = 1.1f;
        [SerializeField, Min(0.01f)] private float plantDuration = 0.9f;
        [SerializeField, Min(0.01f)] private float waterDuration = 1f;
        public string CropName => cropName;
        public float GrowDuration => growDuration;
        public float DigDuration => digDuration;
        public float HoeDuration => hoeDuration;
        public float PlantDuration => plantDuration;
        public float WaterDuration => waterDuration;

        public float GetGrowDuration(string cropItemId)
        {
            float cropDuration = SurvivalItemCatalog.Find(cropItemId)?.GrowthSeconds ?? 0f;
            return Mathf.Max(0.01f, cropDuration > 0f ? cropDuration : growDuration);
        }

        internal void Initialize(string label, float grow, float dig, float hoe, float plant, float water)
        {
            cropName = label;
            growDuration = grow;
            digDuration = dig;
            hoeDuration = hoe;
            plantDuration = plant;
            waterDuration = water;
        }
    }
}
