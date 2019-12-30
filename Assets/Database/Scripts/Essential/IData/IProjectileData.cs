using RTSLockstep.Projectiles;

namespace RTSLockstep.Data
{
    public interface IProjectileData : INamedData
    {
        LSProjectile GetProjectile();
    }
}