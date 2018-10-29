using UnityEngine;
using System.Collections; using FastCollections;
namespace RTSLockstep {
public class Stop : ActiveAbility {
		protected override void OnExecute (Command com)
		{
			Agent.StopCast ();
		}
	}
}