using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.UI
{
    /// <summary>Guided play in the real farm. Hints freeze the world; practice releases
    /// only the relevant controls while the three-night clock remains suspended.</summary>
    [DefaultExecutionOrder(-900)]
    public sealed class FarmIntroduction : MonoBehaviour
    {
        public enum Lesson { MovementHint, Movement, DashRunHint, DashRun, ComboHint, Combo, ChargeHint, Charge, Mission }
        private static FarmIntroduction current;
        public static bool IsOpen=>current!=null&&current.open;
        public static bool BlocksGameplay=>IsOpen&&current.IsHint;
        public static bool AllowsMovement=>!IsOpen||current.lesson==Lesson.Movement||current.lesson==Lesson.DashRun||current.lesson==Lesson.Combo||current.lesson==Lesson.Charge;
        public static bool AllowsCombat=>!IsOpen||current.lesson==Lesson.Combo||current.lesson==Lesson.Charge;
        public static bool AllowsInteraction=>!IsOpen;
        private bool open;
        private Lesson lesson;
        private GameObject root;
        private RectTransform panel;
        private readonly Image[] masks=new Image[4];
        private readonly Image[] keys=new Image[4];
        private Text heading,body,progress;
        private Button next;
        private PlayerInventory player;
        private PlayerMovementController movement;
        private PlayerCombatController combat;
        private HitFeedback feedback;
        private TrainingEnemy practice;
        private Vector3 movedFrom;
        private Vector3 skillStart;
        private string sentence;
        private float revealed,readyAt,advanceAt=-1;
        private int hits;
        private bool ran,dashed;
        public int Page=>(int)lesson;
        public Lesson CurrentLesson=>lesson;
        public TrainingEnemy PracticeEnemy=>practice;
        private bool IsHint=>lesson==Lesson.MovementHint||lesson==Lesson.DashRunHint||lesson==Lesson.ComboHint||lesson==Lesson.ChargeHint||lesson==Lesson.Mission;
        public void Open()
        {
            if(IsOpen||PortfolioSession.Instance?.Player==null)return;
            current=this;open=true;player=PortfolioSession.Instance.Player;
            movement=player.GetComponent<PlayerMovementController>();
            combat=player.GetComponent<PlayerCombatController>();feedback=player.GetComponent<HitFeedback>();
            if(feedback!=null)feedback.HitResolved+=OnHit;
            root=MasteryWindow.CreateCanvas("Aprender jugando",130);
            for(int i=0;i<4;i++)
            {
                var rect=AdventureWindow.Rect(root.transform,"Foco de la lección "+i,0,0,1,1);
                rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;
                masks[i]=rect.gameObject.AddComponent<Image>();
            }
            panel=AdventureWindow.Rect(root.transform,"Consejo del vecino",0,0,720,152);
            panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,0);panel.anchoredPosition=new Vector2(0,24);
            FarmUiStyle.Frame(panel.gameObject.AddComponent<Image>());
            heading=MasteryWindow.Label(panel,"",20,12,570,28,21);heading.color=FarmUiStyle.Accent;
            body=MasteryWindow.Label(panel,"",20,49,675,54,17);
            progress=MasteryWindow.Label(panel,"",180,114,335,24,13);progress.color=FarmUiStyle.Muted;
            next=MasteryWindow.Button(panel,"",554,110,145,30,Next);
            MasteryWindow.Button(panel,"Saltar",633,10,65,26,Finish);
            string[] names={"W","A","S","D"};
            for(int i=0;i<4;i++)
            {
                var key=AdventureWindow.Rect(panel,"Tecla "+names[i],20+i*35,111,29,28);
                keys[i]=key.gameObject.AddComponent<Image>();FarmUiStyle.Frame(keys[i],true);
                MasteryWindow.Label(key,names[i],0,0,29,28,17).alignment=TextAnchor.MiddleCenter;
            }
            SetLesson(Lesson.MovementHint);
        }
        private void SetLesson(Lesson value)
        {
            lesson=value;hits=0;ran=false;dashed=false;revealed=0;advanceAt=-1;readyAt=Time.unscaledTime+.65f;
            movement?.StopMovement();
            combat.CancelMelee();
            string title;
            switch(value)
            {
                case Lesson.MovementHint:title="PRIMERO, MUÉVETE";sentence="El pueblo te necesita. Usa W A S D para caminar. Pulsa una de esas teclas cuando estés listo: la pantalla se aclarará.";break;
                case Lesson.Movement:title="DA UNOS PASOS";sentence="Eso es. Camina un poco por el sendero con W A S D.";movedFrom=player.transform.position;break;
                case Lesson.DashRunHint:title="CORRE Y ESQUIVA";sentence="Mantén SHIFT mientras caminas para correr. Pulsa ESPACIO en la dirección en la que quieras hacer dash.";break;
                case Lesson.DashRun:title="PRUEBA MOVILIDAD";sentence="Corre un poco y haz un dash con ESPACIO. El dash te vuelve invulnerable durante un instante.";skillStart=player.transform.position;break;
                case Lesson.ComboHint:title="ESTE ES TU RIVAL DE PRÁCTICA";sentence="No puede hacerte daño. Acércate y apunta hacia él. CLIC, CLIC, CLIC: el tercer corte es un remate.";SpawnPractice();break;
                case Lesson.Combo:title="ENCADENA LOS TRES CORTES";sentence="Clics cortos, uno tras otro. Verás la reacción, las chispas y el remate. Si esperas demasiado, el combo vuelve al primero.";break;
                case Lesson.ChargeHint:title="AHORA, CARGA LA ESPADA";sentence="Mantén CLIC hasta oír la señal y ver la energía dorada. Suelta el botón para descargar un golpe mucho más fuerte.";break;
                case Lesson.Charge:title="MANTÉN… Y SUELTA";sentence="Un segundo de carga. Suelta sobre tu rival: la descarga lo empuja más lejos. Esquivar o recibir daño interrumpe la carga.";break;
                default:title="PROTEGE TU PUEBLO";sentence="Explora campamentos y ruinas. Aprende la carga de espada al alcanzar nivel 2 en K: habilidades. E interactúa · I mochila · F recetas · Espacio esquiva · Q cura.";RemovePractice();break;
            }
            heading.text=title;
            foreach(var key in keys)key.gameObject.SetActive(value==Lesson.MovementHint||value==Lesson.Movement);
            next.gameObject.SetActive(IsHint&&value!=Lesson.MovementHint);
            next.GetComponentInChildren<Text>().text=value==Lesson.Mission?"¡A jugar!":value==Lesson.ComboHint||value==Lesson.ChargeHint||value==Lesson.DashRunHint?"Sigue":"Practicar";
            PortfolioSession.Instance.Pause(IsHint);
            UpdateFocus();
        }
        public bool TryBeginMovement(Vector2 direction)
        {
            if(!open||lesson!=Lesson.MovementHint||Time.unscaledTime<readyAt||direction.sqrMagnitude<.01f)return false;
            SetLesson(Lesson.Movement);return true;
        }
        public void Next()
        {
            if(!open||Time.unscaledTime<readyAt)return;
            if(revealed<sentence.Length){revealed=sentence.Length;return;}
            if(lesson==Lesson.DashRunHint)SetLesson(Lesson.DashRun);
            else if(lesson==Lesson.ComboHint)SetLesson(Lesson.Combo);
            else if(lesson==Lesson.ChargeHint)SetLesson(Lesson.Charge);
            else if(lesson==Lesson.Mission)Finish();
        }
        private void OnHit(ResolvedHit hit)
        {
            if(practice==null||hit.Target!=practice.gameObject||advanceAt>=0)return;
            if(lesson==Lesson.Combo&&!hit.Charged)
            {
                hits=combat.Combo.StepNumber;
                if(hits==3&&hit.Heavy)advanceAt=Time.unscaledTime+.55f;
            }
            else if(lesson==Lesson.Charge&&hit.Charged)advanceAt=Time.unscaledTime+.65f;
        }
        private void Update()
        {
            if(!open)return;
            revealed+=Time.unscaledDeltaTime*52;
            body.text=sentence.Substring(0,Mathf.Min((int)revealed,sentence.Length));
            if(lesson==Lesson.MovementHint)
            {
                Vector2 direction=new Vector2((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0));
                TryBeginMovement(direction);
            }
            else if(lesson==Lesson.Movement&&Vector2.Distance(movedFrom,player.transform.position)>.9f)SetLesson(Lesson.DashRunHint);
            else if(lesson==Lesson.DashRunHint && (Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.RightShift)||Input.GetKeyDown(KeyCode.Space)))
                SetLesson(Lesson.DashRun);
            else if(lesson==Lesson.DashRun)
            {
                if(movement != null && movement.IsDashing)dashed=true;
                if((Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift))&&Vector2.Distance(skillStart,player.transform.position)>.25f)ran=true;
                if(ran&&dashed)SetLesson(Lesson.ComboHint);
            }
            if(advanceAt>=0&&Time.unscaledTime>=advanceAt)SetLesson(lesson==Lesson.Combo&&combat.CanChargeSword?Lesson.ChargeHint:Lesson.Mission);
            if(IsHint&&lesson!=Lesson.MovementHint&&Input.GetKeyDown(KeyCode.Return))Next();
            progress.text=lesson==Lesson.Combo?("CORTES  "+hits+" / 3"):lesson==Lesson.Charge?(combat.Charge.Progress>=1?"¡SUELTA!":"MANTÉN CLIC"):lesson==Lesson.DashRun?("CORRER: "+(ran?"✓":"—")+"   DASH: "+(dashed?"✓":"—")):IsHint?"La noche espera mientras aprendes":"Práctica segura · el reloj está detenido";
            if(practice!=null&&!practice.IsAlive&&!practice.IsDying&&advanceAt<0)
            {practice.ActivateFromPool(PracticePosition());}
            FarmUiStyle.FitWindow(panel);UpdateFocus();
        }
        private Vector3 PracticePosition()
        {
            for(int i=0;i<16;i++)
            {
                float a=i*Mathf.PI/8;Vector3 p=player.transform.position+new Vector3(Mathf.Cos(a),Mathf.Sin(a))*.9f;
                bool blocked=false;
                foreach(var c in Physics2D.OverlapCircleAll(p,.26f))
                    if(!c.isTrigger&&!c.transform.IsChildOf(player.transform)&&(practice==null||!c.transform.IsChildOf(practice.transform))){blocked=true;break;}
                if(!blocked)return p;
            }
            return player.transform.position+Vector3.right;
        }
        private void SpawnPractice()
        {
            if(practice!=null)return;
            var actor=new GameObject("Rival de práctica · no ataca");actor.transform.SetParent(transform,false);
            var art=new GameObject("Goblin del pack original");art.transform.SetParent(actor.transform,false);art.AddComponent<SpriteRenderer>();
            actor.AddComponent<CircleCollider2D>().radius=.24f;
            practice=actor.AddComponent<TrainingEnemy>();practice.ConfigurePractice(player);practice.ActivateFromPool(PracticePosition());
        }
        private void UpdateFocus()
        {
            if(root==null||Camera.main==null)return;
            var canvasRect=(RectTransform)root.transform;Vector2 size=canvasRect.rect.size;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,Camera.main.WorldToScreenPoint(player.transform.position),null,out var p);
            p+=size*.5f;float radiusX=lesson==Lesson.ComboHint||lesson==Lesson.ChargeHint?200:130;
            float l=Mathf.Clamp(p.x-radiusX,0,size.x),r=Mathf.Clamp(p.x+radiusX,0,size.x);
            float b=Mathf.Clamp(p.y-105,0,size.y),t=Mathf.Clamp(p.y+130,0,size.y);
            SetMask(0,new Vector2(0,t),new Vector2(size.x,size.y-t));
            SetMask(1,Vector2.zero,new Vector2(size.x,b));
            SetMask(2,new Vector2(0,b),new Vector2(l,t-b));
            SetMask(3,new Vector2(r,b),new Vector2(size.x-r,t-b));
        }
        private void SetMask(int index,Vector2 p,Vector2 size)
        {
            var rect=masks[index].rectTransform;rect.anchoredPosition=p;rect.sizeDelta=size;
            masks[index].color=new Color(.025f,.04f,.06f,IsHint?.65f:0);
            masks[index].raycastTarget=IsHint;
        }
        private void RemovePractice(){if(practice==null)return;practice.gameObject.SetActive(false);Destroy(practice.gameObject);practice=null;}
        public void Finish()
        {
            if(!open)return;
            open=false;if(current==this)current=null;
            if(feedback!=null)feedback.HitResolved-=OnHit;
            combat?.CancelMelee();RemovePractice();if(root!=null)Destroy(root);
            PortfolioSession.Instance?.Pause(false);
        }
        private void OnDestroy()
        {
            if(feedback!=null)feedback.HitResolved-=OnHit;
            open=false;if(current==this)current=null;RemovePractice();if(root!=null)Destroy(root);
        }
    }
}
