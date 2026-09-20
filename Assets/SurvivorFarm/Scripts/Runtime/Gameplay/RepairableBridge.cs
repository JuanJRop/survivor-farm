using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class RepairableBridge : WorldInteractable
    {
        [SerializeField, Min(1)] private int woodCost = 20;
        [SerializeField, Min(1)] private int stoneCost = 10;
        [SerializeField] private bool repaired;
        [SerializeField] private Collider2D crossingBlocker;
        [SerializeField] private GameObject completeDeck;
        [SerializeField] private GameObject brokenDeck;
        [SerializeField] private Transform interactionPoint;
        public bool IsRepaired => repaired;
        public override bool IsAvailable => !repaired;
        public override Transform Transform => interactionPoint != null ? interactionPoint : transform;

        public void Configure(Collider2D blocker, GameObject complete, GameObject broken, Transform point)
        {
            crossingBlocker = blocker;
            completeDeck = complete;
            brokenDeck = broken;
            interactionPoint = point;
            Refresh();
        }

        protected override void OnEnable() { base.OnEnable(); Refresh(); }
        public override string GetInteractionLabel(FarmTool selectedTool) => repaired ? string.Empty :
            $"Reparar puente: {woodCost} madera y {stoneCost} piedra";
        public override void SetHighlighted(bool highlighted) { }

        public override void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            if (repaired || inventory == null) return;
            if (!inventory.TrySpendMaterials(woodCost, stoneCost))
            {
                FarmNotificationCenter.Show(GetInteractionLabel(selectedTool));
                return;
            }
            Restore(true);
            FarmNotificationCenter.Show("Puente reparado. El bosque del norte y las ruinas ya son accesibles.");
        }

        public void Restore(bool value) { repaired = Core.PortfolioSession.Active || value; Refresh(); }

        public void RecoverLegacyPosition(Transform player)
        {
            if (player == null) return;
            Vector3 offset = player.position - transform.position;
            if (Mathf.Abs(offset.x) >= 37f || Mathf.Abs(offset.y) >= 1.4f ||
                (repaired && Mathf.Abs(offset.x) < .7f)) return;
            var position = player.position;
            position.y = transform.position.y + (offset.y < 0 ? -1.6f : 1.6f);
            player.position = position;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.position = position;
        }

        private void Refresh()
        {
            if (crossingBlocker != null) crossingBlocker.enabled = !repaired;
            if (completeDeck != null) completeDeck.SetActive(repaired);
            if (brokenDeck != null) brokenDeck.SetActive(!repaired);
        }
    }
}
