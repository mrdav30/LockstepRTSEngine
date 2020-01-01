using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Abilities.Essential;
using RTSLockstep.Agents.AgentController;
using RTSLockstep.Simulation.Grid;
using RTSLockstep.Simulation.Pathfinding;
using RTSLockstep.Player.Commands;
using RTSLockstep.Player.Utility;
using RTSLockstep.Utility;
using RTSLockstep.LSResources;
using System.Collections.Generic;
using RTSLockstep.Simulation.LSMath;

namespace RTSLockstep.Grouping
{
    public class MovementGroup
    {
        private Vector2d _destination;
        private Vector2d _groupPosition;

        private Vector2d _farthestPosition;

        private GridNode _groupCurrentNode;
        private GridNode _groupDestinationNode;

        // key = postion, value = direction
        public Dictionary<Vector2d, FlowField> GroupFlowFields = new Dictionary<Vector2d, FlowField>();

        public int IndexID { get; set; }

        public bool AllowUnwalkableEndNode = false;

        private const int _minGroupSize = 2;
        private int _groupGridSize;

        private FastList<Move> _movers;
        private Vector2d _groupDirection;

        private long _radius;
        private long _averageCollisionSize;
        private bool _calculatedBehaviors;

        public void Initialize(Command com)
        {
            _destination = com.GetData<Vector2d>();
            _calculatedBehaviors = false;
            Selection selection = GlobalAgentController.InstanceManagers[com.ControllerID].GetSelection(com);
            _movers = new FastList<Move>(selection.selectedAgentLocalIDs.Count);
        }

        public void LocalSimulate()
        {

        }

        public void LateSimulate()
        {
            if (_movers.IsNull() || _movers.Count == 0)
            {
                Deactivate();
            }
            else if (_movers.Count > 0)
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
                    for (int i = 0; i < _movers.Count; i++)
                    {
                        mover = _movers[i];
                        mover.IsAvoidingLeft = false;
                    }
                }
            }
        }

        public void Add(Move mover)
        {
            if (mover.MyMovementGroup.IsNotNull())
            {
                mover.MyMovementGroup._movers.Remove(mover);
            }
            mover.MyMovementGroup = this;
            mover.MyMovementGroupID = IndexID;

            _movers.Add(mover);
        }

        public void Remove(Move mover)
        {
            if (mover.MyMovementGroup.IsNotNull() && mover.MyMovementGroupID == IndexID)
            {
                _movers.Remove(mover);
                mover.MyMovementGroup = null;
                mover.MyMovementGroupID = -1;
                mover.MyMovementType = MovementType.Individual;
            }
        }

        public bool CalculateAndExecuteBehaviors()
        {
            if (_movers.Count >= _minGroupSize)
            {
                Move mover;

                _averageCollisionSize = 0;
                _radius = 0;
                _groupPosition = Vector2d.zero;
                for (int i = 0; i < _movers.Count; i++)
                {
                    mover = _movers[i];
                    _groupPosition += mover.Position;
                    _averageCollisionSize += mover.CollisionSize;
                }

                _groupPosition /= _movers.Count;
                _averageCollisionSize = (_averageCollisionSize / _movers.Count) + FixedMath.Create(0.5f);

                //pass the largest unit or just average?
                _groupGridSize = _averageCollisionSize.CeilToInt();

                long biggestSqrDistance = 0;
                long farthestSqrDistance = 0;
                for (int i = 0; i < _movers.Count; i++)
                {
                    mover = _movers[i];

                    long currentSqrDistance = mover.Position.SqrDistance(_groupPosition.x, _groupPosition.y);
                    if (currentSqrDistance > biggestSqrDistance)
                    {
                        long currentDistance = FixedMath.Sqrt(currentSqrDistance);

                        biggestSqrDistance = currentSqrDistance;
                        _radius = currentDistance;
                    }

                    long destinationSqrDistance = mover.Position.SqrDistance(_destination.x, _destination.y);
                    if (destinationSqrDistance > farthestSqrDistance)
                    {
                        farthestSqrDistance = destinationSqrDistance;
                        _farthestPosition = mover.Position;
                    }
                }

                // check to make sure the group is moving to a good area
                if (_groupGridSize <= 1 && Pathfinder.GetEndNode(_farthestPosition, _destination, out _groupDestinationNode, AllowUnwalkableEndNode)
                    || Pathfinder.GetClosestViableNode(_farthestPosition, _destination, _groupGridSize, out _groupDestinationNode, AllowUnwalkableEndNode))
                {
                    _destination = _groupDestinationNode.WorldPos;
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
            if (_groupGridSize <= 1 && Pathfinder.GetStartNode(_farthestPosition, out _groupCurrentNode)
                || Pathfinder.GetClosestViableNode(_farthestPosition, _farthestPosition, _groupGridSize, out _groupCurrentNode))
            {
                PathRequestManager.RequestPath(_groupCurrentNode, _groupDestinationNode, _groupGridSize, (_flowField, success) =>
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

                        long expectedSize = _averageCollisionSize.Mul(_averageCollisionSize).Mul(FixedMath.One * 2).Mul(_movers.Count);
                        long groupSize = _radius.Mul(_radius);

                        if (groupSize > expectedSize || _groupPosition.FastDistance(_destination.x, _destination.y) < (_radius.Mul(_radius)))
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
            for (int i = 0; i < _movers.Count; i++)
            {
                mover = _movers[i];
                mover.MyMovementGroup = null;
                mover.MyMovementGroupID = -1;
                mover.MyMovementType = MovementType.Individual;
            }
            _movers.FastClear();
            GroupFlowFields.Clear();
            MovementGroupHelper.Pool(this);
            _calculatedBehaviors = false;
            IndexID = -1;
        }

        private void ExecuteIndividualMove()
        {
            for (int i = 0; i < _movers.Count; i++)
            {
                Move mover = _movers[i];
                mover.MyMovementType = MovementType.Individual;
                mover.StopMultiplier = Move.DirectStop;
                mover.MoveGroupProcessed(_destination);
            }
        }

        private void ExecuteGroupMove()
        {
            _groupDirection = _destination - _groupPosition;

            for (int i = 0; i < _movers.Count; i++)
            {
                Move mover = _movers[i];
                mover.MyMovementType = MovementType.Group;
                mover.StopMultiplier = Move.GroupStop;
                mover.MoveGroupProcessed(mover.Position + _groupDirection);
            }
        }

        private void ExecuteGroupIndividualMove()
        {
            for (int i = 0; i < _movers.Count; i++)
            {
                Move mover = _movers[i];
                mover.MyMovementType = MovementType.GroupIndividual;
                mover.StopMultiplier = Move.GroupDirectStop;
                mover.MoveGroupProcessed(_destination);
            }
        }
    }
}
