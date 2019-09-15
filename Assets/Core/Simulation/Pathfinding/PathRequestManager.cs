using System.Collections.Generic;
using System;
using RTSLockstep.Grid;

namespace RTSLockstep.Pathfinding
{
    public static class PathRequestManager
    {
        private static Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
        private static PathRequest currentPathRequest;

        private static bool isProcessingPath;

        public static void Reset()
        {
            FlowFieldPathFinder.Reset();
        }

        public static void RequestPath(GridNode currentNode, GridNode destinationNode, int gridSize, Action<Dictionary<Vector2d, FlowField>, bool> callback)
        {
            PathRequest newRequest = new PathRequest(currentNode, destinationNode, gridSize, callback);
            pathRequestQueue.Enqueue(newRequest);
            TryProcessNext();
        }

        private static void TryProcessNext()
        {
            if (!isProcessingPath && pathRequestQueue.Count > 0)
            {
                currentPathRequest = pathRequestQueue.Dequeue();
                isProcessingPath = true;
                FlowFieldPathFinder.FindPath(currentPathRequest.currentNode, currentPathRequest.destinationNode, currentPathRequest.gridSize);
            }
        }

        public static void FinishedProcessingPath(Dictionary<Vector2d, FlowField> path, bool success)
        {
            currentPathRequest.callback(path, success);
            isProcessingPath = false;
            TryProcessNext();
        }
    }
}