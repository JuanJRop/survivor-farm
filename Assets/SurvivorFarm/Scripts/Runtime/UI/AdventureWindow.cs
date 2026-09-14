using System;
using System.Linq;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;
using UnityEngine.UI;
namespace SurvivorFarm.Runtime.UI
{
    public sealed class AdventureWindow : MonoBehaviour
    {
        public static bool IsOpen {get;private set;}
        RectTransform root,content;PlayerInventory inventory;Transform canvas;bool paused;float oldTimeScale;string page;BuildingData chest,furniture;
        public string JournalTab { get; private set; } = "Objective";
        public void Configure(PlayerInventory player,Transform ui)
        {
            inventory=player;canvas=ui;root=Rect(ui,"Diario y construcción",0,0,850,600);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
            FarmUiStyle.Frame(root.gameObject.AddComponent<Image>());root.gameObject.SetActive(false);
            var hud=ui.GetComponentInChildren<OriginalSpriteHud>(true);
            if(!Application.isEditor&&!GameSaveSystem.IsQa)Open("Pause");
            if(hud!=null){var button=Button(hud.transform,"Diario [J]",296,0,132,34,()=>Open("Journal"));var rect=button.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;rect.anchoredPosition=new Vector2(296,64);hud.RequestStyleRefresh();}
        }
        public static RectTransform Rect(Transform p,string n,float x,float y,float w,float h){var t=new GameObject(n,typeof(RectTransform)).GetComponent<RectTransform>();t.SetParent(p,false);t.anchorMin=t.anchorMax=t.pivot=new Vector2(0,1);t.anchoredPosition=new Vector2(x,-y);t.sizeDelta=new Vector2(w,h);return t;}
        static Text Label(Transform p,string s,float x,float y,float w,float h,int size=18){var t=Rect(p,"Texto",x,y,w,h).gameObject.AddComponent<Text>();FarmUiStyle.Text(t,size,true);t.text=s;return t;}
        static Button Button(Transform p,string s,float x,float y,float w,float h,Action action){var r=Rect(p,s,x,y,w,h);r.gameObject.AddComponent<Image>();var button=r.gameObject.AddComponent<Button>();FarmUiStyle.Button(button);button.onClick.AddListener(()=>action());var text=Label(r,s,8,4,w-16,h-8,17);text.alignment=TextAnchor.MiddleCenter;return button;}
        public void OpenFurniture(BuildingData data){furniture=data;Open("Furniture");}
        public void OpenChest(BuildingData data){chest=data;Open("Chest");}
        public void Open(string selected)
        {
            if((selected=="Home"||selected=="HomeStorage")&&inventory.GetComponent<HouseSystem>()?.CanUseServices!=true)return;
            VillageDialogueWindow.CloseActive();
            SimpleShopSystem.CloseActive();
            Close();GetComponent<InventoryPanelSystem>()?.Close();GetComponent<PlayerEquipmentWindow>()?.Close();GetComponent<CraftingWindow>()?.Close();inventory.GetComponent<ConstructionSystem>().Cancel();
            page=selected;IsOpen=true;inventory.GetComponent<PlayerMovementController>()?.StopMovement();root.gameObject.SetActive(true);root.SetAsLastSibling();
            if(page=="Pause"){oldTimeScale=Time.timeScale;paused=true;Time.timeScale=0;}
            Draw();FarmUiStyle.FitWindow(root);
        }
        void Draw()
        {
            if(content!=null){content.gameObject.SetActive(false);Destroy(content.gameObject);}content=Rect(root,"Contenido",0,0,850,600);
            Label(content,page=="Home"||page=="HomeStorage"?"REFUGIO":page=="Furniture"?"MUEBLE":page=="Build"?"CONSTRUIR [G]":page=="Chest"?"COFRE":page=="Pause"?"PAUSA [P]":"DIARIO DE EXPEDICIÓN [J]",24,18,620,40,24);
            FarmUiStyle.CloseButton(Button(content,"Cerrar [Esc]",782,16,44,40,Close));
            var progress=inventory.GetComponent<AdventureProgress>();var build=inventory.GetComponent<ConstructionSystem>();
            if(page=="Home")
            {
                var home=inventory.GetComponent<HouseSystem>();
                Label(content,HouseSystem.Names[home.Data.level]+" · Oro: "+inventory.Coins,24,70,800,32,22);
                string[] ids={"Campfire","Bed","Chest","Workbench","Cabinet","Furnace"};
                for(int i=0;i<ids.Length;i++)
                {
                    string id=ids[i];int x=24+(i%2)*405,y=118+(i/2)*74;
                    var icon=Rect(content,"Mueble",x,y,48,48).gameObject.AddComponent<Image>();icon.sprite=ConstructionSystem.SpriteFor(id);icon.preserveAspect=true;
                    string requirement=home.Requirement(id);
                    var b=Button(content,ConstructionSystem.Label(id)+" · "+home.Price(id)+" oro"+(requirement!=""?"\n"+requirement:" · Comprar"),x+56,y,338,62,()=>{if(!home.Buy(id))FarmNotificationCenter.Show("Revisa el oro y los requisitos.");Draw();});b.interactable=requirement==""&&inventory.Coins>=home.Price(id);
                }
                home.UpgradeCost(out var w,out var st,out var iron,out var coins);
                Label(content,home.Data.level==3?"Tu mansión está completa.":"Siguiente: "+HouseSystem.Names[home.Data.level+1]+" · "+w+" madera, "+st+" piedra, "+iron+" hierro, "+coins+" oro\n"+home.UpgradeRequirement,24,351,800,60,18);
                var upgrade=Button(content,"Ampliar casa",24,419,250,42,()=>{if(!home.Upgrade())FarmNotificationCenter.Show("Revisa los materiales y el avance de la historia.");Draw();});upgrade.interactable=home.UpgradeRequirement=="";
                var style=Button(content,"Cambiar acabado",294,419,250,42,()=>{home.ChangeStyle();Draw();});style.interactable=home.Data.level>0;
                Button(content,"Abrir mochila [I]",564,419,260,42,()=>{Close();GetComponent<InventoryPanelSystem>().Toggle();});
                if(build.Buildings.Any(b=>b.indoors))Button(content,"Muebles del refugio",24,487,350,42,()=>Open("HomeStorage"));
            }
            else if(page=="HomeStorage")
            {
                Label(content,"MUEBLES DEL REFUGIO",24,68,720,36,22);
                var scroll=FarmUiStyle.Scroll(content,"Muebles guardados",24,118,802,380);int row=0;
                foreach(var item in build.Buildings.Where(b=>b.indoors))
                {
                    var saved=item;float y=row++*74;
                    var icon=Rect(scroll.content,"Mueble",0,y,48,48).gameObject.AddComponent<Image>();icon.sprite=ConstructionSystem.SpriteFor(item.kind);icon.preserveAspect=true;
                    Label(scroll.content,ConstructionSystem.Label(item.kind),64,y+4,400,42,20);
                    Button(scroll.content,"Gestionar",530,y,240,48,()=>OpenFurniture(saved));
                }
                scroll.content.sizeDelta=new Vector2(790,Mathf.Max(380,row*74));
                Button(content,"Volver al refugio",24,530,300,42,()=>Open("Home"));
            }
            else if(page=="Furniture")
            {
                var home=inventory.GetComponent<HouseSystem>();
                var icon=Rect(content,"Mueble",355,85,140,140).gameObject.AddComponent<Image>();icon.sprite=ConstructionSystem.SpriteFor(furniture.kind);icon.preserveAspect=true;
                Label(content,ConstructionSystem.Label(furniture.kind),24,235,800,40,26);
                string help=furniture.kind=="Bed"?"Dormir por la noche y recuperar salud.":furniture.kind=="Furnace"?"Fundir: 5 piedra + 2 madera → 1 hierro.":furniture.kind=="Chest"||furniture.kind=="Cabinet"?"Guardar recursos; el contenido se conserva al moverlo.":furniture.kind=="Campfire"?"Abrir las recetas para cocinar.":"Banco para reforjar tu espada.";
                Label(content,help,24,288,800,65,19);
                Button(content,"Usar",24,379,250,48,()=>{var selected=furniture;Close();if(selected.kind=="Bed")home.Sleep(selected);else if(selected.kind=="Chest"||selected.kind=="Cabinet")OpenChest(selected);else if(selected.kind=="Furnace")FarmNotificationCenter.Show(home.Smelt(selected)?"+1 hierro. Puedes seguir fundiendo.":"Necesitas 5 piedra y 2 madera junto al horno.");else if(selected.kind=="Campfire")GetComponent<CraftingWindow>().Open();else build.TemperSword();});
                Button(content,"Mover",294,379,250,48,()=>{var selected=furniture;Close();build.BeginMove(selected);});
                Button(content,"Guardar en mochila",564,379,260,48,()=>{var selected=furniture;Close();build.Store(selected);});
                if(furniture.indoors)Button(content,"Volver a muebles",24,458,300,42,()=>Open("HomeStorage"));
            }
            else if(page=="Build")
            {
                var stock=Rect(content,"Materiales disponibles",24,68,800,38);
                MaterialCostBadge.Create(stock,"Wood",12,3,240).Set(inventory.Wood,0,true);MaterialCostBadge.Create(stock,"Stone",276,3,240).Set(inventory.Stone,0,true);MaterialCostBadge.Create(stock,"Iron",540,3,240).Set(progress.Data.iron,0,true);
                string[] kinds={"Campfire","Fence","Chest","Workbench","Beacon"};
                for(int i=0;i<kinds.Length;i++)
                {
                    string kind=kinds[i];ConstructionSystem.Cost(kind,out var w,out var s,out var iron);
                    var icon=Rect(content,"Icono",32,115+i*70,48,48).gameObject.AddComponent<Image>();icon.sprite=ConstructionSystem.SpriteFor(kind);icon.preserveAspect=true;
                    Button(content,ConstructionSystem.Label(kind),96,113+i*70,278,52,()=>{Close();build.Begin(kind);});
                    var costs=Rect(content,"Ingredientes",384,113+i*70,412,52);
                    MaterialCostBadge.Create(costs,"Wood",12,10,126).Set(inventory.Wood,w);if(s>0)MaterialCostBadge.Create(costs,"Stone",146,10,126).Set(inventory.Stone,s);if(iron>0)MaterialCostBadge.Create(costs,"Iron",280,10,126).Set(progress.Data.iron,iron);
                }
                Label(content,"Selecciona, apunta al terreno y confirma con clic izquierdo.\nVerde: válido · Rojo: bloqueado · Clic derecho/Esc: cancelar sin gastar.\nPuedes colocar cerca de ti; deja libres los caminos y las entradas.",24,481,800,97,17);
            }
            else if(page=="Chest")
            {
                Label(content,"Transfiere hasta 10 unidades por clic. El contenido queda guardado en este cofre.",24,76,800,58);
                string[] ids={"Wood","Stone","Food","Iron"};string[] names={"Madera","Piedra","Comida","Hierro"};int[] counts={chest.wood,chest.stone,chest.food,chest.iron};
                for(int i=0;i<4;i++){string id=ids[i];Label(content,names[i]+": "+counts[i],30,155+i*75,220,40);Button(content,"Guardar 10",280,150+i*75,235,48,()=>{build.Transfer(chest,id,true);Draw();});Button(content,"Retirar 10",540,150+i*75,235,48,()=>{build.Transfer(chest,id,false);Draw();});}
            }
            else if(page=="Pause")
            {
                Button(content,"Continuar",24,85,390,48,Close);
                Button(content,"Guardar partida",436,85,390,48,()=>FindFirstObjectByType<GameSaveSystem>()?.SaveGame(true));
                Button(content,"Sonidos y efectos: "+(GameFeelFeedback.Enabled?"sí":"no"),24,149,390,48,()=>{GameFeelFeedback.Enabled=!GameFeelFeedback.Enabled;Draw();});
                Button(content,Screen.fullScreen?"Usar ventana":"Pantalla completa",436,149,390,48,()=>{Screen.fullScreen=!Screen.fullScreen;Draw();});
                Button(content,"Nueva partida en otra ranura",24,213,390,48,()=>{FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);Close();GameSaveSystem.StartNewSlot();});
                Button(content,"Guardar y salir",436,213,390,48,()=>{FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);Close();Application.Quit();});
                Label(content,"CONTROLES\nWASD: moverse · Shift: correr\nClic izquierdo: atacar hacia el ratón · Clic derecho/E: interactuar\n1: espada · 2: arco\nHacha y pico se activan al recolectar\nB/I: mochila · C: equipo · F: recetas · G: construir\nJ: diario · P: pausa · Esc: cerrar/cancelar\n\nUna nueva partida conserva el archivo de la anterior. La última ranura se abre al iniciar.",24,290,800,250,18);
            }
            else
            {
                DrawJournal(inventory.GetComponent<ValleyCampaign>(),progress);
            }
        }
        public void SelectJournalTab(string selected)
        {
            if(selected!="Objective"&&selected!="Projects"&&selected!="Villagers"&&selected!="Combat")return;
            JournalTab=selected;
            if(IsOpen&&page=="Journal")Draw();
        }
        void DrawJournal(ValleyCampaign valley,AdventureProgress progress)
        {
            Label(content,valley!=null?valley.InfluenceLine:"Expedición",24,68,800,32,18);
            string[] ids={"Objective","Projects","Villagers","Combat"};string[] labels={"Objetivo","Proyectos","Aldeanos","Combate"};
            for(int i=0;i<ids.Length;i++)
            {
                string id=ids[i];var tab=Button(content,labels[i],24+i*202,108,194,40,()=>SelectJournalTab(id));
                FarmUiStyle.Button(tab,JournalTab==id);
            }
            var scroll=FarmUiStyle.Scroll(content,"Vista del diario",24,166,802,346);
            var body=scroll.content;float width=body.sizeDelta.x;float y=0;
            if(valley==null)
            {
                var text=FarmUiStyle.Paragraph(body,progress!=null?progress.Objective:"El diario aún no está disponible.",y,width,20);
                y+=text.rectTransform.rect.height;
            }
            else if(JournalTab=="Objective")
            {
                var heading=FarmUiStyle.Paragraph(body,ValleyCampaign.Titles[valley.Chapter],y,width,22);y+=heading.rectTransform.rect.height+10;
                FarmUiStyle.Progress(body,"Historia",valley.Chapter,6,y,width);y+=22;
                var objective=FarmUiStyle.Paragraph(body,valley.Objective,y,width,20);y+=objective.rectTransform.rect.height+18;
                var status=FarmUiStyle.Paragraph(body,valley.VillageStatus,y,width);status.color=FarmUiStyle.Muted;y+=status.rectTransform.rect.height+12;
                var rank=FarmUiStyle.Paragraph(body,valley.NextRankRequirement,y,width);rank.color=FarmUiStyle.Accent;y+=rank.rectTransform.rect.height+12;
            }
            else if(JournalTab=="Projects")
            {
                var heading=FarmUiStyle.Paragraph(body,"Reconstrucción · "+valley.VillageRepairCount+"/5",y,width,22);y+=heading.rectTransform.rect.height+10;
                FarmUiStyle.Progress(body,"Reparaciones",valley.VillageRepairCount,5,y,width);y+=26;
                // Campaign owns each project's availability, costs, rewards and rank requirements.
                foreach(string line in valley.VillageBoard.Split('\n'))
                {
                    var text=FarmUiStyle.Paragraph(body,line,y,width);y+=text.rectTransform.rect.height+12;
                }
            }
            else if(JournalTab=="Combat")
            {
                foreach(var quest in CampCombatQuests.All)
                {
                    string id=quest.CampId;
                    var row=Rect(body,"Encargo de combate "+id,0,y,width,142);
                    var icon=Rect(row,"Combate",0,0,32,32).gameObject.AddComponent<Image>();
                    icon.sprite=FarmUiStyle.ItemIcon("Sword");icon.preserveAspect=true;icon.raycastTarget=false;
                    Label(row,quest.Title,44,0,width-240,30,21);
                    bool accepted=CampCombatQuests.IsAccepted(valley.Data,id),claimed=CampCombatQuests.IsClaimed(valley.Data,id);
                    if(accepted&&!claimed)
                    {
                        bool tracked=valley.Data.trackedCampQuest==id;
                        var track=Button(row,tracked?"Dejar de seguir":"Seguir",width-180,0,176,36,()=>{valley.TrackCampQuest(tracked?null:id);Draw();});
                        FarmUiStyle.Button(track,tracked);
                    }
                    Label(row,quest.Camp.Title+" - "+CampCombatQuests.Progress(valley.Data,id),0,36,width,28,18);
                    FarmUiStyle.Progress(row,"Guardias",CampCombatQuests.DefeatedCount(valley.Data,id),quest.Camp.Roster.Length,66,width-4);
                    Label(row,quest.Location,0,82,width,28,17).color=FarmUiStyle.Muted;
                    Label(row,CampCombatQuests.Status(valley.Data,id),0,114,width/2-8,28,16).color=claimed?FarmUiStyle.Positive:FarmUiStyle.Accent;
                    Label(row,"Iria: "+quest.Reward,width/2,114,width/2-4,28,16);
                    y+=158;
                }
            }
            else
            {
                foreach(string line in valley.VillagerGuide.Split('\n'))
                {
                    var text=FarmUiStyle.Paragraph(body,line,y,width,20);y+=text.rectTransform.rect.height+20;
                }
                var services=FarmUiStyle.Paragraph(body,valley.VillageServicesSummary,y,width);services.color=FarmUiStyle.Muted;y+=services.rectTransform.rect.height+12;
            }
            body.sizeDelta=new Vector2(width,Mathf.Max(346,y));
            Button(content,"Construir",24,538,250,42,()=>Open("Build"));
            Button(content,"Recetas",294,538,250,42,()=>{Close();GetComponent<CraftingWindow>().Open();});
            Button(content,"Pausa",564,538,260,42,()=>Open("Pause"));
        }
        public void Close(){IsOpen=false;if(root!=null)root.gameObject.SetActive(false);if(paused){Time.timeScale=oldTimeScale;paused=false;}}
        void Update()
        {
            if(PlayerRespawnController.MenuOpen){Close();return;}
            if(Input.GetKeyDown(KeyCode.H)){if(IsOpen&&page=="Home")Close();else if(inventory.GetComponent<HouseSystem>()?.CanUseServices==true)Open("Home");else FarmNotificationCenter.Show("Acercate a la fachada de tu refugio para gestionarlo.");}
            if(Input.GetKeyDown(KeyCode.J)){if(IsOpen&&page=="Journal")Close();else Open("Journal");}
            if(Input.GetKeyDown(KeyCode.G)){if(IsOpen&&page=="Build")Close();else Open("Build");}
            if(Input.GetKeyDown(KeyCode.P)){if(IsOpen&&page=="Pause")Close();else Open("Pause");}
            if(IsOpen&&Input.GetKeyDown(KeyCode.Escape))Close();
            if(IsOpen)FarmUiStyle.FitWindow(root);
        }
        void OnDisable()=>Close();void OnDestroy(){Close();if(root!=null)Destroy(root.gameObject);}
    }
}
