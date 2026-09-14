using System.Collections.Generic;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    /// <summary>Small grid over the existing village. Rebuilt only when obstacles change.
    /// Per-destination reverse breadth-first fields are shared by all attackers.</summary>
    public sealed class FarmRaidNavigation
    {
        private const int Width=129,Height=73;
        private const float Cell=.5f;
        private readonly bool[] blocked=new bool[Width*Height];
        private readonly Dictionary<int,int[]> fields=new Dictionary<int,int[]>();
        private readonly Queue<int> queue=new Queue<int>();
        private readonly Collider2D[] hits=new Collider2D[24];
        private float refreshAt;
        private int Index(Vector2 p)
        {
            int x=Mathf.Clamp(Mathf.RoundToInt((p.x+FarmExploration.HalfWidth)/Cell),0,Width-1);
            int y=Mathf.Clamp(Mathf.RoundToInt((p.y+FarmExploration.HalfHeight)/Cell),0,Height-1);
            return y*Width+x;
        }
        private Vector2 Position(int index)=>new Vector2(index%Width*Cell-FarmExploration.HalfWidth,index/Width*Cell-FarmExploration.HalfHeight);
        public void Invalidate()=>refreshAt=0;
        public Vector2 NextDirection(Vector2 from,Vector2 goal)
        {
            if(Time.time>=refreshAt)
            {
                refreshAt=Time.time+1.2f;fields.Clear();
                for(int i=0;i<blocked.Length;i++)
                {
                    int count=Physics2D.OverlapCircle(Position(i),.27f,new ContactFilter2D{useTriggers=false},hits);
                    blocked[i]=count==hits.Length;
                    for(int h=0;h<count;h++)
                        if(hits[h].GetComponentInParent<EnemyAIBase>()==null&&hits[h].GetComponentInParent<PlayerInventory>()==null)blocked[i]=true;
                }
            }
            int destination=Index(goal),start=Index(from);
            if((goal-from).sqrMagnitude<2.25f)return goal-from;
            if(!fields.TryGetValue(destination,out var costs))
            {
                if(fields.Count>12)fields.Clear();
                costs=new int[Width*Height];for(int i=0;i<costs.Length;i++)costs[i]=-1;
                queue.Clear();queue.Enqueue(destination);costs[destination]=0;
                while(queue.Count>0)
                {
                    int cell=queue.Dequeue();
                    Visit(cell,cell-1,costs);Visit(cell,cell+1,costs);Visit(cell,cell-Width,costs);Visit(cell,cell+Width,costs);
                }
                fields[destination]=costs;
            }
            int best=start,bestCost=costs[start]<0?int.MaxValue:costs[start];
            for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
            {
                int n=start+y*Width+x;
                if(n<0||n>=costs.Length||Mathf.Abs(n%Width-start%Width)>1||blocked[n]||costs[n]<0||costs[n]>=bestCost)continue;
                if(x!=0&&y!=0&&(blocked[start+x]||blocked[start+y*Width]))continue;
                best=n;bestCost=costs[n];
            }
            return best==start?goal-from:Position(best)-from;
        }
        private void Visit(int from,int next,int[] costs)
        {
            if(next<0||next>=costs.Length||Mathf.Abs(next%Width-from%Width)>1||costs[next]>=0)return;
            // Permit leaving a goal occupied by its own structure, but not crossing other walls.
            if(blocked[next]&&costs[from]>1)return;
            costs[next]=costs[from]+1;queue.Enqueue(next);
        }
    }
}
