using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using RotaryHeart.Lib.SerializableDictionary;

namespace RTSLockstep
{

    [Serializable]
    public class UpgradeLevel : SerializableDictionaryBase<int, string> { };
    [Serializable]
    public class UpgradeAnimation : SerializableDictionaryBase<int, AnimationClip> { };

    public class StructureAnimator : LSAnimatorBase
    {
        [SerializeField]
        private UpgradeLevel idling = new UpgradeLevel {
            {1, "idling"},
            {2, "idling_upgrade_1" },
            {3, "idling_upgrade_2"},
            {4, "idling_upgrade_3"}
        };
        [SerializeField]
        private UpgradeLevel building = new UpgradeLevel {
            { 1, "building"},
            { 2, "building_upgrade_1"},
            { 3, "building_upgrade_2"},
            { 4, "building_upgrade_3"}
        };
        [SerializeField]
        private UpgradeLevel working = new UpgradeLevel {
            { 1, "working"},
            { 2, "working_upgrade_1"},
            { 3, "working_upgrade_2"},
            { 4, "working_upgrade_3"}
        };
        [SerializeField]
        private UpgradeLevel dying = new UpgradeLevel {
            { 1, "dying"},
            { 2, "dying_upgrade_1"},
            { 3, "dying_upgrade_2"},
            { 4, "dying_upgrade_3"}
        };
        [SerializeField]
        private string engaging = "engaging";

        //sounds that accompany animations
        [Space(10f), SerializeField]
        private AudioClip attackSound;
        [SerializeField]
        private AudioClip selectSound;
        [SerializeField]
        private AudioClip useWeaponSound;
        [SerializeField]
        private float attackVolume = 1.0f, selectVolume = 1.0f, useWeaponVolume = 1.0f;
        [SerializeField]
        private AudioClip finishedJobSound;
        [SerializeField]
        private float finishedJobVolume = 1.0f;

        private UpgradeAnimation idlingClip = new UpgradeAnimation{
            {1,null},
            {2,null},
            {3,null},
            {4,null}
        };
        private UpgradeAnimation buildingClip = new UpgradeAnimation{
            {1,null},
            {2,null},
            {3,null},
            {4,null}
        };
        private UpgradeAnimation workingClip = new UpgradeAnimation{
            {1,null},
            {2,null},
            {3,null},
            {4,null}
        };
        private AnimationClip engagingClip;
        private UpgradeAnimation dyingClip = new UpgradeAnimation{
            {1,null},
            {2,null},
            {3,null},
            {4,null}
        };

        private Animator animator;
        protected const float fadeLength = 0.0f; //0.5f;

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

                int upgradeLevel = cachedAgent.GetAbility<Structure>().GetUpgradeLevel();

                foreach (AnimationClip clip in agentAnimations)
                {
                    //States
                    if (clip.name == idling[upgradeLevel])
                    {
                        idlingClip[upgradeLevel] = clip;
                    }
                    else if (clip.name == building[upgradeLevel])
                    {
                        buildingClip[upgradeLevel] = clip;
                    }
                    else if (clip.name == working[upgradeLevel])
                    {
                        workingClip[upgradeLevel] = clip;
                    }
                    else if (clip.name == engaging)
                    {
                        engagingClip = clip;
                    }
                    else if (clip.name == dying[upgradeLevel])
                    {
                        dyingClip[upgradeLevel] = clip;
                    }
                }
            }
        }

        public override void Play(AnimState state, bool baseAnimate = true)
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

        protected virtual AnimationClip GetStateClip(AnimState state)
        {
            int upgradeLevel = cachedAgent.GetAbility<Structure>().GetUpgradeLevel();
            switch (state)
            {
                case AnimState.Idling:
                    return idlingClip[upgradeLevel];
                case AnimState.Building:
                    return buildingClip[upgradeLevel];
                case AnimState.Working:
                    return workingClip[upgradeLevel];
                case AnimState.Engaging:
                    return engagingClip;
                case AnimState.Dying:
                    return dyingClip[upgradeLevel];
            }
            return idlingClip[upgradeLevel];
        }

        public virtual string GetStateName(AnimState state)
        {
            int upgradeLevel = cachedAgent.GetAbility<Structure>().GetUpgradeLevel();
            switch (state)
            {
                case AnimState.Idling:
                    return idling[upgradeLevel];
                case AnimState.Building:
                    return building[upgradeLevel];
                case AnimState.Working:
                    return working[upgradeLevel];
                case AnimState.Engaging:
                    return engaging;
                case AnimState.Dying:
                    return dying[upgradeLevel];
            }
            return idling[upgradeLevel];
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