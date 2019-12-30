using RotaryHeart.Lib.SerializableDictionary;
using RTSLockstep.Abilities.Essential;
using RTSLockstep.LSResources;
using System;
using UnityEngine;

namespace RTSLockstep
{
    [Serializable]
    public class UpgradeLevel : SerializableDictionaryBase<int, string> { };
    [Serializable]
    public class UpgradeAnimation : SerializableDictionaryBase<int, AnimationClip> { };

    public class GenericStructureAnimator : LSAnimatorBase
    {
        [Space(10f), SerializeField]
        protected UpgradeLevel idling = new UpgradeLevel {
            {1, "idling"},
            {2, "idling_upgrade_1" },
            {3, "idling_upgrade_2"},
            {4, "idling_upgrade_3"}
        };
        [SerializeField]
        protected UpgradeLevel building = new UpgradeLevel {
            { 1, "building"},
            { 2, "building_upgrade_1"},
            { 3, "building_upgrade_2"},
            { 4, "building_upgrade_3"}
        };
        [SerializeField]
        protected UpgradeLevel working = new UpgradeLevel {
            { 1, "working"},
            { 2, "working_upgrade_1"},
            { 3, "working_upgrade_2"},
            { 4, "working_upgrade_3"}
        };
        [SerializeField]
        protected string engaging = "engaging";
        [SerializeField]
        protected UpgradeLevel dying = new UpgradeLevel {
            { 1, "dying"},
            { 2, "dying_upgrade_1"},
            { 3, "dying_upgrade_2"},
            { 4, "dying_upgrade_3"}
        };

        protected UpgradeAnimation idlingClip = new UpgradeAnimation{
            {1,null},
            {2,null},
            {3,null},
            {4,null}
        };
        protected UpgradeAnimation buildingClip = new UpgradeAnimation{
            {1,null},
            {2,null},
            {3,null},
            {4,null}
        };
        protected UpgradeAnimation workingClip = new UpgradeAnimation{
            {1,null},
            {2,null},
            {3,null},
            {4,null}
        };
        protected AnimationClip engagingClip;
        protected UpgradeAnimation dyingClip = new UpgradeAnimation{
            {1,null},
            {2,null},
            {3,null},
            {4,null}
        };

        [Header("Sounds that accompany animations")]
        [Space(10f), SerializeField]
        protected AudioClip finishedJobSound;

        [Space(10f), SerializeField]
        protected float finishedJobVolume = 1.0f;

        public override void Initialize()
        {
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

            Play(AnimState.Idling);
        }

        public override void SetDyingState(AnimState state = AnimState.Dying)
        {
            // Don't play dying animation if construction hasn't started.
            if (cachedAgent.GetAbility<Structure>() && cachedAgent.GetAbility<Structure>().ConstructionStarted)
            {
                SetState(state);
            }
        }

        protected override string GetStateName(AnimState state)
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

        protected override AnimationClip GetStateClip(AnimState state)
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
    }
}