using UnityEngine;
using UnityEngine.UI;
using SurvivorFarm.Runtime.Player;
namespace SurvivorFarm.Runtime.UI
{
    // Shared resource display for crafting and construction. Text stays numeric;
    // the existing resource sprite identifies the ingredient.
    public sealed class MaterialCostBadge
    {
        readonly Text count;
        readonly HudActionTooltip tooltip;
        readonly string resourceName;
        readonly RectTransform progress;
        public static MaterialCostBadge Create(Transform parent,string resource,float x,float y,float width=100)
        {
            var root=AdventureWindow.Rect(parent,resource+" requerido",x,y,width,32);
            root.gameObject.AddComponent<Image>().color=Color.clear;
            var tooltip=root.gameObject.AddComponent<HudActionTooltip>();
            var icon=AdventureWindow.Rect(root,"Icono",0,0,30,30).gameObject.AddComponent<Image>();
            string iconName=SurvivalItemCatalog.Find(resource)?.Icon??(resource=="Coins"?"Coin":resource);
            icon.sprite=FarmUiStyle.ItemIcon(iconName);icon.preserveAspect=true;icon.raycastTarget=false;
            var text=AdventureWindow.Rect(root,"Cantidad",34,2,width-34,28).gameObject.AddComponent<Text>();
            FarmUiStyle.Text(text,16,true);text.alignment=TextAnchor.MiddleLeft;
            var track=AdventureWindow.Rect(root,"Progreso de ingrediente",34,30,width-34,2);
            track.gameObject.AddComponent<Image>().color=FarmUiStyle.Control;
            var progress=AdventureWindow.Rect(track,"Disponible",0,0,width-34,2);
            var fill=progress.gameObject.AddComponent<Image>();fill.color=FarmUiStyle.Positive;fill.raycastTarget=false;
            return new MaterialCostBadge(text,tooltip,Name(resource),progress);
        }
        MaterialCostBadge(Text text,HudActionTooltip hint,string name,RectTransform fill){count=text;tooltip=hint;resourceName=name;progress=fill;}
        static string Name(string resource)
        {
            var item=SurvivalItemCatalog.Find(resource);if(item!=null)return item.Name;
            return resource switch {"Wood"=>"Madera","Stone"=>"Piedra","Iron"=>"Hierro","Food"=>"Raciones","Fruit"=>"Fruta","Coin"=>"Oro","Coins"=>"Oro","Campfire"=>"Fogata colocada",_=>resource};
        }
        public void Set(int owned,int required,bool stock=false)
        {
            count.text=stock?FarmUiStyle.Quantity(owned):FarmUiStyle.Quantity(owned)+"/"+FarmUiStyle.Quantity(required);
            count.color=stock||owned>=required?FarmUiStyle.Ink:FarmUiStyle.Negative;
            tooltip.Caption=resourceName+": "+owned+(stock?"":" / "+required+(owned<required?"\nFaltan "+(required-owned):"\nDisponible"));
            progress.parent.gameObject.SetActive(!stock&&required>0);
            progress.sizeDelta=new Vector2(((RectTransform)progress.parent).rect.width*(required>0?Mathf.Clamp01((float)owned/required):1),2);
        }
        public void SetVisible(bool visible)=>count.transform.parent.gameObject.SetActive(visible);
    }
}
