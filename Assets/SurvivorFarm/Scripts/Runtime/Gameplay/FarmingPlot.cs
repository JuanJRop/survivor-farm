using UnityEngine;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class FarmingPlot : MonoBehaviour, IWorldInteractable
    {
        private enum FarmAction
        {
            None,
            Dig,
            Hoe,
            Plant,
            Water
        }

        public enum StartingSurface
        {
            DirtReady,
            Grass
        }

        public enum PlotState
        {
            Grass,
            Dug,
            Tilled,
            SeededDry,
            Growing,
            Ready
        }

        [SerializeField] private string cropName = "fruta";
        [SerializeField] private float growDuration = 8f;
        [SerializeField] private float digDuration = 1.3f;
        [SerializeField] private float hoeDuration = 1.1f;
        [SerializeField] private float plantDuration = 0.9f;
        [SerializeField] private float waterDuration = 1f;
        [SerializeField] private StartingSurface startingSurface = StartingSurface.DirtReady;
        [SerializeField] private SpriteRenderer groundRenderer;
        [SerializeField] private SpriteRenderer cropRenderer;

        private PlotState state = PlotState.Tilled;
        private float readyTime;
        private float actionEndsAt;
        private float actionDuration;
        private bool readyNotificationSent;
        private bool highlighted;
        private PlayerInventory activeInventory;
        private SeedRarity plantedSeedRarity = SeedRarity.Common;
        private FarmAction activeAction = FarmAction.None;

        public Transform Transform => transform;
        public bool IsAvailable => true;
        public int StateId => (int)state;
        public int PlantedSeedRarityId => (int)plantedSeedRarity;
        public float RemainingGrowTime => state == PlotState.Growing ? SecondsRemaining : 0f;

        public string GetInteractionLabel(FarmTool selectedTool)
        {
            if (activeAction != FarmAction.None)
            {
                return $"{GetActionPresentText(activeAction)} {GetProgressText()}";
            }

            switch (state)
            {
                case PlotState.Grass:
                    return selectedTool == FarmTool.Shovel
                        ? "Interactuar: cavar pasto y crear parcela"
                        : "Selecciona la pala para cavar pasto";
                case PlotState.Dug:
                    return selectedTool == FarmTool.Hoe
                        ? "Interactuar: labrar con azada"
                        : "Selecciona la azada para labrar";
                case PlotState.Tilled:
                    return "Interactuar: plantar semilla";
                case PlotState.SeededDry:
                    return selectedTool == FarmTool.WateringCan
                        ? "Interactuar: regar cultivo"
                        : "Selecciona la regadera para regar";
                case PlotState.Growing:
                    return $"Creciendo... {SecondsRemaining:0}s";
                case PlotState.Ready:
                    return "Interactuar: recoger fruta";
                default:
                    return string.Empty;
            }
        }

        private float SecondsRemaining => Mathf.Max(0f, readyTime - Time.time);
        private float ActionSecondsRemaining => Mathf.Max(0f, actionEndsAt - Time.time);
        private float ActionProgress => actionDuration <= 0f ? 1f : Mathf.Clamp01(1f - ActionSecondsRemaining / actionDuration);

        private void Reset()
        {
            groundRenderer = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            if (groundRenderer == null)
            {
                groundRenderer = GetComponent<SpriteRenderer>();
            }

            ResetToStartingSurface();
            ApplyVisuals();
        }

        public void Configure(SpriteRenderer ground, SpriteRenderer crop, StartingSurface surface)
        {
            groundRenderer = ground;
            cropRenderer = crop;
            startingSurface = surface;
            ResetToStartingSurface();
            ApplyVisuals();
        }

        private void Update()
        {
            if (state != PlotState.Growing || Time.time < readyTime)
            {
                return;
            }

            state = PlotState.Ready;
            ApplyVisuals();

            if (!readyNotificationSent)
            {
                readyNotificationSent = true;
                FarmNotificationCenter.Show($"{cropName} lista para recoger.");
            }
        }

        public void SetHighlighted(bool highlighted)
        {
            this.highlighted = highlighted;

            if (groundRenderer == null)
            {
                return;
            }

            if (activeAction != FarmAction.None)
            {
                return;
            }

            groundRenderer.transform.localScale = highlighted ? Vector3.one * 1.08f : Vector3.one;
        }

        public void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            if (activeAction != FarmAction.None)
            {
                FarmNotificationCenter.Show($"{GetActionPresentText(activeAction)} {GetProgressText()}");
                return;
            }

            switch (state)
            {
                case PlotState.Grass:
                    if (selectedTool != FarmTool.Shovel)
                    {
                        FarmNotificationCenter.Show("Necesitas la pala para sacar un cuadrito de tierra.");
                        return;
                    }

                    activeInventory = inventory;
                    PlayerToolUpgradeController upgrades = inventory != null
                        ? inventory.GetComponent<PlayerToolUpgradeController>()
                        : null;
                    float durationMultiplier = upgrades != null ? upgrades.GetDigDurationMultiplier() : 1f;
                    BeginAction(FarmAction.Dig, digDuration * durationMultiplier);
                    return;
                case PlotState.Dug:
                    if (selectedTool != FarmTool.Hoe)
                    {
                        FarmNotificationCenter.Show("Necesitas la azada para labrar la tierra.");
                        return;
                    }

                    BeginAction(FarmAction.Hoe, hoeDuration);
                    return;
                case PlotState.Tilled:
                    if (inventory != null && !inventory.TryConsumeSeed(out plantedSeedRarity))
                    {
                        FarmNotificationCenter.Show("No tienes semillas en el inventario.");
                        return;
                    }

                    PlayPlayerAction(inventory, "Plant");
                    BeginAction(FarmAction.Plant, plantDuration);
                    return;
                case PlotState.SeededDry:
                    if (selectedTool != FarmTool.WateringCan)
                    {
                        FarmNotificationCenter.Show("Necesitas la regadera para regar.");
                        return;
                    }

                    BeginAction(FarmAction.Water, waterDuration);
                    return;
                case PlotState.Growing:
                    FarmNotificationCenter.Show($"Aun le faltan {SecondsRemaining:0}s.");
                    break;
                case PlotState.Ready:
                    state = PlotState.Tilled;
                    string harvestName = GetCropDisplayName();
                    if (inventory != null)
                    {
                        PlayPlayerAction(inventory, "PickUp");
                        GrantHarvest(inventory);
                    }

                    FarmGameEvents.RaiseCropHarvested();
                    FarmNotificationCenter.Show($"Recogiste {harvestName}. La tierra queda lista.");
                    break;
            }

            ApplyVisuals();
        }

        public void Restore(int savedState, float remainingGrowTime)
        {
            Restore(savedState, remainingGrowTime, (int)SeedRarity.Common);
        }

        public void Restore(int savedState, float remainingGrowTime, int savedSeedRarity)
        {
            state = (PlotState)Mathf.Clamp(savedState, 0, System.Enum.GetValues(typeof(PlotState)).Length - 1);
            plantedSeedRarity = (SeedRarity)Mathf.Clamp(savedSeedRarity, 0, System.Enum.GetValues(typeof(SeedRarity)).Length - 1);
            readyTime = state == PlotState.Growing ? Time.time + Mathf.Max(0f, remainingGrowTime) : 0f;
            readyNotificationSent = state != PlotState.Growing;
            actionEndsAt = 0f;
            actionDuration = 0f;
            activeInventory = null;
            activeAction = FarmAction.None;
            ApplyVisuals();
        }

        private void BeginAction(FarmAction action, float duration)
        {
            activeAction = action;
            actionDuration = Mathf.Max(0.1f, duration);
            actionEndsAt = Time.time + actionDuration;
            FarmNotificationCenter.Show($"{GetActionStartText(action)} {GetProgressText()}");
            ApplyVisuals();
        }

        private void LateUpdate()
        {
            if (activeAction == FarmAction.None)
            {
                return;
            }

            ApplyWorkingVisuals();
            FarmNotificationCenter.SetPrompt($"{GetActionPresentText(activeAction)} {GetProgressText()}");

            if (Time.time < actionEndsAt)
            {
                return;
            }

            CompleteAction(activeAction);
            activeAction = FarmAction.None;
            actionDuration = 0f;
            actionEndsAt = 0f;
            activeInventory = null;
            ApplyVisuals();
        }

        private void CompleteAction(FarmAction action)
        {
            switch (action)
            {
                case FarmAction.Dig:
                    state = PlotState.Dug;
                    FarmGameEvents.RaiseGrassDug();
                    PlayerToolUpgradeController upgrades = activeInventory != null
                        ? activeInventory.GetComponent<PlayerToolUpgradeController>()
                        : null;
                    float seedChance = 0.35f + (upgrades != null ? upgrades.GetSeedFindChanceBonus() : 0f);
                    if (activeInventory != null && Random.value < seedChance)
                    {
                        SeedRarity foundRarity = RollFoundSeedRarity();
                        activeInventory.AddSeeds(foundRarity, 1);
                        FarmNotificationCenter.Show($"Pasto cavado. Encontraste 1 semilla {GetSeedRarityName(foundRarity)}. Siguiente: azada.");
                    }
                    else
                    {
                        FarmNotificationCenter.Show("Pasto cavado. Siguiente: azada.");
                    }
                    break;
                case FarmAction.Hoe:
                    state = PlotState.Tilled;
                    FarmGameEvents.RaiseSoilHoed();
                    FarmNotificationCenter.Show("Tierra arada. Ya puedes plantar.");
                    break;
                case FarmAction.Plant:
                    state = PlotState.SeededDry;
                    FarmGameEvents.RaiseSeedPlanted();
                    FarmNotificationCenter.Show($"Semilla {GetSeedRarityName(plantedSeedRarity)} plantada. Necesita agua.");
                    break;
                case FarmAction.Water:
                    state = PlotState.Growing;
                    readyTime = Time.time + growDuration;
                    readyNotificationSent = false;
                    FarmGameEvents.RaiseCropWatered();
                    FarmNotificationCenter.Show("Cultivo regado. Toca esperar.");
                    break;
            }
        }

        private void ResetToStartingSurface()
        {
            state = startingSurface == StartingSurface.Grass ? PlotState.Grass : PlotState.Tilled;
            readyTime = 0f;
            actionEndsAt = 0f;
            actionDuration = 0f;
            readyNotificationSent = false;
            highlighted = false;
            activeInventory = null;
            plantedSeedRarity = SeedRarity.Common;
            activeAction = FarmAction.None;
        }

        private static SeedRarity RollFoundSeedRarity()
        {
            float roll = Random.value;
            if (roll < 0.05f)
            {
                return SeedRarity.Magic;
            }

            if (roll < 0.22f)
            {
                return SeedRarity.Mineral;
            }

            return SeedRarity.Common;
        }

        private void GrantHarvest(PlayerInventory inventory)
        {
            switch (plantedSeedRarity)
            {
                case SeedRarity.Mineral:
                    inventory.AddFruit(1);
                    inventory.AddStone(2);
                    if (Random.value < 0.45f)
                    {
                        inventory.AddSeeds(SeedRarity.Mineral, 1);
                    }
                    break;
                case SeedRarity.Magic:
                    inventory.AddFruit(2);
                    inventory.AddCoins(8);
                    if (Random.value < 0.35f)
                    {
                        inventory.AddSeeds(SeedRarity.Magic, 1);
                    }
                    break;
                default:
                    inventory.AddFruit(1);
                    inventory.AddSeeds(SeedRarity.Common, 1);
                    break;
            }

            plantedSeedRarity = SeedRarity.Common;
        }

        private string GetCropDisplayName()
        {
            switch (plantedSeedRarity)
            {
                case SeedRarity.Mineral:
                    return "fruta mineral";
                case SeedRarity.Magic:
                    return "fruta magica";
                default:
                    return cropName;
            }
        }

        private static string GetSeedRarityName(SeedRarity rarity)
        {
            switch (rarity)
            {
                case SeedRarity.Mineral:
                    return "mineral";
                case SeedRarity.Magic:
                    return "magica";
                default:
                    return "comun";
            }
        }

        private static string GetActionStartText(FarmAction action)
        {
            switch (action)
            {
                case FarmAction.Dig:
                    return "Usando pala para cavar...";
                case FarmAction.Hoe:
                    return "Usando azada...";
                case FarmAction.Plant:
                    return "Plantando semilla...";
                case FarmAction.Water:
                    return "Regando cultivo...";
                default:
                    return "Trabajando...";
            }
        }

        private static void PlayPlayerAction(PlayerInventory inventory, string actionName)
        {
            PlayerCharacterAnimator animator = inventory != null
                ? inventory.GetComponent<PlayerCharacterAnimator>()
                : null;
            animator?.PlayNamedAction(actionName);
        }

        private static string GetActionPresentText(FarmAction action)
        {
            switch (action)
            {
                case FarmAction.Dig:
                    return "Cavando con pala";
                case FarmAction.Hoe:
                    return "Arando con azada";
                case FarmAction.Plant:
                    return "Plantando semilla";
                case FarmAction.Water:
                    return "Regando";
                default:
                    return "Trabajando";
            }
        }

        private string GetProgressText()
        {
            int percent = Mathf.RoundToInt(ActionProgress * 100f);
            return $"[{BuildProgressBar(ActionProgress)}] {percent}%";
        }

        private static string BuildProgressBar(float progress)
        {
            const int segmentCount = 10;
            int filledSegments = Mathf.RoundToInt(Mathf.Clamp01(progress) * segmentCount);
            return new string('#', filledSegments) + new string('-', segmentCount - filledSegments);
        }

        private void ApplyVisuals()
        {
            if (groundRenderer != null)
            {
                groundRenderer.color = GetGroundColor();
                groundRenderer.transform.localScale = highlighted ? Vector3.one * 1.08f : Vector3.one;
            }

            if (cropRenderer != null)
            {
                cropRenderer.enabled = state == PlotState.SeededDry || state == PlotState.Growing || state == PlotState.Ready;
                cropRenderer.color = GetCropColor();
                cropRenderer.transform.localScale = GetCropScale();
            }
        }

        private void ApplyWorkingVisuals()
        {
            if (groundRenderer == null)
            {
                return;
            }

            float pulse = 0.88f + Mathf.PingPong(Time.time * 3.5f, 0.16f);
            groundRenderer.color = Color.Lerp(GetGroundColor(), GetActionTint(activeAction), ActionProgress * 0.65f);
            groundRenderer.transform.localScale = Vector3.one * pulse;

            if (cropRenderer != null && activeAction == FarmAction.Plant)
            {
                cropRenderer.enabled = true;
                cropRenderer.color = new Color(0.76f, 0.52f, 0.22f);
                cropRenderer.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.28f, ActionProgress);
            }
        }

        private static Color GetActionTint(FarmAction action)
        {
            switch (action)
            {
                case FarmAction.Dig:
                    return new Color(0.44f, 0.28f, 0.14f);
                case FarmAction.Hoe:
                    return new Color(0.23f, 0.14f, 0.08f);
                case FarmAction.Plant:
                    return new Color(0.50f, 0.34f, 0.16f);
                case FarmAction.Water:
                    return new Color(0.18f, 0.36f, 0.54f);
                default:
                    return Color.white;
            }
        }

        private Color GetGroundColor()
        {
            switch (state)
            {
                case PlotState.Grass:
                    return new Color(0.36f, 0.58f, 0.30f, 0.18f);
                case PlotState.Dug:
                    return new Color(0.38f, 0.24f, 0.13f);
                case PlotState.Tilled:
                    return new Color(0.24f, 0.15f, 0.08f);
                case PlotState.SeededDry:
                    return new Color(0.31f, 0.22f, 0.12f);
                case PlotState.Growing:
                    return new Color(0.20f, 0.24f, 0.13f);
                case PlotState.Ready:
                    return new Color(0.24f, 0.30f, 0.12f);
                default:
                    return Color.white;
            }
        }

        private Color GetCropColor()
        {
            switch (state)
            {
                case PlotState.SeededDry:
                    return new Color(0.56f, 0.45f, 0.25f);
                case PlotState.Growing:
                    return new Color(0.30f, 0.72f, 0.28f);
                case PlotState.Ready:
                    if (plantedSeedRarity == SeedRarity.Magic)
                    {
                        return new Color(0.74f, 0.38f, 1f);
                    }

                    if (plantedSeedRarity == SeedRarity.Mineral)
                    {
                        return new Color(0.58f, 0.75f, 0.84f);
                    }

                    return new Color(1f, 0.22f, 0.16f);
                default:
                    return Color.clear;
            }
        }

        private Vector3 GetCropScale()
        {
            switch (state)
            {
                case PlotState.SeededDry:
                    return Vector3.one * 0.25f;
                case PlotState.Growing:
                    float progress = Mathf.InverseLerp(readyTime - growDuration, readyTime, Time.time);
                    return Vector3.one * Mathf.Lerp(0.35f, 0.75f, progress);
                case PlotState.Ready:
                    return Vector3.one * 0.9f;
                default:
                    return Vector3.zero;
            }
        }
    }
}
