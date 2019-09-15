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
        public event Action onArrive;
        public event Action onStartMove;
        //Called whenever movement is stopped... i.e. to attack
        public event Action OnStopMove;

        [Lockstep(true)]
        public bool SlowArrival { get; set; }
        public Vector2d AveragePosition { get; set; }

        public int GridSize { get { return (cachedBody.Radius).CeilToInt(); } }

        public Vector2d Position { get { return cachedBody.Position; } }

        public long CollisionSize { get { return cachedBody.Radius; } }

        public MovementGroup MyMovementGroup { get; set; }

        public int MyMovementGroupID { get; set; }

        public bool IsGroupMoving { get; set; }

        public bool IsMoving { get; private set; }

        public Command LastCommand;

        [HideInInspector]
        public Vector2d Destination;

        private const int MinimumOtherStopTime = LockstepManager.FrameRate / 4;
        private const int StuckTimeThreshold = LockstepManager.FrameRate / 4;
        private const int StuckRepathTries = 4;

        private bool hasPath;
        public bool straightPath;
        private bool viableDestination;

        // key = postion, value = direction
        private Dictionary<Vector2d, FlowField> flowFields = new Dictionary<Vector2d, FlowField>();
        private int _pathIndex;

        private int StoppedTime;

        #region Auto stopping properites
        private const int AUTO_STOP_PAUSE_TIME = LockstepManager.FrameRate / 8;
        private int AutoStopPauser;

        private int StopPauseLayer;

        private int CollisionStopPauser;

        private int StopPauseLooker;
        #endregion

        private LSBody cachedBody { get; set; }
        private Turn cachedTurn { get; set; }

        private long timescaledAcceleration;
        private long timescaledDecceleration;
        private bool decelerating;

        private Vector2d lastTargetPos;

        public GridNode currentNode;
        private GridNode destinationNode;
        private Vector2d movementDirection;
        private Vector2d lastDirection;

        // How far we move each update
        private long distanceToMove;
        // How far away the agent stops from the target
        private long closingDistance;
        private long stuckTolerance;

        private int stuckTime;

        private int RepathTries;
        private bool DoPathfind;

        private readonly int collidedCount;
        private readonly ushort collidedID;

        private static RTSAgent tempAgent;
        private readonly bool paused;
        private static Vector2d desiredVelocity;

        [HideInInspector]
        public bool CanMove = true;
        private bool canTurn;

        #region Serialized
        [SerializeField, FixedNumber]
        private long _speed = FixedMath.One * 4;
        public virtual long Speed { get { return _speed; } }
        [SerializeField, FixedNumber]
        private long _acceleration = FixedMath.One * 4;
        public long Acceleration { get { return _acceleration; } }
        [SerializeField, Tooltip("Disable if unit doesn't need to find path, i.e. flying")]
        private bool _canPathfind = true;
        public bool CanPathfind { get { return _canPathfind; } set { _canPathfind = value; } }
        public bool DrawPath = false;

        public event Action onGroupProcessed;
        public bool MoveOnGroupProcessed { get; private set; }
        #endregion

        protected override void OnSetup()
        {
            cachedBody = Agent.Body;
            cachedBody.onContact += HandleCollision;
            cachedTurn = Agent.GetAbility<Turn>();
            canTurn = cachedTurn.IsNotNull();

            timescaledAcceleration = Acceleration.Mul(Speed) / LockstepManager.FrameRate;
            //Cleaner stops with more decelleration
            timescaledDecceleration = timescaledAcceleration * 4;
            //Fatter objects can afford to land imprecisely
            closingDistance = cachedBody.Radius;
            stuckTolerance = ((cachedBody.Radius * Speed) >> FixedMath.SHIFT_AMOUNT) / LockstepManager.FrameRate;
            stuckTolerance *= stuckTolerance;
            this.SlowArrival = true;
        }

        protected override void OnInitialize()
        {
            StoppedTime = 0;

            IsGroupMoving = false;
            MyMovementGroupID = -1;

            AutoStopPauser = 0;
            CollisionStopPauser = 0;
            StopPauseLooker = 0;
            StopPauseLayer = 0;

            StopMultiplier = DirectStop;

            viableDestination = false;

            Destination = Vector2d.zero;
            hasPath = false;
            IsMoving = false;
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
                //TODO: Organize/split this function
                if (IsMoving)
                {
                    // check if agent has to pathfind, otherwise straight path to rely on destination
                    if (CanPathfind)
                    {
                        GetMovementPath();
                    }

                    SetMovementVelocity();
                }
                // agent is not moving
                else
                {
                    decelerating = true;

                    //Slowin' down
                    if (cachedBody.VelocityMagnitude > 0)
                    {
                        cachedBody.Velocity += GetAdjustVector(Vector2d.zero);
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

        private void GetMovementPath()
        {
            if (DoPathfind)
            {
                DoPathfind = false;
                if (viableDestination)
                {
                    if (Pathfinder.GetStartNode(Position, out currentNode))
                    {
                        if (currentNode.DoesEqual(this.destinationNode))
                        {
                            if (this.RepathTries >= 1)
                            {
                                this.Arrive();
                            }
                        }
                        else
                        {
                            this.CheckPath();
                        }
                    }
                }
                else
                {
                    hasPath = false;
                }
            }
        }

        private void CheckPath()
        {
            PathRequestManager.RequestPath(currentNode, destinationNode, this.GridSize, (_flowField, success) =>
            {
                if (success)
                {
                    hasPath = true;
                    flowFields.Clear();
                    flowFields = _flowField;
                }
            });
        }

        private void SetMovementVelocity()
        {
            if (straightPath)
            {
                // no need to check flow field, we got LOS
                movementDirection = Destination - cachedBody.Position;
            }
            else
            {
                // Calculate steering and flocking forces for all agents &
                // work out the force to apply to us based on the flow field grid squares we are on.
                // We apply bilinear interpolation on the 4 grid squares nearest to us to work out our force.
                // http://en.wikipedia.org/wiki/Bilinear_interpolation#Nonlinear

                movementDirection = cachedBody.Position;

                int floorX = movementDirection.x.ToInt();
                int floorY = movementDirection.y.ToInt();

                FlowField flowDirection;
                Dictionary<Vector2d, FlowField> flowFieldBuffer = !IsGroupMoving ? this.flowFields : MyMovementGroup.GroupFlowField;

                if (flowFieldBuffer.TryGetValue(new Vector2d(floorX, floorY), out flowDirection))
                {
                    movementDirection = flowDirection.Direction;

                    lastDirection = movementDirection;
                }
                else
                {
                    // we need to keep moving on...
                    movementDirection = lastDirection;

                    // If we are centered on a grid square with no flow vector this will happen
                    if (movementDirection.Equals(Vector2d.zero))
                    {
                        Debug.Log("zero vector");
                        movementDirection = Destination - cachedBody.Position;
                    }
                }
            }

            if (Destination.x != lastTargetPos.x || Destination.y != lastTargetPos.y)
            {
                lastTargetPos = Destination;
            }

            // This is now the direction we want to be travelling in 
            // needs to be normalized
            movementDirection.Normalize(out distanceToMove);

            long stuckThreshold = timescaledAcceleration / LockstepManager.FrameRate;
            long slowDistance = cachedBody.VelocityMagnitude.Div(timescaledDecceleration);

            if (distanceToMove > slowDistance)
            {
                desiredVelocity = movementDirection;
                if (canTurn)
                {
                    cachedTurn.StartTurnDirection(movementDirection);
                }
            }
            else
            {
                if (distanceToMove < FixedMath.Mul(closingDistance, StopMultiplier))
                {
                    Arrive();
                    //TODO: Don't skip this frame of slowing down
                    return;
                }

                if (distanceToMove > closingDistance)
                {
                    if (canTurn)
                    {
                        cachedTurn.StartTurnDirection(movementDirection);
                    }
                }

                if (distanceToMove <= slowDistance)
                {
                    long closingSpeed = distanceToMove.Div(slowDistance);
                    if (canTurn)
                    {
                        cachedTurn.StartTurnDirection(movementDirection);
                    }

                    desiredVelocity = movementDirection * closingSpeed;
                    decelerating = true;
                    //Reduce occurence of units preventing other units from reaching destination
                    stuckThreshold *= 4;
                }
            }

            //If unit has not moved stuckThreshold in a frame, it's stuck
            stuckTime++;
            if (GetCanAutoStop())
            {
                if (Agent.Body.Position.FastDistance(AveragePosition) <= (stuckThreshold * stuckThreshold))
                {
                    if (stuckTime > StuckTimeThreshold)
                    {
                        if (RepathTries < StuckRepathTries)
                        {
                            DoPathfind = true;
                            RepathTries++;
                        }
                        else
                        {
                            RepathTries = 0;
                            this.Arrive();
                        }
                        stuckTime = 0;
                    }
                }
                else
                {
                    if (stuckTime > 0)
                    {
                        stuckTime -= 1;
                    }

                    RepathTries = 0;
                }
            }

            desiredVelocity *= Speed;

            cachedBody._velocity += GetAdjustVector(desiredVelocity);

            cachedBody.VelocityChanged = true;
        }

        private uint GetNodeHash(GridNode node)
        {
            //TODO: At the moment, the CombinePathVersion is based on the destination... essentially caching the path to the last destination
            //Should this be based on commands instead?
            //Also, a lot of redundancy can be moved into MovementGroupHelper... i.e. getting destination node 
            uint ret = (uint)(node.gridX * GridManager.Width);
            ret += (uint)node.gridY;
            return ret;
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
        /// Start the search process for collisions/obstructions that are in the same attack group.
        /// </summary>
        public void StartLookingForStopPause()
        {
            StopPauseLooker = AUTO_STOP_PAUSE_TIME;
        }
        #endregion

        private Vector2d GetAdjustVector(Vector2d desiredVel)
        {
            var velocityChange = desiredVel - cachedBody._velocity;
            var adjustFastMag = velocityChange.FastMagnitude();
            //Cap acceleration vector magnitude
            long accel = decelerating ? timescaledDecceleration : timescaledAcceleration;

            if (adjustFastMag > accel * (accel))
            {
                var mag = FixedMath.Sqrt(adjustFastMag >> FixedMath.SHIFT_AMOUNT);
                velocityChange *= accel.Div(mag);
            }

            return velocityChange;
        }

        protected override void OnExecute(Command com)
        {
            LastCommand = com;
            if (com.ContainsData<Vector2d>())
            {
                StartFormalMove(com.GetData<Vector2d>());
            }
        }

        public void StartFormalMove(Vector2d position)
        {
            Agent.StopCast(ID);
            IsCasting = true;
            RegisterGroup();
        }

        public void StartMove(Vector2d destination)
        {
            flowFields.Clear();

            IsMoving = true;
            StoppedTime = 0;
            Arrived = false;

            Agent.Animator.SetMovingState(AnimState.Moving);

            //TODO: If next-best-node, autostop more easily
            //Also implement stopping sooner based on distanceToMove
            stuckTime = 0;
            RepathTries = 0;
            IsCasting = true;

            if (!IsGroupMoving)
            {
                DoPathfind = true;
                hasPath = false;

                //For now, use old next-best-node system when size requires consideration
                viableDestination = this.GridSize <= 1 ?
                    Pathfinder.GetEndNode(Agent.Body.Position, destination, out destinationNode) :
                    Pathfinder.GetClosestViableNode(Agent.Body.Position, destination, this.GridSize, out destinationNode);

                this.Destination = destinationNode.WorldPos;
            }
            else
            {
                DoPathfind = false;
                hasPath = true;
                this.Destination = MyMovementGroup.Destination;
            }

            onStartMove?.Invoke();
        }

        public void RegisterGroup(bool moveOnProcessed = true)
        {
            MoveOnGroupProcessed = moveOnProcessed;
            if (MovementGroupHelper.CheckValidAndAlert())
            {
                MovementGroupHelper.LastCreatedGroup.Add(this);
            }
        }

        public void Arrive()
        {
            StopMove();

            if (onArrive.IsNotNull())
            {
                onArrive();
            }
            this.OnArrive();

            //TODO: Reset this variables when changing destination/command
            AutoStopPauser = 0;
            CollisionStopPauser = 0;
            StopPauseLooker = 0;
            StopPauseLayer = 0;

            Arrived = true;
        }

        protected virtual void OnArrive()
        {

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
                StoppedTime = 0;
                IsGroupMoving = false;

                IsCasting = false;
                if (OnStopMove.IsNotNull())
                {
                    OnStopMove();
                }
            }
        }

        public void OnGroupProcessed(Vector2d destination)
        {
            Destination = destination;
            if (MoveOnGroupProcessed)
            {
                StartMove(destination);
                MoveOnGroupProcessed = false;
            }
            else
            {
                this.Destination = destination;
            }

            this.onGroupProcessed?.Invoke();
        }

        protected override void OnStopCast()
        {
            StopMove();
        }

        private void HandleCollision(LSBody other)
        {
            if (!CanMove)
            {
                return;
            }
            if ((tempAgent = other.Agent) == null)
            {
                return;
            }

            Move otherMover = tempAgent.GetAbility<Move>();
            if (ReferenceEquals(otherMover, null) == false)
            {
                if (IsMoving)
                {
                    //If the other mover is moving to a similar point
                    if (otherMover.MyMovementGroupID == MyMovementGroupID
                        || otherMover.Destination.FastDistance(this.Destination) <= (closingDistance * closingDistance))
                    {
                        if (otherMover.IsMoving == false)
                        {
                            if (otherMover.Arrived
                                && otherMover.StoppedTime > MinimumOtherStopTime)
                            {
                                Arrive();
                            }
                        }
                    }

                    if (GetLookingForStopPause())
                    {
                        //As soon as the original collision stop unit is released, units will start breaking out of pauses
                        if (otherMover.GetCanCollisionStop() == false)
                        {
                            StopPauseLayer = -1;
                            PauseAutoStop();
                        }
                        else if (otherMover.GetCanAutoStop() == false)
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
        }

        #region Debug
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (DrawPath && flowFields.IsNotNull() && !straightPath)
            {
                const float height = 0.25f;
                Dictionary<Vector2d, FlowField> flowFieldBuffer = !IsGroupMoving ? this.flowFields : MyMovementGroup.GroupFlowField;
                foreach (KeyValuePair<Vector2d, FlowField> flow in flowFieldBuffer)
                {
                    Vector2d nodePos = flow.Key;
                    FlowField flowField = flow.Value;
                    UnityEditor.Handles.Label(nodePos.ToVector3(height), flowField.Distance.ToString());
                    if (flowField.Direction != Vector2d.zero)
                    {
                        Color hasLOS = flowField.HasLOS ? Color.yellow : Color.blue;
                        DrawArrow.ForGizmo(nodePos.ToVector3(height), flowField.Direction.ToVector3(height), hasLOS);
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
            SaveManager.WriteBoolean(writer, "GroupMoving", IsGroupMoving);
            SaveManager.WriteBoolean(writer, "Moving", IsMoving);
            SaveManager.WriteBoolean(writer, "HasPath", hasPath);
            SaveManager.WriteBoolean(writer, "StraightPath", straightPath);
            SaveManager.WriteBoolean(writer, "ViableDestination", viableDestination);
            SaveManager.WriteInt(writer, "StoppedTime", StoppedTime);
            SaveManager.WriteVector2d(writer, "Destination", Destination);
            SaveManager.WriteBoolean(writer, "Arrived", Arrived);
            SaveManager.WriteVector2d(writer, "AveragePosition", AveragePosition);
            SaveManager.WriteBoolean(writer, "decelerating", decelerating);
            SaveManager.WriteVector2d(writer, "LastTargetPos", lastTargetPos);
            SaveManager.WriteVector2d(writer, "MovementDirection", movementDirection);
        }

        protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.HandleLoadedProperty(reader, propertyName, readValue);
            switch (propertyName)
            {
                case "MyMovementGroupID":
                    MyMovementGroupID = (int)readValue;
                    break;
                case "GroupMoving":
                    IsGroupMoving = (bool)readValue;
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
                case "ViableDestination":
                    viableDestination = (bool)readValue;
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
                case "decelerating":
                    decelerating = (bool)readValue;
                    break;
                case "LastTargetPos":
                    lastTargetPos = LoadManager.LoadVector2d(reader);
                    break;
                case "MovementDirection":
                    movementDirection = LoadManager.LoadVector2d(reader);
                    break;
                default: break;
            }
        }
    }
}