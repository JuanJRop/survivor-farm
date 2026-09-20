using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Four readable patterns: leap, committed fan, delayed roots, summons.
    /// Phase two changes their sequence and follows a fan with roots.</summary>
    public sealed class FarmBossPattern : MonoBehaviour
    {
        private PortfolioSession session;
        private DungeonBoss boss;
        private EnemyProjectilePool projectiles;
        private readonly CombatTelegraph[] markers=new CombatTelegraph[3];
        private readonly Vector3[] impacts=new Vector3[3];
        private Vector3 leapStart;
        private Vector2 shotDirection;
        private int state,pattern,sequence;
        private float until;
        private bool running,enraged;
        public bool PhaseTwo=>boss.CurrentHealth<=boss.MaximumHealth/2;
        public bool IsExposed=>running&&state==3;
        public int Pattern=>pattern;
        public string Status=>!running?"EL CUSTODIO DESPIERTA":state==3?"RECUPERANDO · DAÑO DOBLE":
            state==1?(pattern==0?"SALTO · SAL DEL CÍRCULO":pattern==1?"ABANICO · BUSCA COBERTURA":pattern==2?"RAÍCES · SIGUE MOVIÉNDOTE":"LLAMADA · PROTEGE A LOS VECINOS"):
            PhaseTwo?"FASE II · LAS RAÍCES RESPONDEN":"FASE I · EL CUSTODIO DEL VALLE";
        public void Configure(PortfolioSession owner,DungeonBoss enemy,EnemyProjectilePool arrows)
        {
            session=owner;boss=enemy;projectiles=arrows;
            for(int i=0;i<markers.Length;i++)markers[i]=CombatTelegraph.Create(transform,"Custodio · impacto "+i);
        }
        public void Begin(){running=true;state=0;until=Time.time+1;}
        public void Stop(){running=false;foreach(var marker in markers)marker?.Hide();}
        public void Tick()
        {
            if(!running||session.Phase!=SlicePhase.Boss)return;
            if(!enraged&&PhaseTwo)
            {
                enraged=true;session.Player.GetComponent<GameFeelFeedback>()?.Pulse("FASE II",transform.position,false,true);
            }
            if(state==0&&Time.time>=until)Telegraph();
            else if(state==1&&Time.time>=until)Execute();
            else if(state==2)
            {
                if(pattern==0)
                {
                    float t=Mathf.Clamp01(1-(until-Time.time)/.4f);
                    transform.position=Vector3.Lerp(leapStart,impacts[0],t);
                    boss.SpriteAnimation.Visual.transform.localPosition=Vector3.up*Mathf.Sin(t*Mathf.PI)*1.3f;
                }
                if(Time.time>=until)
                {
                    if(pattern==0){boss.SpriteAnimation.Visual.transform.localPosition=Vector3.zero;DamageArea(impacts[0],1.7f);}
                    if(pattern==2)for(int i=0;i<3;i++)DamageArea(impacts[i],1.15f);
                    foreach(var marker in markers)marker.Hide();
                    state=3;until=Time.time+(PhaseTwo?1.25f:1.9f);
                }
            }
            else if(state==3&&Time.time>=until){state=0;until=Time.time+.35f;}
        }
        private void Telegraph()
        {
            int[] first={0,1,0,2,3};int[] second={1,2,0,3,2,0};
            var order=PhaseTwo?second:first;pattern=order[sequence++%order.Length];
            state=1;until=Time.time+(PhaseTwo?.95f:1.2f);
            Vector3 player=session.Player.transform.position;
            shotDirection=(player-transform.position).normalized;
            impacts[0]=Clamp(player);
            // A locked marker remains where it was announced; it never follows the player.
            if(pattern==0)
            {
                impacts[0]=transform.position+Vector3.ClampMagnitude(impacts[0]-transform.position,5);
                foreach(var hit in Physics2D.LinecastAll(transform.position,impacts[0]))
                    if(!hit.collider.isTrigger&&!hit.transform.IsChildOf(transform)&&!hit.transform.IsChildOf(session.Player.transform))
                    {impacts[0]=(Vector3)hit.point-shotDirection.ToVector3()*.85f;break;}
                markers[0].Show(impacts[0],1.7f,new Color(1,.4f,.15f,.9f));
            }
            else if(pattern==2)
            {
                var velocity=session.Player.GetComponent<Rigidbody2D>().linearVelocity;
                for(int i=0;i<3;i++)
                {
                    impacts[i]=Clamp(player+(Vector3)Vector2.ClampMagnitude(velocity,3)*(i*.35f));
                    if(i>0&&(impacts[i]-impacts[0]).sqrMagnitude<.5f)impacts[i]+=new Vector3(i==1?-2:2,0);
                    markers[i].Show(impacts[i],1.15f,new Color(.95f,.25f,.6f,.9f));
                }
            }
            else markers[0].Show(transform.position,pattern==1?2:1.2f,new Color(1,.7f,.2f,.9f));
            boss.SpriteAnimation.PlayAttack(shotDirection,1.4f);
        }
        private void Execute()
        {
            state=2;until=Time.time+(pattern==0?.4f:pattern==2?.25f:.2f);leapStart=transform.position;
            if(pattern==1)
            {
                int count=PhaseTwo?7:5;
                for(int i=0;i<count;i++)
                {
                    Vector2 heading=Quaternion.Euler(0,0,(i-(count-1)*.5f)*15)*shotDirection;
                    projectiles.Fire(boss,session.Player.transform,heading,1,null);
                }
            }
            if(pattern==3)session.Raids.SpawnBossAdds();
        }
        private void DamageArea(Vector3 point,float radius)
        {
            CultivationSoilVisual.Emit(point,false);
            if(Vector2.Distance(session.Player.transform.position,point)<radius)session.Player.GetComponent<PlayerSurvivalStats>().TakeDamage(2);
            foreach(var house in new System.Collections.Generic.List<VillageHouseHealth>(VillageHouseHealth.All))
                if(house.IsAlive&&Vector2.Distance(house.ContactPoint(point),point)<radius)house.TakeDamage(3,null);
            foreach(var resident in new System.Collections.Generic.List<VillageResidentHealth>(VillageResidentHealth.All))
                if(resident.IsAlive&&Vector2.Distance(resident.transform.position,point)<radius)resident.TakeDamage(2,null);
        }
        private static Vector3 Clamp(Vector3 point)=>new Vector3(Mathf.Clamp(point.x,-15,15),Mathf.Clamp(point.y,-9,6),0);
        private void OnDisable()=>Stop();
    }
    internal static class FarmVectorExtensions
    {
        public static Vector3 ToVector3(this Vector2 value)=>new Vector3(value.x,value.y,0);
    }
}
