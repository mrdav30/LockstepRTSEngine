using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    public class GenericUnitAnimator: LSAnimatorBase
    {
        [Space(10f), SerializeField]
        protected string idling = "idling";
        [SerializeField]
        protected string moving = "moving";
        [SerializeField]
        protected string engaging = "engaging";
        [SerializeField]
        protected string specialEngaging = "specialEngaging";
        [SerializeField]
        protected string dying = "dying";
        [Space(10f), SerializeField]
        protected string fire = "fire";
        [SerializeField]
        protected string specialFire = "specialFire";
        [SerializeField]
        protected string specialAttack = "specialAttack";
        [SerializeField]
        protected string extra = "extra";

        [Header("Sounds that accompany animations")]
        [Space(10f), SerializeField]
        protected AudioClip attackSound;
        [SerializeField]
        protected AudioClip selectSound;
        [SerializeField]
        protected AudioClip useWeaponSound;
        [SerializeField]
        protected AudioClip finishedJobSound;
        [SerializeField]
        protected AudioClip driveSound, moveSound;

        [Space(10f), SerializeField]
        protected float attackVolume = 1.0f;
        [SerializeField]
        protected float selectVolume = 1.0f;
        [SerializeField]
        protected float useWeaponVolume = 1.0f;
        [SerializeField]
        protected float finishedJobVolume = 1.0f;
        [SerializeField]
        protected float driveVolume = 0.5f;
        [SerializeField]
        protected float moveVolume = 1.0f;

        protected AnimationClip idlingClip;
        protected AnimationClip movingClip;
        protected AnimationClip engagingClip;
        protected AnimationClip dyingClip;
        protected AnimationClip specialEngagingClip;

        protected AnimationClip fireClip;
        protected AnimationClip specialFireClip;
        protected AnimationClip specialAttackClip;
        protected AnimationClip extraClip;

        public override void Initialize()
        {
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

        protected override string GetImpulseName(AnimImpulse impulse)
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

        protected override AnimationClip GetImpulseClip(AnimImpulse impulse)
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

        protected override string GetStateName(AnimState state)
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

        protected override AnimationClip GetStateClip(AnimState state)
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

            if (finishedJobVolume < 0.0f)
            {
                finishedJobVolume = 0.0f;
            }
            if (finishedJobVolume > 1.0f)
            {
                finishedJobVolume = 1.0f;
            }
            sounds.Add(finishedJobSound);
            volumes.Add(finishedJobVolume);

            audioElement.Add(sounds, volumes);
        }
    }
}
