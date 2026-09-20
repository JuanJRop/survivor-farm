using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Small, persistent hand-picked vegetation. It never pays twice or trains a pickaxe.</summary>
    public sealed class ForagePlant : WorldInteractable
    {
        public bool Fruit;
        public string Id { get; private set; }
        public bool Collected { get; private set; }
        public override bool IsAvailable => !Collected && isActiveAndEnabled;
        protected override float HighlightScale => 1;
        public void Configure(bool fruit)
        {
            Fruit = fruit;
            Id = System.FormattableString.Invariant($"plant:{transform.position.x:F3}:{transform.position.y:F3}");
            var area = GetComponent<CircleCollider2D>();
            if(area==null)area=gameObject.AddComponent<CircleCollider2D>();
            area.radius = .25f / Mathf.Max(.01f, transform.lossyScale.x); area.isTrigger = true;
            if (fruit) World.TreeOcclusionFader.Ensure(gameObject);
        }
        public override string GetInteractionLabel(FarmTool tool) => Fruit ? "E · recoger arbusto · 2 frutos" : "E · retirar flores · 1 semilla";
        public override void Interact(FarmTool tool, PlayerInventory player)
        {
            if (!IsAvailable || player == null || Vector2.Distance(player.transform.position, transform.position) > 1.5f) return;
            Restore(true);
            if (Fruit) player.AddFruit(2); else player.AddSeeds(1);
            AudioFeedback.PlayAt(CombatSound.Pickup, transform.position);
            CombatHitParticles.Spawn(transform.position, transform.parent, ImpactSurface.Leaves, false, false);
            FarmNotificationCenter.Show(Fruit ? "+2 frutos" : "+1 semilla");
        }
        public void Restore(bool collected) { Collected = collected; gameObject.SetActive(!collected); }
    }
}
