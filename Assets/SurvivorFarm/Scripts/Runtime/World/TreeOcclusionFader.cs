using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    public sealed class TreeOcclusionFader : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] renderers = new SpriteRenderer[0];
        [SerializeField, Range(0.25f, 0.9f)] private float fadedAlpha = 0.42f;
        [SerializeField] private float fadeSpeed = 7.5f;
        [SerializeField] private float horizontalPadding = 0.15f;
        [SerializeField] private float upperPadding = 0.25f;
        [SerializeField] private float behindStartYOffset = -0.12f;

        private Transform player;
        private Color[] baseColors = new Color[0];
        private float nextPlayerSearch;

        public static TreeOcclusionFader Ensure(GameObject tree)
        {
            if (tree == null) return null;
            TreeOcclusionFader fader = tree.GetComponent<TreeOcclusionFader>();
            if (fader == null) fader = tree.AddComponent<TreeOcclusionFader>();
            fader.CacheRenderers();
            return fader;
        }

        private void Awake()
        {
            CacheRenderers();
        }

        private void OnEnable()
        {
            CacheRenderers();
        }

        private void LateUpdate()
        {
            if (renderers == null || renderers.Length == 0) return;
            if (player == null && Time.unscaledTime >= nextPlayerSearch) FindPlayer();

            bool fade = player != null && PlayerIsBehindTree(player.position);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer sprite = renderers[i];
                if (sprite == null) continue;

                Color color = sprite.color;
                float baseAlpha = i < baseColors.Length ? baseColors[i].a : 1f;
                float targetAlpha = fade ? baseAlpha * fadedAlpha : baseAlpha;
                color.a = Mathf.MoveTowards(color.a, targetAlpha, fadeSpeed * Time.deltaTime);
                sprite.color = color;
            }
        }

        private void CacheRenderers()
        {
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                baseColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
            }
        }

        private void FindPlayer()
        {
            nextPlayerSearch = Time.unscaledTime + 0.5f;
            PlayerMovementController movement = FindFirstObjectByType<PlayerMovementController>();
            if (movement != null)
            {
                player = movement.transform;
                return;
            }

            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            player = taggedPlayer != null ? taggedPlayer.transform : null;
        }

        private bool PlayerIsBehindTree(Vector3 playerPosition)
        {
            if (!TryGetVisualBounds(out Bounds bounds)) return false;

            bool insideTreeWidth = playerPosition.x >= bounds.min.x - horizontalPadding &&
                playerPosition.x <= bounds.max.x + horizontalPadding;
            bool behindBase = playerPosition.y >= transform.position.y + behindStartYOffset;
            bool belowTop = playerPosition.y <= bounds.max.y + upperPadding;
            return insideTreeWidth && behindBase && belowTop;
        }

        private bool TryGetVisualBounds(out Bounds bounds)
        {
            bounds = new Bounds(transform.position, Vector3.zero);
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer sprite = renderers[i];
                if (sprite == null || !sprite.enabled) continue;
                if (!hasBounds)
                {
                    bounds = sprite.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(sprite.bounds);
                }
            }

            return hasBounds;
        }
    }
}
