using UnityEngine;
using System.Collections; using FastCollections;

namespace RTSLockstep.Data
{
    public interface IAvatarDataProvider
    {
        AvatarDataItem[] AvatarData { get; }
    }
}