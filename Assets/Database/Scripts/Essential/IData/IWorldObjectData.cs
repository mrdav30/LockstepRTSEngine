using UnityEngine;

namespace RTSLockstep.Data
{
    public interface IWorldObjectData : INamedData
    {
        GameObject GetWorldObject();
    }
}