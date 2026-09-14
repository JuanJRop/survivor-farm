using UnityEngine;
using SurvivorFarm.Runtime.Player;
namespace SurvivorFarm.Runtime.Gameplay
{
    public sealed class GameFeelFeedback : MonoBehaviour
    {
        AudioFeedback audioFeedback;
        public static bool Enabled=true;
        void Awake()
        {
            audioFeedback=GetComponent<AudioFeedback>()??gameObject.AddComponent<AudioFeedback>();
            if(GetComponent<HitFeedback>()==null)gameObject.AddComponent<HitFeedback>();
        }
        public void Pulse(string text,Vector3 position,bool damage=false,bool fabrication=false,bool playAudio=true)
        {
            if(!Enabled)return;
            if(playAudio)audioFeedback?.Play(damage?CombatSound.Impact:fabrication?CombatSound.Craft:CombatSound.Pickup,position);
            var root=new GameObject("Respuesta: "+text);root.transform.position=position+Vector3.up*.65f;
            var label=root.AddComponent<TextMesh>();label.text=text;label.fontSize=32;label.characterSize=.045f;label.anchor=TextAnchor.MiddleCenter;label.color=damage?new Color(1,.5f,.4f):new Color(1,.95f,.6f);
            root.GetComponent<MeshRenderer>().sortingOrder=20000;root.AddComponent<FloatingFeedback>();
        }
    }
    public sealed class FloatingFeedback : MonoBehaviour
    {
        float life=.7f;
        void Update(){life-=Time.deltaTime;transform.position+=Vector3.up*Time.deltaTime*.6f;if(life<=0)Destroy(gameObject);}
    }
}
