using UnityEngine;
using SurvivorFarm.Runtime.Player;

namespace SurvivorFarm.Runtime.Gameplay
{
    // Legacy plots remain dormant; the portfolio session explicitly enables six authored plots.
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
        private bool portfolioEnabled;
        private Sprite soilSprite;
        private Sprite[] growthSprites;
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
        public float GrowDuration => portfolioEnabled ? 35f : Definition.GetGrowDuration(cropItemId);
        public bool IsWorking => false;
        public override bool IsAvailable => portfolioEnabled && isActiveAndEnabled && state != 4;
        public bool SupportsTool(FarmTool tool) => portfolioEnabled;
        public override string GetInteractionLabel(FarmTool tool) => !portfolioEnabled ? string.Empty :
            state == 5 ? "Recoger cosecha · +2 fruta" : state == 3 ? "Regar cultivo" : state == 4 ? $"Creciendo · {remainingGrowTime:0}s" : "Plantar · 1 semilla";
        public override void SetHighlighted(bool highlighted) { }
        public override void Interact(FarmTool tool, PlayerInventory inventory)
        {
            if (!IsAvailable || inventory == null || Vector2.Distance(inventory.transform.position, transform.position) > 1.5f) return;
            if (state == 5)
            {
                state = 2;
                inventory.AddFruit(2+(inventory.GetComponent<ToolMastery>()?.Level(FarmTool.Hoe)??1)-1);
                inventory.AddSeeds(1);
                FarmGameEvents.RaiseCropHarvested();
                inventory.GetComponent<GameFeelFeedback>()?.Pulse("+2 fruta · +1 semilla", transform.position);
            }
            else if (state == 3)
            {
                state = 4; remainingGrowTime = GrowDuration;
                inventory.GetComponent<PlayerCharacterAnimator>()?.PlayAction("Watering", 0, transform.position);
                CultivationSoilVisual.Emit(transform.position, true);
                FarmGameEvents.RaiseCropWatered();
                inventory.GetComponent<ToolMastery>()?.Earn(FarmTool.WateringCan);
                remainingGrowTime/=1+.15f*((inventory.GetComponent<ToolMastery>()?.Level(FarmTool.WateringCan)??1)-1);
            }
            else
            {
                if (!inventory.TryRemoveSeeds(1)) { UI.FarmNotificationCenter.Show("Necesitas una semilla. Cada cosecha devuelve la suya."); return; }
                state = 3;
                inventory.GetComponent<PlayerCharacterAnimator>()?.PlayAction("Hoe", 0, transform.position);
                CultivationSoilVisual.Emit(transform.position, false);
                FarmGameEvents.RaiseSeedPlanted();
                inventory.GetComponent<ToolMastery>()?.Earn(FarmTool.Hoe);
            }
            RefreshPlot();
        }
        public void CancelAction() { }
        private void Awake()
        {
            _ = PersistentId; _ = LegacySortKey;
            state = startingSurface == StartingSurface.Grass ? 0 : 2;
            if (!portfolioEnabled) HidePlot(); else RefreshPlot();
        }
        public void Configure(SpriteRenderer ground, SpriteRenderer crop, StartingSurface surface)
        {
            groundRenderer = ground; cropRenderer = crop; startingSurface = surface;
            state = surface == StartingSurface.Grass ? 0 : 2;
            HidePlot();
        }
        public void ConfigureArt(Sprite soil, Sprite wetSoil, Sprite[] growth) => HidePlot();
        public void EnableForSlice(Sprite soil, Sprite[] growth)
        {
            portfolioEnabled = true;
            soilSprite = soil; growthSprites = growth;
            gameObject.SetActive(true);
            transform.localScale = Vector3.one;
            if (groundRenderer == null) groundRenderer = GetComponent<SpriteRenderer>();
            if (groundRenderer == null) groundRenderer = gameObject.AddComponent<SpriteRenderer>();
            if (groundRenderer.transform == transform)
            {
                groundRenderer.enabled=false;
                var soilObject=new GameObject("Tilled soil");soilObject.transform.SetParent(transform,false);
                soilObject.transform.localPosition=new Vector3(0,-.5f,0);
                groundRenderer=soilObject.AddComponent<SpriteRenderer>();
            }
            if (cropRenderer == null)
            {
                var crop = new GameObject("Crop Visual"); crop.transform.SetParent(transform, false);
                cropRenderer = crop.AddComponent<SpriteRenderer>();
            }
            var collider = GetComponent<BoxCollider2D>();
            if (collider == null) collider = gameObject.AddComponent<BoxCollider2D>();
            collider.enabled = true; collider.isTrigger = true; collider.size = Vector2.one * .95f;
            state = 2;
            RefreshPlot();
        }
        public void AdvanceGrowth(float seconds)
        {
            if (!portfolioEnabled || state != 4 || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            remainingGrowTime = Mathf.Max(0, remainingGrowTime - seconds);
            if (remainingGrowTime == 0) state = 5;
            RefreshPlot();
        }
        private void Update() => AdvanceGrowth(Time.deltaTime);
        private void RefreshPlot()
        {
            if (!portfolioEnabled) return;
            groundRenderer.enabled = true; groundRenderer.sprite = soilSprite;
            groundRenderer.color = state >= 4 ? new Color(.74f,.69f,.58f) : Color.white;
            groundRenderer.sortingOrder = -85;
            if (soilSprite != null) groundRenderer.transform.localScale = Vector3.one * (1.1f / soilSprite.bounds.size.x);
            cropRenderer.enabled = state >= 3;
            if (growthSprites != null && growthSprites.Length > 0)
                cropRenderer.sprite = growthSprites[Mathf.Clamp(state == 5 ? growthSprites.Length - 1 : state == 3 ? 0 : 1, 0, growthSprites.Length - 1)];
            cropRenderer.transform.localPosition = new Vector3(0, .05f, 0);
            cropRenderer.transform.localScale = Vector3.one * (state == 5 ? .9f : .65f);
            cropRenderer.color = state == 5 ? Color.white : new Color(.85f,1f,.72f);
            cropRenderer.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 20);
        }
        public void Restore(int savedState, float remaining, int rarity = 0, string crop = null)
        {
            state = Mathf.Clamp(savedState, 0, 5);
            seedRarity = Mathf.Clamp(rarity, 0, 2);
            cropItemId = crop;
            remainingGrowTime = Mathf.Max(0, remaining);
            if (!portfolioEnabled) HidePlot(); else RefreshPlot();
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
