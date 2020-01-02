using System;

using RTSLockstep.Integration;
using RTSLockstep.Simulation.LSMath;

namespace RTSLockstep.Managers.GameManagers
{
    [Serializable]
    public struct LSAgentSpawnInfo
    {
        [DataCode("Agents")]
        public string AgentCode;
        public int Count;
        [DataCode("AgentControllers")]
        public string ControllerCode;
        public Vector2d Position;
    }
}
