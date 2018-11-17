using RTSLockstep;
namespace RTSLockstep.Utility { }
namespace RTSLockstep.Abilities { }
namespace RTSLockstep.Agents { }

public static class BuildGridAPI
{
    const int defaultGridLength = 500;
    const int defaultBuildSpacing = 1;
    const int defaultGridOffset = -100;
    public static BuildGridManager MainBuildGrid { get; private set; }
    public static int GridOffset { get; private set; }

    public static void Initialize(
        int gridLength = defaultGridLength,
        int buildSpacing = defaultBuildSpacing,
        int gridOffset = defaultGridOffset)
    {
        MainBuildGrid = new BuildGridManager(gridLength, buildSpacing);
        GridOffset = gridOffset;
    }

    internal static bool Construct(IBuildable building)
    {
        return MainBuildGrid.Construct(building);
    }
    internal static void Unbuild(IBuildable building)
    {
        MainBuildGrid.Unbuild(building);
    }
    internal static bool CanBuild(Coordinate buildPos, IBuildable building)
    {
        return MainBuildGrid.CanBuild(buildPos, building.BuildSizeLow, building.BuildSizeHigh);
    }

    public static Coordinate ToGridPos(Vector2d worldPos)
    {
        return new Coordinate(worldPos.x.RoundToInt() - GridOffset, worldPos.y.RoundToInt() - GridOffset);
    }

    public static Vector2d ToWorldPos(Coordinate gridPos)
    {
        return new Vector2d(gridPos.x + GridOffset, gridPos.y + GridOffset);
    }
    public static Coordinate ToGridPos(Vector2d worldPos, int buildingSize)
    {
        if (buildingSize % 2 == 0)
        {
            worldPos.x += FixedMath.Half;
            worldPos.y += FixedMath.Half;
        }
        return ToGridPos(worldPos);
    }
    public static Vector2d ToWorldPos(Coordinate coordinate, int buildingSize)
    {
        Vector2d worldPos = ToWorldPos(coordinate);
        if (buildingSize % 2 == 0)
        {
            worldPos.x -= FixedMath.Half;
            worldPos.y -= FixedMath.Half;
        }
        return worldPos;
    }

    public static bool TryGetNode(Coordinate coordinate, out BuildGridNode buildNode)
    {
        if (MainBuildGrid.IsOnGrid(coordinate))
        {
            buildNode = MainBuildGrid.Grid[coordinate.x, coordinate.y];
            return true;
        }
        buildNode = null;
        return false;
    }
}
