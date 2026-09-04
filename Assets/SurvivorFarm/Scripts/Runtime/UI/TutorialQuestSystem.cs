using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    public sealed class TutorialQuestSystem : MonoBehaviour
    {
        [SerializeField] private Text questText;

        private int questIndex;
        private int questProgress;

        public int QuestIndex => questIndex;
        public int QuestProgress => questProgress;

        public void Configure(Text text)
        {
            questText = text;
            Subscribe();
            Refresh();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        public void Restore(int savedQuestIndex, int savedQuestProgress)
        {
            questIndex = Mathf.Clamp(savedQuestIndex, 0, GetQuestCount() - 1);
            questProgress = Mathf.Max(0, savedQuestProgress);
            Refresh();
        }

        private void Subscribe()
        {
            FarmGameEvents.TreeHarvested += OnTreeHarvested;
            FarmGameEvents.RockHarvested += OnRockHarvested;
            FarmGameEvents.GrassDug += OnGrassDug;
            FarmGameEvents.SoilHoed += OnSoilHoed;
            FarmGameEvents.SeedPlanted += OnSeedPlanted;
            FarmGameEvents.CropWatered += OnCropWatered;
            FarmGameEvents.CropHarvested += OnCropHarvested;
            FarmGameEvents.EnemyDefeated += OnEnemyDefeated;
        }

        private void Unsubscribe()
        {
            FarmGameEvents.TreeHarvested -= OnTreeHarvested;
            FarmGameEvents.RockHarvested -= OnRockHarvested;
            FarmGameEvents.GrassDug -= OnGrassDug;
            FarmGameEvents.SoilHoed -= OnSoilHoed;
            FarmGameEvents.SeedPlanted -= OnSeedPlanted;
            FarmGameEvents.CropWatered -= OnCropWatered;
            FarmGameEvents.CropHarvested -= OnCropHarvested;
            FarmGameEvents.EnemyDefeated -= OnEnemyDefeated;
        }

        private void OnTreeHarvested()
        {
            AdvanceIf(0, 1);
        }

        private void OnRockHarvested()
        {
            AdvanceIf(1, 1);
        }

        private void OnGrassDug()
        {
            AdvanceIf(2, 1);
        }

        private void OnSoilHoed()
        {
            AdvanceIf(3, 1);
        }

        private void OnSeedPlanted()
        {
            AdvanceIf(4, 1);
        }

        private void OnCropWatered()
        {
            AdvanceIf(5, 1);
        }

        private void OnCropHarvested()
        {
            AdvanceIf(6, 1);
        }

        private void OnEnemyDefeated()
        {
            AdvanceIf(7, 1);
        }

        private void AdvanceIf(int expectedQuestIndex, int requiredAmount)
        {
            if (questIndex != expectedQuestIndex)
            {
                return;
            }

            questProgress++;
            if (questProgress >= requiredAmount)
            {
                questIndex = Mathf.Min(questIndex + 1, GetQuestCount() - 1);
                questProgress = 0;
                FarmNotificationCenter.Show("Mision completada.");
            }

            Refresh();
        }

        private void Refresh()
        {
            if (questText == null)
            {
                return;
            }

            questText.text = GetQuestText();
        }

        private string GetQuestText()
        {
            switch (questIndex)
            {
                case 0:
                    return "Mision: tala un arbol con el hacha.";
                case 1:
                    return "Mision: pica una roca con el pico.";
                case 2:
                    return "Mision: cava pasto con la pala.";
                case 3:
                    return "Mision: labra la tierra con la azada.";
                case 4:
                    return "Mision: planta una semilla.";
                case 5:
                    return "Mision: riega el cultivo.";
                case 6:
                    return "Mision: espera y recoge la fruta.";
                case 7:
                    return "Mision: entra a una mazmorra y derrota un enemigo.";
                default:
                    return "Misiones iniciales completadas.";
            }
        }

        private static int GetQuestCount()
        {
            return 9;
        }
    }
}
