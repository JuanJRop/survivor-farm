using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    public sealed class CameraFollowTarget : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float followSpeed = 12f;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
        private bool arenaCamera;
        private float previousSize;
        private Transform combatFocus;
        private float explorationSize;

        public void SetCombatFocus(Transform enemy)
        {
            var camera = GetComponent<Camera>();
            if (enemy != null && combatFocus == null && camera != null) explorationSize = camera.orthographicSize;
            if (enemy == null && combatFocus != null && camera != null) camera.orthographicSize = explorationSize;
            combatFocus = enemy;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                transform.position = target.position + offset;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var camera = GetComponent<Camera>();
            if (combatFocus != null && !combatFocus.gameObject.activeInHierarchy) SetCombatFocus(null);
            if (combatFocus != null && camera != null)
            {
                Vector3 midpoint = (target.position + combatFocus.position) * .5f;
                camera.orthographicSize = Mathf.Clamp(Mathf.Max(Mathf.Abs(target.position.y - combatFocus.position.y) * .5f + 2.5f,
                    Mathf.Abs(target.position.x - combatFocus.position.x) * .5f / Mathf.Max(.5f, camera.aspect) + 2.5f), 6.4f, 9f);
                transform.position = Vector3.Lerp(transform.position, midpoint + offset, followSpeed * Time.deltaTime);
                return;
            }
            var arena = SurvivorFarm.Runtime.Gameplay.ValleyWorld.Center(5);
            bool insideArena = Vector2.Distance(target.position, arena) < 18f;
            if (insideArena && camera != null)
            {
                if (!arenaCamera) previousSize = camera.orthographicSize;
                arenaCamera = true;
                camera.orthographicSize = 9f;
                transform.position = arena + offset;
                return;
            }
            if (arenaCamera && camera != null) { camera.orthographicSize = previousSize; arenaCamera = false; }
            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        }
    }
}
