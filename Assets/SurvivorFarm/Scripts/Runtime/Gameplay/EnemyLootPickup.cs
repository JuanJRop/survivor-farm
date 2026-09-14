using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class EnemyLootPickup : MonoBehaviour
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField] private Transform visual;
        private bool collected;
        private float spawnedAt;
        private Vector3 restPosition;
        private bool landed;
        public int Amount => amount;
        public ItemDefinition Item => item;
        public bool IsUncollected => !collected && gameObject.activeSelf;
        public bool IsDungeon => GetComponentInParent<DungeonEnemyPool>(true) != null;
        public void Discard() { collected = true; gameObject.SetActive(false); Destroy(gameObject); }

        public void Configure(ItemDefinition definition, int count, Transform sprite)
        {
            item = definition;
            amount = Mathf.Max(1, count);
            visual = sprite;
            var renderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            if(renderer != null && renderer.bounds.size.x > .001f) visual.localScale *= .65f / renderer.bounds.size.x;
            collected = false;
            landed = false;
            spawnedAt = Time.time;
            restPosition = visual != null ? visual.localPosition : Vector3.zero;
        }

        private void OnEnable()
        {
            spawnedAt = Time.time;
            if (visual != null) restPosition = visual.localPosition;
        }

        public static EnemyLootPickup Spawn(EnemyLootPickup prefab, Vector3 position, Transform parent, ItemDefinition item, int amount)
        {
            if (prefab == null || amount <= 0) return null;
            var drop = Instantiate(prefab, position, Quaternion.identity, parent);
            drop.Configure(item, amount, drop.visual);
            drop.gameObject.SetActive(true);
            return drop;
        }

        private void Update()
        {
            if (visual == null) return;
            float age = Time.time - spawnedAt;
            if (!landed && age >= .4f)
            { landed = true; AudioFeedback.PlayAt(CombatSound.Drop, transform.position, .45f); }
            float bounce = age < 0.4f ? Mathf.Sin(age / 0.4f * Mathf.PI) * 0.28f : Mathf.Sin(age * 4f) * 0.04f;
            visual.localPosition = restPosition + Vector3.up * bounce;
        }

        private void OnTriggerEnter2D(Collider2D other) => TryCollect(other.GetComponentInParent<PlayerInventory>());
        private void OnTriggerStay2D(Collider2D other) => TryCollect(other.GetComponentInParent<PlayerInventory>());

        public bool TryCollect(PlayerInventory inventory)
        {
            if (collected || inventory == null || item == null || Time.time - spawnedAt < 0.25f) return false;
            var stats = inventory.GetComponent<PlayerSurvivalStats>();
            if (stats != null && stats.CurrentHealth <= 0) return false;
            collected = true;
            gameObject.SetActive(false);
            item.Grant(inventory, amount);
            inventory.GetComponent<GameFeelFeedback>()?.Pulse($"+{amount} {item.DisplayName}", transform.position);
            FarmNotificationCenter.Show($"+{amount} {item.DisplayName}");
            Destroy(gameObject);
            return true;
        }
    }
}
