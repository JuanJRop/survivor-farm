using System.Collections.Generic;
using SurvivorFarm.Runtime.Core;
using SurvivorFarm.Runtime.Gameplay;
using SurvivorFarm.Runtime.Player;
using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    public static class PracticeWorld
    {
        public static void BuildArena(PortfolioSession session)
        {
            var world=session.Player.GetComponent<ValleyCampaign>().World;
            // Keep inactive authored objects for references, but never their colliders or behaviours.
            var outdoor=GameObject.Find("02 Mundo");
            if(outdoor==null)throw new System.InvalidOperationException("Arena requires Main's authored world section.");
            outdoor.SetActive(false);world.gameObject.SetActive(false);
            session.Player.GetComponent<ConstructionSystem>().Restore(new List<BuildingData>());
            var regrowth=session.Player.GetComponent<WildResourceRegrowth>();if(regrowth!=null)regrowth.enabled=false;
            var root=new GameObject("Arena · claro del entrenamiento");
            for(int x=-23;x<=23;x++)for(int y=-18;y<=13;y++)
            {
                var grass=world.Prop("Grass",new Vector3(x,y),1);
                grass.transform.SetParent(root.transform,true);
                Object.Destroy(grass.GetComponent<WorldSpriteDepth>());
                grass.GetComponentInChildren<SpriteRenderer>().sortingOrder=-30000;
            }
            var path=new HashSet<Vector2Int>();
            for(int x=-13;x<=13;x++)for(int y=-9;y<=4;y++)
                if(x*x/169f+(y+2.5f)*(y+2.5f)/42.25f<1)path.Add(new Vector2Int(x,y));
            root.AddComponent<ValleyTerrain>().Paint(path,Vector3.zero,Vector3.right,Vector3.up,-29900);
            for(int x=-17;x<=17;x+=2)
            {
                Decor(world,root.transform,"Tree",new Vector3(x,5.2f),2.5f);
                Decor(world,root.transform,"Rock",new Vector3(x,-10.5f),1.5f);
            }
            for(int y=-9;y<5;y+=2)
            {
                Decor(world,root.transform,"Rock",new Vector3(-16.5f,y),1.5f);
                Decor(world,root.transform,"Tree",new Vector3(16.5f,y),2.5f);
            }
            Wall(root.transform,new Vector2(0,5),new Vector2(34,1));
            Wall(root.transform,new Vector2(0,-10),new Vector2(34,1));
            Wall(root.transform,new Vector2(-16, -2.5f),new Vector2(1,16));
            Wall(root.transform,new Vector2(16, -2.5f),new Vector2(1,16));
            session.Player.GetComponent<ValleyCampaign>().Teleport(new Vector3(0,-2));
            if(Camera.main!=null){Camera.main.orthographicSize=6.8f;Camera.main.GetComponent<CameraFollowTarget>()?.SetTarget(session.Player.transform);}
        }
        private static void Decor(ValleyWorld world,Transform parent,string key,Vector3 position,float width)
        {
            var prop=world.Prop(key,position,width);prop.transform.SetParent(parent,true);
            var fader=prop.GetComponent<TreeOcclusionFader>();if(fader!=null)fader.enabled=false;
        }
        private static void Wall(Transform parent,Vector2 position,Vector2 size)
        {
            var go=new GameObject("Borde visible de la arena");go.transform.SetParent(parent,false);go.transform.position=position;
            go.AddComponent<BoxCollider2D>().size=size;
        }
    }
}
