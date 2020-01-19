using System.Collections.Generic;

using RTSLockstep.Simulation.Grid;
using RTSLockstep.Simulation.LSMath;

namespace RTSLockstep.Simulation.Pathfinding
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

            StartNodeIndex = StartNode.GridIndex;
            EndNodeIndex = EndNode.GridIndex;

            OutputVectorPath = new Dictionary<Vector2d, FlowField>();
        }

        public bool CheckValid()
        {
            if (StartNode.Unwalkable)
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