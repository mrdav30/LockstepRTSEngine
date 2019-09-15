using RTSLockstep.Grid;
using System.Collections.Generic;

namespace RTSLockstep.Pathfinding
{
    public class FlowFieldPath
    {
        public GridNode StartNode;
        public GridNode EndNode;

        public uint StartNodeIndex;
        public uint EndNodeIndex;

        public Dictionary<Vector2d, FlowField> OutputVectorPath;

        public FlowFieldPath(GridNode _startNode, GridNode _endNode)
        {
            StartNode = _startNode;
            EndNode = _endNode;
            EndNode.FlowField.Distance = 0;
            EndNode.FlowField.HasLOS = true;

            StartNodeIndex = StartNode.gridIndex;
            EndNodeIndex = EndNode.gridIndex;

            OutputVectorPath = new Dictionary<Vector2d, FlowField>();
        }

        public bool CheckValid()
        {
            if (EndNode.Unwalkable || StartNode.Unwalkable)
            {
                return false;
            }

            if (ReferenceEquals(StartNode, EndNode))
            {
                // we're already at the destination!
                return false;
            }

            return true;
        }
    }
}