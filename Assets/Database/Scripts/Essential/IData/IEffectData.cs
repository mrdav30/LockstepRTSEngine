using RTSLockstep.Effects;

namespace RTSLockstep.Data
{
    public interface IEffectData : INamedData
    {
        LSEffect GetEffect();
    }
}