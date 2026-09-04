using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.Player
{
    public sealed class PlayerCombatController : MonoBehaviour
    {
        [SerializeField] private float swordRange = 1.25f;
        [SerializeField] private float bowRange = 6f;
        [SerializeField] private float attackCooldown = 0.45f;
        [SerializeField] private int swordDamage = 1;
        [SerializeField] private int arrowDamage = 1;

        private PlayerToolbelt toolbelt;
        private PlayerMovementController movement;
        private PlayerCharacterAnimator characterAnimator;
        private Text attackButtonText;
        private Image cooldownFill;
        private float nextAttackTime;

        private void Awake()
        {
            toolbelt = GetComponent<PlayerToolbelt>();
            movement = GetComponent<PlayerMovementController>();
            characterAnimator = GetComponent<PlayerCharacterAnimator>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Attack();
            }

            RefreshAttackUi();
        }

        public void ConfigureAttackUi(Text buttonText, Image fill)
        {
            attackButtonText = buttonText;
            cooldownFill = fill;
            RefreshAttackUi();
        }

        public void Attack()
        {
            if (Time.time < nextAttackTime)
            {
                FarmNotificationCenter.Show($"Ataque recargando {nextAttackTime - Time.time:0.0}s.");
                return;
            }

            FarmTool selectedTool = toolbelt != null ? toolbelt.SelectedTool : FarmTool.Sword;
            if (selectedTool != FarmTool.Sword && selectedTool != FarmTool.Bow)
            {
                FarmNotificationCenter.Show("Selecciona espada o arco para atacar.");
                return;
            }

            movement?.StopMovement();
            bool attacked;

            if (selectedTool == FarmTool.Sword)
            {
                attacked = SwingSword();
            }
            else
            {
                attacked = ShootArrow();
            }

            if (attacked)
            {
                characterAnimator?.PlayToolAction(selectedTool);
                nextAttackTime = Time.time + attackCooldown;
            }
        }

        private bool SwingSword()
        {
            BasicEnemyAI enemy = FindNearestEnemy(swordRange);
            if (enemy == null)
            {
                FarmNotificationCenter.Show("No hay enemigos cerca.");
                return false;
            }

            enemy.TakeDamage(swordDamage);
            FarmNotificationCenter.Show("Golpeaste con la espada.");
            return true;
        }

        private bool ShootArrow()
        {
            BasicEnemyAI enemy = FindNearestEnemy(bowRange);
            if (enemy == null)
            {
                FarmNotificationCenter.Show("No hay enemigos en rango.");
                return false;
            }

            GameObject arrowObject = new GameObject("Arrow Projectile");
            arrowObject.transform.position = transform.position;
            arrowObject.transform.localScale = new Vector3(0.38f, 0.12f, 1f);

            SpriteRenderer renderer = arrowObject.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateArrowSprite();
            renderer.color = new Color(0.92f, 0.84f, 0.58f);
            renderer.sortingOrder = 8;

            ArrowProjectile projectile = arrowObject.AddComponent<ArrowProjectile>();
            projectile.Configure(enemy, arrowDamage);
            FarmNotificationCenter.Show("Disparaste una flecha.");
            return true;
        }

        private void RefreshAttackUi()
        {
            float remaining = Mathf.Max(0f, nextAttackTime - Time.time);
            bool ready = remaining <= 0f;

            if (attackButtonText != null)
            {
                attackButtonText.text = ready ? "Atacar" : $"{remaining:0.0}s";
            }

            if (cooldownFill != null)
            {
                cooldownFill.enabled = !ready;
                cooldownFill.fillAmount = attackCooldown <= 0f ? 0f : Mathf.Clamp01(remaining / attackCooldown);
            }
        }

        private BasicEnemyAI FindNearestEnemy(float range)
        {
            BasicEnemyAI[] enemies = FindObjectsByType<BasicEnemyAI>(FindObjectsSortMode.None);
            BasicEnemyAI nearest = null;
            float nearestDistance = range * range;

            foreach (BasicEnemyAI enemy in enemies)
            {
                if (!enemy.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float distance = (enemy.transform.position - transform.position).sqrMagnitude;
                if (distance <= nearestDistance)
                {
                    nearest = enemy;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private static Sprite CreateArrowSprite()
        {
            Texture2D texture = new Texture2D(24, 8, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 24; x++)
                {
                    bool shaft = y >= 3 && y <= 4 && x <= 18;
                    bool head = x >= 18 && Mathf.Abs(y - 3.5f) <= 23 - x;
                    texture.SetPixel(x, y, shaft || head ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 24f, 8f), new Vector2(0.5f, 0.5f), 24f);
        }
    }
}
