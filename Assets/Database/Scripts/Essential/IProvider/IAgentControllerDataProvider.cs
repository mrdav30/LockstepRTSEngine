using UnityEngine;
using System.Collections; using FastCollections;

namespace RTSLockstep.Data
{
	public interface IAgentControllerDataProvider
	{
		AgentControllerDataItem[] AgentControllerData {get;}
	}
}