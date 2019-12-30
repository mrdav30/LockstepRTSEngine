using System.Collections.Generic;
using UnityEngine;
using RTSLockstep.LSResources;
using RTSLockstep.Utility;

namespace RTSLockstep
{
    public class WorkerAnimator : GenericUnitAnimator
    {
        [Header("Worker specific animations")]
        [Space(10f), SerializeField]
        private string idlingWood = "idlingWood";
        [SerializeField]
        private string idlingOre = "idlingOre";
        [SerializeField]
        private string movingWood = "movingWood";
        [SerializeField]
        private string movingOre = "movingOre";
        [SerializeField]
        private string engagingWood = "engagingWood";
        [SerializeField]
        private string engagingOre = "engagingOre";
        [SerializeField]
        private string constructing = "constructing";

        [Header("Sounds that accompany worker animations")]
        [Space(10f), SerializeField]
        private AudioClip emptyHarvestSound;
        [SerializeField]
        private AudioClip harvestSound;
        [SerializeField]
        private AudioClip startHarvestSound;

        [Space(10f), SerializeField]
        private float emptyHarvestVolume = 0.5f;
        [SerializeField]
        private float harvestVolume = 0.5f;
        [SerializeField]
        private float startHarvestVolume = 1.0f;

        private AnimationClip idlingWoodClip;
        private AnimationClip idlingOreClip;
        private AnimationClip movingWoodClip;
        private AnimationClip movingOreClip;
        private AnimationClip engagingWoodClip;
        private AnimationClip engagingOreClip;
        private AnimationClip constructingClip;

        public override void Initialize()
        {
            base.Initialize();
            if (CanAnimate = (animator != null))
            {
                AnimationClip[] agentAnimations = animator.runtimeAnimatorController.animationClips;

                foreach (AnimationClip clip in agentAnimations)
                {
                    //States
                    if (clip.name == idlingWood)
                    {
                        idlingWoodClip = clip;
                    }
                    else if (clip.name == idlingOre)
                    {
                        idlingOreClip = clip;
                    }
                    else if (clip.name == movingWood)
                    {
                        movingWoodClip = clip;
                    }
                    else if (clip.name == movingOre)
                    {
                        movingOreClip = clip;
                    }
                    else if (clip.name == engagingWood)
                    {
                        engagingWoodClip = clip;
                    }
                    else if (clip.name == engagingOre)
                    {
                        engagingOreClip = clip;
                    }
                    else if (clip.name == constructing)
                    {
                        constructingClip = clip;
                    }
                }
            }
        }

        public override void SetIdleState(AnimState state = AnimState.None)
        {
            state = cachedAgent.MyStats.CachedHarvest.IsNotNull() && cachedAgent.MyStats.CachedHarvest.GetCurrentLoad() > 0 ? cachedAgent.MyStats.CachedHarvest.IdlingAnimState
                : state != AnimState.None ? state :  AnimState.Idling;

            SetState(state);
        }

        public override void SetMovingState(AnimState state = AnimState.None)
        {
            state = cachedAgent.MyStats.CachedHarvest.IsNotNull() && cachedAgent.MyStats.CachedHarvest.GetCurrentLoad() > 0 ? cachedAgent.MyStats.CachedHarvest.MovingAnimState
                : state != AnimState.None ? state : AnimState.Moving;

            SetState(state);
        }

        protected override AnimationClip GetStateClip(AnimState state)
        {
            switch (state)
            {
                case AnimState.MovingWood:
                    return movingWoodClip;
                case AnimState.MovingOre:
                    return movingOreClip;
                case AnimState.IdlingWood:
                    return idlingWoodClip;
                case AnimState.IdlingOre:
                    return idlingOreClip;
                case AnimState.EngagingWood:
                    return engagingWoodClip;
                case AnimState.EngagingOre:
                    return engagingOreClip;
                case AnimState.Constructing:
                    return constructingClip;
            }
            return base.GetStateClip(state);
        }

        protected override string GetStateName(AnimState state)
        {
            switch (state)
            {
                case AnimState.MovingWood:
                    return movingWood;
                case AnimState.MovingOre:
                    return movingOre;
                case AnimState.IdlingWood:
                    return idlingWood;
                case AnimState.IdlingOre:
                    return idlingOre;
                case AnimState.EngagingWood:
                    return engagingWood;
                case AnimState.EngagingOre:
                    return engagingOre;
                case AnimState.Constructing:
                    return constructing;
            }
            return base.GetStateName(state);
        }

        protected override void InitialiseAudio()
        {
            base.InitialiseAudio();
            List<AudioClip> sounds = new List<AudioClip>();
            List<float> volumes = new List<float>();

            if (emptyHarvestVolume < 0.0f)
            {
                emptyHarvestVolume = 0.0f;
            }
            if (emptyHarvestVolume > 1.0f)
            {
                emptyHarvestVolume = 1.0f;
            }
            sounds.Add(emptyHarvestSound);
            volumes.Add(emptyHarvestVolume);

            if (harvestVolume < 0.0f)
            {
                harvestVolume = 0.0f;
            }
            if (harvestVolume > 1.0f)
            {
                harvestVolume = 1.0f;
            }
            sounds.Add(harvestSound);
            volumes.Add(harvestVolume);

            if (startHarvestVolume < 0.0f)
            {
                startHarvestVolume = 0.0f;
            }
            if (startHarvestVolume > 1.0f)
            {
                startHarvestVolume = 1.0f;
            }
            sounds.Add(startHarvestSound);
            volumes.Add(startHarvestVolume);

            audioElement.Add(sounds, volumes);
        }
    }
}