using SurvivorFarm.Runtime.Player;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class RockResource : HarvestableResource
    {
        protected override FarmTool RequiredTool => FarmTool.Pickaxe;
        protected override string InteractionText => "Interactuar: picar con pico";
        protected override string MissingToolText => "Necesitas el pico para romper roca.";
        protected override ItemKind ResourceKind => ItemKind.Stone;
        protected override float GatherDuration => 2.3f;
        protected override int GatherHitCount => 4;
        protected override string GatherStartText => "Picando roca...";
        protected override string GatherPresentText => "Picando roca";

        protected override void RaiseHarvestEvent()
        {
            FarmGameEvents.RaiseRockHarvested();
        }
    }
}
