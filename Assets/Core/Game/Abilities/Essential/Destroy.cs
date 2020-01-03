using UnityEngine;
using RTSLockstep.Player.Commands;

namespace RTSLockstep.Abilities.Essential
{
    public class Destroy : ActiveAbility
    {
        public Texture2D DestroyIcon;

        protected override void OnExecute(Command com)
        {
            Agent.Die();
        }
    }
}