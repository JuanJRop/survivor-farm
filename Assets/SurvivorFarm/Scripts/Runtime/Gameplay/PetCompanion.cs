using System.Collections.Generic;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class PetCompanion : MonoBehaviour
    {
        [SerializeField] private PetDefinition definition;
        [SerializeField] private SpriteRenderer body;
        private PlayerPetController owner;
        private PlayerInventory inventory;
        private PlayerSurvivalStats survival;
        private readonly Queue<Vector3> trail = new Queue<Vector3>();
        private readonly Collider2D[] candidates = new Collider2D[32];
        private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
        private Vector3 lastOwnerPosition;
        private float nextAttack;
        private float attackUntil;
        private int direction;
        public PetDefinition Definition => definition;

        public void ConfigureDefinition(PetDefinition data, SpriteRenderer renderer)
        {
            definition = data;
            body = renderer;
            if (body != null && definition != null) body.sprite = definition.GetFrame(0, false, false, 0f);
        }

        public void ConfigureOwner(PlayerPetController player)
        {
            owner = player;
            inventory = player.GetComponent<PlayerInventory>();
            survival = player.GetComponent<PlayerSurvivalStats>();
        }

        public void Recall()
        {
            if (owner == null) return;
            transform.position = owner.transform.position;
            lastOwnerPosition = owner.transform.position;
            trail.Clear();
            attackUntil = 0f;
        }

        private void Update()
        {
            if (owner == null || !owner.Equipped || definition == null) return;
            if (survival != null && survival.CurrentHealth <= 0) return;
            Vector3 playerPosition = owner.transform.position;
            if (Vector3.Distance(playerPosition, lastOwnerPosition) > 6f || Vector3.Distance(transform.position, playerPosition) > 10f) Recall();
            if (Vector3.Distance(lastOwnerPosition, playerPosition) > 0.18f)
            {
                trail.Enqueue(playerPosition);
                lastOwnerPosition = playerPosition;
                if (trail.Count > 80) trail.Dequeue();
            }
            Vector3 before = transform.position;
            bool running = Vector3.Distance(before, playerPosition) > 2f;
            EnemyAIBase enemy = FindEnemy();
            if (enemy != null && Vector2.Distance(before, enemy.transform.position) <= 0.85f)
            {
                Face(enemy.transform.position - before);
                if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time + definition.AttackCooldown;
                    attackUntil = Time.time + 0.25f;
                    enemy.TakeDamage(definition.Damage, inventory);
                }
            }
            else if (enemy != null)
            {
                Move(enemy.transform.position, definition.Speed);
            }
            else if (Vector3.Distance(before, playerPosition) > 0.8f)
            {
                while (trail.Count > 0 && Vector3.Distance(before, trail.Peek()) < 0.2f) trail.Dequeue();
                Move(trail.Count > 0 ? trail.Peek() : playerPosition, definition.Speed * (running ? 1.4f : 1f));
            }
            Vector3 delta = transform.position - before;
            if (delta.sqrMagnitude > 0.000001f) Face(delta);
            if (body != null)
            {
                bool attacking = Time.time < attackUntil;
                body.sprite = definition.GetFrame(direction, attacking || delta.sqrMagnitude > 0.000001f, attacking || running, Time.time);
                body.transform.localPosition = attacking ? Vector3.up * (Mathf.Sin((attackUntil - Time.time) / 0.25f * Mathf.PI) * 0.12f) : Vector3.zero;
            }
        }

        private void Face(Vector3 delta)
        {
            direction = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) ? 0 : delta.y < 0f ? 1 : 2;
            if (body != null) body.flipX = direction == 0 && delta.x < 0f;
        }

        private void Move(Vector3 destination, float speed)
        {
            Vector3 next = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
            if (ClearPath(next, null)) transform.position = next;
        }

        private EnemyAIBase FindEnemy()
        {
            int count = Physics2D.OverlapCircle(transform.position, 3f, new ContactFilter2D { useTriggers = false }, candidates);
            EnemyAIBase best = null;
            float distance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var enemy = candidates[i].GetComponentInParent<EnemyAIBase>();
                if (enemy == null || !enemy.IsAlive || Vector2.Distance(enemy.transform.position, owner.transform.position) > 4f) continue;
                if (enemy is OutdoorEnemyAI outdoor && !outdoor.CanBeAttackedByPet(owner.transform.position)) continue;
                if (enemy is CampEnemyAI guard && !guard.CanBeAttackedByPet) continue;
                float candidateDistance = (enemy.transform.position - transform.position).sqrMagnitude;
                if (candidateDistance < distance && ClearPath(enemy.transform.position, enemy.transform))
                {
                    best = enemy;
                    distance = candidateDistance;
                }
            }
            return best;
        }

        private bool ClearPath(Vector2 end, Transform target)
        {
            int count = Physics2D.Linecast(transform.position, end, new ContactFilter2D { useTriggers = false }, hits);
            if (count == hits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Transform hit = hits[i].transform;
                if (hit.IsChildOf(owner.transform) || hit.IsChildOf(transform) || (target != null && hit.IsChildOf(target))) continue;
                return false;
            }
            return true;
        }
    }
}
