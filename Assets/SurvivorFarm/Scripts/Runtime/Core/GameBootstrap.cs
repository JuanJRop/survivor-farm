using UnityEngine;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.World;

namespace SurvivorFarm.Runtime.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string projectName = "Survivor Farm";
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool buildFarmPrototypeOnStart = true;

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
        }

        private void Start()
        {
            Debug.Log($"{projectName} started.");

            if (!buildFarmPrototypeOnStart || FindFirstObjectByType<Player.PlayerInventory>() != null)
            {
                return;
            }

            FarmPrototypeBuilder.Build();
        }
    }
}
