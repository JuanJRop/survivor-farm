using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>A retracting drawer. It never owns placement, stock, movement or spending.</summary>
    public sealed class ConstructionPalette : MonoBehaviour
    {
        private ConstructionSystem construction;
        private PlayerInventory inventory;
        private GameObject canvasRoot;
        private RectTransform panel, handle;
        private Text status, packed, handleText;
        private CanvasGroup drawerGroup;
        private static readonly string[] Utilities={"Campfire","Chest","Workbench","Bed","Furnace"};
        private readonly Button[] cards=new Button[5];
        private readonly Text[] amounts=new Text[5];
        private readonly MaterialCostBadge[] costs=new MaterialCostBadge[4];
        private float refreshAt, expansion, holdOpenUntil;
        private bool expanded;
        public bool Visible=>canvasRoot!=null&&canvasRoot.activeSelf;
        public float Expansion=>expansion;
        public bool Expanded=>expanded;
        public RectTransform Drawer=>panel;
        public RectTransform Handle=>handle;

        public void Configure(ConstructionSystem owner,PlayerInventory source)
        {
            construction=owner;inventory=source;
            bool first=canvasRoot==null;
            if(first)Build();
            Refresh();
            if(first)SetExpanded(true);
        }
        private void Build()
        {
            canvasRoot=MasteryWindow.CreateCanvas("Construcción · barra retráctil",95);
            panel=AdventureWindow.Rect(canvasRoot.transform,"Muebles y cocina",0,0,730,182);
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,0);
            FarmUiStyle.Frame(panel.gameObject.AddComponent<Image>());
            drawerGroup=panel.gameObject.AddComponent<CanvasGroup>();
            status=MasteryWindow.Label(panel,"",14,8,365,26,15);
            MasteryWindow.Button(panel,"Mover · M",388,6,126,30,()=>construction.SelectMoveMode());
            MasteryWindow.Button(panel,"Desmontar · B",524,6,144,30,()=>construction.SelectDemolitionMode());
            FarmUiStyle.CloseButton(MasteryWindow.Button(panel,"×",676,2,38,36,()=>construction.Cancel()));
            string[] names={"Fogata","Cofre","Banco","Cama","Horno"};
            for(int i=0;i<cards.Length;i++)
            {
                string kind=Utilities[i];
                var card=MasteryWindow.Button(panel,"",14+i*142,42,134,90,()=>{construction.Begin(kind);SetExpanded(false);});
                cards[i]=card;
                var icon=AdventureWindow.Rect(card.transform,"Pieza",47,5,40,40).gameObject.AddComponent<Image>();
                icon.sprite=ConstructionSystem.SpriteFor(kind);icon.preserveAspect=true;icon.raycastTarget=false;
                MasteryWindow.Label(card.transform,names[i],3,46,128,21,14).alignment=TextAnchor.MiddleCenter;
                amounts[i]=MasteryWindow.Label(card.transform,"",3,68,128,19,12);amounts[i].alignment=TextAnchor.MiddleCenter;
            }
            string[] ids={"Wood","Stone","Iron","GoldOre"};
            for(int i=0;i<costs.Length;i++)costs[i]=MaterialCostBadge.Create(panel,ids[i],14+i*128,142,119);
            packed=MasteryWindow.Label(panel,"",534,139,180,32,13);
            handle=AdventureWindow.Rect(canvasRoot.transform,"Abrir construcciones",0,0,356,34);
            handle.anchorMin=handle.anchorMax=handle.pivot=new Vector2(.5f,0);
            var handleButton=handle.gameObject.AddComponent<Image>();FarmUiStyle.Frame(handleButton,true);
            var button=handle.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>SetExpanded(!expanded));
            handleText=MasteryWindow.Label(handle,"",8,1,340,32,13);handleText.alignment=TextAnchor.MiddleCenter;
            construction.SelectionChanged+=Refresh;inventory.InventoryChanged+=Refresh;
            Animate(0);
        }
        public void SetExpanded(bool value)
        {
            expanded=value;holdOpenUntil=value?Time.unscaledTime+.8f:0;
        }
        private void Animate(float delta)
        {
            expansion=Mathf.MoveTowards(expansion,expanded?1:0,delta*5.5f);
            float eased=expansion*expansion*(3-2*expansion);
            panel.anchoredPosition=new Vector2(0,Mathf.Lerp(-184,8,eased));
            handle.anchoredPosition=new Vector2(0,8+eased*186);
            drawerGroup.alpha=Mathf.Clamp01(expansion*2);
            drawerGroup.blocksRaycasts=expansion>.02f;
        }
        public void Refresh()
        {
            if(canvasRoot==null||construction==null)return;
            bool show=ConstructionSystem.IsPlacing&&construction.SelectedKind!=null&&
                (!PortfolioSession.Active||PortfolioSession.Instance.HasBegun&&!PortfolioSession.Instance.IsPaused);
            canvasRoot.SetActive(show);if(!show)return;
            string selected=construction.SelectedKind;
            status.text=construction.IsSelectingDemolition?"Desmontar · devolución del 69%":construction.IsSelectingMove?"Selecciona una pieza":construction.IsMoving?"Pieza anclada al cursor · clic para soltar":ConstructionSystem.Label(selected)+"  ·  R girar";
            for(int i=0;i<cards.Length;i++)
            {
                string kind=Utilities[i];bool unlocked=!PortfolioSession.Active||PortfolioSession.Instance.CanBuild(kind);
                cards[i].interactable=unlocked&&!construction.IsMoving;FarmUiStyle.Button(cards[i],selected==kind);
                int count=construction.AvailablePlacements(kind);
                amounts[i].text=unlocked?"× "+FarmUiStyle.Quantity(count):"No disponible";
                amounts[i].color=!unlocked?FarmUiStyle.Muted:count>0?FarmUiStyle.Positive:FarmUiStyle.Negative;
            }
            ConstructionSystem.Cost(selected,out var wood,out var stone,out var iron);
            costs[0].Set(inventory.Wood,wood);costs[1].Set(inventory.Stone,stone);
            costs[2].Set(inventory.GetComponent<AdventureProgress>()?.Data.iron??0,iron);
            costs[3].Set(inventory.GetAvailableItemCount("GoldOre"),FortressPieces.GoldCost(selected));
            costs[0].SetVisible(wood>0);costs[1].SetVisible(stone>0);costs[2].SetVisible(iron>0);costs[3].SetVisible(FortressPieces.GoldCost(selected)>0);
            int stock=inventory.PackedCount(selected);
            packed.text=construction.IsMoving||construction.IsSelectingMove?"Mover no cuesta materiales":stock>0?$"×{stock} en mochila · primero":"Materiales por pieza";
        }
        private void Update()
        {
            if(Time.unscaledTime>=refreshAt){refreshAt=Time.unscaledTime+.15f;Refresh();}
            if(!Visible)return;
            FarmUiStyle.FitWindow(panel);handle.localScale=panel.localScale;
            bool overHandle=RectTransformUtility.RectangleContainsScreenPoint(handle,Input.mousePosition);
            bool overPanel=expansion>.05f&&RectTransformUtility.RectangleContainsScreenPoint(panel,Input.mousePosition);
            if(overHandle||overPanel)expanded=true;
            else if(Time.unscaledTime>=holdOpenUntil)expanded=false;
            Animate(Time.unscaledDeltaTime);
            string name=construction.IsSelectingDemolition?"DESMONTAR · 69%":construction.IsSelectingMove||construction.IsMoving?"MOVER PIEZAS":ConstructionSystem.Label(construction.SelectedKind).ToUpperInvariant();
            handleText.text=expanded?"▾  "+name+"  ·  vuelve al mapa para ocultar":name+"   ▴   PIEZAS   ·   ESC salir";
        }
        private void OnDestroy()
        {
            if(construction!=null)construction.SelectionChanged-=Refresh;
            if(inventory!=null)inventory.InventoryChanged-=Refresh;
            if(canvasRoot!=null)Destroy(canvasRoot);
        }
    }
}
