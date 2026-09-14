using UnityEngine;
using SurvivorFarm.Runtime.Player;

namespace SurvivorFarm.Runtime.Gameplay
{
    // Save-only compatibility for plots authored in older scenes. No growth or interactions.
    public sealed class FarmingPlot : WorldInteractable
    {
        [SerializeField] private CultivationDefinition definition;
        [SerializeField] private StartingSurface startingSurface = StartingSurface.DirtReady;
        [SerializeField] private SpriteRenderer groundRenderer;
        [SerializeField] private SpriteRenderer cropRenderer;
        public enum StartingSurface { DirtReady, Grass }
        public enum PlotState { Grass, Dug, Tilled, SeededDry, Growing, Ready }
        private int state;
        private float remainingGrowTime;
        private int seedRarity;
        private string cropItemId, persistentId, legacySortKey;
        public CultivationDefinition Definition => definition != null ? definition : definition = ResourceFlyweights.Cultivation();
        public void SetDefinition(CultivationDefinition value) { definition = value; }
        public string LegacySortKey => legacySortKey ??= $"{name}_{transform.position.x:000.000}_{transform.position.y:000.000}";
        public string PersistentId
        {
            get
            {
                if (persistentId == null)
                {
                    persistentId = name;
                    for (var p = transform; p != null; p = p.parent) persistentId = "/" + p.GetSiblingIndex() + persistentId;
                }
                return Core.StableSaveId.Resolve(this, persistentId);
            }
        }
        public int StateId => state;
        public int PlantedSeedRarityId => seedRarity;
        public string PlantedCropItemId => cropItemId;
        public float RemainingGrowTime => remainingGrowTime;
        public float GrowDuration => Definition.GetGrowDuration(cropItemId);
        public bool IsWorking => false;
        public override bool IsAvailable => false;
        public bool SupportsTool(FarmTool tool) => false;
        public override string GetInteractionLabel(FarmTool tool) => string.Empty;
        public override void SetHighlighted(bool highlighted) { }
        public override void Interact(FarmTool tool, PlayerInventory inventory) { }
        public void CancelAction() { }
        private void Awake()
        {
            _ = PersistentId; _ = LegacySortKey;
            state = startingSurface == StartingSurface.Grass ? 0 : 2;
            HidePlot();
        }
        public void Configure(SpriteRenderer ground, SpriteRenderer crop, StartingSurface surface)
        {
            groundRenderer = ground; cropRenderer = crop; startingSurface = surface;
            state = surface == StartingSurface.Grass ? 0 : 2;
            HidePlot();
        }
        public void ConfigureArt(Sprite soil, Sprite wetSoil, Sprite[] growth) => HidePlot();
        public void Restore(int savedState, float remaining, int rarity = 0, string crop = null)
        {
            state = Mathf.Clamp(savedState, 0, 5);
            seedRarity = Mathf.Clamp(rarity, 0, 2);
            cropItemId = crop;
            remainingGrowTime = Mathf.Max(0, remaining);
            HidePlot();
        }
        private void HidePlot()
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            foreach (var collider in GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
            if (groundRenderer != null) groundRenderer.enabled = false;
            if (cropRenderer != null) cropRenderer.enabled = false;
            if (Application.isPlaying) gameObject.SetActive(false);
        }
    }
}
