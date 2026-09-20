using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    /// <summary>Only hold/release state. No damage, input polling, animation or UI.</summary>
    [DisallowMultipleComponent]
    public sealed class SwordChargeController : MonoBehaviour
    {
        public const float Anticipation=.2f, FullChargeTime=1.05f;
        private float pressedAt;
        public bool IsPressed {get;private set;}
        public bool IsCharging=>IsPressed&&Time.time-pressedAt>=Anticipation;
        public float Progress=>ProgressAt(Time.time);
        public float ProgressAt(float now)=>IsPressed?Mathf.Clamp01((now-pressedAt-Anticipation)/(FullChargeTime-Anticipation)):0;
        public void Press(float now){pressedAt=now;IsPressed=true;}
        public float Release(float now){float charge=ProgressAt(now);Cancel();return charge;}
        public void Cancel(){IsPressed=false;pressedAt=0;}
        private void OnDisable()=>Cancel();
    }
}
