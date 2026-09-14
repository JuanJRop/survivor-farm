using System.Collections.Generic;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class EnemyProjectilePool : MonoBehaviour
    {
        private readonly List<EnemyArrowProjectile> arrows = new List<EnemyArrowProjectile>();
        private Sprite arrowSprite;
        private int cursor;
        public int PoolCount => arrows.Count;
        public int ActiveCount
        {
            get { int count = 0; foreach (var arrow in arrows) if (arrow.gameObject.activeSelf) count++; return count; }
        }

        public static EnemyProjectilePool Ensure(GameObject owner, int capacity)
        {
            var pool = owner.GetComponent<EnemyProjectilePool>() ?? owner.AddComponent<EnemyProjectilePool>();
            pool.Initialize(capacity);
            return pool;
        }

        public void Initialize(int capacity)
        {
            if (arrowSprite == null)
            {
                var art = Resources.Load<PlayerAnimationLibrary>("EnemyProjectileArt");
                if (art != null) arrowSprite = art.Frame(art.Find("Arrow"), 2, 0);
            }
            for (int i = arrows.Count; i < capacity; i++)
            {
                var go = new GameObject("Enemy Arrow " + (i + 1));
                go.transform.SetParent(transform, false);
                go.transform.localScale = Vector3.one * 1.15f;
                go.SetActive(false);
                var visual = go.AddComponent<SpriteRenderer>();
                visual.sprite = arrowSprite;
                visual.sortingOrder = 2200;
                arrows.Add(go.AddComponent<EnemyArrowProjectile>());
            }
        }

        public bool Fire(EnemyAIBase source, Transform target, Vector2 direction, int damage, HomeSafeZone safeZone)
        {
            if (!isActiveAndEnabled || source == null || !source.CanLaunchProjectile || target == null || arrowSprite == null) return false;
            for (int offset = 0; offset < arrows.Count; offset++)
            {
                int index = (cursor + offset) % arrows.Count;
                if (arrows[index].gameObject.activeSelf) continue;
                arrows[index].Launch(source, target, direction, damage, safeZone);
                cursor = (index + 1) % arrows.Count;
                return true;
            }
            // The prewarmed pool is bounded; a saturated volley skips its shot.
            return false;
        }

        public void Cancel(EnemyAIBase source)
        {
            foreach (var arrow in arrows) if (arrow.Source == source) arrow.ReturnToPool();
        }

        public void ReturnAll()
        {
            foreach (var arrow in arrows) if (arrow != null) arrow.ReturnToPool();
            cursor = 0;
        }

        private void OnDisable() => ReturnAll();
    }
}
