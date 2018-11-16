using RTSLockstep;
using UnityEngine;

public class TempStructure : MonoBehaviour, IBuildable
{
    /// <summary>
    /// Describes the width and height of the buildable. This value does not change on the buildable.
    /// </summary>
    /// <value>The size of the build.</value>
    public int BuildSize { get; set; }

    public Coordinate GridPosition { get; set; }
    /// <summary>
    /// Function that relays to the buildable whether or not it's on a valid building spot.
    /// </summary>
    public bool IsValidOnGrid { get; set; }
    public bool IsMoving { get; set; }
}
