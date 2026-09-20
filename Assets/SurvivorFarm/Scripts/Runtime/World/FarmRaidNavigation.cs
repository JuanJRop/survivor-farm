using System.Collections.Generic;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    /// <summary>Shared reverse paths. A wall-free field identifies a breach only when
    /// walls disconnect the destination; an open fence is never a target.</summary>
    public sealed class FarmRaidNavigation
    {
        private const float Cell=.5f;
        private const int Width=(int)(FarmExploration.HalfWidth*2/Cell)+1,
            Height=(int)(FarmExploration.HalfHeight*2/Cell)+1, Count=Width*Height;
        private readonly bool[] blocked=new bool[Count], terrain=new bool[Count];
        private readonly FarmDefense[] walls=new FarmDefense[Count];
        private const int MaxFields=24;
        private const float Clearance=.24f;
        private sealed class RouteField
        {
            internal readonly int[] Costs=new int[Count];
            internal int Key=-1,Stamp,LastUse;
        }
        private readonly RouteField[] fields=new RouteField[MaxFields];
        private readonly Queue<int> queue=new Queue<int>(Count);
        private readonly List<Collider2D> obstacles=new List<Collider2D>(512);
        private readonly List<Collider2D> boundaryHits=new List<Collider2D>(16);
        private int fieldStamp,fieldUse;
        private float refreshAt;
        public int FieldBufferCount {get;private set;}
        public int MapRefreshCount {get;private set;}
        public int MapOverlapQueryCount {get;private set;}
        public int BoundaryOverlapQueryCount {get;private set;}

        private int Index(Vector2 p) => Mathf.Clamp(Mathf.RoundToInt((p.y+FarmExploration.HalfHeight)/Cell),0,Height-1)*Width+
            Mathf.Clamp(Mathf.RoundToInt((p.x+FarmExploration.HalfWidth)/Cell),0,Width-1);
        private Vector2 Position(int n)=>new Vector2(n%Width*Cell-FarmExploration.HalfWidth,n/Width*Cell-FarmExploration.HalfHeight);
        public void Invalidate()=>refreshAt=-1;
        private void Refresh()
        {
            if(Time.time<refreshAt)return;
            refreshAt=Time.time+1.2f;fieldStamp++;MapRefreshCount++;Physics2D.SyncTransforms();
            for(int n=0;n<Count;n++)
            {
                blocked[n]=terrain[n]=false;walls[n]=null;
            }
            // One broad-phase query replaces the full-grid sweep. ClosestPoint handles interiors;
            // native contact tests resolve only the narrow boundary where shape skin can differ.
            // This is a candidate margin, not added clearance: the exact .24 overlap decides it.
            float rasterClearance=Clearance+Mathf.Max(.05f,Physics2D.defaultContactOffset*2);
            obstacles.Clear();MapOverlapQueryCount++;
            Physics2D.OverlapBox(Vector2.zero,new Vector2(FarmExploration.HalfWidth*2+rasterClearance*2,
                FarmExploration.HalfHeight*2+rasterClearance*2),0,new ContactFilter2D{useTriggers=false},obstacles);
            foreach(var hit in obstacles)
            {
                // Classify each body once, instead of repeating GetComponent for every touched cell.
                if(hit==null||hit.GetComponentInParent<EnemyAIBase>()!=null||hit.GetComponentInParent<PlayerInventory>()!=null||
                    hit.GetComponentInParent<VillageResidentHealth>()!=null)continue;
                var defense=hit.GetComponentInParent<FarmDefense>();
                bool wall=defense!=null&&defense.IsAlive&&FortressPieces.IsWall(defense.Kind);
                Bounds bounds=hit.bounds;
                int minX=Mathf.Clamp(Mathf.CeilToInt((bounds.min.x-rasterClearance+FarmExploration.HalfWidth)/Cell),0,Width-1);
                int maxX=Mathf.Clamp(Mathf.FloorToInt((bounds.max.x+rasterClearance+FarmExploration.HalfWidth)/Cell),0,Width-1);
                int minY=Mathf.Clamp(Mathf.CeilToInt((bounds.min.y-rasterClearance+FarmExploration.HalfHeight)/Cell),0,Height-1);
                int maxY=Mathf.Clamp(Mathf.FloorToInt((bounds.max.y+rasterClearance+FarmExploration.HalfHeight)/Cell),0,Height-1);
                for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++)
                {
                    int n=y*Width+x;Vector2 point=Position(n);
                    float distanceSquared=(hit.ClosestPoint(point)-point).sqrMagnitude;
                    if(distanceSquared>Clearance*Clearance)
                    {
                        if(distanceSquared>rasterClearance*rasterClearance||!NativeBoundaryContact(hit,point))continue;
                    }
                    blocked[n]=true;
                    if(wall)walls[n]=defense;
                    else terrain[n]=true;
                }
            }
        }
        private bool NativeBoundaryContact(Collider2D obstacle,Vector2 point)
        {
            BoundaryOverlapQueryCount++;
            Physics2D.OverlapCircle(point,Clearance,new ContactFilter2D{useTriggers=false},boundaryHits);
            // The candidate already passed dynamic-body and trigger exclusions. Matching its
            // actual collider avoids accidentally baking another nearby moving body into the map.
            for(int i=0;i<boundaryHits.Count;i++)if(boundaryHits[i]==obstacle)return true;
            return false;
        }
        private bool Adjacent(int a,int b)=>b>=0&&b<Count&&Mathf.Abs(a%Width-b%Width)+Mathf.Abs(a/Width-b/Width)==1;
        private int Walkable(Vector2 point,bool ignoreWalls)
        {
            var occupied=ignoreWalls?terrain:blocked;
            int origin=Index(point);if(!occupied[origin])return origin;
            int best=-1;float nearest=float.PositiveInfinity;
            // Escape only a body's occupied cell, not a distant enclosure.
            for(int y=-2;y<=2;y++)for(int x=-2;x<=2;x++)
            {
                int n=origin+y*Width+x;
                if(n<0||n>=Count||Mathf.Abs(n%Width-origin%Width)>2||occupied[n])continue;
                float d=(Position(n)-point).sqrMagnitude;
                if(d<nearest){nearest=d;best=n;}
            }
            return best;
        }
        private int[] Field(Vector2 goal,bool ignoreWalls)
        {
            Refresh();int destination=Walkable(goal,ignoreWalls);
            if(destination<0)return null;
            int key=destination+(ignoreWalls?Count:0);
            RouteField selected=null;
            for(int i=0;i<FieldBufferCount;i++)
            {
                var field=fields[i];
                if(field.Stamp==fieldStamp&&field.Key==key){field.LastUse=++fieldUse;return field.Costs;}
                if(selected==null||field.LastUse<selected.LastUse)selected=field;
            }
            if(FieldBufferCount<MaxFields)
            {
                selected=new RouteField();fields[FieldBufferCount++]=selected;
            }
            selected.Key=key;selected.Stamp=fieldStamp;selected.LastUse=++fieldUse;
            var costs=selected.Costs;for(int i=0;i<Count;i++)costs[i]=-1;
            var occupied=ignoreWalls?terrain:blocked;
            queue.Clear();queue.Enqueue(destination);costs[destination]=0;
            while(queue.Count>0)
            {
                int cell=queue.Dequeue();
                Visit(cell,cell-1);Visit(cell,cell+1);Visit(cell,cell-Width);Visit(cell,cell+Width);
            }
            return costs;
            void Visit(int from,int next)
            {
                if(!Adjacent(from,next)||occupied[next]||costs[next]>=0)return;
                costs[next]=costs[from]+1;queue.Enqueue(next);
            }
        }
        public bool HasRoute(Vector2 from,Vector2 goal)
        {
            var costs=Field(goal,false);int start=Walkable(from,false);
            return costs!=null&&start>=0&&costs[start]>=0;
        }
        public FarmDefense BlockingWall(Vector2 from,Vector2 goal)
        {
            if(HasRoute(from,goal))return null;
            var costs=Field(goal,true);int n=Walkable(from,true);
            if(costs==null||n<0||costs[n]<0)return null; // Water or a house is not a reason to hit a fence.
            for(int step=0;step<Count&&costs[n]>0;step++)
            {
                if(walls[n]!=null&&walls[n].IsAlive)return walls[n];
                int next=BestNeighbour(n,costs,terrain);
                if(next==n)break;n=next;
            }
            return null;
        }
        private int BestNeighbour(int start,int[] costs,bool[] occupied)
        {
            int best=start,bestCost=costs[start]<0?int.MaxValue:costs[start];
            for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
            {
                int n=start+y*Width+x;
                if(n<0||n>=Count||Mathf.Abs(n%Width-start%Width)>1||occupied[n]||costs[n]<0||costs[n]>=bestCost)continue;
                if(x!=0&&y!=0&&(occupied[start+x]||occupied[start+y*Width]))continue;
                best=n;bestCost=costs[n];
            }
            return best;
        }
        public Vector2 NextDirection(Vector2 from,Vector2 goal)
        {
            var costs=Field(goal,false);int start=Walkable(from,false);
            if(costs==null||start<0||costs[start]<0)return Vector2.zero;
            if(costs[start]<=1)return goal-from;
            int best=BestNeighbour(start,costs,blocked);
            return best==start?Position(start)-from:Position(best)-from;
        }
    }
}
