using UnityEngine;
using System.Collections;
using FastCollections;
using RTSLockstep;
namespace RTSLockstep.Data
{
    public interface IAvatarData : INamedData
    {
        Texture2D GetAvatar();
    }
}