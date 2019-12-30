using UnityEngine;

namespace RTSLockstep.Player.Utility
{
    public interface IMousable
    {
        Vector3 WorldPosition { get; }
        float MousableRadius { get; }
    }
}