using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Core;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Encounter selection and bounded spawning; attacks stay in the existing enemies.</summary>
    public sealed class PracticeWaveDirector : MonoBehaviour
    {
        public static readonly Vector3[] Entries={new Vector3(-11,-2),new Vector3(11,-2),new Vector3(0,2),new Vector3(0,-8)};
        public static readonly string[] Roster={"Limo","Murciélago","Gólem","Rastreador","Arquero","Demoledor","Custodio","Soldado","Orco","Demonio","Monstruo de sangre"};
        public static readonly string[] WaveNames={"Criaturas del valle","Soldados y goblins","Orcos y resistencia","Asalto demoníaco","El Custodio"};
        private static readonly int[][] Sequences={new[]{0,0,1,1},new[]{7,4,3,7},new[]{2,8,5},new[]{7,8,9,10,4,9},new[]{6}};
        private readonly List<EnemyAIBase> enemies=new List<EnemyAIBase>();
        private PracticeSession practice;
        private PracticeEnemyFactory factory;
        private CombatTelegraph marker;
        private int[] sequence;
        private int cursor;
        private float countdown;
        private bool announced,circuit,between;
        public bool Running {get;private set;}
        public int WaveIndex {get;private set;}
        public int Spawned=>cursor;
        public int Alive=>enemies.Count(e=>e!=null&&e.IsAlive)+practice.Session.Raids.Alive+(practice.Session.Raids.Boss!=null&&practice.Session.Raids.Boss.IsAlive?1:0);
        public IReadOnlyList<EnemyAIBase> Enemies=>enemies;
        public void Configure(PracticeSession owner,BasicEnemyAI[] templates)
        {
            practice=owner;factory=new PracticeEnemyFactory(owner,transform,templates);
            marker=CombatTelegraph.Create(transform,"Arena · aparición anunciada");
        }
        public void StartCircuit(){StartWave(0);circuit=true;}
        public void StartWave(int index)
        {
            index=Mathf.Clamp(index,0,Sequences.Length-1);StartEncounter(Sequences[index]);WaveIndex=index;
            practice.SetStatus($"Oleada {index+1}/5 · {WaveNames[index]}");
        }
        public void StartSingle(int rosterIndex)
        {
            rosterIndex=Mathf.Clamp(rosterIndex,0,Roster.Length-1);StartEncounter(new[]{rosterIndex});
            WaveIndex=-1;practice.SetStatus("Práctica individual · "+Roster[rosterIndex]);
        }
        private void StartEncounter(int[] roster)
        {
            Stop();practice.Session.Raids.ResetPracticeBoss();practice.Heal();
            practice.Teleport(new Vector3(0,-2));practice.Session.SetPracticePhase(true);
            sequence=roster;cursor=0;countdown=1;Running=true;
        }
        public void Tick(float seconds)
        {
            if(!Running)return;
            countdown-=seconds;
            if(between)
            {
                if(countdown<=0){int next=WaveIndex+1;StartWave(next);circuit=true;}
                return;
            }
            if(cursor<sequence.Length&&countdown<=0)
            {
                if(!announced)
                {
                    marker.Show(Entries[cursor%Entries.Length],.8f,new Color(1,.65f,.25f,.85f));
                    announced=true;countdown=1.1f;return;
                }
                int kind=sequence[cursor];marker.Hide();
                if(kind==6)
                {
                    practice.Session.Raids.IntroduceBoss();practice.Session.SetPracticePhase(true,true);
                    practice.Session.Raids.ActivateBoss();
                }
                else enemies.Add(factory.Create(kind,Entries[cursor%Entries.Length]));
                cursor++;announced=false;countdown=2.1f;
            }
            if(cursor>=sequence.Length&&Alive==0)Complete();
        }
        public void BossDefeated()
        {
            practice.Session.Raids.Stop();Complete();
        }
        private void Complete()
        {
            if(!Running||between)return;
            practice.Session.SetPracticePhase(false);
            Camera.main?.GetComponent<World.CameraFollowTarget>()?.SetCombatFocus(null);
            if(circuit&&WaveIndex<Sequences.Length-1)
            {
                between=true;countdown=6;
                practice.SetStatus("Oleada superada · siguiente en 6 s · recuperas vida entre rondas.");
            }
            else
            {
                Running=false;practice.SetStatus(circuit?"Circuito completado · cinco oleadas y Custodio vencidos. ¡Repite cuando quieras!":"Encuentro superado · elige otro o repítelo.");
            }
        }
        public void Stop()
        {
            Running=false;circuit=false;between=false;announced=false;marker?.Hide();
            foreach(var enemy in enemies)if(enemy!=null){enemy.ReturnToPool();Destroy(enemy.gameObject);}
            enemies.Clear();factory?.ClearProjectiles();
            Camera.main?.GetComponent<World.CameraFollowTarget>()?.SetCombatFocus(null);
        }
        private void OnDisable()=>Stop();
    }
}
