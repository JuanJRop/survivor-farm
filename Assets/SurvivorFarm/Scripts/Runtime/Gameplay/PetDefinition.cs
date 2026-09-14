using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    [CreateAssetMenu(menuName = "Survivor Farm/Pet Definition")]
    public sealed class PetDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Gato";
        [SerializeField, Min(0)] private int playerDamageBonus = 1;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Min(0.1f)] private float attackCooldown = 1.4f;
        [SerializeField, Min(0.1f)] private float speed = 3.8f;
        [SerializeField] private Sprite[] walkFrames = new Sprite[0];
        [SerializeField] private Sprite[] runFrames = new Sprite[0];
        public string DisplayName => displayName;
        public int PlayerDamageBonus => Mathf.Max(0, playerDamageBonus);
        public int Damage => Mathf.Max(1, damage);
        public float AttackCooldown => Mathf.Max(0.1f, attackCooldown);
        public float Speed => Mathf.Max(0.1f, speed);

        public Sprite GetFrame(int direction, bool moving, bool running, float elapsed)
        {
            var frames = running && runFrames.Length >= 12 ? runFrames : walkFrames;
            if (frames.Length < 12) return null;
            int frame = moving ? Mathf.FloorToInt(elapsed * (running ? 12f : 8f)) % 4 : 0;
            return frames[Mathf.Clamp(direction, 0, 2) * 4 + frame];
        }
    }
}
