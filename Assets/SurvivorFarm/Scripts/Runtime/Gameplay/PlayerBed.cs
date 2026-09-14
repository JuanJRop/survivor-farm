using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class PlayerBed : WorldInteractable
    {
        [SerializeField] private SpriteRenderer bedRenderer;
        [SerializeField] private PlayerCraftingController crafting;
        [SerializeField] private DayNightCycle clock;

        public void Configure(SpriteRenderer visual, PlayerCraftingController owner, DayNightCycle cycle)
        {
            Unsubscribe();
            bedRenderer = visual;
            crafting = owner;
            clock = cycle;
            if (isActiveAndEnabled) Subscribe();
            RefreshVisual();
        }

        private void OnEnable()
        {
            if (crafting == null) crafting = FindFirstObjectByType<PlayerCraftingController>();
            if (clock == null) clock = FindFirstObjectByType<DayNightCycle>();
            Subscribe();
            RefreshVisual();
        }

        private void Subscribe()
        {
            if (crafting == null) return;
            crafting.CraftingChanged -= RefreshVisual;
            crafting.CraftingChanged += RefreshVisual;
        }

        private void Unsubscribe()
        {
            if (crafting != null) crafting.CraftingChanged -= RefreshVisual;
        }

        private void OnDisable() => Unsubscribe();

        private void RefreshVisual()
        {
            if (bedRenderer != null)
                bedRenderer.color = new Color(1f, 1f, 1f, crafting != null && crafting.BedBuilt ? 1f : 0.3f);
        }

        public override string GetInteractionLabel(FarmTool selectedTool)
        {
            if (crafting == null || !crafting.BedBuilt)
                return $"Construir cama: {PlayerCraftingController.BedWoodCost} madera, {PlayerCraftingController.BedStoneCost} piedra";
            return clock != null && clock.IsNight ? "Dormir hasta las 08:00" : "Solo puedes dormir de noche (21:00 - 05:00)";
        }

        public override void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            if (inventory == null || crafting == null || inventory.gameObject != crafting.gameObject) return;
            inventory.GetComponent<PlayerMovementController>()?.StopMovement();
            if (!crafting.BedBuilt)
            {
                if (crafting.CraftBed()) FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);
                return;
            }
            var stats = inventory.GetComponent<PlayerSurvivalStats>();
            if (stats != null && stats.CurrentHealth <= 0) return;
            if (clock == null || !clock.TrySleepUntilMorning())
            {
                FarmNotificationCenter.Show("Solo puedes dormir de noche (21:00 - 05:00).");
                return;
            }
            if (stats != null) stats.Restore(stats.MaxHealth, stats.MaxHealth, stats.HungerPercent);
            inventory.GetComponent<PlayerRespawnController>()?.SetCheckpoint(inventory.transform.position);
            inventory.GetComponent<PlayerCharacterAnimator>()?.PlayAction("Sleep", 1.2f);
            FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);
            FarmNotificationCenter.Show($"Buenos dias. Dia {clock.Day}, 08:00.");
        }
    }
}
