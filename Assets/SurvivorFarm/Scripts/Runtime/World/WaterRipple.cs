using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    /// <summary>Three pixel strokes drift and fade; no collision or per-frame allocation.</summary>
    public sealed class WaterRipple : MonoBehaviour
    {
        static Sprite pixel;SpriteRenderer[] strokes;Vector3 origin;float offset;
        public static void Create(Transform parent,Vector2 position)
        {
            var root=new GameObject("Reflejos animados");root.transform.SetParent(parent,false);root.transform.position=position;
            root.AddComponent<WaterRipple>();
        }
        void Awake()
        {
            if(pixel==null)pixel=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,2,2),new Vector2(.5f,.5f),32);
            origin=transform.position;offset=Mathf.Abs(origin.x*.13f);strokes=new SpriteRenderer[3];
            for(int i=0;i<3;i++)
            {
                var go=new GameObject("Trazo de luz");go.transform.SetParent(transform,false);go.transform.localPosition=new Vector3(i==1?.12f:0,i*.12f);
                go.transform.localScale=new Vector3(i==1?3:2,.65f,1);strokes[i]=go.AddComponent<SpriteRenderer>();strokes[i].sprite=pixel;strokes[i].sortingOrder=-57;
            }
        }
        void Update()
        {
            float phase=Mathf.Repeat(Time.time*.32f+offset,1);transform.position=origin+Vector3.right*(phase*.25f);
            var color=new Color(.66f,.91f,1,Mathf.Sin(phase*Mathf.PI)*.85f);
            foreach(var sr in strokes)sr.color=color;
        }
    }
}
