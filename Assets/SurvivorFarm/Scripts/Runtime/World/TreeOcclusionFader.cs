using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.Core;
using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    public sealed class TreeOcclusionFader : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] renderers = new SpriteRenderer[0];
        [SerializeField, Range(0.25f, 0.9f)] private float fadedAlpha = 0.3f;
        [SerializeField] private float fadeSpeed = 7.5f;
        [SerializeField] private float horizontalPadding = 0.15f;
        [SerializeField] private float upperPadding = 0.25f;
        [SerializeField] private float behindStartYOffset = -0.12f;

        private Transform player;
        private SpriteRenderer playerSprite;
        private static Transform sharedPlayer;
        private static float nextSharedSearch;
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
                if (!Mathf.Approximately(color.a, targetAlpha))
                {
                    color.a = Mathf.MoveTowards(color.a, targetAlpha, fadeSpeed * Time.deltaTime);
                    sprite.color = color;
                }
            }
        }

        private void CacheRenderers()
        {
            // Reconfiguration/regrowth must not record the temporarily faded alpha as the new normal.
            var previousRenderers = renderers;
            var previousColors = baseColors;
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                baseColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
                for (int j = 0; j < previousRenderers.Length && j < previousColors.Length; j++)
                    if (previousRenderers[j] == renderers[i]) { baseColors[i] = previousColors[j]; break; }
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < renderers.Length && i < baseColors.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color color = renderers[i].color; color.a = baseColors[i].a; renderers[i].color = color;
            }
        }

        private void FindPlayer()
        {
            nextPlayerSearch = Time.unscaledTime + 0.5f;
            if (PortfolioSession.Instance != null && PortfolioSession.Instance.Player != null)
                sharedPlayer = PortfolioSession.Instance.Player.transform;
            if (sharedPlayer == null && Time.unscaledTime >= nextSharedSearch)
            {
                nextSharedSearch = Time.unscaledTime + .5f;
                PlayerMovementController movement = FindFirstObjectByType<PlayerMovementController>();
                if (movement != null) sharedPlayer = movement.transform;
                else
                {
                    GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
                    sharedPlayer = taggedPlayer != null ? taggedPlayer.transform : null;
                }
            }
            player = sharedPlayer;
            playerSprite = player != null ? player.GetComponentInChildren<SpriteRenderer>() : null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPlayer() { sharedPlayer = null; nextSharedSearch = 0; }

        private bool PlayerIsBehindTree(Vector3 playerPosition)
        {
            if (!TryGetVisualBounds(out Bounds bounds)) return false;

            bool insideTreeWidth = playerPosition.x >= bounds.min.x - horizontalPadding &&
                playerPosition.x <= bounds.max.x + horizontalPadding;
            if (!insideTreeWidth) return false;
            bool behindBase = playerPosition.y >= transform.position.y + behindStartYOffset;
            if (playerSprite != null && bounds.Intersects(playerSprite.bounds))
                foreach (var visual in renderers)
                    if (visual != null && visual.sortingOrder > playerSprite.sortingOrder) { behindBase = true; break; }
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
                if (sprite == null || !sprite.enabled || !sprite.gameObject.activeInHierarchy || sprite.sprite == null) continue;
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
