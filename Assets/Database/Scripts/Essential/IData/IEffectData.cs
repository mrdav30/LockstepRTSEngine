using UnityEngine;
using System.Collections; using FastCollections;

namespace RTSLockstep.Data
{
    public interface IEffectData : INamedData
    {
        LSEffect GetEffect ();
    }
}