using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace RTSLockstep
{
    public class LSAnimator : LSAnimatorBase
    {
        [SerializeField]
        protected string idling = "idling";
        [SerializeField]
        protected string moving = "moving";
        [SerializeField]
        protected string engaging = "engaging";
        [SerializeField]
        private string specialEngaging = "specialEngaging";
        [SerializeField]
        protected string dying = "dying";
        [Space(10f), SerializeField]
        private string fire = "fire";
        [SerializeField]
        private string specialFire = "specialFire";
        [SerializeField]
        private string specialAttack = "specialAttack";
        [SerializeField]
        private string extra = "extra";
        //sounds that accompany animations
        [SerializeField]
        private AudioClip attackSound, selectSound, useWeaponSound;
        [SerializeField]
        private float attackVolume = 1.0f, selectVolume = 1.0f, useWeaponVolume = 1.0f;
        [SerializeField]
        private AudioClip driveSound, moveSound;
        [SerializeField]
        private float driveVolume = 0.5f, moveVolume = 1.0f;

        private AnimationClip idlingClip;
        private AnimationClip movingClip;
        private AnimationClip engagingClip;
        private AnimationClip dyingClip;
        private AnimationClip specialEngagingClip;

        private AnimationClip fireClip;
        private AnimationClip specialFireClip;
        private AnimationClip specialAttackClip;
        private AnimationClip extraClip;

        protected Animator animator;

        public override void Setup()
        {
            base.Setup();
        }

        public override void Initialize()
        {
            base.Initialize();
            // update to you more modern Animator component
            // Animation will become depreciated soon
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = this.GetComponentInChildren<Animator>();
            }
            if (CanAnimate = (animator != null))
            {
                AnimationClip[] agentAnimations = animator.runtimeAnimatorController.animationClips;

                foreach (AnimationClip clip in agentAnimations)
                {
                    //States
                    if (clip.name == idling)
                    {
                        idlingClip = clip;
                    }
                    else if (clip.name == moving)
                    {
                        movingClip = clip;
                    }
                    else if (clip.name == engaging)
                    {
                        engagingClip = clip;
                    }
                    else if (clip.name == dying)
                    {
                        dyingClip = clip;
                    }
                    else if (clip.name == this.specialEngaging)
                    {
                        specialEngagingClip = clip;
                    }
                    //Impulses
                    else if (clip.name == fire)
                    {
                        fireClip = clip;
                    }
                    else if (clip.name == specialFire)
                    {
                        specialFireClip = clip;
                    }
                    else if (clip.name == specialAttack)
                    {
                        specialAttackClip = clip;
                    }
                    else if (clip.name == extra)
                    {
                        extraClip = clip;
                    }
                }
            }
            Play(AnimState.Idling);
        }

        public override void Play(AnimState state)
        {
            base.Play(state);
            if (CanAnimate)
            {
                AnimationClip clip = GetStateClip(state);
                if (clip.IsNotNull())
                {
                    animator.CrossFade(clip.name, fadeLength);
                }
            }
        }

        protected const float fadeLength = .5f;

        public override void Play(AnimImpulse impulse, int rate = 0)
        {
            base.Play(impulse, rate);

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

        protected virtual AnimationClip GetStateClip(AnimState state)
        {
            switch (state)
            {
                case AnimState.Moving:
                    return movingClip;
                case AnimState.Idling:
                    return idlingClip;
                case AnimState.Engaging:
                    return engagingClip;
                case AnimState.Dying:
                    return dyingClip;
                case AnimState.SpecialEngaging:
                    return this.specialEngagingClip;
            }
            return idlingClip;
        }

        public virtual string GetStateName(AnimState state)
        {
            switch (state)
            {
                case AnimState.Moving:
                    return moving;
                case AnimState.Idling:
                    return idling;
                case AnimState.Engaging:
                    return engaging;
                case AnimState.Dying:
                    return dying;
                case AnimState.SpecialEngaging:
                    return this.specialEngaging;
            }
            return idling;
        }

        public string GetImpulseName(AnimImpulse impulse)
        {
            switch (impulse)
            {
                case AnimImpulse.Fire:
                    return fire;
                case AnimImpulse.SpecialFire:
                    return specialFire;
                case AnimImpulse.SpecialAttack:
                    return specialAttack;
                case AnimImpulse.Extra:
                    return extra;
            }
            return idling;
        }

        private AnimationClip GetImpulseClip(AnimImpulse impulse)
        {
            switch (impulse)
            {
                case AnimImpulse.Fire:
                    return fireClip;
                case AnimImpulse.SpecialFire:
                    return specialFireClip;
                case AnimImpulse.SpecialAttack:
                    return specialAttackClip;
                case AnimImpulse.Extra:
                    return extraClip;
            }
            return idlingClip;
        }

        protected override void InitialiseAudio()
        {
            base.InitialiseAudio();
            List<AudioClip> sounds = new List<AudioClip>();
            List<float> volumes = new List<float>();
            if (attackVolume < 0.0f)
            {
                attackVolume = 0.0f;
            }
            if (attackVolume > 1.0f)
            {
                attackVolume = 1.0f;
            }
            sounds.Add(attackSound);
            volumes.Add(attackVolume);
            if (selectVolume < 0.0f)
            {
                selectVolume = 0.0f;
            }
            if (selectVolume > 1.0f)
            {
                selectVolume = 1.0f;
            }
            sounds.Add(selectSound);
            volumes.Add(selectVolume);
            if (useWeaponVolume < 0.0f)
            {
                useWeaponVolume = 0.0f;
            }
            if (useWeaponVolume > 1.0f)
            {
                useWeaponVolume = 1.0f;
            }
            sounds.Add(useWeaponSound);
            volumes.Add(useWeaponVolume);
            if (driveVolume < 0.0f)
            {
                driveVolume = 0.0f;
            }
            if (driveVolume > 1.0f)
            {
                driveVolume = 1.0f;
            }
            volumes.Add(driveVolume);
            sounds.Add(driveSound);
            if (moveVolume < 0.0f)
            {
                moveVolume = 0.0f;
            }
            if (moveVolume > 1.0f)
            {
                moveVolume = 1.0f;
            }
            sounds.Add(moveSound);
            volumes.Add(moveVolume);
            audioElement.Add(sounds, volumes);
        }
    }
}