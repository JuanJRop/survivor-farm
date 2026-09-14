using System.Collections.Generic;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.World;
using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class FarmRaidDirector : MonoBehaviour
    {
        private PortfolioSession session;
        private readonly List<RaidEnemy> pool=new List<RaidEnemy>();
        private EnemyProjectilePool projectiles;
        private SliceDay wave;
        private int cursor;
        private float elapsed,spawnAt=-1;
        private Vector3 pendingPosition;
        private CombatTelegraph entrance;
        private static readonly Vector3[] Entries={new Vector3(-15,-.3f),new Vector3(15,-.3f),new Vector3(0,-9)};
        public DungeonBoss Boss { get; private set; }
        public FarmBossPattern BossPattern => Boss!=null?Boss.GetComponent<FarmBossPattern>():null;
        public int Alive {get{int n=0;foreach(var enemy in pool)if(enemy.IsAlive)n++;return n;}}
        public int Total=>wave?.enemies.Length??0;
        public int Defeated=>Mathf.Max(0,cursor-Alive);
        public int Spawned=>cursor;
        public int PoolCount=>pool.Count;
        public bool Complete=>wave!=null&&cursor>=Total&&Alive==0&&spawnAt<0;
        public IReadOnlyList<RaidEnemy> Enemies=>pool;
        public void Configure(PortfolioSession owner)
        {
            session=owner;projectiles=EnemyProjectilePool.Ensure(gameObject,24);
            entrance=CombatTelegraph.Create(transform,"Entrada anunciada de oleada");
            int capacity=Mathf.Clamp(owner.Settings.maximumConcurrentEnemies,3,12);
            for(int i=0;i<capacity;i++)
            {
                var root=new GameObject("Raid slot "+i);root.transform.SetParent(transform,false);root.SetActive(false);
                var art=new GameObject("Original goblin art");art.transform.SetParent(root.transform,false);art.AddComponent<SpriteRenderer>();
                root.AddComponent<CircleCollider2D>().radius=.25f;
                var enemy=root.AddComponent<RaidEnemy>();enemy.ConfigureRaid(owner,RaidRole.Chaser,projectiles);enemy.ReturnToPool();pool.Add(enemy);
            }
        }
        public void Begin(int day){Stop();wave=session.Settings.days[day-1];cursor=0;elapsed=0;}
        public void Tick(float seconds)
        {
            if(wave==null||!session.InCombat)return;
            elapsed+=seconds;
            if(spawnAt>=0)
            {
                if(elapsed<spawnAt)return;
                if(Spawn(wave.enemies[cursor],pendingPosition))cursor++;
                spawnAt=-1;entrance.Hide();return;
            }
            float due=3+cursor*Mathf.Max(1,(wave.nightSeconds-20)/Mathf.Max(1,Total));
            if(cursor>=Total||Alive>=pool.Count||elapsed<due)return;
            if(!FindEntry(cursor+session.Day,out pendingPosition))return;
            entrance.Show(pendingPosition,.8f,new Color(1,.65f,.2f,.8f));
            spawnAt=elapsed+1.1f;
        }
        private bool Spawn(RaidRole role,Vector3 position)
        {
            foreach(var enemy in pool)
            {
                if(enemy.gameObject.activeSelf)continue;
                enemy.ConfigureRaid(session,role,projectiles);enemy.ActivateFromPool(position);return true;
            }
            return false;
        }
        private bool FindEntry(int seed,out Vector3 position)
        {
            for(int i=0;i<18;i++)
            {
                position=Entries[(seed+i/6)%Entries.Length]+new Vector3(i%3*.6f-.6f,i%2*.6f);
                bool blocked=false;
                foreach(var hit in Physics2D.OverlapCircleAll(position,.4f))if(!hit.isTrigger){blocked=true;break;}
                if(!blocked)return true;
            }
            position=default;return false;
        }
        public void SpawnBossAdds()
        {
            for(int i=0;i<2;i++)if(Alive<4&&FindEntry(i,out var p))Spawn(i==0?RaidRole.Chaser:RaidRole.Archer,p);
        }
        public void IntroduceBoss()
        {
            if(Boss!=null)return;
            var root=new GameObject("El Custodio · jefe final");root.transform.SetParent(transform,false);
            root.transform.position=new Vector3(0,-7.8f);
            var art=new GameObject("Original Custodian art");art.transform.SetParent(root.transform,false);
            var sr=art.AddComponent<SpriteRenderer>();
            root.AddComponent<CircleCollider2D>().radius=.6f;
            Boss=root.AddComponent<DungeonBoss>();Boss.ConfigureFarmBoss(session,sr,projectiles);
            Boss.ActivateFromPool(root.transform.position);
            Camera.main?.GetComponent<CameraFollowTarget>()?.SetCombatFocus(root.transform);
        }
        public void ActivateBoss()=>BossPattern?.Begin();
        public void Stop()
        {
            foreach(var enemy in pool)enemy.ReturnToPool();
            projectiles?.ReturnAll();spawnAt=-1;entrance?.Hide();
            if(Boss!=null)BossPattern?.Stop();
        }
        private void OnDisable()=>Stop();
    }
}
