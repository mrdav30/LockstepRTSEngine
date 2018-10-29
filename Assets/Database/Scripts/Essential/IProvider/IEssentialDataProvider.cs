using UnityEngine;
using System.Collections; using FastCollections;

namespace RTSLockstep.Data
{
    public interface IEssentialDataProvider :
    IAgentDataProvider
    ,IAbilityDataProvider
    ,IEffectDataProvider
    ,IInputDataProvider
    ,IProjectileDataProvider
    {

    }
}