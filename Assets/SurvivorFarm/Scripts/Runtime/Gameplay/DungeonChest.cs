using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class DungeonChest : MonoBehaviour, IWorldInteractable
    {
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer lidRenderer;
        [SerializeField] private int coinsReward = 8;
        [SerializeField] private int woodReward;
        [SerializeField] private int stoneReward;
        [SerializeField] private int commonSeedsReward;
        [SerializeField] private int mineralSeedsReward;
        [SerializeField] private int magicSeedsReward;

        private bool opened;

        public Transform Transform => transform;
        public bool IsAvailable => !opened && gameObject.activeInHierarchy;
        public bool IsOpened => opened;

        public void Configure(
            SpriteRenderer body,
            SpriteRenderer lid,
            int coins,
            int wood,
            int stone,
            int commonSeeds,
            int mineralSeeds,
            int magicSeeds)
        {
            bodyRenderer = body;
            lidRenderer = lid;
            coinsReward = Mathf.Max(0, coins);
            woodReward = Mathf.Max(0, wood);
            stoneReward = Mathf.Max(0, stone);
            commonSeedsReward = Mathf.Max(0, commonSeeds);
            mineralSeedsReward = Mathf.Max(0, mineralSeeds);
            magicSeedsReward = Mathf.Max(0, magicSeeds);
            ApplyVisuals();
        }

        public string GetInteractionLabel(FarmTool selectedTool)
        {
            return opened ? "Cofre abierto" : "Interactuar: abrir cofre";
        }

        public void SetHighlighted(bool highlighted)
        {
            transform.localScale = highlighted ? Vector3.one * 1.12f : Vector3.one;
        }

        public void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            if (opened)
            {
                return;
            }

            opened = true;
            if (inventory != null)
            {
                inventory.AddCoins(coinsReward);
                inventory.AddWood(woodReward);
                inventory.AddStone(stoneReward);
                inventory.AddSeeds(SeedRarity.Common, commonSeedsReward);
                inventory.AddSeeds(SeedRarity.Mineral, mineralSeedsReward);
                inventory.AddSeeds(SeedRarity.Magic, magicSeedsReward);
            }

            FarmNotificationCenter.Show($"Cofre abierto: +{coinsReward} oro, +{woodReward} madera, +{stoneReward} piedra.");
            ApplyVisuals();
        }

        public void Restore(bool wasOpened)
        {
            opened = wasOpened;
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            Color closedBody = new Color(0.54f, 0.30f, 0.12f);
            Color openBody = new Color(0.23f, 0.16f, 0.09f);

            if (bodyRenderer != null)
            {
                bodyRenderer.color = opened ? openBody : closedBody;
            }

            if (lidRenderer != null)
            {
                lidRenderer.transform.localRotation = opened
                    ? Quaternion.Euler(0f, 0f, 18f)
                    : Quaternion.identity;
                lidRenderer.color = opened ? new Color(0.33f, 0.21f, 0.10f) : new Color(0.76f, 0.52f, 0.18f);
            }
        }
    }
}
