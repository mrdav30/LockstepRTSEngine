//=======================================================================
// Copyright (c) 2015 John Pan
// Distributed under the MIT License.
// (See accompanying file LICENSE or copy at
// http://opensource.org/licenses/MIT)
//=======================================================================

using FastCollections;
using RTSLockstep.Pathfinding;
using UnityEngine;

namespace RTSLockstep.Grid
{
    public class GridNode
    {
        #region Properties
        #region Collection Helpers
        /// <summary>
        /// TODO: Maybe it will be more efficient for memory to not cache this?
        /// </summary>
        public uint GridVersion;
        #endregion

        public GridNode[] NeighborNodes = new GridNode[8];
        public Vector2d WorldPos;

        public ScanNode LinkedScanNode;

        #region Pathfinding Variables
        public Vector2d GridPos;
        public int gridX;
        public int gridY;
        public uint gridIndex;

        public FlowField FlowField;

        public const byte DEFAULT_DEGREE = byte.MaxValue;
        public const byte DEFAULT_SOURCE = byte.MaxValue;

        private byte _obstacleCount;

        public bool Unwalkable
        {
            get
            {
                return _obstacleCount > 0;
            }
        }

        public bool Occupied
        {
            get
            {
                return LinkedScanNode.IsNotNull() && LinkedScanNode.AgentCount > 0;
            }
        }

        public byte ClearanceSource;
        /// <summary>
        /// How many connections until the closest unwalkable node.
        /// If a big unit stands directly on this node, it won't be able to fit if the degree is too low.
        /// </summary>
        public byte ClearanceDegree;
        #endregion

        private static int CachedUnpassableCheckSize;
        private static int _i;

        private GridNode _node;

        private static int x, y, checkX, checkY, leIndex;
        #endregion        

        #region Constructor
        public GridNode()
        {

        }

        public void Setup(int _x, int _y)
        {
            if (_x < 0 || _y < 0)
            {
                Debug.LogError("Cannot be negative!");
            }
            gridX = _x;
            gridY = _y;
            GridPos = new Vector2d(gridX, gridY);
            gridIndex = GridManager.GetGridIndex(gridX, gridY);
            WorldPos.x = gridX * FixedMath.One + GridManager.OffsetX;
            WorldPos.y = gridY * FixedMath.One + GridManager.OffsetY;

            FlowField = new FlowField(WorldPos, 0, Vector2d.zero);
        }

        public void Initialize()
        {
            GenerateNeighbors();
            LinkedScanNode = GridManager.GetScanNode(gridX / GridManager.ScanResolution, gridY / GridManager.ScanResolution);
            ClearanceDegree = DEFAULT_DEGREE;
            ClearanceSource = DEFAULT_SOURCE;
            FastInitialize();
        }

        public void FastInitialize()
        {
            GridVersion = 0;
            _obstacleCount = 0;
        }
        #endregion

        #region Pathfinding
        public byte GetClearanceDegree()
        {
            CheckUpdateValues();
            return ClearanceDegree;
        }

        private void CheckUpdateValues()
        {
            if (GridVersion != GridManager.GridVersion)
            {
                UpdateValues();
            }
        }

        private void UpdateClearance()
        {
            if (Unwalkable)
            {
                ClearanceDegree = 0;
                ClearanceSource = DEFAULT_SOURCE;
            }
            else
            {
                if (ClearanceSource <= 7)
                {
                    //refresh source in case the map changed
                    var source = NeighborNodes[ClearanceSource];
                    if (source.IsNotNull())
                    {
                        var prevSourceDegree = source.ClearanceDegree;
                        if (source.ClearanceDegree < ClearanceDegree)
                        {
                            source.UpdateValues();
                            //Clearance from source can no longer be trusted!
                            if (source.ClearanceDegree != prevSourceDegree)
                            {
                                ClearanceDegree = DEFAULT_DEGREE;
                                ClearanceSource = DEFAULT_SOURCE;
                            }
                        }
                        else
                        {
                            ClearanceDegree = (byte)(source.ClearanceDegree + 1);
                        }
                    }
                }
                // This method isn't always 100% accurate but after several updates, it will have a better picture of the map
                // Clarification: _clearanceSource is the source of a blockage. 
                // It's cached so that when the map is changed, the source of the major block can be rechecked for changes.
                // TODO: Test this thoroughly and visualize
                for (int i = 7; i >= 0; i--)
                {
                    var neighbor = NeighborNodes[i];
                    if (neighbor.IsNull() || neighbor.Unwalkable)
                    {
                        ClearanceDegree = 1;
                        ClearanceSource = (byte)i;
                        break;
                    }
                    //Cap clearance to 8. Something larger than that won't work very well with pathfinding.
                    if (neighbor.ClearanceDegree < ClearanceDegree && neighbor.ClearanceDegree < 8)
                    {
                        ClearanceDegree = (byte)(neighbor.ClearanceDegree + 1);
                        ClearanceSource = (byte)i;
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if clearance degree changed.
        /// </summary>
        /// <returns></returns>
        private void UpdateValues()
        {
            GridVersion = GridManager.GridVersion;

            //fast enough to just do it
            UpdateClearance();
        }

        //Prepare Unpassable check optimizations
        internal static void PrepareUnpassableCheck(int size)
        {
            CachedUnpassableCheckSize = size;
        }

        /// <summary>
        /// If this unit is too fat to fit.
        /// </summary>
        internal bool Unpassable()
        {
            if (CachedUnpassableCheckSize > 0)
            {
                //If there's an unwalkable within the size's number of connections, the unit cannot pass
                return GetClearanceDegree() < CachedUnpassableCheckSize;
            }
            else
            {
                return Unwalkable;
            }
        }

        public void AddObstacle()
        {
#if DEBUG
            if (_obstacleCount == byte.MaxValue)
            {
                Debug.LogErrorFormat("Too many obstacles on this node ({0})!", new Coordinate(gridX, gridY));
            }
#endif
            _obstacleCount++;
            GridManager.NotifyGridChanged();
        }

        public void RemoveObstacle()
        {
            if (_obstacleCount == 0)
            {
                Debug.LogErrorFormat("No obstacle to remove on this node ({0})!", new Coordinate(gridX, gridY));
            }
            _obstacleCount--;
            GridManager.NotifyGridChanged();
        }

        private void GenerateNeighbors()
        {
            //0-3 = sides, 4-7 = diagonals
            //0 = (-1, 0)   // West
            //1 = (0,-1) // South
            //2 = (0,1)  // East
            //3 = (1,0) // North
            //4 = (-1,-1)   // South-West
            //5 = (-1,1)  // North-West
            //6 = (1,-1)  // South-East
            //7 = (1,1)   // North-East
            int sideIndex = 0;
            int diagonalIndex = 4;

            for (x = -1; x <= 1; x++)
            {
                for (y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) //Don't do anything for the same node
                    {
                        continue;
                    }

                    checkX = gridX + x;
                    checkY = gridY + y;

                    if (GridManager.ValidateCoordinates(checkX, checkY))
                    {
                        int neighborIndex;
                        if ((x != 0 && y != 0))
                        {
                            //Diagonal
                            if (GridManager.UseDiagonalConnections)
                            {
                                neighborIndex = diagonalIndex++;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            neighborIndex = sideIndex++;
                        }

                        GridNode checkNode = GridManager.Grid[GridManager.GetGridIndex(checkX, checkY)];
                        NeighborNodes[neighborIndex] = checkNode;
                    }
                }
            }
        }

        //Returns the unobstructed neighbours of the given grid location.
        //Diagonals are only included if their neighbours are also unobstructed
        public FastList<GridNode> UnblockedNeighboursOf()
        {
            FastList<GridNode> unobstructedNeighbors = new FastList<GridNode>();

            GridNode South = NeighborNodes[1];
            GridNode East = NeighborNodes[2];
            GridNode West = NeighborNodes[0];
            GridNode North = NeighborNodes[3];

            //We test each straight direction, then subtest the next one clockwise

            if (West.IsNotNull() && !West.Unwalkable)
            {
                unobstructedNeighbors.Add(West);

                //left up
                if (South.IsNotNull() && !South.Unwalkable)
                {
                    GridNode SouthWest = NeighborNodes[4];
                    if (SouthWest.IsNotNull() && !SouthWest.Unwalkable)
                    {
                        unobstructedNeighbors.Add(SouthWest);
                    }
                }
            }

            if (South.IsNotNull() & !South.Unwalkable)
            {
                unobstructedNeighbors.Add(South);

                //up right
                if (North.IsNotNull() && !North.Unwalkable)
                {
                    GridNode SouthEast = NeighborNodes[6];
                    if (SouthEast.IsNotNull() && !SouthEast.Unwalkable)
                    {
                        unobstructedNeighbors.Add(SouthEast);
                    }
                }
            }

            if (North.IsNotNull() && !North.Unwalkable)
            {
                unobstructedNeighbors.Add(North);

                //right down
                if (East.IsNotNull() && !East.Unwalkable)
                {
                    GridNode NorthEast = NeighborNodes[7];
                    if (NorthEast.IsNotNull() && !NorthEast.Unwalkable)
                    {
                        unobstructedNeighbors.Add(NorthEast);
                    }
                }
            }

            if (East.IsNotNull() && !East.Unwalkable)
            {
                unobstructedNeighbors.Add(East);

                //down left
                if (West.IsNotNull() && !West.Unwalkable)
                {
                    GridNode NorthWest = NeighborNodes[5];
                    if (NorthWest.IsNotNull() && !NorthWest.Unwalkable)
                    {
                        unobstructedNeighbors.Add(NorthWest);
                    }
                }
            }

            return unobstructedNeighbors;
        }
        #endregion

        #region Influence
        public void Add(LSInfluencer influencer)
        {
            LinkedScanNode.Add(influencer);
        }

        public void Remove(LSInfluencer influencer)
        {
            LinkedScanNode.Remove(influencer);
        }
        #endregion

        public override int GetHashCode()
        {
            return (int)gridIndex;
        }

        public bool DoesEqual(GridNode obj)
        {
            return obj.gridIndex == gridIndex;
        }

        public override string ToString()
        {
            return "(" + gridX.ToString() + ", " + gridY.ToString() + ")";
        }
    }
}