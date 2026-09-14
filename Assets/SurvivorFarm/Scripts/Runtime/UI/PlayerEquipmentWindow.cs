using System;
using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    public sealed class PlayerEquipmentWindow : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }
        private PlayerInventory inventory;
        private InventoryPanelSystem backpack;
        private RectTransform root, storage;
        private Image portrait, ghost;
        private readonly List<EquipmentCell> slots = new List<EquipmentCell>();
        private readonly List<EquipmentCell> choices = new List<EquipmentCell>();
        private Text description, empty, comparisonTitle, comparison, totals;
        private ScrollRect comparisonScroll;
        private string previewId;
        private PlayerAnimationLibrary library;
        private int filter = -1;
        private string dragging;
        public int AvailableCount => choices.Count(c => c.gameObject.activeSelf);
        public void Configure(PlayerInventory source, InventoryPanelSystem menu, Transform canvas)
        {
            inventory = source; backpack = menu;
            library = Resources.Load<PlayerAnimationLibrary>("JoshAnimationLibrary");
            root = Rect(canvas,"Player Equipment",0,0,900,640);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(.5f,.5f); root.anchoredPosition = Vector2.zero;
            FarmUiStyle.Frame(root.gameObject.AddComponent<Image>());
            Label(root,"PERSONAJE · EQUIPAMIENTO",24,14,510,35,24);
            FarmUiStyle.IconButton(Button(root,"Mochila [B]",774,16,44,40,() => { Close(); backpack.Toggle(); }),"Backpack","Mochila [B]");
            FarmUiStyle.CloseButton(Button(root,"Cerrar [Esc]",830,16,44,40,Close));
            for (int i=0;i<8;i++)
            {
                int x = i<4 ? 26 : 624;
                var cell = MakeCell(root,x,94+(i%4)*70,250,62);
                cell.Slot = i; slots.Add(cell);
            }
            var portraitPanel = Rect(root,"Retrato del jugador",310,84,280,90);
            portrait = Rect(portraitPanel,"Player original",95,0,90,90).gameObject.AddComponent<Image>();
            portrait.preserveAspect=true; portrait.raycastTarget=false;
            comparisonTitle=Label(root,"Bonificaciones de equipo",302,178,296,52,17);
            comparisonScroll=FarmUiStyle.Scroll(root,"Comparacion de equipo",302,238,296,132);
            comparison=FarmUiStyle.Paragraph(comparisonScroll.content,"",0,284,16);
            Button(root,"Todo el equipo",26,383,158,32,() => {filter=-1;previewId=null;storage.anchoredPosition=Vector2.zero;Refresh();});
            description = Label(root,"",192,380,680,36,15);
            var scroll=FarmUiStyle.Scroll(root,"Equipo disponible",26,429,848,145);
            var viewport=scroll.viewport;storage=scroll.content;storage.name="Lista de equipo";
            empty=Label(viewport,"No tienes equipo disponible para esta casilla.",10,30,820,70,18);
            totals=Label(root,"",26,588,848,32,17);
            ghost=Rect(root,"Equipo en arrastre",0,0,48,48).gameObject.AddComponent<Image>();ghost.raycastTarget=false;ghost.preserveAspect=true;ghost.gameObject.SetActive(false);
            // A permanent HUD entry, next to the backpack, also works without keyboard shortcuts.
            var hud=canvas.GetComponentInChildren<OriginalSpriteHud>(true);
            if(hud!=null)
            {
                var button=Button(hud.transform,"Personaje [C]",16,0,132,36,() => {backpack.Close();Open();});
                var rect=button.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0,0);rect.pivot=new Vector2(0,0);rect.anchoredPosition=new Vector2(16,64);
                hud.RequestStyleRefresh();
            }
            inventory.InventoryChanged += Refresh;
            root.gameObject.SetActive(false); Refresh();
        }
        public void Open()
        {
            VillageDialogueWindow.CloseActive(); SimpleShopSystem.CloseActive(); GetComponent<AdventureWindow>()?.Close(); inventory.GetComponent<ConstructionSystem>()?.Cancel(); backpack.Close(); GetComponent<CraftingWindow>()?.Close(); IsOpen=true; inventory.GetComponent<PlayerMovementController>()?.StopMovement();
            root.gameObject.SetActive(true); root.SetAsLastSibling(); filter=-1;previewId=null; Refresh();FarmUiStyle.FitWindow(root);
        }
        public void Close() { IsOpen=false; if(root!=null)root.gameObject.SetActive(false); EndDrag(); }
        private void OnDisable() { Close(); }
        private void OnDestroy() { if(inventory!=null)inventory.InventoryChanged-=Refresh; if(root!=null)Destroy(root.gameObject); }
        private void Update()
        {
            if(PlayerRespawnController.MenuOpen){Close();return;}
            if(Input.GetKeyDown(KeyCode.C)) { if(IsOpen)Close();else Open(); }
            if(Input.GetKeyDown(KeyCode.Escape) && IsOpen)Close();
            if(!IsOpen || root==null)return;
            FarmUiStyle.FitWindow(root);
            var idle=library?.Find("Idle");
            if(idle!=null)portrait.sprite=library.Frame(idle,0,(int)(Time.unscaledTime*idle.FramesPerSecond)%idle.Frames);
        }
        public void Refresh()
        {
            if(root==null)return;
            for(int i=0;i<slots.Count;i++)
            {
                var cell=slots[i]; var definition=EquipmentItems.Find(inventory.EquippedEquipment[i]);
                cell.Id=definition?.Id;
                cell.Icon.sprite=definition!=null?Icon(definition.Icon):null;cell.Icon.enabled=definition!=null;
                cell.Label.text=EquipmentItems.SlotNames[i]+"\n"+(definition?.Name??"Vacío");
                cell.GetComponent<Image>().color=filter==i?FarmUiStyle.Accent:Color.white;
            }
            var available=EquipmentItems.All.Where(d=>inventory.OwnsEquipment(d.Id)&&(filter<0||d.Fits(filter))).ToArray();
            while(choices.Count<available.Length) choices.Add(MakeCell(storage,0,0,270,90));
            for(int i=0;i<choices.Count;i++)
            {
                var cell=choices[i];cell.gameObject.SetActive(i<available.Length);if(i>=available.Length)continue;
                var item=available[i];cell.Id=item.Id;cell.Slot=-1;
                ((RectTransform)cell.transform).anchoredPosition=new Vector2(i%3*282,-(i/3)*98);
                cell.Icon.rectTransform.anchoredPosition=new Vector2(10,-21);cell.Icon.rectTransform.sizeDelta=new Vector2(44,44);
                cell.Label.rectTransform.anchoredPosition=new Vector2(64,-8);cell.Label.rectTransform.sizeDelta=new Vector2(194,74);
                cell.Icon.sprite=Icon(item.Icon);cell.Label.fontSize=16;cell.Label.text=item.Name+"\n"+(Array.IndexOf(inventory.EquippedEquipment,item.Id)>=0?"Equipado":"Disponible");
            }
            storage.sizeDelta=new Vector2(836,Mathf.Max(145,Mathf.Ceil(available.Length/3f)*98));
            storage.anchoredPosition=new Vector2(0,Mathf.Clamp(storage.anchoredPosition.y,0,storage.sizeDelta.y-145));
            empty.gameObject.SetActive(available.Length==0);
            description.text=filter<0?$"Equipo que posees: {available.Length}":"Para: "+EquipmentItems.SlotNames[filter]+$" · {available.Length} disponibles";
            totals.text=$"Defensa {Mathf.RoundToInt(inventory.ArmorReduction*100)}% · Velocidad {Mathf.RoundToInt(inventory.MovementBonus*100)}%";
            Preview(previewId);
        }
        public void Preview(string id)
        {
            previewId=id;var candidate=EquipmentItems.Find(id);
            if(candidate==null)
            {
                comparisonTitle.text="Bonificaciones de equipo";
                SetComparison(filter>=0?EquipmentItems.Effect(inventory.EquippedEquipment[filter]):"");return;
            }
            int target=UiEquipmentComparison.TargetSlot(inventory.EquippedEquipment,candidate,filter);
            var current=EquipmentItems.Find(inventory.EquippedEquipment[target]);
            int existing=Array.IndexOf(inventory.EquippedEquipment,id);
            comparisonTitle.text=candidate.Name;
            string text="Casilla: "+EquipmentItems.SlotNames[target]+"\n";
            if(candidate.Weapon.HasValue)text+="Arma: "+PlayerToolbelt.GetDisplayName(candidate.Weapon.Value)+"\n";
            text+=(current==null?"Casilla vacía":"Sustituye: "+current.Name)+"\n\n";
            if(existing==target)text="Equipado en "+EquipmentItems.SlotNames[target]+"\n\n"+EquipmentItems.Effect(id);
            else if(existing>=0)text+="Se mueve desde "+EquipmentItems.SlotNames[existing]+"\n"+UiEquipmentComparison.Differences(current,null);
            else text+="Cambio de bonificaciones\n"+UiEquipmentComparison.Differences(current,candidate);
            SetComparison(text);
        }
        void SetComparison(string text)
        {
            comparison.text=text;
            float height=Mathf.Max(132,Mathf.Ceil(comparison.preferredHeight)+8);
            comparison.rectTransform.sizeDelta=new Vector2(284,height);
            comparisonScroll.content.sizeDelta=new Vector2(284,height);
            comparisonScroll.verticalNormalizedPosition=1;
        }
        public void Click(EquipmentCell cell, bool right)
        {
            if(cell.Slot>=0)
            {
                if(right)inventory.Unequip(cell.Slot);
                else {filter=cell.Slot;previewId=cell.Id;storage.anchoredPosition=Vector2.zero;Refresh();}
            }
            else if(!right)
            {
                var definition=EquipmentItems.Find(cell.Id);
                if(definition==null)return;
                int slot=UiEquipmentComparison.TargetSlot(inventory.EquippedEquipment,definition,filter);
                inventory.Equip(cell.Id,slot);
            }
        }
        public void BeginDrag(EquipmentCell cell)
        {
            dragging=cell.Id;if(dragging==null)return;ghost.sprite=Icon(EquipmentItems.Find(dragging).Icon);ghost.gameObject.SetActive(true);
        }
        public void Drag(PointerEventData e)
        {
            if(dragging==null)return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root,e.position,e.pressEventCamera,out var point);
            ghost.rectTransform.localPosition=point;ghost.transform.SetAsLastSibling();
        }
        public void EndDrag(){dragging=null;if(ghost!=null)ghost.gameObject.SetActive(false);}
        public void Drop(EquipmentCell cell)
        {
            if(dragging==null||cell.Slot<0)return;
            if(!inventory.Equip(dragging,cell.Slot))description.text="Ese objeto no corresponde a esta casilla.";
        }
        private EquipmentCell MakeCell(Transform parent,float x,float y,float w,float h)
        {
            var rect=Rect(parent,"Casilla de equipo",x,y,w,h);var image=rect.gameObject.AddComponent<Image>();image.sprite=Icon("Slot");image.type=Image.Type.Sliced;
            var cell=rect.gameObject.AddComponent<EquipmentCell>();cell.Owner=this;
            cell.Icon=Rect(rect,"Icono",9,9,44,44).gameObject.AddComponent<Image>();cell.Icon.preserveAspect=true;cell.Icon.raycastTarget=false;
            cell.Label=Label(rect,"",61,4,w-66,h-8,16);cell.Label.color=FarmUiStyle.Ink;return cell;
        }
        private Sprite Icon(string name)=>Resources.Load<Sprite>("BackpackIcons/"+name);
        private RectTransform Rect(Transform p,string n,float x,float y,float w,float h)
        {
            var r=new GameObject(n,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        private Text Label(Transform p,string value,float x,float y,float w,float h,int size)
        {
            var t=Rect(p,"Etiqueta",x,y,w,h).gameObject.AddComponent<Text>();FarmUiStyle.Text(t,size,true);t.text=value;
            t.alignment=TextAnchor.MiddleCenter;return t;
        }
        private Button Button(Transform p,string value,float x,float y,float w,float h,UnityEngine.Events.UnityAction action)
        {
            var r=Rect(p,value,x,y,w,h);var img=r.gameObject.AddComponent<Image>();img.sprite=Icon("Panel");img.type=Image.Type.Sliced;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=img;FarmUiStyle.Button(b);b.onClick.AddListener(action);Label(r,value,3,2,w-6,h-4,16);return b;
        }
    }
    public sealed class EquipmentCell : MonoBehaviour,IPointerEnterHandler,IPointerClickHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IDropHandler
    {
        public PlayerEquipmentWindow Owner; public int Slot=-1;public string Id;public Image Icon;public Text Label;
        public void OnPointerEnter(PointerEventData e){if(Slot<0)Owner.Preview(Id);}
        public void OnPointerClick(PointerEventData e){if(!e.dragging)Owner.Click(this,e.button==PointerEventData.InputButton.Right);}
        public void OnBeginDrag(PointerEventData e){Owner.BeginDrag(this);Owner.Drag(e);}
        public void OnDrag(PointerEventData e)=>Owner.Drag(e);
        public void OnEndDrag(PointerEventData e)=>Owner.EndDrag();
        public void OnDrop(PointerEventData e)=>Owner.Drop(this);
    }
}
