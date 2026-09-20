using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class EnemyLootPickup : MonoBehaviour
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField] private Transform visual;
        private bool collected, landed;
        private float spawnedAt;
        private Vector3 restPosition, launchPosition, landingPosition;
        private const float FlightDuration = .48f;
        public const float MagnetRadius = 2.2f;
        private PlayerInventory magnetTarget;
        private PlayerSurvivalStats magnetStats;
        private SpriteRenderer shadow;
        private SpriteRenderer icon;
        private WorldSpriteDepth depth;
        private Collider2D trigger;
        private Rigidbody2D body;
        private LootPickupPool service;
        private SceneComponentPool<EnemyLootPickup> leasePool;
        private Vector3 authoredScale = Vector3.one, authoredVisualPosition;
        private Quaternion authoredVisualRotation;
        private bool hasAuthoredVisual, lookedForBody;
        private float nextMagnetProbe, inverseScaleY = 1f;
        private string encounterId;
        private bool isDungeon;
        private bool attracting;
        public bool IsAttracting => attracting;
        public int Amount => amount;
        public ItemDefinition Item => item;
        public Vector3 LandingPosition => landingPosition;
        public string EncounterId => encounterId;
        public bool IsUncollected => !collected && gameObject.activeSelf;
        public bool IsDungeon => isDungeon;
        public void Discard()
        {
            if (collected) return;
            collected = true;
            Release();
        }

        internal void RememberAuthoredScale() => authoredScale = transform.localScale;

        internal void PrepareLease(LootPickupPool owner, SceneComponentPool<EnemyLootPickup> pool, Transform parent, Vector3 position)
        {
            service = owner; leasePool = pool;
            transform.SetParent(parent, false);
            transform.localScale = authoredScale;
            transform.SetPositionAndRotation(position, Quaternion.identity);
        }

        public void Configure(ItemDefinition definition, int count, Transform sprite)
        {
            item = definition; amount = Mathf.Max(1, count);
            if (sprite != null && sprite != visual) { visual = sprite; hasAuthoredVisual = false; icon = null; depth = null; }
            if (service == null) service = LootPickupPool.For(transform);
            if (visual == null)
            {
                visual = new GameObject("Loot icon").transform;
                visual.SetParent(transform, false);
            }
            if (!hasAuthoredVisual)
            {
                authoredVisualPosition = visual.localPosition;
                authoredVisualRotation = visual.localRotation;
                hasAuthoredVisual = true;
            }
            visual.localPosition = authoredVisualPosition;
            visual.localRotation = authoredVisualRotation;
            if (icon == null) icon = visual.GetComponent<SpriteRenderer>();
            if (icon == null) icon = visual.gameObject.AddComponent<SpriteRenderer>();
            icon.enabled = true;
            icon.sprite = service.Icon(item != null ? item.Kind : ItemKind.Coins);
            icon.color = TintFor(item != null ? item.Kind : ItemKind.Coins);
            if (icon.sprite != null)
            {
                float size = item != null && item.Kind == ItemKind.Experience ? .3f : item != null && item.Kind == ItemKind.Diamond ? .65f : item != null && item.Kind == ItemKind.Bow ? .85f : .46f;
                visual.localScale = Vector3.one * (size / Mathf.Max(.01f, icon.sprite.bounds.size.x) / Mathf.Max(.01f, Mathf.Abs(transform.lossyScale.x)));
            }
            if (depth == null) depth = visual.GetComponent<WorldSpriteDepth>();
            if (depth == null) depth = visual.gameObject.AddComponent<WorldSpriteDepth>();
            depth.Visual = icon;
            EnsureShadow();
            if (trigger == null) trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true; trigger.enabled = true;
            if (trigger is CircleCollider2D circle) circle.radius = .3f / Mathf.Max(.01f, Mathf.Abs(transform.lossyScale.x));
            if (!lookedForBody) { body = GetComponent<Rigidbody2D>(); lookedForBody = true; }
            if (body != null) { body.linearVelocity = Vector2.zero; body.angularVelocity = 0; }
            collected = landed = false; spawnedAt = Time.time;
            attracting = false; magnetTarget = null; magnetStats = null;
            nextMagnetProbe = spawnedAt + FlightDuration + .12f;
            inverseScaleY = 1f / Mathf.Max(.01f, Mathf.Abs(transform.lossyScale.y));
            restPosition = authoredVisualPosition;
            launchPosition = landingPosition = transform.position;
            CacheEncounter();
        }

        private void EnsureShadow()
        {
            if (shadow == null)
            {
                var existing = transform.Find("Loot ground shadow");
                if (existing != null) shadow = existing.GetComponent<SpriteRenderer>();
                if (shadow == null)
                {
                    var ground = new GameObject("Loot ground shadow"); ground.transform.SetParent(transform, false);
                    shadow = ground.AddComponent<SpriteRenderer>();
                }
            }
            shadow.sprite = service.Shadow;
            shadow.enabled = true;
            shadow.color = Color.white;
            shadow.transform.localPosition = Vector3.zero;
            shadow.transform.localRotation = Quaternion.identity;
            shadow.transform.localScale = Vector3.one * .48f / Mathf.Max(.01f, Mathf.Abs(transform.lossyScale.x));
        }

        internal static string IconFor(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Coins: return "Coin";
                case ItemKind.Experience: case ItemKind.Ruby: case ItemKind.Emerald: case ItemKind.Diamond: case ItemKind.EmeraldShard: case ItemKind.EarthEssence: return "Gem";
                case ItemKind.Iron: case ItemKind.GoldOre: return "Iron";
                case ItemKind.CommonSeed: return "CommonSeeds";
                case ItemKind.MineralSeed: return "MineralSeeds";
                case ItemKind.MagicSeed: return "MagicSeeds";
                case ItemKind.Wood: return "Wood";
                case ItemKind.Stone: return "Stone";
                case ItemKind.Fruit: return "Fruit";
                case ItemKind.Food: return "Food";
                case ItemKind.Arrow: return "Arrow";
                case ItemKind.Leather: return "Leather";
                case ItemKind.Bow: return "Bow";
                default: return "Gem";
            }
        }
        private static Color TintFor(ItemKind kind) => kind == ItemKind.Experience ? new Color(.42f, 1f, .95f) :
            kind == ItemKind.Ruby ? new Color(1, .35f, .45f) : kind == ItemKind.Emerald || kind == ItemKind.EmeraldShard ? new Color(.35f, 1, .55f) :
            kind == ItemKind.EarthEssence ? new Color(.7f, .8f, .35f) :
            kind == ItemKind.GoldOre ? new Color(1, .8f, .22f) : Color.white;

        public static EnemyLootPickup Spawn(EnemyLootPickup prefab, Vector3 position, Transform parent, ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return null;
            return LootPickupPool.For(parent).Rent(prefab, position, parent, item, amount);
        }

        public static EnemyLootPickup Scatter(Vector3 origin, Transform parent, ItemKind kind, int amount, EnemyLootPickup prefab = null, float angle = -1)
        {
            var drop = Spawn(prefab, origin, parent, ResourceFlyweights.Item(kind), amount);
            if (drop == null) return null;
            float startAngle = angle < 0 ? Random.Range(0f, Mathf.PI * 2) : angle;
            Physics2D.SyncTransforms();
            for (int attempt = 0; attempt < 24; attempt++)
            {
                float a = startAngle + attempt * 2.399963f;
                float radius = .9f + (attempt % 4) * .25f;
                Vector3 destination = origin + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                // A clear landing point on the other side of a wall is still unreachable.
                // Ignore the chest/body containing the launch point, but not intervening walls.
                if (drop.IsDungeon && !DungeonLayout.Walkable(destination, .2f)) continue;
                if (!drop.service.CanLand(origin, destination)) continue;
                drop.landingPosition = destination; break;
            }
            return drop;
        }

        private void Update()
        {
            float age = Time.time - spawnedAt;
            float flight = Mathf.Clamp01(age / FlightDuration);
            if (!landed) transform.position = Vector3.Lerp(launchPosition, landingPosition, 1 - (1 - flight) * (1 - flight));
            if (!landed && flight >= 1) { landed = true; AudioFeedback.PlayAt(CombatSound.Drop, transform.position, .45f); }
            if (landed && (attracting || Time.time >= nextMagnetProbe))
            {
                nextMagnetProbe = Time.time + .1f;
                if (magnetTarget == null) magnetTarget = service.GetPlayer(out magnetStats);
                if (magnetTarget != null && magnetTarget.gameObject.activeInHierarchy && (magnetStats == null || magnetStats.CurrentHealth > 0))
                {
                    Vector3 targetPosition = magnetTarget.transform.position;
                    float distanceSquared = ((Vector2)(transform.position - targetPosition)).sqrMagnitude;
                    if (attracting && distanceSquared > MagnetRadius * MagnetRadius * 9) attracting = false;
                    bool clear = (attracting || distanceSquared <= MagnetRadius * MagnetRadius) && HasClearPath(magnetTarget);
                    if (clear)
                    {
                        attracting = true;
                        transform.position = Vector3.MoveTowards(transform.position, targetPosition, 12f * Time.deltaTime);
                        landingPosition = transform.position;
                        if (((Vector2)(transform.position - targetPosition)).sqrMagnitude <= .04f && TryCollect(magnetTarget)) return;
                    }
                }
            }
            if (visual != null)
                visual.localPosition = restPosition + Vector3.up * (flight < 1 ? Mathf.Sin(flight * Mathf.PI) * .65f : .12f + Mathf.Sin(age * 4f) * .06f) * inverseScaleY;
            if (shadow != null) shadow.sortingOrder = 996 - Mathf.RoundToInt(transform.position.y * 20f);
        }

        private bool HasClearPath(PlayerInventory player)
        {
            return service != null && service.HasClearPath(transform.position, player.transform.position);
        }

        private void OnTriggerEnter2D(Collider2D other) => TryTouch(other);
        private void OnTriggerStay2D(Collider2D other) => TryTouch(other);
        private void TryTouch(Collider2D other)
        {
            if (collected || Time.time - spawnedAt < FlightDuration + .12f) return;
            var inventory=other.GetComponentInParent<PlayerInventory>();
            // Trigger circles may overlap through a thin wall before either centre
            // crosses it. Physical pickup uses the same visibility rule as the magnet.
            if(inventory!=null&&HasClearPath(inventory))TryCollect(inventory);
        }

        public bool TryCollect(PlayerInventory inventory)
        {
            if (collected || inventory == null || item == null || Time.time - spawnedAt < FlightDuration + .12f) return false;
            var stats = inventory.GetComponent<PlayerSurvivalStats>();
            if (stats != null && stats.CurrentHealth <= 0) return false;
            collected = true;
            // Capture position before the pool reparents the instance to inactive storage.
            Vector3 pickupPosition = transform.position;
            gameObject.SetActive(false);
            item.Grant(inventory, amount);
            AudioFeedback.PlayAt(CombatSound.Pickup, pickupPosition, .65f);
            Release();
            return true;
        }

        private void Release()
        {
            StopAllCoroutines();
            attracting = landed = false; magnetTarget = null; magnetStats = null;
            encounterId = null; isDungeon = false;
            item = null; amount = 0;
            if (visual != null) { visual.localPosition = authoredVisualPosition; visual.localRotation = authoredVisualRotation; }
            if (body != null) { body.linearVelocity = Vector2.zero; body.angularVelocity = 0; }
            if (leasePool != null && leasePool.Return(this)) return;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void CacheEncounter()
        {
            encounterId = GetComponentInParent<EnemyCamp>(true)?.Definition?.Id;
            isDungeon = GetComponentInParent<DungeonEnemyPool>(true) != null || GetComponentInParent<DungeonExpedition>(true) != null;
        }

        private void OnTransformParentChanged()
        {
            if (!collected) CacheEncounter();
        }

        private void OnDestroy() => leasePool?.Forget(this);
    }
}
