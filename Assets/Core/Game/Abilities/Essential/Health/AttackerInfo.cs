using UnityEngine;
using System.Collections; using FastCollections;
namespace RTSLockstep
{
	public class AttackerInfo
	{
		public AttackerInfo (RTSAgent attacker, AgentController controller)
		{
			Attacker = attacker;
			Controller = controller;
		}
		public RTSAgent Attacker;
		public AgentController Controller;
	}
}