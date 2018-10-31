using System.Collections; using FastCollections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
	[System.Serializable]
	public class EnvironmentAgentInfo
	{
		public EnvironmentAgentInfo (string agentCode, RTSAgent agent, Vector3d pos, Vector2d rot)
		{
			AgentCode = agentCode;
			Agent = agent;
			Position = pos;
			Rotation = rot;
		}
		public string AgentCode;
		public RTSAgent Agent;
		public Vector3d Position;
		public Vector2d Rotation;
	}
}