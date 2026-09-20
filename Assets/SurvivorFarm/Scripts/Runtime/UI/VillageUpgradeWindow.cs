using System;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>One local management panel for repairs, village tiers and hired defenders.</summary>
    public sealed class VillageUpgradeWindow : MonoBehaviour
    {
        public static bool IsOpen {get;private set;}
        private VillageProgression progression;
        private GameObject canvas;
        private RectTransform panel,content;
        private Text wallet,status;
        private Button[] tabs;
        private int tab;
        private float nextRefresh;
        private VillageHouseHealth selectedHouse;
        public void Configure(VillageProgression owner){if(progression!=null)progression.Changed-=Refresh;progression=owner;progression.Changed+=Refresh;}
        public void Open(VillageHouseHealth house)
        {
            if(progression==null||!progression.CanManage)return;
            selectedHouse=house;tab=house!=null?1:0;
            if(canvas==null)Build();canvas.SetActive(true);IsOpen=true;Refresh();
            GetComponent<PlayerMovementController>()?.StopMovement();GetComponent<PlayerCombatController>()?.CancelMelee();
        }
        public void Close(){IsOpen=false;if(canvas!=null)canvas.SetActive(false);}
        private void Update()
        {
            if(!IsOpen)return;
            if(Input.GetKeyDown(KeyCode.Escape)||progression==null||!progression.CanManage){Close();return;}
            if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.4f;UpdateReadout();FarmUiStyle.FitWindow(panel);}
        }
        private void Build()
        {
            canvas=MasteryWindow.CreateCanvas("Mejoras de Raízclara",116);
            panel=AdventureWindow.Rect(canvas.transform,"Administrar el pueblo",0,0,900,596);
            panel.anchorMin=panel.anchorMax=panel.pivot=Vector2.one*.5f;panel.anchoredPosition=Vector2.zero;
            FarmUiStyle.Frame(panel.gameObject.AddComponent<Image>());
            MasteryWindow.Label(panel,"MEJORAS DE RAÍZCLARA",24,18,790,35,24);
            FarmUiStyle.CloseButton(MasteryWindow.Button(panel,"×",834,18,42,36,Close));
            wallet=MasteryWindow.Label(panel,"",24,59,850,28,16);
            tabs=new Button[3];string[] names={"Pozo y reclutamiento","Casas","Guardias"};
            for(int i=0;i<3;i++){int index=i;tabs[i]=MasteryWindow.Button(panel,names[i],24+i*286,101,278,40,()=>{tab=index;selectedHouse=null;Refresh();});}
            content=FarmUiStyle.Scroll(panel,"Inversiones del pueblo",24,153,852,365).content;
            status=MasteryWindow.Label(panel,"",24,527,852,31,15);status.color=FarmUiStyle.Accent;
            MasteryWindow.Label(panel,"El pozo atiende a los vecinos cercanos en calma. La guardia combate mientras exploras.",24,565,852,20,12).color=FarmUiStyle.Muted;
            FarmUiStyle.FitWindow(panel);
        }
        private void UpdateReadout()
        {
            if(wallet==null||progression==null||progression.Player==null)return;
            var stock=progression.Player;
            wallet.text=$"{stock.Coins} oro   ·   {stock.Wood} madera   ·   {stock.Stone} piedra   ·   {stock.Food} raciones";
            status.text=progression.LastMessage;
        }
        private void Refresh()
        {
            if(content==null||progression==null)return;
            foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            for(int i=0;i<tabs.Length;i++)FarmUiStyle.Button(tabs[i],tab==i);
            UpdateReadout();float y=0;
            if(tab==0)
            {
                Card("Well","POZO · NIVEL "+progression.WellLevel,
                    $"Aloja {progression.GuardCapacity} guardias. Recupera {progression.WellLevel} de vida cada 8 s\na los vecinos próximos cuando no hay enemigos.",y,150);
                Purchase("Mejorar pozo",progression.WellCost,16,y+96,390,()=>progression.UpgradeWell(),progression.WellLevel<3);y+=164;
                Card("Sword",$"RECLUTAR GUARDIA · {progression.Guards.Count}/{progression.GuardCapacity}",
                    "Patrulla el pueblo, persigue amenazas cercanas y pelea cuerpo a cuerpo.\nMejora su fuerza y resistencia desde la pestaña Guardias.",y,150);
                Purchase("Reclutar",$"{progression.RecruitCost} oro · 2 raciones",16,y+96,390,()=>progression.Recruit(),progression.Guards.Count<progression.GuardCapacity);y+=164;
            }
            else if(tab==1)
            {
                foreach(var house in progression.Houses)
                {
                    var selected=house;
                    Card("Wood",VillageProgression.HouseTitle(house)+(house==selectedHouse?"  ←":""),
                        $"Nivel {house.Level} · {house.Health}/{house.Maximum} resistencia   |   Mejora: {progression.HouseCost(house)}",y,122);
                    Purchase("Reparar +8","2 madera",16,y+73,235,()=>progression.RepairHouse(selected),house.Health<house.Maximum);
                    Purchase("Subir a nivel "+(house.Level+1),"",268,y+73,310,()=>progression.UpgradeHouse(selected),house.Level<3&&house.Health==house.Maximum);y+=134;
                }
            }
            else
            {
                foreach(var guard in progression.Guards)
                {
                    var selected=guard;
                    Card("Sword",guard.DisplayName+(guard.Body.IsAlive?" · GUARDIA":" · CAÍDO"),
                        $"Ataque {guard.Damage} · Vida {guard.Body.Health}/{guard.Body.Maximum} · Armadura {guard.Body.Armor}\nFuerza {guard.Strength}/3: {progression.StrengthCost(guard)}   |   Resistencia {guard.Toughness}/3: {progression.ToughnessCost(guard)}",y,144);
                    Purchase("Mejorar fuerza","",16,y+96,246,()=>progression.TrainStrength(selected),guard.Strength<3&&guard.Body.IsAlive);
                    Purchase("Mejorar resistencia","",280,y+96,246,()=>progression.TrainToughness(selected),guard.Toughness<3&&guard.Body.IsAlive);
                    Purchase("Recuperar · 12 oro + 2 raciones","",544,y+96,268,()=>progression.Recover(selected),!guard.Body.IsAlive);y+=156;
                }
                if(progression.Guards.Count==0)
                {
                    MasteryWindow.Label(content,"Aún no tienes guardias. Recluta el primero junto al pozo.",20,36,788,65,20);y=140;
                }
            }
            content.sizeDelta=new Vector2(840,Mathf.Max(365,y));
        }
        private void Card(string icon,string title,string detail,float y,float height)
        {
            var card=AdventureWindow.Rect(content,title,0,y,834,height);FarmUiStyle.Frame(card.gameObject.AddComponent<Image>(),true);
            var image=AdventureWindow.Rect(card,"Icono",16,14,42,42).gameObject.AddComponent<Image>();image.sprite=FarmUiStyle.ItemIcon(icon);image.preserveAspect=true;image.raycastTarget=false;
            MasteryWindow.Label(card,title,74,10,744,29,19);
            var text=MasteryWindow.Label(card,detail,74,42,744,48,14);text.color=FarmUiStyle.Muted;
        }
        private void Purchase(string title,string cost,float x,float y,float width,Func<bool> action,bool enabled)
        {
            var button=MasteryWindow.Button(content,title+(string.IsNullOrEmpty(cost)?"":" · "+cost),x,y,width,37,()=>{action();Refresh();});
            button.interactable=enabled;
        }
        private void OnDisable()=>Close();
        private void OnDestroy(){if(progression!=null)progression.Changed-=Refresh;Close();if(canvas!=null)Destroy(canvas);}
    }
}
