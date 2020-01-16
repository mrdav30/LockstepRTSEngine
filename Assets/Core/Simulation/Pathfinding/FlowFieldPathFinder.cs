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
        private static int _searchCount;

        private static FlowFieldPath _flowFieldPath;

        private static FastList<GridNode> _markedNodesBuffer = new FastList<GridNode>();
        private static FastList<GridNode> _rawMarkedNodes;
        //holding tank to check if node was checked, faster than searching list
        private static HashSet<GridNode> _closedSet = new HashSet<GridNode>();

        private static GridNode _rawNode;
        private static GridNode _neighbor;
        private static FastList<GridNode> _neighbors;

        private static GridNode _minDistanceNode;
        private static FlowField _flowField;

        private static bool _destinationIsReached;
        #endregion

        public static void Reset()
        {
            //Reset combine value so it doesn't overflow with multiple scenes
            _markedNodesBuffer.FastClear();
            _markedNodesBuffer.EnsureCapacity(GridManager.GridSize);
            _closedSet.Clear();
        }

        public static void FindPath(GridNode startNode, GridNode endNode, int unitHalfSize = 1)
        {
            _markedNodesBuffer.FastClear();
            if (GenerateDistanceField(startNode, endNode, unitHalfSize, _markedNodesBuffer))
            {
                // Distance vector field created, 
                GenerateVectorFlowField(_markedNodesBuffer, _flowFieldPath);
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

            _rawMarkedNodes = _markedNodes;
            _rawMarkedNodes.FastClear();

            _closedSet.Clear();



            if (!_flowFieldPath.CheckValid())
            {
                return false;
            }

            //flood fill out from the end point with a distance of 0

            if (_flowFieldPath.EndNode.Unwalkable
                && Pathfinder.StarCast(_flowFieldPath.EndNode.WorldPos, out GridNode closestNode, 10))
            {
                _flowFieldPath = new FlowFieldPath(_startNode, closestNode);
            }
            else
            {
                _flowFieldPath = new FlowFieldPath(_startNode, _endNode);
            }

            _rawMarkedNodes.Add(_flowFieldPath.EndNode);
            _closedSet.Add(_flowFieldPath.EndNode);

            _destinationIsReached = false;
            _searchCount = 0;
            //for each node we need to visit, starting with the pathStart
            while (_searchCount < GridManager.Grid.Length)
            {
                if (_searchCount >= _rawMarkedNodes.Count)
                {
                    break; 
                }

                _rawNode = _rawMarkedNodes[_searchCount];

                if (_rawNode.IsNotNull())
                {
                    _rawNode.FlowField.HasLOS = false;

                    if (_rawNode.GridIndex != _flowFieldPath.EndNodeIndex)
                    {
                        _rawNode.FlowField.HasLOS = Pathfinder.NeedsPath(_rawNode, _flowFieldPath.EndNode, _unitHalfSize) ? false : true;
                    }

                    // for each straight line neighbour of this node (no diagonals)
                    for (int i = 0; i < 4; i++)
                    {
                        _neighbor = _rawNode.NeighborNodes[i];

                        //We will only ever visit every node once as we are always visiting nodes in the most efficient order
                        if (_neighbor.IsNotNull() && !_closedSet.Contains(_neighbor))
                        {
                            // if neighbor is passable or is start node, add to marked nodes
                            if (!_neighbor.Unpassable() || _neighbor.GridIndex == _flowFieldPath.StartNodeIndex)
                            {
                                _neighbor.FlowField.Distance = _rawNode.FlowField.Distance + 1;

                                _rawMarkedNodes.Add(_neighbor);
                                _closedSet.Add(_neighbor);

                                if (_neighbor.GridIndex == _flowFieldPath.StartNodeIndex)
                                {
                                    //We found our way to the start node!
                                    _destinationIsReached = true;
                                }
                            }
                        }
                    }

                    // no need to capture entire grid if start point reached
                    // go a bit further past the start node
                    if (_destinationIsReached
                        && _rawNode.FlowField.Distance > (_flowFieldPath.StartNode.FlowField.Distance + 10))
                    {
                        //sw.Stop();
                        //UnityEngine.Debug.Log("Path found: " + sw.ElapsedMilliseconds + " ms");
                        break;
                    }
                }

                _searchCount++;
            }

            return _destinationIsReached;
        }

        private static void GenerateVectorFlowField(FastList<GridNode> _markedNodes, FlowFieldPath _flowFieldPath)
        {
            int length = _markedNodes.Count - 1;
            int greatestDistance = _markedNodes[length].FlowField.Distance;

            try
            {
                //for each grid square
                for (int i = 0; i < length; i++)
                {
                    _rawNode = _markedNodes[i];

                    if (_rawNode.GridIndex != _flowFieldPath.EndNodeIndex)
                    {
                        _neighbors = _rawNode.UnblockedNeighboursOf();

                        // exclude LOS from a node if it has blockers for neighbors
                        if (_neighbors.Count < 8)
                        {
                            _rawNode.FlowField.HasLOS = false;
                        }

                        //Go through all neighbours and find the one with the lowest distance
                        _minDistanceNode = null;
                        int minDist = 0;
                        for (int z = 0; z < _neighbors.Count; z++)
                        {
                            _neighbor = _neighbors[z];

                            // check if node is in closed set, otherwise return greatest distance
                            int nDistance = _closedSet.Contains(_neighbor) ? _neighbor.FlowField.Distance : greatestDistance;
                            int dist = nDistance - _rawNode.FlowField.Distance;

                            if (dist < minDist)
                            {
                                _minDistanceNode = _neighbor;
                                minDist = dist;
                            }
                        }

                        //If we found a valid neighbour, point in its direction
                        if (_minDistanceNode.IsNotNull())
                        {
                            // If nodes has line of sight to destination, point in that direction instead
                            _rawNode.FlowField.Direction = _rawNode.FlowField.HasLOS ? (FlowFieldPathFinder._flowFieldPath.EndNode.GridPos - _rawNode.GridPos)
                                : (_minDistanceNode.GridPos - _rawNode.GridPos);
                        }
                        else
                        {
                            // no good direction
                            _rawNode.FlowField.Direction = Vector2d.zero;
                        }
                    }
                    else
                    {
                        //end node shouldn't point anywhere
                        _rawNode.FlowField.Direction = Vector2d.zero;
                    }

                    _rawNode.FlowField.Direction.Normalize();

                    _flowField = new FlowField(_rawNode.WorldPos, _rawNode.FlowField.Distance, _rawNode.FlowField.Direction, _rawNode.FlowField.HasLOS);

                    _flowFieldPath.OutputVectorPath.Add(_rawNode.GridPos, _flowField);
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