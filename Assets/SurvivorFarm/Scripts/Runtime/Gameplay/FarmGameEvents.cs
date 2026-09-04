using System;

namespace SurvivorFarm.Runtime.Gameplay
{
    public static class FarmGameEvents
    {
        public static event Action TreeHarvested;
        public static event Action RockHarvested;
        public static event Action GrassDug;
        public static event Action SoilHoed;
        public static event Action SeedPlanted;
        public static event Action CropWatered;
        public static event Action CropHarvested;
        public static event Action EnemyDamaged;
        public static event Action EnemyDefeated;

        public static void RaiseTreeHarvested() => TreeHarvested?.Invoke();
        public static void RaiseRockHarvested() => RockHarvested?.Invoke();
        public static void RaiseGrassDug() => GrassDug?.Invoke();
        public static void RaiseSoilHoed() => SoilHoed?.Invoke();
        public static void RaiseSeedPlanted() => SeedPlanted?.Invoke();
        public static void RaiseCropWatered() => CropWatered?.Invoke();
        public static void RaiseCropHarvested() => CropHarvested?.Invoke();
        public static void RaiseEnemyDamaged() => EnemyDamaged?.Invoke();
        public static void RaiseEnemyDefeated() => EnemyDefeated?.Invoke();
    }
}
