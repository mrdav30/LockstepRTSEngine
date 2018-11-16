using RTSLockstep;
using System.Collections.Generic;
using UnityEngine;

public static class GridBuilder
{
    public static IBuildable Target { get; private set; }
    public static bool IsMovingBuilding { get; private set; }

    private static bool TargetOriginalValid { get; set; }
    private static Coordinate TargetOriginalPosition { get; set; }
    public static Coordinate GetTargetOriginalPosition { get { return TargetOriginalPosition; } }

    #region Setup
    private static void SetTarget(IBuildable buildable)
    {
        Target = buildable;
    }

    public static void Initialize()
    {
        BuildGridAPI.Initialize();
        Reset();
    }

    public static void Reset()
    {
        ClearTarget();
        IsMovingBuilding = false;
    }

    public static void ClearTarget()
    {
        Target = null;
    }
    #endregion

    #region Placing
    public static bool Place(IBuildable buildable, Vector2d newPos)
    {
        SetTarget(buildable);
        StartPlace();
        UpdatePlace(newPos);
        return EndPlace();
    }

    private static void StartPlace()
    {
        Target.IsMoving = true;
    }

    private static bool UpdatePlace(Vector2d newPos)
    {
        Coordinate coor = BuildGridAPI.ToGridPos(newPos);
        Target.GridPosition = coor;
        bool canPlace = BuildGridAPI.CanBuild(coor, Target);
        Target.IsValidOnGrid = (canPlace);
        return canPlace;
    }

    private static bool EndPlace()
    {
        bool canBuild = (BuildGridAPI.Construct(Target));
        Target.IsValidOnGrid = (canBuild);
        Target.IsMoving = false;
        return canBuild;
    }
    #endregion

    #region Moving
    public static void StartMove(IBuildable buildable)
    {
        SetTarget(buildable);
        StartPlace();
        TargetOriginalValid = Target.IsValidOnGrid;
        TargetOriginalPosition = Target.GridPosition;
        if (TargetOriginalValid)
        {
            BuildGridAPI.Unbuild(Target);
        }
        IsMovingBuilding = true;
    }

    /// <summary>
    /// Moves the target buildable and returns whether or not its new position is a valid place to build.
    /// </summary>
    /// <param name="newPos">New position.</param>
    public static bool UpdateMove(Vector2d newPos)
    {
        if (IsMovingBuilding == false) throw new System.Exception("Building move must be started before being updated");
        return UpdatePlace(newPos);
    }

    /// <summary>
    /// Returns whether or not the buildable could be built on where its movement ends.
    /// If not, the buildable is returned to its previous position.
    /// </summary>
    /// <returns><c>true</c>, if move was ended, <c>false</c> otherwise.</returns>
    public static PlacementResult EndMove()
    {
        IsMovingBuilding = false;
        if (EndPlace())
        {
            return PlacementResult.Placed;
        }

        if (TargetOriginalValid)
        {
            Target.GridPosition = TargetOriginalPosition;
            BuildGridAPI.Construct(Target);
            Target.IsValidOnGrid = true;
            return PlacementResult.Returned;
        }
        else
        {
            return PlacementResult.Limbo;
        }
        //return PlacementResult.Returned;
    }
    #endregion

    #region Unbuilding
    public static void Unbuild(IBuildable buildable)
    {
        BuildGridAPI.Unbuild(buildable);
    }
    #endregion


    public static IEnumerable<IBuildable> GetOccupyingBuildings(IBuildable buildable)
    {
        foreach (var obj in BuildGridAPI.MainBuildGrid.GetOccupyingBuildables(buildable))
        {
            yield return obj;
        }
    }
}

public enum PlacementResult
{
    Placed,
    Returned,
    Limbo
}
