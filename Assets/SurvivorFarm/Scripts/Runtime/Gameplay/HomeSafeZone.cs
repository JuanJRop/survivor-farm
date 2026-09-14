using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class HomeSafeZone : MonoBehaviour
    {
        [SerializeField] private Transform home;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField, Min(1f)] private float initialRadius = 2.8f;
        [SerializeField, Min(1f)] private float maxRadius = 7f;
        [SerializeField, Min(1)] private int resourcesPerUnit = 20;
        [SerializeField] private LineRenderer boundary;
        public Vector2 Center => home != null ? (Vector2)home.position : (Vector2)transform.position;
        public float Radius => Mathf.Min(maxRadius, initialRadius + (inventory != null ? inventory.TotalGathered : 0) / (float)Mathf.Max(1, resourcesPerUnit));

        public void Configure(Transform house, PlayerInventory owner, LineRenderer line)
        {
            home = house;
            inventory = owner;
            boundary = line;
            RefreshBoundary();
        }

        public bool Contains(Vector2 point, float margin = 0f) => (point - Center).sqrMagnitude <= Mathf.Pow(Radius + margin, 2f);

        private Vector2 lastCenter;
        private float lastRadius = -1f;
        private void LateUpdate()
        {
            if (lastCenter != Center || !Mathf.Approximately(lastRadius, Radius)) RefreshBoundary();
        }

        private void RefreshBoundary()
        {
            if (boundary == null) return;
            boundary.enabled = false;
            lastCenter = Center;
            lastRadius = Radius;
            boundary.useWorldSpace = true;
            boundary.loop = true;
            boundary.positionCount = 64;
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2f / 64f;
                boundary.SetPosition(i, new Vector3(Center.x + Mathf.Cos(angle) * Radius, Center.y + Mathf.Sin(angle) * Radius, 0f));
            }
        }
    }
}
