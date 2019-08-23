using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    public class LSAnimatorBase : MonoBehaviour
    {
        public event Action<AnimState> OnStatePlay;
        public event Action<AnimImpulse, int> OnImpulsePlay;

        public bool CanAnimate { get; protected set; }

        protected Animator animator;
        protected const float fadeLength = .5f;

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

                currentAnimState = value;
            }
        }

        private AnimState _prevIdleState;

        public virtual void Setup()
        {
            cachedAgent = transform.GetComponentInParent<RTSAgent>();
            InitialiseAudio();

            animStateChanged = false;
            lastAnimState = currentAnimState = AnimState.Idling;

            // update to you more modern Animator component
            // Animation will become depreciated soon
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = this.GetComponentInChildren<Animator>();
            }
        }

        public virtual void Initialize()
        {

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

        public virtual void ApplyImpulse(AnimImpulse animImpulse, int rate = 0)
        {
            Play(animImpulse, rate);
        }

        public virtual void Play(AnimState state, bool baseAnimate = true)
        {
            if (OnStatePlay.IsNotNull())
            {
                OnStatePlay(state);
            }

            if (CanAnimate && baseAnimate)
            {
                AnimationClip clip = GetStateClip(state);
                if (clip.IsNotNull())
                {
                    animator.CrossFade(clip.name, fadeLength);
                }
            }
        }

        public virtual void Play(AnimImpulse impulse, int rate = 0)
        {
            if (OnImpulsePlay.IsNotNull())
            {
                OnImpulsePlay(impulse, rate);
            }

            if (CanAnimate)
            {
                AnimationClip clip = GetImpulseClip(impulse);
                if (clip.IsNotNull())
                {
                    //animator.Blend(clip.name,.8f,fadeLength);
                    animator.Play(clip.name);
                }
            }
        }

        public virtual void SetIdleState(AnimState state = AnimState.None)
        {
            if (state != AnimState.None)
            {
                _prevIdleState = state;
            }

            state = state != AnimState.None ? state : _prevIdleState.IsNotNull() ? _prevIdleState : AnimState.Idling;

            SetState(state);
        }

        public virtual void SetDyingState(AnimState state = AnimState.None)
        {
            state = state != AnimState.None ? state : AnimState.Dying;
            SetState(state);
        }

        public virtual void SetMovingState(AnimState state = AnimState.None)
        {
            state = state != AnimState.None ? state : AnimState.Moving;
            SetState(state);
        }


        public void SetState(AnimState animState)
        {
            CurrentAnimState = animState;
        }

        protected virtual string GetImpulseName(AnimImpulse impulse)
        {
            return null;
        }

        protected virtual AnimationClip GetImpulseClip(AnimImpulse impulse)
        {
            return null;
        }

        protected virtual string GetStateName(AnimState state)
        {
            return null;
        }

        protected virtual AnimationClip GetStateClip(AnimState state)
        {
            return null;
        }

        protected virtual void InitialiseAudio()
        {
            audioElement = new AudioElement(null, null, cachedAgent.objectName + cachedAgent.GlobalID, this.transform);
        }
    }
}