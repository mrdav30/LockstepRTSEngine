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
    public static class VectorFlowFieldFinder
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
        private static Dictionary<Vector2d, bool> dijkstraGrid = new Dictionary<Vector2d, bool>();

        private static GridNode rawNode;
        #endregion

        private static bool destinationIsReached;

        public static bool AllowUnwalkableEndNode { get; set; }

        //Reset pathfinding so it doesn't overflow with multiple scenes
        public static void Reset()
        {
            markedNodesBuffer.FastClear();
            markedNodesBuffer.EnsureCapacity(GridManager.GridSize);
            dijkstraGrid.Clear();
        }

        public static bool FindPath(GridNode startNode, GridNode endNode, Dictionary<Vector2d, FlowField> outputVectorPath, int unitHalfSize = 1)
        {
            markedNodesBuffer.FastClear();
            if (GenerateDistanceField(startNode, endNode, markedNodesBuffer, unitHalfSize))
            {
                GenerateVectorFlowField(markedNodesBuffer, outputVectorPath);
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
        public static bool GenerateDistanceField(GridNode _startNode, GridNode _endNode, FastList<GridNode> _markedNodes, int _unitHalfSize)
        {
            rawMarkedNodes = _markedNodes;
            rawMarkedNodes.FastClear();

            dijkstraGrid.Clear();

            startNode = _startNode;
            endNode = _endNode;
            endNode.Distance = 0;
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

            //Prepare Unpassable check optimizations
            GridNode.PrepareUnpassableCheck(_unitHalfSize);

            //flood fill out from the end point with a distance of 0
            rawMarkedNodes.Add(endNode);
            dijkstraGrid.Add(endNode.WorldPos, true);

            SearchCount = 0;
            //for each node we need to visit, starting with the pathStart
            while (rawMarkedNodes.Count <= GridManager.Grid.Length)
            {
                rawNode = rawMarkedNodes[SearchCount];

                if (rawNode.IsNull())
                {
                    continue;
                }

                //for each neighbour of this node (only straight line neighbours, not diagonals)
                for (int i = 0; i < 8; i++)
                {
                    neighbor = rawNode.NeighborNodes[i];

                    //We will only ever visit every node once as we are always visiting nodes in the most efficient order
                    if (neighbor.IsNotNull() && !neighbor.Unpassable())
                    {
                        if (!dijkstraGrid.ContainsKey(neighbor.WorldPos))
                        {
                            neighbor.Distance = rawNode.Distance + 1;
                            rawMarkedNodes.Add(neighbor);
                            dijkstraGrid.Add(neighbor.WorldPos, true);
                        }
                    }
                }

                if (rawNode.gridIndex == StartNodeIndex)
                {
                    //We found our way to the start node!
                    destinationIsReached = true;
                    return true;
                }

                SearchCount++;
            }

            return destinationIsReached;
        }

        public static void GenerateVectorFlowField(FastList<GridNode> _markedNodes, Dictionary<Vector2d, FlowField> _outputVectorPath)
        {
            _outputVectorPath.Clear();
            length = _markedNodes.Count - 1;

            greatestDistance = _markedNodes[length].Distance;

            //for each grid square
            for (int i = 0; i < length; i++)
            {
                GridNode node = _markedNodes[i];

                GridNode[] neighbors = node.NeighborNodes;

                int left, right, up, down;

                // The 4 weights we'll interpolate
                // see http://en.wikipedia.org/wiki/File:Bilininterp.png for the coordinates
                left = GetNodeDistance(neighbors[0], node.Distance); // west
                down = GetNodeDistance(neighbors[1], node.Distance); // south
                up = GetNodeDistance(neighbors[2], node.Distance); // east
                right = GetNodeDistance(neighbors[3], node.Distance); // north

                //Do the x interpolations
                int x = left - right;
                //Do the y interpolation
                int y = down - up;

                //point in the direction of its neighbor
                node.Direction = new Vector2d(x, y);
                node.Direction.Normalize();

                _outputVectorPath.Add(node.WorldPos, new FlowField(node.Distance, node.Direction));
            }
        }

        private static int GetNodeDistance(GridNode node, int parentNodeDistance)
        {

            if (node.IsNotNull() && !node.Unpassable())
            {
                if (!dijkstraGrid.ContainsKey(node.WorldPos))
                {
                    // node not in grid, assign greatest distance
                    return greatestDistance;
                }

                return node.Distance;
            }


            // node is either null or next to a block, assign parent
            return parentNodeDistance;
        }
    }
}