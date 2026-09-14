using System;
using System.IO;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>Session UI augments the original hearts, weapon slots, backpack and crafting window.</summary>
    public sealed class SliceHud : MonoBehaviour
    {
        private PortfolioSession session;
        private GameObject canvasRoot,hud,overlay;
        private RectTransform card;
        private Text phase,objective,core,controls,bossText,title,description;
        private Text comboText;
        private RectTransform stockPanel;
        private readonly MaterialCostBadge[] stockBadges=new MaterialCostBadge[6];
        private Image coreFill,bossFill;
        private GameObject bossPanel;
        private Button ready;
        private readonly System.Collections.Generic.List<GameObject> buttons=new System.Collections.Generic.List<GameObject>();
        private float refreshAt;
        private CanvasGroup fade;
        private static readonly Color Surface=new Color(.075f,.115f,.12f,.94f);
        private static readonly Color Paper=new Color(.94f,.9f,.77f);
        private static readonly Color Gold=new Color(.95f,.72f,.35f);
        private static readonly Color Muted=new Color(.69f,.77f,.73f);

        public void Configure(PortfolioSession owner)
        {
            session=owner;
            canvasRoot=new GameObject("Portfolio · session HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=canvasRoot.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=90;
            var scaler=canvasRoot.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            hud=Rect(canvasRoot.transform,"Session readouts",Vector2.zero,Vector2.zero,Vector2.zero).gameObject;
            Stretch(hud.GetComponent<RectTransform>());
            phase=Label(Panel(hud.transform,"Fase",new Vector2(.5f,1),new Vector2(0,-12),new Vector2(402,58)),"",20,Gold);
            var goals=Panel(hud.transform,"Objetivo",Vector2.one,new Vector2(-12,-12),new Vector2(350,92));
            objective=Label(goals,"",17,Paper);objective.alignment=TextAnchor.MiddleLeft;Inset(objective.rectTransform,16,10);
            var well=Panel(hud.transform,"Estado del pozo",new Vector2(0,1),new Vector2(12,-72),new Vector2(204,58));
            core=Label(well,"",15,Paper);core.rectTransform.offsetMin=new Vector2(12,20);core.rectTransform.offsetMax=new Vector2(-12,-4);
            coreFill=Bar(well,new Vector2(12,10),new Vector2(180,5),new Color(.5f,.8f,.65f));
            stockPanel=Panel(hud.transform,"Recursos con iconos",new Vector2(1,0),new Vector2(-12,12),new Vector2(280,82));
            string[] stockIds={"Wood","Stone","Iron","GoldOre","Food","CommonSeeds"};
            for(int i=0;i<stockBadges.Length;i++)stockBadges[i]=MaterialCostBadge.Create(stockPanel,stockIds[i],10+i%3*90,7+i/3*36,82);
            var hints=Rect(hud.transform,"Controles",new Vector2(.5f,0),new Vector2(0,90),new Vector2(690,28));
            controls=Label(hints,"",14,Paper);controls.gameObject.AddComponent<Shadow>().effectDistance=new Vector2(1,-1);
            comboText=Label(Panel(hud.transform,"Combo de espada",new Vector2(.5f,0),new Vector2(0,121),new Vector2(234,29)),"",15,Gold);
            ready=Button(hud.transform,"Listo para la noche",new Vector2(1,1),new Vector2(-12,-112),new Vector2(198,34),session.PrepareNow);
            var bossRect=Panel(hud.transform,"Jefe",new Vector2(.5f,1),new Vector2(0,-82),new Vector2(490,69));
            bossPanel=bossRect.gameObject;bossText=Label(bossRect,"",17,Paper);bossText.rectTransform.offsetMin=new Vector2(10,18);bossText.rectTransform.offsetMax=new Vector2(-10,-4);
            bossFill=Bar(bossRect,new Vector2(18,9),new Vector2(454,6),new Color(.9f,.36f,.35f));
            overlay=Rect(canvasRoot.transform,"Portada y pausa",Vector2.zero,Vector2.zero,Vector2.zero).gameObject;Stretch(overlay.GetComponent<RectTransform>());
            overlay.AddComponent<Image>().color=new Color(.025f,.045f,.055f,.72f);
            fade=overlay.AddComponent<CanvasGroup>();
            card=Panel(overlay.transform,"Portada",new Vector2(.5f,.5f),Vector2.zero,new Vector2(724,528));
            var eyebrow=Label(Rect(card,"Edición",new Vector2(.5f,1),new Vector2(0,-28),new Vector2(660,24)),"UNA GRANJA · TRES NOCHES",14,Gold);
            title=Label(Rect(card,"Título",new Vector2(.5f,1),new Vector2(0,-72),new Vector2(660,106)),"SURVIVAL FARM",48,Paper);
            description=Label(Rect(card,"Descripción",new Vector2(.5f,1),new Vector2(0,-190),new Vector2(614,108)),"",18,Muted);
            var footer=Label(Rect(card,"Pie",new Vector2(.5f,0),new Vector2(0,18),new Vector2(660,24)),"WASD mover  ·  E interactuar  ·  Clic atacar  ·  Espacio esquivar",14,Muted);
            HideOldSessionReadouts();
            RefreshOverlay();
        }
        private void HideOldSessionReadouts()
        {
            var original=FindFirstObjectByType<OriginalSpriteHud>();
            if(original==null)return;
            if(original.Day!=null)original.Day.transform.parent.gameObject.SetActive(false);
            foreach(var item in original.GetComponentsInChildren<RectTransform>(true))
                if(item.name=="Mission"||item.name=="Diario [J]"||item.name=="Personaje [C]"||item.name=="HUD Settings")item.gameObject.SetActive(false);
        }
        private void Update()
        {
            if(fade!=null&&overlay.activeSelf)fade.alpha=Mathf.MoveTowards(fade.alpha,1,Time.unscaledDeltaTime*1.1f);
            if(session==null||!session.IsReady||Time.unscaledTime<refreshAt)return;
            refreshAt=Time.unscaledTime+.1f;
            bool blocked=InventoryPanelSystem.IsOpen&&!ConstructionSystem.IsPlacing;
            hud.SetActive(session.HasBegun&&!overlay.activeSelf&&!blocked);
            string label=session.Phase==SlicePhase.Day?"PREPARA LA GRANJA":session.Phase==SlicePhase.Preparation?"CAE LA NOCHE":
                session.Phase==SlicePhase.Night?"DEFIENDE LA GRANJA":session.Phase==SlicePhase.Dawn?"AMANECER":session.Phase==SlicePhase.Boss?"ÚLTIMA DEFENSA":"EL CUSTODIO";
            phase.text=$"DÍA {session.Day} / 3   ·   {label}"+(session.Remaining>0?$"\n{Mathf.CeilToInt(session.Remaining)/60:00}:{Mathf.CeilToInt(session.Remaining)%60:00}":"");
            objective.text=session.Objective;
            if(!session.InCombat&&Vector2.Distance(session.Player.transform.position,session.Core.transform.position)>16)
                objective.text="EXPLORA Y FORTIFICA\n"+Mathf.CeilToInt(Vector2.Distance(session.Player.transform.position,session.Core.transform.position))+" m hasta el pozo · regresa antes de la noche";
            core.text=$"POZO   {session.Core.Health} / {session.Core.Maximum}";
            coreFill.fillAmount=session.Core.Health/(float)session.Core.Maximum;
            var player=session.Player;
            var combo=player.GetComponent<ComboController>();
            bool sword=player.GetComponent<PlayerToolbelt>()?.SelectedTool==FarmTool.Sword;
            comboText.transform.parent.gameObject.SetActive(sword&&!ConstructionSystem.IsPlacing);
            comboText.text=combo!=null&&combo.StepNumber>0?$"CORTE {combo.StepNumber} / 3"+(combo.StepNumber==3?" · REMATE":" · CLIC para seguir"):"CLIC · 1 → 2 → 3 REMATE";
            stockPanel.gameObject.SetActive(!ConstructionSystem.IsPlacing);
            int[] counts={player.Wood,player.Stone,player.GetComponent<AdventureProgress>().Data.iron,player.GetAvailableItemCount("GoldOre"),player.Food,player.CommonSeeds};
            for(int i=0;i<stockBadges.Length;i++)stockBadges[i].Set(counts[i],0,true);
            ready.gameObject.SetActive(session.Phase==SlicePhase.Day);
            controls.text=ConstructionSystem.IsPlacing?"":
                session.Day==1?"E interactuar   ·   I mochila   ·   F recetas   ·   Z construir   ·   Q curar":
                "Z construir   ·   X trampa   ·   C ballesta   ·   I mochila   ·   Q curar";
            var boss=session.Raids.Boss;
            bossPanel.SetActive(boss!=null&&(session.Phase==SlicePhase.Boss||session.Phase==SlicePhase.BossIntro));
            if(boss!=null)
            {
                bossText.text=$"EL CUSTODIO   {boss.CurrentHealth}/{boss.MaximumHealth}\n{session.Raids.BossPattern.Status}";
                bossFill.fillAmount=boss.CurrentHealth/(float)boss.MaximumHealth;
                bossFill.color=session.Raids.BossPattern.IsExposed?new Color(.5f,.85f,.7f):new Color(.9f,.36f,.35f);
            }
        }
        public void RefreshOverlay()
        {
            if(overlay==null)return;
            foreach(var button in buttons)Destroy(button);buttons.Clear();
            bool show=!session.HasBegun||session.IsPaused||session.Phase==SlicePhase.Victory||session.Phase==SlicePhase.Defeat;
            overlay.SetActive(show);if(!show)return;
            fade.alpha=session.Phase==SlicePhase.Victory?0:1;
            bool victory=session.Phase==SlicePhase.Victory,defeat=session.Phase==SlicePhase.Defeat;
            title.text=victory?"UN NUEVO AMANECER":defeat?"LA GRANJA TE NECESITA":session.IsPaused?"UN RESPIRO":"SURVIVAL FARM";
            title.fontSize=victory||defeat?36:48;
            description.text=victory?$"El Custodio descansa. El Corazón del Valle sigue a salvo.\n\nTres noches superadas · {TimeSpan.FromSeconds(session.Elapsed):mm\\:ss}\nGracias por jugar esta pequeña historia.":
                defeat?session.EndingReason+"\n\nContinúa desde la última preparación guardada.":
                session.IsPaused?"Tu granja espera.\nLa partida se guarda durante la preparación de cada día.":
                "De día, cultiva y prepara tus defensas.\nDe noche, protege el pozo de las criaturas del valle.\nAl tercer anochecer, despierta su guardián.";
            if(!session.HasBegun)
            {
                AddButton("Empezar · unos 20 minutos",-334,session.BeginNewGame);
                var save=FindFirstObjectByType<GameSaveSystem>();
                if(save!=null&&File.Exists(save.SaveFile))AddButton("Continuar preparación guardada",-391,()=>session.ContinueGame());
                AddButton("Salir",-448,session.Quit);
            }
            else if(session.IsPaused)
            {
                AddButton("Volver a la granja",-292,()=>session.Pause(false));
                AddButton(AudioListener.volume>0?"Silenciar audio":"Activar audio",-342,()=>{AudioListener.volume=AudioListener.volume>0?0:1;RefreshOverlay();});
                AddButton(CombatTimeFeedback.ReducedMotion?"Impacto de cámara: reducido":"Impacto de cámara: normal",-392,()=>{CombatTimeFeedback.ReducedMotion=!CombatTimeFeedback.ReducedMotion;RefreshOverlay();});
                AddButton("Volver al inicio",-442,session.ReturnToTitle);
            }
            else
            {
                AddButton(victory?"Volver al inicio":"Inicio · continuar preparación",-354,session.ReturnToTitle);
                AddButton("Salir",-414,session.Quit);
            }
        }
        private void AddButton(string text,float y,UnityEngine.Events.UnityAction action)=>
            buttons.Add(Button(card,text,new Vector2(.5f,1),new Vector2(0,y),new Vector2(422,44),action).gameObject);
        private static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var root=new GameObject(name,typeof(RectTransform));root.transform.SetParent(parent,false);
            var r=root.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=position;r.sizeDelta=size;return r;
        }
        private static RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var r=Rect(parent,name,anchor,position,size);var image=r.gameObject.AddComponent<Image>();
            image.sprite=Resources.Load<Sprite>("BackpackIcons/Panel");image.type=Image.Type.Sliced;image.color=Surface;image.raycastTarget=false;return r;
        }
        private static Text Label(RectTransform parent,string value,int size,Color color)
        {
            var r=Rect(parent,"Texto",Vector2.zero,Vector2.zero,Vector2.zero);Stretch(r);
            var text=r.gameObject.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=size;text.color=color;text.text=value;
            text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;return text;
        }
        private static Image Bar(Transform parent,Vector2 position,Vector2 size,Color color)
        {
            var r=Rect(parent,"Barra",Vector2.zero,position,size);var image=r.gameObject.AddComponent<Image>();
            image.sprite=Resources.Load<Sprite>("BackpackIcons/Panel");image.type=Image.Type.Filled;image.fillMethod=Image.FillMethod.Horizontal;image.color=color;image.raycastTarget=false;return image;
        }
        private static Button Button(Transform parent,string label,Vector2 anchor,Vector2 position,Vector2 size,UnityEngine.Events.UnityAction action)
        {
            var r=Panel(parent,label,anchor,position,size);var image=r.GetComponent<Image>();image.color=new Color(.23f,.36f,.31f);image.raycastTarget=true;
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);
            var colors=button.colors;colors.highlightedColor=new Color(1.25f,1.2f,1.1f);colors.pressedColor=new Color(.7f,.8f,.7f);button.colors=colors;
            Label(r,label,17,Paper);return button;
        }
        private static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        private static void Inset(RectTransform r,float x,float y){r.offsetMin=new Vector2(x,y);r.offsetMax=new Vector2(-x,-y);}
        private void OnDestroy(){if(canvasRoot!=null)Destroy(canvasRoot);}
    }
}
