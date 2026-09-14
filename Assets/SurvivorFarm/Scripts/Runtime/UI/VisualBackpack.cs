using System;
using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    public sealed class VisualBackpack : MonoBehaviour
    {
        public sealed class Item
        {
            public string Key, Id, Name, Icon, Description;
            public int Category, Count;
            public FarmTool? Tool;
        }
        private PlayerInventory inventory;
        private PlayerToolUpgradeController upgrades;
        private InventoryPanelSystem owner;
        private readonly List<Item> items = new List<Item>();
        private readonly List<BackpackCell> cells = new List<BackpackCell>();
        private readonly List<Button> tabs = new List<Button>();
        private List<Item> visible = new List<Item>();
        private RectTransform content;
        private ScrollRect scroll;
        private Text detail, summary, empty;
        private Button use, sell, discard, quantity;
        private int amount=1;
        private bool selectionPinned;
        private Image selectedIcon, ghost;
        private Item selected;
        private string dragging;
        private int category;
        private Sprite panelSprite, slotSprite;
        public int StackCount => items.Count;
        public int VisibleCount => visible.Count;

        public void Configure(PlayerInventory source, PlayerToolUpgradeController toolUpgrades, InventoryPanelSystem controller)
        {
            inventory = source; upgrades = toolUpgrades; owner = controller;
            Build(); Refresh();
        }
        private Sprite Icon(string name) => BackpackActions.IsBuilding(name)?ConstructionSystem.SpriteFor(name):HouseSprites.Furniture(name)??FarmUiStyle.ItemIcon(name);
        private void Build()
        {
            if (content != null) return;
            foreach (Transform child in transform) child.gameObject.SetActive(false);
            var root = (RectTransform)transform;
            root.sizeDelta = new Vector2(850, 600);
            panelSprite = Icon("Panel"); slotSprite = Icon("Slot");
            FarmUiStyle.Frame(GetComponent<Image>());
            Label(root, "MOCHILA", 24, 18, 400, 32, 26);
            if(Core.PortfolioSession.Active)
            {
                Button(root,"Construir  [Z]",566,16,196,40,BeginConstruction);
                var inset=Rect(root,"Detalle de inventario",590,120,238,365).gameObject.AddComponent<Image>();
                FarmUiStyle.Frame(inset,true);inset.raycastTarget=false;
            }
            if(!Core.PortfolioSession.Active)FarmUiStyle.IconButton(Button(root, "Personaje [C]", 726, 16, 44, 40, owner.OpenEquipment),"Helmet","Personaje [C]");
            FarmUiStyle.CloseButton(Button(root, "Cerrar  [Esc]", 782, 16, 44, 40, owner.Close));
            string[] labels = Core.PortfolioSession.Active?new[]{"Todo","Comida","Materiales","Construcción"}:new[]{ "Todo", "Comida", "Materiales", "Gemas", "Elementos", "Varios" };
            int[] categoryIds = Core.PortfolioSession.Active?new[]{0,3,4,9}:new[]{ 0, 3, 4, 6, 7, 1 };
            for (int i = 0; i < labels.Length; i++)
            {
                int value = categoryIds[i];
                tabs.Add(Button(root, labels[i], 24 + i * 135, 66, 128, 38, () => SetCategory(value)));
            }
            var viewport = Rect(root, "Objetos", 24, 120, 558, 365);
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll = viewport.gameObject.AddComponent<ScrollRect>();
            content = Rect(viewport, "Contenido", 0, 0, 548, 365);
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35;
            scroll.onValueChanged.AddListener(_ => DrawCells());
            // Recycle only the visible rows, even when the backpack contains thousands of stacks.
            for (int i = 0; i < 24; i++)
            {
                var rect = Rect(content, "Casilla", 0, 0, 128, 104);
                var image = rect.gameObject.AddComponent<Image>(); image.sprite = slotSprite; image.type = Image.Type.Sliced;
                var cell = rect.gameObject.AddComponent<BackpackCell>(); cell.Owner = this;
                cell.Icon = Rect(rect, "Icono", 29, 9, 70, 70).gameObject.AddComponent<Image>();
                cell.Icon.preserveAspect = true; cell.Icon.raycastTarget = false;
                cell.Name = Label(rect, "", 5, 80, 118, 20, 13);cell.Name.alignment=TextAnchor.MiddleCenter;
                cell.Count = Label(rect, "", 77, 4, 46, 24, 17);cell.Count.alignment=TextAnchor.UpperRight;
                cell.Name.color = cell.Count.color = FarmUiStyle.Ink;
                cells.Add(cell);
            }
            empty = Label(viewport, "Todavía no hay objetos en esta categoría.", 30, 100, 490, 80, 20);
            selectedIcon = Rect(root, "Objeto seleccionado", 660, 140, 96, 96).gameObject.AddComponent<Image>();
            selectedIcon.preserveAspect = true; selectedIcon.raycastTarget = false;
            var detailScroll=FarmUiStyle.Scroll(root,"Detalle del objeto",598,242,230,100);
            detail=FarmUiStyle.Paragraph(detailScroll.content,"",0,218,17);
            use = Button(root, "Usar", 608, 347, 210, 36, UseSelected);
            quantity = Button(root,"Cantidad: 1",608,387,210,30,()=>{amount=amount==1?10:amount==10?99:1;Detail();});
            sell = Button(root,"Vender",608,423,210,32,()=>Act(true));
            discard = Button(root,"Descartar",608,459,210,32,()=>Act(false));
            summary = Label(root, "", 24, 495, 795, 26, 16);
            if(Core.PortfolioSession.Active)
            {
                Button(root,"Paleta de construcción",608,407,210,40,BeginConstruction);
                Label(root,"Selecciona una pieza y coloca varias seguidas.",608,452,210,36,14).color=FarmUiStyle.Muted;
                Label(root,"EXPLORA · REÚNE · FORTIFICA",24,545,795,24,15).color=FarmUiStyle.Accent;
            }
            ghost = Rect(root, "Arrastre", 0, 0, 48, 48).gameObject.AddComponent<Image>();
            ghost.preserveAspect = true; ghost.raycastTarget = false; ghost.gameObject.SetActive(false);
        }
        public void Refresh()
        {
            if (content == null || inventory == null) return;
            string previous = selected?.Key;
            items.Clear();
            Add("Wood", "Madera", inventory.Wood, 4); Add("Stone", "Piedra", inventory.Stone, 4);
            Add("Iron", "Hierro", inventory.GetComponent<AdventureProgress>()?.Data.iron ?? 0, 4);
            Add("Fruit", "Fruta", inventory.Fruit, 3); Add("Food", "Comida", inventory.Food, 3);
            if(Core.PortfolioSession.Active)Add("CommonSeeds","Semillas",inventory.CommonSeeds,4,"CommonSeeds","Acércate a un surco del huerto y pulsa E para plantar.");
            foreach (var definition in SurvivalItemCatalog.All.Where(item => !item.IsSeed))
                Add(definition.Id, definition.Name, inventory.GetItemCount(definition.Id), Core.PortfolioSession.Active?(definition.IsFood?3:4):(int)definition.Category, definition.Icon, definition.Description);
            foreach(var packed in inventory.PackedBuildings)Add(packed.kind,ConstructionSystem.Label(packed.kind),packed.count,Core.PortfolioSession.Active?9:4);
            var positions = new Dictionary<string, int>();
            foreach (string key in inventory.BackpackOrder ?? new string[0])
                if (key != null && !positions.ContainsKey(key)) positions[key] = positions.Count;
            var ordered = items.OrderBy(item => positions.TryGetValue(item.Key, out int rank) ? rank : int.MaxValue).ToArray();
            items.Clear(); items.AddRange(ordered);
            selected = items.FirstOrDefault(i => i.Key == previous);
            Filter(); Detail();
        }
        private void Add(string id, string name, int amount, int cat, string icon = null, string description = null)
        {
            for (int stack = 0; amount > 0; stack++)
            {
                int count = Mathf.Min(99, amount); amount -= count;
                items.Add(new Item { Key = id + ":" + stack, Id = id, Name = name, Icon = icon ?? id, Description = description, Category = cat, Count = count });
            }
        }
        public void SetCategory(int value)
        {
            category = value; scroll.verticalNormalizedPosition = 1; Filter();
        }
        private void Filter()
        {
            visible = items.Where(i => category == 0 || i.Category == category).ToList();
            content.sizeDelta = new Vector2(548, Mathf.Max(365, Mathf.Ceil(visible.Count / 4f) * 112));
            content.anchoredPosition = new Vector2(0, Mathf.Clamp(content.anchoredPosition.y, 0, content.sizeDelta.y - 365));
            summary.text = Core.PortfolioSession.Active?$"{visible.Count} pilas  ·  Arrastra para ordenar  ·  I / Esc para cerrar":$"{visible.Count} pilas · Oro: {inventory.Coins}";
            empty.gameObject.SetActive(visible.Count == 0);
            for (int i = 0; i < tabs.Count; i++) FarmUiStyle.Button(tabs[i],(Core.PortfolioSession.Active?new[]{0,3,4,9}:new[] { 0, 3, 4, 6, 7, 1 })[i] == category);
            DrawCells();
        }
        private void DrawCells()
        {
            int first = Mathf.Max(0, Mathf.FloorToInt(content.anchoredPosition.y / 112)) * 4;
            for (int i = 0; i < cells.Count; i++)
            {
                int index = first + i;
                var cell = cells[i]; cell.gameObject.SetActive(index < visible.Count);
                if (index >= visible.Count) { cell.Item = null; continue; }
                var item = visible[index]; cell.Item = item;
                ((RectTransform)cell.transform).anchoredPosition = new Vector2(index % 4 * 138, -(index / 4) * 112);
                cell.Icon.sprite = Icon(item.Icon); cell.Name.text = item.Name;
                cell.Count.text = item.Tool.HasValue ? "Nv. " + (upgrades != null ? upgrades.GetToolLevel(item.Tool.Value) : 1) : "×" + item.Count;
                cell.GetComponent<Image>().color = selected?.Key == item.Key ? new Color(.68f,.72f,.47f) : new Color(.4f,.49f,.45f);
                if(item.Id=="ReinforcedWall")cell.Icon.color=new Color(1,.86f,.58f);else cell.Icon.color=Color.white;
            }
        }
        public void Hover(Item item) { if(selectionPinned)return;Select(item);selectionPinned=false; }
        public void Select(Item item) { if(dragging!=null||item==null)return;selectionPinned=true; if(selected?.Key!=item.Key)amount=1; selected = item; Detail(); DrawCells(); }
        private void Detail()
        {
            selectedIcon.enabled = selected != null;
            selectedIcon.sprite = selected != null ? Icon(selected.Icon) : null;
            bool tool=selected?.Tool!=null;
            int count=selected==null?0:Mathf.Min(amount,selected.Count);
            string id = selected?.Id;
            bool food = id != null && (id == "Food" || id == "Fruit" || SurvivalItemCatalog.IsFood(id));
            string actionHint = selected?.Description;
            if (string.IsNullOrEmpty(actionHint))
                actionHint = tool ? "Herramienta permanente. Equípala en la barra." : BackpackActions.IsBuilding(id) ? "Elige dónde colocarlo." : food ? "+" + BackpackActions.FoodHealing(inventory,id) + " vida." : "Elige una acción abajo.";
            detail.text=selected==null?"Selecciona un objeto":selected.Name+"\n"+(tool?actionHint:"×"+selected.Count+"\n"+actionHint);
            float detailHeight=Mathf.Max(100,Mathf.Ceil(detail.preferredHeight)+8);
            detail.rectTransform.sizeDelta=new Vector2(218,detailHeight);
            var detailContent=(RectTransform)detail.transform.parent;
            detailContent.sizeDelta=new Vector2(218,detailHeight);
            detailContent.anchoredPosition=Vector2.zero;
            use.gameObject.SetActive(selected!=null);
            use.GetComponentInChildren<Text>().text=tool?"Equipar":selected!=null&&BackpackActions.IsBuilding(id)?"Colocar":food?"Comer 1":"Ver recetas";
            bool removable=!Core.PortfolioSession.Active&&selected!=null&&!tool&&BackpackActions.Price(id)>0;
            quantity.gameObject.SetActive(removable);sell.gameObject.SetActive(removable);discard.gameObject.SetActive(removable);
            quantity.GetComponentInChildren<Text>().text="Cantidad: "+count+" · cambiar";
            sell.GetComponentInChildren<Text>().text="Vender "+count+" · +"+(selected==null?0:count*BackpackActions.Price(id))+" oro";
            discard.GetComponentInChildren<Text>().text="Descartar "+count;
        }
        public void UseSelected()
        {
            var item=selected;if(item==null)return;
            if(item.Tool.HasValue){inventory.GetComponent<PlayerToolbelt>().Select(item.Tool.Value);owner.Close();return;}
            if(item.Id=="Food"||item.Id=="Fruit"||SurvivalItemCatalog.IsFood(item.Id))
            {if(!BackpackActions.Use(inventory,item.Id))FarmNotificationCenter.Show("Tienes la vida completa o no puedes comer ahora.");Refresh();return;}
            owner.Close();
            if(BackpackActions.IsBuilding(item.Id))BackpackActions.Use(inventory,item.Id);
            else FindFirstObjectByType<CraftingWindow>()?.Open();
        }
        private void BeginConstruction()
        {
            owner.Close();inventory.GetComponent<ConstructionSystem>()?.Begin("Fence");
        }
        void Act(bool selling)
        {
            var item=selected;if(item==null||item.Tool.HasValue)return;
            int count=Mathf.Min(amount,item.Count);bool done=selling?BackpackActions.Sell(inventory,item.Id,count):BackpackActions.Remove(inventory,item.Id,count);
            if(done&&!selling)FarmNotificationCenter.Show("Descartaste "+count+" × "+item.Name+".");
            Refresh();
        }
        public void BeginDrag(Item item) { if(item==null)return;dragging = item.Key; ghost.sprite = Icon(item.Icon); ghost.gameObject.SetActive(true); }
        public void Drag(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, e.position, e.pressEventCamera, out var point);
            ghost.rectTransform.localPosition = point; ghost.transform.SetAsLastSibling();
        }
        public void EndDrag() { dragging = null; ghost.gameObject.SetActive(false); }
        private void OnDisable() { selectionPinned=false;if (ghost != null) EndDrag(); }
        private void LateUpdate(){FarmUiStyle.FitWindow((RectTransform)transform);}
        public void Drop(Item target)
        {
            if (dragging == null || target == null || dragging == target.Key) return;
            int from = items.FindIndex(i => i.Key == dragging), to = items.FindIndex(i => i.Key == target.Key);
            if (from < 0 || to < 0) return;
            var moving = items[from]; items.RemoveAt(from); items.Insert(to, moving);
            inventory.BackpackOrder = items.Select(i => i.Key).ToArray();
            Filter();
        }
        private RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        private Text Label(Transform parent, string text, float x, float y, float w, float h, int size)
        {
            var t = Rect(parent, "Etiqueta", x,y,w,h).gameObject.AddComponent<Text>();
            t.text = text;FarmUiStyle.Text(t,size,true);t.alignment = TextAnchor.MiddleCenter; return t;
        }
        private Button Button(Transform parent, string text, float x, float y, float w, float h, UnityEngine.Events.UnityAction action)
        {
            var r = Rect(parent, text, x,y,w,h); var img = r.gameObject.AddComponent<Image>(); img.sprite = panelSprite; img.type = Image.Type.Sliced;
            var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = img;FarmUiStyle.Button(b); b.onClick.AddListener(action); Label(r,text,3,2,w-6,h-4,16); return b;
        }
    }
    public sealed class BackpackCell : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        public VisualBackpack Owner;
        public VisualBackpack.Item Item;
        public Image Icon;
        public Text Name, Count;
        public void OnPointerEnter(PointerEventData e) => Owner.Hover(Item);
        public void OnPointerClick(PointerEventData e) { Owner.Select(Item); }
        public void OnBeginDrag(PointerEventData e) { Owner.BeginDrag(Item); Owner.Drag(e); }
        public void OnDrag(PointerEventData e) => Owner.Drag(e);
        public void OnEndDrag(PointerEventData e) => Owner.EndDrag();
        public void OnDrop(PointerEventData e) => Owner.Drop(Item);
    }
}
