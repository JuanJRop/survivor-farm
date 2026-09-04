using UnityEngine;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class LandUnlockZone : MonoBehaviour, IWorldInteractable
    {
        [SerializeField] private int unlockCost = 25;
        [SerializeField] private bool unlocked;
        [SerializeField] private SpriteRenderer blockerRenderer;
        [SerializeField] private Collider2D blockerCollider;
        [SerializeField] private Transform interactionPoint;
        [SerializeField] private LandUnlockZone prerequisiteZone;

        public Transform Transform => interactionPoint != null ? interactionPoint : transform;
        public bool IsAvailable => !unlocked;
        public bool IsUnlocked => unlocked;
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

        public string GetInteractionLabel(FarmTool selectedTool)
        {
            if (unlocked)
            {
                return string.Empty;
            }

            return CanUnlock
                ? $"Interactuar: desbloquear zona por {unlockCost} oro"
                : "Bloqueado: abre una zona vecina primero";
        }

        public void SetHighlighted(bool highlighted)
        {
            if (blockerRenderer == null || unlocked)
            {
                return;
            }

            blockerRenderer.color = highlighted
                ? new Color(0.24f, 0.22f, 0.14f, 0.72f)
                : new Color(0.05f, 0.06f, 0.05f, 0.68f);
        }

        public void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            if (unlocked)
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
            ApplyVisuals();
            FarmNotificationCenter.Show("Zona desbloqueada.");
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
                blockerRenderer.enabled = !unlocked;
                blockerRenderer.color = new Color(0.05f, 0.06f, 0.05f, 0.68f);
            }

            if (blockerCollider != null)
            {
                blockerCollider.enabled = !unlocked;
            }

            if (interactionPoint != null)
            {
                interactionPoint.gameObject.SetActive(!unlocked);
            }
        }
    }
}
