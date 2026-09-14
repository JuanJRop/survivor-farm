using UnityEngine;

namespace SurvivorFarm.Runtime.Gameplay
{
    [CreateAssetMenu(menuName = "Survivor Farm/Flyweights/Resource Animation")]
    public sealed class ResourceAnimation : ScriptableObject
    {
        [SerializeField] private Sprite[] frames = new Sprite[0];
        [SerializeField, Min(1f)] private float framesPerSecond = 6f;
        public int FrameCount => frames.Length;
        public float FramesPerSecond => framesPerSecond;
        public Sprite GetFrame(int index) => frames[index];
        public Sprite Evaluate(float time) => frames.Length == 0 ? null : frames[(int)(time * framesPerSecond) % frames.Length];

        internal void Initialize(Sprite[] source, float rate)
        {
            frames = (Sprite[])source.Clone();
            framesPerSecond = Mathf.Max(1f, rate);
        }
    }
}
