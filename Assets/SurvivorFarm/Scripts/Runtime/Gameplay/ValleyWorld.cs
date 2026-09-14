using System.Collections.Generic;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.World;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class ValleyWorld : MonoBehaviour
    {
        struct VillageUpgradeArt { public string Id; public GameObject Root; }
        ValleyCampaign campaign;
        readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        readonly List<ValleyEnemy> enemies=new List<ValleyEnemy>();
        readonly List<GameObject> restoredArt=new List<GameObject>();
        readonly List<VillageUpgradeArt> villageDamage=new List<VillageUpgradeArt>();
        readonly List<VillageUpgradeArt> villageUpgrades=new List<VillageUpgradeArt>();
        VillageNpcArtCatalog npcArt;
        ValleyInteraction workshop;
        GameObject chicken, deer, seal, cargo, maraHouse, millHouse, warehouseHouse, shopHouse;
        public static Vector3 Center(int zone)=>new Vector3(100+40*(zone-1),0,0);
        public Sprite Art(string name)
        {
            if(sprites.TryGetValue(name,out var found))return found;
            var tex=Resources.Load<Texture2D>("StoryArt/"+name);
            if(tex==null)return Resources.Load<Sprite>("BackpackIcons/"+name);
            Rect r=new Rect(0,0,tex.width,tex.height);
            if(name=="Boss")r=new Rect(0,tex.height-32,16,32);
            if(name=="Josh")r=new Rect(0,tex.height-32,32,32);
            if(name=="Portal")r=new Rect(96,tex.height-48,48,48);
            var s=Sprite.Create(tex,r,new Vector2(.5f,0),16);sprites[name]=s;return s;
        }
        public Sprite BossFrame()
        {
            var tex=Resources.Load<Texture2D>("StoryArt/Boss");
            int columns=Mathf.Max(1,tex.width/16);
            int frame=(int)(Time.time*6)%columns;string key="BossFrame"+frame;
            if(sprites.TryGetValue(key,out var sprite))return sprite;
            sprite=Sprite.Create(tex,new Rect(frame*16,tex.height-32,16,32),new Vector2(.5f,0),16);sprites[key]=sprite;return sprite;
        }
        public GameObject Prop(string key,Vector3 pos,float width=1,bool solid=false)
        {
            var go=new GameObject(key);go.transform.SetParent(transform,false);go.transform.position=pos;
            var art=new GameObject("Sprite");art.transform.SetParent(go.transform,false);
            var sr=art.AddComponent<SpriteRenderer>();sr.sprite=Art(key);
            if(sr.sprite!=null)art.transform.localScale=Vector3.one*(width/sr.sprite.bounds.size.x);
            go.AddComponent<WorldSpriteDepth>().Visual=sr;
            if(key=="Tree")TreeOcclusionFader.Ensure(go);
            if(solid){var c=go.AddComponent<BoxCollider2D>();c.size=new Vector2(width*.65f,.45f);c.offset=new Vector2(0,.2f);}
            return go;
        }
        public TextMesh Label(string text,Vector3 position,float size=.13f)
        {
            var go=new GameObject(text);go.transform.SetParent(transform,false);go.transform.position=position;
            var t=go.AddComponent<TextMesh>();t.text=text;t.characterSize=size;t.fontSize=40;t.anchor=TextAnchor.MiddleCenter;t.color=new Color(1,.95f,.75f);go.GetComponent<MeshRenderer>().sortingOrder=28000;return t;
        }
        ValleyInteraction Item(string key,Vector3 pos,string id,string label,float width=1)
        {var go=Prop(key,pos,width);var c=go.AddComponent<CircleCollider2D>();c.radius=.35f;c.isTrigger=true;var i=go.AddComponent<ValleyInteraction>();i.Campaign=campaign;i.Id=id;i.Label=label;if(id.StartsWith("go:")&&pos.x>70)Label(label,pos+Vector3.up*(width+.6f),.07f);return i;}
        void Ground(string key,Vector3 center,int w,int h)
        {for(int x=-w/2;x<w/2;x++)for(int y=-h/2;y<h/2;y++){var go=Prop(key,center+new Vector3(x+.5f,y),1);var sr=go.GetComponentInChildren<SpriteRenderer>();Destroy(go.GetComponent<WorldSpriteDepth>());sr.sortingOrder=key=="Floor"?-29950:-30000;}}
        void Wall(Vector3 p,Vector2 size){var go=new GameObject("Límite del sendero");go.transform.SetParent(transform,false);go.transform.position=p;go.AddComponent<BoxCollider2D>().size=size;}
        void Exit(int from,int to,Vector3 local,string text){Item("Sign",Center(from)+local,"go:"+to,text,.65f);}
        void BuildStartingVillage(ValleyCampaign owner)
        {
            var village=new GameObject("Pueblo inicial - Raizclara").transform;village.SetParent(transform,false);
            VillageLayout.ConnectPaths();
            // Doors share the same pixel scale as the player's house.
            maraHouse=VillageBuilding(village,"mara");
            millHouse=VillageBuilding(village,"dalia");
            warehouseHouse=VillageBuilding(village,"nico");
            shopHouse=VillageBuilding(village,"rolo");

            npcArt=Resources.Load<VillageNpcArtCatalog>("VillageNpcArt");
            VillageNpc(village,new Vector3(-4.1f,1.35f),"village:elder","Mara",1.6f,null,new Color(1f,.93f,.82f));
            VillageNpc(village,new Vector3(-4.15f,-3.3f),"village:blacksmith","Nico - herrero",1.6f,null,new Color(.78f,.86f,1f));
            VillageNpc(village,new Vector3(7.25f,-3.1f),"village:farmer","Dalia - provisiones",1.6f,null,new Color(.9f,1f,.76f));
            VillageNpc(village,new Vector3(5.05f,-4.2f),"village:merchant","Rolo - trueques",1.6f,null,new Color(1f,.86f,.66f));
            VillageNpc(village,new Vector3(8.55f,1.15f),"village:guard","Iria - guardia",1.6f,null,new Color(.86f,.9f,1f));
            VillageItem(village,"Sign",new Vector3(-1.45f,-1.1f),"village:board","Tablon de Raizclara",.7f,null);

            // Reuse the original well, so there is one communal water point.
            var originalWell=GameObject.Find("Well");
            if(originalWell!=null)
            {
                originalWell.transform.position=VillageLayout.Well;
                foreach(var sr in originalWell.GetComponentsInChildren<SpriteRenderer>())sr.enabled=false;
                foreach(var collider in originalWell.GetComponents<Collider2D>())collider.enabled=false;
            }
            var well=VillageItem(village,"Well",VillageLayout.Well,"village:well","Pozo comunal",1f,null);
            var wellFeet=well.AddComponent<BoxCollider2D>();wellFeet.size=new Vector2(.6f,.35f);wellFeet.offset=new Vector2(0,.15f);
            workshop=VillageItem(village,"Workbench",new Vector3(-7.05f,-3.25f),"village:workshop","Taller de Nico",.8f,null).GetComponent<ValleyInteraction>();

            // Small enclosed yards leave the streets and every entrance clear.
            for(int i=0;i<4;i++)
            {
                if(i<3)VillageProp(village,"Fence",new Vector3(-7.1f+i*.65f,1.1f),.65f,false);
                VillageProp(village,"Fence",new Vector3(7.75f+i*.9f,-6.3f),.8f,false);
            }
            VillageRuin(village,"nico","Fence",new Vector3(-6.8f,-4.1f),.6f,new Color(.66f,.58f,.49f));
            VillageRuin(village,"rolo","Chest",new Vector3(6.85f,-3.4f),.55f,new Color(.76f,.68f,.58f));
            VillageRuin(village,"guard","Rock",new Vector3(7.2f,2.1f),.45f,new Color(.68f,.66f,.6f));

            VillageUpgrade(village,"mara","Chest",new Vector3(-7.1f,2),.6f,Color.white);
            VillageUpgrade(village,"nico","Rock",new Vector3(-7.1f,-2.5f),.45f,new Color(.72f,.77f,.8f));
            VillageUpgrade(village,"nico","Iron",new Vector3(-7.05f,-2.85f),.3f,Color.white);
            VillageUpgrade(village,"nico","Rock",new Vector3(-3.9f,-2.55f),.35f,Color.white);
            var furnace=VillageUpgrade(village,"nico","Workbench",new Vector3(-6.9f,-1.9f),.6f,Color.white);
            SetSprite(furnace,HouseSprites.Furniture("Furnace"),.6f);
            for(int i=0;i<3;i++)VillageUpgrade(village,"dalia","Chest",new Vector3(8.25f+i*.7f,-6.05f),.45f,Color.white);
            VillageUpgrade(village,"rolo","Chest",new Vector3(6.95f,-3.4f),.65f,Color.white);
            VillageUpgrade(village,"guard","Fence",new Vector3(9.7f,2.3f),.85f,Color.white);

            VillageProp(village,"Tree",new Vector3(-8,3.9f),1.65f,true);
            VillageProp(village,"Tree",new Vector3(8,3.9f),1.65f,true);
            VillageProp(village,"Tree",new Vector3(-1.5f,-6.4f),1.4f,true);
        }
        GameObject VillageProp(Transform parent,string key,Vector3 pos,float width,bool solid)
        {var go=Prop(key,pos,width,solid);go.transform.SetParent(parent,true);return go;}
        GameObject VillageItem(Transform parent,string key,Vector3 pos,string id,string label,float width,string title)
        {var item=Item(key,pos,id,label,width);item.transform.SetParent(parent,true);if(!string.IsNullOrEmpty(title)){var t=Label(title,pos+Vector3.up*(width+.45f),.06f);t.transform.SetParent(parent,true);}return item.gameObject;}
        GameObject VillageNpc(Transform parent,Vector3 pos,string id,string label,float width,string title,Color tint)
        {
            var go=VillageItem(parent,"Josh",pos,id,label,width,title);go.name=VillageResidents.Name(id);Tint(go,tint);
            go.AddComponent<VillageNpcRoutine>().Configure(campaign,id,go.GetComponentInChildren<SpriteRenderer>(),npcArt);
            return go;
        }
        GameObject VillageRuin(Transform parent,string id,string key,Vector3 pos,float width,Color tint)
        {var go=VillageProp(parent,key,pos,width,false);Tint(go,tint);villageDamage.Add(new VillageUpgradeArt{Id=id,Root=go});return go;}
        GameObject VillageUpgrade(Transform parent,string id,string key,Vector3 pos,float width,Color tint)
        {var go=VillageProp(parent,key,pos,width,false);Tint(go,tint);go.SetActive(false);villageUpgrades.Add(new VillageUpgradeArt{Id=id,Root=go});return go;}
        void Tint(GameObject go,Color color)
        {if(go==null)return;foreach(var sr in go.GetComponentsInChildren<SpriteRenderer>(true))sr.color=color;}
        bool VillageUpgradeVisible(string id) => id=="mara" ? campaign.Data.maraPantryStocked :
            id=="nico" ? campaign.Data.nicoWorkshopRepaired :
            id=="dalia" ? campaign.Data.daliaGardenRestored :
            id=="rolo" ? campaign.Data.roloMarketOpened :
            id=="guard" && campaign.Data.guardPostBuilt;
        GameObject VillageBuilding(Transform parent,string id)
        {
            var lot=VillageLayout.GetLot(id);
            float width=VillageLayout.HouseWidth;
            var go=new GameObject(lot.Name);go.transform.SetParent(parent,false);go.transform.position=lot.Position;
            var art=new GameObject("Sprite");art.transform.SetParent(go.transform,false);
            var sr=art.AddComponent<SpriteRenderer>();sr.sprite=HouseSprites.Facade(lot.InitialFacade)??Art("House");
            if(sr.sprite!=null)art.transform.localScale=Vector3.one*(width/sr.sprite.bounds.size.x);
            go.AddComponent<WorldSpriteDepth>().Visual=sr;
            var c=go.AddComponent<BoxCollider2D>();c.size=new Vector2(width*.75f,.58f);c.offset=new Vector2(0,.34f);
            var service=go.AddComponent<VillageBuildingService>();service.LotId=id;service.Campaign=campaign;
            return go;
        }
        static void SetSprite(GameObject root,Sprite sprite,float width)
        {
            if(root==null||sprite==null)return;
            var renderer=root.GetComponentInChildren<SpriteRenderer>(true);
            if(renderer==null)return;
            renderer.sprite=sprite;
            renderer.transform.localScale=Vector3.one*(width/sprite.bounds.size.x);
        }
        void RefreshBuilding(GameObject root,string id)
        {
            var lot=VillageLayout.GetLot(id);
            bool restored=VillageUpgradeVisible(id);
            SetSprite(root,HouseSprites.Facade(restored?lot.RestoredFacade:lot.InitialFacade),VillageLayout.HouseWidth);
            Tint(root,restored?Color.white:new Color(.7f,.65f,.6f));
        }
        public void Build(ValleyCampaign owner)
        {
            campaign=owner;
            BuildStartingVillage(owner);
            if (Core.PortfolioSession.Active) { Refresh(); return; }
            Item("Chest",VillageLayout.Note,"note","Nota del valle",.65f);
            VillageItem(transform,"Sign",VillageLayout.CampExit,"go:1","Camino al campamento",.7f,null);
            Item("Gem",new Vector3(-1.4f,-3.1f),"plant","Devolver reliquia al altar",.7f);
            chicken=Prop("Chicken",new Vector3(-9.6f,-5.6f),.65f);
            for(int i=0;i<5;i++)restoredArt.Add(Prop("Gem",new Vector3(-10.7f+i*.6f,-6.3f),.45f));
            for(int zone=1;zone<=5;zone++)
            {
                var p=Center(zone);Ground("Grass",p,32,20);
                Wall(p+new Vector3(-14,0),new Vector2(1,18));Wall(p+new Vector3(14,0),new Vector2(1,18));Wall(p+new Vector3(0,9),new Vector2(28,1));Wall(p+new Vector3(0,-9),new Vector2(28,1));
                for(int j=0;j<12;j++){Prop("Tree",p+new Vector3(-12+j*2,6.4f),1.9f,true);if(j!=1&&j!=10)Prop("Tree",p+new Vector3(-12+j*2,-8),1.9f,true);}
                if(zone!=5)Label((zone+1)+" · "+ValleyCampaign.Titles[zone],p+new Vector3(0,6),.13f);
            }
            var a=Center(1);
            Prop("House",a+new Vector3(-8,1),3);Item("Josh",a+new Vector3(-6,-1),"trade","Exploradora / trueque",.9f);
            Item("Workbench",a+new Vector3(-3,0),"bench","Reparar banco",1.2f);
            cargo=Item("Chest",a+new Vector3(8,3),"cargo","Cargamento perdido",.9f).gameObject;
            Item("Chicken",a+new Vector3(7,-6),"chicken","Rescatar gallina",.7f);
            for(int j=0;j<5;j++)Item("Tree",a+new Vector3(-1+j*2,3),"wood:"+j,"Madera",1.3f);
            Enemy(1,new Vector2(3,-2),false,false);Enemy(1,new Vector2(6,-1),false,false);
            Label("Rodea el claro por el norte",a+new Vector3(6,5),.1f);
            Exit(1,0,new Vector3(-11,-4),"Volver a la granja");Exit(1,2,new Vector3(11,-4),"Cantera →");
            a=Center(2);
            for(int j=0;j<8;j++)Item("Rock",a+new Vector3(-8+(j%4)*3,1+(j/4)*3),"stone:"+j,"Extraer piedra",1.1f);
            seal=Item("Rock",a+new Vector3(6,2),"seal","Derrumbe / primer sello",2).gameObject;
            Prop("Gem",a+new Vector3(6,2.7f),.45f);
            Item("Rock",a+new Vector3(10,5),"secret","Veta escondida",.7f);
            Exit(2,1,new Vector3(-11,-4),"Campamento");Exit(2,3,new Vector3(11,-4),"Bosque →");Exit(2,0,new Vector3(0,-7),"Atajo a casa");
            a=Center(3);
            Prop("Campfire",a+new Vector3(-9,-2),.8f);
            Enemy(3,new Vector2(-4,-3),false,false);Enemy(3,new Vector2(1,-2),false,false);Enemy(3,new Vector2(3,-1),false,false);Enemy(3,new Vector2(8,2),true,false);
            for(int j=0;j<4;j++)Prop("Tree",a+new Vector3(-5+j*3,3),1.6f,true);
            deer=Prop("Deer",a+new Vector3(4,4),1);
            Exit(3,2,new Vector3(-11,-4),"Cantera");Exit(3,4,new Vector3(11,-4),"Santuario →");
            a=Center(4);Ground("Floor",a+new Vector3(0,2),6,4);
            Item("Portal",a+new Vector3(0,1),"portal","Portal de las raíces",3);
            Prop("Gem",a+new Vector3(-3,1),.7f);Prop("Gem",a+new Vector3(3,1),.7f);
            Prop("Well",a+new Vector3(8,2),1.3f);Prop("Campfire",a+new Vector3(-9,-2),.8f);
            Exit(4,3,new Vector3(11,-4),"Volver al bosque");Exit(4,0,new Vector3(-11,-4),"Atajo a casa");
            a=Center(5);Enemy(5,new Vector2(1,1),false,true);
            Prop("Rock",a+new Vector3(-4,-1),1.3f,true);Prop("Rock",a+new Vector3(5,-1),1.3f,true);
            Item("Chest",a+new Vector3(0,5),"seed","Corazón del Valle",1);
            Item("Portal",a+new Vector3(-11,-4),"go:4","Regresar al santuario",1.6f);
            gameObject.AddComponent<ValleyTerrain>().Build(this);
            Refresh();
        }
        void Enemy(int zone,Vector2 local,bool guardian,bool boss)
        {
            var go=Prop(boss||guardian?"Boss":"Slime",Center(zone)+(Vector3)local,boss?2.2f:guardian?1.2f:.8f);
            var col=go.AddComponent<CircleCollider2D>();col.radius=boss?0.8f:0.38f;col.isTrigger=true;
            var e=go.AddComponent<ValleyEnemy>();e.Configure(campaign,this,zone,guardian,boss);enemies.Add(e);
        }
        public void ResetEncounter(){foreach(var e in enemies)e.ResetEncounter();}
        public void Refresh()
        {
            campaign.SyncVillageProgress();
            foreach(var damage in villageDamage)if(damage.Root!=null)damage.Root.SetActive(!VillageUpgradeVisible(damage.Id));
            foreach(var art in villageUpgrades)if(art.Root!=null)art.Root.SetActive(VillageUpgradeVisible(art.Id));
            RefreshBuilding(maraHouse,"mara");
            RefreshBuilding(millHouse,"dalia");
            RefreshBuilding(warehouseHouse,"nico");
            RefreshBuilding(shopHouse,"rolo");
            if(workshop!=null)workshop.Label=campaign.WorkshopRestored?"Nico - herrajes: 4 madera + 6 piedra":"Reparar taller: 12 madera + 8 piedra";
            if(chicken!=null)chicken.SetActive(campaign.Data.chicken);
            if(deer!=null)deer.SetActive(campaign.Data.guardian);
            if(seal!=null)seal.GetComponentInChildren<SpriteRenderer>().color=campaign.Data.sealStone?new Color(1,1,1,.3f):Color.white;
            if(cargo!=null)cargo.GetComponentInChildren<SpriteRenderer>().color=campaign.Data.cargo?new Color(1,1,1,.4f):Color.white;
            foreach(var go in restoredArt)go.SetActive(campaign.Data.restored);
        }
        void OnDestroy(){foreach(var s in sprites.Values)if(s!=null)Destroy(s);}
    }
}
