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
        public bool costRecorded, authoredFree;
        public int paidWood,paidStone,paidIron,paidGold;
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
        public bool IsSelectingMove { get; private set; }
        public bool IsSelectingDemolition {get;private set;}
        public BuildingData MovingPiece=>moving;
        public SpriteRenderer Preview=>preview;
        public event Action SelectionChanged;
        void Awake(){inventory=GetComponent<PlayerInventory>();}
        void Start(){EnsureWorld();CreateVeins();}
        void EnsureWorld(){if(world==null){world=FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="Outdoor World");}}
        public static string Label(string kind)=>kind switch{"Campfire"=>"Fogata","Fence"=>"Empalizada","StoneWall"=>"Muralla de piedra","ReinforcedWall"=>"Muro reforzado","Trap"=>"Trampa","Turret"=>"Ballesta","Chest"=>"Cofre","Workbench"=>"Banco de trabajo","Beacon"=>"Baliza","Bed"=>"Cama","Cabinet"=>"Armario","Furnace"=>"Horno",_=>kind};
        public static void Cost(string kind,out int wood,out int stone,out int iron)
        {wood=kind=="Fence"?2:kind=="Chest"?8:kind=="Workbench"?12:kind=="Beacon"?12:6;stone=kind=="Fence"?0:kind=="Chest"?2:kind=="Workbench"?8:kind=="Beacon"?8:4;iron=kind=="Beacon"?10:0;if(kind=="Bed"){wood=10;stone=4;}if(kind=="Cabinet"){wood=16;stone=4;}if(kind=="Furnace"){wood=8;stone=20;iron=3;}if(kind=="Trap"){wood=3;stone=3;iron=0;}if(kind=="Turret"){wood=10;stone=6;iron=3;}if(kind=="StoneWall"){wood=2;stone=6;iron=0;}if(kind=="ReinforcedWall"){wood=2;stone=8;iron=2;}}
        public static Sprite SpriteFor(string kind)=>kind=="Campfire"?HouseSprites.Slice("Kitchen",96,0,32,32):FortressPieces.IsWall(kind)?FortressPieces.Art(kind):HouseSprites.Furniture(kind)??Resources.Load<Sprite>("BackpackIcons/"+(kind=="Beacon"?"Gem":kind=="Trap"?"Fence":kind=="Turret"?"Bow":kind));
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
            if (PortfolioSession.Active && !PortfolioSession.Instance.CanBuild(kind)) { FarmNotificationCenter.Show("Este objeto no se puede colocar en esta demo."); return; }
            Cancel();selected=kind;rotation=0;IsPlacing=true;
            var root=new GameObject("Vista previa de construcción");preview=root.AddComponent<SpriteRenderer>();preview.sprite=SpriteFor(kind);preview.sortingOrder=20000;Size(preview,kind);
            RefreshPreview();
            if(PortfolioSession.Active)(GetComponent<ConstructionPalette>()??gameObject.AddComponent<ConstructionPalette>()).Configure(this,inventory);
            SelectionChanged?.Invoke();
            GetComponent<PlayerMovementController>()?.StopMovement();
        }
        static void Size(SpriteRenderer sr,string kind){if(sr.sprite!=null)sr.transform.localScale=Vector3.one*((kind=="Bed"||kind=="Cabinet"?1.6f:kind=="Furnace"||kind=="Campfire"?1.25f:kind=="Fence"?1f:.85f)/Mathf.Max(sr.sprite.bounds.size.x,sr.sprite.bounds.size.y));}
        void ShowOriginal(bool visible)
        {var root=visuals.FirstOrDefault(v=>v!=null&&v.GetComponent<PlacedBuilding>()?.Data==moving);if(root!=null)foreach(var art in root.GetComponentsInChildren<SpriteRenderer>())art.enabled=visible;}
        public void Cancel(){if(moving!=null)ShowOriginal(true);moving=null;IsSelectingMove=IsSelectingDemolition=false;IsPlacing=false;selected=null;if(preview!=null)Destroy(preview.gameObject);preview=null;SelectionChanged?.Invoke();}
        public void SelectMoveMode()
        {
            if(!IsPlacing)Begin("Chest");
            if(!IsPlacing)return;
            if(moving!=null)ShowOriginal(true);moving=null;IsSelectingDemolition=false;IsSelectingMove=true;if(preview!=null)preview.enabled=false;
            SelectionChanged?.Invoke();
        }
        public bool SelectPieceAt(Vector2 point)
        {
            var root=PieceAt(point);
            return root!=null&&BeginMove(root.GetComponent<PlacedBuilding>().Data);
        }
        GameObject PieceAt(Vector2 point)=>visuals.Where(v=>v!=null&&v.activeInHierarchy&&v.GetComponentInChildren<SpriteRenderer>()!=null)
                .OrderBy(v=>Vector2.Distance(v.transform.position,point))
                .FirstOrDefault(v=>v.GetComponentInChildren<SpriteRenderer>().bounds.Contains(new Vector3(point.x,point.y,v.transform.position.z)));
        public void SelectDemolitionMode()
        {
            if(!IsPlacing)Begin("Chest");if(!IsPlacing)return;if(moving!=null)ShowOriginal(true);moving=null;
            IsSelectingMove=false;IsSelectingDemolition=true;if(preview!=null)preview.enabled=false;SelectionChanged?.Invoke();
        }
        public bool DemolishAt(Vector2 point)=>Demolish(PieceAt(point)?.GetComponent<PlacedBuilding>()?.Data);
        public bool Demolish(BuildingData data)
        {
            if(data==null||!Buildings.Contains(data)||Vector2.Distance(transform.position,new Vector2(data.x,data.y))>7)return false;
            if(data.wood+data.stone+data.food+data.iron>0){FarmNotificationCenter.Show("Vacía el almacenamiento antes de desmontarlo.");return false;}
            var refund=new DemolitionRefund(data);var p=new Vector2(data.x,data.y);RemoveDestroyed(data);
            inventory.AddWood(refund.Wood);inventory.AddStone(refund.Stone);GetComponent<AdventureProgress>()?.AddIron(refund.Iron);inventory.AddItem("GoldOre",refund.Gold);
            GetComponent<PlayerCraftingController>()?.RefreshPlacedUtilities();AudioFeedback.PlayAt(CombatSound.Break,p);
            CombatHitParticles.Spawn(p,world,FortressPieces.Tier(data.kind)>1?ImpactSurface.Stone:ImpactSurface.Wood,true,true);
            FarmNotificationCenter.Show($"Desmontado · 69%: {refund.Wood} madera, {refund.Stone} piedra, {refund.Iron} hierro, {refund.Gold} oro");
            FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);SelectionChanged?.Invoke();return true;
        }
        private BuildingData UpgradeAt(string kind,Vector2 point)=>moving!=null||!FortressPieces.IsWall(kind)?null:
            Buildings.FirstOrDefault(b=>!b.indoors&&b.wallVersion>0&&FortressPieces.IsWall(b.kind)&&FortressPieces.Tier(b.kind)<FortressPieces.Tier(kind)&&
                b.rotation%2==rotation%2&&Vector2.Distance(point,new Vector2(b.x,b.y))<.01f);
        public void Rotate() { rotation=1-rotation;RefreshPreview();SelectionChanged?.Invoke(); }
        public Vector2 SnapPlacement(string kind,Vector2 point)
        {
            if(!FortressPieces.IsWall(kind))return FortressPieces.Snap(kind,point,rotation);
            // A first piece may start anywhere on the half-unit grid. Existing endpoints
            // then take priority, including the authored walls and old saved positions.
            Vector2 result=new Vector2(Mathf.Round(point.x*2)*.5f,Mathf.Round(point.y*2)*.5f);
            float best=1.05f*1.05f;
            Vector2 axis=rotation%2==0?Vector2.right:Vector2.up;
            float half=rotation%2==0?FortressPieces.Length*.5f:FortressPieces.VerticalLength*.5f;
            foreach(var b in Buildings)
            {
                if(b==moving||b.indoors||b.wallVersion==0||!FortressPieces.IsWall(b.kind))continue;
                var center=new Vector2(b.x,b.y);
                // Clicking a placed wall must remain a duplicate or an upgrade, not jump to its neighbour.
                if(b.rotation%2==rotation%2&&(point-center).sqrMagnitude<.26f*.26f)return center;
                var otherAxis=b.rotation%2==0?Vector2.right:Vector2.up;
                float otherHalf=b.rotation%2==0?FortressPieces.Length*.5f:FortressPieces.VerticalLength*.5f;
                for(int a=-1;a<=1;a+=2)for(int c=-1;c<=1;c+=2)
                {
                    var candidate=center+otherAxis*otherHalf*a+axis*half*c;
                    if(FortressPieces.Overlaps(kind,candidate,rotation,true,b.kind,center,b.rotation,true))continue;
                    float distance=(candidate-point).sqrMagnitude;
                    if(distance<best){best=distance;result=candidate;}
                }
            }
            return result;
        }
        void RefreshPreview()
        {
            if(preview==null)return;
            if(FortressPieces.IsWall(selected))FortressPieces.Style(preview,selected,rotation);
        }
        public int AvailablePlacements(string kind)
        {
            if(inventory==null||!BackpackActions.IsBuilding(kind))return 0;
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
            return BackpackActions.IsBuilding(kind)&&inventory!=null&&inventory.Wood>=wood&&inventory.Stone>=stone&&
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
            if (PortfolioSession.Active && !PortfolioSession.Instance.CanBuild(kind)) { reason="Este objeto no se puede colocar en esta demo."; return false; }
            var house=GetComponent<HouseSystem>();bool indoors=house!=null&&house.IsInside;
            if(!indoors&&(world==null||!world.gameObject.activeInHierarchy)){reason="Construye en tu terreno.";return false;}
            if(indoors && (!house.Contains(p)||Mathf.Abs(p.x)<.8f&&p.y<HouseSystem.Center.y-house.Size.y/2+2)){reason="Deja libre la puerta y las paredes.";return false;}
            if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||!indoors&&(Mathf.Abs(p.x)>35||Mathf.Abs(p.y)>20)){reason="Fuera del terreno.";return false;}
            var footprint=FortressPieces.Footprint(kind,rotation,moving==null||moving.wallVersion>0);
            var upgrade=UpgradeAt(kind,p);
            if(!indoors&&PortfolioSession.Active&&(!SurvivorFarm.Runtime.World.FarmExploration.Contains(p-footprint*.5f,.3f)||!SurvivorFarm.Runtime.World.FarmExploration.Contains(p+footprint*.5f,.3f))){reason="La pieza saldría del terreno.";return false;}
            Vector2 nearest=new Vector2(Mathf.Clamp(transform.position.x,p.x-footprint.x*.5f,p.x+footprint.x*.5f),Mathf.Clamp(transform.position.y,p.y-footprint.y*.5f,p.y+footprint.y*.5f));
            if(Vector2.Distance(transform.position,nearest)>(PortfolioSession.Active?7:5)){reason="Acércate para seguir construyendo.";return false;}
            if(!indoors&&!CultivationGrid.IsGreen(p) && !(PortfolioSession.Active && FarmDefense.IsDefense(kind))){reason="Deja libres los caminos principales.";return false;}
            if(!indoors&&GetComponent<SurvivorFarm.Runtime.World.EnemyCampWorld>()?.ContainsCamp(p,.6f)==true){reason="Deja libre el campamento y sus accesos.";return false;}
            foreach(var c in Physics2D.OverlapBoxAll(p,footprint,0))
            {
                if(moving!=null&&c.GetComponentInParent<PlacedBuilding>()?.Data==moving)continue;
                var placed=c.GetComponentInParent<PlacedBuilding>();
                if(placed!=null)
                {
                    var b=placed.Data;
                    if(b==upgrade)continue;
                    if(!FortressPieces.Overlaps(kind,p,rotation,moving==null||moving.wallVersion>0,b.kind,new Vector2(b.x,b.y),b.rotation,b.wallVersion>0))continue;
                }
                var plot=c.GetComponentInParent<FarmingPlot>();
                var resource=c.GetComponentInParent<HarvestableResource>();
                // Service/interaction zones are not physical occupancy. Crops, living
                // resources and solid bodies still block placement.
                if(!c.isTrigger||plot!=null&&plot.StateId!=0||resource!=null&&resource.IsAvailable)
                {
                    if(plot!=null&&plot.StateId==0)continue;
                    reason=c.GetComponentInParent<PlayerInventory>()!=null?"La pieza toca tus pies: aléjate un poco.":
                        plot!=null?"Hay un cultivo en ese espacio.":resource!=null?"Retira primero ese árbol, roca o animal.":
                        c.GetComponentInParent<VillageResidentHealth>()!=null?"Espera a que pase el aldeano.":"La pieza toca un obstáculo.";return false;
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
            p=SnapPlacement(selected,p);
            bool wasMoving=moving!=null;
            bool done=PlaceInternal(selected,p,inventory.PackedCount(selected)>0);
            if(done&&wasMoving){moving=null;SelectMoveMode();}
            else if(done)SelectionChanged?.Invoke();
            return done;
        }
        bool PlaceInternal(string kind,Vector2 p,bool packed)
        {
            if(moving!=null&&!Buildings.Contains(moving)){Cancel();FarmNotificationCenter.Show("La pieza ha sido destruida.");return false;}
            if(!CanPlace(kind,p,out var reason)){PlacementHint=reason;FarmNotificationCenter.Show(reason);return false;}
            if(moving!=null){var moved=moving;RemoveDestroyed(moved);moved.x=p.x;moved.y=p.y;moved.rotation=rotation;moved.indoors=GetComponent<HouseSystem>()?.IsInside==true;Buildings.Add(moved);CreateVisual(moved);FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);return true;}
            Cost(kind,out var wood,out var stone,out var iron);var progress=GetComponent<AdventureProgress>();
            if(!packed&&!CanAfford(kind)){FarmNotificationCenter.Show("Faltan materiales: consulta los iconos de la paleta.");return false;}
            if(packed){if(!inventory.RemovePacked(kind))return false;}
            else Pay(kind);
            var upgrade=UpgradeAt(kind,p);
            if(upgrade!=null)
            {
                DemolitionRefund.RecordPayment(upgrade,kind);
                float fraction=upgrade.health/(float)FortressPieces.Health(upgrade.kind);
                RemoveDestroyed(upgrade);upgrade.kind=kind;upgrade.health=Mathf.Max(1,Mathf.RoundToInt(FortressPieces.Health(kind)*Mathf.Clamp01(fraction)));
                Buildings.Add(upgrade);CreateVisual(upgrade);
                GetComponent<GameFeelFeedback>()?.Pulse("Muralla mejorada",p,false,true);
                FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);return true;
            }
            var data=new BuildingData{kind=kind,x=p.x,y=p.y,rotation=rotation,wallVersion=1,costRecorded=true,indoors=GetComponent<HouseSystem>()?.IsInside==true};
            DemolitionRefund.RecordPayment(data,kind);Buildings.Add(data);CreateVisual(data);
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
            if(FortressPieces.IsWall(data.kind))SurvivorFarm.Runtime.World.TreeOcclusionFader.Ensure(root);
            if(PortfolioSession.Active&&data.kind=="Campfire")SurvivorFarm.Runtime.World.PackEnvironment.Animate(sr,"Kitchen",32,0,32,32,4,32,0,4);
            PortfolioSession.Instance?.Navigation.Invalidate();
        }
        public void AddAuthoredDefense(string kind, Vector2 position, int health = -1)
        {
            if(!BackpackActions.IsBuilding(kind))return;
            var data = new BuildingData { kind=kind, x=position.x, y=position.y, health=health,wallVersion=1,authoredFree=true,costRecorded=true };
            Buildings.Add(data); CreateVisual(data);
        }
        public void RemoveDestroyed(BuildingData data)
        {
            var root=visuals.FirstOrDefault(v=>v!=null&&v.GetComponent<PlacedBuilding>()?.Data==data);
            Buildings.Remove(data);
            if(root!=null){visuals.Remove(root);root.SetActive(false);Destroy(root);}
            PortfolioSession.Instance?.Navigation.Invalidate();
        }
        public bool CanAccess(BuildingData data) => data!=null && Buildings.Contains(data) && (data.indoors ? GetComponent<HouseSystem>()?.CanUseServices==true : Vector2.Distance(transform.position,new Vector2(data.x,data.y))<=2);
        public bool BeginMove(BuildingData data)
        {if(data==null||!Buildings.Contains(data)||(!CanAccess(data)&&(!PortfolioSession.Active||data.indoors||Vector2.Distance(transform.position,new Vector2(data.x,data.y))>7)))return false;Begin(data.kind);if(selected!=data.kind)return false;moving=data;ShowOriginal(false);rotation=data.rotation;RefreshPreview();SelectionChanged?.Invoke();return true;}
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
            // Old saves remain readable, but retired defenses must not return as
            // invisible blockers or continue dealing damage after the rules change.
            Buildings=(data??new List<BuildingData>()).Where(b=>b!=null&&BackpackActions.IsBuilding(b.kind)).ToList();
            foreach(var b in Buildings)CreateVisual(b);
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
            if(Input.GetKeyDown(KeyCode.M))SelectMoveMode();
            if(Input.GetKeyDown(KeyCode.B))SelectDemolitionMode();
            if(IsSelectingDemolition)
            {
                PlacementHint="Clic sobre una pieza · desmontar y recuperar 69% · Esc cancelar";
                if(Input.GetMouseButtonDown(0)&&(EventSystem.current==null||!EventSystem.current.IsPointerOverGameObject()))
                    if(!DemolishAt(Camera.main.ScreenToWorldPoint(Input.mousePosition)))FarmNotificationCenter.Show("Selecciona una construcción cercana para desmontar.");
                return;
            }
            if(IsSelectingMove)
            {
                PlacementHint="Selecciona un mueble · después elige su nuevo lugar · sin coste";
                FarmNotificationCenter.SetPrompt("");
                if(Input.GetMouseButtonDown(0)&&(EventSystem.current==null||!EventSystem.current.IsPointerOverGameObject()))
                    if(!SelectPieceAt(Camera.main.ScreenToWorldPoint(Input.mousePosition)))FarmNotificationCenter.Show("Selecciona una pieza construida a menos de 7 metros.");
                return;
            }
            if(Input.GetKeyDown(KeyCode.R))Rotate();
            Vector2 p=SnapPlacement(selected,Camera.main.ScreenToWorldPoint(Input.mousePosition));
            bool valid=CanPlace(selected,p,out var reason);PlacementHint=valid?(UpgradeAt(selected,p)!=null?"Clic: mejorar este muro (coste de pieza completa)":"Clic: colocar "+Label(selected)):reason;
            if(valid&&moving==null&&AvailablePlacements(selected)==0)
            {
                valid=false;reason="Sin materiales para otra pieza · elige otra o sal a recolectar";PlacementHint=reason;
            }
            FarmNotificationCenter.SetPrompt("");
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
        public override bool IsAvailable => GetComponent<FarmDefense>() == null&&!(PortfolioSession.Active&&Data?.kind=="Campfire");
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
