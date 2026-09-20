using SurvivorFarm.Runtime.Player;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Shared damage contract keeps friendly defenses out of player targeting.</summary>
    public static class DamageRules
    {
        public static bool CanPlayerHit(IDamageable target) => target != null && target.IsAlive &&
            !(target is PlayerSurvivalStats) && !(target is FarmDefense) && !(target is VillageResidentHealth) && !(target is VillageHouseHealth);
    }
}
