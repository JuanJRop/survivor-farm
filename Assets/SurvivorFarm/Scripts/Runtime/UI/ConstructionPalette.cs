using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>Presentation only. Selection, validity, stock and payment stay in ConstructionSystem.</summary>
    public sealed class ConstructionPalette : MonoBehaviour
    {
        private ConstructionSystem construction;
        private PlayerInventory inventory;
        private GameObject canvasRoot;
        private RectTransform panel;
        private Text status, packed;
        private readonly Button[] cards=new Button[5];
        private readonly Text[] amounts=new Text[5];
        private readonly MaterialCostBadge[] costs=new MaterialCostBadge[4];
        private float refreshAt;
        public bool Visible => canvasRoot!=null&&canvasRoot.activeSelf;

        public void Configure(ConstructionSystem owner,PlayerInventory source)
        {
            construction=owner;inventory=source;
            if(canvasRoot==null)Build();
            Refresh();
        }
        private void Build()
        {
            canvasRoot=new GameObject("Paleta de fortaleza",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=canvasRoot.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=95;
            var scaler=canvasRoot.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            panel=AdventureWindow.Rect(canvasRoot.transform,"Construcción continua",0,0,760,222);
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,0);panel.anchoredPosition=new Vector2(0,12);
            FarmUiStyle.Frame(panel.gameObject.AddComponent<Image>());
            status=Label(panel,"",18,10,635,32,18);
            FarmUiStyle.CloseButton(Button(panel,"Terminar",704,8,40,36,()=>construction.Cancel()));
            string[] names={"Empalizada","Piedra","Reforzado","Trampa","Ballesta"};
            for(int i=0;i<cards.Length;i++)
            {
                int index=i;string kind=FortressPieces.Palette[i];
                var card=Button(panel,"",18+i*145,48,136,104,()=>construction.Begin(kind));cards[i]=card;
                var icon=AdventureWindow.Rect(card.transform,"Pieza",43,7,50,50).gameObject.AddComponent<Image>();
                icon.sprite=ConstructionSystem.SpriteFor(kind);icon.preserveAspect=true;icon.raycastTarget=false;
                if(kind=="ReinforcedWall")icon.color=new Color(1,.86f,.58f);
                Label(card.transform,names[index],5,58,126,22,15).alignment=TextAnchor.MiddleCenter;
                amounts[i]=Label(card.transform,"",5,80,126,20,14);amounts[i].alignment=TextAnchor.MiddleCenter;
            }
            string[] ids={"Wood","Stone","Iron","GoldOre"};
            for(int i=0;i<costs.Length;i++)costs[i]=MaterialCostBadge.Create(panel,ids[i],22+i*144,169,130);
            packed=Label(panel,"",607,164,133,45,14);
            Label(panel,"CLIC colocar varias   ·   R girar   ·   ESC salir   ·   Deja un acceso entre los muros",22,202,718,18,13).color=FarmUiStyle.Muted;
            construction.SelectionChanged+=Refresh;
            inventory.InventoryChanged+=Refresh;
        }
        public void Refresh()
        {
            if(canvasRoot==null||construction==null)return;
            bool show=ConstructionSystem.IsPlacing&&construction.SelectedKind!=null&&
                (!PortfolioSession.Active||PortfolioSession.Instance.HasBegun&&!PortfolioSession.Instance.IsPaused);
            canvasRoot.SetActive(show);if(!show)return;
            string selected=construction.SelectedKind;
            status.text="FORTALEZA  ·  "+ConstructionSystem.Label(selected);
            for(int i=0;i<cards.Length;i++)
            {
                string kind=FortressPieces.Palette[i];bool unlocked=!PortfolioSession.Active||PortfolioSession.Instance.CanBuild(kind);
                cards[i].interactable=unlocked&&!construction.IsMoving;FarmUiStyle.Button(cards[i],selected==kind);
                int count=construction.AvailablePlacements(kind);
                amounts[i].text=unlocked?"× "+FarmUiStyle.Quantity(count)+" disponibles":"Día 2";
                amounts[i].color=!unlocked?FarmUiStyle.Muted:count>0?FarmUiStyle.Positive:FarmUiStyle.Negative;
            }
            ConstructionSystem.Cost(selected,out var wood,out var stone,out var iron);
            costs[0].Set(inventory.Wood,wood);costs[1].Set(inventory.Stone,stone);
            costs[2].Set(inventory.GetComponent<AdventureProgress>()?.Data.iron??0,iron);
            costs[3].Set(inventory.GetAvailableItemCount("GoldOre"),FortressPieces.GoldCost(selected));
            costs[0].SetVisible(wood>0);costs[1].SetVisible(stone>0);costs[2].SetVisible(iron>0);costs[3].SetVisible(FortressPieces.GoldCost(selected)>0);
            int stock=inventory.PackedCount(selected);
            packed.text=stock>0?$"×{stock} en mochila\nSe usan primero":"Coste por pieza";
        }
        private void Update()
        {
            if(Time.unscaledTime<refreshAt)return;refreshAt=Time.unscaledTime+.15f;Refresh();
            if(panel!=null)FarmUiStyle.FitWindow(panel);
        }
        private static Text Label(Transform parent,string caption,float x,float y,float w,float h,int size)
        {
            var text=AdventureWindow.Rect(parent,caption,x,y,w,h).gameObject.AddComponent<Text>();
            FarmUiStyle.Text(text,size);text.text=caption;return text;
        }
        private static Button Button(Transform parent,string caption,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
        {
            var rect=AdventureWindow.Rect(parent,string.IsNullOrEmpty(caption)?"Elegir pieza":caption,x,y,w,h);
            rect.gameObject.AddComponent<Image>();var button=rect.gameObject.AddComponent<Button>();FarmUiStyle.Button(button);button.onClick.AddListener(action);
            Label(rect,caption,2,2,w-4,h-4,16).alignment=TextAnchor.MiddleCenter;return button;
        }
        private void OnDestroy()
        {
            if(construction!=null)construction.SelectionChanged-=Refresh;
            if(inventory!=null)inventory.InventoryChanged-=Refresh;
            if(canvasRoot!=null)Destroy(canvasRoot);
        }
    }
}
