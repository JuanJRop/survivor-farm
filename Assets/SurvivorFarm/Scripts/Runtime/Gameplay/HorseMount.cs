using System.Collections.Generic;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>An authored, reusable horse. Mounting uses the player's existing solid movement body.</summary>
    public sealed class HorseMount : WorldInteractable
    {
        public static readonly HashSet<HorseMount> All = new HashSet<HorseMount>();
        public string Id { get; private set; }
        public Vector3 Home { get; private set; }
        public SpriteRenderer Visual { get; private set; }
        public PlayerMountController Rider { get; private set; }
        public bool IsMounted => Rider != null;
        public override bool IsAvailable => isActiveAndEnabled && !IsMounted;
        protected override float HighlightScale => 1f;
        private Collider2D body;
        private AnimalRoamingVisual roaming;

        public void Configure(string id, SpriteRenderer visual)
        {
            Id = id; Home = transform.position; Visual = visual;
            body = GetComponent<Collider2D>(); roaming = GetComponent<AnimalRoamingVisual>();
        }
        public override string GetInteractionLabel(FarmTool tool) => "Montar · silla de montar";
        public override void Interact(FarmTool tool, PlayerInventory inventory)
        {
            if (inventory == null) return;
            var riding = inventory.GetComponent<PlayerMountController>();
            if (riding == null) riding = inventory.gameObject.AddComponent<PlayerMountController>();
            riding.TryMount(this);
        }
        public void AttachRider(PlayerMountController value)
        {
            Rider = value;
            if (body != null) body.enabled = false;
            if (roaming != null) roaming.enabled = false;
        }
        public void ReleaseRider(Vector3 position)
        {
            Rider = null; transform.position = position;
            if (Visual != null) { Visual.sprite = HouseSprites.Slice("PackHorseIdle", 0, 0, 32, 32); Visual.flipX = false; }
            if (body != null) body.enabled = true;
            if (roaming != null) { roaming.enabled = true; roaming.ResetHome(); }
        }
        public void ShowRider(Vector2 direction, bool moving)
        {
            if (Visual == null) return;
            int row = Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ? 2 : direction.y > 0 ? 1 : 0;
            int frame = (int)(Time.time * (moving ? 10f : 3f));
            Visual.sprite = moving ? HouseSprites.Slice("PackRiderRun", frame % 6 * 32, row * 48, 32, 48) :
                HouseSprites.Slice("PackRiderIdle", (row * 2 + frame % 2) * 32, 0, 32, 48);
            Visual.flipX = row == 2 && direction.x < 0;
        }
        protected override void OnEnable() { base.OnEnable(); All.Add(this); }
        protected override void OnDisable() { All.Remove(this); if (Rider != null) Rider.ForceDismount(); base.OnDisable(); }
    }
}
