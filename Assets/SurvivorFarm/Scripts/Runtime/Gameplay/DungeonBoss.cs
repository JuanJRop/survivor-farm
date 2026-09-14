using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class DungeonBoss : EnemyAIBase
    {
        private DungeonExpedition expedition;
        private LineRenderer warning;
        private Material warningMaterial;
        private Vector3 impact, chargeStart;
        private float until;
        private int phase;
        public bool IsTelegraphing => phase == 1;
        public bool IsExposed => phase == 3;
        public Vector3 ImpactPosition => impact;
        public string PhaseLabel => phase == 1 ? "IMPACTO INMINENTE" : phase == 2 ? "EMBESTIDA" : phase == 3 ? "RECUPERANDO" : CurrentHealth <= MaximumHealth / 2 ? "ENFURECIDO" : "EL CUSTODIO";

        public void ConfigureBoss(DungeonExpedition owner, Transform player, SpriteRenderer visual)
        {
            expedition = owner; Configure(player, null);
            ConfigureVisuals(visual, null, Color.white, "El Custodio");
            ConfigureStats("El Custodio", 56, 2, 1.4f, 1.5f, 2, 0);
            ConfigureAnimation(Resources.Load<PlayerAnimationLibrary>("SpearGoblinAnimations"));
            var marker = new GameObject("Custodian impact telegraph"); marker.transform.SetParent(owner.transform, false);
            warning = marker.AddComponent<LineRenderer>();
            warningMaterial = new Material(Shader.Find("Sprites/Default")); warning.sharedMaterial = warningMaterial;
            warning.useWorldSpace = true; warning.loop = true; warning.positionCount = 48;
            warning.startWidth = warning.endWidth = .09f; warning.sortingOrder = -50; warning.enabled = false;
        }
        public override void ActivateFromPool(Vector3 position)
        {
            base.ActivateFromPool(position); phase = 0; until = Time.time + 1.2f; warning.enabled = false;
        }
        public override void ReturnToPool() { if (warning != null) warning.enabled = false; base.ReturnToPool(); }
        protected override bool CanMoveTo(Vector2 p) => DungeonLayout.RoomAt(p) == 5 && DungeonLayout.Walkable(p, .65f);
        protected override bool CanApplyKnockback(Vector2 p) => CanMoveTo(p);
        protected override void TickEnemy()
        {
            if (!expedition.BossFightActive || DungeonLayout.RoomAt(Target.position) != 5) { warning.enabled = false; return; }
            if (phase == 0)
            {
                SpriteAnimation.Face(Target.position - transform.position);
                if (Time.time < until) return;
                Vector3 toPlayer = Target.position - transform.position;
                impact = transform.position + Vector3.ClampMagnitude(toPlayer, 4.5f);
                impact.x = Mathf.Clamp(impact.x, DungeonLayout.Origin.x - 8, DungeonLayout.Origin.x + 8);
                impact.y = Mathf.Clamp(impact.y, DungeonLayout.Origin.y + 47, DungeonLayout.Origin.y + 58);
                foreach (var hit in Physics2D.LinecastAll(transform.position, impact))
                    if (!hit.collider.isTrigger && !hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(Target))
                    { impact = (Vector3)hit.point - toPlayer.normalized * .9f; break; }
                phase = 1; until = Time.time + (CurrentHealth <= MaximumHealth / 2 ? .85f : 1.15f);
                SpriteAnimation.PlayAttack(toPlayer, 1.5f); DrawWarning();
            }
            else if (phase == 1 && Time.time >= until) { phase = 2; until = Time.time + .35f; chargeStart = transform.position; }
            else if (phase == 2)
            {
                transform.position = Vector3.Lerp(chargeStart, impact, Mathf.Clamp01(1 - (until - Time.time) / .35f));
                if (Time.time >= until)
                {
                    if (Vector2.Distance(Target.position, impact) < 1.65f && ClearImpact()) Target.GetComponent<PlayerSurvivalStats>()?.TakeDamage(2);
                    phase = 3; until = Time.time + (CurrentHealth <= MaximumHealth / 2 ? 1.25f : 1.8f); warning.enabled = false;
                }
            }
            else if (phase == 3 && Time.time >= until) { phase = 0; until = Time.time + .4f; }
        }
        private bool ClearImpact()
        {
            foreach (var hit in Physics2D.LinecastAll(impact, Target.position))
                if (!hit.collider.isTrigger && !hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(Target)) return false;
            return true;
        }
        private void DrawWarning()
        {
            warning.enabled = true; warning.startColor = warning.endColor = new Color(1, .35f, .2f, .9f);
            for (int i = 0; i < 48; i++) { float angle = i * Mathf.PI * 2 / 48; warning.SetPosition(i, impact + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.65f); }
        }
        protected override void OnDefeated(PlayerInventory inventory)
        {
            warning.enabled = false; expedition.CompleteBoss(); FarmGameEvents.RaiseEnemyDefeated();
        }
        private void OnDestroy() { if (warning != null) Destroy(warning.gameObject); if (warningMaterial != null) Destroy(warningMaterial); }
    }
}
