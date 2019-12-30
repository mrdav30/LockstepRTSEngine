using UnityEngine;

namespace RTSLockstep.Data
{
    public interface IAvatarData : INamedData
    {
        Texture2D GetAvatar();
    }
}