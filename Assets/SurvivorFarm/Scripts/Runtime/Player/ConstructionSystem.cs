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
    }
    public sealed class ConstructionSystem : MonoBehaviour
    {
        public static bool IsPlacing {get;private set;}
        public List<BuildingData> Buildings=new List<BuildingData>();
        readonly List<GameObject> visuals=new List<GameObject>();
        bool fromBackpack;BuildingData moving;
        int rotation;
        Transform world;SpriteRenderer preview;string selected;PlayerInventory inventory;
        public string PlacementHint {get;private set;}
        void Awake(){inventory=GetComponent<PlayerInventory>();}
        void Start(){EnsureWorld();CreateVeins();}
        void EnsureWorld(){if(world==null){world=FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="Outdoor World");}}
        public static string Label(string kind)=>kind switch{"Campfire"=>"Fogata","Fence"=>"Barricada","Trap"=>"Trampa","Turret"=>"Ballesta automática","Chest"=>"Cofre","Workbench"=>"Banco de trabajo","Beacon"=>"Baliza","Bed"=>"Cama","Cabinet"=>"Armario","Furnace"=>"Horno",_=>kind};
        public static void Cost(string kind,out int wood,out int stone,out int iron)
        {wood=kind=="Fence"?2:kind=="Chest"?8:kind=="Workbench"?12:kind=="Beacon"?12:6;stone=kind=="Fence"?0:kind=="Chest"?2:kind=="Workbench"?8:kind=="Beacon"?8:4;iron=kind=="Beacon"?10:0;if(kind=="Bed"){wood=10;stone=4;}if(kind=="Cabinet"){wood=16;stone=4;}if(kind=="Furnace"){wood=8;stone=20;iron=3;}if(kind=="Trap"){wood=3;stone=3;iron=0;}if(kind=="Turret"){wood=10;stone=6;iron=3;}}
        public static Sprite SpriteFor(string kind)=>HouseSprites.Furniture(kind)??Resources.Load<Sprite>("BackpackIcons/"+(kind=="Beacon"?"Gem":kind=="Trap"?"Fence":kind=="Turret"?"Bow":kind));
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
            if(inventory.Wood<wood||inventory.Stone<stone||progress.Data.iron<iron)return false;
            inventory.TryRemoveWood(wood);if(stone>0)inventory.TryRemoveStone(stone);if(iron>0)progress.SpendIron(iron);
            inventory.AddPacked(kind);FarmNotificationCenter.Show(Label(kind)+" guardada en la mochila [I]. Pulsa Colocar para elegir dónde usarla.");return true;
        }
        public bool BeginPacked(string kind)
        {if(inventory.PackedCount(kind)<1)return false;Begin(kind);fromBackpack=true;return IsPlacing;}
        public void Begin(string kind)
        {
            if(!BackpackActions.IsBuilding(kind))return;
            if (PortfolioSession.Active && !PortfolioSession.Instance.CanBuild(kind)) { FarmNotificationCenter.Show("Esta defensa se desbloquea al amanecer del día 2."); return; }
            Cancel();selected=kind;rotation=0;IsPlacing=true;
            var root=new GameObject("Vista previa de construcción");preview=root.AddComponent<SpriteRenderer>();preview.sprite=SpriteFor(kind);preview.sortingOrder=20000;Size(preview,kind);
            GetComponent<PlayerMovementController>()?.StopMovement();FarmNotificationCenter.Show("Clic: colocar · R: girar · Clic derecho / Esc: cancelar.");
        }
        static void Size(SpriteRenderer sr,string kind){if(sr.sprite!=null)sr.transform.localScale=Vector3.one*((kind=="Bed"||kind=="Cabinet"?1.6f:kind=="Furnace"?1.25f:kind=="Fence"?1f:.85f)/Mathf.Max(sr.sprite.bounds.size.x,sr.sprite.bounds.size.y));}
        public void Cancel(){moving=null;fromBackpack=false;IsPlacing=false;selected=null;if(preview!=null)Destroy(preview.gameObject);preview=null;}
        public bool CanPlace(string kind,Vector2 p,out string reason)
        {
            EnsureWorld();reason="";
            if (PortfolioSession.Active && !PortfolioSession.Instance.CanBuild(kind)) { reason="Defensa no disponible todavía."; return false; }
            var house=GetComponent<HouseSystem>();bool indoors=house!=null&&house.IsInside;
            if(!indoors&&(world==null||!world.gameObject.activeInHierarchy)){reason="Construye en tu terreno.";return false;}
            if(indoors && (!house.Contains(p)||Mathf.Abs(p.x)<.8f&&p.y<HouseSystem.Center.y-house.Size.y/2+2)){reason="Deja libre la puerta y las paredes.";return false;}
            if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||!indoors&&(Mathf.Abs(p.x)>35||Mathf.Abs(p.y)>20)){reason="Fuera del terreno.";return false;}
            if(Vector2.Distance(transform.position,p)<.85f){reason="Deja espacio para el personaje.";return false;}
            if(Vector2.Distance(transform.position,p)>5){reason="Acércate: alcance máximo 5 metros.";return false;}
            if(!indoors&&!CultivationGrid.IsGreen(p) && !(PortfolioSession.Active && FarmDefense.IsDefense(kind))){reason="Deja libres los caminos principales.";return false;}
            if(!indoors&&GetComponent<SurvivorFarm.Runtime.World.EnemyCampWorld>()?.ContainsCamp(p,.6f)==true){reason="Deja libre el campamento y sus accesos.";return false;}
            foreach(var c in Physics2D.OverlapBoxAll(p,kind=="Fence"?new Vector2(.99f,.32f):new Vector2(.95f,.85f),kind=="Fence"?rotation*90:0))
            {
                if(moving!=null&&c.GetComponent<PlacedBuilding>()?.Data==moving)continue;
                if(!c.isTrigger||c.GetComponentInParent<WorldInteractable>()!=null)
                {
                    var plot=c.GetComponentInParent<FarmingPlot>();
                    if(plot!=null&&plot.StateId==0)continue;
                    reason="Espacio ocupado o parcela bloqueada.";return false;
                }
            }
            if(Buildings.Any(b=>b!=moving&&Vector2.Distance(p,new Vector2(b.x,b.y))<1)){reason="Otra construcción está demasiado cerca.";return false;}
            if(kind=="Beacon"&&(!GetComponent<AdventureProgress>().Data.returnedHome||GetComponent<AdventureProgress>().Data.beacon)){reason="Regresa con la gema del gólem; solo necesitas una baliza.";return false;}
            return true;
        }
        public bool Place(string kind,Vector2 p) => PlaceInternal(kind,p,false);
        public bool PlacePacked(string kind,Vector2 p) => PlaceInternal(kind,p,true);
        bool PlaceInternal(string kind,Vector2 p,bool packed)
        {
            if(!BackpackActions.IsBuilding(kind)||!CanPlace(kind,p,out var reason)){FarmNotificationCenter.Show("No puedes construir en ese lugar.");return false;}
            if(moving!=null){moving.x=p.x;moving.y=p.y;moving.indoors=false;var root=visuals.FirstOrDefault(v=>v!=null&&v.GetComponent<PlacedBuilding>()?.Data==moving);if(root!=null){root.transform.SetParent(world,true);root.transform.position=p;}FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);return true;}
            Cost(kind,out var wood,out var stone,out var iron);var progress=GetComponent<AdventureProgress>();
            if(!packed&&(inventory.Wood<wood||inventory.Stone<stone||progress.Data.iron<iron)){FarmNotificationCenter.Show($"Necesitas {wood} madera, {stone} piedra y {iron} hierro.");return false;}
            if(packed){if(!inventory.RemovePacked(kind))return false;}
            else {inventory.TryRemoveWood(wood);if(stone>0)inventory.TryRemoveStone(stone);progress.SpendIron(iron);}
            var data=new BuildingData{kind=kind,x=p.x,y=p.y,rotation=rotation,indoors=GetComponent<HouseSystem>()?.IsInside==true};Buildings.Add(data);CreateVisual(data);
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
            var collider=root.AddComponent<BoxCollider2D>();collider.size=data.kind=="Fence"?new Vector2(.95f,.25f):new Vector2(.7f,.55f);
            var item=root.AddComponent<PlacedBuilding>();item.Data=data;item.Owner=this;visuals.Add(root);
            if (data.kind == "Fence") root.transform.rotation = Quaternion.Euler(0,0,data.rotation * 90);
            if (PortfolioSession.Active && FarmDefense.IsDefense(data.kind))
                root.AddComponent<FarmDefense>().Configure(data, this, inventory);
        }
        public void AddAuthoredDefense(string kind, Vector2 position, int health = -1)
        {
            var data = new BuildingData { kind=kind, x=position.x, y=position.y, health=health };
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
        {if(!CanAccess(data))return false;Begin(data.kind);moving=data;return true;}
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
            if(world==null)return;
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
            if(GetComponent<PlayerSurvivalStats>().CurrentHealth<=0||Input.GetKeyDown(KeyCode.Escape)||Input.GetMouseButtonDown(1)){Cancel();return;}
            if(Camera.main==null)return;Vector2 p=Camera.main.ScreenToWorldPoint(Input.mousePosition);p=new Vector2(Mathf.Round(p.x*2)/2,Mathf.Round(p.y*2)/2);
            if (Input.GetKeyDown(KeyCode.R)) { rotation = 1 - rotation; preview.transform.rotation = Quaternion.Euler(0,0,selected=="Fence"?rotation*90:0); }
            bool valid=CanPlace(selected,p,out var reason);PlacementHint=valid?"Clic: colocar "+Label(selected):reason;
            if(valid&&moving==null&&!fromBackpack)
            {
                Cost(selected,out var needWood,out var needStone,out var needIron);
                if(inventory.Wood<needWood||inventory.Stone<needStone||GetComponent<AdventureProgress>().Data.iron<needIron)
                {valid=false;reason=$"Necesitas {needWood} madera, {needStone} piedra, {needIron} hierro";PlacementHint=reason;}
            }
            FarmNotificationCenter.SetPrompt(PlacementHint);
            preview.transform.position=p;preview.color=valid?new Color(.5f,1,.5f,.65f):new Color(1,.35f,.35f,.65f);
            if(Input.GetMouseButtonDown(0)&&(EventSystem.current==null||!EventSystem.current.IsPointerOverGameObject())){if(!valid)FarmNotificationCenter.Show(reason);else if(PlaceInternal(selected,p,fromBackpack))Cancel();}
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
    public sealed class IronVein : WorldInteractable
    {
        public int Index;
        bool mining;PlayerInventory activeInventory;AdventureProgress activeProgress;int activeDay,activePickaxeLevel,completedHits;float mineStartedAt,mineEndsAt,nextHitAt;
        const float MineDuration=2.8f;const int MineHits=5;
        public override string GetInteractionLabel(FarmTool tool)=>mining?"Picando veta "+ProgressText():"Veta rica: hierro, oro y gemas (pico Nv.2, se renueva al día siguiente)";
        public override void Interact(FarmTool tool,PlayerInventory inventory)
        {
            if(mining){FarmNotificationCenter.Show("Picando veta "+ProgressText());return;}
            if(!CanMine(tool,inventory,out var progress,out var day,out var pickaxeLevel))return;
            PlayerCharacterAnimator animator=inventory.GetComponent<PlayerCharacterAnimator>();
            if(animator!=null&&Application.isPlaying){BeginMining(inventory,progress,day,pickaxeLevel,animator);return;}
            animator?.PlayAction("Pickaxe",0,transform.position);
            CompleteMine(inventory,progress,day,pickaxeLevel);
        }
        bool CanMine(FarmTool tool,PlayerInventory inventory,out AdventureProgress progress,out int day,out int pickaxeLevel)
        {
            progress=inventory!=null?inventory.GetComponent<AdventureProgress>():null;var clock=FindFirstObjectByType<DayNightCycle>();day=clock!=null?clock.Day:1;
            pickaxeLevel=inventory!=null&&inventory.GetComponent<PlayerToolUpgradeController>()!=null?inventory.GetComponent<PlayerToolUpgradeController>().PickaxeLevel:0;
            if(progress==null)return false;
            if(tool!=FarmTool.Pickaxe||pickaxeLevel<2){FarmNotificationCenter.Show("Mejora el pico a nivel 2 en el taller F.");return false;}
            if(Index<0||Index>=progress.Data.minedDays.Length){FarmNotificationCenter.Show("Esta veta todavía no está registrada.");return false;}
            if(progress.Data.minedDays[Index]>=day){FarmNotificationCenter.Show("Esta veta se recupera mañana.");return false;}
            return true;
        }
        void BeginMining(PlayerInventory inventory,AdventureProgress progress,int day,int pickaxeLevel,PlayerCharacterAnimator animator)
        {
            activeInventory=inventory;activeProgress=progress;activeDay=day;activePickaxeLevel=pickaxeLevel;completedHits=0;mineStartedAt=Time.time;mineEndsAt=Time.time+MineDuration;nextHitAt=mineStartedAt+MineDuration/(MineHits+1f);mining=true;
            animator.PlayAction("Pickaxe",MineDuration,transform.position);
            FarmNotificationCenter.Show("Picando veta "+ProgressText());
        }
        void Update()
        {
            if(!mining)return;
            var stats=activeInventory!=null?activeInventory.GetComponent<PlayerSurvivalStats>():null;
            if(activeInventory==null||activeProgress==null||stats!=null&&stats.CurrentHealth<=0){ClearMining();return;}
            while(Time.time>=nextHitAt&&completedHits<MineHits)
            {
                completedHits++;VisibleHitFeedback.Play(gameObject,.08f,false);
                activeInventory.GetComponent<AudioFeedback>()?.Play(CombatSound.Mine,transform.position);
                CombatHitParticles.Spawn(transform.position,transform.parent,ImpactSurface.Stone,false,false);
                activeInventory.GetComponent<GameFeelFeedback>()?.Pulse("Golpe "+completedHits+"/"+MineHits,transform.position,false,false,false);
                nextHitAt=mineStartedAt+MineDuration*(completedHits+1f)/(MineHits+1f);
            }
            FarmNotificationCenter.SetPrompt("Picando veta "+ProgressText());
            if(Time.time<mineEndsAt)return;
            var inventory=activeInventory;var progress=activeProgress;int day=activeDay,pickaxeLevel=activePickaxeLevel;ClearMining();
            CompleteMine(inventory,progress,day,pickaxeLevel);
        }
        void CompleteMine(PlayerInventory inventory,AdventureProgress progress,int day,int pickaxeLevel)
        {
            progress.Data.minedDays[Index]=day;progress.AddIron(3);
            string rareDrop=GrantRareDrop(inventory,pickaxeLevel);
            inventory.GetComponent<GameFeelFeedback>()?.Pulse(string.IsNullOrEmpty(rareDrop)?"+3 hierro":"+3 hierro "+rareDrop,transform.position);
            FarmNotificationCenter.Show(string.IsNullOrEmpty(rareDrop)?"+3 hierro. Úsalo para armaduras, armas y construcciones.":"+3 hierro y "+rareDrop+". Material para equipo avanzado.");
        }
        void ClearMining(){mining=false;activeInventory=null;activeProgress=null;activeDay=0;activePickaxeLevel=0;completedHits=0;mineStartedAt=0;mineEndsAt=0;nextHitAt=0;}
        float Progress=>mining?Mathf.Clamp01((Time.time-mineStartedAt)/MineDuration):1f;
        string ProgressText(){int percent=Mathf.RoundToInt(Progress*100f);return "["+BuildProgressBar(Progress)+"] "+percent+"%";}
        static string BuildProgressBar(float progress){const int segmentCount=10;int filled=Mathf.RoundToInt(Mathf.Clamp01(progress)*segmentCount);return new string('#',filled)+new string('-',segmentCount-filled);}
        static string GrantRareDrop(PlayerInventory inventory,int pickaxeLevel)
        {
            float roll=UnityEngine.Random.value;
            string id=null;
            if(pickaxeLevel>=3)
            {
                id=roll<.06f?"Diamond":roll<.16f?"Emerald":roll<.28f?"Ruby":roll<.58f?"GoldOre":null;
            }
            else
            {
                id=roll<.22f?"GoldOre":roll<.30f?"RubyShard":roll<.38f?"EmeraldShard":null;
            }
            if(string.IsNullOrEmpty(id))return string.Empty;
            inventory.AddItem(id,1);
            var item=SurvivalItemCatalog.Find(id);
            return "+1 "+(item!=null?item.Name:id);
        }
    }
}
