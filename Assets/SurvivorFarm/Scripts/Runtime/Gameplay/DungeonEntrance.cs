using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class DungeonEntrance : MonoBehaviour, IWorldInteractable
    {
        [SerializeField] private GameObject outdoorRoot;
        [SerializeField] private GameObject dungeonRoot;
        [SerializeField] private Transform player;
        [SerializeField] private Transform outsideSpawn;
        [SerializeField] private Transform insideSpawn;
        [SerializeField] private LandUnlockZone requiredZone;
        [SerializeField] private DungeonEnemyPool enemyPool;
        [SerializeField] private float outsideCameraSize = 6f;
        [SerializeField] private float insideCameraSize = 4.6f;

        private bool insideDungeon;

        public Transform Transform => transform;
        public bool IsAvailable => requiredZone == null || requiredZone.IsUnlocked;
        public bool IsInsideDungeon => insideDungeon;

        public void Configure(
            GameObject outsideWorld,
            GameObject dungeonWorld,
            Transform playerTransform,
            Transform outsidePoint,
            Transform insidePoint,
            LandUnlockZone unlockZone,
            DungeonEnemyPool pool)
        {
            outdoorRoot = outsideWorld;
            dungeonRoot = dungeonWorld;
            player = playerTransform;
            outsideSpawn = outsidePoint;
            insideSpawn = insidePoint;
            requiredZone = unlockZone;
            enemyPool = pool;
            SetInside(false, false);
        }

        public string GetInteractionLabel(FarmTool selectedTool)
        {
            if (!IsAvailable)
            {
                return "Desbloquea esta zona para entrar a la mazmorra";
            }

            return insideDungeon ? "Interactuar: salir de mazmorra" : "Interactuar: entrar a mazmorra";
        }

        public void SetHighlighted(bool highlighted)
        {
            transform.localScale = highlighted ? Vector3.one * 1.1f : Vector3.one;
        }

        public void Interact(FarmTool selectedTool, PlayerInventory inventory)
        {
            if (!IsAvailable)
            {
                FarmNotificationCenter.Show("Primero desbloquea esta zona.");
                return;
            }

            EnterDungeon();
        }

        public void EnterDungeon()
        {
            SetInside(true, true);
        }

        public void ExitDungeon()
        {
            SetInside(false, true);
        }

        public void RestoreInsideState(bool restoreInsideDungeon)
        {
            SetInside(restoreInsideDungeon, false);
        }

        private void SetInside(bool value, bool notify)
        {
            insideDungeon = value;

            if (outdoorRoot != null)
            {
                outdoorRoot.SetActive(!insideDungeon);
            }

            if (dungeonRoot != null)
            {
                dungeonRoot.SetActive(insideDungeon);
            }

            Transform spawn = insideDungeon ? insideSpawn : outsideSpawn;
            if (player != null && spawn != null)
            {
                player.position = spawn.position;
                PlayerMovementController movement = player.GetComponent<PlayerMovementController>();
                movement?.StopMovement();
            }

            if (enemyPool != null)
            {
                if (insideDungeon)
                {
                    enemyPool.SpawnEncounter();
                }
                else
                {
                    enemyPool.DespawnAll();
                }
            }

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.orthographicSize = insideDungeon ? insideCameraSize : outsideCameraSize;
                if (player != null)
                {
                    camera.transform.position = player.position + new Vector3(0f, 0f, -10f);
                }
            }

            if (notify)
            {
                FarmNotificationCenter.Show(insideDungeon ? "Entraste a la mazmorra." : "Saliste de la mazmorra.");
            }
        }
    }
}
