using UnityEngine;

namespace SurvivorFarm.Runtime.World
{
    /// <summary>Lightweight shared-atlas animation; never changes collisions or sprite scale.</summary>
    public sealed class EnvironmentSpriteAnimation : MonoBehaviour
    {
        public SpriteRenderer Visual;public Sprite[] Frames;public float FramesPerSecond=3;
        public bool Synchronized;
        private float phase;
        void Awake()=>phase=Mathf.Abs(transform.position.x*.31f+transform.position.y*.53f);
        void LateUpdate(){if(Visual!=null&&Frames!=null&&Frames.Length>0)Visual.sprite=Frames[(int)((Time.time+(Synchronized?0:phase))*FramesPerSecond)%Frames.Length];}
    }
}
