using UnityEngine;
using System.Collections; using FastCollections;

namespace RTSLockstep.Data
{
    public interface IProjectileData : INamedData
    {
        LSProjectile GetProjectile();
    }
}