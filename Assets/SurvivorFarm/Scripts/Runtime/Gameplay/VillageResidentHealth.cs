using System;
using System.Collections.Generic;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.UI;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    [Serializable] public sealed class ResidentSnapshot {public string id;public int health=8;public float x,y;}

    /// <summary>A civilian's body, life and death. Routines and enemy targeting remain independent.</summary>
    public sealed class VillageResidentHealth : WorldInteractable,IDamageable
    {
        public static readonly HashSet<VillageResidentHealth> All=new HashSet<VillageResidentHealth>();
        public string Id {get;private set;}
        public int Health {get;private set;}=8;
        public int Maximum {get;private set;}=8;
        public int Armor {get;private set;}
        public string DisplayName=>GetComponent<VillageGuard>()?.DisplayName??VillageResidents.Name(Id);
        public bool IsAlive=>Health>0&&gameObject.activeInHierarchy;
        public int SpawnGeneration=>0;
        public override bool IsAvailable=>IsAlive&&Health<Maximum;
        protected override float HighlightScale=>1f;
        public Vector2 Home {get;private set;}
        private SpriteRenderer visual;
        private float flashUntil;
        private float nextAid;
        public void Configure(string id)
        {
            Id=id;Home=transform.position;visual=GetComponentInChildren<SpriteRenderer>();
            var body=GetComponent<CircleCollider2D>();
            if(body==null)body=gameObject.AddComponent<CircleCollider2D>();
            body.isTrigger=false;body.radius=.26f;body.offset=new Vector2(0,.26f);
        }
        protected override void OnEnable(){base.OnEnable();All.Add(this);}
        protected override void OnDisable(){All.Remove(this);base.OnDisable();}
        public void TakeDamage(int amount,PlayerInventory source)
        {
            if(!IsAlive||amount<=0||source!=null)return;
            amount=Mathf.Max(1,amount-Armor);
            Health=Mathf.Max(0,Health-amount);flashUntil=Time.time+.15f;
            HitFeedback.Report(gameObject,null,amount,Health==0);
            if(Health==0)
            {
                FarmNotificationCenter.Show(DisplayName+(GetComponent<VillageGuard>()!=null?" ha caído. Recupéralo desde las mejoras del pueblo.":" ha caído. La seguridad de Raízclara disminuye."));
                gameObject.SetActive(false);
            }
            PortfolioSession.Instance?.Security?.RefreshState();
        }
        public void SetTraining(int maximum,int armor,bool grantCapacity=false)
        {
            int previous=Maximum;Maximum=Mathf.Clamp(maximum,8,64);Armor=Mathf.Clamp(armor,0,2);
            Health=Mathf.Clamp(Health+(grantCapacity?Maximum-previous:0),0,Maximum);
        }
        public void Heal(int amount){if(IsAlive&&amount>0)Health=Mathf.Min(Maximum,Health+amount);}
        public override string GetInteractionLabel(FarmTool tool)=>$"Auxiliar a {DisplayName} · 1 ración · {Health}/{Maximum}";
        public override void Interact(FarmTool tool,PlayerInventory inventory)=>Aid(inventory);
        public bool Aid(PlayerInventory inventory)
        {
            if(!IsAvailable||inventory==null||Time.time<nextAid||Vector2.Distance(inventory.transform.position,transform.position)>1.65f)return false;
            if(!inventory.TryRemoveFood(1)){FarmNotificationCenter.Show("Necesitas 1 ración para auxiliar al aldeano.");return false;}
            int restored=Mathf.Min(4,Maximum-Health);Health+=restored;nextAid=Time.time+.6f;
            PortfolioSession.Instance?.Security?.RefreshState();
            inventory.GetComponent<GameFeelFeedback>()?.Pulse(VillageResidents.Name(Id)+" +"+restored,transform.position,false,true);
            return true;
        }
        void LateUpdate(){if(visual!=null&&Time.time<flashUntil)visual.color=new Color(1,.45f,.4f);}
        public ResidentSnapshot Capture()=>new ResidentSnapshot{id=Id,health=Health,x=transform.position.x,y=transform.position.y};
        public void Restore(ResidentSnapshot data)
        {
            Health=data==null?Maximum:Mathf.Clamp(data.health,0,Maximum);
            Vector2 destination=data==null?Home:new Vector2(data.x,data.y);
            if(World.FarmExploration.Contains(destination,1))transform.position=new Vector3(destination.x,destination.y,0);
            flashUntil=0;nextAid=0;
            if(visual!=null)visual.color=Color.white;
            gameObject.SetActive(Health>0);
        }
    }
}
