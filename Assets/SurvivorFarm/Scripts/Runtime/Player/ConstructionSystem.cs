using System;
using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
using UnityEngine.EventSystems;
namespace SurvivorFarm.Runtime.Player
{
    [Serializable] public sealed class BuildingData
    {
        public bool indoors;public string kind;public float x,y;public int wood,stone,food,iron;
        public int rotation, health = -1;
        public int wallVersion; // 0 preserves the footprint of pre-fortress saves.
    }
    public sealed class ConstructionSystem : MonoBehaviour
    {
        public static bool IsPlacing {get;private set;}
        public List<BuildingData> Buildings=new List<BuildingData>();
        readonly List<GameObject> visuals=new List<GameObject>();
        BuildingData moving;
        int rotation;
        Transform world;SpriteRenderer preview;string selected;PlayerInventory inventory;
        public string PlacementHint {get;private set;}
        public string SelectedKind => selected;
        public int Rotation => rotation;
        public bool IsMoving => moving != null;
        public event Action SelectionChanged;
        void Awake(){inventory=GetComponent<PlayerInventory>();}
        void Start(){EnsureWorld();CreateVeins();}
        void EnsureWorld(){if(world==null){world=FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="Outdoor World");}}
        public static string Label(string kind)=>kind switch{"Campfire"=>"Fogata","Fence"=>"Empalizada","StoneWall"=>"Muralla de piedra","ReinforcedWall"=>"Muro reforzado","Trap"=>"Trampa","Turret"=>"Ballesta","Chest"=>"Cofre","Workbench"=>"Banco de trabajo","Beacon"=>"Baliza","Bed"=>"Cama","Cabinet"=>"Armario","Furnace"=>"Horno",_=>kind};
        public static void Cost(string kind,out int wood,out int stone,out int iron)
        {wood=kind=="Fence"?2:kind=="Chest"?8:kind=="Workbench"?12:kind=="Beacon"?12:6;stone=kind=="Fence"?0:kind=="Chest"?2:kind=="Workbench"?8:kind=="Beacon"?8:4;iron=kind=="Beacon"?10:0;if(kind=="Bed"){wood=10;stone=4;}if(kind=="Cabinet"){wood=16;stone=4;}if(kind=="Furnace"){wood=8;stone=20;iron=3;}if(kind=="Trap"){wood=3;stone=3;iron=0;}if(kind=="Turret"){wood=10;stone=6;iron=3;}if(kind=="StoneWall"){wood=2;stone=6;iron=0;}if(kind=="ReinforcedWall"){wood=2;stone=8;iron=2;}}
        public static Sprite SpriteFor(string kind)=>FortressPieces.IsWall(kind)?FortressPieces.Art(kind):HouseSprites.Furniture(kind)??Resources.Load<Sprite>("BackpackIcons/"+(kind=="Beacon"?"Gem":kind=="Trap"?"Fence":kind=="Turret"?"Bow":kind));
        public bool Pack(string kind)
        {
            if(!BackpackActions.IsBuilding(kind))return false;
            if (PortfolioSession.Active && !PortfolioSession.Instance.CanBuild(kind)) return false;
            if(inventory==null)inventory=GetComponent<PlayerInventory>();
            if(inventory==null)return false;
            var crafting=GetComponent<PlayerCraftingController>();
            if(kind=="Campfire"&&((crafting!=null&&crafting.CampfireBuilt)||inventory.PackedCount("Campfire")>0))return false;
            if(kind=="Bed"&&((crafting!=null&&crafting.BedBuilt)||inventory.PackedCount("Bed")>0))return false;
            if((kind=="Cabinet"||kind=="Furnace")&&GetComponent<HouseSystem>()?.Requirement(kind)!="")return false;
            Cost(kind,out int wood,out int stone,out int iron);var progress=GetComponent<AdventureProgress>()??gameObject.AddComponent<AdventureProgress>();
            if(!CanAfford(kind))return false;
            Pay(kind);
            inventory.AddPacked(kind);FarmNotificationCenter.Show(Label(kind)+" guardada en la mochila [I]. Pulsa Colocar para elegir dónde usarla.");return true;
        }
        public bool BeginPacked(string kind)
        {if(inventory.PackedCount(kind)<1)return false;Begin(kind);return IsPlacing&&selected==kind;}
        public void Begin(string kind)
        {
            if(!BackpackActions.IsBuilding(kind))return;
            if (PortfolioSession.Active && !PortfolioSession.Instance.CanBuild(kind)) { FarmNotificationCenter.Show("Esta defensa se desbloquea al amanecer del día 2."); return; }
            Cancel();selected=kind;rotation=0;IsPlacing=true;
            var root=new GameObject("Vista previa de construcción");preview=root.AddComponent<SpriteRenderer>();preview.sprite=SpriteFor(kind);preview.sortingOrder=20000;Size(preview,kind);
            RefreshPreview();
            if(PortfolioSession.Active)(GetComponent<ConstructionPalette>()??gameObject.AddComponent<ConstructionPalette>()).Configure(this,inventory);
            SelectionChanged?.Invoke();
            GetComponent<PlayerMovementController>()?.StopMovement();FarmNotificationCenter.Show("Clic: colocar varias · R: girar · Clic derecho / Esc: terminar.");
        }
        static void Size(SpriteRenderer sr,string kind){if(sr.sprite!=null)sr.transform.localScale=Vector3.one*((kind=="Bed"||kind=="Cabinet"?1.6f:kind=="Furnace"?1.25f:kind=="Fence"?1f:.85f)/Mathf.Max(sr.sprite.bounds.size.x,sr.sprite.bounds.size.y));}
        public void Cancel(){moving=null;IsPlacing=false;selected=null;if(preview!=null)Destroy(preview.gameObject);preview=null;SelectionChanged?.Invoke();}
        private BuildingData UpgradeAt(string kind,Vector2 point)=>moving!=null||!FortressPieces.IsWall(kind)?null:
            Buildings.FirstOrDefault(b=>!b.indoors&&b.wallVersion>0&&FortressPieces.IsWall(b.kind)&&FortressPieces.Tier(b.kind)<FortressPieces.Tier(kind)&&
                b.rotation%2==rotation%2&&Vector2.Distance(point,new Vector2(b.x,b.y))<.01f);
        public void Rotate() { rotation=1-rotation;RefreshPreview();SelectionChanged?.Invoke(); }
        void RefreshPreview()
        {
            if(preview==null)return;
            if(FortressPieces.IsWall(selected))FortressPieces.Style(preview,selected,rotation);
        }
        public int AvailablePlacements(string kind)
        {
            if(inventory==null)return 0;
            Cost(kind,out var wood,out var stone,out var iron);
            int gold=FortressPieces.GoldCost(kind), count=int.MaxValue;
            if(wood>0)count=Mathf.Min(count,inventory.Wood/wood);
            if(stone>0)count=Mathf.Min(count,inventory.Stone/stone);
            if(iron>0)count=Mathf.Min(count,(GetComponent<AdventureProgress>()?.Data.iron??0)/iron);
            if(gold>0)count=Mathf.Min(count,inventory.GetAvailableItemCount("GoldOre")/gold);
            return (int)Math.Min(int.MaxValue,(long)inventory.PackedCount(kind)+(count==int.MaxValue?0:count));
        }
        public bool CanAfford(string kind)
        {
            Cost(kind,out var wood,out var stone,out var iron);
            return inventory!=null&&inventory.Wood>=wood&&inventory.Stone>=stone&&
                (GetComponent<AdventureProgress>()?.Data.iron??0)>=iron&&inventory.GetAvailableItemCount("GoldOre")>=FortressPieces.GoldCost(kind);
        }
        void Pay(string kind)
        {
            Cost(kind,out var wood,out var stone,out var iron);
            if(wood>0)inventory.TryRemoveWood(wood);if(stone>0)inventory.TryRemoveStone(stone);
            if(iron>0)GetComponent<AdventureProgress>().SpendIron(iron);
            if(FortressPieces.GoldCost(kind)>0)inventory.TryRemoveItem("GoldOre",FortressPieces.GoldCost(kind));
        }
        public bool CanPlace(string kind,Vector2 p,out string reason)
        {
            EnsureWorld();reason="";
            if(!BackpackActions.IsBuilding(kind)){reason="Pieza desconocida.";return false;}
            if (PortfolioSession.Active && !PortfolioSession.Instance.CanBuild(kind)) { reason="Defensa no disponible todavía."; return false; }
            var house=GetComponent<HouseSystem>();bool indoors=house!=null&&house.IsInside;
            if(!indoors&&(world==null||!world.gameObject.activeInHierarchy)){reason="Construye en tu terreno.";return false;}
            if(indoors && (!house.Contains(p)||Mathf.Abs(p.x)<.8f&&p.y<HouseSystem.Center.y-house.Size.y/2+2)){reason="Deja libre la puerta y las paredes.";return false;}
            if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||!indoors&&(Mathf.Abs(p.x)>35||Mathf.Abs(p.y)>20)){reason="Fuera del terreno.";return false;}
            var footprint=FortressPieces.Footprint(kind,rotation,moving==null||moving.wallVersion>0);
            var upgrade=UpgradeAt(kind,p);
            if(!indoors&&PortfolioSession.Active&&!SurvivorFarm.Runtime.World.FarmExploration.Contains(p,1.2f)){reason="Deja libre el borde del bosque.";return false;}
            if(Vector2.Distance(transform.position,p)<.85f){reason="Deja espacio para el personaje.";return false;}
            if(Vector2.Distance(transform.position,p)>(PortfolioSession.Active?7:5)){reason="Acércate para seguir construyendo.";return false;}
            if(!indoors&&!CultivationGrid.IsGreen(p) && !(PortfolioSession.Active && FarmDefense.IsDefense(kind))){reason="Deja libres los caminos principales.";return false;}
            if(!indoors&&GetComponent<SurvivorFarm.Runtime.World.EnemyCampWorld>()?.ContainsCamp(p,.6f)==true){reason="Deja libre el campamento y sus accesos.";return false;}
            foreach(var c in Physics2D.OverlapBoxAll(p,footprint,0))
            {
                if(moving!=null&&c.GetComponent<PlacedBuilding>()?.Data==moving)continue;
                var placed=c.GetComponent<PlacedBuilding>();
                if(placed!=null)
                {
                    var b=placed.Data;
                    if(b==upgrade)continue;
                    if(!FortressPieces.Overlaps(kind,p,rotation,moving==null||moving.wallVersion>0,b.kind,new Vector2(b.x,b.y),b.rotation,b.wallVersion>0))continue;
                }
                if(!c.isTrigger||c.GetComponentInParent<WorldInteractable>()!=null)
                {
                    var plot=c.GetComponentInParent<FarmingPlot>();
                    if(plot!=null&&plot.StateId==0)continue;
                    reason="Espacio ocupado o parcela bloqueada.";return false;
                }
            }
            if(Buildings.Any(b=>b!=moving&&b!=upgrade&&b.indoors==indoors&&FortressPieces.Overlaps(kind,p,rotation,moving==null||moving.wallVersion>0,b.kind,new Vector2(b.x,b.y),b.rotation,b.wallVersion>0))){reason="Otra construcción ocupa ese módulo.";return false;}
            if(kind=="Beacon"&&(!GetComponent<AdventureProgress>().Data.returnedHome||GetComponent<AdventureProgress>().Data.beacon)){reason="Regresa con la gema del gólem; solo necesitas una baliza.";return false;}
            return true;
        }
        public bool Place(string kind,Vector2 p) => PlaceInternal(kind,p,false);
        public bool PlacePacked(string kind,Vector2 p) => PlaceInternal(kind,p,true);
        public bool PlaceSelected(Vector2 p)
        {
            if(!IsPlacing||selected==null)return false;
            p=FortressPieces.Snap(selected,p,rotation);
            bool wasMoving=moving!=null;
            bool done=PlaceInternal(selected,p,inventory.PackedCount(selected)>0);
            if(done&&wasMoving)Cancel();
            else if(done)SelectionChanged?.Invoke();
            return done;
        }
        bool PlaceInternal(string kind,Vector2 p,bool packed)
        {
            if(!BackpackActions.IsBuilding(kind)||!CanPlace(kind,p,out var reason)){FarmNotificationCenter.Show("No puedes construir en ese lugar.");return false;}
            if(moving!=null){var moved=moving;RemoveDestroyed(moved);moved.x=p.x;moved.y=p.y;moved.rotation=rotation;moved.indoors=GetComponent<HouseSystem>()?.IsInside==true;Buildings.Add(moved);CreateVisual(moved);FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);return true;}
            Cost(kind,out var wood,out var stone,out var iron);var progress=GetComponent<AdventureProgress>();
            if(!packed&&!CanAfford(kind)){FarmNotificationCenter.Show("Faltan materiales: consulta los iconos de la paleta.");return false;}
            if(packed){if(!inventory.RemovePacked(kind))return false;}
            else Pay(kind);
            var upgrade=UpgradeAt(kind,p);
            if(upgrade!=null)
            {
                float fraction=upgrade.health/(float)FortressPieces.Health(upgrade.kind);
                RemoveDestroyed(upgrade);upgrade.kind=kind;upgrade.health=Mathf.Max(1,Mathf.RoundToInt(FortressPieces.Health(kind)*Mathf.Clamp01(fraction)));
                Buildings.Add(upgrade);CreateVisual(upgrade);
                GetComponent<GameFeelFeedback>()?.Pulse("Muralla mejorada",p,false,true);
                FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);return true;
            }
            var data=new BuildingData{kind=kind,x=p.x,y=p.y,rotation=rotation,wallVersion=1,indoors=GetComponent<HouseSystem>()?.IsInside==true};Buildings.Add(data);CreateVisual(data);
            if (PortfolioSession.Active && FarmDefense.IsDefense(kind)) PortfolioSession.Instance.RegisterDefense();
            if(kind=="Campfire")GetComponent<PlayerCraftingController>().RegisterPlacedFire();
            if(kind=="Bed")GetComponent<PlayerCraftingController>().RegisterPlacedBed();
            if(kind=="Beacon")progress.Data.beacon=true;
            GetComponent<GameFeelFeedback>()?.Pulse(Label(kind)+" lista",p,false,true);
            FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);return true;
        }
        void CreateVisual(BuildingData data)
        {
            EnsureWorld();var root=new GameObject(Label(data.kind));root.transform.SetParent(data.indoors?GetComponent<HouseSystem>().RoomRoot:world,false);root.transform.position=new Vector3(data.x,data.y);
            var art=new GameObject("Arte original");art.transform.SetParent(root.transform,false);var sr=art.AddComponent<SpriteRenderer>();sr.sprite=SpriteFor(data.kind);root.AddComponent<SurvivorFarm.Runtime.World.WorldSpriteDepth>().Visual=sr;Size(sr,data.kind);
            var collider=root.AddComponent<BoxCollider2D>();collider.size=FortressPieces.Footprint(data.kind,data.rotation,data.wallVersion>0);
            var item=root.AddComponent<PlacedBuilding>();item.Data=data;item.Owner=this;visuals.Add(root);
            if (FortressPieces.IsWall(data.kind)&&data.wallVersion>0)FortressPieces.Style(sr,data.kind,data.rotation);
            else if(data.kind=="Fence")sr.transform.rotation=Quaternion.Euler(0,0,data.rotation*90);
            if (PortfolioSession.Active && FarmDefense.IsDefense(data.kind))
                root.AddComponent<FarmDefense>().Configure(data, this, inventory);
        }
        public void AddAuthoredDefense(string kind, Vector2 position, int health = -1)
        {
            var data = new BuildingData { kind=kind, x=position.x, y=position.y, health=health,wallVersion=1 };
            Buildings.Add(data); CreateVisual(data);
        }
        public void RemoveDestroyed(BuildingData data)
        {
            var root=visuals.FirstOrDefault(v=>v!=null&&v.GetComponent<PlacedBuilding>()?.Data==data);
            Buildings.Remove(data);
            if(root!=null){visuals.Remove(root);root.SetActive(false);Destroy(root);}
        }
        public bool CanAccess(BuildingData data) => data!=null && Buildings.Contains(data) && (data.indoors ? GetComponent<HouseSystem>()?.CanUseServices==true : Vector2.Distance(transform.position,new Vector2(data.x,data.y))<=2);
        public bool BeginMove(BuildingData data)
        {if(!CanAccess(data))return false;Begin(data.kind);if(selected!=data.kind)return false;moving=data;rotation=data.rotation;RefreshPreview();return true;}
        public bool Store(BuildingData data)
        {
            if(!CanAccess(data))return false;
            if(data.wood+data.stone+data.food+data.iron>0){FarmNotificationCenter.Show("Vacía el almacenamiento antes de guardarlo; puedes moverlo con su contenido.");return false;}
            var root=visuals.FirstOrDefault(v=>v!=null&&v.GetComponent<PlacedBuilding>()?.Data==data);if(root!=null){visuals.Remove(root);root.SetActive(false);Destroy(root);}Buildings.Remove(data);inventory.AddPacked(data.kind);GetComponent<PlayerCraftingController>().RefreshPlacedUtilities();FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);return true;
        }
        public bool Near(string kind)=>Buildings.Any(b=>b.kind==kind&&CanAccess(b));
        public void Restore(List<BuildingData> data)
        {
            Cancel();foreach(var root in visuals)if(root!=null){root.SetActive(false);Destroy(root);}visuals.Clear();
            Buildings=data??new List<BuildingData>();foreach(var b in Buildings)CreateVisual(b);
        }
        public bool Transfer(BuildingData chest,string resource,bool deposit)
        {
            if(chest==null||(chest.kind!="Chest"&&chest.kind!="Cabinet")||!CanAccess(chest))return false;
            var progress=GetComponent<AdventureProgress>();
            int count=resource=="Wood"?(deposit?inventory.Wood:chest.wood):resource=="Stone"?(deposit?inventory.Stone:chest.stone):resource=="Food"?(deposit?inventory.Food:chest.food):(deposit?progress.Data.iron:chest.iron);
            int amount=Mathf.Min(10,count);if(amount<=0)return false;
            if(resource=="Wood"){if(deposit){inventory.TryRemoveWood(amount);chest.wood+=amount;}else{inventory.AddWood(amount);chest.wood-=amount;}}
            else if(resource=="Stone"){if(deposit){inventory.TryRemoveStone(amount);chest.stone+=amount;}else{inventory.AddStone(amount);chest.stone-=amount;}}
            else if(resource=="Food"){if(deposit){inventory.TryRemoveFood(amount);chest.food+=amount;}else{inventory.AddFood(amount);chest.food-=amount;}}
            else if(resource=="Iron"){if(deposit){progress.SpendIron(amount);chest.iron+=amount;}else{progress.AddIron(amount);chest.iron-=amount;}}
            else return false;
            return true;
        }
        public bool TemperSword()
        {
            var progress=GetComponent<AdventureProgress>();
            if(!Near("Workbench")||progress.Data.temperedBlade||!progress.Data.returnedHome||progress.Data.iron<6){FarmNotificationCenter.Show("Necesitas regresar de las ruinas y 6 hierro; acércate a tu banco de trabajo.");return false;}
            progress.SpendIron(6);progress.Data.temperedBlade=true;FarmNotificationCenter.Show("Espada reforjada: +2 daño permanente.");GetComponent<GameFeelFeedback>()?.Pulse("Espada +2",transform.position,false,true);return true;
        }
        void CreateVeins()
        {
            if(world==null||PortfolioSession.Active)return;
            for(int i=0;i<4;i++)
            {
                var root=new GameObject("Veta de hierro "+i);root.transform.SetParent(world,false);root.transform.position=new Vector3(21+i*1.5f,4);
                var sr=root.AddComponent<SpriteRenderer>();sr.sprite=SpriteFor("Iron");root.AddComponent<SurvivorFarm.Runtime.World.WorldSpriteDepth>().Visual=sr;Size(sr,"Iron");
                var collider=root.AddComponent<CircleCollider2D>();collider.radius=.35f;
                var vein=root.AddComponent<IronVein>();vein.Index=i;
            }
        }
        void Update()
        {
            if(!IsPlacing)return;
            if(GetComponent<PlayerSurvivalStats>()?.CurrentHealth<=0||Input.GetKeyDown(KeyCode.Escape)||Input.GetMouseButtonDown(1)){Cancel();return;}
            if(Time.timeScale<=0)return;
            if(Camera.main==null)return;
            if(Input.GetKeyDown(KeyCode.R))Rotate();
            Vector2 p=FortressPieces.Snap(selected,Camera.main.ScreenToWorldPoint(Input.mousePosition),rotation);
            bool valid=CanPlace(selected,p,out var reason);PlacementHint=valid?(UpgradeAt(selected,p)!=null?"Clic: mejorar este muro (coste de pieza completa)":"Clic: colocar "+Label(selected)):reason;
            if(valid&&moving==null&&AvailablePlacements(selected)==0)
            {
                valid=false;reason="Sin materiales para otra pieza · elige otra o sal a recolectar";PlacementHint=reason;
            }
            FarmNotificationCenter.SetPrompt(PlacementHint);
            preview.transform.position=(Vector3)p+(FortressPieces.IsWall(selected)?FortressPieces.VisualOffset(selected,rotation):Vector3.zero);preview.color=valid?new Color(.5f,1,.5f,.65f):new Color(1,.35f,.35f,.65f);
            preview.enabled=EventSystem.current==null||!EventSystem.current.IsPointerOverGameObject();
            if(Input.GetMouseButtonDown(0)&&preview.enabled){if(!valid)FarmNotificationCenter.Show(reason);else PlaceSelected(p);}
        }
        void OnDisable()=>Cancel();
        void OnDestroy(){foreach(var root in visuals)if(root!=null)Destroy(root);}
    }
    public sealed class PlacedBuilding : WorldInteractable
    {
        public BuildingData Data;public ConstructionSystem Owner;
        public override bool IsAvailable => GetComponent<FarmDefense>() == null;
        public override string GetInteractionLabel(FarmTool selectedTool)=>Data.indoors?ConstructionSystem.Label(Data.kind)+" · usar o mover":Data.kind=="Workbench"?"Banco: reforjar espada (+2 daño, 6 hierro)":Data.kind=="Chest"?"Abrir cofre":Data.kind=="Campfire"?"Fogata: abrir recetas":"Construcción: "+ConstructionSystem.Label(Data.kind);
        public override void Interact(FarmTool tool,PlayerInventory inventory)
        {
            if(Data.indoors){FindFirstObjectByType<AdventureWindow>()?.OpenFurniture(Data);return;}
            if(Data.kind=="Workbench")Owner.TemperSword();
            else if(Data.kind=="Chest")FindFirstObjectByType<AdventureWindow>()?.OpenChest(Data);
            else if(Data.kind=="Campfire")FindFirstObjectByType<CraftingWindow>()?.Open();
        }
    }
}
