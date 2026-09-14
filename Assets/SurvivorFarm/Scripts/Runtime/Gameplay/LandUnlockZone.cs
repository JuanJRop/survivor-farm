using UnityEngine;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class LandUnlockZone : WorldInteractable
    {
        [SerializeField] private int unlockCost = 25;
        [SerializeField] private bool unlocked;
        [SerializeField] private SpriteRenderer blockerRenderer;
        [SerializeField] private Collider2D blockerCollider;
        [SerializeField] private Transform interactionPoint;
        [SerializeField] private LandUnlockZone prerequisiteZone;
        [SerializeField] private bool openExploration;
        [SerializeField] private RepairableBridge explorationBridge;

        public override Transform Transform => interactionPoint != null ? interactionPoint : transform;
        public override bool IsAvailable => !openExploration && !unlocked;
        public bool IsUnlocked => openExploration ? explorationBridge == null || explorationBridge.IsRepaired : unlocked;

        public void ConfigureExploration(RepairableBridge bridge)
        {
            openExploration = true;
            explorationBridge = bridge;
            ApplyVisuals();
        }
        public string RegionDescription => name switch
        {
            "West Zone" => "Bosque · reserva de madera",
            "East Zone" => "Cantera · reserva de piedra",
            "North Zone" => "Campamento · provisiones",
            "South Zone" => "Pradera · provisiones",
            "North West Zone" => "Bosque antiguo · anillo de sustento",
            "North East Zone" => "Ruinas · acceso a la mazmorra y gemas",
            "South West Zone" => "Vergel · amuleto de recuperación",
            "South East Zone" => "Yacimiento · minerales",
            _ => "Frontera · recursos"
        };
        private void GrantDiscovery(PlayerInventory inventory)
        {
            switch(name)
            {
                case "West Zone": inventory.AddWood(12); break;
                case "East Zone": inventory.AddStone(12); break;
                case "North Zone": inventory.AddFruit(4); break;
                case "South Zone": inventory.AddFood(3); break;
                case "North West Zone": inventory.AddEquipment("Ring"); break;
                case "North East Zone": inventory.AddItem("Ruby",1); break;
                case "South West Zone": inventory.AddEquipment("Amulet"); break;
                case "South East Zone": inventory.AddItem("GoldOre",2); break;
            }
        }
        private bool CanUnlock => prerequisiteZone == null || prerequisiteZone.IsUnlocked;

        public void Configure(int cost, bool startsUnlocked, SpriteRenderer renderer, Collider2D collider, Transform point)
        {
            Configure(cost, startsUnlocked, renderer, collider, point, null);
        }

        public void Configure(
            int cost,
            bool startsUnlocked,
            SpriteRenderer renderer,
            Collider2D collider,
            Transform point,
            LandUnlockZone prerequisite)
        {
            unlockCost = Mathf.Max(0, cost);
            blockerRenderer = renderer;
            blockerCollider = collider;
            interactionPoint = point;
            prerequisiteZone = prerequisite;
            Restore(startsUnlocked);
        }

        public override string GetInteractionLabel(FarmTool selectedTool)
        {
            if (!IsAvailable)
            {
                return string.Empty;
            }

            return CanUnlock
                ? $"{RegionDescription} · abrir por {unlockCost} oro"
                : "Bloqueado: abre una zona vecina primero";
        }

        public override void SetHighlighted(bool highlighted)
        {
            if (blockerRenderer == null || unlocked)
            {
                return;
            }

            blockerRenderer.color = highlighted
                ? new Color(0.24f, 0.22f, 0.14f, 0.72f)
                : new Color(0.05f, 0.06f, 0.05f, 0.68f);
        }

        public override void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            if (!IsAvailable)
            {
                return;
            }

            if (!CanUnlock)
            {
                FarmNotificationCenter.Show("Primero desbloquea una zona vecina.");
                return;
            }

            if (inventory == null || !inventory.TrySpendCoins(unlockCost))
            {
                FarmNotificationCenter.Show($"Necesitas {unlockCost} oro para desbloquear.");
                return;
            }

            unlocked = true;
            GrantDiscovery(inventory);
            ApplyVisuals();
            FarmNotificationCenter.Show(RegionDescription + ": recompensa de descubrimiento recibida.");
        }

        public void Restore(bool isUnlocked)
        {
            unlocked = isUnlocked;
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            if (blockerRenderer != null)
            {
                blockerRenderer.enabled = !openExploration && !unlocked;
                blockerRenderer.color = new Color(0.05f, 0.06f, 0.05f, 0.68f);
            }

            if (blockerCollider != null)
            {
                blockerCollider.enabled = !openExploration && !unlocked;
            }

            if (interactionPoint != null)
            {
                interactionPoint.gameObject.SetActive(!openExploration && !unlocked);
            }
        }
    }
}
