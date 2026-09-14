using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>A bounded, reusable world-space warning. The marked radius is the damage radius.</summary>
    public sealed class CombatTelegraph : MonoBehaviour
    {
        private LineRenderer line;
        private Material material;
        public static CombatTelegraph Create(Transform parent, string label)
        {
            var root = new GameObject(label); root.transform.SetParent(parent, false);
            var marker = root.AddComponent<CombatTelegraph>();
            marker.line = root.AddComponent<LineRenderer>();
            marker.material = new Material(Shader.Find("Sprites/Default"));
            marker.line.sharedMaterial = marker.material;
            marker.line.useWorldSpace = true; marker.line.loop = true;
            marker.line.positionCount = 48; marker.line.sortingOrder = 16000;
            marker.line.startWidth = marker.line.endWidth = .055f;
            marker.Hide(); return marker;
        }
        public void Show(Vector3 center, float radius, Color color)
        {
            line.enabled = true; line.startColor = line.endColor = color;
            for (int i=0; i<48; i++)
            {
                float a=i*Mathf.PI*2/48;
                line.SetPosition(i,center+new Vector3(Mathf.Cos(a),Mathf.Sin(a))*radius);
            }
        }
        public void Hide() { if(line!=null)line.enabled=false; }
        private void OnDisable() => Hide();
        private void OnDestroy() { if(material!=null)Destroy(material); }
    }
}
