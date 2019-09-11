using System;
using System.Collections.Generic;
namespace RTSLockstep.Pathfinding
{
    public struct PathRequest
    {
        public GridNode currentNode;
        public GridNode destinationNode;
        public int gridSize;
        public Action<Dictionary<Vector2d, FlowField>, bool> callback;

        public PathRequest(GridNode _currentNode, GridNode _destinationNode, int _gridSize, Action<Dictionary<Vector2d, FlowField>, bool> _callback)
        {
            currentNode = _currentNode;
            destinationNode = _destinationNode;
            gridSize = _gridSize;
            callback = _callback;
        }
    }
}
