using UnityEngine;
using RTSLockstep.LSResources;

namespace RTSLockstep.Agents
{
    public class LSParticler : MonoBehaviour
    {
        LSAnimatorBase animator;

        void Awake()
        {
            animator = GetComponent<LSAnimatorBase>();
            animator.OnStatePlay += HandleOnStatePlay;
            animator.OnImpulsePlay += HandleOnImpulsePlay;
        }

        void HandleOnImpulsePlay(AnimImpulse obj, int rate)
        {
        }

        void HandleOnStatePlay(AnimState obj)
        {

        }
    }
}