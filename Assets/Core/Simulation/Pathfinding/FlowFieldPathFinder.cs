//=======================================================================
// Copyright (c) 2015 John Pan
// Distributed under the MIT License.
// (See accompanying file LICENSE or copy at
// http://opensource.org/licenses/MIT)
// @modified 2019 David Oravsky
//=======================================================================

using FastCollections;
using RTSLockstep.Grid;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace RTSLockstep.Pathfinding
{
    public static class FlowFieldPathFinder
    {
        #region method sharing variables
        private static int SearchCount;
        private static int length;
        private static int greatestDistance;

        private static FlowFieldPath flowFieldPath;

        private static FastList<GridNode> markedNodesBuffer = new FastList<GridNode>();
        private static FastList<GridNode> rawMarkedNodes;
        //holding tank to check if node was checked, faster than searching list
        //  private static Dictionary<Vector2d, bool> dijkstraGrid = new Dictionary<Vector2d, bool>();
        private static HashSet<GridNode> closedSet = new HashSet<GridNode>();

        private static GridNode rawNode;
        private static GridNode neighbor;

        private static bool destinationIsReached;
        #endregion

        public static void Reset()
        {
            //Reset combine value so it doesn't overflow with multiple scenes
            markedNodesBuffer.FastClear();
            markedNodesBuffer.EnsureCapacity(GridManager.GridSize);
            closedSet.Clear();
        }

        public static void FindPath(GridNode startNode, GridNode endNode, int unitHalfSize = 1)
        {
            markedNodesBuffer.FastClear();
            if (GenerateDistanceField(startNode, endNode, unitHalfSize, markedNodesBuffer))
            {
                // Distance vector field created, 
                GenerateVectorFlowField(markedNodesBuffer);
            }
            else
            {
                // Finish path request, destination not found
                PathRequestManager.FinishedProcessingPath(null, false);
            }
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
        public static bool GenerateDistanceField(GridNode _startNode, GridNode _endNode, int _unitHalfSize, FastList<GridNode> _markedNodes)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            rawMarkedNodes = _markedNodes;
            rawMarkedNodes.FastClear();

            closedSet.Clear();

            flowFieldPath = new FlowFieldPath(_startNode, _endNode);

            if (!flowFieldPath.CheckValid())
            {
                return false;
            }

            //flood fill out from the end point with a distance of 0
            rawMarkedNodes.Add(flowFieldPath.EndNode);
            closedSet.Add(flowFieldPath.EndNode);

            //Prepare Unpassable check optimizations
            GridNode.PrepareUnpassableCheck(_unitHalfSize);

            destinationIsReached = false;
            SearchCount = 0;
            //for each node we need to visit, starting with the pathStart
            while (SearchCount <= GridManager.Grid.Length)
            {
                rawNode = rawMarkedNodes[SearchCount];

                if (rawNode.IsNull())
                {
                    continue;
                }

                //for each neighbour of this node  (only straight line neighbours, not diagonals)
                for (int i = 0; i < 4; i++)
                {
                    neighbor = rawNode.NeighborNodes[i];

                    //We will only ever visit every node once as we are always visiting nodes in the most efficient order
                    if (neighbor.IsNotNull() && !closedSet.Contains(neighbor))
                    {
                        if (!neighbor.Unpassable())
                        {
                            neighbor.FlowField.Distance = rawNode.FlowField.Distance + 1;

                            // Check if we have LOS		
                            //rawNode.FlowField.HasLOS = !Pathfinder.NeedsPath(rawNode, flowFieldPath.EndNode, _unitHalfSize);

                            rawMarkedNodes.Add(neighbor);
                            closedSet.Add(neighbor);

                            if (neighbor.gridIndex == flowFieldPath.StartNodeIndex)
                            {
                                //We found our way to the start node!
                                sw.Stop();
                                UnityEngine.Debug.Log("Path found: " + sw.ElapsedMilliseconds + " ms");

                                destinationIsReached = true;
                                // break;
                            }
                        }
                    }
                }

                if (destinationIsReached
                    && rawNode.FlowField.Distance > flowFieldPath.StartNode.FlowField.Distance + 10)
                {
                    break;
                }

                SearchCount++;
            }

            return destinationIsReached;
        }

        public static void GenerateVectorFlowField(FastList<GridNode> _markedNodes)
        {
            length = _markedNodes.Count - 1;
            greatestDistance = _markedNodes[length].FlowField.Distance;
            UnityEngine.Debug.Log("endnode: " + flowFieldPath.EndNode.WorldPos);
            //for each grid square
            for (int i = 0; i < length; i++)
            {
                GridNode node = _markedNodes[i];
                int nodeDistance = node.FlowField.Distance;

                if (node.gridIndex != flowFieldPath.EndNodeIndex)
                {
                    GridNode[] neighbors = node.NeighborNodes;

                    int left, right, up, down;

                    //// The 4 weights we'll interpolate (only straight line neighbours, not diagonals)
                    //// see http://en.wikipedia.org/wiki/File:Bilininterp.png for the coordinates
                    left = GetNodeDistance(neighbors[0], nodeDistance); // west
                    down = GetNodeDistance(neighbors[1], nodeDistance); // south
                    up = GetNodeDistance(neighbors[2], nodeDistance); // east
                    right = GetNodeDistance(neighbors[3], nodeDistance); // north

                    //Do the x interpolations
                    int x = left - right;
                    //Do the y interpolation
                    int y = down - up;

                    //point in the direction of its neighbor
                    node.FlowField.Direction = new Vector2d(x, y);
                }
                else
                {
                    //end node shouldn't point anywhere
                    node.FlowField.Direction = Vector2d.zero;
                }

                node.FlowField.Direction.Normalize();

                flowFieldPath.OutputVectorPath.Add(node.WorldPos, new FlowField(node.FlowField.Distance, node.FlowField.Direction, node.FlowField.HasLOS));
            }

            PathRequestManager.FinishedProcessingPath(flowFieldPath.OutputVectorPath, true);
        }

        //private void CalculateLOS()
        //{
        //    var xDif = flowFieldPath.EndNode.gridX - rawNode.gridX;
        //    var yDif = flowFieldPath.EndNode.gridY - rawNode.gridY;

        //    var xDifAbs = Math.Abs(xDif);
        //    var yDifAbs = Math.Abs(yDif);

        //    var hasLos = false;

        //    var xDifOne = Math.Sign(xDif);
        //    var yDifOne = Math.Sign(yDif);

        //    //Check the direction we are furtherest from the destination on (or both if equal)
        //    // If it has LOS then we might

        //    //Check in the x direction
        //    if (xDifAbs >= yDifAbs)
        //    {

        //        if (closedSet.[new Vector2d(rawNode.gridX + xDifOne, rawNode.gridY)].HasLOS)
        //        {
        //            hasLos = true;
        //        }
        //    }
        //    //Check in the y direction
        //    if (yDifAbs >= xDifAbs)
        //    {

        //        if (losGrid[at.x][at.y + yDifOne])
        //        {
        //            hasLos = true;
        //        }
        //    }

        //    //If we are not a straight line vertically/horizontally to the exit
        //    if (yDifAbs > 0 && xDifAbs > 0)
        //    {
        //        //If the diagonal doesn't have LOS, we don't
        //        if (!losGrid[at.x + xDifOne][at.y + yDifOne])
        //        {
        //            hasLos = false;
        //        }
        //        else if (yDifAbs === xDifAbs)
        //        {
        //            //If we are an exact diagonal and either straight direction is a wall, we don't have LOS
        //            if (dijkstraGrid[at.x + xDifOne][at.y] === Number.MAX_VALUE || dijkstraGrid[at.x][at.y + yDifOne] === Number.MAX_VALUE)
        //            {
        //                hasLos = false;
        //            }
        //        }
        //    }
        //    //It's a definite now
        //    losGrid[at.x][at.y] = hasLos;

        //    //TODO: Could replace our distance with a direct distance?
        //    // Might not be worth it, would need to use a priority queue for the open list.
        //}

        private static int GetNodeDistance(GridNode node, int parentNodeDistance)
        {
            if (node.IsNotNull())
            {
                if (!node.Unpassable())
                {
                    if (!closedSet.Contains(node))
                    {
                        // node not in grid, should be the nodes on the outside edge
                        // assign greatest distance
                        return greatestDistance;
                    }

                    // only the end node should have 0 distance;
                    return node.FlowField.Distance > 0 ? node.FlowField.Distance : parentNodeDistance;
                }
                else if (node.gridIndex != flowFieldPath.EndNodeIndex)
                {
                    // node is a blocker, assign parent
                    return parentNodeDistance + 1;
                }
            }

            //node is null, assign max distance
            return int.MaxValue;
        }
    }
}