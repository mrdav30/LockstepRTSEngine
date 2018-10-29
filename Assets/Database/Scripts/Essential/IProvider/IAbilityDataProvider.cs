using UnityEngine;
using System.Collections; using FastCollections;

namespace RTSLockstep.Data
{
    public interface IAbilityDataProvider
    {
        AbilityDataItem[] AbilityData { get; }
    }
}