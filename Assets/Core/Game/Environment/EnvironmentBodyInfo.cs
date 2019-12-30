using RTSLockstep.Simulation.LSPhysics;
using RTSLockstep.Simulation.LSMath;

namespace RTSLockstep.Environment
{
    [System.Serializable]
    public class EnvironmentBodyInfo
    {
        public EnvironmentBodyInfo(
            UnityLSBody body,
            Vector3d position,
            Vector2d rotation
        )
        {
            Body = body;
            Position = position;
            Rotation = rotation;
        }

        public UnityLSBody Body;
        public Vector3d Position;
        public Vector2d Rotation;
    }
}