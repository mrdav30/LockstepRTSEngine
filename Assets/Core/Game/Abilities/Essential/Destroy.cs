using UnityEngine;
using RTSLockstep.Player.Commands;

namespace RTSLockstep.Abilities.Essential
{
    public class Destroy : ActiveAbility
    {
        protected override void OnExecute(Command com)
        {
            Agent.Die();
        }
    }
}