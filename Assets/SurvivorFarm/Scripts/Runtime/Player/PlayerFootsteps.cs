using SurvivorFarm.Runtime.Gameplay;
using UnityEngine;

namespace SurvivorFarm.Runtime.Player
{
    /// <summary>Distance-driven steps: running faster changes cadence, standing against a wall is silent.</summary>
    public sealed class PlayerFootsteps : MonoBehaviour
    {
        private Vector3 previous;private float travelled,nextStep;
        private PlayerMovementController movement;
        private void Start(){previous=transform.position;movement=GetComponent<PlayerMovementController>();}
        private void Update()
        {
            float distance=Vector2.Distance(previous,transform.position);previous=transform.position;
            if(Time.timeScale<=0||distance>1||movement!=null&&movement.IsDashing){travelled=0;return;}
            if(distance<.001f)return;travelled+=distance;
            if(travelled<.55f||Time.time<nextStep)return;
            bool running=distance/Mathf.Max(.001f,Time.deltaTime)>3.7f;
            travelled=0;nextStep=Time.time+.16f;
            AudioFeedback.PlayAt(running?CombatSound.RunStep:CombatSound.WalkStep,transform.position,running?.55f:.35f);
        }
    }
}
