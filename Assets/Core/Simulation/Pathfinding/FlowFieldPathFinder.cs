//=======================================================================
// Copyright (c) 2015 John Pan
// Distributed under the MIT License.
// (See accompanying file LICENSE or copy at
// http://opensource.org/licenses/MIT)
// @modified 2019 David Oravsky
//=======================================================================

using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Simulation.Grid;
using System;
using System.Collections.Generic;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Utility;

namespace RTSLockstep.Simulation.Pathfinding
{
    public static class FlowFieldPathFinder
    {
        #region method sharing variables
        private static int SearchCount;

        private static FlowFieldPath flowFieldPath;

        private static FastList<GridNode> markedNodesBuffer = new FastList<GridNode>();
        private static FastList<GridNode> rawMarkedNodes;
        //holding tank to check if node was checked, faster than searching list
        private static HashSet<GridNode> closedSet = new HashSet<GridNode>();

        private static GridNode rawNode;
        private static GridNode neighbor;
        private static FastList<GridNode> neighbors;

        private static GridNode minDistanceNode;
        private static FlowField flowField;

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
                GenerateVectorFlowField(markedNodesBuffer, flowFieldPath, unitHalfSize);
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
            //Stopwatch sw = new Stopwatch();
            //sw.Start();

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

            destinationIsReached = false;
            SearchCount = 0;
            //for each node we need to visit, starting with the pathStart
            while (SearchCount < GridManager.Grid.Length)
            {
                if (SearchCount >= rawMarkedNodes.Count)
                {
                    break;
                }

                rawNode = rawMarkedNodes[SearchCount];

                if (rawNode.IsNotNull())
                {
                    rawNode.FlowField.HasLOS = false;

                    if (rawNode.gridIndex != flowFieldPath.EndNodeIndex)
                    {
                        rawNode.FlowField.HasLOS = Pathfinder.NeedsPath(rawNode, flowFieldPath.EndNode, _unitHalfSize) ? false : true;
                    }

                    // for each straight line neighbour of this node (no diagonals)
                    for (int i = 0; i < 4; i++)
                    {
                        neighbor = rawNode.NeighborNodes[i];

                        //We will only ever visit every node once as we are always visiting nodes in the most efficient order
                        if (neighbor.IsNotNull() && !closedSet.Contains(neighbor))
                        {
                            // if neighbor is passable or is start node, add to marked nodes
                            if (!neighbor.Unpassable() || neighbor.gridIndex == flowFieldPath.StartNodeIndex)
                            {
                                neighbor.FlowField.Distance = rawNode.FlowField.Distance + 1;

                                rawMarkedNodes.Add(neighbor);
                                closedSet.Add(neighbor);

                                if (neighbor.gridIndex == flowFieldPath.StartNodeIndex)
                                {
                                    //We found our way to the start node!
                                    destinationIsReached = true;
                                }
                            }
                        }
                    }

                    // no need to capture entire grid if start point reached
                    // go a bit further past the start node
                    if (destinationIsReached
                        && rawNode.FlowField.Distance > (flowFieldPath.StartNode.FlowField.Distance + 10))
                    {
                        //sw.Stop();
                        //UnityEngine.Debug.Log("Path found: " + sw.ElapsedMilliseconds + " ms");
                        break;
                    }
                }

                SearchCount++;
            }

            return destinationIsReached;
        }

        public static void GenerateVectorFlowField(FastList<GridNode> _markedNodes, FlowFieldPath _flowFieldPath, int _unitHalfSize)
        {
            int length = _markedNodes.Count - 1;
            int greatestDistance = _markedNodes[length].FlowField.Distance;

            try
            {
                //for each grid square
                for (int i = 0; i < length; i++)
                {
                    rawNode = _markedNodes[i];

                    if (rawNode.gridIndex != _flowFieldPath.EndNodeIndex)
                    {
                        neighbors = rawNode.UnblockedNeighboursOf();

                        // exclude LOS from a node if it has blockers for neighbors
                        if (neighbors.Count < 8)
                        {
                            rawNode.FlowField.HasLOS = false;
                        }

                        //Go through all neighbours and find the one with the lowest distance
                        minDistanceNode = null;
                        int minDist = 0;
                        for (int z = 0; z < neighbors.Count; z++)
                        {
                            neighbor = neighbors[z];

                            // check if node is in closed set, otherwise return greatest distance
                            int nDistance = closedSet.Contains(neighbor) ? neighbor.FlowField.Distance : greatestDistance;
                            int dist = nDistance - rawNode.FlowField.Distance;

                            if (dist < minDist)
                            {
                                minDistanceNode = neighbor;
                                minDist = dist;
                            }
                        }

                        //If we found a valid neighbour, point in its direction
                        if (minDistanceNode.IsNotNull())
                        {
                            // If nodes has line of sight to destination, point in that direction instead
                            rawNode.FlowField.Direction = rawNode.FlowField.HasLOS ? (flowFieldPath.EndNode.GridPos - rawNode.GridPos)
                                : (minDistanceNode.GridPos - rawNode.GridPos);
                        }
                        else
                        {
                            // no good direction
                            rawNode.FlowField.Direction = Vector2d.zero;
                        }
                    }
                    else
                    {
                        //end node shouldn't point anywhere
                        rawNode.FlowField.Direction = Vector2d.zero;
                    }

                    rawNode.FlowField.Direction.Normalize();

                    flowField = new FlowField(rawNode.WorldPos, rawNode.FlowField.Distance, rawNode.FlowField.Direction, rawNode.FlowField.HasLOS);

                    _flowFieldPath.OutputVectorPath.Add(rawNode.GridPos, flowField);
                }

                PathRequestManager.FinishedProcessingPath(_flowFieldPath.OutputVectorPath, true);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("couldn't create vector path: " + e.Message);
                PathRequestManager.FinishedProcessingPath(null, false);
            }
        }
    }
}