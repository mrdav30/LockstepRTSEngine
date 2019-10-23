using FastCollections;
using RTSLockstep.Grid;
using RTSLockstep.Pathfinding;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    public class MovementGroup
    {
        public Vector2d Destination;
        private Vector2d groupPosition;

        private Vector2d farthestPosition;

        private GridNode groupCurrentNode;
        private GridNode groupDestinationNode;

        // key = postion, value = direction
        public Dictionary<Vector2d, FlowField> GroupFlowFields = new Dictionary<Vector2d, FlowField>();

        public int indexID { get; set; }

        public MovementType movementType { get; private set; }

        private const int MinGroupSize = 2;

        public FastList<Move> Movers { get; private set; }
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
            if (Movers.IsNotNull())
            {
                if (Movers.Count > 0)
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
                        for(int i = 0; i < Movers.Count; i++)
                        {
                            mover = Movers[i];
                            mover.IsAvoidingLeft = false;
                        }

                    }
                }

                if (Movers.Count == 0)
                {
                    Deactivate();
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
            mover.MyMovementGroupID = indexID;

            Movers.Add(mover);
        }

        public void Remove(Move mover)
        {
            if (mover.MyMovementGroup.IsNotNull() && mover.MyMovementGroupID == indexID)
            {
                Movers.Remove(mover);
            }
        }

        public bool CalculateAndExecuteBehaviors()
        {
            if (Movers.Count >= MinGroupSize)
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

                // Generate a flow field for the entire movement group
                GetGroupMovementPath();
            }
            else
            {
                // try to find the individual agent path
                ExecuteIndividualMove();
            }

            // at this point we know what to do
            return true;
        }

        public void GetGroupMovementPath()
        {
            //pass the largest unit or just average?
            int groupGridSize = _averageCollisionSize.CeilToInt();

            if (Pathfinder.GetStartNode(farthestPosition, out groupCurrentNode)
                || Pathfinder.GetClosestViableNode(farthestPosition, farthestPosition, groupGridSize, out groupCurrentNode))
            {
                // check to make sure the group is moving to a good area
                if (Pathfinder.GetEndNode(groupPosition, Destination, out groupDestinationNode)
                    || Pathfinder.GetClosestViableNode(farthestPosition, Destination, groupGridSize, out groupDestinationNode))
                {
                    Destination = groupDestinationNode.WorldPos;
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
        }

        public void Deactivate()
        {
            Move mover;
            for (int i = 0; i < Movers.Count; i++)
            {
                mover = Movers[i];
                mover.MyMovementGroup = null;
                mover.MyMovementGroupID = -1;
                mover.IsGroupMoving = false;
            }
            Movers.FastClear();
            GroupFlowFields.Clear();
            MovementGroupHelper.Pool(this);
            _calculatedBehaviors = false;
            indexID = -1;
        }

        private void ExecuteIndividualMove()
        {
            movementType = MovementType.Individual;
            for (int i = 0; i < Movers.Count; i++)
            {
                Move mover = Movers[i];
                mover.IsGroupMoving = false;
                mover.StopMultiplier = Move.DirectStop;
                mover.OnGroupProcessed(Destination);
            }
        }

        private void ExecuteGroupMove()
        {
            movementType = MovementType.Group;
            groupDirection = Destination - groupPosition;

            for (int i = 0; i < Movers.Count; i++)
            {
                Move mover = Movers[i];
                mover.IsGroupMoving = true;
                mover.StopMultiplier = Move.GroupStop;
                mover.OnGroupProcessed(mover.Position + groupDirection);
            }
        }

        private void ExecuteGroupIndividualMove()
        {
            Debug.Log("individual group moving");
            movementType = MovementType.GroupIndividual;

            for (int i = 0; i < Movers.Count; i++)
            {
                Move mover = Movers[i];
                mover.IsGroupMoving = true;
                mover.StopMultiplier = Move.GroupDirectStop;
                mover.OnGroupProcessed(Destination);
            }
        }
    }
}
