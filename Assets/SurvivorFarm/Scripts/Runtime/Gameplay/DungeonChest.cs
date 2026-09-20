using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class DungeonChest : WorldInteractable
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
        private bool originalArt;
        private int ironReward, goldReward, rubyReward, foodReward;
        public System.Func<bool> Unlocked;
        public string PersistentId => name;

        public override bool IsAvailable => !opened && gameObject.activeInHierarchy;
        public bool IsOpened => opened;

        private void Awake() => EnsureSolidBody();
        private void EnsureSolidBody()
        {
            var body = GetComponent<BoxCollider2D>();
            if (body == null) body = gameObject.AddComponent<BoxCollider2D>();
            body.isTrigger = false;
            Vector3 scale = transform.lossyScale;
            body.size = new Vector2(.8f / Mathf.Max(.01f, Mathf.Abs(scale.x)), .55f / Mathf.Max(.01f, Mathf.Abs(scale.y)));
            body.offset = Vector2.zero;
        }

        public void ConfigureExpedition(SpriteRenderer visual, int coins, int iron, int gold, int ruby, int food)
        {
            bodyRenderer = visual;
            if (lidRenderer != null) lidRenderer.gameObject.SetActive(false);
            lidRenderer = null; originalArt = true;
            coinsReward = coins; ironReward = iron; goldReward = gold; rubyReward = ruby; foodReward = food;
            woodReward = stoneReward = commonSeedsReward = mineralSeedsReward = magicSeedsReward = 0;
            EnsureSolidBody();
            ApplyVisuals();
        }

        protected override float HighlightScale => 1.12f;

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
            EnsureSolidBody();
            ApplyVisuals();
        }

        public override string GetInteractionLabel(FarmTool selectedTool)
        {
            return opened ? "Cofre abierto" : Unlocked != null && !Unlocked() ? "Cofre sellado: vence al Custodio" : "Interactuar: abrir cofre";
        }

        public override void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            if (!IsAvailable || inventory == null || Unlocked != null && !Unlocked())
            {
                return;
            }

            opened = true;
            var kinds = new[] { ItemKind.Coins, ItemKind.Wood, ItemKind.Stone, ItemKind.Iron, ItemKind.GoldOre, ItemKind.Ruby, ItemKind.Food, ItemKind.CommonSeed, ItemKind.MineralSeed, ItemKind.MagicSeed };
            var amounts = new[] { coinsReward, woodReward, stoneReward, ironReward, goldReward, rubyReward, foodReward, commonSeedsReward, mineralSeedsReward, magicSeedsReward };
            for (int i = 0; i < kinds.Length; i++)
                if (amounts[i] > 0) EnemyLootPickup.Scatter(transform.position, transform.parent, kinds[i], amounts[i], angle: i * 2.399963f);
            AudioFeedback.PlayAt(CombatSound.Drop, transform.position, .8f);
            ApplyVisuals();
        }

        public void Restore(bool wasOpened)
        {
            opened = wasOpened;
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            if (originalArt) { if (bodyRenderer != null) bodyRenderer.color = opened ? new Color(.45f, .5f, .5f) : Color.white; return; }
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
