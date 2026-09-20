using System.Linq;
using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    /// <summary>Composes existing pack tiles and animation strips. No generated replacement art.</summary>
    public static class PackEnvironment
    {
        public static SpriteRenderer Sprite(Transform parent,string name,string atlas,int x,int y,int w,int h,Vector2 p,float scale=1)
        {
            var root=new GameObject(name);root.transform.SetParent(parent,false);root.transform.position=p;root.transform.localScale=Vector3.one*scale;
            var art=root.AddComponent<SpriteRenderer>();art.sprite=HouseSprites.Slice(atlas,x,y,w,h);
            root.AddComponent<WorldSpriteDepth>().Visual=art;return art;
        }
        public static void Animate(SpriteRenderer art,string atlas,int x,int y,int w,int h,int count,int dx,int dy,float fps)
        {
            var animation=art.GetComponent<EnvironmentSpriteAnimation>()??art.gameObject.AddComponent<EnvironmentSpriteAnimation>();
            animation.Visual=art;animation.Frames=Enumerable.Range(0,count).Select(i=>HouseSprites.Slice(atlas,x+i*dx,y+i*dy,w,h)).ToArray();animation.FramesPerSecond=fps;
        }
        public static void Configure(ValleyWorld world)
        {
            var root=new GameObject("Animaciones originales · orillas y vida").transform;root.SetParent(world.transform,false);
            foreach(var ripple in Object.FindObjectsByType<WaterRipple>(FindObjectsSortMode.None))ripple.gameObject.SetActive(false);
            // Grass banks use the same 32 px/unit as the existing half-unit terrain
            // tiles. Replace the dirt lip without moving the water or either bank.
            foreach(var shore in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                bool north=shore.name=="North Shore";
                if(!north&&shore.name!="South Shore")continue;
                var bounds=shore.bounds;
                shore.enabled=false;
                for(int half=0;half<2;half++)
                {
                    var position=new Vector2(bounds.center.x+(half==0?-.25f:.25f),north?bounds.min.y:bounds.max.y-.25f);
                    // Keep water behind the transparent grass fringe. The native
                    // foam crop starts after row 4: no brown bank pixels in any frame.
                    var water=Sprite(root,"Agua bajo la orilla","LivingWater",352,8,16,8,position,.5f);
                    water.GetComponent<WorldSpriteDepth>().enabled=false;water.sortingOrder=-59;
                    if(north)
                    {
                        var foam=Sprite(root,"Agua animada sin tierra","LivingWater",352,5,16,11,position+Vector2.down*(9f/32f),.5f);
                        foam.GetComponent<WorldSpriteDepth>().enabled=false;foam.sortingOrder=-58;
                        Animate(foam,"LivingWater",352,5,16,11,4,0,64,4);
                        foam.GetComponent<EnvironmentSpriteAnimation>().Synchronized=true;
                    }
                    var bank=Sprite(root,north?"Orilla norte de pasto":"Orilla sur de pasto","TerrainAtlas",32,north?56:0,16,8,
                        position,.5f);
                    bank.GetComponent<WorldSpriteDepth>().enabled=false;bank.sortingOrder=-57;
                }
            }
            foreach(var bridge in Object.FindObjectsByType<RepairableBridge>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                foreach(var old in bridge.GetComponentsInChildren<SpriteRenderer>(true))old.enabled=false;
                // Mirror the old view horizontally. Centre the traversable
                // planks, not the asymmetric bottom-pivot sprite, on the river opening.
                var art=Sprite(root,"Puente de tablones y barandas del pack","PackBridge",8,4,64,51,bridge.transform.position,.85f);
                art.transform.rotation=Quaternion.Euler(0,0,-90);
                art.flipX=true;
                art.transform.position-=art.transform.TransformVector(new Vector3(0,29f/16f));
                art.GetComponent<WorldSpriteDepth>().enabled=false;art.sortingOrder=-48;
                // Collider strips follow the actual 13px and 20px rail bands in the
                // source art. They rotate with it and leave the central deck open.
                BridgeRail(art.transform,38f/16f,13f/16f);
                BridgeRail(art.transform,0,20f/16f);
            }
            Vector2[] pockets={new Vector2(-9,-5),new Vector2(10,-8),new Vector2(-17,3),new Vector2(15,-10),new Vector2(-22,-9)};
            for(int i=0;i<pockets.Length;i++)
            {
                var p=pockets[i];
                var butterfly=Sprite(root,"Mariposa del valle","PackButterfly",0,0,16,16,p+Vector2.up*1.2f,.7f);
                Animate(butterfly,"PackButterfly",0,0,16,16,7,16,0,8);butterfly.gameObject.AddComponent<AmbientFlight>();
                Sprite(root,"Ramitas del sendero","PackWoodDebris",0,0,64,16,p+Vector2.right*.8f,.28f);
                if(i<3)
                {
                    var rabbit=Sprite(root,"Liebre del valle","PackRabbit",0,0,16,16,p+Vector2.down*.9f,.8f);
                    rabbit.gameObject.AddComponent<CircleCollider2D>().isTrigger=true;
                    var animal=rabbit.gameObject.AddComponent<AnimalResource>();animal.Configure(rabbit,null,2,1);animal.ConfigureHealth(2);
                    animal.ConfigureAnimation(rabbit,Enumerable.Range(0,4).Select(f=>HouseSprites.Slice("PackRabbit",f*16,0,16,16)).ToArray());
                    animal.DeathSound=CombatSound.RabbitDeath;ResourceSpawnPoint.Attach(animal);
                }
            }
        }
        static void BridgeRail(Transform bridge,float bottom,float height)
        {
            var rail=new GameObject("Baranda del puente");rail.transform.SetParent(bridge,false);
            var collider=rail.AddComponent<BoxCollider2D>();collider.size=new Vector2(4,height);
            collider.offset=new Vector2(0,bottom+height*.5f);
        }
    }
    public sealed class AmbientFlight : MonoBehaviour
    {
        private Vector3 origin;private float phase;
        private void Start(){origin=transform.position;phase=origin.x*.7f;}
        private void Update()=>transform.position=origin+new Vector3(Mathf.Sin(Time.time*.7f+phase)*.55f,Mathf.Sin(Time.time*1.1f+phase)*.18f);
    }
}
