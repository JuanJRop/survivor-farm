using UnityEngine;
using SurvivorFarm.Runtime.Player;
namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class GameFeelFeedback : MonoBehaviour
    {
        AudioSource source;AudioClip collect,hit,craft;
        public static bool Enabled=true;
        void Awake()
        {
            source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;source.volume=.22f;
            collect=Tone("Recoger",720,.10f);hit=Tone("Impacto",150,.09f);craft=Tone("Fabricar",960,.16f);
        }
        static AudioClip Tone(string name,float frequency,float duration)
        {
            int count=(int)(22050*duration);var data=new float[count];
            for(int i=0;i<count;i++){float t=i/22050f;data[i]=Mathf.Sin(t*frequency*Mathf.PI*2)*(1-i/(float)count)*Mathf.Min(1,t*100);}
            var clip=AudioClip.Create(name,count,1,22050,false);clip.SetData(data,0);return clip;
        }
        public void Pulse(string text,Vector3 position,bool damage=false,bool fabrication=false)
        {
            if(!Enabled)return;source.PlayOneShot(damage?hit:fabrication?craft:collect);
            var root=new GameObject("Respuesta: "+text);root.transform.position=position+Vector3.up*.65f;
            var label=root.AddComponent<TextMesh>();label.text=text;label.fontSize=32;label.characterSize=.045f;label.anchor=TextAnchor.MiddleCenter;label.color=damage?new Color(1,.5f,.4f):new Color(1,.95f,.6f);
            root.GetComponent<MeshRenderer>().sortingOrder=20000;root.AddComponent<FloatingFeedback>();
        }
        void OnDestroy(){Destroy(collect);Destroy(hit);Destroy(craft);}
    }
    public sealed class FloatingFeedback : MonoBehaviour
    {
        float life=.7f;
        void Update(){life-=Time.deltaTime;transform.position+=Vector3.up*Time.deltaTime*.6f;if(life<=0)Destroy(gameObject);}
    }
}
