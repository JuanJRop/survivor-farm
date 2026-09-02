using UnityEngine;

namespace SurvivorFarm.Runtime.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private string projectName = "Survivor Farm";
        [SerializeField] private int targetFrameRate = 60;

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
        }

        private void Start()
        {
            Debug.Log($"{projectName} started.");
        }
    }
}
