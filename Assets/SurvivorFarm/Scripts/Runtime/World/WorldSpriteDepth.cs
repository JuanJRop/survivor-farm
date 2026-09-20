using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    /// <summary>Sort the visual at its ground contact point, keeping roofs behind actors below them.</summary>
    [ExecuteAlways]
    public sealed class WorldSpriteDepth : MonoBehaviour
    {
        public SpriteRenderer Visual;
        public float GroundOffset;
        private void LateUpdate()
        {
            if (Visual == null) return;
            int order = 1000 - Mathf.RoundToInt((transform.position.y + GroundOffset) * 20f);
            if (Visual.sortingOrder != order) Visual.sortingOrder = order;
        }
    }
}
