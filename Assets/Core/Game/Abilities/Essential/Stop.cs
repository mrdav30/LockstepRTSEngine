using RTSLockstep.Player.Commands;

namespace RTSLockstep.Abilities.Essential
{
    public class Stop : ActiveAbility
    {
        protected override void OnExecute(Command com)
        {
            Agent.StopCast();
        }
    }
}