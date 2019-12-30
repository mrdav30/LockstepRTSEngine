using RTSLockstep.Agents;
using RTSLockstep.Player.Commands;

namespace RTSLockstep.Abilities.Essential
{
    public class SpecialHitEffect : DurationAbility
    {
        protected Attack CachedAtack { get; private set; }

        protected override void OnSetup()
        {
            CachedAtack = Agent.GetAbility<Attack>();
        }
        protected override void OnExecute(Command com)
        {
            base.OnExecute(com);
        }

        protected override void OnStartWorking()
        {
            CachedAtack.ExtraOnHit += ApplyEffect;
        }
        protected virtual void ApplyEffect(LSAgent agent, bool isCurrent)
        {
            OnApplyEffect(agent, isCurrent);
        }
        protected virtual void OnApplyEffect(LSAgent agent, bool isCurrent)
        {
        }

        protected override void OnStopWorking()
        {
            CachedAtack.ExtraOnHit -= ApplyEffect;
        }
    }
}