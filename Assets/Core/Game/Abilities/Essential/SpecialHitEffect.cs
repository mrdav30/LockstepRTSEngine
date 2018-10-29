using UnityEngine;
using System.Collections; using FastCollections;
using RTSLockstep;
namespace RTSLockstep
{
	public class SpecialHitEffect : DurationAbility
	{
		protected Attack cachedAtack { get; private set;}

		protected override void OnSetup()
		{
            cachedAtack = Agent.GetAbility<Attack>();
		}
		protected override void OnExecute(Command com)
		{
			base.OnExecute(com);
		}

		protected override void OnStartWorking()
		{
            cachedAtack.ExtraOnHit += ApplyEffect;
		}
		protected virtual void ApplyEffect(LSAgent agent, bool isCurrent)
		{
			OnApplyEffect(agent, isCurrent);
		}
		protected virtual void OnApplyEffect (LSAgent agent, bool isCurrent)
		{
		}

		protected override void OnStopWorking()
		{
            cachedAtack.ExtraOnHit -= ApplyEffect;
		}
	}
}