using UnityEngine;
using System.Collections; using FastCollections;

namespace RTSLockstep {
	public enum MessageType : byte
	{
		Input,
		Frame,
		Init,
        Matchmaking,
        Register,
        Test,
    }
}