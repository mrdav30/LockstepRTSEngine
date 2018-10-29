using UnityEngine;
using System;

namespace RTSLockstep
{
    public class LSAnimatorBase : MonoBehaviour
    {
        public event Action<AnimState> OnStatePlay;
        public event Action<AnimImpulse, int> OnImpulsePlay;

        public bool CanAnimate { get; protected set; }
        private AnimImpulse currentImpulse;
        private bool isImpulsing = false;
        private bool animStateChanged;
        private AnimState lastAnimState;
        protected AudioElement audioElement;
        protected RTSAgent cachedAgent;

        [SerializeField]
        private AnimState currentAnimState;

        public AnimState CurrentAnimState
        {
            get { return currentAnimState; }
            set
            {
                if (value != lastAnimState)
                {
                    animStateChanged = true;
                }
                else
                {

                }
                currentAnimState = value;
            }
        }

        public virtual void Setup()
        {
            cachedAgent = transform.GetComponentInParent<RTSAgent>();
            InitialiseAudio();
        }

        public virtual void Initialize()
        {
            animStateChanged = false;
            lastAnimState = currentAnimState = AnimState.Idling;
        }

        public virtual void ApplyImpulse(AnimImpulse animImpulse, int rate = 0)
        {
            Play(animImpulse, rate);
        }

        public virtual void Play(AnimState state)
        {
            if (OnStatePlay.IsNotNull())
                OnStatePlay(state);
        }

        public virtual void Play(AnimImpulse impulse, int rate = 0)
        {
            if (OnImpulsePlay.IsNotNull())
                OnImpulsePlay(impulse, rate);
        }

        public virtual void Visualize()
        {
            if (isImpulsing == false)
            {
                if (animStateChanged)
                {
                    Play(currentAnimState);

                    animStateChanged = false;
                    lastAnimState = currentAnimState;
                }
            }
        }

        protected virtual void InitialiseAudio()
        {
            audioElement = new AudioElement(null, null, cachedAgent.objectName + cachedAgent.GlobalID, this.transform);
        }
    }
}