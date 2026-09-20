using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    /// <summary>Only the chain state. The combat controller owns input and hit resolution.</summary>
    [DisallowMultipleComponent]
    public sealed class ComboController : MonoBehaviour
    {
        [SerializeField] private MeleeComboDefinition definition;
        private MeleeComboDefinition fallback;
        private string weapon;
        private int nextIndex;
        private float expiresAt;
        public int StepNumber { get; private set; }
        public MeleeComboDefinition Definition => definition;
        public float WindowRemaining => Mathf.Max(0, expiresAt - Time.time);

        private void Awake()
        {
            if (definition == null) definition = Resources.Load<MeleeComboDefinition>("SwordCombo");
            if (definition == null) definition = fallback = ScriptableObject.CreateInstance<MeleeComboDefinition>();
        }

        public void Configure(MeleeComboDefinition value) { if (value != null) definition = value; ResetChain(); }

        public void SelectWeapon(string identity)
        {
            if (weapon == identity) return;
            weapon = identity;
            ResetChain();
        }

        public ComboAttack Begin(float now)
        {
            if (now > expiresAt) ResetChain();
            var attacks = definition.attacks;
            if (attacks == null || attacks.Length == 0) return null;
            nextIndex %= attacks.Length;
            var attack = attacks[nextIndex];
            if (attack == null) { ResetChain(); return null; }
            StepNumber = nextIndex + 1;
            nextIndex = (nextIndex + 1) % attacks.Length;
            expiresAt = now + Mathf.Max(.15f, attack.duration) + definition.continuationWindow;
            return attack;
        }

        public void ResetChain() { nextIndex = 0; expiresAt = 0; StepNumber = 0; }
        public ComboAttack BeginCharged() { ResetChain(); return definition.chargedAttack; }
        private void Update() { if (StepNumber > 0 && Time.time > expiresAt) ResetChain(); }
        private void OnDisable() => ResetChain();
        private void OnDestroy() { if (fallback != null) Destroy(fallback); }
    }
}
