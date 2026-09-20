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
            audioFeedback=GetComponent<AudioFeedback>();
            if(audioFeedback==null)audioFeedback=gameObject.AddComponent<AudioFeedback>();
            if(GetComponent<HitFeedback>()==null)gameObject.AddComponent<HitFeedback>();
        }
        public void Pulse(string text,Vector3 position,bool damage=false,bool fabrication=false,bool playAudio=true)
        {
            if(!Enabled)return;
            if(playAudio)audioFeedback?.Play(damage?CombatSound.Impact:fabrication?CombatSound.Craft:CombatSound.Pickup,position);
            var label=FloatingFeedbackPool.For(transform).Rent();
            if(label!=null)label.Play(text,position,damage);
        }
    }
    public sealed class FloatingFeedback : MonoBehaviour
    {
        float life=.7f;
        private TextMesh label;
        private FloatingFeedbackPool pool;
        private bool leased;
        internal void Initialize(TextMesh visual) => label=visual;
        internal void Lease(FloatingFeedbackPool owner){pool=owner;leased=true;}
        internal void Play(string text,Vector3 position,bool damage)
        {
            life=.7f;
            transform.position=position+Vector3.up*.65f;
            transform.rotation=Quaternion.identity;transform.localScale=Vector3.one;
            label.text=text;
            label.color=damage?new Color(1,.5f,.4f):new Color(1,.95f,.6f);
            gameObject.SetActive(true);
        }
        public void ReturnToPool()
        {
            if(pool!=null&&!leased)return;
            bool returnLease=leased&&pool!=null;
            leased=false;life=0;
            if(label!=null)label.text=string.Empty;
            if(returnLease)pool.Return(this);
            else Destroy(gameObject);
        }
        void Update(){life-=Time.deltaTime;transform.position+=Vector3.up*Time.deltaTime*.6f;if(life<=0)ReturnToPool();}
        void OnDisable()
        {
            if(label!=null)label.text=string.Empty;
            // The owner defers reparenting until outside an activation/destruction callback.
            if(leased&&pool!=null)pool.Deactivated(this);
        }
        void OnDestroy(){if(pool!=null)pool.Forget(this);}
    }
}
