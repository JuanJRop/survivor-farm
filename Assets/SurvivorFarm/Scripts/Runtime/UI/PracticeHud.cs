using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>Compact practice status and a paused control drawer, using the game's existing UI skin.</summary>
    public sealed class PracticeHud : MonoBehaviour
    {
        private PracticeSession practice;
        private GameObject canvasRoot;
        private RectTransform panel;
        private Text status;
        private Text bossLabel;
        private Image bossFill;
        private GameObject bossRoot;
        public void Configure(PracticeSession owner)
        {
            practice=owner;canvasRoot=MasteryWindow.CreateCanvas("Escena de práctica · DEMO",115);
            var strip=AdventureWindow.Rect(canvasRoot.transform,"Estado de práctica",0,0,610,66);
            strip.anchorMin=strip.anchorMax=strip.pivot=new Vector2(.5f,1);strip.anchoredPosition=new Vector2(0,-12);
            FarmUiStyle.Frame(strip.gameObject.AddComponent<Image>());
            status=MasteryWindow.Label(strip,"",15,8,580,52,15);status.alignment=TextAnchor.MiddleCenter;status.raycastTarget=false;
            var open=MasteryWindow.Button(canvasRoot.transform,"Prácticas · F6",0,0,155,38,Toggle);
            var rect=(RectTransform)open.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,1);rect.anchoredPosition=new Vector2(-16,-18);
            panel=AdventureWindow.Rect(canvasRoot.transform,"Controles de práctica",0,0,920,580);
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.anchoredPosition=Vector2.zero;
            FarmUiStyle.Frame(panel.gameObject.AddComponent<Image>());
            MasteryWindow.Label(panel,owner.Mode==PracticeMode.Combat?"ARENA DE COMBATE":"TALLER DE GRANJA",26,18,790,40,28);
            MasteryWindow.Label(panel,"Práctica independiente · no guarda la campaña · Esc o F6 para cerrar",26,65,850,27,15).color=FarmUiStyle.Muted;
            MasteryWindow.Button(panel,"×",855,20,38,36,Toggle);
            if(owner.Mode==PracticeMode.Combat)BuildCombat();else BuildFarm();
            MasteryWindow.Label(panel,"Equipo de prueba",26,375,250,26,17);
            for(int i=1;i<=3;i++){int tier=i;MasteryWindow.Button(panel,"Nivel "+i,268+(i-1)*137,367,123,40,()=>owner.SetToolTier(tier));}
            MasteryWindow.Button(panel,"Reponer / curar",26,426,272,43,()=>owner.Refill());
            MasteryWindow.Button(panel,"Reiniciar esta escena",324,426,272,43,owner.ResetScene);
            MasteryWindow.Button(panel,"Cambiar de área",622,426,272,43,()=>PracticeSession.Open(owner.Mode==PracticeMode.Combat?PracticeMode.Farm:PracticeMode.Combat));
            MasteryWindow.Label(panel,"WASD · mover    Clic · combo    Mantener clic · cargar    Espacio · esquivar\nE · interactuar    I · mochila    F · recetas    Q · curar    K · maestrías",26,486,860,53,15);
            MasteryWindow.Button(panel,"Menú principal",670,541,224,30,owner.ReturnToTitle);
            panel.gameObject.SetActive(false);
            var boss=AdventureWindow.Rect(canvasRoot.transform,"Jefe de práctica",0,0,510,57);
            boss.anchorMin=boss.anchorMax=boss.pivot=new Vector2(.5f,1);boss.anchoredPosition=new Vector2(0,-85);bossRoot=boss.gameObject;
            bossLabel=MasteryWindow.Label(boss,"",0,0,510,30,15);bossLabel.alignment=TextAnchor.MiddleCenter;bossLabel.raycastTarget=false;
            var bar=AdventureWindow.Rect(boss,"Vida",15,38,480,6);bossFill=bar.gameObject.AddComponent<Image>();bossFill.color=new Color(.85f,.35f,.2f);bossFill.raycastTarget=false;
            bossRoot.SetActive(false);
        }
        private void BuildCombat()
        {
            MasteryWindow.Button(panel,"CIRCUITO COMPLETO · 5 OLEADAS",26,110,868,43,()=>Run(practice.Waves.StartCircuit));
            for(int i=0;i<PracticeWaveDirector.WaveNames.Length;i++)
            {int wave=i;MasteryWindow.Button(panel,(i+1)+" · "+PracticeWaveDirector.WaveNames[i],26+i*175,169,163,58,()=>Run(()=>practice.Waves.StartWave(wave)));}
            MasteryWindow.Label(panel,"BESTIARIO · practicar un enemigo",26,233,850,27,16);
            for(int i=0;i<PracticeWaveDirector.Roster.Length;i++)
            {int enemy=i;MasteryWindow.Button(panel,PracticeWaveDirector.Roster[i],26+i%4*219,266+i/4*33,208,29,()=>Run(()=>practice.Waves.StartSingle(enemy)));}
        }
        private void BuildFarm()
        {
            string[] labels={"Huerto · plantar y regar","Claro · practicar movimiento","Bosque · talar","Cantera · piedra / hierro","Oro · recursos de nivel III","Cocina · fabricar recetas"};
            for(int i=0;i<labels.Length;i++)
            {int station=i;MasteryWindow.Button(panel,labels[i],26+i%2*440,112+i/2*64,428,51,()=>Run(()=>practice.GoToStation(station)));}
            MasteryWindow.Button(panel,"Adelantar cultivos regados · 60 s",26,308,868,43,()=>Run(practice.GrowCrops));
        }
        private void Run(System.Action action){action();practice.Session.Pause(false);}
        private void Toggle()=>practice.Session.Pause(!practice.Session.IsPaused);
        public void RefreshPause(){if(panel!=null)panel.gameObject.SetActive(practice.Session.IsPaused);}
        private void Update()
        {
            if(practice==null||!practice.Ready)return;
            if(Input.GetKeyDown(KeyCode.F6))Toggle();
            if(panel.gameObject.activeSelf)FarmUiStyle.FitWindow(panel);
            status.text=(practice.Mode==PracticeMode.Combat?"ARENA":"TALLER")+" · DEMO · SIN GUARDADO\n"+practice.Status;
            var boss=practice.Session.Raids.Boss;bool show=boss!=null&&boss.IsAlive;
            bossRoot.SetActive(show&&!practice.Session.IsPaused);
            if(show){bossLabel.text=$"CUSTODIO  {boss.CurrentHealth}/{boss.MaximumHealth} · {practice.Session.Raids.BossPattern.Status}";bossFill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,480f*boss.CurrentHealth/boss.MaximumHealth);}
        }
        private void OnDestroy(){if(canvasRoot!=null)Destroy(canvasRoot);}
    }
}
