using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.Player
{
    public sealed class PlayerRespawnController : MonoBehaviour
    {
        public static bool MenuOpen { get; private set; }
        public Vector3 Checkpoint { get; private set; }
        private PlayerSurvivalStats stats;
        private PlayerDeathDrops deathDrops;
        private GameObject overlay;
        private float previousTimeScale = 1;
        private bool ownsPause;
        private bool deathHandled;

        private void Awake()
        {
            stats=GetComponent<PlayerSurvivalStats>();
            deathDrops=GetComponent<PlayerDeathDrops>() ?? gameObject.AddComponent<PlayerDeathDrops>();
            Checkpoint=transform.position;
        }
        private void OnEnable()
        {
            if (stats != null) stats.Died += OnDeath;
        }
        private void Start()
        {
            if (stats != null) { stats.Died -= OnDeath; stats.Died += OnDeath; }
        }
        private void OnDisable()
        {
            if (stats != null) stats.Died -= OnDeath;
            Hide();
        }
        private void OnDeath()
        {
            if (stats == null || deathHandled) return;
            deathHandled = true;
            deathDrops?.DropAtDeathPosition();
            // Save immediately so the emptied inventory and physical drops are
            // durable even if the process closes while the death menu is open.
            FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);
        }
        public void SetCheckpoint(Vector3 position)
        {
            if(float.IsNaN(position.x)||float.IsNaN(position.y)||float.IsInfinity(position.x)||float.IsInfinity(position.y))return;
            Checkpoint=new Vector3(position.x,position.y,0);
        }
        private void LateUpdate()
        {
            if(stats.CurrentHealth<=0 && !ownsPause) Show();
            else if(stats.CurrentHealth>0 && ownsPause) Hide();
        }
        private void Show()
        {
            FindFirstObjectByType<InventoryPanelSystem>()?.Close();
            FindFirstObjectByType<PlayerEquipmentWindow>()?.Close();
            GetComponent<PlayerMovementController>()?.StopMovement();
            if(overlay==null) BuildMenu();
            overlay.SetActive(true); MenuOpen=true;
            GetComponent<CombatTimeFeedback>()?.Cancel();
            previousTimeScale=Time.timeScale;ownsPause=true;Time.timeScale=0;
        }
        private void Hide()
        {
            if(overlay!=null)overlay.SetActive(false);
            if(ownsPause)Time.timeScale=previousTimeScale;
            ownsPause=false;MenuOpen=false;
        }
        public void ContinueHere() => Revive(false);
        public void ReturnToCheckpoint() => Revive(true);
        private void Revive(bool atCheckpoint)
        {
            if(atCheckpoint)
            {
                var house=GetComponent<HouseSystem>();if(house!=null&&house.IsInside)house.Exit(false);
                var shop=FindFirstObjectByType<ShopEntrance>(FindObjectsInactive.Include);
                var dungeon=FindFirstObjectByType<DungeonEntrance>(FindObjectsInactive.Include);
                if(shop!=null && shop.IsInsideShop)shop.RestoreInsideState(false);
                if(dungeon!=null && dungeon.IsInsideDungeon)dungeon.RestoreInsideState(false);
                transform.position=Checkpoint;
                var body=GetComponent<Rigidbody2D>();if(body!=null)body.position=Checkpoint;
                if(Camera.main!=null)Camera.main.transform.position=Checkpoint+new Vector3(0,0,-10);
            }
            GetComponent<PlayerMovementController>()?.StopMovement();
            stats.Revive();
            deathHandled=false;
            deathDrops?.ResetForNextLife();
            GetComponent<PlayerCharacterAnimator>()?.CancelAction();
            Hide();
            FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);
        }
        public void ExitGame()
        {
            FindFirstObjectByType<GameSaveSystem>()?.SaveGame(false);
            Hide();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        private void OnDestroy(){Hide();if(overlay!=null)Destroy(overlay);}
        private void BuildMenu()
        {
            overlay=new GameObject("Death Menu",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=overlay.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=500;
            var scaler=overlay.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            var shade=new GameObject("Fondo",typeof(RectTransform),typeof(Image));shade.transform.SetParent(overlay.transform,false);
            var sr=shade.GetComponent<RectTransform>();sr.anchorMin=Vector2.zero;sr.anchorMax=Vector2.one;sr.offsetMin=sr.offsetMax=Vector2.zero;
            shade.GetComponent<Image>().color=new Color(0,0,0,.65f);
            var panel=new GameObject("Has muerto",typeof(RectTransform),typeof(Image));panel.transform.SetParent(overlay.transform,false);
            var rect=panel.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(520,370);
            var image=panel.GetComponent<Image>();image.sprite=Resources.Load<Sprite>("BackpackIcons/Panel");image.type=Image.Type.Sliced;
            Label(rect,"HAS MUERTO",126,30,26);
            Label(rect,"Tus recursos quedaron en el lugar de la caída.",83,42,16);
            Button(rect,"Continuar aquí",20,ContinueHere);
            Button(rect,"Último punto de aparición",-43,ReturnToCheckpoint);
            Button(rect,"Salir de la partida",-106,ExitGame);
            Label(rect,"Al continuar recuperas toda la vida; el equipo permanece contigo.",-153,24,14);
        }
        private Text Label(Transform parent,string value,float y,float height,int size)
        {
            var go=new GameObject(value,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            var t=go.GetComponent<Text>();t.rectTransform.anchoredPosition=new Vector2(0,y);t.rectTransform.sizeDelta=new Vector2(480,height);
            t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=size;t.text=value;t.color=new Color32(71,42,41,255);t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;
        }
        private void Button(Transform parent,string text,float y,UnityEngine.Events.UnityAction action)
        {
            var go=new GameObject(text,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
            var rect=go.GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(0,y);rect.sizeDelta=new Vector2(434,50);
            var image=go.GetComponent<Image>();image.sprite=Resources.Load<Sprite>("BackpackIcons/Panel");image.type=Image.Type.Sliced;image.color=new Color(1,.82f,.58f);
            var button=go.GetComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(action);Label(rect,text,0,40,19);
        }
    }
}
