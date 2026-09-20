using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Supplied pixel-art sword sweeps and charge animation, synchronized to accepted attacks.</summary>
    public sealed class CombatFeelRangeCue : MonoBehaviour
    {
        private CombatSpriteEffect sweep;
        private SpriteRenderer charge;
        private CombatSpriteEffect areaBurst;
        private float chargeStartedAt;
        public float Radius { get; private set; }
        public bool IsShowing => sweep != null && sweep.IsPlaying;
        public bool IsCharging => charge != null && charge.enabled;
        public SpriteRenderer SweepVisual => sweep != null ? sweep.Visual : null;
        public SpriteRenderer ChargeVisual => charge;

        private void Ensure()
        {
            if (sweep != null) return;
            var trail = new GameObject("Estela de espada · Combat FX");
            trail.transform.SetParent(transform, false);
            sweep = trail.AddComponent<CombatSpriteEffect>();
            var energy = new GameObject("Carga de espada · Combat FX");
            energy.transform.SetParent(transform, false);
            charge = energy.AddComponent<SpriteRenderer>();
            charge.sortingOrder = 11997; charge.enabled = false;
        }

        public void Show(Vector3 center, float radius, bool heavy = false, int step = 1, bool charged = false, Vector2 heading = default, int swordTier = 2)
        {
            Ensure(); HideCharge(); Radius = radius;
            sweep.transform.position = center;
            sweep.transform.rotation = Quaternion.Euler(0, 0,
                heading.sqrMagnitude > .01f ? Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg : 0);
            int row = charged && swordTier >= 2 ? CombatFxLibrary.ChargedSweep : heavy ? CombatFxLibrary.HeavySweep : CombatFxLibrary.NormalSweep;
            Color tint = charged ? Color.white : swordTier >= 3 ? new Color(.55f, .87f, 1) : Color.white;
            sweep.Play(row, charged ? .36f : heavy ? .28f : .21f, radius * 2f, 11998, tint, step == 2);
            if (charged && swordTier >= 3)
            {
                if (areaBurst == null)
                {
                    var go = new GameObject("Onda de maestría III"); go.transform.SetParent(transform, false);
                    areaBurst = go.AddComponent<CombatSpriteEffect>();
                }
                areaBurst.transform.position = center;
                // An opaque golden ring communicates the area without stacking a
                // second spiked explosion over the player and the actual hit spark.
                areaBurst.Play(CombatFxLibrary.ChargeSpark, .32f, radius * 2, 11996, Color.white);
            }
        }

        public void ShowCharge(Vector3 center, float progress, Vector2 heading, int swordTier = 2)
        {
            if (swordTier < 2) { HideCharge(); return; }
            Ensure();
            if (!charge.enabled) chargeStartedAt = Time.unscaledTime;
            var aim = heading.sqrMagnitude > .01f ? heading.normalized : Vector2.down;
            Vector3 position = center + (Vector3)aim * .45f + Vector3.up * .12f;
            charge.transform.position = new Vector3(Mathf.Round(position.x * 16) / 16f, Mathf.Round(position.y * 16) / 16f, position.z);
            // Fixed scales and nearest-neighbour sampling preserve the original pixel edges while charging.
            charge.transform.localScale = Vector3.one * (swordTier >= 3 ? .5f : .375f);
            // Keep the authored golden sparkle alive while the player holds the charge.
            float phase = .12f + Mathf.Repeat((Time.unscaledTime - chargeStartedAt) / .75f, .67f);
            charge.sprite = CombatFxLibrary.At(CombatFxLibrary.ChargeSpark, phase);
            if (charge.sprite != null) charge.sprite.texture.filterMode = FilterMode.Point;
            charge.color = swordTier >= 3 ? new Color(.6f, .9f, 1, 1) : Color.white;
            charge.enabled = GameFeelFeedback.Enabled && charge.sprite != null;
        }

        public void HideCharge() { if (charge != null) charge.enabled = false; }
        private void OnDisable() { HideCharge(); if (sweep != null) sweep.Stop(); if (areaBurst != null) areaBurst.Stop(); }
    }
}
