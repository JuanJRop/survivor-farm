using System;
using UnityEngine;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;

namespace SurvivorFarm.Runtime.Gameplay
{
    public abstract class HarvestableResource : WorldInteractable
    {
        [SerializeField] private ResourceDefinition definition;
        public ResourceDefinition Definition => definition != null ? definition : definition = ResourceFlyweights.Resource(ResourceKind);
        public void SetDefinition(ResourceDefinition sharedDefinition) { definition = sharedDefinition; }
        [SerializeField] private SpriteRenderer mainRenderer;
        [SerializeField] private SpriteRenderer secondaryRenderer;
        [SerializeField] private Collider2D blockingCollider;
        private int harvestAmount => Definition.HarvestAmount;
        private int coinReward => Definition.CoinReward;
        private int maxHealth => Definition.MaxHealth;
        [SerializeField, HideInInspector] private int currentHealth = -1;
        [SerializeField, HideInInspector] private bool harvested;

        private Renderer[] renderers;
        private Collider2D[] colliders;
        private Color defaultColor;
        private float hurtFlashEndsAt;
        private bool gathering;
        private PlayerInventory activeGatherInventory;
        private int activeToolBonus;
        private int activeGatherHitCount;
        private int completedGatherHits;
        private float gatherStartedAt;
        private float gatherEndsAt;
        private float nextGatherHitAt;
        private float activeGatherDuration;
        private float gatherHitInterval;
        private PlayerCharacterAnimator activeGatherAnimator;
        private PlayerSurvivalStats activeGatherStats;
        private string activeGatherClip;
        private const float GatherReach = 1.35f;
        private int lastFeedbackFrame = -1;
        private World.WorldActionClock workClock;

        public override bool IsAvailable => !harvested && isActiveAndEnabled;
        public bool IsHarvested => harvested;
        public bool IsGathering => gathering;
        public int CurrentHealth => currentHealth < 0 ? maxHealth : currentHealth;
        public int SpawnGeneration { get; private set; }
        public event Action<HarvestableResource> Depleted;

        protected override float HighlightScale => 1.12f;

        protected abstract FarmTool RequiredTool { get; }
        public bool SupportsTool(FarmTool tool) => !(this is AnimalResource) && tool == RequiredTool;
        protected abstract string InteractionText { get; }
        protected abstract string MissingToolText { get; }
        protected abstract ItemKind ResourceKind { get; }
        protected string RewardName => Definition.Reward.DisplayName;
        protected abstract void RaiseHarvestEvent();
        /// <summary>
        /// Trees in the authored farm use the same physical loot pipeline as enemies.
        /// Fixtures and non-portfolio scenes keep the old immediate reward semantics so
        /// resources remain useful outside the playable campaign.
        /// </summary>
        protected virtual bool UsesPhysicalHarvestDrop => PortfolioSession.Active && this is TreeResource;
        protected virtual float GatherDuration => 2.2f;
        protected virtual int GatherHitCount => 4;
        protected virtual string GatherStartText => "Recolectando...";
        protected virtual string GatherPresentText => "Recolectando";

        protected virtual void Awake()
        {
            var feet = GetComponent<CircleCollider2D>();
            if (feet != null) { feet.radius = this is TreeResource ? .28f : .34f; if(this is AnimalResource) feet.isTrigger=true; }
            if (mainRenderer == null)
            {
                mainRenderer = GetComponent<SpriteRenderer>();
            }

            if (blockingCollider == null)
            {
                blockingCollider = GetComponent<Collider2D>();
            }

            CacheVisuals();
            if (currentHealth < 0)
            {
                currentHealth = maxHealth;
            }
        }

        protected virtual void Update()
        {
            if (mainRenderer != null)
            {
                Color tint = Time.time < hurtFlashEndsAt ? Color.Lerp(defaultColor, Color.white, 0.7f) : defaultColor;
                tint.a = mainRenderer.color.a;
                mainRenderer.color = tint;
            }

            if (gathering)
            {
                UpdateGathering();
            }
        }

        public void Configure(SpriteRenderer primary, SpriteRenderer secondary, int amount)
        {
            Configure(primary, secondary, amount, coinReward);
        }

        public void Configure(SpriteRenderer primary, SpriteRenderer secondary, int amount, int coins)
        {
            mainRenderer = primary;
            secondaryRenderer = secondary;
            blockingCollider = GetComponent<Collider2D>();
            definition = ResourceFlyweights.Resource(ResourceKind, amount, coins, maxHealth, Definition.Animation);
            CacheVisuals();
            ApplyVisuals();
        }

        public void ConfigureHealth(int health)
        {
            definition = ResourceFlyweights.Resource(ResourceKind, harvestAmount, coinReward, health, Definition.Animation);
            currentHealth = maxHealth;
        }

        public override string GetInteractionLabel(FarmTool selectedTool)
        {
            if (!IsAvailable)
            {
                return string.Empty;
            }

            if (gathering)
            {
                return GatherPresentText;
            }

            var tier=GetComponent<ResourceTier>();
            return tier!=null?$"{(this is TreeResource?"Árbol":"Roca")} Nv.{tier.Tier} · E recolectar":InteractionText;
        }

        public override void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            if (!IsAvailable || inventory == null)
            {
                return;
            }

            if (selectedTool != RequiredTool)
            {
                FarmNotificationCenter.Show(MissingToolText);
                return;
            }

            if(GetComponent<ResourceTier>() is ResourceTier tier&&!tier.CanHarvest(inventory))return;

            var stats = inventory.GetComponent<PlayerSurvivalStats>();
            if (stats != null && stats.CurrentHealth <= 0) return;

            if (gathering)
            {
                return;
            }

            PlayerToolUpgradeController upgrades = inventory != null
                ? inventory.GetComponent<PlayerToolUpgradeController>()
                : null;
            int toolBonus = upgrades != null ? upgrades.GetResourceBonus(selectedTool) : 0;
            PlayerCharacterAnimator animator = inventory.GetComponent<PlayerCharacterAnimator>();
            if (animator != null && Application.isPlaying)
            {
                if (animator.MovementLocked || !inventory.isActiveAndEnabled ||
                    Vector2.Distance(inventory.transform.position, transform.position) > GatherReach) return;
                BeginGathering(selectedTool, inventory, toolBonus, animator);
                return;
            }

            animator?.PlayAction(PlayerCharacterAnimator.ToolClip(selectedTool), 0, transform.position);
            ApplyDamage(1 + toolBonus, inventory, toolBonus);
        }

        private void BeginGathering(FarmTool tool, PlayerInventory inventory, int toolBonus, PlayerCharacterAnimator animator)
        {
            activeGatherInventory = inventory;
            activeGatherAnimator = animator;
            activeGatherStats = inventory.GetComponent<PlayerSurvivalStats>();
            activeGatherClip = PlayerCharacterAnimator.ToolClip(tool);
            activeToolBonus = toolBonus;
            completedGatherHits = 0;
            activeGatherDuration = Mathf.Max(0.8f, GatherDuration / (1f + Mathf.Max(0, toolBonus) * 0.18f));
            var clip = animator.Library != null ? animator.Library.Find(activeGatherClip) : null;
            gatherHitInterval = clip != null && clip.FramesPerSecond > 0f
                ? clip.Frames / clip.FramesPerSecond : activeGatherDuration / Mathf.Max(1, GatherHitCount);
            gatherHitInterval = Mathf.Max(0.01f, gatherHitInterval);
            activeGatherDuration = Mathf.Max(activeGatherDuration, gatherHitInterval);
            // Impact once per animation cycle, after the tool has descended.
            float firstImpact = gatherHitInterval * 0.6f;
            activeGatherHitCount = Mathf.Max(1, Mathf.CeilToInt((activeGatherDuration - firstImpact) / gatherHitInterval));
            gatherStartedAt = Time.time;
            gatherEndsAt = Time.time + activeGatherDuration;
            nextGatherHitAt = gatherStartedAt + firstImpact;
            gathering = true;
            if (activeGatherStats != null) activeGatherStats.StatsChanged += CheckGathererHealth;
            animator.PlayAction(activeGatherClip, activeGatherDuration, transform.position);
            workClock=World.WorldActionClock.For(inventory.gameObject);workClock.Show(this,0);
        }

        private void UpdateGathering()
        {
            if (activeGatherInventory == null || !activeGatherInventory.isActiveAndEnabled ||
                Vector2.Distance(activeGatherInventory.transform.position, transform.position) > GatherReach)
            {
                CancelGathering();
                return;
            }

            if (activeGatherStats != null && activeGatherStats.CurrentHealth <= 0 ||
                activeGatherAnimator == null || !activeGatherAnimator.isActiveAndEnabled)
            {
                CancelGathering();
                return;
            }

            bool finished = Time.time >= gatherEndsAt;
            bool naturalEnd = finished && activeGatherAnimator.CurrentClip == "Idle" && !activeGatherAnimator.MovementLocked;
            if (activeGatherAnimator.CurrentClip != activeGatherClip && !naturalEnd)
            {
                CancelGathering();
                return;
            }

            bool hit = false;
            while (Time.time >= nextGatherHitAt && completedGatherHits < activeGatherHitCount)
            {
                completedGatherHits++;
                hit = true;
                nextGatherHitAt += gatherHitInterval;
            }
            // A delayed frame advances work but emits only one visual impact.
            if (hit)
            {
                hurtFlashEndsAt = Time.time + 0.1f;
                PlayHarvestImpact(activeGatherInventory);
            }

            workClock?.Show(this,GatherProgress);
            if (!finished)
            {
                return;
            }

            PlayerInventory inventory = activeGatherInventory;
            int toolBonus = activeToolBonus;
            CancelGathering();
            ApplyDamage(CurrentHealth, inventory, toolBonus);
        }

        private void CheckGathererHealth()
        {
            if (activeGatherStats != null && activeGatherStats.CurrentHealth <= 0) CancelGathering();
        }

        public void CancelGathering()
        {
            if (!gathering) return;
            var animator = activeGatherAnimator;
            string clip = activeGatherClip;
            ClearGatheringState();
            // Damage/death may already own the animator; do not replace that action.
            if (animator != null && animator.CurrentClip == clip) animator.CancelAction();
            hurtFlashEndsAt = 0f;
        }

        protected override void OnDisable() { CancelGathering(); base.OnDisable(); }

        private void ClearGatheringState()
        {
            workClock?.Hide(this);
            if (activeGatherStats != null) activeGatherStats.StatsChanged -= CheckGathererHealth;
            gathering = false;
            activeGatherAnimator = null;
            activeGatherStats = null;
            activeGatherClip = null;
            activeGatherInventory = null;
            activeToolBonus = 0;
            activeGatherHitCount = 0;
            completedGatherHits = 0;
            gatherStartedAt = 0f;
            gatherEndsAt = 0f;
            nextGatherHitAt = 0f;
            activeGatherDuration = 0f;
            gatherHitInterval = 0f;
        }

        protected void ApplyDamage(int damage, PlayerInventory inventory, int toolBonus = 0)
        {
            if (!IsAvailable || inventory == null || damage <= 0)
            {
                return;
            }

            currentHealth = Mathf.Max(0, CurrentHealth - damage);
            hurtFlashEndsAt = Time.time + 0.15f;
            if (this is AnimalResource) HitFeedback.Report(gameObject, inventory, damage, currentHealth == 0);
            else PlayHarvestImpact(inventory);
            if (currentHealth > 0)
            {
                return;
            }

            // Deactivate before rewards/events, so another hit cannot pay twice.
            CancelGathering();
            harvested = true;
            SetHighlighted(false);
            ApplyVisuals();
            int finalHarvestAmount = harvestAmount + Mathf.Max(0, toolBonus);
            int finalCoinReward = coinReward + Mathf.Max(0, toolBonus) * 2;

            bool scattered = this is AnimalResource animal && animal.ScatterAnimalRewards(finalHarvestAmount, finalCoinReward);
            if (!scattered && UsesPhysicalHarvestDrop)
            {
                // The tree disappears first, then its wood and coin rewards follow the
                // same short hop, float and fast magnet flight used by combat loot.
                float playerAngle = Mathf.Atan2(inventory.transform.position.y - transform.position.y,
                    inventory.transform.position.x - transform.position.x);
                EnemyLootPickup.Scatter(transform.position, transform.parent, Definition.Reward.Kind,
                    finalHarvestAmount, null, playerAngle);
                if (finalCoinReward > 0)
                    EnemyLootPickup.Scatter(transform.position, transform.parent, ItemKind.Coins,
                        finalCoinReward, null, playerAngle);
            }
            else if (!scattered)
            {
                Definition.Reward.Grant(inventory, finalHarvestAmount);
                ResourceFlyweights.Item(ItemKind.Coins).Grant(inventory, finalCoinReward);
                string rewardText = $"+{finalHarvestAmount} {RewardName}";
                inventory.GetComponent<GameFeelFeedback>()?.Pulse(rewardText, transform.position);
                FarmNotificationCenter.Show(finalCoinReward > 0 ? $"{rewardText}, +{finalCoinReward} oro" : rewardText);
            }
            inventory?.RecordGathered(finalHarvestAmount);
            RaiseHarvestEvent();
            GetComponent<ResourceTier>()?.Grant(inventory, UsesPhysicalHarvestDrop);
            Depleted?.Invoke(this);
        }

        private void PlayHarvestImpact(PlayerInventory inventory)
        {
            if (lastFeedbackFrame == Time.frameCount) return;
            lastFeedbackFrame = Time.frameCount;
            bool tree = this is TreeResource;
            Vector3 point = transform.position + Vector3.up * .35f;
            inventory?.GetComponent<AudioFeedback>()?.Play(tree ? CombatSound.Chop : CombatSound.Mine, point, .8f);
            CombatHitParticles.Spawn(point, transform.parent, tree ? ImpactSurface.Leaves : ImpactSurface.Stone, false, false);
        }

        public void Spawn(Vector3 position)
        {
            transform.position = position;
            if(SurvivorFarm.Runtime.Core.PortfolioSession.Active){ResourceTier.Configure(this);SurvivorFarm.Runtime.World.FarmWorldPolish.StyleResource(this);}
            SpawnGeneration++;
            Restore(false);
        }

        public void Restore(bool wasHarvested, int savedHealth = -1)
        {
            harvested = wasHarvested;
            currentHealth = harvested ? 0 : savedHealth < 0 ? maxHealth : Mathf.Clamp(savedHealth, 1, maxHealth);
            hurtFlashEndsAt = 0f;
            CancelGathering();
            SetHighlighted(false);
            ApplyVisuals();
        }

        public float GatherProgress => gathering ? Mathf.Clamp01((Time.time - gatherStartedAt) / Mathf.Max(0.01f, activeGatherDuration)) : 1f;

        private void CacheVisuals()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider2D>(true);
            defaultColor = mainRenderer != null ? mainRenderer.color : Color.white;
        }

        private void ApplyVisuals()
        {
            if (renderers == null || colliders == null)
            {
                CacheVisuals();
            }

            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = !harvested;
            }

            foreach (Collider2D collider in colliders)
            {
                if(collider!=null)collider.enabled = !harvested &&
                    (!(this is TreeResource)||!Core.PortfolioSession.Active||collider is CircleCollider2D&&collider.transform==transform);
            }

            gameObject.SetActive(!harvested);
        }
    }
}
