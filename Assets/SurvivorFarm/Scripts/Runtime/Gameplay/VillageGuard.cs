using SurvivorFarm.Runtime.World;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>A local defender with solid movement, a committed windup and a bounded patrol.</summary>
    public sealed class VillageGuard : MonoBehaviour
    {
        private VillageProgression owner;
        private VillageNpcArtCatalog catalog;
        private SpriteRenderer visual,sword;
        private CombatTelegraph tell;
        private EnemyAIBase target,strikeTarget;
        private Vector2 home,patrol,facing=Vector2.down,strikePoint;
        private float nextScan,nextPatrol,strikeAt=-1,nextAttack;
        private int strikeGeneration;
        private readonly Collider2D[] hits=new Collider2D[48];
        private readonly RaycastHit2D[] sightHits=new RaycastHit2D[32];
        public VillageResidentHealth Body {get;private set;}
        public int Slot {get;private set;}
        public int Strength {get;private set;}=1;
        public int Toughness {get;private set;}=1;
        public int Damage=>Strength==3?5:Strength+1;
        public string DisplayName=>VillageProgression.GuardName(Slot);
        public EnemyAIBase CurrentTarget=>target;
        public bool IsPreparingAttack=>strikeAt>=0;
        public int LandedStrikes {get;private set;}
        public const float NoticeRange=5.5f,LeashRange=10f,AttackRange=1.15f;

        public void Configure(VillageProgression progression,VillageGuardState state)
        {
            owner=progression;Slot=Mathf.Clamp(state.slot,0,3);home=VillageProgression.GuardPost(Slot);patrol=home;
            nextScan=Time.time+Slot*.05f;
            catalog=Resources.Load<VillageNpcArtCatalog>("VillageNpcArt");
            var art=new GameObject("Guardia del pueblo");art.transform.SetParent(transform,false);art.transform.localScale=Vector3.one*.8f;
            visual=art.AddComponent<SpriteRenderer>();
            visual.sprite=catalog?.Frame("village:guard",false,false,0,0)??HouseSprites.Slice("Josh",0,0,32,32);
            gameObject.AddComponent<WorldSpriteDepth>().Visual=visual;
            Body=gameObject.AddComponent<VillageResidentHealth>();Body.Configure("hired-guard:"+Slot);
            Train(Mathf.Clamp(state.strength,1,3),Mathf.Clamp(state.toughness,1,3));
            var weapon=new GameObject("Espada del guardia");weapon.transform.SetParent(transform,false);weapon.transform.localScale=Vector3.one*.55f;
            sword=weapon.AddComponent<SpriteRenderer>();sword.sprite=UI.FarmUiStyle.ItemIcon("Sword");sword.enabled=false;
            tell=CombatTelegraph.Create(transform,"Preparación del guardia");
            Vector2 position=new Vector2(state.x,state.y);
            if(float.IsNaN(position.x)||float.IsNaN(position.y)||float.IsInfinity(position.x)||float.IsInfinity(position.y)||Vector2.Distance(position,home)>LeashRange)position=home;
            Body.Restore(new ResidentSnapshot{id=Body.Id,health=state.health,x=position.x,y=position.y});
        }
        public void Train(int strength,int toughness)
        {
            Strength=Mathf.Clamp(strength,1,3);Toughness=Mathf.Clamp(toughness,1,3);
            Body?.SetTraining(14+(Toughness-1)*9,Toughness==3?1:0,true);
        }
        public void Recover()
        {
            CancelStrike();target=null;nextAttack=Time.time+1;
            Body.Restore(new ResidentSnapshot{id=Body.Id,health=Body.Maximum,x=home.x,y=home.y});
        }
        private void Update()
        {
            if(owner==null||owner.Session==null||!owner.Session.HasBegun||owner.Session.IsPaused||Body==null||!Body.IsAlive)return;
            if(target!=null&&(!target.IsAlive||Vector2.Distance(target.transform.position,home)>LeashRange))target=null;
            if(Time.time>=nextScan&&strikeAt<0){nextScan=Time.time+.2f;FindTarget();}
            Vector2 previous=transform.position;
            if(strikeAt>=0)
            {
                if(Time.time>=strikeAt)ResolveStrike();
            }
            else if(target!=null)
            {
                Vector2 delta=(Vector2)target.transform.position-previous;
                if(delta.sqrMagnitude>.001f)facing=delta.normalized;
                if(delta.sqrMagnitude<=AttackRange*AttackRange)
                {
                    if(Time.time>=nextAttack)
                    {
                        if(Clear(target))BeginStrike();
                        else Move(facing,1.85f);
                    }
                }
                else Move(facing,1.85f);
            }
            else
            {
                if(Time.time>=nextPatrol){nextPatrol=Time.time+3.5f+Slot*.35f;patrol=home+Random.insideUnitCircle*1.25f;}
                Vector2 delta=patrol-(Vector2)transform.position;
                if(delta.magnitude>.15f){facing=delta.normalized;Move(facing,.75f);}
            }
            bool walking=Vector2.Distance(previous,transform.position)>.0001f;
            int direction=Mathf.Abs(facing.x)>Mathf.Abs(facing.y)?2:facing.y>0?1:0;
            if(catalog!=null)visual.sprite=catalog.Frame("village:guard",walking,IsPreparingAttack,direction,(int)(Time.time*(walking?7:3)))??visual.sprite;
            visual.flipX=direction==2&&facing.x<0;
            visual.color=Toughness==3?new Color(.87f,.94f,1):Strength==3?new Color(1,.92f,.72f):Color.white;
            if(sword!=null&&sword.enabled)
            {
                sword.transform.localPosition=(Vector3)(facing*.35f)+Vector3.up*.45f;
                sword.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(facing.y,facing.x)*Mathf.Rad2Deg-45);
                sword.sortingOrder=visual.sortingOrder+1;
            }
        }
        private void FindTarget()
        {
            // Retain nearby threats; never inspect the entire map or dungeon roster.
            if(target!=null&&Vector2.Distance(target.transform.position,transform.position)<NoticeRange+1)return;
            target=null;float nearest=NoticeRange*NoticeRange;
            int count=Physics2D.OverlapCircle(transform.position,NoticeRange,new ContactFilter2D{useTriggers=true},hits);
            for(int i=0;i<count;i++)
            {
                var enemy=hits[i].GetComponentInParent<EnemyAIBase>();
                if(enemy==null||!enemy.IsAlive||Vector2.Distance(enemy.transform.position,home)>LeashRange)continue;
                float distance=((Vector2)enemy.transform.position-(Vector2)transform.position).sqrMagnitude;
                if(distance>=nearest)continue;
                nearest=distance;target=enemy;
            }
        }
        private void BeginStrike()
        {
            strikeTarget=target;strikeGeneration=target.SpawnGeneration;strikePoint=target.transform.position;
            strikeAt=Time.time+.38f;nextAttack=Time.time+1.55f;
            tell.Show(strikePoint,.42f,new Color(.45f,.85f,1,.7f));sword.enabled=true;
        }
        private void ResolveStrike()
        {
            var victim=strikeTarget;
            bool connects=victim!=null&&victim.IsAlive&&victim.SpawnGeneration==strikeGeneration&&
                Vector2.Distance(transform.position,victim.transform.position)<=AttackRange+.15f&&
                Vector2.Distance(victim.transform.position,strikePoint)<=.6f&&Clear(victim);
            CancelStrike();
            if(!connects)return;
            victim.TakeDamage(Damage,null);LandedStrikes++;
            // Null player source prevents a distant explorer from stealing guard aggro.
            if(victim is RaidEnemy raider)raider.ProvokeByDefender(Body);
            AudioFeedback.PlayAt(CombatSound.Swing,transform.position,.55f);
        }
        private void CancelStrike(){strikeAt=-1;strikeTarget=null;tell?.Hide();if(sword!=null)sword.enabled=false;}
        private bool Clear(EnemyAIBase enemy)
        {
            int count=Physics2D.Linecast((Vector2)transform.position+Vector2.up*.26f,
                (Vector2)enemy.transform.position+Vector2.up*.25f,new ContactFilter2D{useTriggers=false},sightHits);
            if(count==sightHits.Length)return false;
            for(int i=0;i<count;i++)
                if(!sightHits[i].transform.IsChildOf(transform)&&!sightHits[i].transform.IsChildOf(enemy.transform))return false;
            return true;
        }
        private void Move(Vector2 desired,float speed)
        {
            float step=Mathf.Min(Time.deltaTime,.05f)*speed;
            for(int i=0;i<7;i++)
            {
                float angle=i==0?0:((i+1)/2)*35f*(i%2==0?-1:1);
                Vector2 direction=Quaternion.Euler(0,0,angle)*desired;
                Vector2 point=(Vector2)transform.position+direction*step;
                if(!CanStand(point))continue;
                transform.position=point;return;
            }
        }
        private bool CanStand(Vector2 point)
        {
            if(Vector2.Distance(point,home)>LeashRange||!FarmExploration.Contains(point,1)||FarmExploration.IsRiver(point,.25f))return false;
            int count=Physics2D.OverlapCircle(point+Vector2.up*.26f,.27f,new ContactFilter2D{useTriggers=false},hits);
            if(count==hits.Length)return false;
            for(int i=0;i<count;i++)if(!hits[i].transform.IsChildOf(transform))return false;
            return true;
        }
        public VillageGuardState Capture()=>new VillageGuardState{slot=Slot,strength=Strength,toughness=Toughness,health=Body.Health,x=transform.position.x,y=transform.position.y};
        private void OnDisable()=>CancelStrike();
    }
}
