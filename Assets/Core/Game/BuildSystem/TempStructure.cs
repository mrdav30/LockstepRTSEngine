using RTSLockstep;
using UnityEngine;

// Component attached to temporary object created for building placement
public class TempStructure : MonoBehaviour, IBuildable
{
    public GameObject EmptyGO;
    public GameObject EmptyPillarGO;
    public GameObject EmptyWallSegmentGO;

    /// <summary>
    /// Describes the width and height of the buildable. This value does not change on the buildable.
    /// </summary>
    /// <value>The size of the build.</value>
    public int BuildSizeLow { get; set; }
    public int BuildSizeHigh { get; set; }

    public Coordinate GridPosition { get; set; }
    /// <summary>
    /// Function that relays to the buildable whether or not it's on a valid building spot.
    /// </summary>
    public bool IsValidOnGrid { get; set; }
    public bool IsMoving { get; set; }
    public bool IsOverlay { get; set; }
}
