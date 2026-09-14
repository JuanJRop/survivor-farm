using UnityEngine;
namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class CultivationSoilVisual : MonoBehaviour
    {
        SpriteRenderer ground;SpriteRenderer[] rows;Color target;bool initialized;
        public static Color Tint(int state)=>state==0?Color.clear:state==1?new Color(1,.94f,.86f):state>=4?new Color(.68f,.61f,.52f):new Color(.83f,.75f,.64f);
        public void Show(SpriteRenderer source,Sprite soil,int state)
        {
            ground=source;ground.sprite=soil;target=Tint(state);
            if(!initialized){ground.color=target;initialized=true;}
            if(rows==null&&state>=2)
            {
                rows=new SpriteRenderer[3];
                for(int i=0;i<3;i++)
                {
                    var go=new GameObject("Surco "+(i+1));go.transform.SetParent(ground.transform,false);
                    go.transform.localPosition=new Vector3(0,(i-1)*soil.bounds.size.y*.23f,-.001f);
                    go.transform.localScale=new Vector3(.72f,.055f,1);rows[i]=go.AddComponent<SpriteRenderer>();rows[i].sprite=soil;
                    rows[i].sortingLayerID=ground.sortingLayerID;rows[i].sortingOrder=ground.sortingOrder+1;
                }
            }
            if(rows!=null)foreach(var row in rows){row.enabled=state>=2;row.color=state>=4?new Color(.42f,.34f,.25f):new Color(.6f,.46f,.32f);}
        }
        void Update(){if(ground!=null)ground.color=Color.Lerp(ground.color,target,1-Mathf.Exp(-Time.deltaTime*12));}
        static Material dustMaterial;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetMaterial(){if(dustMaterial!=null)Destroy(dustMaterial);dustMaterial=null;}
        public static void Emit(Vector3 position,bool water)
        {
            var go=new GameObject(water?"Gotas sobre el surco":"Tierra removida");go.transform.position=position;
            var particles=go.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;main.loop=false;main.playOnAwake=false;main.startLifetime=.35f;main.startSpeed=new ParticleSystem.MinMaxCurve(.5f,1.2f);main.startSize=new ParticleSystem.MinMaxCurve(.045f,.09f);main.startColor=water?new Color(.6f,.83f,1):new Color(.58f,.32f,.17f);main.gravityModifier=.25f;main.maxParticles=10;
            var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=.24f;
            var emission=particles.emission;emission.rateOverTime=0;
            var renderer=particles.GetComponent<ParticleSystemRenderer>();if(dustMaterial==null)dustMaterial=new Material(Shader.Find("Sprites/Default"));renderer.sharedMaterial=dustMaterial;renderer.sortingOrder=20000;
            particles.Play();particles.Emit(10);Destroy(go,.6f);
        }
        public void SetImmediate(){if(ground!=null)ground.color=target;}
    }
}
