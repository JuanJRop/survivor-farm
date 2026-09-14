using UnityEngine;
namespace SurvivorFarm.Runtime.Gameplay
{
    [DefaultExecutionOrder(2000)]
    public sealed class VisibleHitFeedback : MonoBehaviour
    {
        SpriteRenderer[] renderers;Material[] originals;float until;
        static Material white;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetMaterial(){if(white!=null)Destroy(white);white=null;}
        public bool IsFlashing=>Time.time<until;
        public static Material WhiteMaterial {get {if(white==null){var shader=Resources.Load<Shader>("CombatHitFlash");if(shader!=null)white=new Material(shader){hideFlags=HideFlags.DontSave};}return white;}}
        public static void Play(GameObject target, float duration=.12f, bool particles=true)
        {
            var effect=target.GetComponent<VisibleHitFeedback>()??target.AddComponent<VisibleHitFeedback>();
            if(effect.renderers==null){effect.renderers=target.GetComponentsInChildren<SpriteRenderer>();effect.originals=new Material[effect.renderers.Length];for(int i=0;i<effect.renderers.Length;i++)effect.originals[i]=effect.renderers[i].sharedMaterial;}
            effect.until=Time.time+duration;
            var position=effect.renderers.Length>0?effect.renderers[0].bounds.center:target.transform.position;
            if(particles)CombatHitParticles.Spawn(position,target.transform.parent,WhiteMaterial);effect.LateUpdate();
        }
        void LateUpdate(){if(renderers==null)return;for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)renderers[i].sharedMaterial=IsFlashing&&WhiteMaterial!=null?WhiteMaterial:originals[i];}
        public void ResetFlash(){until=0;LateUpdate();}
        void OnDisable()=>ResetFlash();
    }
}
