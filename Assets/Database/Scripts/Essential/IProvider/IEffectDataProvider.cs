using UnityEngine;
using System.Collections; using FastCollections;

namespace RTSLockstep.Data
{
    public interface IEffectDataProvider
    {
        IEffectData[] EffectData {get;}
    }
}