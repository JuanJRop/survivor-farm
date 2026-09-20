using System.Collections.Generic;
using System.IO;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>A real title flow: home, slot browser and preferences, independent from pause/end cards.</summary>
    public sealed class DemoFrontEnd : MonoBehaviour
    {
        private PortfolioSession session;private GameObject root,body;private Text message;
        private int page;private GameObject originalHud;
        public string ScreenName {get;private set;}
        public bool IsVisible=>root!=null&&root.activeSelf;
        private List<PortfolioSaveEntry> Saves
        {get {var save=FindFirstObjectByType<GameSaveSystem>();return save==null?new List<PortfolioSaveEntry>():PortfolioSaveCatalog.List(Path.GetDirectoryName(save.SaveFile));}}
        public void Configure(PortfolioSession owner)
        {
            session=owner;root=MasteryWindow.CreateCanvas("SURVIVAL FARM · DEMO · Menú principal",120);
            var shade=root.AddComponent<Image>();shade.color=new Color(.025f,.055f,.042f,.82f);
            var original=FindFirstObjectByType<OriginalSpriteHud>();if(original!=null){originalHud=original.gameObject;originalHud.SetActive(false);}
            if(!GameSaveSystem.IsQa){AudioListener.volume=PlayerPrefs.GetFloat("FarmVolume",1);CombatTimeFeedback.ReducedMotion=PlayerPrefs.GetInt("FarmReducedMotion",0)==1;}
            Home();
        }
        private Transform Begin(string screen)
        {
            ScreenName=screen;if(body!=null){body.SetActive(false);Destroy(body);}
            body=AdventureWindow.Rect(root.transform,"Contenido · "+screen,0,0,1280,720).gameObject;
            var r=(RectTransform)body.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;
            return body.transform;
        }
        public void Home()
        {
            var p=Begin("Inicio");
            var plaque=AdventureWindow.Rect(p,"Navegación",40,36,518,648).gameObject.AddComponent<Image>();FarmUiStyle.Frame(plaque);
            MasteryWindow.Label(p,"DEMO JUGABLE · TRES NOCHES",72,64,454,30,16).color=FarmUiStyle.Accent;
            MasteryWindow.Label(p,"SURVIVAL\nFARM",72,104,456,138,57);
            MasteryWindow.Label(p,"Explora el valle. Defiende a los tuyos.",76,256,447,28,17).color=FarmUiStyle.Muted;
            var entries=Saves;
            var resume=MasteryWindow.Button(p,"Continuar",76,326,444,46,()=>ResumeLatest());resume.interactable=entries.Count>0;
            MasteryWindow.Button(p,"Nueva partida",76,382,444,46,NewGame);
            var load=MasteryWindow.Button(p,"Cargar partida",76,438,444,46,()=>{page=0;LoadScreen();});load.interactable=entries.Count>0;
            MasteryWindow.Button(p,"Opciones",76,494,444,46,Options);
            MasteryWindow.Button(p,"Salir",76,550,444,46,()=>session.Quit());
            MasteryWindow.Label(p,"Demo de portfolio · Windows · guardados locales",76,629,450,30,13).color=FarmUiStyle.Muted;
            MasteryWindow.Label(p,"UN PUEBLO QUE\nDEPENDE DE TI",654,125,545,96,30).color=FarmUiStyle.Accent;
            MasteryWindow.Label(p,"Asalta campamentos, cruza portales y descubre tesoros.\nRegresa para proteger las casas y a sus habitantes.",656,234,533,70,19);
            MasteryWindow.Button(p,"Escenas de práctica",680,532,444,44,PracticeScreen);
            var icon=AdventureWindow.Rect(p,"La casa del valle",788,330,262,184).gameObject.AddComponent<Image>();
            icon.sprite=HouseSprites.Facade(1);icon.preserveAspect=true;icon.raycastTarget=false;
            MasteryWindow.Label(p,"Contenido limitado: tres noches y un encuentro final.\nLa versión completa todavía no está disponible.",638,588,568,58,16).color=FarmUiStyle.Muted;
            message=MasteryWindow.Label(p,"",652,662,534,28,14);
        }
        public void NewGame(){if(!session.IsReady)return;session.BeginNewGame();Close();(GetComponent<FarmIntroduction>()??gameObject.AddComponent<FarmIntroduction>()).Open();}
        public void PracticeScreen()
        {
            var p=Begin("Prácticas");
            MasteryWindow.Label(p,"EXPLORA CADA SISTEMA",260,132,780,55,32);
            MasteryWindow.Label(p,"Escenas independientes · sin guardar ni modificar la campaña",260,196,780,40,17);
            MasteryWindow.Button(p,"ARENA · oleadas, bestiario y Custodio",260,284,760,62,()=>PracticeSession.Open(PracticeMode.Combat));
            MasteryWindow.Button(p,"TALLER · cultivar, recolectar y cocinar",260,366,760,62,()=>PracticeSession.Open(PracticeMode.Farm));
            MasteryWindow.Label(p,"Repite encuentros, repón materiales y prueba los niveles de tus herramientas.\nPuedes volver al menú principal en cualquier momento.",260,460,760,70,18);
            MasteryWindow.Button(p,"Volver",260,572,760,48,Home);
        }
        public bool ResumeLatest()
        {
            var entries=Saves;if(entries.Count==0)return false;
            string preferred=GameSaveSystem.IsQa?session.QaSlot:PlayerPrefs.GetString("SurvivorFarmPortfolioSlot","");
            var entry=entries.Find(e=>e.Slot==preferred)??entries[0];return Load(entry.Slot);
        }
        private bool Load(string slot)
        {bool success=PortfolioSaveCatalog.Load(session,slot);if(success)Close();else if(message!=null)message.text="No se pudo cargar. La partida original se conserva.";return success;}
        public void LoadScreen()
        {
            var p=Begin("Cargar");MasteryWindow.Label(p,"CARGAR PARTIDA",274,106,730,54,34);
            MasteryWindow.Label(p,"Cada nueva partida tiene su propio guardado. No se reemplazan al empezar otra.",275,170,730,54,16);
            var entries=Saves;page=Mathf.Clamp(page,0,Mathf.Max(0,(entries.Count-1)/5));
            for(int i=0;i<5&&page*5+i<entries.Count;i++)
            {var entry=entries[page*5+i];MasteryWindow.Button(p,entry.Label,274,240+i*57,730,44,()=>Load(entry.Slot));}
            if(entries.Count==0)MasteryWindow.Label(p,"Todavía no hay partidas guardadas.",274,250,730,40,19);
            MasteryWindow.Button(p,"←",274,542,70,40,()=>{page--;LoadScreen();});
            MasteryWindow.Label(p,$"Página {page+1} / {Mathf.Max(1,Mathf.CeilToInt(entries.Count/5f))}",365,548,340,34,16);
            MasteryWindow.Button(p,"→",934,542,70,40,()=>{page++;LoadScreen();});
            MasteryWindow.Button(p,"Volver",274,610,730,44,Home);message=MasteryWindow.Label(p,"",274,663,730,30,14);
        }
        public void Options()
        {
            var p=Begin("Opciones");MasteryWindow.Label(p,"OPCIONES · DEMO",294,130,700,58,34);
            MasteryWindow.Label(p,$"Volumen  {Mathf.RoundToInt(AudioListener.volume*100)}%",294,238,700,36,21);
            MasteryWindow.Button(p,"−",294,292,100,44,()=>{SetVolume(AudioListener.volume-.1f);Options();});
            MasteryWindow.Button(p,"+",416,292,100,44,()=>{SetVolume(AudioListener.volume+.1f);Options();});
            MasteryWindow.Button(p,CombatTimeFeedback.ReducedMotion?"Impacto de cámara: reducido":"Impacto de cámara: normal",294,365,700,48,()=>{CombatTimeFeedback.ReducedMotion=!CombatTimeFeedback.ReducedMotion;SaveOptions();Options();});
            MasteryWindow.Label(p,"Reduce las sacudidas, el zoom y la cámara lenta sin cambiar el daño.",294,428,700,54,16).color=FarmUiStyle.Muted;
            MasteryWindow.Label(p,"Combat FX · Raphael Hatencia / RagnaPixel · CC BY 4.0",294,486,700,32,15).color=FarmUiStyle.Muted;
            MasteryWindow.Button(p,"Volver",294,548,700,48,Home);
        }
        public static void SetVolume(float value){AudioListener.volume=Mathf.Clamp01(value);SaveOptions();}
        private static void SaveOptions(){if(GameSaveSystem.IsQa)return;PlayerPrefs.SetFloat("FarmVolume",AudioListener.volume);PlayerPrefs.SetInt("FarmReducedMotion",CombatTimeFeedback.ReducedMotion?1:0);PlayerPrefs.Save();}
        private void Close(){if(root!=null)root.SetActive(false);if(originalHud!=null)originalHud.SetActive(true);}
        private void Update(){if(session.HasBegun&&IsVisible)Close();if(body!=null)FarmUiStyle.FitWindow((RectTransform)body.transform);}
        private void OnDestroy(){if(root!=null)Destroy(root);}
    }
}
