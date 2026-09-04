using UnityEngine;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class HarvestableResource : MonoBehaviour, IWorldInteractable
    {
        public enum ResourceKind
        {
            Tree,
            Rock
        }

        [SerializeField] private ResourceKind kind = ResourceKind.Tree;
        [SerializeField] private SpriteRenderer mainRenderer;
        [SerializeField] private SpriteRenderer secondaryRenderer;
        [SerializeField] private Collider2D blockingCollider;
        [SerializeField] private int harvestAmount = 2;
        [SerializeField] private int coinReward = 5;

        private bool harvested;

        public Transform Transform => transform;
        public bool IsAvailable => !harvested;
        public bool IsHarvested => harvested;

        private void Awake()
        {
            if (mainRenderer == null)
            {
                mainRenderer = GetComponent<SpriteRenderer>();
            }

            if (blockingCollider == null)
            {
                blockingCollider = GetComponent<Collider2D>();
            }
        }

        public void Configure(ResourceKind resourceKind, SpriteRenderer primary, SpriteRenderer secondary, int amount)
        {
            Configure(resourceKind, primary, secondary, amount, coinReward);
        }

        public void Configure(ResourceKind resourceKind, SpriteRenderer primary, SpriteRenderer secondary, int amount, int coins)
        {
            kind = resourceKind;
            mainRenderer = primary;
            secondaryRenderer = secondary;
            blockingCollider = GetComponent<Collider2D>();
            harvestAmount = Mathf.Max(1, amount);
            coinReward = Mathf.Max(0, coins);
            ApplyVisuals();
        }

        public string GetInteractionLabel(FarmTool selectedTool)
        {
            if (harvested)
            {
                return string.Empty;
            }

            return kind == ResourceKind.Tree ? "Interactuar: talar con hacha" : "Interactuar: picar con pico";
        }

        public void SetHighlighted(bool highlighted)
        {
            transform.localScale = highlighted ? Vector3.one * 1.12f : Vector3.one;
        }

        public void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            if (harvested)
            {
                return;
            }

            if (kind == ResourceKind.Tree && selectedTool != FarmTool.Axe)
            {
                FarmNotificationCenter.Show("Necesitas el hacha para talar.");
                return;
            }

            if (kind == ResourceKind.Rock && selectedTool != FarmTool.Pickaxe)
            {
                FarmNotificationCenter.Show("Necesitas el pico para romper roca.");
                return;
            }

            harvested = true;
            SetHighlighted(false);

            PlayerToolUpgradeController upgrades = inventory != null
                ? inventory.GetComponent<PlayerToolUpgradeController>()
                : null;
            int toolBonus = upgrades != null ? upgrades.GetResourceBonus(selectedTool) : 0;
            int finalHarvestAmount = harvestAmount + toolBonus;
            int finalCoinReward = coinReward + toolBonus * 2;

            if (kind == ResourceKind.Tree)
            {
                inventory?.AddWood(finalHarvestAmount);
                inventory?.AddCoins(finalCoinReward);
                FarmNotificationCenter.Show($"+{finalHarvestAmount} madera, +{finalCoinReward} oro");
                FarmGameEvents.RaiseTreeHarvested();
            }
            else
            {
                inventory?.AddStone(finalHarvestAmount);
                inventory?.AddCoins(finalCoinReward);
                FarmNotificationCenter.Show($"+{finalHarvestAmount} piedra, +{finalCoinReward} oro");
                FarmGameEvents.RaiseRockHarvested();
            }

            ApplyVisuals();
        }

        public void Restore(bool wasHarvested)
        {
            harvested = wasHarvested;
            SetHighlighted(false);
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            if (mainRenderer != null)
            {
                mainRenderer.enabled = !harvested;
            }

            if (secondaryRenderer != null)
            {
                secondaryRenderer.enabled = !harvested;
            }

            if (blockingCollider != null)
            {
                blockingCollider.enabled = !harvested;
            }
        }
    }
}
