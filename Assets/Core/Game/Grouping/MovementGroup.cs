using FastCollections;
using RTSLockstep.Grid;
using RTSLockstep.Pathfinding;
using System.Collections.Generic;

namespace RTSLockstep
{
    public class MovementGroup
    {
        private Vector2d Destination;
        private Vector2d groupPosition;

        private Vector2d farthestPosition;

        private GridNode groupCurrentNode;
        private GridNode groupDestinationNode;

        // key = postion, value = direction
        public Dictionary<Vector2d, FlowField> GroupFlowFields = new Dictionary<Vector2d, FlowField>();

        public int IndexID { get; set; }

        private const int minGroupSize = 2;
        private int groupGridSize;

        private FastList<Move> Movers;
        private Vector2d groupDirection;

        private long _radius;
        private long _averageCollisionSize;
        private bool _calculatedBehaviors;

        public void Initialize(Command com)
        {
            // we might have to create a movement group without an initial destination, i.e. construction
            if (com.ContainsData<Vector2d>())
            {
                Destination = com.GetData<Vector2d>();
            }
            _calculatedBehaviors = false;
            Selection selection = AgentController.InstanceManagers[com.ControllerID].GetSelection(com);
            Movers = new FastList<Move>(selection.selectedAgentLocalIDs.Count);
        }

        public void LocalSimulate()
        {

        }

        public void LateSimulate()
        {
            if (Movers.IsNull() || Movers.Count == 0)
            {
                Deactivate();
            }
            else if (Movers.Count > 0)
            {
                if (!_calculatedBehaviors)
                {
                    _calculatedBehaviors = CalculateAndExecuteBehaviors();
                }
                else
                {
                    // reset movement and avoidance every game tick
                    // should reset before move ability
                    Move mover;
                    for (int i = 0; i < Movers.Count; i++)
                    {
                        mover = Movers[i];
                        mover.IsAvoidingLeft = false;
                    }

                }
            }
        }

        public void Add(Move mover)
        {
            if (mover.MyMovementGroup.IsNotNull())
            {
                mover.MyMovementGroup.Movers.Remove(mover);
            }
            mover.MyMovementGroup = this;
            mover.MyMovementGroupID = IndexID;

            Movers.Add(mover);
        }

        public void Remove(Move mover)
        {
            if (mover.MyMovementGroup.IsNotNull() && mover.MyMovementGroupID == IndexID)
            {
                Movers.Remove(mover);
                mover.MyMovementGroup = null;
                mover.MyMovementGroupID = -1;
                mover.MyMovementType = MovementType.Individual;
            }
        }

        public bool CalculateAndExecuteBehaviors()
        {
            if (Movers.Count >= minGroupSize)
            {
                Move mover;

                _averageCollisionSize = 0;
                _radius = 0;
                groupPosition = Vector2d.zero;
                for (int i = 0; i < Movers.Count; i++)
                {
                    mover = Movers[i];
                    groupPosition += mover.Position;
                    _averageCollisionSize += mover.CollisionSize;
                }

                groupPosition /= Movers.Count;
                _averageCollisionSize = (_averageCollisionSize / Movers.Count) + FixedMath.Create(0.5f);

                //pass the largest unit or just average?
                groupGridSize = _averageCollisionSize.CeilToInt();

                long biggestSqrDistance = 0;
                long farthestSqrDistance = 0;
                for (int i = 0; i < Movers.Count; i++)
                {
                    mover = Movers[i];

                    long currentSqrDistance = mover.Position.SqrDistance(groupPosition.x, groupPosition.y);
                    if (currentSqrDistance > biggestSqrDistance)
                    {
                        long currentDistance = FixedMath.Sqrt(currentSqrDistance);

                        biggestSqrDistance = currentSqrDistance;
                        _radius = currentDistance;
                    }

                    long destinationSqrDistance = mover.Position.SqrDistance(Destination.x, Destination.y);
                    if (destinationSqrDistance > farthestSqrDistance)
                    {
                        farthestSqrDistance = destinationSqrDistance;
                        farthestPosition = mover.Position;
                    }
                }

                // check to make sure the group is moving to a good area
                if (groupGridSize <= 1 && Pathfinder.GetEndNode(farthestPosition, Destination, out groupDestinationNode)
                    || Pathfinder.GetClosestViableNode(farthestPosition, Destination, groupGridSize, out groupDestinationNode))
                {
                    Destination = groupDestinationNode.WorldPos;
                    // Generate a flow field for the entire movement group
                    ValidateGroupMovementPath();
                }
                else
                {
                    // can't get there as a group, try to find the individual agent path
                    ExecuteIndividualMove();
                }
            }
            else
            {
                // We don't meet the minimum for a group, try to find the individual agent path
                ExecuteIndividualMove();
            }

            // at this point we know what to do
            return true;
        }

        public void ValidateGroupMovementPath()
        {
            if (groupGridSize <= 1 && Pathfinder.GetStartNode(farthestPosition, out groupCurrentNode)
                || Pathfinder.GetClosestViableNode(farthestPosition, farthestPosition, groupGridSize, out groupCurrentNode))
            {
                PathRequestManager.RequestPath(groupCurrentNode, groupDestinationNode, groupGridSize, (_flowField, success) =>
                {
                    if (success)
                    {
                        GroupFlowFields.Clear();
                        GroupFlowFields = _flowField;

                        if (_radius == 0)
                        {
                            // we must not have a group then...
                            ExecuteGroupIndividualMove();
                            return;
                        }

                        long expectedSize = _averageCollisionSize.Mul(_averageCollisionSize).Mul(FixedMath.One * 2).Mul(Movers.Count);
                        long groupSize = _radius.Mul(_radius);

                        if (groupSize > expectedSize || groupPosition.FastDistance(Destination.x, Destination.y) < (_radius.Mul(_radius)))
                        {
                            // group members are spread to far out
                            ExecuteGroupIndividualMove();
                            return;
                        }
                        else
                        {
                            // everyone is huddled together for group movement
                            ExecuteGroupMove();
                            return;
                        }
                    }
                    else
                    {
                        // unable to find path, try having each agent find their own path
                        ExecuteIndividualMove();
                        return;
                    }
                });
            }
        }

        public void Deactivate()
        {
            Move mover;
            for (int i = 0; i < Movers.Count; i++)
            {
                mover = Movers[i];
                mover.MyMovementGroup = null;
                mover.MyMovementGroupID = -1;
                mover.MyMovementType = MovementType.Individual;
            }
            Movers.FastClear();
            GroupFlowFields.Clear();
            MovementGroupHelper.Pool(this);
            _calculatedBehaviors = false;
            IndexID = -1;
        }

        private void ExecuteIndividualMove()
        {
            for (int i = 0; i < Movers.Count; i++)
            {
                Move mover = Movers[i];
                mover.MyMovementType = MovementType.Individual;
                mover.StopMultiplier = Move.DirectStop;
                mover.MoveGroupProcessed(Destination);
            }
        }

        private void ExecuteGroupMove()
        {
            groupDirection = Destination - groupPosition;

            for (int i = 0; i < Movers.Count; i++)
            {
                Move mover = Movers[i];
                mover.MyMovementType = MovementType.Group;
                mover.StopMultiplier = Move.GroupStop;
                mover.MoveGroupProcessed(mover.Position + groupDirection);
            }
        }

        private void ExecuteGroupIndividualMove()
        {
            for (int i = 0; i < Movers.Count; i++)
            {
                Move mover = Movers[i];
                mover.MyMovementType = MovementType.GroupIndividual;
                mover.StopMultiplier = Move.GroupDirectStop;
                mover.MoveGroupProcessed(Destination);
            }
        }
    }
}
