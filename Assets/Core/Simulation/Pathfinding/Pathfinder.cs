//=======================================================================
// Copyright (c) 2015 John Pan
// Distributed under the MIT License.
// (See accompanying file LICENSE or copy at
// http://opensource.org/licenses/MIT)
// @modified 2019 David Oravsky
//=======================================================================

//Resources:
//Bresenham's Algorithm Implementation: ericw. (Source: http://ericw.ca/notes/bresenhams-line-algorithm-in-csharp.html)
using FastCollections;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep.Pathfinding
{
    public static class Pathfinder
    {
        #region Path Variables
        private static GridNode currentNode;
        private static GridNode neighbor;
        private static long lastDistance;
        #endregion

        private static int SearchCount;

        #region method sharing variables

        private static Dictionary<Vector2d, long> dijkstraGrid = new Dictionary<Vector2d, long>();
        private static Dictionary<Vector2d, long> rawDijkstraGrid;
        private static FastList<GridNode> markedNodes = new FastList<GridNode>();
        private static FastList<GridNode> rawMarkedNodes;

        private static GridNode rawNode;
        #endregion

        private static bool destinationIsReached;

        public static bool AllowUnwalkableEndNode { get; set; }

        //Reset pathfinding so it doesn't overflow with multiple scenes
        public static void Reset()
        {
            dijkstraGrid.Clear();
            markedNodes.FastClear();
            markedNodes.EnsureCapacity(GridManager.GridSize);
        }

        public static bool FindPath(GridNode startNode, GridNode endNode, Dictionary<Vector2d, Vector2d> outputVectorPath, int unitHalfSize = 1)
        {
            dijkstraGrid.Clear();
            markedNodes.FastClear();
            if (GenerateDistanceField(startNode, endNode, dijkstraGrid, markedNodes, unitHalfSize))//
            {
                GenerateVectorFlowField(rawDijkstraGrid, rawMarkedNodes, outputVectorPath);//

                return true;
            }

            return false;
        }

        /// <summary>                        
        /// Wavefront algorithm to create a distance field.
        /// </summary>
        /// <returns>
        /// Returns 
        /// <c>true</c> if path was found and necessary
        /// <c>false</c> if path to End is impossible or not found.
        /// </returns>
        /// <param name="startNode">Start node.</param>
        /// <param name="endNode">End node.</param>
        public static bool GenerateDistanceField(GridNode _startNode, GridNode _endNode, Dictionary<Vector2d, long> _dijkstraGrid, FastList<GridNode> _markedNodes, int _unitHalfSize) //
        {
            rawDijkstraGrid = _dijkstraGrid;
            rawDijkstraGrid.Clear();

            rawMarkedNodes = _markedNodes;
            rawMarkedNodes.FastClear();

            GridNode startNode = _startNode;
            GridNode endNode = _endNode;
            uint StartNodeIndex = startNode.gridIndex;

            destinationIsReached = false;

            #region Broadphase and Preperation
            if (endNode.Unwalkable && !AllowUnwalkableEndNode || startNode.Unwalkable)
            {
                return false;
            }

            if (ReferenceEquals(startNode, endNode))
            {
                // we're already at the destination!
                return false;
            }
            #endregion

            //Prepare Unpassable check optimizations
            GridNode.PrepareUnpassableCheck(_unitHalfSize);

            //flood fill out from the end point with a distance of 0
             rawDijkstraGrid.Add(endNode.WorldPos, 0);
         //   endNode.Distance = 0;
            rawMarkedNodes.Add(endNode);

            SearchCount = 0;
            lastDistance = 0;
            //for each node we need to visit, starting with the pathStart
            while (rawMarkedNodes.Count <= GridManager.Grid.Length)
            {
                rawNode = rawMarkedNodes[SearchCount];

                if (rawNode.IsNull() || rawNode.gridIndex == StartNodeIndex)
                {
                    //We found our way to the start node!
                    destinationIsReached = true;
                    return true;
                }

                //for each neighbour of this node (only straight line neighbours, not diagonals)
                long distance = lastDistance + 1;
                for (int i = 0; i < 4; i++)
                {
                    neighbor = rawNode.NeighborNodes[i];

                    if (neighbor.IsNull() || neighbor.Unpassable() || rawDijkstraGrid.ContainsKey(neighbor.WorldPos))
                    {
                        continue;
                    }

                    //We will only ever visit every node once as we are always visiting nodes in the most efficient order

            //        neighbor.Distance = rawNode.Distance + 1;
                    rawMarkedNodes.Add(neighbor);
                    rawDijkstraGrid.Add(neighbor.WorldPos, distance);
                }

                lastDistance = distance;
                SearchCount++;
            }

            return destinationIsReached;
        }

        public static void GenerateVectorFlowField(Dictionary<Vector2d, long> _dijkstraGrid, FastList<GridNode> _markedNodes, Dictionary<Vector2d, Vector2d> _outputVectorPath)//
        {
            _outputVectorPath.Clear();

            //for each grid square
            for (int i = 0; i < _markedNodes.Count - 1; i++)
            {
                GridNode node = _markedNodes[i];

                Vector2d nodePos = node.WorldPos;
                long nodeDistance;
                if (_dijkstraGrid.TryGetValue(nodePos, out nodeDistance))
                {
                    GridNode[] neighbors = node.NeighborNodes;

                    //Go through all neighbours and find the one with the lowest distance
                    GridNode minNeighbor = null;
                    long minDistance = 0;
                    for (int z = 0; z < neighbors.Length; z++)
                    {
                        GridNode neighbor = neighbors[z];
                        long neighborDistance;
                        if (neighbor.IsNotNull() && _dijkstraGrid.TryGetValue(neighbor.WorldPos, out neighborDistance))
                        {
                            neighborDistance = neighborDistance - nodeDistance;
                            if (neighborDistance < minDistance)
                            {
                                minNeighbor = neighbor;
                                minDistance = neighborDistance;
                            }
                        }
                    }

                    //If we found a valid neighbour, point in its direction
                    if (minNeighbor.IsNotNull() && !_outputVectorPath.ContainsKey(node.WorldPos))
                    {
                        Vector2d direction = minNeighbor.WorldPos - node.WorldPos;
                        direction.Normalize();

                        _outputVectorPath.Add(node.WorldPos, direction);
                    }
                }
            }
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
                        if (currentNode.IsNotNull() && !currentNode.Unwalkable)
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
                        if (currentNode.IsNotNull() || !currentNode.Unwalkable)
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