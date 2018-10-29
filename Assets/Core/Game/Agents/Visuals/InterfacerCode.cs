 using UnityEngine;
using System.Collections; using FastCollections;
namespace RTSLockstep {

	public enum InformationGatherType {
		None,
		Position,
		Target,
		PositionOrTarget,
        PositionOrAction
	}
}