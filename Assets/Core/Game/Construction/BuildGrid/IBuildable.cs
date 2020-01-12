using RTSLockstep.Simulation.LSMath;

namespace RTSLockstep.BuildSystem.BuildGrid
{
    public interface IBuildable
    {
        Coordinate GridPosition { get; set; }
        /// <summary>
        /// Describes the width and height of the buildable. This value does not change on the buildable.
        /// </summary>
        /// <value>The size of the build.</value>
        int BuildSizeLow { get; }
        int BuildSizeHigh { get; }
        /// <summary>
        /// Function that relays to the buildable whether or not it's on a valid building spot.
        /// </summary>
        bool IsValidOnGrid { get; set; }
        bool IsMoving { get; set; }
        /// <summary>
        /// Set to true to allow buildable to build on top of other structures.
        /// </summary>
        bool CanOverlay { get; set; }
    }
}