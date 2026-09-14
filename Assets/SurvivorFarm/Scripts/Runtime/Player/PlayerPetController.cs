using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.Player
{
    public sealed class PlayerPetController : MonoBehaviour
    {
        [SerializeField] private PetCompanion prefab;
        [SerializeField] private PetCompanion companion;
        [SerializeField] private bool equipped = true;
        [SerializeField] private Text equipmentLabel;
        public bool Equipped => equipped;
        public PetCompanion Companion => companion;
        public int DamageBonus => isActiveAndEnabled && equipped && companion != null && companion.Definition != null
            ? companion.Definition.PlayerDamageBonus : 0;

        public void Configure(PetCompanion template, PetCompanion instance, Text label)
        {
            prefab = template;
            companion = instance;
            equipmentLabel = label;
            Refresh();
        }

        private void OnEnable() => Refresh();
        private void OnDisable()
        {
            if (companion != null) companion.gameObject.SetActive(false);
        }

        public void ToggleEquipped() => SetEquipped(!equipped);

        public void SetEquipped(bool value)
        {
            equipped = value;
            Refresh();
        }

        private void Refresh()
        {
            if (companion == null && prefab != null && Application.isPlaying)
                companion = Instantiate(prefab, transform.parent);
            if (companion != null)
            {
                companion.ConfigureOwner(this);
                companion.gameObject.SetActive(equipped && isActiveAndEnabled);
                if (equipped) companion.Recall();
            }
            if (equipmentLabel != null)
                equipmentLabel.text = equipped ? "Gato equipado (+1 dano) - Retirar" : "Equipar gato (+1 dano)";
        }
    }
}
