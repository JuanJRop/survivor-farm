using System.Collections.Generic;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class FarmDefense : WorldInteractable, IDamageable
    {
        private static readonly List<FarmDefense> all = new List<FarmDefense>();
        public static IReadOnlyList<FarmDefense> All => all;
        private BuildingData data;
        private ConstructionSystem construction;
        private PlayerInventory player;
        private SpriteRenderer visual;
        private float nextShot, nextRepair;
        private int health, maximum;
        private CombatTelegraph range;
        public bool IsCore { get; private set; }
        public int Health => health;
        public int Maximum => maximum;
        public bool IsAlive => isActiveAndEnabled && health > 0;
        public int SpawnGeneration => 0;
        public string Kind => IsCore ? "Core" : data?.kind;
        public override bool IsAvailable => IsAlive && health < maximum;
        public static bool IsDefense(string kind) => kind == "Fence" || kind == "Trap" || kind == "Turret";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => all.Clear();
        private void OnEnable() { if(!all.Contains(this))all.Add(this); }
        private void OnDisable() => all.Remove(this);

        public void Configure(BuildingData state, ConstructionSystem owner, PlayerInventory inventory)
        {
            data=state; construction=owner; player=inventory;
            maximum=state.kind=="Fence"?14:state.kind=="Turret"?10:8;
            health=state.health<0?maximum:Mathf.Clamp(state.health,0,maximum);
            data.health=health;
            visual=GetComponentInChildren<SpriteRenderer>();
            if(Kind=="Trap")
            {
                GetComponent<Collider2D>().isTrigger=true;
                if(visual!=null){visual.color=new Color(.75f,.84f,.95f);visual.transform.localScale*=.85f;}
            }
            if(Kind=="Turret")range=CombatTelegraph.Create(transform,"Ballesta · alcance");
            Refresh();
        }
        public void ConfigureCore(PlayerInventory inventory, int maxHealth)
        {
            IsCore=true; player=inventory; maximum=maxHealth; health=maximum;
            visual=GetComponentInChildren<SpriteRenderer>();
        }
        public void RestoreHealth(int value) { health=Mathf.Clamp(value,1,maximum); Refresh(); }
        public override string GetInteractionLabel(FarmTool tool) =>
            $"Reparar {(IsCore?"pozo":ConstructionSystem.Label(Kind))} · 2 madera · {health}/{maximum}";
        public override void Interact(FarmTool tool, PlayerInventory inventory) => Repair(inventory);
        public bool Repair(PlayerInventory inventory)
        {
            if(!IsAvailable || inventory==null || Time.time<nextRepair ||
                Vector2.Distance(inventory.transform.position,transform.position)>1.6f || !inventory.TryRemoveWood(2))return false;
            nextRepair=Time.time+.6f; health=Mathf.Min(maximum,health+6); Refresh();
            inventory.GetComponent<GameFeelFeedback>()?.Pulse("+6 reparación",transform.position,false,true);
            PortfolioSession.Instance?.RegisterRepair();
            return true;
        }
        public void TakeDamage(int amount,PlayerInventory source)
        {
            if(!IsAlive||amount<=0)return;
            health=Mathf.Max(0,health-amount);Refresh();VisibleHitFeedback.Play(gameObject);
            player?.GetComponent<GameFeelFeedback>()?.Pulse("−"+amount,transform.position,true);
            if(health>0)return;
            if(IsCore)PortfolioSession.Instance?.Lose("El pozo ha caído. La granja necesita sus defensas.");
            else construction?.RemoveDestroyed(data);
        }
        private void Refresh()
        {
            if(data!=null)data.health=health;
            if(visual!=null&&!IsCore)visual.color=Color.Lerp(new Color(.5f,.32f,.25f),Color.white,health/(float)Mathf.Max(1,maximum));
        }
        public override void SetHighlighted(bool highlighted)
        {
            if(range==null)return;
            if(highlighted)range.Show(transform.position,4.5f,new Color(.6f,.9f,1,.6f));else range.Hide();
        }
        private void Update()
        {
            if(!IsAlive||!PortfolioSession.Active||!PortfolioSession.Instance.InCombat||Time.time<nextShot)return;
            float reach=Kind=="Trap"?.8f:Kind=="Turret"?4.5f:0;
            if(reach==0)return;
            IDamageable nearest=null;float distance=reach*reach;
            foreach(var collider in Physics2D.OverlapCircleAll(transform.position,reach))
            {
                var target=collider.GetComponentInParent<IDamageable>();
                if(!DamageRules.CanPlayerHit(target)||!(target is EnemyAIBase))continue;
                float d=(target.Transform.position-transform.position).sqrMagnitude;
                if(d>distance)continue;
                if(Kind=="Turret"&&!Clear(target))continue;
                nearest=target;distance=d;
            }
            if(nearest==null)return;
            nextShot=Time.time+(Kind=="Trap"?1.5f:2.3f);
            if(Kind=="Trap")
            {
                nearest.TakeDamage(3,player);
                CultivationSoilVisual.Emit(transform.position,false);
            }
            else
            {
                var arrow=new GameObject("Ballesta · flecha");arrow.transform.position=transform.position;
                var art=arrow.AddComponent<SpriteRenderer>();art.sprite=CombatFeelVisuals.Arrow;art.sortingOrder=15000;
                arrow.AddComponent<ArrowProjectile>().Configure(nearest,2,player);
            }
        }
        private bool Clear(IDamageable target)
        {
            foreach(var hit in Physics2D.LinecastAll(transform.position,target.Transform.position))
                if(!hit.collider.isTrigger&&!hit.transform.IsChildOf(transform)&&!hit.transform.IsChildOf(target.Transform))return false;
            return true;
        }
    }
}
