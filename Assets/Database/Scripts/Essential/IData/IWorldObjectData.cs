using UnityEngine;
using System.Collections; using FastCollections;
using RTSLockstep;
namespace RTSLockstep.Data
{
    public interface IWorldObjectData : INamedData
    {
        GameObject GetWorldObject ();
    }
}