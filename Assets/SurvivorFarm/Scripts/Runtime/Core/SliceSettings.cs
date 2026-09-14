using System;
using UnityEngine;

namespace SurvivorFarm.Runtime.Core
{
    public enum SlicePhase { Introduction, Day, Preparation, Night, Dawn, BossIntro, Boss, Victory, Defeat }
    public enum RaidRole { Chaser, Archer, Brute }

    [Serializable]
    public sealed class SliceDay
    {
        public float preparationSeconds = 240;
        public float nightSeconds = 90;
        public RaidRole[] enemies = { RaidRole.Chaser, RaidRole.Chaser, RaidRole.Chaser };
    }

    [CreateAssetMenu(menuName = "Survivor Farm/Portfolio/Three night settings")]
    public sealed class SliceSettings : ScriptableObject
    {
        public float duskSeconds = 25;
        public int maximumConcurrentEnemies = 8;
        public int coreHealth = 36;
        public int bossHealth = 96;
        public Sprite[] cropStages;
        public SliceDay[] days = {
            new SliceDay { preparationSeconds = 240, nightSeconds = 90, enemies = new[] {
                RaidRole.Chaser, RaidRole.Chaser, RaidRole.Chaser, RaidRole.Chaser,
                RaidRole.Chaser, RaidRole.Chaser, RaidRole.Chaser, RaidRole.Chaser, RaidRole.Chaser } },
            new SliceDay { preparationSeconds = 210, nightSeconds = 120, enemies = new[] {
                RaidRole.Chaser, RaidRole.Archer, RaidRole.Chaser, RaidRole.Brute,
                RaidRole.Chaser, RaidRole.Archer, RaidRole.Chaser, RaidRole.Brute,
                RaidRole.Archer, RaidRole.Chaser, RaidRole.Chaser, RaidRole.Archer, RaidRole.Brute, RaidRole.Chaser } },
            new SliceDay { preparationSeconds = 180, nightSeconds = 120, enemies = new[] {
                RaidRole.Brute, RaidRole.Chaser, RaidRole.Archer, RaidRole.Chaser, RaidRole.Chaser,
                RaidRole.Archer, RaidRole.Brute, RaidRole.Chaser, RaidRole.Archer, RaidRole.Chaser,
                RaidRole.Brute, RaidRole.Archer, RaidRole.Chaser, RaidRole.Brute, RaidRole.Chaser,
                RaidRole.Archer, RaidRole.Chaser, RaidRole.Brute } }
        };
    }

    [Serializable]
    public sealed class SliceSnapshot
    {
        public int day = 1, coreHealth = 36, planted, harvested, trees, rocks, defenses;
        public float elapsed;
        // A save resumes at daylight before its current encounter, with current resources.
        // Projectiles and attack windups are never serialized.
        public bool completed, repaired;
    }
}
