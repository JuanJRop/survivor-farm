using SurvivorFarm.Runtime.Core;
using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    public sealed class FarmAtmosphere : MonoBehaviour
    {
        private Texture2D texture;
        private Sprite glow;
        private SpriteRenderer[] lamps;
        private DayNightCycle clock;
        private void Start()
        {
            clock=FindFirstObjectByType<DayNightCycle>();
            texture=new Texture2D(32,32,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear};
            var pixels=new Color[1024];
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {
                float radius=Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))/16;
                pixels[y*32+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-radius),2));
            }
            texture.SetPixels(pixels);texture.Apply();
            glow=Sprite.Create(texture,new Rect(0,0,32,32),new Vector2(.5f,.5f),16);
            Vector3[] positions={new Vector3(-1.1f,.3f),new Vector3(-3.8f,-2.3f),new Vector3(-5.6f,1.7f),new Vector3(5.6f,1.7f)};
            lamps=new SpriteRenderer[positions.Length];
            for(int i=0;i<lamps.Length;i++)
            {
                var root=new GameObject("Luz cálida de granja");root.transform.SetParent(transform,false);root.transform.position=positions[i];root.transform.localScale=Vector3.one*2.8f;
                lamps[i]=root.AddComponent<SpriteRenderer>();lamps[i].sprite=glow;lamps[i].sortingOrder=12000;
            }
        }
        private void Update()
        {
            if(lamps==null)return;
            float strength=clock!=null&&clock.Hour>=18?.35f:.055f;
            for(int i=0;i<lamps.Length;i++)lamps[i].color=new Color(1,.66f,.24f,strength*(.93f+.07f*Mathf.Sin(Time.time*2+i)));
        }
        private void OnDestroy(){if(glow!=null)Destroy(glow);if(texture!=null)Destroy(texture);}
    }
}
