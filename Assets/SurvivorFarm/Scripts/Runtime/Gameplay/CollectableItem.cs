using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public abstract class CollectableItem : MonoBehaviour
    {
        [SerializeField] private ItemDefinition definition;
        public ItemDefinition Definition => definition != null ? definition : definition = ResourceFlyweights.Item(ItemKind.Fruit);
        public void SetDefinition(ItemDefinition sharedDefinition) { definition = sharedDefinition; }
        [SerializeField] private int amount = 1;
        private bool collected;

        protected int Amount => Mathf.Max(1, amount);

        public void Configure(int pickupAmount)
        {
            amount = Mathf.Max(1, pickupAmount);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || !other.TryGetComponent(out PlayerInventory inventory))
            {
                return;
            }

            collected = true;
            inventory.GetComponent<PlayerCharacterAnimator>()?.PlayNamedAction("PickUp");
            Definition.Grant(inventory, Amount);
            inventory.RecordGathered(Amount);
            FarmNotificationCenter.Show(GetPickupMessage());
            Destroy(gameObject);
        }

        protected virtual string GetPickupMessage() => $"+{Amount} {Definition.DisplayName}";
    }
}
