using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SurvivorFarm.Runtime.World
{
    /// <summary>A small authored extension on the existing terrain, with shared playable bounds.</summary>
    public static class FarmExploration
    {
        public const float HalfWidth=44, HalfHeight=26;
        public static readonly Vector2[] VeinPositions={new Vector2(20,4),new Vector2(24,-4),new Vector2(28,12),new Vector2(28,-12)};
        public static bool Contains(Vector2 p,float margin=0)=>p.x>=-HalfWidth+margin&&p.x<=HalfWidth-margin&&p.y>=-HalfHeight+margin&&p.y<=HalfHeight-margin;
        public static bool IsRiver(Vector2 p, float margin=0) => Mathf.Abs(p.x)>1.1f-margin && p.y>5.5f-margin && p.y<8.6f+margin;
        public static Vector3 FrameCamera(Vector3 p,Camera camera)
        {
            if(camera==null)return p;
            if(VillageAdventure.Instance!=null&&VillageAdventure.Instance.IsInsideDungeon)
                return VillageAdventure.Instance.FrameDungeonCamera(p,camera);
            float x=Mathf.Max(0,HalfWidth+1-camera.orthographicSize*camera.aspect);
            float y=Mathf.Max(0,HalfHeight+1-camera.orthographicSize);
            return new Vector3(Mathf.Clamp(p.x,-x,x),Mathf.Clamp(p.y,-y,y),p.z);
        }
        public static void Configure(ValleyWorld world,PlayerInventory player)
        {
            if(world.transform.Find("Exploración de la demo")!=null)return;
            var marker=new GameObject("Exploración de la demo");marker.transform.SetParent(world.transform,false);
            ExtendGrass();
            ExtendRiverAndRemoveOldEdges();
            // Retain the large painted terrain; open the old purchase gates for this short demo.
            foreach(var zone in Object.FindObjectsByType<LandUnlockZone>(FindObjectsInactive.Include,FindObjectsSortMode.None))zone.ConfigureExploration(null);
            foreach(var bridge in Object.FindObjectsByType<RepairableBridge>(FindObjectsInactive.Include,FindObjectsSortMode.None))bridge.Restore(true);
            var paths=Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(m=>m.name=="Farm Paths");
            if(paths!=null)
            {
                var tile=paths.GetTile(paths.WorldToCell(Vector3.zero));
                if(tile!=null)
                {
                    Trail(paths,tile,new Vector2(14,0),new Vector2(28,0));
                    Trail(paths,tile,new Vector2(0,8.5f),new Vector2(27,10.5f));
                    Trail(paths,tile,new Vector2(27,10.5f),new Vector2(27,11));
                    Trail(paths,tile,new Vector2(27,0),new Vector2(27,-11));
                    Trail(paths,tile,new Vector2(-14,0),new Vector2(-27,0));
                    Trail(paths,tile,new Vector2(-27,0),new Vector2(-35,0));
                    Trail(paths,tile,new Vector2(-35,0),new Vector2(-35,-19));
                    Trail(paths,tile,new Vector2(27,0),new Vector2(35,0));
                    Trail(paths,tile,new Vector2(35,0),new Vector2(35,-19));
                    Trail(paths,tile,new Vector2(0,9),new Vector2(0,20));
                    Trail(paths,tile,new Vector2(0,12),new Vector2(-35,12));
                    Trail(paths,tile,new Vector2(-35,12),new Vector2(-35,18));
                    Trail(paths,tile,new Vector2(27,11),new Vector2(33,11));
                }
            }
            // Stable positions and ResourceSpawnPoint identities participate in the existing save/regrowth.
            var tree=Object.FindFirstObjectByType<TreeResource>();var rock=Object.FindFirstObjectByType<RockResource>();
            Vector2[] trees={new Vector2(-20,-5),new Vector2(-24,4),new Vector2(-27,-8),new Vector2(-18,-12),new Vector2(-23,11),new Vector2(-28,8),new Vector2(-20,8),new Vector2(-26,-13)};
            Vector2[] rocks={new Vector2(17,-5),new Vector2(20,-9),new Vector2(23,-7),new Vector2(18,11),new Vector2(23,9),new Vector2(20,14)};
            foreach(var p in trees)AddResource(tree,p,world.transform);
            foreach(var p in rocks)AddResource(rock,p,world.transform);
            Vector2[] outerGrove={new Vector2(-27,21),new Vector2(-23,23),new Vector2(-19,19),new Vector2(-14,22),
                new Vector2(-8,21),new Vector2(8,21),new Vector2(14,23),new Vector2(20,21),new Vector2(26,23),
                new Vector2(-39,-3),new Vector2(-40,2),new Vector2(40,-5),new Vector2(40,2),
                new Vector2(-20,-22),new Vector2(-12,-21),new Vector2(-5,-23),new Vector2(6,-22),new Vector2(17,-22),new Vector2(25,-23)};
            foreach(var p in outerGrove)AddResource(tree,p,world.transform);
            // Authored pockets break up the outer grass while keeping the routes clear.
            for(int i=0;i<28;i++)
            {
                var p=new Vector3(16+(i%7)*2.1f,-15+(i/7)*9.2f);
                if(p.y>5&&p.y<9)continue;
                world.Prop("Tuft",p,.3f+(i%3)*.08f);
                if(i%6==0)world.Prop("Rock",p+new Vector3(.6f,.4f),.35f);
            }
            for(int i=0;i<VeinPositions.Length;i++)
            {
                Vector3 p=VeinPositions[i];
                var root=world.Prop("Rock",p,1.3f,true);root.name=i<2?"Cantera · hierro":"Saliente · hierro y oro";
                var vein=root.AddComponent<IronVein>();vein.Index=i;vein.Precious=i>=2;
                var mineral=world.Prop(i<2?"Iron":"Gem",p+new Vector3(0,.18f),.55f);
                mineral.transform.SetParent(root.transform,true);
                // The ore is mounted on the rock: sort it above the host, not behind its higher Y.
                mineral.GetComponent<WorldSpriteDepth>().GroundOffset=-.5f;
                var art=mineral.GetComponentInChildren<SpriteRenderer>();
                art.sprite=UI.FarmUiStyle.ItemIcon(i<2?"Iron":SurvivalItemCatalog.Find("GoldOre").Icon);
                if(art.sprite!=null)art.transform.localScale=Vector3.one*(.55f/art.sprite.bounds.size.x);
                world.Prop("Rock",p+new Vector3(1.6f,1.8f),1.6f,true);
            }
            world.Prop("Sign",new Vector3(13,1.5f),.7f);world.Label("CANTERA →\nHierro · oro más lejos",new Vector3(14,3),.065f);
            world.Prop("Sign",new Vector3(-13,1.5f),.7f);world.Label("← BOSQUE\nMadera para reparar el pueblo",new Vector3(-15,3),.065f);
            world.Label("SALIENTE DORADO",new Vector3(27,14.3f),.075f);
            world.Label("CANTERA DE HIERRO",new Vector3(21,6.6f),.075f);
            world.Prop("Sign",new Vector3(1.8f,5.2f),.65f);world.Label("ORO ↑\nCruza el puente",new Vector3(2.4f,6.3f),.06f);
            // A visible forest edge explains the boundary; no invisible wall near the village.
            for(int x=-(int)HalfWidth;x<=HalfWidth;x+=2){world.Prop("Tree",new Vector3(x,HalfHeight),2.8f);world.Prop("Tree",new Vector3(x,-HalfHeight-1.1f),2.8f);}
            for(int y=-(int)HalfHeight+2;y<HalfHeight;y+=2)
            {
                world.Prop("Tree",new Vector3(-HalfWidth-1,y),2.6f+(y%3)*.12f);
                if(y<6||y>8)world.Prop("Rock",new Vector3(HalfWidth+.5f+(y%3)*.18f,y),1.8f+(Mathf.Abs(y)%3)*.25f);
            }
            Boundary(marker.transform,new Vector2(-HalfWidth-.5f,0),new Vector2(1,HalfHeight*2+2));Boundary(marker.transform,new Vector2(HalfWidth+.5f,0),new Vector2(1,HalfHeight*2+2));
            Boundary(marker.transform,new Vector2(0,-HalfHeight-.5f),new Vector2(HalfWidth*2+2,1));Boundary(marker.transform,new Vector2(0,HalfHeight+.5f),new Vector2(HalfWidth*2+2,1));
            player.GetComponent<WildResourceRegrowth>()?.Initialize();
        }
        private static void ExtendGrass()
        {
            var grass=Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(m=>m.name=="Spring Grass");
            if(grass==null)return;
            var tile=grass.GetTile(grass.WorldToCell(new Vector3(0,-10)));
            if(tile==null)foreach(var cell in grass.cellBounds.allPositionsWithin)if(grass.HasTile(cell)){tile=grass.GetTile(cell);break;}
            if(tile==null)return;
            var min=grass.WorldToCell(new Vector3(-HalfWidth-2,-HalfHeight-2));
            var max=grass.WorldToCell(new Vector3(HalfWidth+2,HalfHeight+2));
            for(int x=min.x;x<=max.x;x++)for(int y=min.y;y<=max.y;y++)
            {
                var cell=new Vector3Int(x,y,0);var p=grass.GetCellCenterWorld(cell);
                if((Mathf.Abs(p.x)>31.5f||Mathf.Abs(p.y)>17.5f)&&!grass.HasTile(cell))grass.SetTile(cell,tile);
            }
        }
        private static void ExtendRiverAndRemoveOldEdges()
        {
            // The authored map had a second, larger perimeter behind the old demo boundary.
            // Replace only those four colliders; the river and bridge remain solid as before.
            foreach(var body in Object.FindObjectsByType<BoxCollider2D>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(body.name=="West World Edge"||body.name=="East World Edge"||body.name=="North World Edge"||body.name=="South World Edge")body.enabled=false;
                if(body.name=="West River Bank"||body.name=="East River Bank")
                {
                    float side=body.name=="West River Bank"?-1:1;
                    body.transform.position=new Vector3(side*(HalfWidth+1.1f)*.5f,7,0);
                    body.size=new Vector2(HalfWidth-1.1f,2.9f);
                }
            }
            var art=Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var waterRows=art.Where(s=>s.name=="River Water").GroupBy(s=>Mathf.RoundToInt(s.transform.position.y*10)).Select(g=>g.First()).ToArray();
            var north=art.FirstOrDefault(s=>s.name=="North Shore");
            var south=art.FirstOrDefault(s=>s.name=="South Shore");
            for(int x=-(int)HalfWidth;x<HalfWidth;x++)
            {
                if(x>=-37&&x<37)continue;
                foreach(var water in waterRows)CloneColumn(water,x+.5f);
                if(north!=null)CloneColumn(north,x+.5f);
                if(south!=null)CloneColumn(south,x+.5f);
            }
        }
        private static void CloneColumn(SpriteRenderer template,float x)
        {
            var copy=Object.Instantiate(template.gameObject,template.transform.parent);
            copy.name=template.name;
            copy.transform.position=new Vector3(x,template.transform.position.y,template.transform.position.z);
        }
        private static void AddResource(HarvestableResource template,Vector2 p,Transform parent)
        {
            if(template==null)return;
            var copy=Object.Instantiate(template,parent);copy.name=template is TreeResource?"Árbol del bosque exterior":"Roca de la cantera";
            copy.Spawn(p);ResourceSpawnPoint.Attach(copy);
        }
        private static void Trail(Tilemap map,TileBase tile,Vector2 a,Vector2 b)
        {
            int steps=Mathf.CeilToInt(Vector2.Distance(a,b)*2);
            for(int i=0;i<=steps;i++)
            {
                Vector3 p=Vector2.Lerp(a,b,i/(float)Mathf.Max(1,steps));
                for(int side=-1;side<=1;side++)map.SetTile(map.WorldToCell(p+(Mathf.Abs(a.x-b.x)<.1f?Vector3.right:Vector3.up)*side*.5f),tile);
            }
        }
        private static void Boundary(Transform parent,Vector2 p,Vector2 size)
        {
            var root=new GameObject("Borde natural del valle");root.transform.SetParent(parent,false);root.transform.position=p;root.AddComponent<BoxCollider2D>().size=size;
        }
    }
}
