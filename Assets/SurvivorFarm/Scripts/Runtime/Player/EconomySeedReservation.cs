using SurvivorFarm.Runtime.Gameplay;

namespace SurvivorFarm.Runtime.Player
{
    public sealed class EconomySeedReservation
    {
        public SeedRarity Rarity { get; }
        public string SeedItemId { get; }
        public string CropItemId { get; }
        public bool IsActive { get; internal set; } = true;

        internal EconomySeedReservation(SeedRarity rarity, string seedItemId = null, string cropItemId = null)
        {
            Rarity = rarity;
            SeedItemId = seedItemId;
            CropItemId = cropItemId;
        }
    }
}
