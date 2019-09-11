//=======================================================================
// Copyright (c) 2015 John Pan
// Distributed under the MIT License.
// (See accompanying file LICENSE or copy at
// http://opensource.org/licenses/MIT)
// @modified 2019 David Oravsky
//=======================================================================

using FastCollections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace RTSLockstep.Pathfinding
{
    public static class FlowFieldFinder
    {
        #region Path Variables
        private static GridNode neighbor;
        private static int SearchCount;

        private static uint StartNodeIndex;
        private static uint EndNodeIndex;
        #endregion

        #region wrapper variables
        private static int length;
        #endregion

        #region method sharing variables
        private static GridNode startNode;
        private static GridNode endNode;

        private static int greatestDistance;

        private static FastList<GridNode> markedNodesBuffer = new FastList<GridNode>();
        private static FastList<GridNode> rawMarkedNodes;
        //holding tank to check if node was checked, faster than searching list
        //  private static Dictionary<Vector2d, bool> dijkstraGrid = new Dictionary<Vector2d, bool>();
        private static HashSet<GridNode> closedSet = new HashSet<GridNode>();

        private static GridNode rawNode;
        #endregion

        private static bool destinationIsReached;

        public static bool AllowUnwalkableEndNode { get; set; }

        //Reset pathfinding so it doesn't overflow with multiple scenes
        public static void Reset()
        {
            markedNodesBuffer.FastClear();
            markedNodesBuffer.EnsureCapacity(GridManager.GridSize);
            closedSet.Clear();
        }

        public static void FindPath(GridNode startNode, GridNode endNode, int unitHalfSize = 1)
        {
            UnityEngine.Debug.Log("finding path");
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

            startNode = _startNode;
            endNode = _endNode;
            endNode.FlowField.Distance = 0;
            StartNodeIndex = startNode.gridIndex;
            EndNodeIndex = endNode.gridIndex;

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

            //flood fill out from the end point with a distance of 0
            rawMarkedNodes.Add(endNode);
            closedSet.Add(endNode);

            //Prepare Unpassable check optimizations
            GridNode.PrepareUnpassableCheck(_unitHalfSize);

            SearchCount = 0;
            //for each node we need to visit, starting with the pathStart
            while (SearchCount <= GridManager.Grid.Length)
            {
                rawNode = rawMarkedNodes[SearchCount];

                if (rawNode.IsNull())
                {
                    continue;
                }

                //for each neighbour of this node 
                for (int i = 0; i < 8; i++)
                {
                    neighbor = rawNode.NeighborNodes[i];

                    //We will only ever visit every node once as we are always visiting nodes in the most efficient order
                    if (!closedSet.Contains(neighbor))
                    {
                        if (neighbor.IsNotNull() && !neighbor.Unpassable())
                        {
                            neighbor.FlowField.Distance = rawNode.FlowField.Distance + 1;
                            rawMarkedNodes.Add(neighbor);

                            closedSet.Add(neighbor);
                        }
                    }
                }

                if (rawNode.gridIndex == StartNodeIndex)
                {
                    sw.Stop();
                    UnityEngine.Debug.Log("Path found: " + sw.ElapsedMilliseconds + " ms");
                    //We found our way to the start node!
                    destinationIsReached = true;
                     return true;
                   // break;
                }

                SearchCount++;
            }

            UnityEngine.Debug.Log("destinationIsReached" + destinationIsReached);
            return destinationIsReached;
        }

        public static void GenerateVectorFlowField(FastList<GridNode> _markedNodes)
        {
            Dictionary<Vector2d, FlowField> _outputVectorPath = new Dictionary<Vector2d, FlowField>();

            length = _markedNodes.Count - 1;
            greatestDistance = _markedNodes[length].FlowField.Distance;

            //for each grid square
            for (int i = 0; i < length; i++)
            {
                GridNode node = _markedNodes[i];
                int nodeDistance = node.FlowField.Distance;

                if (node.gridIndex != EndNodeIndex)
                {
                    GridNode[] neighbors = node.NeighborNodes;

                    int left, right, up, down;

                    // The 4 weights we'll interpolate (only straight line neighbours, not diagonals)
                    // see http://en.wikipedia.org/wiki/File:Bilininterp.png for the coordinates
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

                _outputVectorPath.Add(node.WorldPos, new FlowField(node.FlowField.Distance, node.FlowField.Direction));
            }

            PathRequestManager.FinishedProcessingPath(_outputVectorPath, true);
        }

        private static int GetNodeDistance(GridNode node, int parentNodeDistance)
        {
            if (node.IsNotNull())
            {
                if (!node.Unpassable())
                {
                    if (!closedSet.Contains(node))
                    {
                        // node not in grid, assign greatest distance
                        return greatestDistance;
                    }

                    // only the end node should have 0 distance;
                    return node.FlowField.Distance > 0 ? node.FlowField.Distance : parentNodeDistance;
                }
                else if (node.gridIndex != EndNodeIndex)
                {
                    // node is next to a block, assign parent
                    return parentNodeDistance;
                }
            }

            //node is null, assign max distance
            return int.MaxValue;
        }
    }
}