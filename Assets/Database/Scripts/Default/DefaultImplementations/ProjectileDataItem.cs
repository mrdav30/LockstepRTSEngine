using RTSLockstep.Projectiles;
using System;

namespace RTSLockstep.Data
{
    [Serializable]
    public class ProjectileDataItem : ObjectDataItem, IProjectileData
    {
        public LSProjectile GetProjectile()
        {
            return Prefab.GetComponent<LSProjectile>();
        }
    }
}