using System.Collections.Generic;
using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SurvivorFarm.Runtime.World
{
    /// <summary>Art direction on top of Main's terrain and stable resource identities.</summary>
    public static class FarmWorldPolish
    {
        public static void Configure(ValleyWorld world,PlayerInventory player)
        {
            var root=new GameObject("Valle vivo");root.transform.SetParent(world.transform,false);
            var paths=Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None).FirstOrDefault(m=>m.name=="Farm Paths");
            if(paths!=null)
            {
                var cells=new HashSet<Vector2Int>();
                foreach(var c in paths.cellBounds.allPositionsWithin)if(paths.HasTile(c))cells.Add(new Vector2Int(c.x,c.y));
                var terrain=root.AddComponent<ValleyTerrain>();var renderer=paths.GetComponent<TilemapRenderer>();
                terrain.Paint(cells,paths.CellToWorld(Vector3Int.zero),paths.CellToWorld(Vector3Int.right)-paths.CellToWorld(Vector3Int.zero),
                    paths.CellToWorld(Vector3Int.up)-paths.CellToWorld(Vector3Int.zero),renderer.sortingOrder+1);
                renderer.enabled=false;
            }
            foreach(var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if(sr.name=="Grass Tuft"||sr.transform.parent!=null&&sr.transform.parent.name=="Tuft")sr.enabled=false;
                if(sr.name.StartsWith("Chicken ")&&sr.GetComponent<AnimalResource>()==null)
                {
                    var collider=sr.gameObject.AddComponent<CircleCollider2D>();collider.radius=.2f;collider.isTrigger=true;
                    var chicken=sr.gameObject.AddComponent<AnimalResource>();chicken.Configure(sr,null,2,1);chicken.ConfigureHealth(2);
                    chicken.DeathSound=CombatSound.ChickenDeath;
                    ResourceSpawnPoint.Attach(chicken);
                }
            }
            // The village props use the same harvest/save contract as forest trees.
            foreach(var prop in world.GetComponentsInChildren<Transform>().Where(t=>t.name=="Tree").ToArray())
            {
                if(!FarmExploration.Contains(prop.position,1))continue;
                if(FarmExploration.IsRiver(prop.position,.45f)){prop.gameObject.SetActive(false);continue;}
                if(prop.GetComponent<TreeResource>()!=null)continue;
                var art=prop.GetComponentInChildren<SpriteRenderer>();
                var tree=prop.gameObject.AddComponent<TreeResource>();tree.Configure(art,null,4,1);ResourceSpawnPoint.Attach(tree);
            }
            foreach(var resource in Object.FindObjectsByType<HarvestableResource>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(resource is TreeResource&&FarmExploration.IsRiver(resource.transform.position,.5f))
                {
                    var p=resource.transform.position;p.y=p.y<7?4.4f:10;
                    var spawn=resource.GetComponentInParent<ResourceSpawnPoint>();if(spawn!=null)spawn.transform.position=p;
                    resource.transform.position=p;
                }
            }
            foreach(var resource in Object.FindObjectsByType<HarvestableResource>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {ResourceTier.Configure(resource);StyleResource(resource);}
            foreach(var building in Object.FindObjectsByType<VillageBuildingService>(FindObjectsInactive.Include,FindObjectsSortMode.None))TreeOcclusionFader.Ensure(building.gameObject);
            foreach(var building in Object.FindObjectsByType<BaseHouse>(FindObjectsSortMode.None))TreeOcclusionFader.Ensure(building.gameObject);
            var village=world.transform.Find("Pueblo inicial - Raizclara");
            if(village!=null)foreach(Transform item in village)
            {
                if(item.name=="Fence"||item.name=="Sign"||item.name=="Workbench"||item.name=="Chest")item.gameObject.SetActive(false);
                if(item.name=="Tree"&&item.position.y<-6)item.position+=new Vector3(-2.8f,-1.1f);
            }
            foreach(var sign in world.GetComponentsInChildren<Transform>())if(sign.name=="Sign")sign.gameObject.SetActive(false);
            foreach(var label in world.GetComponentsInChildren<TextMesh>())if(label.text!="HUERTO"&&!label.text.StartsWith("POZO"))label.gameObject.SetActive(false);
            foreach(var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                if(sr.name=="Shipping Chest"||sr.name=="Farm Sign")sr.gameObject.SetActive(false);

            // Authored pockets instead of tiny evenly distributed grass specks; leave roads and footprints clear.
            var rng=new System.Random(15627);var accepted=new List<Vector2>();
            for(int attempt=0;attempt<2600&&accepted.Count<330;attempt++)
            {
                var p=new Vector2(-30+(float)rng.NextDouble()*60,-16+(float)rng.NextDouble()*32);
                if(p.y>5.3f&&p.y<8.9f||Mathf.Abs(p.x)<2&&Mathf.Abs(p.y)<3)continue;
                if(paths!=null)
                {
                    var cell=paths.WorldToCell(p);
                    if(paths.HasTile(cell)||paths.HasTile(cell+Vector3Int.left)||paths.HasTile(cell+Vector3Int.right)||paths.HasTile(cell+Vector3Int.up)||paths.HasTile(cell+Vector3Int.down))continue;
                }
                if(Physics2D.OverlapCircleAll(p,.65f).Any(c=>!c.isTrigger||c.GetComponent<FarmingPlot>()?.IsAvailable==true)||accepted.Any(q=>(q-p).sqrMagnitude<.75f))continue;
                accepted.Add(p);int kind=rng.Next(5);
                Sprite sprite=kind<2?HouseSprites.Slice("TerrainAtlas",144,64,16,16):kind==2?HouseSprites.Slice("SunflowerAtlas",80,0,16,32):HouseSprites.Slice("BushAtlas",48,kind==3?32:192,48,32);
                // All ground vegetation shares the map's 32 px/unit scale.
                // Width-based scaling made identical grass pixels change size by kind.
                var plant=Decor(root.transform,"Vegetación agrupada",sprite,p,kind<2?.5f:kind==2?.5f:1.5f);
                plant.gameObject.AddComponent<ForagePlant>().Configure(kind>=3);
            }
            foreach(var sr in world.GetComponentsInChildren<SpriteRenderer>())
                if(sr.transform.parent!=null&&sr.transform.parent.name=="Tree"&&sr.GetComponentInParent<TreeResource>()==null)AnimateTree(sr,1);
            foreach(var bank in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
                if(bank.name=="West River Bank"||bank.name=="East River Bank")bank.size=new Vector2(bank.size.x,2.9f);
            // Water animation is confined to the north bank by PackEnvironment.
        }
        public static void StyleResource(HarvestableResource resource)
        {
            var tier=resource.GetComponent<ResourceTier>();if(tier==null)return;
            var art=resource.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(s=>s.sprite!=null);if(art==null)return;
            if(resource is TreeResource tree)TreePresentation.Configure(tree,tier.Tier);
            else if(resource is RockResource)
            {
                art.sprite=HouseSprites.Slice("MineralRocks",tier.Tier==1?0:tier.Tier==2?128:96,144,32,16);
                if(art.transform!=resource.transform)art.transform.localScale=Vector3.one;
            }
        }
        static void AnimateTree(SpriteRenderer art,int tier)
        {
            var animation=art.GetComponent<EnvironmentSpriteAnimation>()??art.gameObject.AddComponent<EnvironmentSpriteAnimation>();
            string atlas=tier==2?"LivingBirch":"LivingMaple";int row=tier==3?96:48;
            animation.Visual=art;animation.FramesPerSecond=2.3f;
            animation.Frames=new[]{HouseSprites.Slice(atlas,0,row,32,48),HouseSprites.Slice(atlas,64,row,32,48),HouseSprites.Slice(atlas,96,row,32,48)};
            art.sprite=animation.Frames[0];
        }
        static SpriteRenderer Decor(Transform parent,string name,Sprite sprite,Vector2 p,float width)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=p;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;
            if(sprite!=null)go.transform.localScale=Vector3.one*(width/sprite.bounds.size.x);
            go.AddComponent<WorldSpriteDepth>().Visual=sr;return sr;
        }
    }
}
