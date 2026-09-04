using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class BaseHouse : MonoBehaviour, IWorldInteractable
    {
        [SerializeField] private SpriteRenderer[] renderers = new SpriteRenderer[0];

        public Transform Transform => transform;
        public bool IsAvailable => true;

        public void Configure(params SpriteRenderer[] houseRenderers)
        {
            renderers = houseRenderers ?? new SpriteRenderer[0];
        }

        public string GetInteractionLabel(FarmTool selectedTool)
        {
            return "Interactuar: descansar y guardar en casa";
        }

        public void SetHighlighted(bool highlighted)
        {
            transform.localScale = highlighted ? Vector3.one * 1.08f : Vector3.one;
        }

        public void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            PlayerSurvivalStats survivalStats = inventory != null
                ? inventory.GetComponent<PlayerSurvivalStats>()
                : null;

            if (survivalStats != null)
            {
                survivalStats.Restore(survivalStats.MaxHealth, survivalStats.MaxHealth, 1f);
            }

            GameSaveSystem saveSystem = FindFirstObjectByType<GameSaveSystem>();
            saveSystem?.SaveGame(true);
            FarmNotificationCenter.Show("Descansaste en casa. Partida guardada.");
        }
    }
}
