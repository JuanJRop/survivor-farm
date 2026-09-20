using System;
using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
namespace SurvivorFarm.Runtime.Player
{
    [Serializable] public sealed class HouseData { public int level,style;public bool inside; }
    public sealed class HouseSystem : MonoBehaviour
    {
        public HouseData Data=new HouseData();public bool IsInside {get;private set;}
        public static readonly Vector3 Center=new Vector3(0,100,0);
        public static readonly string[] Names={"Refugio","Casa de madera","Casa de piedra","Mansión del valle"};
        public Transform RoomRoot {get;private set;}Transform shell;GameObject outside;SpriteRenderer facade;BoxCollider2D exteriorCollider;PlayerInventory inventory;BaseHouse exterior;
        public Vector2 Size=>new[]{new Vector2(8,6),new Vector2(10,8),new Vector2(14,10),new Vector2(18,12)}[Data.level];
        public Vector3 ReturnPosition => exterior!=null?exterior.transform.position+Vector3.down*1.5f:Vector3.zero;float outsideCamera=6;
        public bool CanUseServices => !IsInside && exterior!=null && Vector2.Distance(transform.position,exterior.transform.position)<=2.5f && (GetComponent<PlayerSurvivalStats>()==null||GetComponent<PlayerSurvivalStats>().CurrentHealth>0);
        public bool Contains(Vector2 p)=>Mathf.Abs(p.x)<Size.x/2-.65f&&Mathf.Abs(p.y-100)<Size.y/2-.65f;
        void Awake()
        {
            inventory=GetComponent<PlayerInventory>();
            outside=FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="Outdoor World")?.gameObject;
            var house=FindFirstObjectByType<BaseHouse>(FindObjectsInactive.Include);exterior=house;
            if(house!=null){exteriorCollider=house.GetComponent<BoxCollider2D>();foreach(var sr in house.GetComponentsInChildren<SpriteRenderer>())sr.enabled=false;var art=new GameObject("Fachada mejorable");art.transform.SetParent(house.transform,false);art.transform.localPosition=new Vector3(0,-.5f);facade=art.AddComponent<SpriteRenderer>();art.AddComponent<SurvivorFarm.Runtime.World.WorldSpriteDepth>().Visual=facade;}
            RoomRoot=new GameObject("Interior de tu casa").transform;Rebuild();RoomRoot.gameObject.SetActive(false);
            foreach(var bed in FindObjectsByType<PlayerBed>(FindObjectsInactive.Include,FindObjectsSortMode.None))bed.gameObject.SetActive(false);
        }
        public HouseData Snapshot()=>new HouseData{level=Data.level,style=Data.style,inside=false};
        public void LoadLayout(HouseData data){Data=data??new HouseData();Data.level=Mathf.Clamp(Data.level,0,3);Data.style=Mathf.Clamp(Data.style,0,2);Rebuild();}
        void Tile(Transform parent,string name,Sprite sprite,Vector3 p,Vector2 size,int order,Color color)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=p;var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.color=color;sr.sortingOrder=order;if(sprite!=null)go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);}
        void Wall(Vector3 p,Vector2 size){var go=new GameObject("Pared sólida");go.transform.SetParent(shell,false);go.transform.position=p;go.AddComponent<BoxCollider2D>().size=size;}
        void Rebuild()
        {
            if(RoomRoot==null)return;
            if(shell!=null){shell.gameObject.SetActive(false);Destroy(shell.gameObject);}shell=new GameObject("Suelo y paredes").transform;shell.SetParent(RoomRoot,false);
            var floor=HouseSprites.Slice("HouseTiles",Data.style==2?208:272,16,16,16);var wall=HouseSprites.Slice("HouseTiles",16,96,32,64);
            Color tint=Data.style==1?new Color(.85f,.95f,1):Data.style==2?new Color(.85f,.85f,.87f):Color.white;
            for(int x=-(int)Size.x/2;x<Size.x/2;x++)for(int y=-(int)Size.y/2;y<Size.y/2;y++)Tile(shell,"Suelo",floor,Center+new Vector3(x+.5f,y),Vector2.one,-10000,tint);
            for(int x=-(int)Size.x/2;x<Size.x/2;x++)Tile(shell,"Pared del fondo",wall,Center+new Vector3(x+.5f,Size.y/2),new Vector2(1,1.5f),-9990,tint);
            Wall(Center+Vector3.up*(Size.y/2+.15f),new Vector2(Size.x+.6f,.3f));Wall(Center+Vector3.down*(Size.y/2+.15f),new Vector2(Size.x+.6f,.3f));
            Wall(Center+Vector3.left*(Size.x/2+.15f),new Vector2(.3f,Size.y));Wall(Center+Vector3.right*(Size.x/2+.15f),new Vector2(.3f,Size.y));
            var exit=new GameObject("Puerta de salida");exit.transform.SetParent(shell,false);exit.transform.position=Center+Vector3.down*(Size.y/2-.65f);var sr=exit.AddComponent<SpriteRenderer>();sr.sprite=HouseSprites.Slice("HouseTiles",0,0,64,64);sr.sortingOrder=100;exit.transform.localScale=Vector3.one*.2f;exit.AddComponent<CircleCollider2D>().isTrigger=true;exit.AddComponent<HouseExit>().Owner=this;
            var text=new GameObject("Ayuda de la casa");text.transform.SetParent(shell,false);text.transform.position=Center+Vector3.up*(Size.y/2+.65f);var label=text.AddComponent<TextMesh>();label.text=Names[Data.level]+" · H: amueblar y ampliar";label.characterSize=.075f;label.fontSize=40;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.3f,.16f,.1f);text.GetComponent<MeshRenderer>().sortingOrder=2000;
            if(facade!=null)
            {
                facade.sprite=HouseSprites.Facade(Data.level);
                if(facade.sprite!=null)
                {
                    facade.transform.localScale=Vector3.one*.5f;
                    if(exteriorCollider!=null)exteriorCollider.size=new Vector2(facade.sprite.bounds.size.x*.4f,.8f);
                }
            }
        }
        public void Enter(bool notify=true)
        {
            OpenServices();
        }
        public void OpenServices()
        {
            if(!CanUseServices)return;
            Data.inside=false;
            FindFirstObjectByType<AdventureWindow>()?.Open("Home");
        }
        public void Exit(bool notify=true)
        {
            FindFirstObjectByType<AdventureWindow>()?.Close();GetComponent<ConstructionSystem>()?.Cancel();IsInside=false;Data.inside=false;RoomRoot.gameObject.SetActive(false);if(outside!=null)outside.SetActive(true);
            var campaign=GetComponent<ValleyCampaign>();if(campaign!=null)campaign.Teleport(ReturnPosition);else {transform.position=ReturnPosition;var body=GetComponent<Rigidbody2D>();if(body!=null){body.position=ReturnPosition;body.linearVelocity=Vector2.zero;}}
            if(Camera.main!=null){Camera.main.orthographicSize=outsideCamera;Camera.main.transform.position=ReturnPosition+new Vector3(0,0,-10);}
            if(notify)FarmNotificationCenter.Show("Saliste de casa.");
        }
        public void RestorePresence(bool inside){if(inside||IsInside)Exit(false);else if(RoomRoot!=null)RoomRoot.gameObject.SetActive(false);Data.inside=false;}
        public int RequiredLevel(string kind)=>kind=="Cabinet"||kind=="Workbench"?1:kind=="Furnace"?2:0;
        public int Price(string kind)=>kind switch{"Campfire"=>12,"Bed"=>25,"Chest"=>35,"Cabinet"=>50,"Furnace"=>80,"Workbench"=>60,_=>0};
        public string Requirement(string kind)=>Data.level<RequiredLevel(kind)?"Necesita "+Names[RequiredLevel(kind)]:kind!="Campfire"&&!GetComponent<PlayerCraftingController>().CampfireBuilt?"Primero coloca una fogata":"";
        public bool Buy(string kind)
        {int price=Price(kind);if(!CanUseServices||price<=0||Requirement(kind)!=""||inventory.Coins<price)return false;if(!inventory.TrySpendCoins(price))return false;inventory.AddPacked(kind);FarmNotificationCenter.Show(ConstructionSystem.Label(kind)+" en tu mochila [I]. Elige Colocar para situarlo.");Save();return true;}
        public void UpgradeCost(out int wood,out int stone,out int iron,out int coins){wood=new[]{30,80,180,0}[Data.level];stone=new[]{20,60,140,0}[Data.level];iron=new[]{0,5,20,0}[Data.level];coins=new[]{50,150,400,0}[Data.level];}
        public string UpgradeRequirement=>Data.level==3?"Casa al máximo":!GetComponent<PlayerCraftingController>().CampfireBuilt?"Coloca tu primera fogata":Data.level==1&&!GetComponent<ValleyCampaign>().Data.camp?"Abre el camino al campamento":Data.level==2&&!GetComponent<ValleyCampaign>().Data.boss?"Recupera el valle venciendo al Rey Limo":"";
        public bool Upgrade()
        {
            if(!CanUseServices||UpgradeRequirement!="")return false;UpgradeCost(out int w,out int s,out int i,out int c);var progress=GetComponent<AdventureProgress>();if(inventory.Wood<w||inventory.Stone<s||inventory.Coins<c||progress.Data.iron<i)return false;
            inventory.TryRemoveWood(w);inventory.TryRemoveStone(s);if(c>0)inventory.TrySpendCoins(c);if(i>0)progress.SpendIron(i);Data.level++;Rebuild();Save();FarmNotificationCenter.Show("Tu hogar ahora es: "+Names[Data.level]+". Tienes más espacio y nuevos muebles.");return true;
        }
        public void ChangeStyle(){if(!CanUseServices||Data.level<1)return;Data.style=(Data.style+1)%3;Rebuild();Save();}
        public void Sleep(BuildingData bed)
        {
            if(bed==null||bed.kind!="Bed"||!GetComponent<ConstructionSystem>().Buildings.Contains(bed)||GetComponent<PlayerSurvivalStats>().CurrentHealth<=0||!GetComponent<ConstructionSystem>().CanAccess(bed))return;
            var clock=FindFirstObjectByType<DayNightCycle>();if(clock==null||!clock.TrySleepUntilMorning()){FarmNotificationCenter.Show("Puedes dormir de noche, entre las 21:00 y las 05:00.");return;}
            var stats=GetComponent<PlayerSurvivalStats>();stats.Restore(stats.MaxHealth,stats.MaxHealth,stats.HungerPercent);GetComponent<PlayerRespawnController>()?.SetCheckpoint(ReturnPosition);Save();FarmNotificationCenter.Show("Descansaste en tu cama. Ya es un nuevo día.");
        }
        public bool Smelt(BuildingData furnace)
        {if(furnace==null||furnace.kind!="Furnace"||!GetComponent<ConstructionSystem>().Buildings.Contains(furnace)||!GetComponent<ConstructionSystem>().CanAccess(furnace)||inventory.Wood<2||inventory.Stone<5)return false;inventory.TryRemoveWood(2);inventory.TryRemoveStone(5);GetComponent<AdventureProgress>().AddIron(1);Save();return true;}
        void Save()=>FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);
        void Update(){if(IsInside&&Mathf.Abs(transform.position.y-100)>20){IsInside=false;Data.inside=false;RoomRoot.gameObject.SetActive(false);if(outside!=null)outside.SetActive(true);if(Camera.main!=null)Camera.main.orthographicSize=outsideCamera;}}
        void OnDestroy(){if(RoomRoot!=null)Destroy(RoomRoot.gameObject);}
    }
    public sealed class HouseExit : WorldInteractable
    {public HouseSystem Owner;public override string GetInteractionLabel(FarmTool tool)=>"Salir de casa";public override void Interact(FarmTool tool,PlayerInventory inventory){if(inventory==Owner.GetComponent<PlayerInventory>())Owner.Exit();}}
}
