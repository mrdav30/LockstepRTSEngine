using UnityEngine;
using System.Collections; using FastCollections;

namespace RTSLockstep.Data
{
    public interface IProjectileDataProvider
    {
        IProjectileData[] ProjectileData {get;}
    }
}