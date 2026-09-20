using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    /// <summary>A small world-space clock beside the tool hand. No canvas or text meter.</summary>
    public sealed class WorldActionClock : MonoBehaviour
    {
        private GameObject clock;
        private LineRenderer hand,rim;
        private Material material;
        private Behaviour owner;
        public bool IsVisible=>clock!=null&&clock.activeSelf;
        public float Progress {get;private set;}
        public static WorldActionClock For(GameObject player)=>player.GetComponent<WorldActionClock>()??player.AddComponent<WorldActionClock>();
        public void Show(Behaviour source,float progress)
        {
            owner=source;Progress=Mathf.Clamp01(progress);
            if(clock==null)Build();clock.SetActive(true);
            float a=Mathf.PI*.5f-Progress*Mathf.PI*2;
            hand.SetPosition(1,new Vector3(Mathf.Cos(a),Mathf.Sin(a))*.11f);
            rim.startColor=rim.endColor=Progress>.95f?new Color(.6f,1,.68f):new Color(1,.85f,.48f);
        }
        public void Hide(Behaviour source){if(owner!=source)return;owner=null;if(clock!=null)clock.SetActive(false);}
        private LineRenderer Line(string label,int count,float width,Color color,int order)
        {
            var root=new GameObject(label);root.transform.SetParent(clock.transform,false);
            var line=root.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=false;
            line.positionCount=count;line.widthMultiplier=width;line.startColor=line.endColor=color;line.sortingOrder=order;
            return line;
        }
        private void Build()
        {
            clock=new GameObject("Reloj de trabajo");clock.transform.SetParent(transform,false);clock.transform.localPosition=new Vector3(.48f,.72f);
            material=new Material(Shader.Find("Sprites/Default")){hideFlags=HideFlags.DontSave};
            var shadow=Line("Borde oscuro",24,.055f,new Color(.12f,.16f,.16f),15000);shadow.loop=true;
            rim=Line("Esfera",24,.024f,Color.white,15001);rim.loop=true;
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI*2/24;var p=new Vector3(Mathf.Cos(a),Mathf.Sin(a))*.145f;
                shadow.SetPosition(i,p);rim.SetPosition(i,p);
            }
            hand=Line("Aguja",2,.026f,Color.white,15002);hand.SetPosition(0,Vector3.zero);
            var twelve=Line("Doce",2,.03f,Color.white,15002);twelve.SetPosition(0,Vector3.up*.115f);twelve.SetPosition(1,Vector3.up*.145f);
        }
        private void Update(){if(IsVisible&&(owner==null||!owner.isActiveAndEnabled)){owner=null;clock.SetActive(false);}}
        private void OnDisable(){owner=null;if(clock!=null)clock.SetActive(false);}
        private void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
