using FastCollections;
using RTSLockstep.Grid;
using RTSLockstep.Pathfinding;
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

        private FastList<Move> movers;
        private Vector2d groupDirection;

        private long radius;
        private long averageCollisionSize;
        private bool calculatedBehaviors;

        public void Initialize(Command com)
        {
            Destination = com.GetData<Vector2d>(); ;
            calculatedBehaviors = false;
            Selection selection = AgentController.InstanceManagers[com.ControllerID].GetSelection(com);
            movers = new FastList<Move>(selection.selectedAgentLocalIDs.Count);
        }

        public void LocalSimulate()
        {

        }

        public void LateSimulate()
        {
            if (movers.Count > 0 && !calculatedBehaviors)
            {
                calculatedBehaviors = CalculateAndExecuteBehaviors();
            }

            if(movers.Count == 0)
            {
                Deactivate();
            }
        }

        public void Add(Move mover)
        {
            if (mover.MyMovementGroup.IsNotNull())
            {
                mover.MyMovementGroup.movers.Remove(mover);
            }
            mover.MyMovementGroup = this;
            mover.MyMovementGroupID = indexID;

            movers.Add(mover);
        }

        public void Remove(Move mover)
        {
            if (mover.MyMovementGroup.IsNotNull() && mover.MyMovementGroupID == indexID)
            {
                movers.Remove(mover);
            }
        }

        public bool CalculateAndExecuteBehaviors()
        {
            if (movers.Count >= MinGroupSize)
            {
                // check to make sure the group is moving to a good area
                if (Pathfinder.GetEndNode(groupPosition, Destination, out groupDestinationNode))
                {
                    Move mover;

                    averageCollisionSize = 0;
                    groupPosition = Vector2d.zero;
                    for (int i = 0; i < movers.Count; i++)
                    {
                        mover = movers[i];
                        groupPosition += mover.Position;
                        averageCollisionSize += mover.CollisionSize;
                    }

                    groupPosition /= movers.Count;
                    averageCollisionSize /= movers.Count;

                    long biggestSqrDistance = 0;
                    long farthestSqrDistance = 0;
                    for (int i = 0; i < movers.Count; i++)
                    {
                        mover = movers[i];

                        long currentSqrDistance = mover.Position.SqrDistance(groupPosition.x, groupPosition.y);
                        if (currentSqrDistance > biggestSqrDistance)
                        {
                            long currentDistance = FixedMath.Sqrt(currentSqrDistance);

                            biggestSqrDistance = currentSqrDistance;
                            radius = currentDistance;
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
            int groupGridSize = averageCollisionSize.CeilToInt();

            if (Pathfinder.GetStartNode(farthestPosition, out groupCurrentNode)
                || Pathfinder.GetClosestViableNode(farthestPosition, farthestPosition, groupGridSize, out groupCurrentNode))
            {
                PathRequestManager.RequestPath(groupCurrentNode, groupDestinationNode, groupGridSize, (_flowField, success) =>
                {
                    if (success)
                    {
                        GroupFlowFields.Clear();
                        GroupFlowFields = _flowField;

                        if (radius == 0)
                        {
                            // we must not have a group then...
                            ExecuteGroupIndividualMove();
                            return;
                        }

                        long expectedSize = averageCollisionSize.Mul(averageCollisionSize).Mul(FixedMath.One * 2).Mul(movers.Count);
                        long groupSize = radius.Mul(radius);

                        if (groupSize > expectedSize || groupPosition.FastDistance(Destination.x, Destination.y) < (radius.Mul(radius)))
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
            for (int i = 0; i < movers.Count; i++)
            {
                mover = movers[i];
                mover.MyMovementGroup = null;
                mover.MyMovementGroupID = -1;
            }
            movers.FastClear();
            GroupFlowFields.Clear();
            MovementGroupHelper.Pool(this);
            calculatedBehaviors = false;
            indexID = -1;
        }

        private void ExecuteIndividualMove()
        {
            movementType = MovementType.Individual;
            for (int i = 0; i < movers.Count; i++)
            {
                Move mover = movers[i];
                mover.IsGroupMoving = false;
                mover.StopMultiplier = Move.DirectStop;
                mover.OnGroupProcessed(Destination);
            }
        }

        private void ExecuteGroupMove()
        {
            movementType = MovementType.Group;
            groupDirection = Destination - groupPosition;

            for (int i = 0; i < movers.Count; i++)
            {
                Move mover = movers[i];
                mover.IsGroupMoving = true;
                mover.StopMultiplier = Move.GroupStop;
                mover.OnGroupProcessed(mover.Position + groupDirection);
            }
        }

        private void ExecuteGroupIndividualMove()
        {
            Debug.Log("individual group moving");
            movementType = MovementType.GroupIndividual;
            for (int i = 0; i < movers.Count; i++)
            {
                Move mover = movers[i];
                mover.IsGroupMoving = true;
                mover.StopMultiplier = Move.GroupDirectStop;
                mover.OnGroupProcessed(Destination);
            }
        }
    }
}
