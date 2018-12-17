using FastCollections;
using RTSLockstep;
using System.Collections.Generic;

public class BuildGridManager
{
    public int GridLength { get; private set; }

    public int BuildSpacing { get; private set; }

    public BuildGridNode[,] Grid { get; private set; }

    private readonly FastList<Coordinate> bufferBuildCoordinates = new FastList<Coordinate>();
    private readonly FastList<Coordinate> bufferNeighborCoordinates = new FastList<Coordinate>();

    public FastList<Coordinate> BufferNeighborCoordinates
    {
        get { return bufferNeighborCoordinates; }
    }

    public BuildGridManager(int gridLength, int buildSpacing)
    {
        this.GridLength = gridLength;
        this.BuildSpacing = buildSpacing;
        Initialize();
    }

    private void Initialize()
    {
        Grid = new BuildGridNode[GridLength, GridLength];
        for (int i = 0; i < GridLength; i++)
        {
            for (int j = 0; j < GridLength; j++)
            {
                BuildGridNode node = new BuildGridNode(this, new Coordinate(i, j));
                Grid[i, j] = node;
            }
        }
    }

    public IEnumerable<IBuildable> GetOccupyingBuildables(IBuildable buildable)
    {
        if (!CanBuild(buildable.GridPosition, buildable.BuildSizeLow, buildable.BuildSizeHigh))
        {
            for (int i = 0; i < bufferBuildCoordinates.Count; i++)
            {
                Coordinate coor = bufferBuildCoordinates[i];
                BuildGridNode buildNode = Grid[coor.x, coor.y];
                if (buildNode.Occupied)
                {
                    yield return buildNode.RegisteredBuilding;
                }
            }
        }
    }

    public bool Construct(IBuildable buildable)
    {
        if (CanBuild(buildable.GridPosition, buildable.BuildSizeLow, buildable.BuildSizeHigh))
        {
            for (int i = 0; i < bufferBuildCoordinates.Count; i++)
            {
                Coordinate coor = bufferBuildCoordinates[i];
                Grid[coor.x, coor.y].RegisteredBuilding = buildable;
            }
            return true;
        }
        return false;
    }

    public void Unbuild(IBuildable buildable)
    {
        if (TryGetBuildCoordinates(buildable.GridPosition, buildable.BuildSizeLow, buildable.BuildSizeHigh, bufferBuildCoordinates))
        {
            for (int i = 0; i < bufferBuildCoordinates.Count; i++)
            {
                Coordinate coor = bufferBuildCoordinates[i];
                BuildGridNode buildNode = Grid[coor.x, coor.y];
                /*
                if (buildNode.Occupied == false)
                    Debug.Log("Not built");*/
                buildNode.RegisteredBuilding = null;
            }
        }
        else
        {
            throw new System.Exception("Specified area to unbuild is invalid");
        }

    }

    public bool CanBuild(Coordinate position, int sizeLow, int sizeHigh)
    {
        if (TryGetBuildCoordinates(position, sizeLow, sizeHigh, bufferBuildCoordinates) == false)
        {
            return false;
        }

        this.GetSpacedNeighborCoordinates(position, sizeLow, sizeHigh, this.bufferNeighborCoordinates);
        for (int i = 0; i < this.bufferNeighborCoordinates.Count; i++)
        {
            Coordinate coor = this.bufferNeighborCoordinates[i];
            if (Grid[coor.x, coor.y].Occupied)
            {
                return false;
            }
        }

        for (int i = 0; i < bufferBuildCoordinates.Count; i++)
        {
            Coordinate coor = bufferBuildCoordinates[i];
            if (this.IsOnGrid(coor.x, coor.y))
            {
                if (this.Grid[coor.x, coor.y].Occupied)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool IsValid(Coordinate coor, int size)
    {
        for (int i = 0; i < size + 2; i++)
        {
            int temp = coor.x + i - 1;
            if (temp < 0 || temp >= GridLength)
                return false;
            temp = coor.y - 1;
            if (temp < 0 || temp >= GridLength)
                return false;
            temp = coor.y + size;
            if (temp < 0 || temp >= GridLength)
                return false;
            temp = coor.y + i - 1;
            if (temp < 0 || temp >= GridLength)
                return false;
        }
        return true;
    }

    public bool IsOnGrid(Coordinate coor)
    {
        return IsOnGrid(coor.x, coor.y);
    }

    public bool IsOnGrid(int x, int y)
    {
        return IsOnGrid(x) && IsOnGrid(y);
    }

    public bool IsOnGrid(int value)
    {
        return value >= 0 && value < GridLength;
    }

    private bool TryGetBuildCoordinates(Coordinate position, int sizeLow, int sizeHigh, FastList<Coordinate> output)
    {
        int halfLow = sizeLow / 2;
        int halfHigh = sizeHigh / 2;

        int lowX = halfLow, lowY = halfHigh;
        int highX = halfLow, highY = halfHigh;

        if (sizeLow % 2 == 0)
        {
            lowX -= 1;
            lowY -= 1;
        }

        lowX = position.x - lowX;
        if (!IsOnGrid(lowX))
            return false;
        lowY = position.y - lowY;
        if (!IsOnGrid(lowY))
            return false;
        highX = position.x + highX;
        if (!IsOnGrid(highX))
            return false;
        highY = position.y + highY;
        if (!IsOnGrid(highY))
            return false;

        int width = highX - lowX;
        int height = highY - lowY;

        output.FastClear();

        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                output.Add(new Coordinate(lowX + x, lowY + y));
            }
        }

        return true;
    }

    public void GetSpacedNeighborCoordinates(Coordinate position, int sizeLow, int sizeHigh, FastList<Coordinate> output)
    {
        int halfLow = sizeLow / 2;
        int halfHigh = sizeHigh / 2;

        int lowX = halfLow, lowY = halfHigh;
        int highX = halfLow, highY = halfHigh;

        if (sizeLow % 2 == 0)
        {
            lowX -= 1;
            lowY -= 1;
        }

        lowX = position.x - lowX;
        highX = position.x + highX;
        lowY = position.y - lowY;
        highY = position.y + highY;

        int neighborLowX = lowX - BuildSpacing;
        int neighborHighX = highX + BuildSpacing;
        int neighborLowY = lowY - BuildSpacing;
        int neighborHighY = highY + BuildSpacing;

        output.FastClear();

        for (int x = neighborLowX; x <= neighborHighX; x++)
        {
            if (IsOnGrid(x) == false)
            {
                continue;
            }
            for (int y = neighborLowY; y <= neighborHighY; y++)
            {
                if (IsOnGrid(y) == false)
                {
                    continue;
                }
                if (x >= lowX && x <= highX && y >= lowY && y <= highY)
                    continue;
                output.Add(new Coordinate(x, y));
            }
        }
    }

    public void SetBuildSpacing(int buildSpacing)
    {
        this.BuildSpacing = buildSpacing;
    }
}

