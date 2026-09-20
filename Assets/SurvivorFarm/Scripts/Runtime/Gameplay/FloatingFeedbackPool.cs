using System.Collections.Generic;
using SurvivorFarm.Runtime.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SurvivorFarm.Runtime.Gameplay
{
    /// <summary>Bounded cosmetic labels share their TextMesh and renderer for the life of a scene.</summary>
    public sealed class FloatingFeedbackPool : MonoBehaviour
    {
        public const int MaximumLabels = 24;
        private static readonly Dictionary<int,FloatingFeedbackPool> scenes = new Dictionary<int,FloatingFeedbackPool>();
        private readonly Queue<FloatingFeedback> disabled = new Queue<FloatingFeedback>(MaximumLabels);
        private SceneComponentPool<FloatingFeedback> pool;
        private int sceneHandle;
        public int CreatedCount => pool!=null?pool.CreatedCount:0;
        public int ActiveCount => pool!=null?pool.ActiveCount:0;
        public int InactiveCount => pool!=null?pool.InactiveCount:0;

        public static FloatingFeedbackPool For(Transform source)
        {
            var scene=source!=null?source.gameObject.scene:SceneManager.GetActiveScene();
            if(scenes.TryGetValue(scene.handle,out var existing)&&existing!=null)return existing;
            var storage=new GameObject("Floating feedback · reusable");
            SceneManager.MoveGameObjectToScene(storage,scene);
            var created=storage.AddComponent<FloatingFeedbackPool>();
            scenes[scene.handle]=created;
            return created;
        }
        private void Awake()
        {
            sceneHandle=gameObject.scene.handle;
            pool=new SceneComponentPool<FloatingFeedback>(transform,CreateLabel,4,MaximumLabels,MaximumLabels);
        }
        private FloatingFeedback CreateLabel(Transform storage)
        {
            var go=new GameObject("Respuesta flotante");go.SetActive(false);go.transform.SetParent(storage,false);
            var label=go.AddComponent<TextMesh>();
            label.text=string.Empty;label.fontSize=32;label.characterSize=.045f;label.anchor=TextAnchor.MiddleCenter;
            go.GetComponent<MeshRenderer>().sortingOrder=20000;
            var feedback=go.AddComponent<FloatingFeedback>();feedback.Initialize(label);return feedback;
        }
        public FloatingFeedback Rent()
        {
            DrainDisabled();
            var label=pool.Rent();
            if(label!=null)label.Lease(this);
            return label;
        }
        internal void Return(FloatingFeedback label){if(pool!=null)pool.Return(label);}
        internal void Deactivated(FloatingFeedback label)=>disabled.Enqueue(label);
        internal void Forget(FloatingFeedback label){if(pool!=null)pool.Forget(label);}
        private void Update()=>DrainDisabled();
        private void DrainDisabled()
        {
            while(disabled.Count>0)
            {
                var label=disabled.Dequeue();
                if(label!=null)label.ReturnToPool();
            }
        }
        private void OnDestroy()
        {
            if(scenes.TryGetValue(sceneHandle,out var owner)&&owner==this)scenes.Remove(sceneHandle);
            disabled.Clear();pool?.Dispose();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetScenes()=>scenes.Clear();
    }
}
