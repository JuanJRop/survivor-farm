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
        private float nextShot, nextRepair, nextScan;
        private PlayerProjectilePool projectiles;
        private readonly List<Collider2D> targets = new List<Collider2D>(32);
        private readonly List<RaycastHit2D> sightHits = new List<RaycastHit2D>(16);
        private int health, maximum;
        private CombatTelegraph range;
        public bool IsCore { get; private set; }
        public int Health => health;
        public int Maximum => maximum;
        public bool IsAlive => isActiveAndEnabled && health > 0;
        public int SpawnGeneration => 0;
        public string Kind => IsCore ? "Core" : data?.kind;
        public Vector2 ContactPoint(Vector2 from)
        {
            var collider=GetComponent<Collider2D>();
            return collider!=null?collider.ClosestPoint(from):(Vector2)transform.position;
        }
        public override bool IsAvailable => !IsCore && IsAlive && health < maximum;
        public static bool IsDefense(string kind) => FortressPieces.IsWall(kind) || kind == "Trap" || kind == "Turret";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => all.Clear();
        protected override void OnEnable() { base.OnEnable(); if(!all.Contains(this))all.Add(this); }
        protected override void OnDisable() { all.Remove(this); base.OnDisable(); }

        public void Configure(BuildingData state, ConstructionSystem owner, PlayerInventory inventory)
        {
            data=state; construction=owner; player=inventory;
            maximum=FortressPieces.IsWall(state.kind)?(state.wallVersion==0?14:FortressPieces.Health(state.kind)):state.kind=="Turret"?10:8;
            health=state.health<0?maximum:Mathf.Clamp(state.health,0,maximum);
            data.health=health;
            visual=GetComponentInChildren<SpriteRenderer>();
            if(Kind=="Trap")
            {
                GetComponent<Collider2D>().isTrigger=true;
                if(visual!=null){visual.color=new Color(.75f,.84f,.95f);visual.transform.localScale*=.85f;}
            }
            if(Kind=="Turret")
            {
                range=CombatTelegraph.Create(transform,"Ballesta · alcance");
                if(projectiles==null)projectiles=PlayerProjectilePool.Create(transform,1,2,4);
            }
            nextScan=Time.time+(Mathf.Abs(GetInstanceID())%15)*.01f;
            Refresh();
        }
        public void ConfigureCore(PlayerInventory inventory, int maxHealth)
        {
            IsCore=true; player=inventory; maximum=maxHealth; health=maximum;
            visual=GetComponentInChildren<SpriteRenderer>();
        }
        public void RestoreHealth(int value) { health=Mathf.Clamp(value,1,maximum); Refresh(); }
        public void ReduceStrength(int amount)
        {if(!IsCore)return;maximum=Mathf.Max(12,maximum-Mathf.Max(0,amount));health=Mathf.Min(health,maximum);Refresh();}
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
            if(!IsAlive||amount<=0||IsCore)return;
            health=Mathf.Max(0,health-amount);Refresh();VisibleHitFeedback.Play(gameObject,.12f,false);
            player?.GetComponent<GameFeelFeedback>()?.Pulse("−"+amount,transform.position,true,false,false);
            player?.GetComponent<AudioFeedback>()?.Play(health<=0?CombatSound.Break:CombatSound.Chop,transform.position,.7f);
            CombatHitParticles.Spawn(transform.position,transform.parent,Kind=="StoneWall"||Kind=="ReinforcedWall"?ImpactSurface.Stone:ImpactSurface.Wood,false,health<=0);
            if(health>0)return;
            construction?.RemoveDestroyed(data);
        }
        private void Refresh()
        {
            if(data!=null)data.health=health;
            if(visual!=null&&!IsCore)visual.color=Color.Lerp(new Color(.5f,.32f,.25f),Kind=="ReinforcedWall"?new Color(1,.86f,.58f):Color.white,health/(float)Mathf.Max(1,maximum));
        }
        public override void SetHighlighted(bool highlighted)
        {
            if(range==null)return;
            if(highlighted)range.Show(transform.position,4.5f,new Color(.6f,.9f,1,.6f));else range.Hide();
        }
        private void Update()
        {
            if(!IsAlive||!PortfolioSession.Active||!PortfolioSession.Instance.InCombat||Time.time<nextShot||Time.time<nextScan)return;
            float reach=Kind=="Trap"?.8f:Kind=="Turret"?4.5f:0;
            if(reach==0)return;
            nextScan=Time.time+.15f;
            IDamageable nearest=null;float distance=reach*reach;
            Physics2D.OverlapCircle(transform.position,reach,new ContactFilter2D{useTriggers=true},targets);
            foreach(var collider in targets)
            {
                var target=collider.GetComponentInParent<IDamageable>();
                if(!DamageRules.CanPlayerHit(target)||!(target is EnemyAIBase))continue;
                float d=(target.Transform.position-transform.position).sqrMagnitude;
                if(d>distance)continue;
                if(Kind=="Turret"&&!Clear(target))continue;
                nearest=target;distance=d;
            }
            if(nearest==null)return;
            if(Kind=="Trap")
            {
                nextShot=Time.time+1.5f;
                nearest.TakeDamage(3,player);
                CultivationSoilVisual.Emit(transform.position,false);
            }
            else
            {
                if(projectiles==null)projectiles=PlayerProjectilePool.Create(transform,1,2,4);
                var arrow=projectiles.Rent();if(arrow==null)return;
                nextShot=Time.time+2.3f;
                arrow.transform.position=transform.position;arrow.transform.localScale=Vector3.one;
                var art=arrow.GetComponent<SpriteRenderer>();art.sprite=CombatFeelVisuals.Arrow;art.sortingOrder=15000;
                art.enabled=true;art.color=Color.white;art.flipX=false;art.flipY=false;
                arrow.Configure(nearest,2,player,false,transform);arrow.gameObject.SetActive(true);
            }
        }
        private bool Clear(IDamageable target)
        {
            Physics2D.Linecast(transform.position,target.Transform.position,new ContactFilter2D{useTriggers=false},sightHits);
            foreach(var hit in sightHits)
                if(!hit.transform.IsChildOf(transform)&&!hit.transform.IsChildOf(target.Transform))return false;
            return true;
        }
        private void OnDestroy(){if(projectiles!=null)Destroy(projectiles.gameObject);}
    }
}
