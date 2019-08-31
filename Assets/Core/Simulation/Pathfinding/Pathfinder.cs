//=======================================================================
// Copyright (c) 2015 John Pan
// Distributed under the MIT License.
// (See accompanying file LICENSE or copy at
// http://opensource.org/licenses/MIT)
//=======================================================================

//Resources:
//Bresenham's Algorithm Implementation: ericw. (Source: http://ericw.ca/notes/bresenhams-line-algorithm-in-csharp.html)
//AStar Algorithm Template: Sebastian Lague (Source: https://www.youtube.com/watch?v=-L-WgKMFuhE)
using FastCollections;

//TODO: System for combining paths. i.e. 1st path is found, 2nd path finds way onto 1st path, 2nd path returns rest of 1st path
namespace RTSLockstep.Pathfinding
{
    public static class Pathfinder
    {
        #region Wrapper Variables
        private static GridNode node1;
        private static GridNode node2;
        private static int IndexX, IndexY;
        private static FastList<GridNode> TracePath = new FastList<GridNode>();
        private static int length;
        #endregion

        #region Astar Variables
        private static GridNode currentNode;
        private static GridNode neighbor;
        private static int newMovementCostToNeighbor;
        private static int i;
        private static uint StartNodeIndex;
        private static uint EndNodeIndex;
        private static bool FindStraight;
        private static bool LastIsObstructed;
        private static bool hasInvalidEdge;
        #endregion

        #region CombinePath Variables
        private static uint lastGridIndex;

        public static uint CombineIteration { get; private set; }
        #endregion

        public static uint CombineVersionCheck { get; private set; }
        private const uint DefaultCombineVersion = uint.MaxValue;
        public static uint CombineVersionSet { get; private set; }
        private static int SearchCount;

        #region method sharing variables
        static GridNode startNode;
        static GridNode endNode;
        static FastList<GridNode> outputPathBuffer = new FastList<GridNode>();
        static FastList<GridNode> rawOutputPath;

        static int unitHalfSize;
        static GridNode rawNode;
        #endregion

        private static int xSign, ySign;

        private static bool destinationIsReached;

        public static bool AllowUnwalkableEndNode { get; set; }

        //CombinePath is a system that saves the paths to most recent destination, allowing new path queries to use these paths if they happen to find them
        public static void ChangeCombineIteration()
        {
            lastGridIndex = uint.MaxValue;
        }
        public static void Reset()
        {
            //Reset combine value so it doesn't overflow with multiple scenes
            CombineIteration = 0;
            GridHeap.Reset();
        }

        public static bool FindPath(Vector2d Start, Vector2d End, FastList<Vector2d> outputVectorPath, int unitHalfSize = 1)
        {
            if (!GetPathNodes(Start.x, Start.y, End.x, End.y, out node1, out node2))
            {
                return false;
            }

            outputPathBuffer.FastClear();
            if (FindRawPath(node1, node2, outputPathBuffer, unitHalfSize))
            {
                SmoothPath(outputPathBuffer, End, outputVectorPath, unitHalfSize);
                return true;
            }
            return false;
        }

        public static bool FindPath(Vector2d End, GridNode startNode, GridNode endNode, FastList<Vector2d> outputVectorPath, int unitHalfSize = 1, uint combinePathsVersion = 0)
        {
            outputPathBuffer.FastClear();
            if (FindRawPath(startNode, endNode, outputPathBuffer, unitHalfSize))
            {
                SmoothPath(outputPathBuffer, End, outputVectorPath, unitHalfSize);

                return true;
            }
            return false;
        }

        /// <summary>                        
        /// Finds a path and outputs it to <c>outputPath</c>. Note: outputPath is unpredictably changed.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if path was found and necessary, <c>false</c> if path to End is impossible or not found.
        /// </returns>
        /// <param name="startNode">Start node.</param>
        /// <param name="endNode">End node.</param>
        /// <param name="outputPath">Return path.</param>
        public static bool FindRawPath(GridNode _startNode, GridNode _endNode, FastList<GridNode> _outputPath, int _unitHalfSize)
        {
            //TODO: Not critical but there's a lot of room for better organization
            //i.e. All these static variables and methods goes into individual singleton classes
            startNode = _startNode;
            endNode = _endNode;

            rawOutputPath = _outputPath;
            rawOutputPath.FastClear();

            unitHalfSize = _unitHalfSize;
            StartNodeIndex = startNode.gridIndex;
            EndNodeIndex = endNode.gridIndex;

            destinationIsReached = false;

            #region Broadphase and Preperation
            if (endNode.Unwalkable && !AllowUnwalkableEndNode || startNode.Unwalkable)
            {
                return destinationIsReached;
            }

            if (ReferenceEquals(startNode, endNode))
            {
                rawOutputPath.Add(endNode);
                return destinationIsReached = true;
            }
            GridHeap.FastClear();
            //POSBUG: Hash for end destination and frame count. *Most likely* won't overflow
            //Or no need to factor in frame count
            #endregion

            #region AStar Algorithm
            GridHeap.Add(startNode);
            GridNode.HeuristicTargetX = endNode.gridX;
            GridNode.HeuristicTargetY = endNode.gridY;

            GridNode.PrepareUnpassableCheck(unitHalfSize); //Prepare Unpassable check optimizations

            SearchCount = 0;
            CombineVersionSet = CombineIteration * GridManager.MaxIndex + EndNodeIndex;
            if (lastGridIndex == EndNodeIndex)
            {
                CombineVersionCheck = CombineVersionSet;
            }
            else
            {
                if (CombineVersionCheck != DefaultCombineVersion)
                {
                    CombineIteration++;
                    CombineVersionCheck = DefaultCombineVersion;
                }
            }

            lastGridIndex = EndNodeIndex;
            while (GridHeap.Count > 0)
            {
                SearchCount++;
                rawNode = GridHeap.RemoveFirst();

                if (rawNode.gridIndex == EndNodeIndex)
                {
                    //We found our way to the end node!
                    DestinationReached();
                    return destinationIsReached = true;
                }

                if (CombineVersionCheck != DefaultCombineVersion)
                {
                    if (rawNode.CombinePathVersion == CombineVersionCheck)
                    {
                        //We found our way onto an existing path!
                        DestinationReached(true);
                        return destinationIsReached = true;
                    }
                }

                #region Allows diagonal access when edges are blocked
                for (i = 0; i < 4; i++)
                {
                    neighbor = rawNode.NeighborNodes[i];
                    if (CheckNeighborSearchable())
                    {
                        if (neighbor.Unpassable() == false)
                        {
                            newMovementCostToNeighbor = rawNode.gCost + 100;
                            ProcessNode();
                        }
                        else if (neighbor.gridIndex == EndNodeIndex)
                        {
                            AddBestNode();
                            DestinationReached();
                            return destinationIsReached = true;
                        }
                    }
                }

                for (int i = 4; i < 8; i++)
                {
                    neighbor = rawNode.NeighborNodes[i];
                    if (CheckNeighborSearchable())
                    {
                        if (neighbor.Unpassable() == false)
                        {
                            newMovementCostToNeighbor = rawNode.gCost + 141;
                            ProcessNode();
                        }
                        else if (neighbor.gridIndex == EndNodeIndex)
                        {
                            AddBestNode();
                            DestinationReached();
                            return destinationIsReached = true;
                        }
                    }
                }

                GridHeap.Close(rawNode);
                #endregion
            }

            #endregion
            return destinationIsReached;
        }

        public static void SmoothPath(FastList<GridNode> nodePath, Vector2d End, FastList<Vector2d> outputVectorPath, int unitHalfSize)
        {
            //nodePath should include the start and end nodes
            outputVectorPath.FastClear();
            length = nodePath.Count - 1;
            //culling out unneded nodes

            var StartNode = nodePath[0];
            //outputVectorPath.Add(StartNode.WorldPos);
            GridNode oldNode = StartNode;
            long oldX = 0;
            long oldY = 0;
            long newX;
            long newY;
            for (i = 1; i < length; i++)
            {
                GridNode node = nodePath[i];

                //  bool important = false;
                //Anyone who's somebody is near an unwalkable node
                bool important = node.GetClearanceDegree() <= unitHalfSize + 1;
                if (important)
                {
                    newX = node.gridX - oldNode.gridX;
                    newY = node.gridY - oldNode.gridY;
                    if ((newX <= 1 && newX >= -1)
                        && (newY <= 1 && newY >= -1))
                    {
                        if (newX == oldX && newY == oldY)
                        {
                            if (oldX != 0 || oldY != 0)
                            {
                                outputVectorPath.RemoveAt(outputVectorPath.Count - 1);
                            }
                        }
                        else
                        {
                            oldX = newX;
                            oldY = newY;
                        }
                    }
                    else
                    {
                        oldX = 0;
                        oldY = 0;
                    }
                    outputVectorPath.Add(node.WorldPos);

                    oldNode = node;

                }
            }

            outputVectorPath.Add(End);
        }

        private static bool CheckInvalid(GridNode gridNode)
        {
            return gridNode.IsNull() || GridHeap.Closed(gridNode) || gridNode.Unpassable();
        }

        private static bool CheckNeighborSearchable()
        {
            return neighbor.IsNotNull() && GridHeap.Closed(neighbor) == false;
        }

        private static void ProcessNode()
        {
            if (!GridHeap.Contains(neighbor))
            {
                AddBestNode();
                GridHeap.Add(neighbor);
            }
            else if (newMovementCostToNeighbor < neighbor.gCost)
            {
                AddBestNode();
                GridHeap.UpdateItem(neighbor);
            }
        }

        private static void AddBestNode()
        {
            neighbor.gCost = newMovementCostToNeighbor;

            //Optimized heuristic calculation
            neighbor.CalculateHeuristic();
            neighbor.parent = rawNode;
            //CombinePaths
        }

        private static void DestinationReached(bool isCombine = false)
        {
            //Faster to do this to end loop than to check every AnalyzeNode () call
            destinationIsReached = true;

            rawOutputPath.FastClear();
            TracePath.FastClear();

            GridNode node;
            if (isCombine)
            {
                node = rawNode;
            }
            else
            {
                node = endNode;
            }

            int count = 0;
            while (node.gridIndex != StartNodeIndex)
            {
                //Sets CombinePathVersion while tracing path
                node.CombinePathVersion = CombineVersionSet;
                node.parent.combineTrailNode = node;
                node = node.parent;
                count++;

                if (count > 1000)
                {
                    throw new System.Exception("path too long");
                }
            }

            count = 0;
            //Trace with combineTrail from startNode to endNode
            //output should include start node and end node
            rawOutputPath.Add(startNode);
            node = startNode.combineTrailNode;
            while (node.gridIndex != EndNodeIndex)
            {
                rawOutputPath.Add(node);
                node = node.combineTrailNode;
                count++;
                if (count > 1000)
                {
                    throw new System.Exception("trail too long");
                }
            }
            //adds the end node
            rawOutputPath.Add(node);
        }

        public static bool NeedsPath(GridNode startNode, GridNode endNode, int unitSize)
        {
            int dx, dy, error, ystep, x, y, t;
            int x0, y0, x1, y1;
            int compare1, compare2;
            int retX, retY;
            bool steep;

            //Tests if there is a direct path. If there is, no need to run AStar.
            x0 = startNode.gridX;
            y0 = startNode.gridY;
            x1 = endNode.gridX;
            y1 = endNode.gridY;
            if (y1 > y0)
            {
                compare1 = y1 - y0;
            }
            else
            {
                compare1 = y0 - y1;
            }

            if (x1 > x0)
            {
                compare2 = x1 - x0;
            }
            else
            {
                compare2 = x0 - x1;
            }

            steep = compare1 > compare2;
            if (steep)
            {
                t = x0; // swap x0 and y0
                x0 = y0;
                y0 = t;
                t = x1; // swap x1 and y1
                x1 = y1;
                y1 = t;
            }
            if (x0 > x1)
            {
                t = x0; // swap x0 and x1
                x0 = x1;
                x1 = t;
                t = y0; // swap y0 and y1
                y0 = y1;
                y1 = t;
            }
            dx = x1 - x0;

            dy = (y1 - y0);
            if (dy < 0)
            {
                dy = -dy;
            }

            error = dx / 2;
            ystep = (y0 < y1) ? 1 : -1;
            y = y0;
            GridNode.PrepareUnpassableCheck(unitSize);

            for (x = x0; x <= x1; x++)
            {
                retX = (steep ? y : x);
                retY = (steep ? x : y);

                currentNode = GridManager.Grid[GridManager.GetGridIndex(retX, retY)];
                if (currentNode.IsNotNull() && currentNode.Unpassable())
                {
                    break;
                }
                else if (x == x1)
                {
                    return false;
                }

                error = error - dy;
                if (error < 0)
                {
                    y += ystep;
                    error += dx;
                }
            }

            return true;
        }

        public static bool GetPathNodes(long StartX, long StartY, long EndX, long EndY, out GridNode startNode, out GridNode endNode)
        {
            startNode = GridManager.GetNode(StartX, StartY);
            if (startNode.Unwalkable)
            {
                for (i = 0; i < 8; i++)
                {
                    currentNode = startNode.NeighborNodes[i];
                    if (System.Object.ReferenceEquals(currentNode, null) == false && currentNode.Unwalkable == false)
                    {
                        startNode = currentNode;
                        break;
                    }
                }
                if (startNode.Unwalkable)
                {
                    endNode = null;
                    return false;
                }
            }
            endNode = GridManager.GetNode(EndX, EndY);

            if (endNode.Unwalkable)
            {
                if (AllowUnwalkableEndNode)
                {
                    return AlternativeNodeFinder.Instance.CheckValidNeighbor(endNode);
                }
                for (i = 0; i < 8; i++)
                {
                    currentNode = endNode.NeighborNodes[i];
                    if (ReferenceEquals(currentNode, null) == false && currentNode.Unwalkable == false)
                    {
                        endNode = currentNode;
                        break;
                    }
                }
                if (endNode.Unwalkable)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Finds closest next-best-node also when destination is off invalid
        /// </summary>
        /// <param name="from"></param>
        /// <param name="dest"></param>
        /// <param name="returnNode"></param>
        /// <returns></returns>
		public static bool GetEndNode(Vector2d from, Vector2d dest, out GridNode outputNode)
        {
            outputNode = GridManager.GetNode(dest.x, dest.y);
            if (outputNode == null)
            {
                //If null, it is off the grid. Raycast back onto grid for closest viable node to the destination.
                foreach (var coordinate in PanLineAlgorithm.FractionalLineAlgorithm.Trace(dest.x.ToDouble(), dest.y.ToDouble(), from.x.ToDouble(), from.y.ToDouble()))
                {
                    outputNode = GridManager.GetNode(FixedMath.Create(coordinate.X), FixedMath.Create(coordinate.Y));
                    if (outputNode != null)
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (outputNode.Unwalkable)
            {
                if (AllowUnwalkableEndNode && AlternativeNodeFinder.Instance.CheckValidNeighbor(outputNode))
                {
                    return true;
                }
                return StarCast(dest, out outputNode);
            }
            return true;
        }

        /// <summary>
        /// Finds closest next-best-node
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="returnNode"></param>
        /// <returns></returns>
        public static bool GetStartNode(Vector2d dest, out GridNode returnNode)
        {
            returnNode = GridManager.GetNode(dest.x, dest.y);
            if (returnNode == null || (returnNode.Unwalkable))
            {
                return StarCast(dest, out returnNode);
            }
            return true;
        }

        public static bool StarCast(Vector2d dest, out GridNode returnNode)
        {
            int xGrid, yGrid;
            GridManager.GetCoordinates(dest.x, dest.y, out xGrid, out yGrid);
            const int maxTestDistance = 3;
            AlternativeNodeFinder.Instance.SetValues(dest, xGrid, yGrid, maxTestDistance);
            returnNode = AlternativeNodeFinder.Instance.GetNode();
            if (returnNode == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool GetClosestViableNode(Vector2d from, Vector2d dest, int pathingSize, out GridNode returnNode)
        {
            returnNode = GridManager.GetNode(dest.x, dest.y);
            if (returnNode.Unwalkable)
            {
                bool valid = false;
                PanLineAlgorithm.FractionalLineAlgorithm.Coordinate cacheCoord = new PanLineAlgorithm.FractionalLineAlgorithm.Coordinate();
                bool validTriggered = false;
                pathingSize = (pathingSize + 1) / 2;
                int minSqrMag = pathingSize * pathingSize;
                minSqrMag *= 2;

                foreach (var coordinate in PanLineAlgorithm.FractionalLineAlgorithm.Trace(dest.x.ToDouble(), dest.y.ToDouble(), from.x.ToDouble(), from.y.ToDouble()))
                {
                    currentNode = GridManager.GetNode(FixedMath.Create(coordinate.X), FixedMath.Create(coordinate.Y));
                    if (!validTriggered)
                    {
                        if (currentNode != null && currentNode.Unwalkable == false)
                        {
                            validTriggered = true;
                        }
                        else
                        {
                            cacheCoord = coordinate;
                        }
                    }
                    if (validTriggered)
                    {
                        if (currentNode != null || !currentNode.Unwalkable)
                        {
                            //calculate sqrMag to last invalid node
                            int testMag = coordinate.X - cacheCoord.X;
                            testMag *= testMag;
                            int buffer = coordinate.Y - cacheCoord.Y;
                            buffer *= buffer;
                            testMag += buffer;
                            if (testMag >= minSqrMag)
                            {
                                valid = true;
                                break;
                            }
                        }
                    }
                }

                if (!valid)
                {
                    return false;
                }
                else
                {
                    returnNode = currentNode;
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
    }
}