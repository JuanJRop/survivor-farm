using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SurvivorFarm.Runtime.World
{
    /// <summary>Quarter tiles preserve both concave and convex grass corners at junctions.</summary>
    public sealed class ValleyTerrain : MonoBehaviour
    {
        readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        public int EdgeQuarters {get;private set;}
        public int ConcaveQuarters {get;private set;}
        public int PlantCount {get;private set;}
        GameObject farmLayer;TilemapRenderer farmRenderer;
        readonly List<KeyValuePair<FarmingPlot,SpriteRenderer>> farmPlants=new List<KeyValuePair<FarmingPlot,SpriteRenderer>>();
        float nextRefresh;
        Sprite Slice(string atlas,int x,int y,int w,int h)
        {
            string key=atlas+":"+x+":"+y+":"+w+":"+h;
            if(sprites.TryGetValue(key,out var s))return s;
            var tex=Resources.Load<Texture2D>("StoryArt/"+atlas);
            s=Sprite.Create(tex,new Rect(x,tex.height-y-h,w,h),new Vector2(.5f,.5f),16);sprites[key]=s;return s;
        }
        public void Build(ValleyWorld world)
        {
            for(int zone=1;zone<=5;zone++)
            {
                var cells=new HashSet<Vector2Int>();
                void Rect(int x0,int y0,int x1,int y1){for(int x=x0;x<=x1;x++)for(int y=y0;y<=y1;y++)cells.Add(new Vector2Int(x,y));}
                Rect(-12,-5,11,-3);
                if(zone==1){Rect(-8,-3,-6,0);Rect(7,-3,9,3);Rect(-5,4,9,5);Rect(-5,-2,-3,4);}
                if(zone==2){Rect(5,-3,7,2);Rect(-9,0,9,4);cells.Remove(new Vector2Int(-9,4));cells.Remove(new Vector2Int(9,4));}
                if(zone==3){Rect(6,-3,8,2);Rect(-4,-3,-2,0);}
                if(zone==4)Rect(-1,-3,1,2);
                if(zone==5)for(int x=-12;x<=12;x++)for(int y=-7;y<=6;y++)if(x*x/144f+y*y/49f<1)cells.Add(new Vector2Int(x,y));
                Vector3 center=ValleyWorld.Center(zone);
                Paint(cells,center,Vector3.right,Vector3.up,-29900);
                Vegetation(world,zone,cells);
            }
            var farm=FindObjectsByType<Tilemap>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="Farm Paths");
            if(farm!=null)
            {
                var cells=new HashSet<Vector2Int>();foreach(var cell in farm.cellBounds.allPositionsWithin)if(farm.HasTile(cell))cells.Add(new Vector2Int(cell.x,cell.y));
                var renderer=farm.GetComponent<TilemapRenderer>();
                var layer=new GameObject("Bordes de los caminos de la granja");layer.transform.SetParent(farm.transform,false);
                farmLayer=layer;farmRenderer=renderer;
                var terrain=layer.AddComponent<ValleyTerrain>();
                terrain.Paint(cells,farm.CellToWorld(Vector3Int.zero),farm.CellToWorld(Vector3Int.right)-farm.CellToWorld(Vector3Int.zero),farm.CellToWorld(Vector3Int.up)-farm.CellToWorld(Vector3Int.zero),renderer.sortingOrder+1);
                renderer.enabled=false;
                terrain.FarmVegetation(farm);
                // Keep the underlying tilemap and cultivation data intact.
            }
        }
        void FarmVegetation(Tilemap paths)
        {
            int count=0;
            foreach(var plot in FindObjectsByType<FarmingPlot>(FindObjectsSortMode.None).OrderBy(p=>p.transform.position.x*31+p.transform.position.y*17))
            {
                var p=plot.transform.position;
                if(Mathf.Abs(p.x)<3||Mathf.Abs(p.y)<3||Mathf.Abs(p.x)>32||Mathf.Abs(p.y)>17)continue;
                var cell=paths.WorldToCell(p);
                if(paths.HasTile(cell)||paths.HasTile(cell+Vector3Int.left)||paths.HasTile(cell+Vector3Int.up))continue;
                if((Mathf.RoundToInt(p.x*13+p.y*19)&15)!=0)continue;
                if(Physics2D.OverlapCircleAll(p,.7f).Any(c=>c.GetComponentInParent<FarmingPlot>()==null&&!c.isTrigger))continue;
                var go=new GameObject("Flores silvestres de la granja");go.transform.SetParent(transform,false);go.transform.position=p;
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=count%3==0?Slice("BushAtlas",48,32,48,32):Slice("SunflowerAtlas",80,0,16,32);
                go.transform.localScale=Vector3.one*.5f;go.AddComponent<WorldSpriteDepth>().Visual=sr;
                sr.enabled=plot.StateId==0;farmPlants.Add(new KeyValuePair<FarmingPlot,SpriteRenderer>(plot,sr));PlantCount++;if(++count>=40)break;
            }
        }
        void Update()
        {
            if(Time.time<nextRefresh)return;nextRefresh=Time.time+.4f;
            foreach(var pair in farmPlants)if(pair.Value!=null)pair.Value.enabled=pair.Key!=null&&pair.Key.StateId==0;
        }
        public void Paint(HashSet<Vector2Int> cells,Vector3 origin,Vector3 dx,Vector3 dy,int order)
        {
            foreach(var cell in cells)for(int qy=0;qy<2;qy++)for(int qx=0;qx<2;qx++)
            {
                bool left=qx==0,top=qy==1;
                var h=new Vector2Int(left?-1:1,0);var v=new Vector2Int(0,top?1:-1);
                bool edgeH=!cells.Contains(cell+h),edgeV=!cells.Contains(cell+v),inner=!edgeH&&!edgeV&&!cells.Contains(cell+h+v);
                // The 48px inset has grass outside all four edges. The old 64px
                // transition patch starts with dirt on its left corners, producing
                // the square spikes at every path end and junction.
                int x=edgeH?(left?16:56):edgeV?32+qx*8:88;
                int y=edgeV?(top?128:168):edgeH?144:152;
                if(inner){x=left?32:24;y=top?144:136;ConcaveQuarters++;}
                var go=new GameObject(edgeH||edgeV||inner?"Borde de pasto":"Tierra");go.transform.SetParent(transform,false);
                go.transform.position=origin+dx*(cell.x+qx*.5f+.25f)+dy*(cell.y+qy*.5f+.25f);
                go.transform.localScale=new Vector3(dx.magnitude,dy.magnitude,1);
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=Slice("TerrainAtlas",x,y,8,8);sr.sortingOrder=order;
                if(edgeH||edgeV||inner)EdgeQuarters++;
            }
        }
        void Vegetation(ValleyWorld world,int zone,HashSet<Vector2Int> dirt)
        {
            var rng=new System.Random(4300+zone);var center=ValleyWorld.Center(zone);
            var occupied=world.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.sortingOrder>-20000&&s.sprite!=null).Select(s=>s.bounds).ToArray();
            var points=new List<Vector2>();int target=zone==3?95:zone==5?35:65;
            for(int attempt=0;attempt<1600&&points.Count<target;attempt++)
            {
                var p=new Vector2(-12+(float)rng.NextDouble()*24,-7.5f+(float)rng.NextDouble()*13.5f);
                var cell=new Vector2Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y));
                if(dirt.Contains(cell)||dirt.Contains(cell+Vector2Int.left)||dirt.Contains(cell+Vector2Int.right)||dirt.Contains(cell+Vector2Int.up)||dirt.Contains(cell+Vector2Int.down))continue;
                var position=center+(Vector3)p;
                if(zone==5&&Mathf.Abs(p.x)<10&&Mathf.Abs(p.y)<6)continue;
                if(occupied.Any(b=>{b.Expand(.8f);return b.Contains(new Vector3(position.x,position.y,b.center.z));})||points.Any(q=>Vector2.Distance(p,q)<.65f))continue;
                points.Add(p);int kind=rng.Next(5);
                var go=new GameObject("Planta decorativa");go.transform.SetParent(transform,false);go.transform.position=position;
                var sr=go.AddComponent<SpriteRenderer>();
                go.transform.localScale=Vector3.one*.5f;
                if(kind<2){sr.sprite=Slice("TerrainAtlas",144,64,16,16);sr.sortingOrder=-29800;}
                else if(kind==2&&zone!=5){sr.sprite=Slice("SunflowerAtlas",80,0,16,32);go.AddComponent<WorldSpriteDepth>().Visual=sr;}
                else {sr.sprite=Slice("BushAtlas",48,kind==4?32:0,48,32);go.AddComponent<WorldSpriteDepth>().Visual=sr;}
                PlantCount++;
            }
        }
        void OnDestroy(){if(farmRenderer!=null)farmRenderer.enabled=true;if(farmLayer!=null)Destroy(farmLayer);foreach(var s in sprites.Values)if(s!=null)Destroy(s);}
    }
}
