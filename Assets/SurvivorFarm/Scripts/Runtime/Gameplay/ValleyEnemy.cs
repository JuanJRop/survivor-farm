using System.Linq;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class ValleyEnemy : MonoBehaviour, IDamageable
    {
        ValleyCampaign campaign;ValleyWorld world;int zone;bool guardian,boss;Vector3 origin,landing,leapStart;
        SpriteRenderer visual;TextMesh label;LineRenderer warning;float until,baseScale;int state;bool engaged;
        Vector3 restingVisualPosition;
        Collider2D bodyCollider;
        PlayerSurvivalStats playerStats;
        Material warningMaterial;
        float phaseDuration;
        const float MeleeRadius=1.1f, SlamRadius=1.7f;
        const float LeapWarningDuration=1.3f, LeapDuration=.55f, ExhaustedDuration=2.5f;
        static readonly Color ThreatColor=new Color(1f,.4f,.18f);
        static readonly Color OpeningColor=new Color(.6f,1f,.8f);
        readonly ValleyBud[] buds=new ValleyBud[2];
        GameObject bossHud;Text bossText;Image healthFill;
        public int Health {get;private set;}
        public int Maximum => boss?36:guardian?12:8;
        public bool Vulnerable=>boss&&state==3;
        public bool IsAlive=>isActiveAndEnabled&&Health>0;
        public Transform Transform=>transform;
        public int SpawnGeneration {get;private set;}
        public float AttackRadius=>boss?SlamRadius:MeleeRadius;
        public float PhaseRemaining=>Mathf.Max(0f,until-Time.time);
        public string Phase=>boss
            ? state==1?$"SALTO en {PhaseRemaining:0.0}s":state==2?"Saltando":state==3?$"AGOTADO · daño doble · {PhaseRemaining:0.0}s":buds.Any(b=>b!=null&&b.IsAlive)?"Destruye los brotes":"Preparando salto"
            : state==1?$"Golpe en {PhaseRemaining:0.0}s":engaged&&PhaseRemaining>0?$"Recuperando {PhaseRemaining:0.0}s":"";
        public void Configure(ValleyCampaign owner,ValleyWorld map,int area,bool isGuardian,bool isBoss)
        {
            campaign=owner;world=map;zone=area;guardian=isGuardian;boss=isBoss;origin=transform.position;visual=GetComponentInChildren<SpriteRenderer>();baseScale=visual.transform.localScale.x;
            restingVisualPosition=visual.transform.localPosition;bodyCollider=GetComponent<Collider2D>();playerStats=campaign.GetComponent<PlayerSurvivalStats>();
            label=world.Label("",origin+Vector3.up*3,.11f);
            var ring=new GameObject("Marca de impacto");ring.transform.SetParent(world.transform,false);warning=ring.AddComponent<LineRenderer>();warningMaterial=new Material(Shader.Find("Sprites/Default")){hideFlags=HideFlags.DontSave};warning.sharedMaterial=warningMaterial;warning.useWorldSpace=true;warning.startColor=warning.endColor=ThreatColor;warning.startWidth=warning.endWidth=.045f;warning.loop=true;warning.positionCount=40;warning.sortingOrder=6;warning.enabled=false;
            if(boss)
            {
                BuildHud();
                for(int i=0;i<2;i++){var go=world.Prop("Crop",origin+new Vector3(i==0?-5:5,2),.8f);go.AddComponent<CircleCollider2D>().isTrigger=true;buds[i]=go.AddComponent<ValleyBud>();buds[i].Owner=this;}
            }
            ResetEncounter();
        }
        public void ResetEncounter()
        {
            Health=Maximum;SpawnGeneration++;transform.position=origin;SetPhase(0,1.5f);engaged=false;
            if(warning!=null)warning.enabled=false;
            if(visual!=null){visual.transform.localPosition=restingVisualPosition;visual.transform.localScale=new Vector3(baseScale,baseScale,1);visual.color=Color.white;}
            if(label!=null)label.text="";
            if(bossHud!=null)bossHud.SetActive(false);
            foreach(var b in buds)if(b!=null)b.ResetBud();
        }
        bool Completed=>boss?campaign.Data.boss:guardian&&campaign.Data.guardian;
        void Update()
        {
            if(campaign==null)return;
            bool inArea=Vector2.Distance(campaign.transform.position,ValleyWorld.Center(zone))<18;
            bool alive=Health>0&&!Completed;
            visual.enabled=alive;if(bodyCollider!=null)bodyCollider.enabled=alive;
            foreach(var b in buds)if(b!=null)b.gameObject.SetActive(alive&&inArea);
            if(!alive||!inArea||playerStats!=null&&playerStats.CurrentHealth<=0)
            {
                if(engaged&&alive){ResetEncounter();}RefreshPresentation(alive&&inArea);warning.enabled=false;return;
            }
            var distance=Vector2.Distance(transform.position,campaign.transform.position);
            if(!engaged&&distance<(boss?13:guardian?6:4))engaged=true;
            if(!engaged){RefreshPresentation(true);return;}
            if(boss||guardian)visual.sprite=world.BossFrame();
            visual.flipX=campaign.transform.position.x<transform.position.x;
            visual.transform.localScale=new Vector3(baseScale,baseScale*(1+Mathf.Sin(Time.time*8)*.035f),1);
            if(!boss){Melee(distance);RefreshPresentation(true);return;}
            if(state==0&&Time.time>=until)
            {
                // Bud healing is bounded to one pulse per attack cycle.
                foreach(var b in buds)if(b!=null&&b.IsAlive)Health=Mathf.Min(Maximum,Health+1);
                landing=campaign.transform.position;var center=ValleyWorld.Center(zone);landing.x=Mathf.Clamp(landing.x,center.x-11,center.x+11);landing.y=Mathf.Clamp(landing.y,-6,5);
                // Landing on solid cover is stopped at the near edge; cover cannot trap the boss.
                foreach(var hit in Physics2D.LinecastAll(transform.position,landing))if(!hit.collider.isTrigger&&!hit.transform.IsChildOf(transform)&&!hit.transform.IsChildOf(campaign.transform)){landing=(Vector3)hit.point+(transform.position-(Vector3)hit.point).normalized*1.3f;break;}
                SetPhase(1,LeapWarningDuration);
            }
            else if(state==1&&Time.time>=until){SetPhase(2,LeapDuration);leapStart=transform.position;}
            else if(state==2)
            {
                float t=Mathf.Clamp01(1-(until-Time.time)/LeapDuration);transform.position=Vector3.Lerp(leapStart,landing,t);visual.transform.localPosition=restingVisualPosition+Vector3.up*Mathf.Sin(t*Mathf.PI)*2;
                if(Time.time>=until){visual.transform.localPosition=restingVisualPosition;warning.enabled=false;if(Vector2.Distance(campaign.transform.position,landing)<SlamRadius&&Clear(campaign.transform.position))playerStats?.TakeDamage(1);SetPhase(3,ExhaustedDuration);}
            }
            else if(state==3&&Time.time>=until){SetPhase(0,.7f);}
            RefreshPresentation(true);
        }
        void Melee(float distance)
        {
            if(state==1){if(Time.time>=until){if(distance<MeleeRadius&&Clear(campaign.transform.position))playerStats?.TakeDamage(1);SetPhase(0,1.3f);}return;}
            visual.color=Color.white;
            if(distance>.85f)
            {
                var dir=(campaign.transform.position-transform.position).normalized;var next=transform.position+dir*(guardian?1.1f:1.5f)*Time.deltaTime;
                bool blocked=false;foreach(var c in Physics2D.OverlapCircleAll(next,.35f))if(!c.isTrigger&&!c.transform.IsChildOf(transform)&&!c.transform.IsChildOf(campaign.transform)){blocked=true;break;}
                if(!blocked)transform.position=next;
            }
            else if(Time.time>=until){SetPhase(1,guardian?1:.7f);}
        }
        void SetPhase(int value,float duration){state=value;phaseDuration=duration;until=Time.time+duration;}

        void RefreshPresentation(bool visibleInArea)
        {
            bool playerAlive=playerStats==null||playerStats.CurrentHealth>0;
            if(bossHud!=null)
            {
                bossHud.SetActive(visibleInArea&&playerAlive);
                bossText.text="REY LIMO · "+Health+" / "+Maximum+"\n"+Phase;
                healthFill.fillAmount=(float)Health/Maximum;
                healthFill.color=Vulnerable?new Color(.18f,.6f,.4f):new Color(.7f,.18f,.4f);
            }
            if(label!=null)
            {
                label.text=visibleInArea&&playerAlive&&!boss?(guardian?"GUARDIÁN":"Limo")+"  "+Health+" / "+Maximum+(Phase.Length>0?"\n"+Phase:""):"";
                label.transform.position=transform.position+Vector3.up*(boss?3:1.8f);
            }
            visual.color=state==1?Color.Lerp(Color.white,ThreatColor,.55f):Vulnerable?OpeningColor:Color.white;
            if(Vulnerable)visual.transform.localScale=new Vector3(baseScale*1.06f,baseScale*.88f,1);
            warning.enabled=visibleInArea&&playerAlive&&engaged&&(state==1||boss&&state==2);
            if(!warning.enabled)return;
            float progress=state==2?1f:1f-Mathf.Clamp01(PhaseRemaining/Mathf.Max(.01f,phaseDuration));
            Color tint=ThreatColor;tint.a=.35f+.45f*progress;
            warning.startColor=warning.endColor=tint;
            DrawWarning();
        }
        bool Clear(Vector3 to){foreach(var h in Physics2D.LinecastAll(transform.position,to))if(!h.collider.isTrigger&&!h.transform.IsChildOf(transform)&&!h.transform.IsChildOf(campaign.transform))return false;return true;}
        void DrawWarning(){Vector3 center=boss?landing:transform.position;for(int i=0;i<40;i++){float a=i*Mathf.PI*2/40;warning.SetPosition(i,center+new Vector3(Mathf.Cos(a),Mathf.Sin(a))*AttackRadius);}}
        public void TakeDamage(int amount,PlayerInventory source)
        {
            if(!IsAlive||campaign==null||source==null||Completed||amount<=0||source!=campaign.Inventory)return;
            if(boss&&state==2)return;
            VisibleHitFeedback.Play(gameObject);
            Vector3 away=(transform.position-source.transform.position).normalized;Vector3 next=transform.position+away*(boss?0.12f:0.45f);
            if(!Physics2D.OverlapCircleAll(next,.35f).Any(c=>!c.isTrigger&&!c.transform.IsChildOf(transform)&&!c.transform.IsChildOf(source.transform)))transform.position=next;
            engaged=true;Health=Mathf.Max(0,Health-amount*(Vulnerable?2:1));FarmGameEvents.RaiseEnemyDamaged();
            if(Health==0){
                var loot=FindObjectsByType<EnemyAIBase>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(e=>e.LootPrefab).FirstOrDefault(p=>p!=null);
                EnemyLootPickup.Spawn(loot,transform.position,world.transform,ResourceFlyweights.Item(ItemKind.Coins),boss?15:guardian?8:2);
                engaged=false;warning.enabled=false;visual.transform.localPosition=restingVisualPosition;RefreshPresentation(false);FarmGameEvents.RaiseEnemyDefeated();if(boss||guardian)campaign.Defeated(boss);}
            else RefreshPresentation(true);
        }
        void BuildHud()
        {
            bossHud=new GameObject("Vida del Rey Limo",typeof(Canvas),typeof(CanvasScaler));
            var canvas=bossHud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=40;
            var scale=bossHud.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1280,720);scale.matchWidthOrHeight=.5f;
            var panel=new GameObject("Panel",typeof(RectTransform),typeof(Image));panel.transform.SetParent(bossHud.transform,false);var r=panel.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(0,-12);r.sizeDelta=new Vector2(390,77);var image=panel.GetComponent<Image>();image.sprite=Resources.Load<Sprite>("BackpackIcons/Panel");image.type=Image.Type.Sliced;image.raycastTarget=false;
            var text=new GameObject("Estado",typeof(RectTransform),typeof(Text));text.transform.SetParent(panel.transform,false);bossText=text.GetComponent<Text>();bossText.rectTransform.sizeDelta=new Vector2(370,54);bossText.rectTransform.anchoredPosition=new Vector2(0,6);bossText.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");bossText.fontSize=18;bossText.alignment=TextAnchor.MiddleCenter;bossText.color=new Color(.28f,.12f,.2f);bossText.raycastTarget=false;
            var bar=new GameObject("Salud",typeof(RectTransform),typeof(Image));bar.transform.SetParent(panel.transform,false);healthFill=bar.GetComponent<Image>();healthFill.rectTransform.sizeDelta=new Vector2(346,7);healthFill.rectTransform.anchoredPosition=new Vector2(0,-27);healthFill.sprite=Resources.Load<Sprite>("BackpackIcons/Panel");healthFill.color=new Color(.7f,.18f,.4f);healthFill.type=Image.Type.Filled;healthFill.fillMethod=Image.FillMethod.Horizontal;healthFill.raycastTarget=false;
        }
        void OnDisable()
        {
            if(engaged&&Health>0)ResetEncounter();
            if(bossHud!=null)bossHud.SetActive(false);
            if(label!=null)label.text="";
            if(warning!=null)warning.enabled=false;
            foreach(var bud in buds)if(bud!=null)bud.gameObject.SetActive(false);
        }
        void OnDestroy(){if(bossHud!=null)Destroy(bossHud);if(label!=null)Destroy(label.gameObject);if(warningMaterial!=null)Destroy(warningMaterial);if(warning!=null)Destroy(warning.gameObject);foreach(var bud in buds)if(bud!=null)Destroy(bud.gameObject);}
    }
    public sealed class ValleyBud : MonoBehaviour,IDamageable
    {
        public ValleyEnemy Owner;int health=3;
        public Transform Transform=>transform;public bool IsAlive=>health>0&&gameObject.activeInHierarchy;public int SpawnGeneration {get;private set;}
        public void ResetBud(){health=3;SpawnGeneration++;GetComponentInChildren<SpriteRenderer>().enabled=true;GetComponent<Collider2D>().enabled=true;}
        public void TakeDamage(int amount,PlayerInventory source){if(!IsAlive||amount<=0)return;VisibleHitFeedback.Play(gameObject);health-=amount;if(health<=0){GetComponentInChildren<SpriteRenderer>().enabled=false;GetComponent<Collider2D>().enabled=false;}}
    }
}
