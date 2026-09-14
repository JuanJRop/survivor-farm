using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class AnimalResource : HarvestableResource, IDamageable
    {
        [SerializeField] private SpriteRenderer bodyRenderer;

        public bool IsAlive => IsAvailable;
        protected override FarmTool RequiredTool => FarmTool.Sword;
        protected override string InteractionText => "Interactuar: cazar con espada o arco";
        protected override string MissingToolText => "Selecciona espada o arco para cazar.";
        protected override ItemKind ResourceKind => ItemKind.Food;

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
            if(IsAvailable && amount>0) VisibleHitFeedback.Play(gameObject);
            ApplyDamage(amount, source);
        }

        protected override void RaiseHarvestEvent() { }
    }
}
