using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class TreeResource : HarvestableResource
    {
        protected override FarmTool RequiredTool => FarmTool.Axe;
        protected override string InteractionText => "Interactuar: talar con hacha";
        protected override string MissingToolText => "Necesitas el hacha para talar.";
        protected override ItemKind ResourceKind => ItemKind.Wood;
        protected override float GatherDuration => 2.6f;
        protected override int GatherHitCount => 5;
        protected override float HighlightScale => 1;
        protected override string GatherStartText => "Talando árbol...";
        protected override string GatherPresentText => "Talando árbol";

        protected override void Awake()
        {
            base.Awake();
            TreeOcclusionFader.Ensure(gameObject);
        }

        protected override void RaiseHarvestEvent()
        {
            FarmGameEvents.RaiseTreeHarvested();
        }
    }
}
