using FastCollections;
using RTSLockstep.Grid;
using RTSLockstep.Pathfinding;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    public class MovementGroup
    {
        public Vector2d Destination { get; private set; }

        public Vector2d groupPosition;

        private Vector2d farthestPosition;

        private GridNode groupCurrentNode;
        private GridNode groupDestinationNode;

        // key = postion, value = direction
        public Dictionary<Vector2d, FlowField> GroupFlowField = new Dictionary<Vector2d, FlowField>();

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
            if (!calculatedBehaviors)
            {
                CalculateAndExecuteBehaviors();
            }

            if (movers.Count == 0)
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

        public void CalculateAndExecuteBehaviors()
        {
            Move mover;

            // check to make sure the group is moving to a good area
            bool viableDestination = Pathfinder.GetEndNode(groupPosition, Destination, out groupDestinationNode);

            if (movers.Count >= MinGroupSize && viableDestination)
            {
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

                    if (Pathfinder.GetStartNode(mover.Position, out mover.currentNode))
                    {
                        // check if mover already has LOS to destination
                        if (Pathfinder.NeedsPath(mover.currentNode, groupDestinationNode, mover.GridSize))
                        {
                            if (mover.straightPath)
                            {
                                mover.straightPath = false;
                            }
                        }
                        else
                        {
                            // mover doesn't have to path find bc they have LOS
                            mover.straightPath = true;
                        }
                    }

                    long currentSqrDistance = mover.Position.SqrDistance(groupPosition.x, groupPosition.y);
                    if (currentSqrDistance > biggestSqrDistance)
                    {
                        long currentDistance = FixedMath.Sqrt(currentSqrDistance);

                        //long distDif = currentDistance - radius;
                        //if (distDif > maxiumDistDif * movers.Count / 128)
                        //{
                        //    Debug.Log("farther away");
                        //    // ExecuteGroupIndividualMove();
                        //    // return;
                        //}

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

                // at this point we know what to do
                calculatedBehaviors = true;

                // Generate a flow field for the entire movement group
                GetGroupMovementPath();
            }
            else
            {
                // try to find the individual agent path
                ExecuteIndividualMove();
            }
        }

        public void GetGroupMovementPath()
        {
            //pass the largest unit?
            int groupGridSize = 2;

            if (Pathfinder.GetStartNode(farthestPosition, out groupCurrentNode)
                || Pathfinder.GetClosestViableNode(farthestPosition, farthestPosition, groupGridSize, out groupCurrentNode))
            {
                PathRequestManager.RequestPath(groupCurrentNode, groupDestinationNode, groupGridSize, (_flowField, success) =>
                {
                    if (success)
                    {
                        GroupFlowField.Clear();
                        GroupFlowField = _flowField;

                        if (radius == 0)
                        {
                            // we must not have a group then...
                            ExecuteGroupIndividualMove();
                            return;
                        }

                        long expectedSize = averageCollisionSize.Mul(averageCollisionSize).Mul(FixedMath.One * 2).Mul(movers.Count);
                        long groupSize = radius.Mul(radius);

                        if (groupSize > expectedSize || groupPosition.FastDistance(Destination.x, Destination.y) < (radius * radius))
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
            GroupFlowField.Clear();
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
