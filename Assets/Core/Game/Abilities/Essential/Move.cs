using Newtonsoft.Json;
using RTSLockstep.Pathfinding;
using RTSLockstep.Grid;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    public class Move : ActiveAbility
    {
        //Stop multipliers determine accuracy required for stopping on the destination
        public const long GroupStop = FixedMath.One / 4;
        public const long GroupDirectStop = FixedMath.One;
        public const long DirectStop = FixedMath.One / 4;

        public long StopMultiplier { get; set; }

        //Has this unit arrived at destination? Default set to false.
        public bool Arrived { get; private set; }

        //Called when unit arrives at destination
        public event Action OnArrive;
        public event Action OnStartMove;
        //Called whenever movement is stopped... i.e. to attack
        public event Action OnStopMove;
        public event Action OnMoveGroupProcessed;

        public bool MoveOnGroupProcessed { get; private set; }

        [Lockstep(true)]
        public bool SlowArrival { get; set; }
        public Vector2d AveragePosition { get; set; }

        // add a little padding to manevour around blockers
        public int GridSize { get { return Agent.Body.Radius.CeilToInt(); } }

        public Vector2d Position { get { return Agent.Body.Position; } }

        public long CollisionSize { get { return Agent.Body.Radius; } }

        public MovementGroup MyMovementGroup { get; set; }
        public int MyMovementGroupID { get; set; }

        public MovementType MyMovementType { get; set; }

        public bool IsMoving { get; private set; }

        [HideInInspector]
        public Vector2d Destination;
        [HideInInspector]
        public bool IsAvoidingLeft;
        private long _minAvoidanceDistance;

        private const int MinimumOtherStopTime = LockstepManager.FrameRate / 4;
        private const int StuckTimeThreshold = LockstepManager.FrameRate / 4;
        private const int StuckRepathTries = 4;

        private bool hasPath;
        private bool straightPath;

        // key = world position, value = vector flow field
        private Dictionary<Vector2d, FlowField> _flowFields = new Dictionary<Vector2d, FlowField>();
        private Dictionary<Vector2d, FlowField> _flowFieldBuffer;

        private int StoppedTime;

        #region Auto stopping properites
        private const int AUTO_STOP_PAUSE_TIME = LockstepManager.FrameRate / 8;
        private int AutoStopPauser;

        private int StopPauseLayer;

        private int CollisionStopPauser;

        private int StopPauseLooker;
        #endregion

        private long timescaledAcceleration;
        private long timescaledDecceleration;
        private bool decelerating;

        public GridNode currentNode;
        private GridNode destinationNode;
        private bool viableDestination;

        // How far we move each update
        private long distanceToMove;
        // How far away the agent stops from the target
        private long _closingDistance;

        private int stuckTime;
        public bool IsStuck;
        private long stuckTolerance;

        private int RepathTries;
        private bool DoPathfind;

        private readonly int collidedCount;
        private readonly ushort collidedID;

        private RTSAgent tempAgent;
        private readonly bool paused;

        private Vector2d movementDirection;
        private Vector2d desiredVelocity;

        public bool CanMove { get; private set; }

        #region Serialized
        [SerializeField, FixedNumber]
        private long _movementSpeed = FixedMath.One * 4;
        public virtual long MovementSpeed { get { return _movementSpeed; } }
        [SerializeField, FixedNumber]
        private long _acceleration = FixedMath.One * 4;
        public long Acceleration { get { return _acceleration; } }
        [SerializeField, Tooltip("Disable if unit doesn't need to find path, i.e. flying")]
        private bool _canPathfind = true;
        public bool CanPathfind { get { return _canPathfind; } set { _canPathfind = value; } }
        public bool DrawPath;
        #endregion

        protected override void OnSetup()
        {
            CanMove = true;
            //   Agent.Body.onContact += HandleCollision;

            DrawPath = false;

            timescaledAcceleration = Acceleration.Mul(MovementSpeed) / LockstepManager.FrameRate;
            //Cleaner stops with more decelleration
            timescaledDecceleration = timescaledAcceleration * 4;
            //Fatter objects can afford to land imprecisely
            _closingDistance = Agent.Body.Radius;
            stuckTolerance = ((Agent.Body.Radius * MovementSpeed) >> FixedMath.SHIFT_AMOUNT) / LockstepManager.FrameRate;
            stuckTolerance *= stuckTolerance;
            SlowArrival = true;
        }

        protected override void OnInitialize()
        {
            StoppedTime = 0;

            MyMovementType = MovementType.Individual;
            MyMovementGroupID = -1;

            AutoStopPauser = 0;
            CollisionStopPauser = 0;
            StopPauseLooker = 0;
            StopPauseLayer = 0;

            StopMultiplier = DirectStop;

            Destination = Vector2d.zero;
            hasPath = false;
            IsMoving = false;
            IsAvoidingLeft = false;
            IsStuck = false;
            stuckTime = 0;
            RepathTries = 0;

            Arrived = true;
            AveragePosition = Agent.Body.Position;
            DoPathfind = false;
        }

        protected override void OnSimulate()
        {
            if (CanMove)
            {
                if (IsMoving)
                {
                    // check if agent has to pathfind, otherwise straight path to rely on destination
                    if (CanPathfind)
                    {
                        ValidateMovementPath();
                    }

                    movementDirection = SetMovementDirection();
                    SetDesiredVelocity();
                }
                else
                {
                    // agent is not moving
                    decelerating = true;

                    //Slowin' down
                    if (Agent.Body.VelocityMagnitude > 0)
                    {
                        Agent.Body.Velocity += GetAdjustVector(Vector2d.zero);
                    }

                    StoppedTime++;
                }
                decelerating = false;

                AutoStopPauser--;
                CollisionStopPauser--;
                StopPauseLooker--;
                AveragePosition = AveragePosition.Lerped(Agent.Body.Position, FixedMath.One / 2);
            }
        }

        private void ValidateMovementPath()
        {
            if (GridSize <= 1 && Pathfinder.GetStartNode(Position, out currentNode)
                || Pathfinder.GetClosestViableNode(Position, Position, GridSize, out currentNode))
            {
                if (DoPathfind)
                {
                    DoPathfind = false;
                    if (viableDestination)
                    {
                        if (currentNode.DoesEqual(destinationNode))
                        {
                            if (RepathTries >= 1)
                            {
                                Arrive();
                            }
                        }
                        else
                        {
                            if (Pathfinder.NeedsPath(currentNode, destinationNode, GridSize))
                            {
                                straightPath = false;

                                PathRequestManager.RequestPath(currentNode, destinationNode, GridSize, (flowFields, success) =>
                                {
                                    _flowFields.Clear();
                                    if (success)
                                    {
                                        hasPath = true;
                                        _flowFields = flowFields;
                                    }
                                    else
                                    {
                                        // no path found, will have to start over
                                        hasPath = false;
                                    }
                                });
                            }
                            else
                            {
                                // no path required
                                straightPath = true;
                            }
                        }
                    }
                    else
                    {
                        // can't get to destination
                        hasPath = false;
                    }
                }
            }
        }

        private Vector2d SetMovementDirection()
        {
            if (straightPath)
            {
                // no need to check flow field, we got LOS
                movementDirection = Destination - Agent.Body.Position;
            }
            else if (hasPath)
            {
                _flowFieldBuffer = MyMovementType == MovementType.Individual ? _flowFields : MyMovementGroup.GroupFlowFields;

                if (_flowFieldBuffer.Count > 0)
                {
                    if (_flowFieldBuffer.TryGetValue(currentNode.GridPos, out FlowField flowField))
                    {
                        if (flowField.HasLOS)
                        {
                            // we have no more use for flow fields if the agent has line of sight to destination
                            straightPath = true;
                            movementDirection = Destination - Agent.Body.Position;
                        }
                        else
                        {
                            //  Calculate steering for agent
                            movementDirection = SteeringBehaviorFlowField(currentNode.GridPos);
                        }
                    }
                    else
                    {
                        // agent landed on a spot with no flow, 
                        // try to course correct by finding clostest flow field to move towards
                        movementDirection = Pathfinder.ClosestFlowFieldPostion(currentNode.GridPos, _flowFieldBuffer, Agent.MyStats.Sight) - Agent.Body.Position;
                    }
                }
            }

            // This is now the direction we want to be travelling in 
            // needs to be normalized
            movementDirection.Normalize(out distanceToMove);

            if (MyMovementType != MovementType.Individual)
            {
                // Calculate steering and flocking forces for all agents
                movementDirection += CalculateGroupBehaviors();
            }

            // avoid any intersection agents!
            movementDirection += SteeringBehaviourAvoid();

            return movementDirection;
        }

        private void SetDesiredVelocity()
        {
            long stuckThreshold = timescaledAcceleration / LockstepManager.FrameRate;
            long slowDistance = Agent.Body.VelocityMagnitude.Div(timescaledDecceleration);

            if (distanceToMove > slowDistance)
            {
                desiredVelocity = movementDirection;
            }
            else if (distanceToMove <= slowDistance && distanceToMove > FixedMath.Mul(_closingDistance, StopMultiplier))
            {
                long closingSpeed = distanceToMove.Div(slowDistance);

                desiredVelocity = movementDirection * closingSpeed;
                decelerating = true;
                //Reduce occurence of units preventing other units from reaching destination
                stuckThreshold *= 5;
            }

            CheckMovementStatus(stuckThreshold);

            if (distanceToMove < FixedMath.Mul(_closingDistance, StopMultiplier))
            {
                Arrive();
                //TODO: Don't skip this frame of slowing down
                return;
            }
            else if (Agent.MyStats.CanTurn)
            {
                Agent.MyStats.CachedTurn.StartTurnDirection(movementDirection);
            }

            // cap accelateration
            long currentVelocity = desiredVelocity.SqrMagnitude();
            if (currentVelocity > Acceleration)
            {
                desiredVelocity *= (Acceleration / FixedMath.Sqrt(currentVelocity)).CeilToInt();
            }

            //Multiply our direction by speed for our desired speed
            desiredVelocity *= MovementSpeed;

            // Cap speed as required
            var currentSpeed = Agent.Body.Velocity.Magnitude();
            if (currentSpeed > MovementSpeed)
            {
                desiredVelocity *= (MovementSpeed / currentSpeed).CeilToInt();
            }

            //Apply the force
            Agent.Body.Velocity += GetAdjustVector(desiredVelocity);
        }

        private void CheckMovementStatus(long stuckThreshold)
        {
            stuckTime++;
            // if auto stopping is paused (i.e. attack moving), the abili
            if (GetCanAutoStop())
            {
                //If unit has not moved stuckThreshold in a frame, it's stuck
                if (Agent.Body.Position.FastDistance(AveragePosition) <= (stuckThreshold * stuckThreshold))
                {
                    if (stuckTime > StuckTimeThreshold)
                    {
                        if (RepathTries < StuckRepathTries)
                        {
                            Debug.Log("Stuck Agent!");
                            // attempt to repath agent by themselves
                            if (MyMovementGroup.IsNotNull())
                            {
                                MyMovementGroup.Remove(this);
                            }
                            DoPathfind = true;
                            hasPath = false;
                            straightPath = false;
                            RepathTries++;
                        }
                        else
                        {
                            Debug.Log("Stuck Agent arriving!");
                            // we've tried to many times, we stuck stuck
                            IsStuck = true;
                            Arrive();
                        }

                        stuckTime = 0;
                    }
                }
                else
                {
                    IsStuck = false;

                    if (stuckTime > 0)
                    {
                        stuckTime -= 1;
                    }

                    RepathTries = 0;
                }
            }
        }

        private Vector2d SteeringBehaviorFlowField(Vector2d gridPos)
        {
            //Work out the force to apply to us based on the flow field grid squares we are on.
            //we apply bilinear interpolation on the 4 grid squares nearest to us to work out our force.
            // http://en.wikipedia.org/wiki/Bilinear_interpolation#Nonlinear

            //Top left Coordinate of the 4
            int floorX = gridPos.x.CeilToInt();
            int floorY = gridPos.y.CeilToInt();

            //The 4 weights we'll interpolate, see http://en.wikipedia.org/wiki/File:Bilininterp.png for the coordinates

            Vector2d f00 = _flowFieldBuffer.ContainsKey(new Vector2d(floorX, floorY)) ? _flowFieldBuffer[new Vector2d(floorX, floorY)].Direction : Vector2d.zero;
            Vector2d f01 = _flowFieldBuffer.ContainsKey(new Vector2d(floorX, floorY + 1)) ? _flowFieldBuffer[new Vector2d(floorX, floorY)].Direction : Vector2d.zero;
            Vector2d f10 = _flowFieldBuffer.ContainsKey(new Vector2d(floorX + 1, floorY)) ? _flowFieldBuffer[new Vector2d(floorX, floorY)].Direction : Vector2d.zero;
            Vector2d f11 = _flowFieldBuffer.ContainsKey(new Vector2d(floorX + 1, floorY + 1)) ? _flowFieldBuffer[new Vector2d(floorX, floorY)].Direction : Vector2d.zero;

            //Do the x interpolations
            int xWeight = gridPos.x.ToInt() - floorX;

            Vector2d top = f00 * (1 - xWeight) + (f10 * xWeight);
            Vector2d bottom = f01 * (1 - xWeight) + (f11 * xWeight);

            //Do the y interpolation
            int yWeight = gridPos.y.ToInt() - floorY;

            //This is now the direction we want to be travelling in (needs to be normalized)
            Vector2d desiredDirection = top * (1 - yWeight) + (bottom * yWeight);

            //If we are centered on a grid square with no vector this will happen
            if (desiredDirection.Equals(Vector2d.zero))
            {
                return Vector2d.zero;
            }

            return desiredDirection;
        }

        private Vector2d CalculateGroupBehaviors()
        {
            int _neighboursCount = 0;
            long neighborRadius = FixedMath.One * 3;

            Vector2d totalForce = Vector2d.zero;
            Vector2d averageHeading = Vector2d.zero;
            Vector2d centerOfMass = Vector2d.zero;

            for (int i = 0; i < AgentController.GlobalAgents.Length; i++)
            {
                bool neighborFound = false;
                RTSAgent a = AgentController.GlobalAgents[i];
                if (a.IsNotNull()
                    && a.GlobalID != Agent.GlobalID
                    && a.MyAgentType == AgentType.Unit)
                {
                    Vector2d distance = Position - a.Body.Position;
                    //  Normalize returns the magnitude to use for calculations
                    distance.Normalize(out long distanceMag);

                    // agent is within range of neighbor
                    if (distanceMag < neighborRadius)
                    {
                        //  Move away from agents we are too close too
                        //  Vector away from other agent
                        totalForce += (distance * (1 - (distanceMag / neighborRadius)));

                        //  Change our direction to be closer to our neighbours
                        //  That are within the max distance and are moving
                        if (a.Body.VelocityMagnitude > 0)
                        {
                            //Sum up our headings
                            Vector2d head = a.Body._velocity;
                            head.Normalize();
                            averageHeading += head;
                        }

                        //  Move nearer to those entities we are near but not near enough to
                        //  Sum up the position of our neighbours
                        centerOfMass += a.Body.Position;

                        neighborFound = true;
                    }

                    if (neighborFound)
                    {
                        _neighboursCount++;
                    }
                }
            }

            if (_neighboursCount > 0)
            {
                //  Separation calculates a force to move away from all of our neighbours. 
                //  We do this by calculating a force from them to us and scaling it so the force is greater the nearer they are.
                Vector2d _seperation = totalForce * (Acceleration / (_neighboursCount * FixedMath.One));

                //  Cohesion and Alignment are only for when other agents going to a similar location as us, 
                //  otherwise we’ll get caught up when other agents move past.

                //  Alignment calculates a force so that our direction is closer to our neighbours.
                //  It does this similar to cohesion, but by summing up the direction vectors (normalised velocities) of ourself 
                //  and our neighbours and working out the average direction.
                //  Divide by amount of neighbors to get the average heading
                Vector2d _alignment = (averageHeading / _neighboursCount);
                //  Cohesion calculates a force that will bring us closer to our neighbours, so we move together as a group rather than individually.
                //  Cohesion calculates the average position of our neighbours and ourself, and steers us towards it
                //  seek this position
                Vector2d _cohesion = SteeringBehaviorSeek(centerOfMass / _neighboursCount);

                //Combine them to come up with a total force to apply, decreasing the effect of cohesion
                return (_seperation * 2) + (_alignment * FixedMath.Create(0.5f)) + (_cohesion * FixedMath.Create(0.2f));
            }
            else
            {
                return Vector2d.zero;
            }
        }

        private Vector2d SteeringBehaviorSeek(Vector2d _destination)
        {
            if (_destination == Position)
            {
                return Vector2d.zero;
            }

            //Desired change of location
            Vector2d desired = _destination - Position;
            desired.Normalize(out long desiredMag);
            //Desired velocity (move there at maximum speed)
            return desiredMag > 0 ? desired * (MovementSpeed / desiredMag) : Vector2d.zero;
        }

        protected virtual Func<RTSAgent, bool> AvoidAgentConditional
        {
            get
            {
                bool agentConditional(RTSAgent other)
                {
                    // check to make sure we didn't find ourselves and that the other agent can move
                    if (Agent.GlobalID != other.GlobalID
                    && other.GetAbility<Move>())
                    {
                        (other.Body.Position - Position).Normalize(out long distanceMag);

                        if (distanceMag < _minAvoidanceDistance)
                        {
                            _minAvoidanceDistance = distanceMag;
                            return true;
                        }
                    }

                    // we don't need to avoid!
                    return false;
                }

                return agentConditional;
            }
        }

        private Vector2d SteeringBehaviourAvoid()
        {
            if (Agent.Body.Velocity.SqrMagnitude() <= Agent.Body.Radius)
            {
                return Vector2d.zero;
            }

            _minAvoidanceDistance = FixedMath.One * 6;

            Func<RTSAgent, bool> avoidAgentConditional = AvoidAgentConditional;

            RTSAgent closetAgent = InfluenceManager.Scan(
                     Position,
                     Agent.MyStats.Sight,
                     avoidAgentConditional,
                     (bite) =>
                     {
                         // trying to avoid all agents, we don't care about alliances!
                         return true;
                     }
                 );

            if (closetAgent.IsNull())
            {
                return Vector2d.zero;
            }

            Vector2d resultVector = Vector2d.zero;

            LSBody collisionBody = closetAgent.Body;
            long ourVelocityLengthSquared = Agent.Body.Velocity.SqrMagnitude();
            Vector2d combinedVelocity = Agent.Body.Velocity + collisionBody.Velocity;
            long combinedVelocityLengthSquared = combinedVelocity.SqrMagnitude();

            //We are going in the same direction and they aren't avoiding
            if (combinedVelocityLengthSquared > ourVelocityLengthSquared && !closetAgent.GetAbility<Move>().IsAvoidingLeft)
            {
                return Vector2d.zero;
            }

            //Steer to go around it
            ColliderType otherType = closetAgent.Body.Shape;
            if (otherType == ColliderType.Circle)
            {
                Vector2d vectorInOtherDirection = collisionBody.Position - Position;

                //Are we more left or right of them
                bool isLeft = false;
                if (closetAgent.GetAbility<Move>().IsAvoidingLeft)
                {
                    //If they are avoiding, avoid with the same direction as them, so we go the opposite way
                    isLeft = closetAgent.GetAbility<Move>().IsAvoidingLeft;
                }
                else
                {
                    //http://stackoverflow.com/questions/13221873/determining-if-one-2d-vector-is-to-the-right-or-left-of-another
                    long dot = Agent.Body.Velocity.x * -vectorInOtherDirection.y + Agent.Body.Velocity.y * vectorInOtherDirection.x;
                    isLeft = dot > 0;
                }
                IsAvoidingLeft = isLeft;

                //Calculate a right angle of the vector between us
                //http://www.gamedev.net/topic/551175-rotate-vector-90-degrees-to-the-right/#entry4546571
                resultVector = isLeft ? new Vector2d(-vectorInOtherDirection.y, vectorInOtherDirection.x) : new Vector2d(vectorInOtherDirection.y, -vectorInOtherDirection.x);
                resultVector.Normalize();

                //Move it out based on our radius + theirs
                resultVector *= (Agent.Body.Radius + closetAgent.Body.Radius);
            }
            else
            {
                //Not supported
                //otherType == B2Shape.e_polygonShape
                Debug.Log("Collider not supported for avoidance");
            }

            //Steer torwards it, increasing force based on how close we are
            return (resultVector / _minAvoidanceDistance);
        }

        #region Autostopping
        public bool GetCanAutoStop()
        {
            return AutoStopPauser <= 0;
        }

        public bool GetCanCollisionStop()
        {
            return CollisionStopPauser <= 0;
        }

        public void PauseAutoStop()
        {
            AutoStopPauser = AUTO_STOP_PAUSE_TIME;
        }

        public void PauseCollisionStop()
        {
            CollisionStopPauser = AUTO_STOP_PAUSE_TIME;
        }

        //TODO: Improve the naming
        private bool GetLookingForStopPause()
        {
            return StopPauseLooker >= 0;
        }

        /// <summary>
        /// Start the search process for collisions/obstructions that are in the same group.
        /// </summary>
        public void StartLookingForStopPause()
        {
            StopPauseLooker = AUTO_STOP_PAUSE_TIME;
        }
        #endregion

        private Vector2d GetAdjustVector(Vector2d desiredVelocity)
        {
            //The velocity change we want
            var velocityChange = desiredVelocity - Agent.Body._velocity;
            var adjustFastMag = velocityChange.FastMagnitude();
            //Cap acceleration vector magnitude
            long accel = decelerating ? timescaledDecceleration : timescaledAcceleration;

            if (adjustFastMag > accel * (accel))
            {
                var mag = FixedMath.Sqrt(adjustFastMag >> FixedMath.SHIFT_AMOUNT);
                //Convert to a force
                velocityChange *= accel.Div(mag);
            }

            return velocityChange;
        }

        protected override void OnExecute(Command com)
        {
            if (com.ContainsData<Vector2d>())
            {
                Agent.StopCast(ID);
                IsCasting = true;
                RegisterGroup();
            }
        }

        public void RegisterGroup(bool moveOnProcessed = true)
        {
            MoveOnGroupProcessed = moveOnProcessed;
            if (MovementGroupHelper.CheckValidAndAlert())
            {
                MovementGroupHelper.LastCreatedGroup.Add(this);
            }
        }

        public void MoveGroupProcessed(Vector2d _destination)
        {
            if (MoveOnGroupProcessed)
            {
                StartMove(_destination);
                MoveOnGroupProcessed = false;
            }
            else
            {
                Destination = _destination;
            }

            OnMoveGroupProcessed?.Invoke();
        }

        public void StartMove(Vector2d _destination, bool allowUnwalkableEndNode = false)
        {
            _flowFields.Clear();
            straightPath = false;

            IsMoving = true;
            StoppedTime = 0;
            Arrived = false;

            // handle override with onStartMove action
            Agent.Animator.SetMovingState(AnimState.Moving);

            //TODO: If next-best-node, autostop more easily
            //Also implement stopping sooner based on distanceToMove
            stuckTime = 0;
            RepathTries = 0;
            IsCasting = true;

            // still need to check for viable destination for agents in group
            // if size requires consideration, use old next-best-node system
            // also a catch in case GetEndNode returns null
            viableDestination = (GridSize <= 1 && Pathfinder.GetEndNode(Position, _destination, out destinationNode)
                || Pathfinder.GetClosestViableNode(Position, _destination, GridSize, out destinationNode));

            Destination = destinationNode.IsNotNull() ? destinationNode.WorldPos : Vector2d.zero;

            if (MyMovementType == MovementType.Individual)
            {
                DoPathfind = true;
                hasPath = false;
            }
            else
            {
                // we must be group moving
                DoPathfind = false;
                hasPath = true;
            }

            OnStartMove?.Invoke();
        }

        public void Arrive()
        {
            StopMove();

            //TODO: Reset these variables when changing destination/command
            AutoStopPauser = 0;
            CollisionStopPauser = 0;
            StopPauseLooker = 0;
            StopPauseLayer = 0;

            stuckTime = 0;

            Arrived = true;

            OnArrive?.Invoke();
        }

        public void StopMove()
        {
            if (IsMoving)
            {
                if (MyMovementGroup.IsNotNull())
                {
                    MyMovementGroup.Remove(this);
                }

                IsMoving = false;
                IsAvoidingLeft = false;
                StoppedTime = 0;

                _flowFields.Clear();

                movementDirection = Vector2d.zero;
                desiredVelocity = Vector2d.zero;

                DoPathfind = false;
                hasPath = false;
                straightPath = false;

                IsCasting = false;

                Agent.Animator.SetIdleState();

                OnStopMove?.Invoke();
            }
        }

        protected override void OnStopCast()
        {
            StopMove();
        }

        // this helps prevent agents in large groups from trying to get to the middle of the group
        private void HandleCollision(LSBody other)
        {
            if (!CanMove || other.Agent.IsNull())
            {
                return;
            }

            tempAgent = other.Agent;

            Move otherMover = tempAgent.GetAbility<Move>();
            if (otherMover.IsNotNull() && IsMoving)
            {
                //if agent is assigned move group and the other mover is moving to a similar point
                if (otherMover.MyMovementGroupID == MyMovementGroupID
                    || otherMover.Destination.FastDistance(Destination) <= (_closingDistance * _closingDistance))
                {
                    if (!otherMover.IsMoving && !otherMover.Agent.IsCasting)
                    {
                        if (otherMover.Arrived
                            && otherMover.StoppedTime > MinimumOtherStopTime)
                        {
                            Debug.Log("Arrive after collision");
                            Arrive();
                        }
                    }
                }

                if (GetLookingForStopPause())
                {
                    //As soon as the original collision stop unit is released, units will start breaking out of pauses
                    if (!otherMover.GetCanCollisionStop())
                    {
                        StopPauseLayer = -1;
                        PauseAutoStop();
                    }
                    else if (!otherMover.GetCanAutoStop())
                    {
                        if (otherMover.StopPauseLayer < StopPauseLayer)
                        {
                            StopPauseLayer = otherMover.StopPauseLayer + 1;
                            PauseAutoStop();
                        }
                    }
                }
            }
        }

        #region Debug
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (DrawPath && _flowFieldBuffer.IsNotNull()) // && !straightPath
            {
                const float height = 0.25f;
                foreach (KeyValuePair<Vector2d, FlowField> flow in _flowFieldBuffer)
                {
                    FlowField flowField = flow.Value;
                    UnityEditor.Handles.Label(flowField.WorldPos.ToVector3(height), flowField.Distance.ToString());
                    if (flowField.Direction != Vector2d.zero)
                    {
                        Color hasLOS = flowField.HasLOS ? Color.yellow : Color.blue;
                        DrawArrow.ForGizmo(flowField.WorldPos.ToVector3(height), flowField.Direction.ToVector3(height), hasLOS);
                    }
                }
            }
        }
#endif
        #endregion
        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
            SaveManager.WriteInt(writer, "MyMovementGroupID", MyMovementGroupID);
            SaveManager.WriteBoolean(writer, "Moving", IsMoving);
            SaveManager.WriteBoolean(writer, "HasPath", hasPath);
            SaveManager.WriteBoolean(writer, "StraightPath", straightPath);
            SaveManager.WriteInt(writer, "StoppedTime", StoppedTime);
            SaveManager.WriteVector2d(writer, "Destination", Destination);
            SaveManager.WriteBoolean(writer, "Arrived", Arrived);
            SaveManager.WriteVector2d(writer, "AveragePosition", AveragePosition);
            SaveManager.WriteBoolean(writer, "Decelerating", decelerating);
        }

        protected override void OnLoadProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.OnLoadProperty(reader, propertyName, readValue);
            switch (propertyName)
            {
                case "MyMovementGroupID":
                    MyMovementGroupID = (int)readValue;
                    break;
                case "Moving":
                    IsMoving = (bool)readValue;
                    break;
                case "HasPath":
                    hasPath = (bool)readValue;
                    break;
                case "StraightPath":
                    straightPath = (bool)readValue;
                    break;
                case "StoppedTime":
                    StoppedTime = (int)readValue;
                    break;
                case "Destination":
                    Destination = LoadManager.LoadVector2d(reader);
                    break;
                case "Arrived":
                    Arrived = (bool)readValue;
                    break;
                case "AveragePosition":
                    AveragePosition = LoadManager.LoadVector2d(reader);
                    break;
                case "Decelerating":
                    decelerating = (bool)readValue;
                    break;
                default: break;
            }
        }
    }
}