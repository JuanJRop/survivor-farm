using System;
using System.Collections.Generic;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using SurvivorFarm.Runtime.World;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    [Serializable] public sealed class HouseDamageSnapshot {public string id;public int health;}
    /// <summary>A village home can fall into ruins and be rebuilt at its original doorway.</summary>
    public sealed class VillageHouseHealth : WorldInteractable,IDamageable
    {
        public static readonly HashSet<VillageHouseHealth> All=new HashSet<VillageHouseHealth>();
        public string Id {get;private set;}
        public int Health {get;private set;}=32;
        public int Level {get;private set;}=1;
        public int Maximum=>32+(Level-1)*24;
        public bool IsAlive=>isActiveAndEnabled&&Health>0;
        public int SpawnGeneration=>0;
        public override Transform Transform=>repairPoint!=null?repairPoint:transform;
        Transform IDamageable.Transform=>transform;
        public override bool IsAvailable=>isActiveAndEnabled&&(Health<Maximum||VillageProgression.Instance!=null);
        protected override float HighlightScale=>1f;
        private SpriteRenderer[] art;
        private Collider2D[] walls;
        private bool[] originalArt,originalWalls;
        private GameObject ruins;
        private Transform repairPoint;
        private float nextRepair;
        private SpriteRenderer facade;
        private Sprite originalFacade;
        private Vector3 originalScale;
        private Color originalColor;
        private float originalWidth;
        public void Configure(string id)
        {
            Id=id;art=GetComponentsInChildren<SpriteRenderer>(true);walls=GetComponentsInChildren<Collider2D>(true);
            originalArt=Array.ConvertAll(art,r=>r.enabled);originalWalls=Array.ConvertAll(walls,c=>c.enabled);
            foreach(var renderer in art)if(renderer.enabled&&renderer.sprite!=null)
            {facade=renderer;originalFacade=renderer.sprite;originalScale=renderer.transform.localScale;originalColor=renderer.color;originalWidth=renderer.bounds.size.x;break;}
            var readout=GetComponent<WorldHealthReadout>();
            if(readout==null)readout=gameObject.AddComponent<WorldHealthReadout>();
            readout.Configure(this);
            if(repairPoint==null)
            {
                Physics2D.SyncTransforms();
                repairPoint=new GameObject("Reparar casa · entrada").transform;
                repairPoint.SetParent(transform,false);
                repairPoint.position=ContactPoint((Vector2)transform.position+Vector2.down*5)+Vector2.down*.45f;
            }
        }
        protected override void OnEnable(){base.OnEnable();All.Add(this);}
        protected override void OnDisable(){All.Remove(this);base.OnDisable();}
        public Vector2 ContactPoint(Vector2 from)
        {
            Vector2 nearest=transform.position;float distance=float.PositiveInfinity;
            foreach(var wall in walls??Array.Empty<Collider2D>())
            {
                if(wall==null||!wall.enabled||wall.isTrigger)continue;
                Vector2 p=wall.ClosestPoint(from);float d=(p-from).sqrMagnitude;
                if(d<distance){distance=d;nearest=p;}
            }
            return nearest;
        }
        public void TakeDamage(int amount,PlayerInventory source)
        {
            if(!IsAlive||source!=null||amount<=0)return;
            Health=Mathf.Max(0,Health-amount);
            AudioFeedback.PlayAt(Health==0?CombatSound.Break:CombatSound.Chop,transform.position,.85f);
            VisibleHitFeedback.Play(gameObject,.1f,false);
            CombatHitParticles.Spawn(transform.position,transform.parent,ImpactSurface.Wood,Health==0,Health==0);
            if(Health==0){Refresh();Core.PortfolioSession.Instance?.Navigation.Invalidate();}
            Core.PortfolioSession.Instance?.Security?.RefreshState();
        }
        public override string GetInteractionLabel(FarmTool tool)=>Health<Maximum?
            $"{(Health==0?"Reconstruir":"Reparar")} casa · 2 madera · {Health}/{Maximum}":$"Mejorar casa · nivel {Level}";
        public override void Interact(FarmTool tool,PlayerInventory inventory)
        {
            if(VillageProgression.Instance!=null)VillageProgression.Instance.OpenHouse(this);
            else Repair(inventory);
        }
        public bool Repair(PlayerInventory inventory)
        {
            if(!isActiveAndEnabled||Health>=Maximum||inventory==null||Time.time<nextRepair||
                (Vector2.Distance(inventory.transform.position,Transform.position)>1.65f&&VillageProgression.Instance?.CanManage!=true))return false;
            if(!inventory.TryRemoveWood(2)){FarmNotificationCenter.Show("Necesitas 2 madera para reparar la casa.");return false;}
            int restored=Mathf.Min(8,Maximum-Health);Health+=restored;nextRepair=Time.time+.6f;
            Refresh();Core.PortfolioSession.Instance?.Navigation.Invalidate();
            Core.PortfolioSession.Instance?.Security?.RefreshState();
            inventory.GetComponent<GameFeelFeedback>()?.Pulse("Casa +"+restored,Transform.position,false,true);
            return true;
        }
        public void SetLevel(int value,bool grantCapacity=false)
        {
            int previous=Maximum;Level=Mathf.Clamp(value,1,3);
            Health=Mathf.Clamp(Health+(grantCapacity?Maximum-previous:0),0,Maximum);
            if(facade!=null)
            {
                facade.sprite=Level==1?originalFacade:HouseSprites.Facade(Level);
                facade.transform.localScale=Level==1?originalScale:Vector3.one*(originalWidth/facade.sprite.bounds.size.x);
                facade.color=Level==1?originalColor:Color.white;
            }
            Refresh();
        }
        private void Refresh()
        {
            for(int i=0;i<(art?.Length??0);i++)if(art[i]!=null)art[i].enabled=Health>0&&originalArt[i];
            for(int i=0;i<(walls?.Length??0);i++)if(walls[i]!=null)walls[i].enabled=Health>0&&originalWalls[i];
            if(Health<=0&&ruins==null)
            {
                ruins=new GameObject("Restos de la casa");ruins.transform.SetParent(transform,false);
                var p=(Vector2)transform.position;
                PackEnvironment.Sprite(ruins.transform,"Tablones caídos","PackWoodDebris",0,0,64,16,p+Vector2.left*.2f,.5f);
                PackEnvironment.Sprite(ruins.transform,"Piedra del cimiento","MineralRocks",0,144,32,16,p+Vector2.right*.4f,.75f);
            }
            if(ruins!=null)ruins.SetActive(Health<=0);
        }
        public HouseDamageSnapshot Capture()=>new HouseDamageSnapshot{id=Id,health=Health};
        public void Restore(HouseDamageSnapshot data){Health=data==null?Maximum:Mathf.Clamp(data.health,0,Maximum);Refresh();}
    }
}
