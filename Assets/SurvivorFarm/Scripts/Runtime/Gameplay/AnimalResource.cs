using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    [DefaultExecutionOrder(500)]
    public sealed class AnimalResource : HarvestableResource, IDamageable
    {
        [SerializeField] private SpriteRenderer bodyRenderer;
        public CombatSound DeathSound=CombatSound.RabbitDeath;
        public bool IsCow { get; private set; }
        public bool IsReactingToHit => Time.time < hurtUntil;
        private float hurtUntil;
        private Vector3 normalScale;
        private bool scaleCaptured;
        protected override float HighlightScale => 1f;

        public bool IsAlive => IsAvailable;
        protected override FarmTool RequiredTool => FarmTool.Sword;
        protected override string InteractionText => "Interactuar: cazar con espada o arco";
        protected override string MissingToolText => "Selecciona espada o arco para cazar.";
        protected override ItemKind ResourceKind => ItemKind.Food;

        public void ConfigureCow() { IsCow = true; }

        public bool ScatterAnimalRewards(int food, int coins)
        {
            if (!IsCow) return false;
            EnemyLootPickup.Scatter(transform.position, transform.parent, ItemKind.Leather, 4);
            EnemyLootPickup.Scatter(transform.position, transform.parent, ItemKind.Food, food);
            if (coins > 0) EnemyLootPickup.Scatter(transform.position, transform.parent, ItemKind.Coins, coins);
            return true;
        }

        public void ConfigureAnimation(SpriteRenderer body, Sprite[] frames)
        {
            bodyRenderer = body;
            ResourceAnimation clip = ResourceFlyweights.Animation(frames);
            SetDefinition(ResourceFlyweights.Resource(ResourceKind, Definition.HarvestAmount, Definition.CoinReward, Definition.MaxHealth, clip));
        }

        protected override void Update()
        {
            base.Update();
            if (bodyRenderer != null && Definition.Animation != null && Definition.Animation.FrameCount > 0)
            {
                bodyRenderer.sprite = Definition.Animation.Evaluate(Time.time);
            }
            // The base resource update restores its normal tint; the accepted animal hit owns the final pose.
            ApplyHitPose();
        }

        public override void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            if (!IsAvailable || inventory == null)
            {
                return;
            }

            if (selectedTool != FarmTool.Sword && selectedTool != FarmTool.Bow)
            {
                FarmNotificationCenter.Show(MissingToolText);
                return;
            }

            inventory.GetComponent<PlayerCombatController>()?.AttackTarget(this);
        }

        public void TakeDamage(int amount, PlayerInventory source)
        {
            if (!IsAvailable || source == null || amount <= 0) return;
            if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            if (bodyRenderer != null && !scaleCaptured) { normalScale = bodyRenderer.transform.localScale; scaleCaptured = true; }
            bool lethal = amount >= CurrentHealth;
            if (lethal && bodyRenderer != null) World.AnimalDeathVisual.Spawn(bodyRenderer);
            hurtUntil = Time.time + .28f;
            GetComponent<World.AnimalRoamingVisual>()?.ReactToHit(transform.position - source.transform.position);
            ApplyDamage(amount, source);
            if (IsAlive) ApplyHitPose();
        }

        private void ApplyHitPose()
        {
            if (bodyRenderer == null || !scaleCaptured) return;
            float hit = Mathf.Clamp01((hurtUntil - Time.time) / .28f);
            bodyRenderer.color = hit > 0 ? Color.Lerp(Color.white, new Color(1f, .45f, .4f), hit) : Color.white;
            float squash = Mathf.Sin(hit * Mathf.PI) * .18f;
            bodyRenderer.transform.localScale = Vector3.Scale(normalScale, new Vector3(1f + squash, 1f - squash, 1));
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (bodyRenderer != null && scaleCaptured) { bodyRenderer.transform.localScale = normalScale; bodyRenderer.color = Color.white; }
            hurtUntil = 0;
        }

        protected override void RaiseHarvestEvent() { }
    }
}
